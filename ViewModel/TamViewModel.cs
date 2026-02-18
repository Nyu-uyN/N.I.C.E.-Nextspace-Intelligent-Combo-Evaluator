using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// ViewModel for the Tag Attribute Manager.
    /// Tracks changes using the project's TagMask structure for O(1) dirty checking.
    /// </summary>
    public sealed class TamViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<int, CategoryMask> _initialCategoryMasks = new();
        private readonly Dictionary<int, int> _initialBaseSubs = new();
        private readonly Dictionary<int, Tag> _factoryDefaults = new();
        private TagMask _dirtyCosmeticMask = TagMask.Empty;
        private TagMask _dirtyStructuralMask = TagMask.Empty;
        public bool HasValidationError => !string.IsNullOrEmpty(ValidationError);
        private TagViewModel? _selectedTag;
        private string _searchText = string.Empty;
        private string? _validationError;
        private bool _isBusy;
        private bool _isSyncing;

        public ObservableCollection<TagViewModel> Tags { get; }
        public ICollectionView TagView { get; }

        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); } }
        public bool HasPendingChanges => _dirtyCosmeticMask.CountBits() > 0 || _dirtyStructuralMask.CountBits() > 0;
        public bool HasPendingStructuralChanges => _dirtyStructuralMask.CountBits() > 0;
        public string? ValidationError { get => _validationError; private set { _validationError = value; OnPropertyChanged(); } }
        public ICommand CommitCommand { get; }
        public ICommand RevertSelectedToSessionCommand { get; }
        public ICommand RevertAllToSessionCommand { get; }
        public ICommand RevertSelectedToDefaultCommand { get; }
        public ICommand RevertAllToDefaultCommand { get; }
        public TagViewModel? SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (_selectedTag == value) return;
                _selectedTag = value;
                OnPropertyChanged();
                NotifyAllCategoriesChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); TagView.Refresh(); }
        }

        public TamViewModel()
        {
            Tags = new ObservableCollection<TagViewModel>();
            var defaultTags = TagController.GetDefaultTags(); 
            foreach (var dt in defaultTags) _factoryDefaults[dt.Index] = dt;
            // TagController.Tags returns List<Tag>
            var activeTags = TagController.Tags;

            foreach (var tag in activeTags)
            {
                // 1. Snapshot original state from the struct
                _initialCategoryMasks[tag.Index] = tag.CategoryMask;
                _initialBaseSubs[tag.Index] = tag.BaseSubs;

                // 2. Wrap the struct into a ViewModel
                var vm = new TagViewModel(tag);

                // 3. Subscribe to changes for dirty tracking
                vm.PropertyChanged += OnTagPropertyChanged;

                Tags.Add(vm);
            }

            // Initialize View
            TagView = CollectionViewSource.GetDefaultView(Tags);
            TagView.Filter = item =>
            {
                if (string.IsNullOrWhiteSpace(SearchText)) return true;
                var vm = (TagViewModel)item;
                return vm.Index.ToString().Contains(SearchText) ||
                       vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            };
            if (Tags.Count > 0)
            {
                SelectedTag = Tags[0];
            }
            // Commands Initialization
            CommitCommand = new RelayCommand(async _ => await ExecuteCommit(), _ => ValidationError == null && HasPendingChanges);
            RevertSelectedToSessionCommand = new RelayCommand(_ => RevertSelectedToSession(), _ => SelectedTag != null);
            RevertAllToSessionCommand = new RelayCommand(_ => RevertAllToSession(), _ => HasPendingChanges);
            RevertSelectedToDefaultCommand = new RelayCommand(_ => RevertSelectedToDefault(), _ => SelectedTag != null);
            RevertAllToDefaultCommand = new RelayCommand(_ => RevertAllToDefault(), _ => Tags.Any());
        }

        private void OnTagPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not TagViewModel vm || _isSyncing || e.PropertyName == nameof(TagViewModel.IsDirty)) return;

            // 1. Structural Check (Categories OR Rarity)
            bool isStructuralDirty =
                vm.Tag.CategoryMask.Mask != _initialCategoryMasks[vm.Index].Mask ||
                vm.Tag.BaseSubs != _initialBaseSubs[vm.Index];

            if (isStructuralDirty)
                _dirtyStructuralMask.SetBit(vm.Index);
            else
                ClearBitInTagMask(ref _dirtyStructuralMask, vm.Index);

            // 2. Cosmetic Check (Name, Description, Flags)
            bool isCosmeticDirty =
                vm.Name != TagMetadata.GetName(vm.Index) ||
                vm.Description != TagMetadata.GetDescription(vm.Index) ||
                vm.IsControversial != TagMetadata.IsControversial(vm.Index) ||
                vm.IsStoryMission != TagMetadata.IsStoryMission(vm.Index);

            if (isCosmeticDirty)
                _dirtyCosmeticMask.SetBit(vm.Index);
            else
                ClearBitInTagMask(ref _dirtyCosmeticMask, vm.Index);
            vm.IsDirty = isStructuralDirty || isCosmeticDirty;
            OnPropertyChanged(nameof(HasPendingChanges));
            OnPropertyChanged(nameof(HasPendingStructuralChanges));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            Validate();
        }

        #region Category Accessors

        public bool IsCool { get => GetCat(Category.Cool); set => SetCat(Category.Cool, value); }
        public bool IsWeird { get => GetCat(Category.Weird); set => SetCat(Category.Weird, value); }
        public bool IsClever { get => GetCat(Category.Clever); set => SetCat(Category.Clever, value); }
        public bool IsFunny { get => GetCat(Category.Funny); set => SetCat(Category.Funny, value); }
        public bool IsAwesome { get => GetCat(Category.Awesome); set => SetCat(Category.Awesome, value); }
        public bool IsCreepy { get => GetCat(Category.Creepy); set => SetCat(Category.Creepy, value); }
        public bool IsCute { get => GetCat(Category.Cute); set => SetCat(Category.Cute, value); }
        public bool IsNoob { get => GetCat(Category.Noob); set => SetCat(Category.Noob, value); }
        public bool IsGross { get => GetCat(Category.Gross); set => SetCat(Category.Gross, value); }
        public bool IsNaughty { get => GetCat(Category.Naughty); set => SetCat(Category.Naughty, value); }
        public bool IsWild { get => GetCat(Category.Wild); set => SetCat(Category.Wild, value); }
        public bool IsAspiring { get => GetCat(Category.Aspiring); set => SetCat(Category.Aspiring, value); }
        public bool IsNerdy { get => GetCat(Category.Nerdy); set => SetCat(Category.Nerdy, value); }

        private bool GetCat(Category cat) => SelectedTag?.CategoriesContains(cat) ?? false;

        private void SetCat(Category cat, bool value)
        {
            if (SelectedTag == null) return;
            var mask = SelectedTag.Tag.CategoryMask;
            if (value) mask.SetBit((int)cat);
            else mask.Mask &= (ushort)~(1 << (int)cat);

            SelectedTag.CategoryMask = mask;
        }

        private void NotifyAllCategoriesChanged()
        {
            OnPropertyChanged(nameof(IsCool)); OnPropertyChanged(nameof(IsWeird)); OnPropertyChanged(nameof(IsClever));
            OnPropertyChanged(nameof(IsFunny)); OnPropertyChanged(nameof(IsAwesome)); OnPropertyChanged(nameof(IsCreepy));
            OnPropertyChanged(nameof(IsCute)); OnPropertyChanged(nameof(IsNoob)); OnPropertyChanged(nameof(IsGross));
            OnPropertyChanged(nameof(IsNaughty)); OnPropertyChanged(nameof(IsWild)); OnPropertyChanged(nameof(IsAspiring));
            OnPropertyChanged(nameof(IsNerdy));
        }
        #endregion

        #region Revert Logic

        public void RevertSelectedToSession()
        {
            if (SelectedTag == null) return;
            _isSyncing = true;
            try
            {
                SelectedTag.RevertLocalOverrides();
                SelectedTag.CategoryMask = _initialCategoryMasks[SelectedTag.Index];
                SelectedTag.Rarity = BaseSubsToRarityString(_initialBaseSubs[SelectedTag.Index]);

                // Reset bits for this specific tag as it matches session start state
                ClearBitInTagMask(ref _dirtyCosmeticMask, SelectedTag.Index);
                ClearBitInTagMask(ref _dirtyStructuralMask, SelectedTag.Index);
                UpdateDirtyState(SelectedTag);
                NotifyAllCategoriesChanged();
                Validate();
                RefreshChangeIndicators();
            }
            finally { _isSyncing = false; }
        }

        public void RevertAllToSession()
        {
            _isSyncing = true;
            try
            {
                _dirtyCosmeticMask = TagMask.Empty;
                _dirtyStructuralMask = TagMask.Empty;
                foreach (var vm in Tags)
                {
                    vm.RevertLocalOverrides();
                    vm.CategoryMask = _initialCategoryMasks[vm.Index];
                    vm.Rarity = BaseSubsToRarityString(_initialBaseSubs[vm.Index]);
                    UpdateDirtyState(vm);
                }

                

                NotifyAllCategoriesChanged();
                Validate();
                RefreshChangeIndicators();
            }
            finally { _isSyncing = false; }
        }

        public void RevertSelectedToDefault()
        {
            if (SelectedTag == null) return;
            _isSyncing = true;
            try
            {
                int idx = SelectedTag.Index;

                // 1. Reset Cosmetic (Metadata) using existing TagMetadata fields
                SelectedTag.Name = TagMetadata.BaseNames[idx];
                SelectedTag.Description = TagMetadata.BaseDescriptions[idx];
                SelectedTag.IsControversial = TagMetadata.BaseControversial.IsSet(idx);
                SelectedTag.IsStoryMission = TagMetadata.BaseStoryMission.IsSet(idx);

                // 2. Reset Structural (Rarity & Categories)
                SelectedTag.CategoryMask = _factoryDefaults[idx].CategoryMask;
                SelectedTag.Rarity = BaseSubsToRarityString(_factoryDefaults[idx].BaseSubs);

                // Update dirty state to see if factory state differs from session start state
                UpdateDirtyState(SelectedTag);

                NotifyAllCategoriesChanged();
                Validate();
                RefreshChangeIndicators();
            }
            finally { _isSyncing = false; }
        }

        public void RevertAllToDefault()
        {
            _isSyncing = true;
            try
            {
                foreach (var vm in Tags)
                {
                    int idx = vm.Index;
                    vm.Name = TagMetadata.BaseNames[idx];
                    vm.Description = TagMetadata.BaseDescriptions[idx];
                    vm.IsControversial = TagMetadata.BaseControversial.IsSet(idx);
                    vm.IsStoryMission = TagMetadata.BaseStoryMission.IsSet(idx);
                    vm.CategoryMask = _factoryDefaults[idx].CategoryMask;
                    vm.Rarity = BaseSubsToRarityString(_factoryDefaults[idx].BaseSubs);

                    // Each tag must be evaluated against the initial session state
                    UpdateDirtyState(vm);
                }

                NotifyAllCategoriesChanged();
                Validate();
                RefreshChangeIndicators();
            }
            finally { _isSyncing = false; }
        }

        /// <summary>
        /// Re-evaluates dirty bits for a specific tag compared to session start snapshots.
        /// </summary>
        private void UpdateDirtyState(TagViewModel vm)
        {
            // Structural Check
            bool isStructuralDirty = vm.Tag.CategoryMask.Mask != _initialCategoryMasks[vm.Index].Mask ||
                                     vm.Tag.BaseSubs != _initialBaseSubs[vm.Index];

            if (isStructuralDirty) _dirtyStructuralMask.SetBit(vm.Index);
            else ClearBitInTagMask(ref _dirtyStructuralMask, vm.Index);

            // Cosmetic Check using TagMetadata current state (which represents session start if not yet committed)
            bool isCosmeticDirty = vm.Name != TagMetadata.GetName(vm.Index) ||
                                   vm.Description != TagMetadata.GetDescription(vm.Index) ||
                                   vm.IsControversial != TagMetadata.IsControversial(vm.Index) ||
                                   vm.IsStoryMission != TagMetadata.IsStoryMission(vm.Index);

            if (isCosmeticDirty) _dirtyCosmeticMask.SetBit(vm.Index);
            else ClearBitInTagMask(ref _dirtyCosmeticMask, vm.Index);
            vm.IsDirty = isStructuralDirty || isCosmeticDirty;
        }

        /// <summary>
        /// Refreshes UI properties and command states.
        /// </summary>
        private void RefreshChangeIndicators()
        {
            OnPropertyChanged(nameof(HasPendingChanges));
            OnPropertyChanged(nameof(HasPendingStructuralChanges));
            OnPropertyChanged(nameof(HasValidationError));
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }
        private string BaseSubsToRarityString(int subs) => subs switch
        {
            5 => "Common",
            15 => "Uncommon",
            45 => "Rare",
            135 => "Epic",
            405 => "Viral",
            _ => "Common"
        };

        #endregion

        private void Validate()
        {
            string? currentError = null;

            if (Tags.Any(t => string.IsNullOrWhiteSpace(t.Name) || string.IsNullOrWhiteSpace(t.Description)))
                currentError = "Names and Descriptions are required.";
            else if (Tags.Any(t => t.Tag.CategoryMask.Mask == 0))
                currentError = "At least one category is required per tag.";
            

            if (ValidationError != currentError)
            {
                ValidationError = currentError;
                OnPropertyChanged(nameof(ValidationError));
                OnPropertyChanged(nameof(HasValidationError));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public async Task ExecuteCommit()
        {
            if (ValidationError != null) return;
            bool needsRecalc = HasPendingStructuralChanges;
            IsBusy = needsRecalc;
            
            try
            {
                await Task.Run(() =>
                {
                    for (int i = 0; i < 512; i++)
                    {
                        if (!_dirtyCosmeticMask.IsSet(i) && !_dirtyStructuralMask.IsSet(i)) continue;

                        var vm = Tags.FirstOrDefault(t => t.Index == i);
                        if (vm == null) continue;

                        TagMetadata.SetNameOverride(vm.Index, vm.Name.Trim());
                        TagMetadata.SetDescriptionOverride(vm.Index, vm.Description.Replace("\r\n", " ").Replace("\n", " ").Trim());
                        TagMetadata.SetControversial(vm.Index, vm.IsControversial);
                        TagMetadata.SetStoryMission(vm.Index, vm.IsStoryMission);
                        
                    }

                    if (needsRecalc)
                    {
                        
                        var results = ComboController.ComputeAllMaxPotentialScores(Tags.Select(t => t.Tag).ToList(), default);
                        TagController.SaveCalculationResults(results);
                    }
                    else
                    {
                        TagController.SaveCalculationResults(Tags.Select(t => t.Tag).ToList());
                    }

                    TagController.Initialize();
                });
                foreach (var t in Tags) t.IsDirty = false;
                IsBusy = false;
                
                ChoiceDialog.Show(needsRecalc ? "Scores recalculated and saved." : "Changes saved successfully.", "Excellent");

                // Update snapshots
                _dirtyCosmeticMask = TagMask.Empty;
                _dirtyStructuralMask = TagMask.Empty;
                foreach (var t in Tags)
                {
                    _initialCategoryMasks[t.Index] = t.Tag.CategoryMask;
                    _initialBaseSubs[t.Index] = t.Tag.BaseSubs;
                }
                OnPropertyChanged(nameof(HasPendingChanges));
                OnPropertyChanged(nameof(HasPendingStructuralChanges));
            }
            catch (Exception ex)
            {
                ChoiceDialog.Show($"Save error: {ex.Message}", "Close", isDanger: true);
            }
            finally { IsBusy = false; }
        }

        private void ClearBitInTagMask(ref TagMask mask, int index)
        {
            if (index < 64) { mask.A &= ~(1UL << index); return; }
            if (index < 128) { mask.B &= ~(1UL << (index - 64)); return; }
            if (index < 192) { mask.C &= ~(1UL << (index - 128)); return; }
            if (index < 256) { mask.D &= ~(1UL << (index - 192)); return; }
            if (index < 320) { mask.E &= ~(1UL << (index - 256)); return; }
            if (index < 384) { mask.F &= ~(1UL << (index - 320)); return; }
            if (index < 448) { mask.G &= ~(1UL << (index - 384)); return; }
            if (index < 512) { mask.H &= ~(1UL << (index - 448)); return; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}