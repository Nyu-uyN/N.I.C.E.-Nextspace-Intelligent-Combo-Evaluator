using System;
using System.Collections.Generic;
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

        public static readonly CategoryMask Empty = new CategoryMask();

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
            // Hamming weight rapide pour 16 bits
            ushort m = Mask;
            int count = 0;
            while (m != 0)
            {
                count++;
                m &= (ushort)(m - 1);
            }
            return count;
        }
    }
}
