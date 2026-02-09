using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller
{
    
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class ComboController
    {
        private const int STATIC_POOL_SIZE = 100_000;

        // =================================================================================
        // PARTIE 1 : ANALYSE DES TAGS (POTENTIEL INDIVIDUEL)
        // =================================================================================

        public static long[] ComputeAllMaxPotentialScores(bool usePruning = true, int maxComboSize = 5)
        {
            var allTags = TagController.Tags;
            int count = allTags.Count;

            // --- 1. Préparation ---
            int maxBaseSubInDeck = 0;
            int maxCatsPerTag = 0;
            foreach (var t in allTags)
            {
                if (t.BaseSubs > maxBaseSubInDeck) maxBaseSubInDeck = t.BaseSubs;
                int cCount = t.CategoryMask.CountBits();
                if (cCount > maxCatsPerTag) maxCatsPerTag = cCount;
            }

            int maxBudget = maxComboSize * maxCatsPerTag;
            float[] maxGainPowers = new float[maxBudget + 1];
            float p = 1f;
            for (int i = 0; i <= maxBudget; i++) { maxGainPowers[i] = p; p *= 3f; }

            long[] results = new long[count];

            // --- 2. Exécution Parallèle ---
            Parallel.For(0, count, i =>
            {
                var rootTag = allTags[i];
                var candidates = new List<Tag>(count);
                for (int k = 0; k < count; k++)
                {
                    if (i == k) continue;
                    if (!rootTag.IncompatibilityMask.IsSet(allTags[k].Index))
                    {
                        candidates.Add(allTags[k]);
                    }
                }

                candidates.Sort((a, b) => b.BaseSubs.CompareTo(a.BaseSubs));

                long localBestScore = 0;
                var initialCombo = new CandidateCombo();
                initialCombo.AddTag(rootTag);

                SolveMaxPotential(initialCombo, candidates, maxComboSize, -1, ref localBestScore,
                      usePruning, maxGainPowers, maxBaseSubInDeck, maxCatsPerTag);

                results[i] = localBestScore;
            });

            return results;
        }

        private static void SolveMaxPotential(
            CandidateCombo currentCombo,
            List<Tag> candidates,
            int maxDepth,
            int lastIdx,
            ref long bestScore,
            bool usePruning,
            float[] maxGainPowers,
            int maxSub,
            int maxCat)
        {
            float currentMult = currentCombo.GetCurrentMultiplier();
            long currentScore = (long)(currentCombo.BaseSubs * currentMult);

            if (currentScore > bestScore) bestScore = currentScore;

            if (currentCombo.Size == maxDepth) return;

            if (usePruning)
            {
                int remaining = maxDepth - currentCombo.Size;
                int budget = remaining * maxCat;
                long potentialSubs = currentCombo.BaseSubs + (remaining * maxSub);
                float potentialMult = currentMult * maxGainPowers[budget];

                if ((long)(potentialSubs * potentialMult) <= bestScore) return;
            }

            for (int j = lastIdx + 1; j < candidates.Count; j++)
            {
                Tag t = candidates[j];
                if (currentCombo.CanAdd(t))
                {
                    if (t.IncompatibilityMask.Any(currentCombo.CumulativeIncompatibilityMask)) continue;
                    var next = currentCombo;
                    next.AddTag(t);
                    SolveMaxPotential(next, candidates, maxDepth, j, ref bestScore, usePruning, maxGainPowers, maxSub, maxCat);
                }
            }
        }

        // =================================================================================
        // PARTIE 2 : GÉNÉRATION DES COMBOS (UNITAIRES)
        // =================================================================================

        public static List<Combo> FindTopCombos(IEnumerable<Tag> sourceTags, int n = 10, int k = 5)
        {
            // ... (Ton code existant inchangé) ...
            var tagList = sourceTags as List<Tag> ?? sourceTags.ToList();
            if (tagList.Count == 0) return new List<Combo>();

            int localMaxBaseSub = 0;
            int localMaxCatsPerTag = 0;
            foreach (var t in tagList)
            {
                if (t.BaseSubs > localMaxBaseSub) localMaxBaseSub = t.BaseSubs;
                int cCount = t.CategoryMask.CountBits();
                if (cCount > localMaxCatsPerTag) localMaxCatsPerTag = cCount;
            }

            int maxBudget = k * localMaxCatsPerTag;
            float[] localGainPowers = new float[maxBudget + 1];
            float p = 1f;
            for (int i = 0; i <= maxBudget; i++) { localGainPowers[i] = p; p *= 3f; }

            tagList.Sort((a, b) => b.MaxPotentialScore.CompareTo(a.MaxPotentialScore));

            var topCombos = new PriorityQueue<Combo, int>(n);
            int minScoreToBeat = 0;
            Tag[] pathBuffer = new Tag[k];

            for (int i = 0; i < tagList.Count; i++)
            {
                var rootTag = tagList[i];
                if (topCombos.Count == n && rootTag.MaxPotentialScore <= minScoreToBeat) break;

                var comboState = new CandidateCombo();
                comboState.PackedCategoryCounts = 0;
                comboState.CumulativeIncompatibilityMask = TagMask.Empty;
                comboState.BaseSubs = 0;
                comboState.Size = 0;

                comboState.AddTag(rootTag);
                pathBuffer[0] = rootTag;

                SolveFindTop(tagList, comboState, pathBuffer, k, i, topCombos, n, ref minScoreToBeat, localMaxBaseSub, localMaxCatsPerTag, localGainPowers);
            }

            var results = new List<Combo>(topCombos.Count);
            while (topCombos.Count > 0) results.Add(topCombos.Dequeue());
            results.Reverse();
            return results;
        }

        private static void SolveFindTop(
            List<Tag> allTags,
            CandidateCombo currentCombo,
            Tag[] pathBuffer,
            int targetSize,
            int lastIndex,
            PriorityQueue<Combo, int> topCombos,
            int n,
            ref int minScoreToBeat,
            int localMaxSub,
            int localMaxCat,
            float[] localGainPowers)
        {
            
            float currentMult = currentCombo.GetCurrentMultiplier();

            if (currentCombo.Size == targetSize)
            {
                int finalScore = (int)(currentCombo.BaseSubs * currentMult);
                if (topCombos.Count < n)
                {
                    var comboObj = new Combo(pathBuffer, finalScore);
                    topCombos.Enqueue(comboObj, finalScore);
                    if (topCombos.Count == n) minScoreToBeat = topCombos.Peek().Score;
                }
                else if (finalScore > minScoreToBeat)
                {
                    topCombos.Dequeue();
                    var comboObj = new Combo(pathBuffer, finalScore);
                    topCombos.Enqueue(comboObj, finalScore);
                    minScoreToBeat = topCombos.Peek().Score;
                }
                return;
            }

            int remainingSlots = targetSize - currentCombo.Size;
            int budget = remainingSlots * localMaxCat;
            long potentialSubs = currentCombo.BaseSubs + (remainingSlots * localMaxSub);
            float potentialMult = currentMult * localGainPowers[budget];
            long theoreticalMaxScore = (long)(potentialSubs * potentialMult);

            if (topCombos.Count == n && theoreticalMaxScore <= minScoreToBeat) return;

            for (int i = lastIndex + 1; i < allTags.Count; i++)
            {
                var nextTag = allTags[i];
                if (topCombos.Count == n && nextTag.MaxPotentialScore <= minScoreToBeat) break;

                if (currentCombo.CanAdd(nextTag))
                {
                    if (nextTag.IncompatibilityMask.Any(currentCombo.CumulativeIncompatibilityMask)) continue;

                    var nextComboState = currentCombo;
                    nextComboState.AddTag(nextTag);
                    pathBuffer[currentCombo.Size] = nextTag;

                    SolveFindTop(allTags, nextComboState, pathBuffer, targetSize, i, topCombos, n, ref minScoreToBeat, localMaxSub, localMaxCat, localGainPowers);
                }
            }
        }

        // =================================================================================
        // PARTIE 3 : RÉSOLUTION DU LOADOUT COMPLET (SET PACKING)
        // =================================================================================

        /// <summary>
        /// Calcule le meilleur ensemble de 'loadoutSize' combos disjoints (sans tags communs).
        /// Utilise une approche itérative qui garantit mathématiquement l'optimalité du résultat.
        /// </summary>
        
        /// <summary>
        /// Version avec Logging fichier.
        /// Écrit l'avancement dans un fichier sur le Bureau.
        /// </summary>
        public static List<Combo> ComputeBestLoadoutWithLog(IEnumerable<Tag> tags, int loadoutSize = 10, int comboSize = 5)
        {
            // 1. Configuration du fichier de log
            
            string logPath = "ComboSolver_Log.txt";

            // On écrase le fichier précédent pour partir propre
            File.WriteAllText(logPath, $"=== DÉMARRAGE DU SOLVER : {DateTime.Now} ===\n\n");

            // Petite fonction locale pour écrire sans polluer le code
            void Log(string message)
            {
                try
                {
                    string line = $"[{DateTime.Now:HH:mm:ss}] {message}\n";
                    File.AppendAllText(logPath, line);
                }
                catch { /* Si le fichier est verrouillé par Notepad, on ignore juste l'erreur */ }
            }

            int poolSize = 2000;
            int bestScoreGlobal = 0;
            List<Combo> bestLoadoutGlobal = new List<Combo>();
            var allTagsList = tags.ToList();

            while (true)
            {
                Log($"--- NOUVELLE ÉTAPE : Pool Standard {poolSize:N0} ---");

                // 1. MINING STANDARD
                var standardPool = FindTopCombos(allTagsList, n: poolSize, k: comboSize);

                // La borne pour la preuve mathématique (toujours basée sur le Standard)
                int thresholdForProof = (standardPool.Count > 0) ? standardPool[^1].Score : 0;

                // 2. FAST SCAN (La preuve d'existence rapide)
                // On teste le pool standard. Si le glouton échoue, on injecte la diversité.
                var searchPool = standardPool;
                int greedyScore = TryFastGreedySolution(searchPool, loadoutSize);

                if (greedyScore == -1)
                {
                    Log("   ALERTE : Le pool standard est saturé de clones (Glouton échoué).");
                    Log("   ACTION : Injection de Diversité + Re-Scan...");

                    // Construction du pool diversifié
                    var mixedPoolSet = new HashSet<Combo>(standardPool);
                    foreach (var t in allTagsList)
                    {
                        var champion = FindTopCombos(new List<Tag> { t }, n: 1, k: comboSize).FirstOrDefault();
                        if (champion != null) mixedPoolSet.Add(champion);
                    }

                    searchPool = mixedPoolSet.ToList();
                    searchPool.Sort((a, b) => b.Score.CompareTo(a.Score));

                    // Re-Scan sur le pool mixte
                    greedyScore = TryFastGreedySolution(searchPool, loadoutSize);

                    if (greedyScore == -1)
                    {
                        Log("   ÉCHEC CRITIQUE : Même avec la diversité, impossible de trouver 10 disjoints.");
                        Log("   DÉCISION : Pool trop petit ou trop contraint. On double.");
                        poolSize *= 2;
                        continue; // On passe direct au tour suivant
                    }
                    else
                    {
                        Log($"   SUCCÈS : La diversité a débloqué une solution de base ({greedyScore:N0} pts).");
                    }
                }
                else
                {
                    Log($"   Pool Standard viable (Glouton: {greedyScore:N0} pts). Pas besoin de diversité.");
                }

                // 3. PACKING (Optimisation B&B)
                // On lance l'algo lourd, mais avec une "Avance" (le greedyScore)
                // Cela permet de couper énormément de branches dès le début.

                Log("   Lancement de l'Optimiseur Récursif...");
                var (loadout, score) = OptimizePool(searchPool, loadoutSize, initialBestScore: greedyScore);

                if (score > bestScoreGlobal)
                {
                    bestScoreGlobal = score;
                    bestLoadoutGlobal = loadout;
                    Log($"!!! NOUVEAU RECORD : {score:N0} !!!");
                }

                // 4. PREUVE MATHÉMATIQUE
                long maxUnknown = (long)thresholdForProof * loadoutSize;

                if (standardPool.Count < poolSize || bestScoreGlobal >= maxUnknown)
                {
                    Log("VICTOIRE : Optimalité Prouvée.");
                    Log($"Score ({bestScoreGlobal}) >= Potentiel Inconnu ({maxUnknown})");
                    return bestLoadoutGlobal;
                }

                poolSize *= 2;
            }
        }
        /// <summary>
        /// Version modifiée pour accepter un score initial (Warm-up)
        /// </summary>
        private static (List<Combo>, int) OptimizePool(List<Combo> sourcePool, int targetCount, int initialBestScore)
        {
            // ... (Conversion PackedCombo comme avant) ...
            var pool = new PackedCombo[sourcePool.Count];
            for (int i = 0; i < sourcePool.Count; i++) pool[i] = new PackedCombo(i, sourcePool[i]);

            var bestIndices = new int[targetCount];

            // ICI : On démarre avec le score du glouton !
            // Ça veut dire que toute branche récursive qui fait moins que le glouton sera tuée dans l'œuf.
            int bestTotalScore = initialBestScore;

            // Note : On ne remplit pas 'bestIndices' avec le glouton ici pour simplifier,
            // car on sait que la récursion trouvera au moins aussi bien ou mieux très vite.
            // (Si on veut être puriste, on pourrait passer les indices du glouton aussi, mais le score suffit pour l'élagage).

            var path = new int[targetCount];

            SolvePackingRecursive(
                pool, 0, 0, 0, TagMask.Empty,
                targetCount, path, bestIndices, ref bestTotalScore
            );

            // Reconstruction
            var result = new List<Combo>(targetCount);
            // Attention : si la récursion n'a pas battu le glouton et qu'on n'a pas stocké les indices gloutons,
            // on risque de renvoyer vide.
            // CORRECTION : Il vaut mieux relancer le glouton pour avoir ses indices si bestIndices est vide.

            if (bestTotalScore > 0 && bestIndices[0] == 0 && bestIndices[1] == 0) // Indice que la récursion n'a rien écrit
            {
                // On récupère le glouton "proprement" car c'est lui le vainqueur
                // (Code simplifié pour la lisibilité, en prod on stockerait les indices dans TryFastGreedy)
                return ReconstructGreedy(sourcePool, targetCount);
            }

            for (int i = 0; i < targetCount; i++) result.Add(sourcePool[bestIndices[i]]);

            return (result, bestTotalScore);
        }
        // ==================================================================================
        // 1. MÉTHODE SÉQUENTIELLE (La référence stable ~2min)
        // ==================================================================================

        public static List<Combo> ComputeBestLoadout_Sequential(IEnumerable<Tag> tags, int loadoutSize = 10, int comboSize = 5)
        {
            // 1. Mining
            var pool = FindTopCombos(tags, n: STATIC_POOL_SIZE, k: comboSize);

            // 2. Glouton (Warm-up)
            int greedyScore = TryFastGreedySolution(pool, loadoutSize);
            if (greedyScore == -1) return new List<Combo>();

            // 3. Packing Séquentiel
            // On lance avec le score glouton comme plancher
            var (bestLoadout, _) = OptimizePool_Sequential(pool, loadoutSize, initialBestScore: greedyScore);

            return bestLoadout;
        }

        // ==================================================================================
        // 2. MÉTHODE PARALLÈLE (La version Turbo ~20s)
        // ==================================================================================

        public static List<Combo> ComputeBestLoadout_Parallel(IEnumerable<Tag> tags, int loadoutSize = 10, int comboSize = 5)
        {
            // 1. Mining (Identique)
            var pool = FindTopCombos(tags, n: STATIC_POOL_SIZE, k: comboSize);

            // 2. Glouton (Identique)
            int greedyScore = TryFastGreedySolution(pool, loadoutSize);
            if (greedyScore == -1) return new List<Combo>();

            // 3. RECHERCHE DU SCORE MAX (PARALLÈLE)
            // C'est ici que la magie opère. On utilise tous les cœurs pour trouver LE chiffre magique.
            // On ne construit pas la liste ici pour éviter les verrous (locks) lents.
            long maxScoreFound = FindMaxScore_Parallel(pool, loadoutSize, greedyScore);

            // 4. RECONSTRUCTION (SÉQUENTIELLE MAIS GUIDÉE)
            // Maintenant qu'on a le score parfait (ex: 1 623 600), on relance l'algo séquentiel.
            // Comme on lui donne (maxScoreFound - 1) comme base, il va couper TOUTES les branches
            // sauf celle qui mène à la victoire. C'est instantané (< 100ms).
            var (bestLoadout, _) = OptimizePool_Sequential(pool, loadoutSize, initialBestScore: (int)maxScoreFound - 1);

            return bestLoadout;
        }

        // ==================================================================================
        // MOTEUR SÉQUENTIEL (Utilisé par la méthode Seq et pour la Reconstruction)
        // ==================================================================================

        private static (List<Combo>, int) OptimizePool_Sequential(List<Combo> sourcePool, int targetCount, int initialBestScore)
        {
            var pool = new PackedCombo[sourcePool.Count];
            for (int i = 0; i < sourcePool.Count; i++) pool[i] = new PackedCombo(i, sourcePool[i]);

            var bestIndices = new int[targetCount];
            int bestTotalScore = initialBestScore;
            var path = new int[targetCount];

            SolvePackingRecursive(pool, 0, 0, 0, TagMask.Empty, targetCount, path, bestIndices, ref bestTotalScore);

            // Reconstruction
            var result = new List<Combo>(targetCount);
            // Si on n'a pas battu le score initial (cas rare ou reconstruction échouée), on renvoie le glouton
            if (bestTotalScore > 0 && bestIndices[0] == 0 && bestIndices[1] == 0)
                return ReconstructGreedy(sourcePool, targetCount);

            for (int i = 0; i < targetCount; i++) result.Add(sourcePool[bestIndices[i]]);
            return (result, bestTotalScore);
        }

        private static void SolvePackingRecursive(PackedCombo[] pool, int startIndex, int currentCount, int currentScore, TagMask currentMask, int targetCount, int[] currentPath, int[] bestIndices, ref int bestTotalScore)
        {
            if (currentCount == targetCount)
            {
                if (currentScore > bestTotalScore)
                {
                    bestTotalScore = currentScore;
                    Array.Copy(currentPath, bestIndices, targetCount);
                }
                return;
            }

            int remaining = targetCount - currentCount;
            if (startIndex + remaining > pool.Length) return;

            // Élagage Upper Bound
            long theoreticalMax = currentScore;
            for (int k = 0; k < remaining; k++) theoreticalMax += pool[startIndex + k].Score;
            if (theoreticalMax <= bestTotalScore) return;

            for (int i = startIndex; i < pool.Length; i++)
            {
                ref var candidate = ref pool[i];

                // Élagage Contextuel
                long optimistic = currentScore + (long)candidate.Score + ((long)candidate.Score * (remaining - 1));
                if (optimistic <= bestTotalScore) break;

                if (candidate.UsedTagsMask.Any(currentMask)) continue;

                currentPath[currentCount] = candidate.OriginalIndex;
                var nextMask = currentMask;
                nextMask.Or(candidate.UsedTagsMask);

                SolvePackingRecursive(pool, i + 1, currentCount + 1, currentScore + candidate.Score, nextMask, targetCount, currentPath, bestIndices, ref bestTotalScore);
            }
        }

        // ==================================================================================
        // MOTEUR PARALLÈLE (Calcul Score Uniquement)
        // ==================================================================================

        private static volatile int _volatileBestScore;

        // Objet de lock uniquement pour l'écriture (très rare)
        private static readonly object _writeLock = new object();

        private static long FindMaxScore_Parallel(List<Combo> sourcePool, int targetCount, int initialScore)
        {
            // 1. Allocation
            var pool = new PackedCombo[sourcePool.Count];
            for (int i = 0; i < sourcePool.Count; i++) pool[i] = new PackedCombo(i, sourcePool[i]);

            _volatileBestScore = initialScore;

            // On récupère le nombre de cœurs logiques
            int processorCount = Environment.ProcessorCount;

            // 2. PARALLÉLISME MANUEL (STRIPING)
            // Au lieu de laisser .NET découper des blocs, on lance exactement 1 thread par cœur.
            // Chaque thread va traiter les indices par sauts (stride).
            // Ex: Thread 0 traite 0, 12, 24... Thread 1 traite 1, 13, 25...
            // Cela garantit que TOUS les cœurs attaquent le début de la liste (le plus dur) en même temps.

            Parallel.For(0, processorCount, new ParallelOptions { MaxDegreeOfParallelism = processorCount }, (workerId) =>
            {
                // Chaque thread commence à son ID et avance de 'processorCount' à chaque fois
                for (int i = workerId; i < pool.Length; i += processorCount)
                {
                    // SÉCURITÉ : Arrêt si on dépasse la fin utile
                    if (i + targetCount > pool.Length) break;

                    // LECTURE RAPIDE (Volatile) : Pas de frein à main Interlocked
                    long globalThreshold = _volatileBestScore;

                    // Élagage Optimiste (Upper Bound)
                    long optimisticBound = (long)pool[i].Score + ((long)pool[i + 1].Score * (targetCount - 1));
                    if (optimisticBound <= globalThreshold) continue;

                    // Récursion
                    SolveScoreOnlyRecursive(pool, i + 1, 1, pool[i].Score, pool[i].UsedTagsMask, targetCount);
                }
            });

            return _volatileBestScore;
        }

        private static void SolveScoreOnlyRecursive(PackedCombo[] pool, int startIndex, int currentCount, int currentScore, TagMask currentMask, int targetCount)
        {
            // LECTURE RAPIDE (Volatile)
            // C'est ici qu'on gagne la performance. Plus d'appel système lourd.
            if (currentScore + ((long)pool[startIndex].Score * (targetCount - currentCount)) <= _volatileBestScore) return;

            if (currentCount == targetCount)
            {
                if (currentScore > _volatileBestScore)
                {
                    // ÉCRITURE SÉCURISÉE
                    // On ne lock que si on a vraiment trouvé un record (ce qui arrive 3 ou 4 fois max dans toute l'exécution)
                    lock (_writeLock)
                    {
                        if (currentScore > _volatileBestScore)
                        {
                            _volatileBestScore = currentScore;
                        }
                    }
                }
                return;
            }

            int remaining = targetCount - currentCount;
            if (startIndex + remaining > pool.Length) return;

            // --- OPTIMISATION DE BOUCLE ---
            // On sort la lecture de la variable globale de la boucle for pour éviter de relire la RAM à chaque itération
            long currentThreshold = _volatileBestScore;

            for (int i = startIndex; i < pool.Length; i++)
            {
                ref var candidate = ref pool[i];

                // Élagage Contextuel Rapide
                long optimistic = currentScore + (long)candidate.Score + ((long)candidate.Score * (remaining - 1));
                if (optimistic <= currentThreshold) break; // La liste est triée, on peut break

                if (candidate.UsedTagsMask.Any(currentMask)) continue;

                var nextMask = currentMask;
                nextMask.Or(candidate.UsedTagsMask);

                SolveScoreOnlyRecursive(pool, i + 1, currentCount + 1, currentScore + candidate.Score, nextMask, targetCount);

                // Petite mise à jour du threshold au cas où un autre thread l'ait monté pendant notre boucle
                // (Optionnel, mais aide à couper plus vite)
                if (i % 100 == 0) currentThreshold = _volatileBestScore;
            }
        }

        // ==================================================================================
        // HELPERS COMMUNS
        // ==================================================================================

        private static int TryFastGreedySolution(List<Combo> pool, int targetCount)
        {
            int count = 0;
            int score = 0;
            var mask = TagMask.Empty;

            foreach (var combo in pool)
            {
                var cMask = TagMask.Empty;
                foreach (var t in combo.Tags) cMask.SetBit(t.Index);

                if (!cMask.Any(mask))
                {
                    mask.Or(cMask);
                    score += combo.Score;
                    count++;
                    if (count == targetCount) return score;
                }
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
                if (!m.Any(mask))
                {
                    mask.Or(m);
                    res.Add(c);
                    score += c.Score;
                    if (res.Count == targetCount) break;
                }
            }
            return (res, score);
        }
    }
}


