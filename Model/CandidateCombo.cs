using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{

    public struct CandidateCombo
    {
       
        // --- Stockage Optimisé ---
        // Remplace short[] _categoryCounts.
        // Stocke 13 compteurs de 4 bits chacun dans un seul ulong (13 * 4 = 52 bits < 64).
        public ulong PackedCategoryCounts;

        public TagMask CumulativeIncompatibilityMask;
        public int BaseSubs;
        public int Size;

        // Table de multiplication (statique pour éviter de la recréer)
        private static readonly float[] Multipliers = { 1f, 1f, 2f, 5f, 15f, 30f, 30f, 30f };

        // Initialisation (plus besoin de 'new short[]')
        public void Reset()
        {
            PackedCategoryCounts = 0;
            CumulativeIncompatibilityMask = TagMask.Empty;
            BaseSubs = 0;
            Size = 0;
        }

        // Ajout ultra-rapide
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTag(Tag tag)
        {
            // 1. Addition atomique des catégories (magie du bit-packing)
            PackedCategoryCounts += tag.CategoryAdder;

            // 2. Mise à jour classique
            CumulativeIncompatibilityMask.Or(tag.IncompatibilityMask);
            BaseSubs += tag.BaseSubs;
            Size++;
        }

        // Calcul du score exact à l'instant T
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ComputeScore()
        {
            float multiplier = GetCurrentMultiplier();
            return (int)(BaseSubs * multiplier);
        }

        // Helper pour extraire le multiplicateur du ulong
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetCurrentMultiplier()
        {
            float mult = 1f;
            ulong temp = PackedCategoryCounts;

            // On déroule la boucle pour 13 catégories (performance critique)
            // (temp & 0xF) récupère la valeur des 4 derniers bits (le compteur de la cat courante)

            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 0
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 1
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 2
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 3
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 4
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 5
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 6
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 7
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 8
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 9
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 10
            mult *= Multipliers[temp & 0xF]; temp >>= 4; // Cat 11
            mult *= Multipliers[temp & 0xF];             // Cat 12

            return mult;
        }

        // Vérification de compatibilité
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAdd(Tag tag)
        {
            return !CumulativeIncompatibilityMask.IsSet(tag.Index);
        }
    }
}

