using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller
{
    /// <summary>
    /// Static engine orchestrating all combinatorial optimization tasks.
    /// Handles record persistence, theoretical potential calculations, tag mining, and set packing.
    /// </summary>
    public static class ComboController
    {
        private static readonly Dictionary<string, ComputationRecord> _records;

        /// <summary>
        /// Initializes the static controller by loading existing computation records from disk.
        /// </summary>
        static ComboController()
        {
            _records = ComputationRecordFactory.LoadRecords();
        }

        #region Record Management

        /// <summary>
        /// Retrieves a computation record from the local cache using its configuration hash.
        /// </summary>
        public static ComputationRecord GetRecord(string hash)
        {
            _records.TryGetValue(hash, out var record);
            return record;
        }

        /// <summary>
        /// Saves a record to the cache and triggers asynchronous persistence to the local file system.
        /// </summary>
        public static void UpsertRecord(ComputationRecord record)
        {
            if (record == null) return;
            _records[record.ConfigurationHash] = record;
            ComputationRecordFactory.SaveRecords(_records.Values);
        }

        #endregion

        #region Phase 0: Potential Analysis

        /// <summary>
        /// Calculates the MaxPotentialScore for every tag in the provided working pool.
        /// The calculation respects the incompatibility masks present in the input tags.
        /// </summary>
        /// <param name="workingPool">The list of tags to process (Core + User Overrides).</param>
        /// <param name="ct">Cancellation token to abort the parallel operation.</param>
        /// <param name="logger">Optional delegate for progress logging.</param>
        /// <param name="maxComboSize">The maximum size of a combo (default 5).</param>
        /// <returns>A new list of Tag structs with updated MaxPotentialScore values.</returns>
        public static List<Tag> ComputeAllMaxPotentialScores(
            List<Tag> workingPool,
            CancellationToken ct,
            Action<LogEventId, long>? logger = null)
        {
            // Convert to array for thread-safe indexed access.
            int maxComboSize = 5;
            var sourceTags = workingPool.ToArray();
            int count = sourceTags.Length;
            var resultArray = new Tag[count];

            // 1. Establish global heuristic bounds based on the input pool
            // These values are critical for the pruning logic in the recursive solver.
            int maxBaseSub = sourceTags.Max(t => t.BaseSubs);
            int maxCats = sourceTags.Max(t => t.CategoryMask.CountBits());

            // 2. Precompute multiplier growth for optimization
            float[] maxGainPowers = PrecomputeGainPowers(maxComboSize, maxCats);

            logger?.Invoke(LogEventId.EngineStarted, count);

            // 3. Parallel Execution
            // Each tag is processed independently to find its best possible combination.
            var options = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            try
            {
                Parallel.For(0, count, options, (i, state) =>
                {
                    // Check cancellation at the iteration level
                    if (ct.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    var rootTag = sourceTags[i];

                    // Candidate Filtering:
                    // Only select tags from the PROVIDED pool that are not the root itself 
                    // and are not marked as incompatible in the root's mask.
                    var candidates = sourceTags
                        .Where((t, idx) => idx != i && !rootTag.IncompatibilityMask.IsSet(t.Index))
                        .OrderByDescending(t => t.BaseSubs)
                        .ToList();

                    long localBestScore = 0;
                    var initialCombo = new CandidateCombo();
                    initialCombo.AddTag(rootTag);

                    // Execute the recursive solver
                    SolveMaxPotentialRecursive(initialCombo, candidates, maxComboSize, -1, ref localBestScore, maxGainPowers, maxBaseSub, maxCats);

                    // Reconstruction:
                    // Create a new Tag struct with the updated score (Tags are immutable).
                    resultArray[i] = new Tag(
                        rootTag.Index,
                        rootTag.BaseSubs,
                        rootTag.IncompatibilityMask,
                        rootTag.CategoryMask,
                        (int)localBestScore // Update the score here
                    );

                    // Report progress
                    logger?.Invoke(LogEventId.SearchDepthChanged, i);
                });
            }
            catch (OperationCanceledException)
            {
                // Graceful exit on cancellation, returning partial or empty results is handled by caller logic
                throw;
            }

            logger?.Invoke(LogEventId.ComputationCompleted, count);

            // Return the fully reconstructed list
            return resultArray.ToList();
        }

        #endregion

        #region Phase 1: Mining (Best Loadout)

        /// <summary>
        /// Identifies the top N combinations of size K. Used as a standalone solver or as a mining phase for packing.
        /// </summary>
        public static List<Combo> ComputeBestLoadout(
            CancellationToken ct,
            IEnumerable<Tag> sourceTags,
            int n = 10,
            int k = 5)
        {
            var tagList = sourceTags as List<Tag> ?? sourceTags.ToList();
            if (tagList.Count == 0) return new List<Combo>();

            // Heuristic preparation
            int maxBaseSub = tagList.Max(t => t.BaseSubs);
            int maxCats = tagList.Max(t => t.CategoryMask.CountBits());
            float[] gainPowers = PrecomputeGainPowers(k, maxCats);

            tagList.Sort((a, b) => b.MaxPotentialScore.CompareTo(a.MaxPotentialScore));

            var topCombos = new PriorityQueue<Combo, int>(n);
            int minScoreToBeat = 0;
            Tag[] pathBuffer = new Tag[k];

            for (int i = 0; i < tagList.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var rootTag = tagList[i];

                if (topCombos.Count == n && rootTag.MaxPotentialScore <= minScoreToBeat) break;

                var state = new CandidateCombo();
                state.AddTag(rootTag);
                pathBuffer[0] = rootTag;

                SolveMiningRecursive(tagList, state, pathBuffer, k, i, topCombos, n, ref minScoreToBeat, maxBaseSub, maxCats, gainPowers);
            }

            return FinalizePriorityQueue(topCombos);
        }

        #endregion

        #region Phase 2: Packing (Disjoint Loadout)

        /// <summary>
        /// Solves the Set Packing problem to find an optimal set of non-overlapping combinations.
        /// Employs an adaptive pool growth strategy starting at 2,000 combos.
        /// </summary>
        public static List<Combo> ComputeBestDisjointLoadout(
            CancellationToken ct,
            Action<LogEventId, long> logger,
            IEnumerable<Tag> tags,
            int loadoutSize = 10,
            int comboSize = 5)
        {
            int poolSize = 2000;
            int bestScoreGlobal = 0;
            List<Combo> bestLoadoutGlobal = new List<Combo>();
            var allTagsList = tags.ToList();

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                // 1. Standard Mining (Basis for Pareto Proof)
                logger?.Invoke(LogEventId.MiningPhaseStarted, poolSize);
                var standardPool = ComputeBestLoadout(ct, allTagsList, n: poolSize, k: comboSize);
                int proofThreshold = (standardPool.Count > 0) ? standardPool[^1].Score : 0;

                // 2. Viability Check & Diversity Injection
                var (searchPool, greedyScore) = PrepareSearchPool(standardPool, allTagsList, comboSize, loadoutSize, ct);

                if (greedyScore == -1)
                {
                    poolSize *= 2;
                    logger?.Invoke(LogEventId.PoolGrowth, poolSize);
                    continue;
                }

                // 3. Recursive Packing
                logger?.Invoke(LogEventId.PackingPhaseStarted, greedyScore);
                var (loadout, score) = OptimizeSetPacking(searchPool, loadoutSize, greedyScore, logger, ct);

                if (score > bestScoreGlobal)
                {
                    bestScoreGlobal = score;
                    bestLoadoutGlobal = loadout;
                    logger?.Invoke(LogEventId.NewGlobalBest, score);
                }

                // 4. Pareto Optimality Verification
                long theoreticalMaxPotential = (long)proofThreshold * loadoutSize;
                if (standardPool.Count < poolSize || bestScoreGlobal >= theoreticalMaxPotential)
                {
                    logger?.Invoke(LogEventId.ComputationCompleted, bestScoreGlobal);
                    return bestLoadoutGlobal;
                }

                poolSize *= 2;
                logger?.Invoke(LogEventId.PoolGrowth, poolSize);
            }
        }

        #endregion

        #region Private Engine Methods (Recursive & Logic)
        /// <summary>
        /// Generates the multiplier growth table based on combo size and category limits.
        /// Growth factor is 3x per step.
        /// </summary>
        private static float[] PrecomputeGainPowers(int k, int maxCat)
        {
            int budget = k * maxCat;
            float[] powers = new float[budget + 1];
            float p = 1f;
            for (int i = 0; i <= budget; i++) { powers[i] = p; p *= 3f; }
            return powers;
        }

        private static List<Combo> FinalizePriorityQueue(PriorityQueue<Combo, int> queue)
        {
            var results = new List<Combo>(queue.Count);
            while (queue.Count > 0) results.Add(queue.Dequeue());
            results.Reverse();
            return results;
        }

        private static (List<Combo> pool, int greedyScore) PrepareSearchPool(List<Combo> pool, List<Tag> allTags, int k, int targetCount, CancellationToken ct)
        {
            int score = TryFastGreedySolution(pool, targetCount);
            if (score != -1) return (pool, score);

            var mixedSet = new HashSet<Combo>(pool);
            foreach (var t in allTags)
            {
                var champion = ComputeBestLoadout(ct, new List<Tag> { t }, 1, k).FirstOrDefault();
                if (champion != null) mixedSet.Add(champion);
            }

            var result = mixedSet.OrderByDescending(c => c.Score).ToList();
            return (result, TryFastGreedySolution(result, targetCount));
        }

        private static (List<Combo> loadout, int score) OptimizeSetPacking(List<Combo> sourcePool, int targetCount, int initialFloor, Action<LogEventId, long> logger, CancellationToken ct)
        {
            var pool = sourcePool.Select((c, i) => new PackedCombo(i, c)).ToArray();
            var bestIndices = new int[targetCount];
            int bestTotalScore = initialFloor;
            var path = new int[targetCount];

            SolvePackingRecursive(pool, 0, 0, 0, TagMask.Empty, targetCount, path, bestIndices, ref bestTotalScore, logger, ct);

            if (bestTotalScore > 0 && bestIndices.All(x => x == 0))
                return ReconstructGreedy(sourcePool, targetCount);

            return (bestIndices.Select(idx => sourcePool[idx]).ToList(), bestTotalScore);
        }

        private static void SolvePackingRecursive(PackedCombo[] pool, int startIdx, int count, int score, TagMask mask, int target, int[] path, int[] bestIdx, ref int bestScore, Action<LogEventId, long> logger, CancellationToken ct)
        {
            if (count == target)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    Array.Copy(path, bestIdx, target);
                }
                return;
            }

            int remaining = target - count;
            if (startIdx + remaining > pool.Length) return;

            // Global Pruning
            long theoreticalMax = score;
            for (int k = 0; k < remaining; k++) theoreticalMax += pool[startIdx + k].Score;
            if (theoreticalMax <= bestScore) return;

            for (int i = startIdx; i < pool.Length; i++)
            {
                if (count == 0) logger?.Invoke(LogEventId.SearchDepthChanged, i);
                if (count <= 1) ct.ThrowIfCancellationRequested();

                ref var candidate = ref pool[i];
                long optimistic = score + (long)candidate.Score * remaining;
                if (optimistic <= bestScore) break;

                if (candidate.UsedTagsMask.Any(mask)) continue;

                path[count] = candidate.OriginalIndex;
                var nextMask = mask;
                nextMask.Or(candidate.UsedTagsMask);

                SolvePackingRecursive(pool, i + 1, count + 1, score + candidate.Score, nextMask, target, path, bestIdx, ref bestScore, logger, ct);
            }
        }

        private static int TryFastGreedySolution(List<Combo> pool, int targetCount)
        {
            int count = 0, score = 0;
            var mask = TagMask.Empty;
            foreach (var combo in pool)
            {
                var cMask = TagMask.Empty;
                foreach (var t in combo.Tags) cMask.SetBit(t.Index);
                if (!cMask.Any(mask)) { mask.Or(cMask); score += combo.Score; count++; if (count == targetCount) return score; }
            }
            return -1;
        }

        private static (List<Combo>, int) ReconstructGreedy(List<Combo> pool, int targetCount)
        {
            var res = new List<Combo>();
            var mask = TagMask.Empty;
            int score = 0;
            foreach (var c in pool)
            {
                var m = TagMask.Empty;
                foreach (var t in c.Tags) m.SetBit(t.Index);
                if (!m.Any(mask)) { mask.Or(m); res.Add(c); score += c.Score; if (res.Count == targetCount) break; }
            }
            return (res, score);
        }

        private static void SolveMiningRecursive(List<Tag> tags, CandidateCombo current, Tag[] path, int target, int lastIdx, PriorityQueue<Combo, int> top, int n, ref int floor, int maxSub, int maxCat, float[] powers)
        {
            float mult = current.GetCurrentMultiplier();
            if (current.Size == target)
            {
                int score = (int)(current.BaseSubs * mult);
                if (top.Count < n) { top.Enqueue(new Combo(path, score), score); if (top.Count == n) floor = top.Peek().Score; }
                else if (score > floor) { top.Dequeue(); top.Enqueue(new Combo(path, score), score); floor = top.Peek().Score; }
                return;
            }

            int remaining = target - current.Size;
            long theoretical = (long)((current.BaseSubs + remaining * maxSub) * (mult * powers[remaining * maxCat]));
            if (top.Count == n && theoretical <= floor) return;

            for (int i = lastIdx + 1; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (top.Count == n && tag.MaxPotentialScore <= floor) break;
                if (current.CanAdd(tag) && !tag.IncompatibilityMask.Any(current.CumulativeIncompatibilityMask))
                {
                    var next = current; next.AddTag(tag);
                    path[current.Size] = tag;
                    SolveMiningRecursive(tags, next, path, target, i, top, n, ref floor, maxSub, maxCat, powers);
                }
            }
        }

        /// <summary>
        /// Recursive branch-and-bound algorithm to find the maximum score.
        /// </summary>
        private static void SolveMaxPotentialRecursive(
            CandidateCombo current,
            List<Tag> candidates,
            int max,
            int last,
            ref long best,
            float[] powers,
            int maxSub,
            int maxCat)
        {
            float mult = current.GetCurrentMultiplier();
            long score = (long)(current.BaseSubs * mult);

            // Update local best if current branch exceeds it
            if (score > best) best = score;

            // Base case: Max size reached
            if (current.Size == max) return;

            // Pruning (Branch and Bound):
            // Estimate the theoretical maximum remaining score for this branch.
            // If even the perfect scenario cannot beat the current best, prune this branch.
            int remaining = max - current.Size;
            float projectedMaxMult = mult * powers[remaining * maxCat];
            long projectedMaxScore = (long)((current.BaseSubs + remaining * maxSub) * projectedMaxMult);

            if (projectedMaxScore <= best) return;

            // Iterate through valid candidates
            for (int i = last + 1; i < candidates.Count; i++)
            {
                var t = candidates[i];

                // Compatibility Check:
                // 1. Logic rules (CanAdd)
                // 2. Mutual exclusion check using the Cumulative Incompatibility Mask of the combo
                if (current.CanAdd(t) && !t.IncompatibilityMask.Any(current.CumulativeIncompatibilityMask))
                {
                    var next = current;
                    next.AddTag(t);
                    SolveMaxPotentialRecursive(next, candidates, max, i, ref best, powers, maxSub, maxCat);
                }
            }
        }

        #endregion
    }
}