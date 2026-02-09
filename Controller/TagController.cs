using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller
{
    /// <summary>
    /// Central controller for all Tags.
    /// Maintains list of tags and provides fast access via masks or IDs.
    /// </summary>
    public static class TagController
    {
        private static readonly List<Tag> _tags;
        private static readonly Tag[] _tagsByIndex;
        private static readonly Tag[] _tagsByMaxPotentialScoreDesc;

        public static readonly TagMask AllTagsMask;
        static TagController()
        {
            _tags = TagFactory.BuildTags();

            int maxIndex = _tags.Count;
            _tagsByIndex = new Tag[maxIndex];

            var allMask = TagMask.Empty;

            foreach (var tag in _tags)
            {
                _tagsByIndex[tag.Index] = tag;
                allMask.SetBit(tag.Index);
            }

            AllTagsMask = allMask;

            // Single global sort by MaxPotentialScore (descending)
            _tagsByMaxPotentialScoreDesc = _tags
                .OrderByDescending(t => t.MaxPotentialScore)
                .ToArray();
        }

        public static IReadOnlyList<Tag> Tags => _tags;

        public static Tag GetByIndex(int index) => _tagsByIndex[index];

        public static TagMask GetMaskFromIndices(IEnumerable<int> indices)
        {
            var mask = TagMask.Empty;
            foreach (var idx in indices)
                mask.SetBit(idx);
            return mask;
        }
        public static Tag[] AllTagsByMaxPotentialScoreDesc
            => _tagsByMaxPotentialScoreDesc;
        public static TagMask GetMaskFromTags(IEnumerable<Tag> tags)
        {
            var mask = TagMask.Empty;
            foreach (var tag in tags)
                mask.SetBit(tag.Index);
            return mask;
        }

        /// <summary>
        /// Ultra-performant : récupère tous les tags présents dans le mask
        /// sans parcourir tous les tags.
        /// </summary>
        public static List<Tag> GetTagsFromMask(TagMask mask)
        {
            var result = new List<Tag>();

            void AddActiveBits(ulong bits, int baseIndex)
            {
                while (bits != 0)
                {
                    int bitPos = BitOperations.TrailingZeroCount(bits);
                    result.Add(_tagsByIndex[baseIndex + bitPos]);
                    bits &= bits - 1; // clear lowest set bit
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
