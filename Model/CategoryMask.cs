using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a bitmask for categories (max 16 categories).
    /// Supports fast O(1) operations.
    /// </summary>
    public struct CategoryMask
    {
        private ushort _mask; // 16 bits suffisent pour 13 catégories

        public static readonly CategoryMask Empty = new();

        public ushort Mask { get => _mask; set => _mask = value; }

        public void SetBit(int index)
        {
            Mask |= (ushort)(1 << index);
        }

        public bool IsSet(int index)
        {
            return (Mask & (1 << index)) != 0;
        }

        public void Or(CategoryMask other)
        {
            Mask |= other.Mask;
        }

        public bool Any(CategoryMask other)
        {
            return (Mask & other.Mask) != 0;
        }

        public int CountBits()
        {
            return BitOperations.PopCount(Mask);
        }
    }
}
