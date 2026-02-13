using System;
using System.Runtime.CompilerServices;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// High-performance mutable struct representing the search state of a combo.
    /// Uses bit-packing (SWAR) to maintain category counts and bitmasks for compatibility.
    /// </summary>
    public struct CandidateCombo
    {
        /// <summary>
        /// Stores 13 category counters (4 bits each) within a single 64-bit integer.
        /// [Slot 12][Slot 11]...[Slot 0]
        /// </summary>
        public ulong PackedCategoryCounts;

        public TagMask CumulativeIncompatibilityMask;
        public int BaseSubs;
        public int Size;

        private static readonly float[] Multipliers = { 1f, 1f, 2f, 5f, 15f, 30f, 30f, 30f };

        /// <summary>
        /// Resets the candidate state for a new search branch.
        /// </summary>
        public void Reset()
        {
            PackedCategoryCounts = 0;
            CumulativeIncompatibilityMask = TagMask.Empty;
            BaseSubs = 0;
            Size = 0;
        }

        /// <summary>
        /// Updates the candidate state by adding a new tag.
        /// Categories are updated via a single atomic addition.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTag(Tag tag)
        {
            PackedCategoryCounts += tag.CategoryAdder;
            CumulativeIncompatibilityMask.Or(tag.IncompatibilityMask);
            BaseSubs += tag.BaseSubs;
            Size++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int ComputeScore()
        {
            return (int)(BaseSubs * GetCurrentMultiplier());
        }

        /// <summary>
        /// Extracts and multiplies all category synergies from the packed ulong.
        /// Manually unrolled for peak performance in the solver's hot path.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float GetCurrentMultiplier()
        {
            float mult = 1f;
            ulong temp = PackedCategoryCounts;

            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF]; temp >>= 4;
            mult *= Multipliers[temp & 0xF];

            return mult;
        }

        /// <summary>
        /// Checks if a tag is compatible with the current selection using bitwise lookup.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool CanAdd(Tag tag)
        {
            return !CumulativeIncompatibilityMask.IsSet(tag.Index);
        }
    }
}

