using System;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents an immutable, computation-ready tag.
    /// Designed for high-performance evaluation within the solver.
    /// </summary>
    public readonly struct Tag : IEquatable<Tag>
    {
        public int Index { get; }
        public int BaseSubs { get; }
        public TagMask IncompatibilityMask { get; }
        public CategoryMask CategoryMask { get; }
        public int MaxPotentialScore { get; }

        /// <summary>
        /// A packed 64-bit integer where each 4-bit slot represents a category counter.
        /// Allows incrementing all category counts in a combo using a single integer addition.
        /// </summary>
        /// 
        public ulong CategoryAdder { get; }

        /// <summary>
        /// Initializes a new instance of the Tag struct and precomputes the CategoryAdder.
        /// </summary>
        public Tag(
            int index,
            int baseSubs,
            TagMask incompatibilityMask,
            CategoryMask categoryMask,
            int maxPotentialScore)
        {
            Index = index;
            BaseSubs = baseSubs;
            IncompatibilityMask = incompatibilityMask;
            CategoryMask = categoryMask;
            MaxPotentialScore = maxPotentialScore;

            // Precompute the packed category adder
            ulong adder = 0;
            ushort bits = categoryMask.Mask;

            while (bits != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bits);
                adder |= 1UL << (bitIndex * 4);
                bits &= (ushort)~(1 << bitIndex);
            }

            CategoryAdder = adder;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Tag other) => Index == other.Index;

        public override bool Equals(object? obj) => obj is Tag other && Equals(other);

        public override int GetHashCode() => Index;

        public static bool operator ==(Tag left, Tag right) => left.Equals(right);

        public static bool operator !=(Tag left, Tag right) => !left.Equals(right);
    }
}

