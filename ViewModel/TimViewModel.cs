using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Windows.Data;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Controller for the Tag Incompatibility Manager session.
    /// Handles dual-list filtering, symmetry enforcement, and multi-level resets.
    /// </summary>
    public class TimViewModel : INotifyPropertyChanged
    {
        private  List<TagViewModel> _allTagViewModels;
        private readonly List<Tag> _coreDefaults;
        private readonly Dictionary<(int, int), bool> _sessionMatrix = new();

        private TagViewModel? _selectedMasterTag;
        private string _searchTextLeft = string.Empty;
        private string _searchTextRight = string.Empty;
        private bool _hasPendingChanges;

        private readonly ICollectionView _masterView;
        private readonly ICollectionView _relationView;
        private FilterState _currentFilterState = FilterState.ShowAll;

        /// <summary>
        /// Gets or sets the active filter mode.
        /// Changing this property triggers a refresh of the relationship grid view.
        /// </summary>
        public FilterState CurrentFilterState
        {
            get => _currentFilterState;
            set
            {
                if (_currentFilterState == value) return;
                _currentFilterState = value;
                OnPropertyChanged();

                // Refresh the view immediately when the state changes.
                _relationView.Refresh();
            }
        }
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
               
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public ObservableCollection<TagViewModel> MasterTags { get; }
        public ObservableCollection<TagIncompatibilityEntryViewModel> RelationGrid { get; } = new();

        public ICommand ToggleCommand { get; }
        public ICommand CommitCommand { get; }
        public ICommand ResetAllToDefaultCommand { get; }
        public ICommand DismissAllChangesCommand { get; }
        public ICommand RevertTagToDefaultCommand { get; }
        public ICommand RevertTagToSavedCommand { get; }
        public ICommand ChangeFilterCommand { get; }
        /// <summary>
        /// Defines the available filtering modes for the relationship grid.
        /// </summary>
        public enum FilterState
        {
            ShowAll,
            OnlyCompatible,
            OnlyIncompatible,
            OnlyPending
        }
        /// <summary>
        /// Initializes the T.I.M. session using current and default tag pools.
        /// </summary>
        public TimViewModel(Action closeAction)
        {
            var tags = TagController.Tags;
            List<TagViewModel> activePool = new();
            foreach (var tag in tags)
            {
                activePool.Add(new TagViewModel(tag));
            }
            _allTagViewModels = activePool;
            _coreDefaults = TagController.GetDefaultTags();
            MasterTags = new ObservableCollection<TagViewModel>(activePool);

            _masterView = CollectionViewSource.GetDefaultView(MasterTags);
            _masterView.Filter = FilterMaster;

            _relationView = CollectionViewSource.GetDefaultView(RelationGrid);
            _relationView.Filter = FilterRelation;

            InitializeSessionMatrix();
             ToggleCommand = new RelayCommand(id => ExecuteToggle((int)id));
             CommitCommand = new RelayCommand(_ => ExecuteCommit(), _ => HasPendingChanges);
             ResetAllToDefaultCommand = new RelayCommand(_ => ExecuteResetAllToDefault());
             DismissAllChangesCommand = new RelayCommand(_ => ExecuteDismissAllChanges());
             RevertTagToDefaultCommand = new RelayCommand(_ => ExecuteRevertTagToDefault());
             RevertTagToSavedCommand = new RelayCommand(_ => ExecuteRevertTagToSaved());
            ChangeFilterCommand = new RelayCommand(param => ExecuteChangeFilter(param));
        }
        /// <summary>
        /// Updates the filter state for the relationship grid based on the UI selection.
        /// </summary>
        /// <param name="filterType">The string identifier of the filter mode (e.g., "OnlyCompatible").</param>
        private void ExecuteChangeFilter(object? parameter)
        {
            if (parameter is not string filterType) return;

            switch (filterType)
            {
                case "ShowAll":
                    CurrentFilterState = FilterState.ShowAll;
                    break;
                case "OnlyCompatible":
                    CurrentFilterState = FilterState.OnlyCompatible;
                    break;
                case "OnlyIncompatible":
                    CurrentFilterState = FilterState.OnlyIncompatible;
                    break;
                case "OnlyPending":
                    CurrentFilterState = FilterState.OnlyPending;
                    break;
            }

            // Trigger a refresh of the CollectionView to apply the new predicate.
            _relationView.Refresh();
        }
        /// <summary>
        /// Ensures symmetry by using a sorted ID tuple as a key.
        /// </summary>
        private void InitializeSessionMatrix()
        {
            foreach (var master in _allTagViewModels)
            {
                foreach (var target in _allTagViewModels)
                {
                    if (master.Index >= target.Index) continue;
                    var key = (master.Index, target.Index);
                    _sessionMatrix[key] = master.Tag.IncompatibilityMask.IsSet(target.Index);
                }
            }
        }
        
        /// <summary>
        /// Refreshes the right grid based on the selected master tag.
        /// Cross-references session matrix with core defaults.
        /// </summary>
        private void RefreshRelationGrid()
        {
            if (SelectedMasterTag == null) return;

            RelationGrid.Clear();

            // Retrieve the immutable factory definition for the selected master tag to serve as the baseline for "Core" comparisons.
            var coreMaster = _coreDefaults.First(t => t.Index == SelectedMasterTag.Index);

            foreach (var target in _allTagViewModels)
            {
                // Skip self-reference.
                if (target.Index == SelectedMasterTag.Index) continue;

                // Generate the symmetric key to access the current session state.
                var key = (Math.Min(SelectedMasterTag.Index, target.Index),
                           Math.Max(SelectedMasterTag.Index, target.Index));

                // 1. Current State: The value currently held in the dirty matrix (potentially modified by the user).
                bool currentMatrixState = _sessionMatrix[key];

                // 2. Initial State: The value as it existed when the window was opened (stored in the immutable Tag struct).
                bool sessionStartState = SelectedMasterTag.Tag.IncompatibilityMask.IsSet(target.Index);

                // 3. Core State: The hardcoded factory default from tags.json.
                bool coreState = coreMaster.IncompatibilityMask.IsSet(target.Index);

                // Initialize the ViewModel with the baseline states (Start and Core).
                var entry = new TagIncompatibilityEntryViewModel(target, sessionStartState, coreState);

                // Apply the current matrix state.
                // If currentMatrixState differs from sessionStartState, the ViewModel will automatically calculate IsPendingChange as true.
                entry.IsIncompatible = currentMatrixState;

                RelationGrid.Add(entry);
            }

            // Re-apply the active filter view to ensure the grid reflects the user's display preferences immediately.
            _relationView.Refresh();
        }

        /// <summary>
        /// Gets or sets the currently selected master tag from the left list.
        /// Triggers a refresh of the relationship grid on the right.
        /// </summary>
        public TagViewModel? SelectedMasterTag
        {
            get => _selectedMasterTag;
            set
            {
                if (_selectedMasterTag == value) return;
                _selectedMasterTag = value;
                OnPropertyChanged();
                RefreshRelationGrid();
            }
        }
        /// <summary>
        /// Gets or sets the search string for the master list (left side).
        /// </summary>
        public string SearchTextLeft
        {
            get => _searchTextLeft;
            set
            {
                _searchTextLeft = value;
                OnPropertyChanged();
                _masterView.Refresh();
            }
        }
        /// <summary>
        /// Gets or sets the search string for the relation grid (right side).
        /// </summary>
        public string SearchTextRight
        {
            get => _searchTextRight;
            set
            {
                _searchTextRight = value;
                OnPropertyChanged();
                _relationView.Refresh();
            }
        }

        public bool HasPendingChanges { get => _hasPendingChanges; set {
                if (_hasPendingChanges == value) return;
                _hasPendingChanges = value;
                OnPropertyChanged();
            } }

        /// <summary>
        /// Predicate for filtering the master tag list.
        /// Filters by Index (ID) or Name.
        /// </summary>
        private bool FilterMaster(object item)
        {
            if (item is not TagViewModel tag) return false;
            if (string.IsNullOrWhiteSpace(SearchTextLeft)) return true;

            string search = SearchTextLeft.Trim();
            return tag.Index.ToString().Contains(search) ||
                   tag.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Predicate used by the ICollectionView to filter items in the right-hand grid.
        /// Combines text search and state filtering.
        /// </summary>
        private bool FilterRelation(object item)
        {
            if (item is not TagIncompatibilityEntryViewModel entry) return false;

            // 1. Apply Text Search
            bool matchesText = true;
            if (!string.IsNullOrWhiteSpace(SearchTextRight))
            {
                string search = SearchTextRight.Trim();
                matchesText = entry.Target.Index.ToString().Contains(search) ||
                              entry.Target.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
            }

            if (!matchesText) return false;

            // 2. Apply State Filter
            return CurrentFilterState switch
            {
                FilterState.OnlyCompatible => !entry.IsIncompatible,
                FilterState.OnlyIncompatible => entry.IsIncompatible,
                FilterState.OnlyPending => entry.IsPendingChange,
                _ => true // ShowAll
            };
        }
        /// <summary>
        /// Safely retrieves or generates a symmetric key for the session dictionary.
        /// </summary>
        private (int, int) GetSymmetricKey(int id1, int id2)
        {
           
            return (Math.Min(id1, id2), Math.Max(id1, id2));
        }
        /// <summary>
        /// Toggles the incompatibility state between the selected master and a target.
        /// Updates the session matrix and synchronizes the UI entry.
        /// </summary>
        private void ExecuteToggle(int targetId)
        {
            if (SelectedMasterTag == null) return;

            var key = GetSymmetricKey(SelectedMasterTag.Index, targetId);
            bool newState = !_sessionMatrix[key];
            _sessionMatrix[key] = newState;

            // Update the specific row in the current filtered view
            var entry = RelationGrid.FirstOrDefault(e => e.Target.Index == targetId);
            if (entry != null)
            {
                entry.IsIncompatible = newState;
            }

            UpdatePendingChangesStatus();
        }
        /// <summary>
        /// Exposes the commit logic as an awaitable Task for the View's lifecycle management.
        /// Wraps the internal command logic.
        /// </summary>
        public async Task CommitAsync()
        {
            if (!HasPendingChanges) return;

            
            await ExecuteCommit();
        }
        /// <summary>
        /// Executes the commit workflow:
        /// 1. Reconstructs 'Dirty' Tags from UI state.
        /// 2. Calls the static scoring engine.
        /// 3. Persists data via Controller -> Factory.
        /// 4. Hard resets the UI.
        /// </summary>
        private async Task ExecuteCommit()
        {
            if (!HasPendingChanges) return;
            IsBusy = true;
            try
            {
                await Task.Run(() =>
                {
                    var dirtyPool = new List<Tag>(_allTagViewModels.Count);

                    foreach (var masterVm in _allTagViewModels)
                    {
                        var newMask = TagMask.Empty;

                        foreach (var targetVm in _allTagViewModels)
                        {
                            if (masterVm.Index == targetVm.Index) continue;

                            var key = GetSymmetricKey(masterVm.Index, targetVm.Index);

                            if (_sessionMatrix.TryGetValue(key, out bool isIncompatible) && isIncompatible)
                            {
                                newMask.SetBit(targetVm.Index);
                            }
                        }

                        dirtyPool.Add(new Tag(
                            masterVm.Index,
                            masterVm.BaseSubs,
                            newMask,
                            masterVm.Tag.CategoryMask,
                            masterVm.Tag.MaxPotentialScore
                        ));
                    }


                    //  calculation

                    var computedPool = ComboController.ComputeAllMaxPotentialScores(
                        dirtyPool,
                        System.Threading.CancellationToken.None
                    );


                    // persist

                    TagController.SaveCalculationResults(computedPool);


                    // refresh and ui reset

                    TagController.Initialize();
                    var freshTags = TagController.Tags;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        HasPendingChanges = false;
                        SelectedMasterTag = null;
                        RelationGrid.Clear();
                        MasterTags.Clear();
                        _allTagViewModels = freshTags.Select(t => new TagViewModel(t)).ToList();
                        foreach (var tag in freshTags)
                        {
                            MasterTags.Add(new TagViewModel(tag));
                        }
                        _sessionMatrix.Clear();
                        InitializeSessionMatrix();
                    });
                });

                ChoiceDialog.Show("Configuration Saved & Scores Recalculated.", "Excellent");

                HasPendingChanges = false;
            }
            catch (Exception ex)
            {
                ChoiceDialog.Show($"Error during commit: {ex.Message}","Close",null,null,isDanger: true);
            }
            finally
            {
                IsBusy = false;
            }
        }



        /// <summary>
        /// Reverts every relationship in the entire pool to factory defaults (tags.json).
        /// </summary>
        private void ExecuteResetAllToDefault()
        {
            foreach (var master in _allTagViewModels)
            {
                var coreMaster = _coreDefaults.First(t => t.Index == master.Index);
                foreach (var target in _allTagViewModels)
                {
                    if (master.Index >= target.Index) continue;
                    var key = GetSymmetricKey(master.Index, target.Index);
                    _sessionMatrix[key] = coreMaster.IncompatibilityMask.IsSet(target.Index);
                }
            }
            RefreshRelationGrid();
            UpdatePendingChangesStatus();
        }

        /// <summary>
        /// Cancels all modifications made during this session and reverts to the last saved state.
        /// </summary>
        private void ExecuteDismissAllChanges()
        {
            // Re-initialize the matrix from the current active Tag structs
            InitializeSessionMatrix();
            RefreshRelationGrid();
            HasPendingChanges = false;
        }

        /// <summary>
        /// Reverts only the relations for the currently selected tag to factory defaults.
        /// </summary>
        private void ExecuteRevertTagToDefault()
        {
            if (SelectedMasterTag == null) return;
            var coreMaster = _coreDefaults.First(t => t.Index == SelectedMasterTag.Index);

            foreach (var entry in RelationGrid)
            {
                var key = GetSymmetricKey(SelectedMasterTag.Index, entry.Target.Index);
                bool defaultState = coreMaster.IncompatibilityMask.IsSet(entry.Target.Index);
                _sessionMatrix[key] = defaultState;
                entry.IsIncompatible = defaultState;
            }
            UpdatePendingChangesStatus();
        }

        /// <summary>
        /// Scans the matrix to determine if any pending changes exist compared to the session start.
        /// </summary>
        private void UpdatePendingChangesStatus()
        {
            bool dirty = false;
            var tagLookup = _allTagViewModels.ToDictionary(t => t.Index);
            foreach (var kvp in _sessionMatrix)
            {
                var key = kvp.Key;
                if (tagLookup.TryGetValue(key.Item1, out var masterVm))
                {
                    if (kvp.Value != masterVm.Tag.IncompatibilityMask.IsSet(key.Item2))
                    {
                        dirty = true;
                        break; 
                    }
                }
            }
            HasPendingChanges = dirty;
        }
        /// <summary>
/// Reverts all relationships for the currently selected tag to their initial session state.
/// This discards any "Pending (*)" changes made during the current session for this specific tag.
/// </summary>
private void ExecuteRevertTagToSaved()
{
    if (SelectedMasterTag == null) return;

    // We iterate through all tags to reconstruct the session start state for the selected master
    foreach (var target in _allTagViewModels)
    {
        if (target.Index == SelectedMasterTag.Index) continue;

        var key = GetSymmetricKey(SelectedMasterTag.Index, target.Index);
        
        // The "Saved" state is stored in the IncompatibilityMask of the initial TagViewModels
        bool initialSavedState = SelectedMasterTag.Tag.IncompatibilityMask.IsSet(target.Index);
        
        _sessionMatrix[key] = initialSavedState;
    }

    // Refresh the right-hand grid to reflect the rollbacked values
    RefreshRelationGrid();
    UpdatePendingChangesStatus();
}
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}