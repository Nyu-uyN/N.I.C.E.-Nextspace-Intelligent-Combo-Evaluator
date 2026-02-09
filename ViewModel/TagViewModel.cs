using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    
    
    public sealed class TagViewModel : INotifyPropertyChanged
    {
        public Tag Tag { get; }

        public int Index => Tag.Index;
        public int BaseSubs => Tag.BaseSubs;
        public long MaxPotentialScore => Tag.MaxPotentialScore;

        public string Name => TagMetadata.Names[Index];
        public string Description => TagMetadata.Descriptions[Index];

        public bool IsControversial => TagMetadata.ControversialMask.IsSet(Index);
        public bool IsStoryMission => TagMetadata.StoryMissionMask.IsSet(Index);

        public string Categories => GetCategoriesString(Tag.CategoryMask);
        public string Rarity => GetRarityEnum().ToString();

        private bool _include = true;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        

        private static string GetCategoriesString(CategoryMask mask)
        {
            if (mask.Mask == 0)
                return string.Empty;

            var sb = new StringBuilder();
            ushort bits = mask.Mask;

            while (bits != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bits);

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append((Category)bitIndex);

                bits &= (ushort)(bits - 1); // clear lowest set bit
            }

            return sb.ToString();
        }
        /// <summary>
        /// Returns true if the tag has the specified category.
        /// </summary>
        public bool CategoriesContains(Category category)
        {
            return Tag.CategoryMask.IsSet((int)category);
        }

        /// <summary>
        /// Returns the Rarity enum corresponding to this tag's BaseSubs.
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
    }
}

