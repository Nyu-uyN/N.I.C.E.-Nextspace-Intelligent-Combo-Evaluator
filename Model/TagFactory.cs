using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Responsible for loading, merging, and constructing Tag objects from JSON sources.
    /// Supports a layered data approach (Core data + User overrides).
    /// </summary>
    public static class TagFactory
    {
        private const string CoreJsonFile = "tags.json";
        private const string UserJsonFile = "user_overrides.json";

        /// <summary>
        /// Orchestrates the full loading process: merging JSON files, initializing metadata,
        /// and generating optimized Tag objects for the solver.
        /// </summary>
        /// <returns>A list of merged and initialized Tag objects.</returns>
        public static List<Tag> BuildTags()
        {
            var finalRawTags = GetMergedRawTags();

            TagMetadata.Reset();

            var tags = new List<Tag>(finalRawTags.Count);

            foreach (var raw in finalRawTags)
            {
                var incompatibilityMask = BuildIncompatibilityMask(raw.IncompatibleIds);
                var categoryMask = BuildCategoryMask(raw.Categories);
                int baseSubs = ComputeBaseSubs(raw.Rarity);

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

        /// <summary>
        /// Merges the read-only core data with the writable user overrides.
        /// Overrides replace core data based on Tag ID.
        /// </summary>
        private static List<RawTag> GetMergedRawTags()
        {
            if (!File.Exists(CoreJsonFile))
                throw new FileNotFoundException($"Core data file {CoreJsonFile} is missing.");

            var coreJson = File.ReadAllText(CoreJsonFile);
            var coreTags = JsonSerializer.Deserialize<List<RawTag>>(coreJson) ?? new();

            if (!File.Exists(UserJsonFile))
                return coreTags;

            var userJson = File.ReadAllText(UserJsonFile);
            var userOverrides = JsonSerializer.Deserialize<List<RawTag>>(userJson) ?? new();

            var mergeMap = coreTags.ToDictionary(t => t.Id);

            foreach (var over in userOverrides)
            {
                mergeMap[over.Id] = over;
            }

            return mergeMap.Values.OrderBy(t => t.Id).ToList();
        }

        /// <summary>
        /// Persists user-defined modifications to the override JSON file.
        /// </summary>
        public static void WriteUserOverrides(List<RawTag> userOverrides)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(userOverrides, options);
            File.WriteAllText(UserJsonFile, json);
        }

        private static TagMask BuildIncompatibilityMask(int[] incompatibleIds)
        {
            var mask = TagMask.Empty;
            if (incompatibleIds == null) return mask;

            foreach (var id in incompatibleIds)
                mask.SetBit(id);

            return mask;
        }

        private static CategoryMask BuildCategoryMask(string[] categories)
        {
            var mask = CategoryMask.Empty;
            if (categories == null) return mask;

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
    }
}
            

            
        
   

