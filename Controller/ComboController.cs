using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller
{
    /// <summary>
    /// Handles generation of tag combos, calculation of their scores,
    /// and tracking of the top N combos in memory using a priority queue.
    /// </summary>
    public class ComboController
    {
        private readonly List<Tag> _tags;

        public int ComboSize { get; set; } = 5;
        public int MaxResults { get; set; } = int.MaxValue;
        public bool AbortRequested { get; set; } = false;

        public ComboController(IEnumerable<Tag> tags, int comboSize = 5, int maxResults = int.MaxValue)
        {
            if (tags is null)
                throw new ArgumentNullException(nameof(tags));

            if (comboSize < 2 || comboSize > 5)
                throw new ArgumentOutOfRangeException(nameof(comboSize), "ComboSize must be between 2 and 5");

            _tags = tags.ToList();
            ComboSize = comboSize;
            MaxResults = maxResults;
        }

        public List<Combo> GenerateTopCombos()
        {
            // Min-heap based on score, smallest score has highest priority
            var topCombos = new PriorityQueue<Combo, int>();

            GenerateCombosRecursive(_tags, ComboSize, 0, new List<Tag>(), topCombos);

            // Extract items and sort descending by score for display
            return topCombos.UnorderedItems
                            .Select(x => x.Element)
                            .OrderByDescending(c => c.Score)
                            .ToList();
        }

        private void GenerateCombosRecursive(
            List<Tag> availableTags,
            int comboSize,
            int startIndex,
            List<Tag> currentCombo,
            PriorityQueue<Combo, int> topCombos)
        {
            if (AbortRequested)
                return;

            if (currentCombo.Count == comboSize)
            {
                int score = CalculateScore(currentCombo);
                var combo = new Combo(currentCombo, score);

                // Insert in priority queue
                if (topCombos.Count < MaxResults)
                {
                    topCombos.Enqueue(combo, score);
                }
                else if (score > topCombos.Peek().Score)
                {
                    topCombos.Dequeue();
                    topCombos.Enqueue(combo, score);
                }

                return;
            }

            for (int i = startIndex; i < availableTags.Count; i++)
            {
                var candidate = availableTags[i];

                if (currentCombo.Any(t => t.IncompatibleTagIds.Contains(candidate.Id)))
                    continue;

                currentCombo.Add(candidate);
                GenerateCombosRecursive(availableTags, comboSize, i + 1, currentCombo, topCombos);
                currentCombo.RemoveAt(currentCombo.Count - 1);

                if (AbortRequested)
                    return;
            }
        }

        private int CalculateScore(IEnumerable<Tag> comboTags)
        {
            if (comboTags == null)
                throw new ArgumentNullException(nameof(comboTags));

            // 1️⃣ Somme des subs selon la rareté
            int baseSubs = comboTags.Sum(tag => tag.Rarity switch
            {
                Rarity.Common => 5,
                Rarity.Uncommon => 15,
                Rarity.Rare => 45,
                Rarity.Epic => 135,
                Rarity.Viral => 405,
                _ => 0
            });

            // 2️⃣ Compter la présence de chaque catégorie
            var categoryCounts = new Dictionary<Category, int>();
            foreach (var tag in comboTags)
            {
                foreach (var cat in tag.Categories)
                {
                    if (categoryCounts.ContainsKey(cat))
                        categoryCounts[cat]++;
                    else
                        categoryCounts[cat] = 1;
                }
            }

            // 3️⃣ Calcul des multiplicateurs par catégorie
            float totalMultiplier = 1f;
            foreach (var count in categoryCounts.Values)
            {
                totalMultiplier *= count switch
                {
                    0 => 1f,
                    1 => 1f,
                    2 => 2f,
                    3 => 5f,
                    4 => 15f,
                    5 => 30f,
                    _ => 30f // ne peut pas dépasser 5 tags par combo
                };
            }

            // 4️⃣ Score final
            return (int)Math.Round(baseSubs * totalMultiplier);
        }
    }
}

