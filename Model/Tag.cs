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
        public long MaxPotentialScore { get; }
        // --- OPTIMISATION : Champ ajouté ---
        // Représente les compteurs sous forme packée (4 bits par catégorie).
        // Permet d'ajouter les catégories au combo via une simple addition d'entiers.
        public ulong CategoryAdder { get; }
        // Constructor
        public Tag(
            int index,
            int baseSubs,
            TagMask incompatibilityMask,
            CategoryMask categoryMask,
            long maxPotentialScore)
        {
            Index = index;
            BaseSubs = baseSubs;
            IncompatibilityMask = incompatibilityMask;
            CategoryMask = categoryMask;
            MaxPotentialScore = maxPotentialScore;
            CategoryAdder = 0;
            ushort bits = categoryMask.Mask;
            int bitIndex = 0;
            while (bits != 0)
            {
                if ((bits & 1) != 0)
                {
                    // On met le bit à 1 dans le slot de 4 bits correspondant à la catégorie
                    CategoryAdder |= 1UL << (bitIndex * 4);
                }
                bits >>= 1;
                bitIndex++;
            }
        }
    }
}

