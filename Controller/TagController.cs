using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
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
        /// Loads tags from the factory and builds internal caches and masks.
        /// Can be called again to refresh data after user modifications (TIM/TAM).
        /// </summary>
        public static void Initialize()
        {
            _tags = TagFactory.BuildTags();

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
    }
}
