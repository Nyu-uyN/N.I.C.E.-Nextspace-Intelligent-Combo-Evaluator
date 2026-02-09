using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<TagViewModel> Tags { get; }

        // --- Filters / Options ---
        private bool _excludeStoryMissions;
        public bool ExcludeStoryMissions
        {
            get => _excludeStoryMissions;
            set { if (_excludeStoryMissions != value) { _excludeStoryMissions = value; OnPropertyChanged(); ApplyFilters(); } }
        }

        private bool _excludeControversial;
        public bool ExcludeControversial
        {
            get => _excludeControversial;
            set { if (_excludeControversial != value) { _excludeControversial = value; OnPropertyChanged(); ApplyFilters(); } }
        }

        private Rarity _minRarity = Rarity.Common;
        public Rarity MinRarity
        {
            get => _minRarity;
            set { if (_minRarity != value) { _minRarity = value; OnPropertyChanged(); ApplyFilters(); } }
        }

        public HashSet<Category> ExcludedCategories { get; } = new();

        private int _maxComboSize = 5;
        public int MaxComboSize
        {
            get => _maxComboSize;
            set { _maxComboSize = value; OnPropertyChanged(); }
        }

        private int _numberOfResults = 5;
        public int NumberOfResults
        {
            get => _numberOfResults;
            set { _numberOfResults = value; OnPropertyChanged(); }
        }

        private bool _topNNonReusedTags = false;
        public bool TopNNonReusedTags
        {
            get => _topNNonReusedTags;
            set { _topNNonReusedTags = value; OnPropertyChanged(); }
        }

        private int _minMaxPotentialScore = 0;
        public int MinMaxPotentialScore
        {
            get => _minMaxPotentialScore;
            set { _minMaxPotentialScore = value; OnPropertyChanged(); ApplyFilters(); }
        }

        private bool _useMultithreading = true;
        public bool UseMultithreading
        {
            get => _useMultithreading;
            set { _useMultithreading = value; OnPropertyChanged(); }
        }

        private int _threadCount = Environment.ProcessorCount;
        public int ThreadCount
        {
            get => _threadCount;
            set { _threadCount = value; OnPropertyChanged(); }
        }

        // --- Status / Progress ---
        private string _statusText = "Idle";
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private long _tagsExplored = 0;
        public long TagsExplored
        {
            get => _tagsExplored;
            set { _tagsExplored = value; OnPropertyChanged(); }
        }

        private TimeSpan _elapsedTime = TimeSpan.Zero;
        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set { _elapsedTime = value; OnPropertyChanged(); }
        }

        // --- Commands ---
        public ICommand ComputeCommand { get; }
        public ICommand AbortCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand IncludeAllCommand { get; }
        public ICommand ExcludeAllCommand { get; }
        public ICommand ApplyFiltersCommand { get; }

        public MainViewModel()
        {
            // Tags triés par Index par défaut
            Tags = new ObservableCollection<TagViewModel>(
                TagController.Tags
                    .OrderBy(t => t.Index)
                    .Select(t => new TagViewModel(t))
            );

            // Commands placeholders
            ComputeCommand = new DummyCommand();
            AbortCommand = new DummyCommand();
            ResetFiltersCommand = new DummyCommand();
            IncludeAllCommand = new DummyCommand();
            ExcludeAllCommand = new DummyCommand();
            ApplyFiltersCommand = new DummyCommand();
        }

        // --- Filter logic ---
        private void ApplyFilters()
        {
            foreach (var tagVm in Tags)
            {
                bool include = true;

                if (ExcludeControversial && tagVm.IsControversial)
                    include = false;

                if (ExcludeStoryMissions && tagVm.IsStoryMission)
                    include = false;

                if ((int)tagVm.MaxPotentialScore < MinMaxPotentialScore)
                    include = false;

                if ((int)tagVm.GetRarityEnum() < (int)MinRarity)
                    include = false;

                if (ExcludedCategories.Any(c => tagVm.CategoriesContains(c)))
                    include = false;

                tagVm.Include = include;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

