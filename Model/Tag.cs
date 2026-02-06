using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Optimized Tag for N.I.C.E.
    /// Stores precomputed masks and base subs for fast combo evaluation.
    /// </summary>
    public readonly struct Tag
    {
        public int Index { get; }
        public int BaseSubs { get; }
        public TagMask IncompatibilityMask { get; }
        public CategoryMask CategoryMask { get; }
        public int MaxPotentialScore { get; }

        // Constructor
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
        }
    }
}

