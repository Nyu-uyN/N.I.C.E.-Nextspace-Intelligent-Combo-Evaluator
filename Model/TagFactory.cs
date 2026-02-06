using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    public static class TagFactory
    {
        // Chemin vers le JSON fusionné
        private const string TagsJsonFile = "tags.json";

        /// <summary>
        /// Construit tous les Tags optimisés pour NICE.
        /// Appelle automatiquement la désérialisation interne.
        /// </summary>
        public static List<Tag> BuildTags()
        {
            var rawTags = GetRawTagsFromJson();
            

            var tags = new List<Tag>(rawTags.Count);

            foreach (var raw in rawTags)
            {
                var incompatibilityMask = BuildIncompatibilityMask(raw.IncompatibleIds);
                var categoryMask = BuildCategoryMask(raw.Categories);
                int baseSubs = ComputeBaseSubs(raw.Rarity);

                // Stockage metadata
                TagMetadata.Add(raw.Id, raw.Name, raw.Description, raw.isControversial, raw.isStoryMission);

                tags.Add(new Tag(
                    index: raw.Id,
                    baseSubs: baseSubs,
                    incompatibilityMask: incompatibilityMask,
                    categoryMask: categoryMask,
                    maxPotentialScore: raw.MaxPotentialScore
                ));
            }

            return tags;
        }

        // ------------------------------
        // helpers privés
        // ------------------------------

        private static List<RawTag> GetRawTagsFromJson()
        {
            var json = File.ReadAllText(TagsJsonFile);
            return JsonSerializer.Deserialize<List<RawTag>>(json)
                   ?? throw new InvalidOperationException("Impossible de désérialiser le JSON en RawTag");
        }

        private static TagMask BuildIncompatibilityMask(int[] incompatibleIds)
        {
            var mask = TagMask.Empty;
            foreach (var id in incompatibleIds)
            { 
               mask.SetBit(id);
            }
            return mask;
        }

        private static CategoryMask BuildCategoryMask(string[] categories)
        {
            var mask = CategoryMask.Empty;
            foreach (var cat in categories)
            {
                if (Enum.TryParse<Category>(cat, ignoreCase: true, out var category))
                    mask.SetBit((int)category);
            }
            return mask;
        }

        private static int ComputeBaseSubs(int rarity) => rarity switch
        {
            0 => 5,
            1 => 15,
            2 => 45,
            3 => 135,
            4 => 405,
            _ => 0
        };

        // ------------------------------
        // type interne pour désérialisation
        // ------------------------------

        private sealed class RawTag
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
            public int Rarity { get; init; }
            public string[] Categories { get; init; } = Array.Empty<string>();
            public bool isControversial { get; init; }
            public bool isStoryMission { get; init; }
            public int[] IncompatibleIds { get; init; } = Array.Empty<int>();
            public int MaxPotentialScore { get; set; }
        }
        internal static class MaxPotentialScoreComputer
        {
            private const int MaxComboSize = 5;

            public static void ComputeAndPersist()
            {
                try
                {
                    MessageBox.Show("ENTER ComputeAndPersist");

                    var rawTags = GetRawTagsFromJson();
                    var tags = BuildTagsFromRaw(rawTags);

                    /*foreach (var tag in tags)
                    {
                        rawTags[tag.Index].MaxPotentialScore =
                            ComputeMaxPotentialForTag(tag, tags);
                        
                    }*/
                    Parallel.ForEach(tags, tag =>
                    {
                        rawTags[tag.Index].MaxPotentialScore = ComputeMaxPotentialForTag(tag, tags);
                    });


                    WriteRawTagsToJson(rawTags);

                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            private static int ComputeMaxPotentialForTag(Tag root, List<Tag> allTags)
            {
                var candidate = new CandidateCombo();
                candidate.AddTag(root);

                int best = candidate.ComputeScore();
                
                Explore(
                    candidate,
                    allTags,
                    0,
                    depth: 1,
                    ref best
                );

                return best;
            }

            private static void Explore(
                CandidateCombo combo,
                List<Tag> allTags,
                int startIndex,
                int depth,
                ref int bestScore
            )
            {
                if (depth == MaxComboSize)
                {
                    int score = combo.ComputeScore();
                    if (score > bestScore)
                        bestScore = score;
                    return;
                }

                for (int i = startIndex; i < allTags.Count; i++)
                {
                    var tag = allTags[i];
                    if (!combo.CanAdd(tag))
                        continue;

                    var next = combo;
                    next.AddTag(tag);
                    Explore(next, allTags, i + 1, depth + 1, ref bestScore); // <-- i + 1
                }
            }

            // -------- helpers JSON / construction --------

            private static List<Tag> BuildTagsFromRaw(List<RawTag> rawTags)
            {
                var tags = new List<Tag>(rawTags.Count);
                foreach (var raw in rawTags)
                {
                    tags.Add(new Tag(
                        index: raw.Id,
                      
                        
                        baseSubs: ComputeBaseSubs(raw.Rarity),
                        incompatibilityMask: BuildIncompatibilityMask(raw.IncompatibleIds),
                        categoryMask: BuildCategoryMask(raw.Categories),0
                        
                    ));
                }
                return tags;
            }

            private static List<RawTag> GetRawTagsFromJson()
                => JsonSerializer.Deserialize<List<RawTag>>(File.ReadAllText(TagsJsonFile))!;

            private static void WriteRawTagsToJson(List<RawTag> rawTags)
                => File.WriteAllText(
                    "tags+.json",
                    JsonSerializer.Serialize(rawTags, new JsonSerializerOptions { WriteIndented = true })
                );
        }
    }
}
