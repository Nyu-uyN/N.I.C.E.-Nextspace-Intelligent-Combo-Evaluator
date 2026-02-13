using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Determines the UI color based on a Tag's rarity.
    /// Since the Tag model is lean, the converter infers rarity from BaseSubs.
    /// </summary>
    public class RarityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Rarity r = Rarity.Common;

            if (value is Rarity rarity)
            {
                r = rarity;
            }
            else if (value is TagViewModel tag)
            {
                r = tag.GetRarityEnum();
            }

            return r switch
            {
                Rarity.Common => new SolidColorBrush(Color.FromRgb(225, 225, 225)),   // Gray
                Rarity.Uncommon => new SolidColorBrush(Color.FromRgb(173, 216, 230)), // Light Blue
                Rarity.Rare => new SolidColorBrush(Color.FromRgb(216, 191, 216)),     // Mauve/Thistle
                Rarity.Epic => new SolidColorBrush(Color.FromRgb(255, 200, 100)),     // Orange
                Rarity.Viral => new SolidColorBrush(Color.FromRgb(255, 120, 120)),    // Red
                _ => Brushes.White
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}