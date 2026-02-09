using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a fixed-size bitmask for tags (supports up to 448 tags).
    /// </summary>
    public struct TagMask
    {
        public ulong A, B, C, D, E, F, G;

        public static readonly TagMask Empty = new();

        // Set a bit at a given index (0..400)
        public void SetBit(int index)
        {
            if (index < 64) A |= 1UL << index;
            else if (index < 128) B |= 1UL << (index - 64);
            else if (index < 192) C |= 1UL << (index - 128);
            else if (index < 256) D |= 1UL << (index - 192);
            else if (index < 320) E |= 1UL << (index - 256);
            else if (index < 384) F |= 1UL << (index - 320);
            else if (index < 448) G |= 1UL << (index - 384);
            else throw new ArgumentOutOfRangeException(nameof(index), "Tag index exceeds 448");
        }

        // Test if a bit at index is set
        public bool IsSet(int index)
        {
            if (index < 64) return (A & (1UL << index)) != 0;
            else if (index < 128) return (B & (1UL << (index - 64))) != 0;
            else if (index < 192) return (C & (1UL << (index - 128))) != 0;
            else if (index < 256) return (D & (1UL << (index - 192))) != 0;
            else if (index < 320) return (E & (1UL << (index - 256))) != 0;
            else if (index < 384) return (F & (1UL << (index - 320))) != 0;
            else if (index < 448) return (G & (1UL << (index - 384))) != 0;
            else throw new ArgumentOutOfRangeException(nameof(index), "Tag index exceeds 448");
        }

        // Union of two masks
        public void Or(TagMask other)
        {
            A |= other.A;
            B |= other.B;
            C |= other.C;
            D |= other.D;
            E |= other.E;
            F |= other.F;
            G |= other.G;
        }

        // Intersection of two masks
        public TagMask And(TagMask other)
        {
            return new TagMask
            {
                A = this.A & other.A,
                B = this.B & other.B,
                C = this.C & other.C,
                D = this.D & other.D,
                E = this.E & other.E,
                F = this.F & other.F,
                G = this.G & other.G
            };
        }

        // Test if intersection is non-empty
        public bool Any(TagMask other)
        {
            return (A & other.A) != 0
                || (B & other.B) != 0
                || (C & other.C) != 0
                || (D & other.D) != 0
                || (E & other.E) != 0
                || (F & other.F) != 0
                || (G & other.G) != 0;
        }
    }
}
