using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a combo of tags and its associated score.
    /// Immutable and JSON-friendly.
    /// </summary>
    public sealed class Combo
    {
        /// <summary>
        /// tags that compose this combo.
        /// </summary>
        public IReadOnlyList<Tag> Tags { get; init; }

        /// <summary>
        /// Score of the combo.
        /// Higher is better.
        /// </summary>
        public int Score { get; init; }

        public Combo(IEnumerable<Tag> tags, int score)
        {
            if (tags is null)
                throw new ArgumentNullException(nameof(tags));

            Tags = new List<Tag>(tags).AsReadOnly();
            Score = score;
        }
        public override string ToString()
        {
            var sb = new StringBuilder();

            // 1. Calculs préliminaires pour l'affichage
            int totalBaseSubs = 0;
            int[] categoryCounts = new int[13]; // 13 catégories dans l'Enum

            foreach (var tag in Tags)
            {
                totalBaseSubs += tag.BaseSubs;

                // On décode le masque de catégorie pour compter
                ushort mask = tag.CategoryMask.Mask;
                int catIndex = 0;
                while (mask != 0)
                {
                    if ((mask & 1) != 0)
                    {
                        categoryCounts[catIndex]++;
                    }
                    mask >>= 1;
                    catIndex++;
                }
            }

            // Calcul du multiplicateur global réel
            float totalMultiplier = totalBaseSubs > 0 ? (float)Score / totalBaseSubs : 0f;

            // --- EN-TÊTE ---
            sb.AppendLine($"=== COMBO SCORE: {Score:N0} ===");
            sb.AppendLine($"Base Subs: {totalBaseSubs} | Multiplicateur Global: x{totalMultiplier:0.00}");
            sb.AppendLine(new string('-', 40));

            // --- LISTE DES TAGS ---
            sb.AppendLine("Tags utilisés :");
            foreach (var tag in Tags)
            {
                // Récupération du nom via TagMetadata
                string tagName = TagMetadata.Names.TryGetValue(tag.Index, out var name)
                    ? name
                    : $"UnknownTag_{tag.Index}";

                sb.AppendLine($" - [{tag.Index}] {tagName,-20} (Subs: {tag.BaseSubs})");
            }
            sb.AppendLine(new string('-', 40));

            // --- DÉTAIL DES CATÉGORIES (SYNERGIES) ---
            sb.AppendLine("Synergies (Catégories) :");

            bool hasSynergy = false;
            // Table de correspondance pour l'affichage (reprise de ta logique)
            float[] multTable = { 1f, 1f, 2f, 5f, 15f, 30f, 30f };

            for (int i = 0; i < categoryCounts.Length; i++)
            {
                int count = categoryCounts[i];
                if (count > 0)
                {
                    hasSynergy = true;
                    var categoryName = (Category)i;
                    float currentMult = (count < multTable.Length) ? multTable[count] : 30f;

                    // Formatage : Nom catégorie, jauge visuelle, compte et bonus
                    string bonusDisplay = currentMult > 1f ? $" -> Multiplicateur x{currentMult}" : " (Pas de bonus)";
                    string visualGauge = new string('■', count).PadRight(5, '·'); // Ex: ■■■··

                    sb.AppendLine($" {visualGauge} {categoryName,-10} : {count} tags{bonusDisplay}");
                }
            }

            if (!hasSynergy)
            {
                sb.AppendLine(" Aucune catégorie active.");
            }

            return sb.ToString();
        }
    }
}
