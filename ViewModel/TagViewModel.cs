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
        private bool _isDirty;
        public bool IsDirty
        {
            get => _isDirty;
            set { _isDirty = value; OnPropertyChanged(); }
        }
        private Tag _tag;
        public Tag Tag => _tag;
        private string? _nameOverride;
        private string? _descriptionOverride;
        private bool? _controversialOverride;
        private bool? _storyMissionOverride;
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
        public string Name
        {
            get => _nameOverride ?? TagMetadata.GetName(Index);
            set
            {
                if (Name == value) return;
                _nameOverride = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Retrieves the description/flavor text from the static metadata dictionary.
        /// </summary>
        public string Description
        {
            get => _descriptionOverride ?? TagMetadata.GetDescription(Index);
            set
            {
                if (Description == value) return;
                _descriptionOverride = value;
                OnPropertyChanged();
            }
        }
        /// <summary>
        /// Gets or sets the category mask. Reconstructs the internal Tag struct to maintain data integrity.
        /// </summary>
        public CategoryMask CategoryMask
        {
            get => _tag.CategoryMask;
            set
            {
                if (_tag.CategoryMask.Mask == value.Mask) return;

                // Reconstruct the struct to ensure the Tag property always returns valid data
                _tag = new Tag(_tag.Index, _tag.BaseSubs, _tag.IncompatibilityMask, value, _tag.MaxPotentialScore);

                OnPropertyChanged();
                OnPropertyChanged(nameof(Categories));
            }
        }
        /// <summary>
        /// Indicates if the tag is marked as 'Controversial' in the global metadata mask.
        /// </summary>
        public bool IsControversial
        {
            get => _controversialOverride ?? TagMetadata.IsControversial(Index);
            set
            {
                if (IsControversial == value) return;
                _controversialOverride = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the story mission status for the current session.
        /// </summary>
        public bool IsStoryMission
        {
            get => _storyMissionOverride ?? TagMetadata.IsStoryMission(Index);
            set
            {
                if (IsStoryMission == value) return;
                _storyMissionOverride = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A comma-separated string representation of the tag's categories.
        /// Generated on-the-fly from the bitmask.
        /// </summary>
        public string Categories => GetCategoriesString(_tag.CategoryMask);

        /// <summary>
        /// The string representation of the tag's rarity (e.g., "Viral", "Epic").
        /// </summary>
        public string Rarity
        {
            get => GetRarityEnum().ToString();
            set
            {
                // Convert string back to BaseSubs value
                int newBaseSubs = value switch
                {
                    "Common" => 5,
                    "Uncommon" => 15,
                    "Rare" => 45,
                    "Epic" => 135,
                    "Viral" => 405,
                    _ => Tag.BaseSubs // No change if unknown
                };

                if (Tag.BaseSubs != newBaseSubs)
                {
                    // Reconstruct the struct to update BaseSubs
                    _tag = new Tag(Tag.Index, newBaseSubs, Tag.IncompatibilityMask, Tag.CategoryMask, Tag.MaxPotentialScore);
                    OnPropertyChanged();
                }
            }
        }

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
            _tag = tag;
        }
        /// <summary>
        /// Clears local overrides to revert to the state stored in TagMetadata.
        /// </summary>
        public void RevertLocalOverrides()
        {
            _nameOverride = null;
            _descriptionOverride = null;
            _controversialOverride = null;
            _storyMissionOverride = null;
            OnPropertyChanged(string.Empty);
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

