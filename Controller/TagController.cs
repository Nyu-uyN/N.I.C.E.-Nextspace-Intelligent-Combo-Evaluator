using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller
{
    /// <summary>
    /// Central controller managing the global state of all Tags.
    /// Provides high-performance access via indices, masks, and pre-sorted arrays.
    /// </summary>
    public static class TagController
    {
        public static event Action? OnDataRefreshed;

        private static List<Tag> _tags = null!;
        private static Tag[] _tagsByIndex = null!;
        private static Tag[] _tagsByMaxPotentialScoreDesc = null!;

        /// <summary>
        /// A bitmask representing all existing tags in the database.
        /// </summary>
        public static TagMask AllTagsMask { get; private set; }

        /// <summary>
        /// Static constructor to ensure the controller is initialized on first access.
        /// </summary>
        static TagController()
        {
            Initialize();
        }
        /// <summary>
        /// Initializes or refreshes the active tag pool and populates global metadata.
        /// Should be called at startup and after a T.I.M./T.A.M. Commit.
        /// </summary>
        public static List<Tag> InitializeAndGetActivePool()
        {
            return TagFactory.InitializeActivePool();
        }
        /// <summary>
        /// Loads tags from the factory and builds internal caches and masks.
        /// Can be called again to refresh data after user modifications (TIM/TAM).
        /// </summary>
        public static void Initialize()
        {
            _tags = InitializeAndGetActivePool();

            // The array size is based on the actual count, assuming continuous 0-based indexing
            _tagsByIndex = new Tag[_tags.Count];

            var allMask = TagMask.Empty;

            foreach (var tag in _tags)
            {
                _tagsByIndex[tag.Index] = tag;
                allMask.SetBit(tag.Index);
            }

            AllTagsMask = allMask;

            // Sort cached for the solver's pruning logic
            _tagsByMaxPotentialScoreDesc = _tags
                .OrderByDescending(t => t.MaxPotentialScore)
                .ToArray();
            OnDataRefreshed?.Invoke();
        }
        /// <summary>
        /// Returns the factory-default Tag structs without affecting the global TagMetadata.
        /// Intended for comparison and reset operations.
        /// </summary>
        public static List<Tag> GetDefaultTags()
        {
            return TagFactory.BuildCoreTagsOnly();
        }
        public static IReadOnlyList<Tag> Tags => _tags;

        public static Tag GetByIndex(int index) => _tagsByIndex[index];

        /// <summary>
        /// Sorts the provided tags by their precomputed MaxPotentialScore in descending order.
        /// Useful for pruning search branches early.
        /// </summary>
        public static Tag[] AllTagsByMaxPotentialScoreDesc => _tagsByMaxPotentialScoreDesc;

        public static TagMask GetMaskFromIndices(IEnumerable<int> indices)
        {
            var mask = TagMask.Empty;
            foreach (var idx in indices)
                mask.SetBit(idx);
            return mask;
        }

        public static TagMask GetMaskFromTags(IEnumerable<Tag> tags)
        {
            var mask = TagMask.Empty;
            foreach (var tag in tags)
                mask.SetBit(tag.Index);
            return mask;
        }

        /// <summary>
        /// Converts a TagMask back into a list of Tag objects.
        /// Uses bit manipulation (TrailingZeroCount) to skip empty bits for O(SetBits) complexity.
        /// </summary>
        public static List<Tag> GetTagsFromMask(TagMask mask)
        {
            var result = new List<Tag>();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void AddActiveBits(ulong bits, int baseIndex)
            {
                while (bits != 0)
                {
                    int bitPos = BitOperations.TrailingZeroCount(bits);
                    result.Add(_tagsByIndex[baseIndex + bitPos]);
                    bits &= bits - 1; // Clear lowest set bit
                }
            }

            AddActiveBits(mask.A, 0);
            AddActiveBits(mask.B, 64);
            AddActiveBits(mask.C, 128);
            AddActiveBits(mask.D, 192);
            AddActiveBits(mask.E, 256);
            AddActiveBits(mask.F, 320);
            AddActiveBits(mask.G, 384);

            return result;
        }
        /// <summary>
        /// Facade method to save the results of a calculation session.
        /// Delegates the conversion and persistence logic to the Factory.
        /// </summary>
        /// <param name="computedTags">The list of updated Tag structs.</param>
        public static void SaveCalculationResults(List<Tag> computedTags)
        {
            TagFactory.Commit(computedTags);
        }
        /// <summary>
        /// Computes a structural fingerprint of the current engine state by hashing all 512 tags.
        /// This includes categories, rarity, and precomputed incompatibility masks.
        /// This method is called on-demand to avoid overhead during data initialization.
        /// </summary>
        /// <returns>A unique MD5 hash representing the current data version.</returns>
        public static string GetGlobalEngineStateHash()
        {
            // Ensure the data source is available before hashing
            if (_tagsByIndex == null || _tagsByIndex.Length == 0) return string.Empty;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Precise iteration over the fixed 512-tag array to ensure deterministic output
            foreach (var tag in _tagsByIndex)
            {
                // Skip empty or uninitialized slots in the index array
                if (tag.CategoryMask.Mask == 0) continue;

                // 1. TAM Structural Data
                writer.Write(tag.Index);
                writer.Write(tag.BaseSubs);
                writer.Write(tag.CategoryMask.Mask);
                writer.Write(tag.MaxPotentialScore);

                // 2. TIM Structural Data (Precomputed Incompatibility Mask)
                writer.Write(tag.IncompatibilityMask.A);
                writer.Write(tag.IncompatibilityMask.B);
                writer.Write(tag.IncompatibilityMask.C);
                writer.Write(tag.IncompatibilityMask.D);
                writer.Write(tag.IncompatibilityMask.E);
                writer.Write(tag.IncompatibilityMask.F);
                writer.Write(tag.IncompatibilityMask.G);
                writer.Write(tag.IncompatibilityMask.H);
            }

            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hashBytes = md5.ComputeHash(ms.ToArray());

            // Hexadecimal string formatting for storage in ComputationRecords
            var sb = new System.Text.StringBuilder();
            foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }

}
