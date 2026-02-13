using System;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Ultra-lightweight structure optimized for the Set Packing algorithm.
    /// Contains only the vital data required for collision testing and score summation.
    /// </summary>
    public readonly struct PackedCombo
    {
        /// <summary>
        /// Index of the original combo in the source pool for reconstruction purposes.
        /// </summary>
        public readonly int OriginalIndex;

        /// <summary>
        /// Pre-calculated final score of the combo.
        /// </summary>
        public readonly int Score;

        /// <summary>
        /// Bitmask of all tags used in this combo.
        /// Enables O(1) collision detection between two combos using bitwise AND.
        /// </summary>
        public readonly TagMask UsedTagsMask;

        /// <summary>
        /// Initializes a new instance of the PackedCombo and builds its tag bitmask.
        /// </summary>
        /// <param name="index">Pool index.</param>
        /// <param name="sourceCombo">The full combo to pack.</param>
        public PackedCombo(int index, Combo sourceCombo)
        {
            OriginalIndex = index;
            Score = (int)sourceCombo.Score;

            // One-time mask construction to avoid redundant bit-setting during search iterations
            var mask = TagMask.Empty;
            if (sourceCombo.Tags != null)
            {
                foreach (var tag in sourceCombo.Tags)
                {
                    mask.SetBit(tag.Index);
                }
            }
            UsedTagsMask = mask;
        }
    }
}
