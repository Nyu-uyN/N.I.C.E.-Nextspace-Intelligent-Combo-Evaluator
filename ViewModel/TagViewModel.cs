using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// ViewModel wrapper for a Tag entity.
    /// Exposes immutable tag properties for UI data binding and manages the mutable selection state.
    /// </summary>
    public sealed class TagViewModel : INotifyPropertyChanged
    {
        public Tag Tag { get; }

        public int Index => Tag.Index;
        public int BaseSubs => Tag.BaseSubs;

        /// <summary>
        /// The theoretically highest score this tag can achieve in an ideal combo.
        /// Calculated via the Deep Scan process.
        /// </summary>
        public long MaxPotentialScore => Tag.MaxPotentialScore;

        /// <summary>
        /// Retrieves the display name from the static metadata dictionary.
        /// </summary>
        public string Name => TagMetadata.Names.TryGetValue(Index, out var name) ? name : $"Unknown_{Index}";

        /// <summary>
        /// Retrieves the description/flavor text from the static metadata dictionary.
        /// </summary>
        public string Description => TagMetadata.Descriptions.TryGetValue(Index, out var desc) ? desc : string.Empty;

        /// <summary>
        /// Indicates if the tag is marked as 'Controversial' in the global metadata mask.
        /// </summary>
        public bool IsControversial => TagMetadata.ControversialMask.IsSet(Index);

        /// <summary>
        /// Indicates if the tag is part of a Story Mission requirement.
        /// </summary>
        public bool IsStoryMission => TagMetadata.StoryMissionMask.IsSet(Index);

        /// <summary>
        /// A comma-separated string representation of the tag's categories.
        /// Generated on-the-fly from the bitmask.
        /// </summary>
        public string Categories => GetCategoriesString(Tag.CategoryMask);

        /// <summary>
        /// The string representation of the tag's rarity (e.g., "Viral", "Epic").
        /// </summary>
        public string Rarity => GetRarityEnum().ToString();

        private bool _include = true;
        /// <summary>
        /// Gets or sets whether this tag should be included in the solver's pool.
        /// This property supports two-way binding with UI CheckBoxes.
        /// </summary>
        public bool Include
        {
            get => _include;
            set
            {
                if (_include == value)
                    return;

                _include = value;
                OnPropertyChanged();
            }
        }

        public TagViewModel(Tag tag)
        {
            Tag = tag;
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion

        #region Helpers

        /// <summary>
        /// Decodes the 13-bit CategoryMask into a readable comma-separated string.
        /// Uses bit manipulation to iterate only over set bits.
        /// </summary>
        private static string GetCategoriesString(CategoryMask mask)
        {
            if (mask.Mask == 0)
                return string.Empty;

            var sb = new StringBuilder();
            ushort bits = mask.Mask;

            while (bits != 0)
            {
                // Find the index of the least significant bit that is set
                int bitIndex = BitOperations.TrailingZeroCount(bits);

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append((Category)bitIndex);

                // Kernighan's algorithm: clear the lowest set bit
                bits &= (ushort)(bits - 1);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks if the underlying Tag possesses a specific category.
        /// </summary>
        public bool CategoriesContains(Category category)
        {
            return Tag.CategoryMask.IsSet((int)category);
        }

        /// <summary>
        /// Derives the Rarity enum from the BaseSubs value.
        /// </summary>
        public Rarity GetRarityEnum()
        {
            return Tag.BaseSubs switch
            {
                5 => Model.Enums.Rarity.Common,
                15 => Model.Enums.Rarity.Uncommon,
                45 => Model.Enums.Rarity.Rare,
                135 => Model.Enums.Rarity.Epic,
                405 => Model.Enums.Rarity.Viral,
                _ => throw new InvalidOperationException($"Unknown BaseSubs value: {Tag.BaseSubs}")
            };
        }

        #endregion
    }
}

