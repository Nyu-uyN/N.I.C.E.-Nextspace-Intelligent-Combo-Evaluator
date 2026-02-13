using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Optimized bitmask supporting up to 448 unique tag indices using seven 64-bit integers.
    /// Used as the primary data structure for high-speed compatibility checks.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 64, Size =64)]
    public struct TagMask : IEquatable<TagMask>
    {
        public ulong A, B, C, D, E, F, G, H;


        public static readonly TagMask Empty = new();

        /// <summary>
        /// Sets the bit at the specified index.
        /// </summary>
        /// <param name="index">Target bit index (0-447).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBit(int index)
        {
            if (index < 64) { A |= 1UL << index; return; }
            if (index < 128) { B |= 1UL << (index - 64); return; }
            if (index < 192) { C |= 1UL << (index - 128); return; }
            if (index < 256) { D |= 1UL << (index - 192); return; }
            if (index < 320) { E |= 1UL << (index - 256); return; }
            if (index < 384) { F |= 1UL << (index - 320); return; }
            if (index < 448) { G |= 1UL << (index - 384); return; }
            if (index < 512) { H |= 1UL << (index - 448); return; }
        }

        /// <summary>
        /// Checks if the bit at the specified index is set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsSet(int index)
        {
            if (index < 64) return (A & (1UL << index)) != 0;
            if (index < 128) return (B & (1UL << (index - 64))) != 0;
            if (index < 192) return (C & (1UL << (index - 128))) != 0;
            if (index < 256) return (D & (1UL << (index - 192))) != 0;
            if (index < 320) return (E & (1UL << (index - 256))) != 0;
            if (index < 384) return (F & (1UL << (index - 320))) != 0;
            if (index < 448) return (G & (1UL << (index - 384))) != 0;
            if (index < 512) return (H & (1UL << (index - 448))) != 0;
            throw new ArgumentOutOfRangeException(nameof(index), "Tag index exceeds 511");
        }

        /// <summary>
        /// Performs a bitwise OR operation with another mask, modifying this instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Or(TagMask other)
        {
            A |= other.A;
            B |= other.B;
            C |= other.C;
            D |= other.D;
            E |= other.E;
            F |= other.F;
            G |= other.G;
            H |= other.H;
        }

        /// <summary>
        /// Returns a new mask representing the bitwise AND intersection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TagMask And(TagMask other)
        {
            return new TagMask
            {
                A = A & other.A,
                B = B & other.B,
                C = C & other.C,
                D = D & other.D,
                E = E & other.E,
                F = F & other.F,
                G = G & other.G,
                H = H & other.H
            };
        }

        /// <summary>
        /// Determines if there is at least one common bit set between this mask and another.
        /// Used for rapid incompatibility detection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Any(TagMask other)
        {

            return (A & other.A) != 0 || (B & other.B) != 0 || (C & other.C) != 0 || (D & other.D) != 0 ||
               (E & other.E) != 0 || (F & other.F) != 0 || (G & other.G) != 0 || (H & other.H) != 0;
        }

        public readonly bool Equals(TagMask other)
        {
            return A == other.A && B == other.B && C == other.C && D == other.D &&
                   E == other.E && F == other.F && G == other.G && H == other.H;
        }
        /// <summary>
        /// Calculates the total number of set bits (population count) across all internal fields (A-H).
        /// Used to determine the density of the mask for heuristic calculations.
        /// </summary>
        /// <returns>The total count of bits set to 1.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CountBits()
        {
            // BitOperations.PopCount is hardware accelerated (POPCNT instruction) on modern CPUs
            return BitOperations.PopCount(A) +
                   BitOperations.PopCount(B) +
                   BitOperations.PopCount(C) +
                   BitOperations.PopCount(D) +
                   BitOperations.PopCount(E) +
                   BitOperations.PopCount(F) +
                   BitOperations.PopCount(G) +
                   BitOperations.PopCount(H);
        }

        public override readonly bool Equals(object? obj) => obj is TagMask other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(A, B, C, D, E, F, G, H);

        public static bool operator ==(TagMask left, TagMask right) => left.Equals(right);

        public static bool operator !=(TagMask left, TagMask right) => !left.Equals(right);
    }
}
