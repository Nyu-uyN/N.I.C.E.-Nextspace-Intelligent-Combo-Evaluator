using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel.ResultsViewModel;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    public class ComboViewModel : INotifyPropertyChanged
    {
        public Combo Model { get; }
        public ObservableCollection<TagViewModel> TagVMs { get; }
        public ObservableCollection<SynergyLine> Synergies { get; }

        public long Score => Model.Score;
        public int BaseSubs { get; }
        public double GlobalMultiplier { get; }
        public int Rank { get; set; }
        public ComboViewModel(Combo combo)
        {
            Model = combo;
            // Transformation immédiate des Tags en TagViewModels pour profiter des noms/couleurs
            TagVMs = new ObservableCollection<TagViewModel>(combo.Tags.Select(t => new TagViewModel(t)));

            // Calculs de confort
            BaseSubs = combo.Tags.Sum(t => t.BaseSubs);
            GlobalMultiplier = BaseSubs > 0 ? (double)combo.Score / BaseSubs : 0;

            // Calcul des synergies
            Synergies = new ObservableCollection<SynergyLine>(CalculateSynergies(combo));
        }

        private List<SynergyLine> CalculateSynergies(Combo combo)
        {
            var categoryCounts = new Dictionary<Category, int>();
            foreach (var tag in combo.Tags)
            {
                foreach (Category cat in Enum.GetValues(typeof(Category)))
                {
                    if (tag.CategoryMask.IsSet((int)cat))
                        categoryCounts[cat] = categoryCounts.GetValueOrDefault(cat) + 1;
                }
            }

            return categoryCounts.Where(kvp => kvp.Value >= 2)
                .OrderByDescending(kvp => kvp.Value)
                .Select(entry => new SynergyLine
                {
                    CategoryName = entry.Key.ToString(),
                    Count = entry.Value,
                    Multiplier = entry.Value switch { 2 => 2.0, 3 => 5.0, 4 => 15.0, 5 => 30.0, _ => 1.0 },
                    VisualBars = new string('■', entry.Value).PadRight(5, '·')
                }).ToList();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}