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
                TagMetadata.Add(raw.Id, raw.Name, raw.Description, raw.IsControversial, raw.IsStoryMission);

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

        public static List<RawTag> GetRawTagsFromJson()
        {
            var json = File.ReadAllText(TagsJsonFile);
            return JsonSerializer.Deserialize<List<RawTag>>(json)
                   ?? throw new InvalidOperationException("Impossible de désérialiser le JSON en RawTag");
        }
        public static void WriteRawTagsToJson(List<RawTag> rawTags)
                => File.WriteAllText(
                    "tags+.json",
                    JsonSerializer.Serialize(rawTags, new JsonSerializerOptions { WriteIndented = true })
                );

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

            

            
        
    }
}
