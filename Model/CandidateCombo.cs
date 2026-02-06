using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public struct CandidateCombo
{
    public TagMask UsedTagsMask;
    public TagMask CumulativeIncompatibilityMask;
    public int BaseSubs; // somme des subs des tags
    public CategoryMask CategoriesMask; // cumul des catégories

    // Tableau de comptage par catégorie (index = Category enum)
    private short[] _categoryCounts;

    public CandidateCombo()
    {
        UsedTagsMask = TagMask.Empty;
        CumulativeIncompatibilityMask = TagMask.Empty;
        BaseSubs = 0;
        CategoriesMask = CategoryMask.Empty;
        _categoryCounts = new short[13]; // 13 catégories fixes
    }

    /// <summary>
    /// Ajoute un tag à ce combo et met à jour toutes les infos O(bits actifs)
    /// </summary>
    public void AddTag(Tag tag)
    {
        UsedTagsMask.SetBit(tag.Index);
        CumulativeIncompatibilityMask.Or(tag.IncompatibilityMask);
        BaseSubs += tag.BaseSubs;
        CategoriesMask.Or(tag.CategoryMask);

        // Met à jour les compteurs de catégories seulement pour les bits actifs
        ushort bits = tag.CategoryMask.Mask; // expose un getter ushort pour le champ _mask
        while (bits != 0)
        {
            int bitIndex = BitOperations.TrailingZeroCount(bits);
            _categoryCounts[bitIndex]++;
            bits &= (ushort)~(1 << bitIndex); // on supprime le bit traité
        }
    }

    /// <summary>
    /// Test rapide si un tag peut être ajouté
    /// </summary>
    public bool CanAdd(Tag tag)
    {
        return !UsedTagsMask.IsSet(tag.Index) &&
               !CumulativeIncompatibilityMask.IsSet(tag.Index);
    }

    /// <summary>
    /// Calcul du score en O(nombre de bits actifs)
    /// </summary>
    public int ComputeScore()
    {
        float totalMultiplier = 1f;

        // Parcourt uniquement les catégories présentes dans le combo
        ushort bits = CategoriesMask.Mask;
        while (bits != 0)
        {
            int bitIndex = BitOperations.TrailingZeroCount(bits);
            int count = _categoryCounts[bitIndex];

            totalMultiplier *= count switch
            {
                2 => 2f,
                3 => 5f,
                4 => 15f,
                5 => 30f,
                _ => 1f
            };

            bits &= (ushort)~(1 << bitIndex);
        }

        return (int)Math.Round(BaseSubs * totalMultiplier);
    }
}

