using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a final, immutable combination of tags with its calculated score.
    /// Provides advanced formatting for text-based export and logging.
    /// </summary>
    public sealed class Combo
    {
        private static readonly float[] MultiplierTable = { 1f, 1f, 2f, 5f, 15f, 30f, 30f };

        public IReadOnlyList<Tag> Tags { get; init; }
        public int Score { get; init; }

        public Combo(IEnumerable<Tag> tags, int score)
        {
            Tags = tags?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(tags));
            Score = score;
        }

        /// <summary>
        /// Generates a detailed report of the combo, including base stats, 
        /// individual tags, and category synergy breakdowns.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            int totalBaseSubs = 0;
            ulong combinedCategories = 0;

            // Aggregate data using precomputed tag properties
            foreach (var tag in Tags)
            {
                totalBaseSubs += tag.BaseSubs;
                combinedCategories += tag.CategoryAdder;
            }

            float globalMultiplier = totalBaseSubs > 0 ? (float)Score / totalBaseSubs : 0f;

            // --- HEADER ---
            sb.AppendLine($"=== COMBO SCORE: {Score:N0} ===");
            sb.AppendLine($"Base Subs: {totalBaseSubs} | Global Multiplier: x{globalMultiplier:F2}");
            sb.AppendLine(new string('-', 40));

            // --- TAG LIST ---
            sb.AppendLine("Tags used:");
            foreach (var tag in Tags)
            {
                if (!TagMetadata.Names.TryGetValue(tag.Index, out var tagName))
                    tagName = $"UnknownTag_{tag.Index}";

                sb.AppendLine($" - [{tag.Index:D3}] {tagName,-20} (Base: {tag.BaseSubs})");
            }
            sb.AppendLine(new string('-', 40));

            // --- SYNERGIES ---
            sb.AppendLine("Synergies (Categories):");
            bool hasSynergy = false;

            // Extract category counts from the packed ulong (4 bits per category)
            for (int i = 0; i < 13; i++)
            {
                int count = (int)((combinedCategories >> (i * 4)) & 0xF);
                if (count <= 0) continue;

                hasSynergy = true;
                var categoryName = (Category)i;
                float currentMult = (count < MultiplierTable.Length) ? MultiplierTable[count] : MultiplierTable[^1];

                string bonusDisplay = currentMult > 1f ? $" -> Multiplier x{currentMult}" : " (No bonus)";
                string visualGauge = new string('■', count).PadRight(5, '·');

                sb.AppendLine($" {visualGauge} {categoryName,-12} : {count} tags{bonusDisplay}");
            }

            if (!hasSynergy)
                sb.AppendLine(" No active categories.");

            return sb.ToString();
        }
    }
}