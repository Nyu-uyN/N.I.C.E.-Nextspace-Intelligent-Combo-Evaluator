using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents the mutable state of a tag as stored in persistence.
    /// This class is the primary target for TAM (Tag Attribute Manager) 
    /// and TIM (Tag Incompatibilities Manager) modifications.
    /// </summary>
    public sealed class RawTag
    {
        /// <summary>
        /// Unique identifier and bitmask index. This value must remain 
        /// unchanged to maintain relational integrity.
        /// </summary>
        public int Id { get; init; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Rarity level: 0 (Common) to 4 (Viral).
        /// </summary>
        public int Rarity { get; set; }

        public string[] Categories { get; set; } = Array.Empty<string>();

        public bool IsControversial { get; set; }

        public bool IsStoryMission { get; set; }

        public int[] IncompatibleIds { get; set; } = Array.Empty<int>();

        public int MaxPotentialScore { get; set; }






        public RawTag() { }

        public RawTag(TagViewModel vm)
        {
            this.Id = vm.Index;
            this.Name = vm.Name;
            this.Description = vm.Description;
            this.IsControversial = vm.IsControversial;
            this.IsStoryMission = vm.IsStoryMission;
            this.MaxPotentialScore = (int)vm.MaxPotentialScore;
            this.Rarity = (int)vm.GetRarityEnum();

            // On utilise la puissance des masques
            this.Categories = ExtractCategories(vm.Tag.CategoryMask);
            this.IncompatibleIds = ExtractIdsFromTagMask(vm.Tag.IncompatibilityMask);
        }

        private static int[] ExtractIdsFromTagMask(TagMask mask)
        {
            var ids = new List<int>();

            // On traite chaque ulong (A à G) avec TrailingZeroCount
            // Décalage de 64 bits à chaque palier
            AppendIdsFromUlong(ids, mask.A, 0);
            AppendIdsFromUlong(ids, mask.B, 64);
            AppendIdsFromUlong(ids, mask.C, 128);
            AppendIdsFromUlong(ids, mask.D, 192);
            AppendIdsFromUlong(ids, mask.E, 256);
            AppendIdsFromUlong(ids, mask.F, 320);
            AppendIdsFromUlong(ids, mask.G, 384);

            return ids.ToArray();
        }

        private static void AppendIdsFromUlong(List<int> ids, ulong val, int offset)
        {
            while (val != 0)
            {
                int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(val);
                ids.Add(offset + bitIndex);
                val &= (val - 1); // Efface le bit le plus bas
            }
        }

        private static string[] ExtractCategories(CategoryMask mask)
        {
            // On suppose ici que CategoryMask est soit un TagMask, 
            // soit le ushort que tu as montré plus tôt.
            // Si c'est le ushort :
            if (mask.Mask == 0) return Array.Empty<string>();

            var list = new List<string>();
            ushort val = mask.Mask;
            while (val != 0)
            {
                int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(val);
                list.Add(((Category)bitIndex).ToString());
                val = (ushort)(val & (val - 1));
            }
            return list.ToArray();
        }
    }
}
