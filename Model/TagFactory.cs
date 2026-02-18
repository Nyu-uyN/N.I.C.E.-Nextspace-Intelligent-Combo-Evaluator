using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Factory responsible for loading raw data and producing computation-ready Tag structs.
    /// Decouples data DTOs (RawTag) from optimized calculation structures (Tag) and UI Metadata.
    /// </summary>
    public static class TagFactory
    {
        private static string CoreJsonFile => AppPaths.TagsJson;
        private static string UserJsonFile => AppPaths.UserOverrideJson;

        /// <summary>
        /// Orchestrates the initial application startup by loading merged data and populating global metadata.
        /// This should be called only once at startup or after a successful Commit.
        /// </summary>
        /// <returns>The active pool of Tags with metadata registered.</returns>
        public static List<Tag> InitializeActivePool()
        {
            var rawTags = GetMergedRawData();

            // Global metadata is only populated here
            TagMetadata.Reset();
            var coreTags = TagFactory.LoadCoreRaw();
            foreach (var core in coreTags)
            {
                // On injecte dans la couche "Base" du Metadata
                TagMetadata.AddBaseData(
                    core.Id,
                    core.Name,
                    core.Description,
                    core.IsControversial,
                    core.IsStoryMission
                );
            }
            var overrides = TagFactory.LoadUserRaw();

            foreach (var ov in overrides)
            {

                if (!string.IsNullOrEmpty(ov.Name))
                    TagMetadata.SetNameOverride(ov.Id, ov.Name);

                if (!string.IsNullOrEmpty(ov.Description))
                    TagMetadata.SetDescriptionOverride(ov.Id, ov.Description);



                TagMetadata.SetControversial(ov.Id, ov.IsControversial);
                TagMetadata.SetStoryMission(ov.Id, ov.IsStoryMission);
            }
                return MapRawToTags(rawTags);
        }

        /// <summary>
        /// Builds Tag structs from the core definition without polluting global metadata.
        /// Used for comparison logic in management modules.
        /// </summary>
        public static List<Tag> BuildCoreTagsOnly()
        {
            return MapRawToTags(LoadCoreRaw());
        }

        /// <summary>
        /// Maps RawTag DTOs to optimized Tag structs. This is a pure transformation.
        /// </summary>
        private static List<Tag> MapRawToTags(List<RawTag> rawTags)
        {
            var tags = new List<Tag>(rawTags.Count);
            foreach (var raw in rawTags)
            {
                tags.Add(new Tag(
                    index: raw.Id,
                    baseSubs: ComputeBaseSubs(raw.Rarity),
                    incompatibilityMask: BuildIncompatibilityMask(raw.IncompatibleIds),
                    categoryMask: BuildCategoryMask(raw.Categories),
                    maxPotentialScore: raw.MaxPotentialScore
                ));
            }
            return tags;
        }
        /// <summary>
        /// Logic for merging Core and User data.
        /// </summary>
        private static List<RawTag> GetMergedRawData()
        {
            var core = LoadCoreRaw();
            var user = LoadUserRaw();
            if (user.Count == 0) return core;

            var mergeMap = core.ToDictionary(t => t.Id);
            foreach (var over in user) mergeMap[over.Id] = over;

            return mergeMap.Values.OrderBy(t => t.Id).ToList();
        }
        private static List<RawTag> LoadCoreRaw()
        {
            AppPaths.InitializeStructure();

            if (!File.Exists(CoreJsonFile))
                throw new FileNotFoundException($"Core tags missing at: {CoreJsonFile}");

            return JsonSerializer.Deserialize<List<RawTag>>(File.ReadAllText(CoreJsonFile)) ?? new();
        }

        private static List<RawTag> LoadUserRaw()
        {
            if (!File.Exists(UserJsonFile)) return new List<RawTag>();
            return JsonSerializer.Deserialize<List<RawTag>>(File.ReadAllText(UserJsonFile)) ?? new();
        }
        /// <summary>
        /// Persists user-defined modifications to the override JSON file.
        /// </summary>
        private static void WriteUserOverrides(List<RawTag> userOverrides)
        {
            AppPaths.InitializeStructure();

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(userOverrides, options);
            File.WriteAllText(UserJsonFile, json);
        }
        /// <summary>
        /// Generic persistence method.
        /// Compares the provided final state of RawTags against the factory defaults.
        /// Identifies ALL differences (Names, Scores, Rules, etc.) and saves the overrides.
        /// </summary>
        /// <param name="finalState">The complete list of RawTags as they should appear in the application.</param>
        public static void Commit(List<Tag> finalState)
        {
            List<RawTag> raws = new();
            var corestate = LoadCoreRaw();
            foreach (var tag in finalState)
            {
                var raw = ReconstructRawTag(tag);
                if (IsDifferent(raw, corestate[raw.Id]))
                {
                    raws.Add(raw); 
                }
            }
            WriteUserOverrides(raws);
        }

        /// <summary>
        /// Internal helper to decode TagMask into a list of IDs.
        /// Keeps the implementation detail of the bitmask hidden within the Factory.
        /// </summary>
        private static IEnumerable<int> DecodeMaskToIds(TagMask mask)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            IEnumerable<int> YieldActiveBits(ulong bits, int baseIndex)
            {
                while (bits != 0)
                {
                    int bitPos = BitOperations.TrailingZeroCount(bits);
                    yield return baseIndex + bitPos;
                    bits &= bits - 1;
                }
            }

            foreach (var id in YieldActiveBits(mask.A, 0)) yield return id;
            foreach (var id in YieldActiveBits(mask.B, 64)) yield return id;
            foreach (var id in YieldActiveBits(mask.C, 128)) yield return id;
            foreach (var id in YieldActiveBits(mask.D, 192)) yield return id;
            foreach (var id in YieldActiveBits(mask.E, 256)) yield return id;
            foreach (var id in YieldActiveBits(mask.F, 320)) yield return id;
            foreach (var id in YieldActiveBits(mask.G, 384)) yield return id;
            foreach (var id in YieldActiveBits(mask.H, 448)) yield return id;
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
        private static RawTag ReconstructRawTag(Tag tag)
        {
            
            string effectiveName = TagMetadata.GetName(tag.Index);
            string effectiveDesc = TagMetadata.GetDescription(tag.Index);
            bool isControversial = TagMetadata.IsControversial(tag.Index);
            bool isStory = TagMetadata.IsStoryMission(tag.Index);

            return new RawTag
            {
                Id = tag.Index,
                Name = effectiveName,
                Description = effectiveDesc,

               
                IsControversial = isControversial,
                IsStoryMission = isStory,

                MaxPotentialScore = tag.MaxPotentialScore,
                
                Rarity = ConvertBaseSubsToRarity(tag.BaseSubs),
                IncompatibleIds = DecodeMaskToIds(tag.IncompatibilityMask).ToArray(),
                Categories = DecodeCategories(tag.CategoryMask).ToArray()
            };
        }
        private static int ConvertBaseSubsToRarity(int baseSubs)
        {
            
            return baseSubs switch
            {
                5 => 0,
                15 => 1,
                45 => 2,
                135 => 3,
                405 => 4,
                _ => 0
            };
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
        private static IEnumerable<string> DecodeCategories(CategoryMask mask)
        {
            
            uint bits = mask.Mask;

            while (bits != 0)
            {
                
                int bitIndex = BitOperations.TrailingZeroCount(bits);

                
                yield return ((Category)bitIndex).ToString().ToUpperInvariant(); 

              
                bits &= (bits - 1);
            }
        }
        private static bool IsDifferent(RawTag a, RawTag b)
        {
            
            if (a.MaxPotentialScore != b.MaxPotentialScore) return true;
            if (a.Name != b.Name) return true;
            if (a.Description != b.Description) return true;
            if (a.Rarity != b.Rarity) return true;

            
            if (a.IsControversial != b.IsControversial) return true;
            if (a.IsStoryMission != b.IsStoryMission) return true;

            
            if (!UnorderedEqual(a.IncompatibleIds, b.IncompatibleIds)) return true;
            if (!UnorderedEqual(a.Categories, b.Categories)) return true;

            return false;
        }
        /// <summary>
        /// Compares two collections to see if they contain the exact same elements, regardless of order.
        /// </summary>
        private static bool UnorderedEqual<T>(IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (first == null && second == null) return true;
            if (first == null || second == null) return false;

            
            var set1 = new HashSet<T>(first);
            var set2 = new HashSet<T>(second);

            return set1.SetEquals(set2);
        }
    }
}
            

            
        
   

