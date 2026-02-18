using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Orchestrates the main application state, tag filtering, and solver execution.
    /// Manages the interaction between the user interface and the core computation engine.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Fields and State
        private Window _timWindow;
        private Window _tamWindow;
        private string _computationStatus = "Solver computing, please wait, it may take awhile...";
        /// <summary>
        /// Gets or sets the status message displayed on the UI overlay during computation.
        /// </summary>
        public string ComputationStatus
        {
            get => _computationStatus;
            set
            {
                if (_computationStatus != value)
                {
                    _computationStatus = value;
                    OnPropertyChanged(nameof(ComputationStatus));
                }
            }
        }
        private LoggerViewModel _currentLogger;

        /// <summary>
        /// Gets or sets the logger for the current computation session.
        /// Exposed so the LogWindow can bind to it.
        /// </summary>
        public LoggerViewModel CurrentLogger
        {
            get => _currentLogger;
            private set
            {
                _currentLogger = value;
                OnPropertyChanged(); // Crucial for binding the window's DataContext
            }
        }
        private bool _isBusy;
        private string _searchText = string.Empty;
        private bool _isRecalculating;
        private CancellationTokenSource? _cts;
        /// <summary>
        /// Core timing engine used for performance metrics and log timestamping.
        /// Shared across the UI and computation tasks for temporal consistency.
        /// </summary>
        private readonly Stopwatch _engineStopwatch = new Stopwatch();
        // Configuration
        private String _maxResult = "50";
        private int _comboSize = 5;
        private int _resultCount = 50;
        private bool _isDisjointMode;
        private bool _includeControversial = true;
        private bool _includeStoryMission = true;

        #endregion

        #region Collections and Views

        /// <summary>
        /// Gets the complete list of tag view models.
        /// </summary>
        public ObservableCollection<TagViewModel> AllTags { get; set; }

        /// <summary>
        /// Gets the filtered view of tags for display in the main list.
        /// </summary>
        public ICollectionView FilteredTagsView { get; set; }

        /// <summary>
        /// Gets the selectors used for batch-toggling rarities.
        /// </summary>
        public ObservableCollection<SelectableEnum<Rarity>> RarityBatchSelectors { get; }

        /// <summary>
        /// Gets the selectors used for batch-toggling categories.
        /// </summary>
        public ObservableCollection<SelectableEnum<Category>> CategoryBatchSelectors { get; }

        /// <summary>
        /// Returns the count of currently included tags.
        /// </summary>
        public int SelectedTagsCount => AllTags.Count(t => t.Include);

        #endregion

        #region UI State Properties

        /// <summary>
        /// Gets a value indicating whether the result count can be manually edited.
        /// Disabled when Disjoint Mode is active as it's hardcoded to 10.
        /// </summary>
        public bool CanEditResultCount => !IsDisjointMode && !IsBusy;

        /// <summary>
        /// Gets a value indicating whether general filters and settings can be modified.
        /// Disabled while the solver is running.
        /// </summary>
        public bool IsInteractionEnabled => !IsBusy;

        #endregion

        #region Commands
        public ICommand OpenTimCommand { get; }
        public ICommand OpenTamCommand { get; }
        public ICommand ComputeCommand { get; }
        public ICommand SelectAllVisibleCommand { get; }
        public ICommand DeselectAllVisibleCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ToggleLogCommand { get; }
        public ICommand OpenAboutCommand { get; }
        #endregion

        #region Events

        /// <summary>
        /// Triggered when a calculation produces a valid result record.
        /// </summary>
        public event Action<CalculationResultPackage> OnCalculationCompleted;

        #endregion

        public MainViewModel()
        {
            // 1. Initialize Batch Selectors (Static logic)
            RarityBatchSelectors = InitializeEnumSelectors<Rarity>((val, selected) => ApplyBatchRarity(val, selected));
            CategoryBatchSelectors = InitializeEnumSelectors<Category>((val, selected) => ApplyBatchCategory(val, selected), "None");

            // 2. Initialize Commands (Static logic)
            ComputeCommand = new RelayCommand(async _ => await HandleComputeAction());
            SelectAllVisibleCommand = new RelayCommand(_ => BatchSetVisibleSelection(true));
            DeselectAllVisibleCommand = new RelayCommand(_ => BatchSetVisibleSelection(false));
            ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
            ToggleLogCommand = new RelayCommand(_ => ToggleLogWindow());
            OpenTimCommand = new RelayCommand(_ => ExecuteOpenTim(), _ => IsInteractionEnabled);
            OpenTamCommand = new RelayCommand(_ => ExecuteOpenTam(), _ => IsInteractionEnabled);
            OpenAboutCommand = new RelayCommand(_ => ShowAbout_Click());
            // 3. Subscribe to External Updates (Le lien magique avec TIM)
            // Dès que le TagController crie "Updated!", on recharge.
            TagController.OnDataRefreshed += OnExternalDataRefresh;

            // 4. Initial Data Load
            LoadData();
        }
        /// <summary>
        /// Handles the broadcast from TagController when data (scores/rules) changes.
        /// </summary>
        private void OnExternalDataRefresh()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                LoadData();
            });
        }
        private void LoadData()
        {
            // A. Fetch fresh data from Controller
            var rawTags = TagController.Tags; 

            // B. Rebuild ViewModels
            var viewModels = rawTags.Select(t => new TagViewModel(t)).ToList();

            // C. Wire up PropertyChanged events (Selection counting)
            foreach (var tag in viewModels)
            {
                tag.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(TagViewModel.Include))
                        OnPropertyChanged(nameof(SelectedTagsCount));
                };
            }

            // D. Update ObservableCollection
            
            AllTags = new ObservableCollection<TagViewModel>(viewModels);
            OnPropertyChanged(nameof(AllTags));

            // E. Reconfigure Collection View (Critical step)
            
            FilteredTagsView = CollectionViewSource.GetDefaultView(AllTags);
            FilteredTagsView.Filter = FilterTagsPredicate;

            
            OnPropertyChanged(nameof(FilteredTagsView));

            // F. Update Dependent Properties
            OnPropertyChanged(nameof(SelectedTagsCount));
        }
        #region Public Properties

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetField(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsInteractionEnabled));
                    OnPropertyChanged(nameof(CanEditResultCount));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        /// <summary>
        /// Proxy property bound to the UI TextBox. Handles validation, 
        /// null/empty strings, and boundary enforcement (1-1000).
        /// </summary>
        public string MaxResult
        {
            get => ResultCount.ToString();
            set
            {
                int validatedValue;

                if (string.IsNullOrWhiteSpace(value))
                {
                    validatedValue = 50;
                }
                else if (int.TryParse(value, out int parsed))
                {
                    // Enforce boundaries: [1, 1000]
                    validatedValue = parsed < 1 ? 1 : (parsed > 1000 ? 1000 : parsed);
                }
                else
                {
                    validatedValue = 50;
                }

                // Update the actual integer property
                ResultCount = validatedValue;
            }
        }
        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); FilteredTagsView.Refresh(); }
        }

        public int ComboSize
        {
            get => _comboSize;
            set { if (value >= 2 && value <= 5) { _comboSize = value; OnPropertyChanged(); } }
        }

        public int ResultCount
        {
            get => _resultCount;
            set
            {
                if (_resultCount != value)
                {
                    _resultCount = value;
                    OnPropertyChanged(nameof(ResultCount));
                    // Synchronize the string proxy for the UI
                    OnPropertyChanged(nameof(MaxResult));
                }
            }
        }

        public bool IsDisjointMode
        {
            get => _isDisjointMode;
            set
            {
                if (SetField(ref _isDisjointMode, value))
                {
                    if (value) ResultCount = 10; // Force 10 for disjoint sets

                    // Notify UI that editability state has changed
                    OnPropertyChanged(nameof(CanEditResultCount));
                    OnPropertyChanged(nameof(IsDisjointMode));
                }
            }
        }

        public bool IncludeControversial
        {
            get => _includeControversial;
            set { if (SetField(ref _includeControversial, value)) ApplyBatchSpecial(t => t.IsControversial, value); }
        }

        public bool IncludeStoryMission
        {
            get => _includeStoryMission;
            set { if (SetField(ref _includeStoryMission, value)) ApplyBatchSpecial(t => t.IsStoryMission, value); }
        }

        #endregion

        #region Solver Orchestration

        /// <summary>
        /// Handles the primary action of the Compute button (Start or Cancel).
        /// </summary>
        private async Task HandleComputeAction()
        {
            if(!IsBusy)ComputationStatus = "The solver is preparing the computation";
            if (IsBusy)
            {
                // Security check to prevent accidental loss of computation progress
                string msg = "Are you sure to want to abort the current computation? All computation progress will be lost.";

                bool result = ChoiceDialog.Show(msg, "Abort", "Nevermind", isDanger: true)==ChoiceResult.Primary;

                if (!result)
                {
                    return; // Return to the orbital loop
                }

                // Proceed with the abort logic
                ComputationStatus = "ABORTING COMMAND...";
                _cts?.Cancel();
                return;
            }
            await RunSolverSequence();
        }

        /// <summary>
        /// Orchestrates the full solver workflow: hashing, record checking, and execution.
        /// </summary>
        private async Task RunSolverSequence()
        {
            IsBusy = true;
            _cts = new CancellationTokenSource();

            // 1. Prepare infrastructure (Not yet timing the computation)
            CurrentLogger = new LoggerViewModel(_engineStopwatch, IsDisjointMode);
            

            try
            {
                // 2. Identify current configuration
                var activePool = GetActivePool();
                var universeMask = ComputeUniverseMask(activePool);
                var maskData = ComputationRecord.TagMaskToData(universeMask);
                string stateHash = TagController.GetGlobalEngineStateHash();

                string configHash = ComputationRecord.GenerateHash(
                    maskData,
                    IsDisjointMode ? 10 : ResultCount,
                    ComboSize,
                    stateHash);

                // 3. Existing record check (Blocking user prompt doesn't affect calculation time)
                ComputationRecord existingRecord = null;
                if (IsDisjointMode)
                {
                    existingRecord = ComboController.GetRecord(configHash);

                    if (existingRecord != null)
                    {
                        if (!PromptForRecompute(existingRecord))
                        {
                            // If the user wants to keep the old one, we bail here.
                            OnCalculationCompleted?.Invoke(new CalculationResultPackage { Record = existingRecord });
                            return;
                        }
                    }
                }

                // 4. Define the asynchronous logging action
                Action<LogEventId, long> logger = (id, val) => CurrentLogger.LogEngineEvent(id, val);

                ComputationStatus = "The solver is computing, please wait, it may take awhile...";

                // 5. Execution timing start
                _engineStopwatch.Restart();
                logger(LogEventId.EngineStarted, activePool.Count);

                List<Combo> results = await Task.Run(() =>
                    ExecuteComputation(activePool, _cts.Token, logger), _cts.Token);

                _engineStopwatch.Stop();

                // 6. Record Handling & Telemetry handoff
                if (results != null && results.Count > 0)
                {
                    var finalRecord = ProcessFinalResults(configHash, maskData, results, _engineStopwatch.Elapsed, existingRecord);
                    var logs = IsDisjointMode ? CurrentLogger.GetSnapshot() : null;

                    var package = new CalculationResultPackage
                    {
                        Record = finalRecord,
                        SessionLogs = logs
                    };

                    OnCalculationCompleted?.Invoke(package);


                    
                }
            }
            catch (OperationCanceledException) { /* Handled silently by UI state */ }
            catch (Exception ex)
            {
                var realError = ex.InnerException ?? ex;
                CurrentLogger.LogEngineEvent(0,realError.Message);
            }
            finally
            {
                if (CurrentLogger != null)
                {
                    await CurrentLogger.FinalizeLogging();
                }
                // Close and nullify the log window to ensure a clean state for the next run.
                if (_logWindow != null)
                {
                    // Unsubscribe to allow the actual Close() to proceed.
                    _logWindow.Closing -= (s, e) => { e.Cancel = true; _logWindow.Hide(); };
                    _logWindow.Close();
                    _logWindow = null;
                }
                IsBusy = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Calls the appropriate solver method based on current settings.
        /// </summary>
        private List<Combo> ExecuteComputation(List<Tag> pool, CancellationToken token, Action<LogEventId, long> logger)
        {
            

            if (IsDisjointMode)
                return ComboController.ComputeBestDisjointLoadout(token, logger, pool, 10, ComboSize);

            return ComboController.ComputeBestLoadout(token, pool, ResultCount, ComboSize);
        }

        #endregion

        #region Helper Methods

        private List<Tag> GetActivePool()
        {
            var pool = AllTags.Where(t => t.Include).Select(t => t.Tag).ToList();
            return pool.Count == 0 ? AllTags.Select(t => t.Tag).ToList() : pool;
        }

        private TagMask ComputeUniverseMask(List<Tag> tags)
        {
            var mask = TagMask.Empty;
            foreach (var t in tags) mask.SetBit(t.Index);
            return mask;
        }

        private bool PromptForRecompute(ComputationRecord existing)
        {
            // Technical summary of the archived run
            string message =
                $"An identical configuration was found in the archives.\n\n" +
                $"• Computed on: {existing.ComputationDate:g}\n" +
                $"• Execution time: {existing.BestComputationTime:hh\\:mm\\:ss\\.fff}\n" +
                "Would you prefer to recompute or to load this known result?";
            return ChoiceDialog.Show(message, "RECOMPUTE", "LOAD KNOWN RESULT")==ChoiceResult.Primary;            
        }

        private ComputationRecord ProcessFinalResults(string hash, ulong[] maskData, List<Combo> results, TimeSpan elapsed, ComputationRecord existing)
        {
            if (existing != null)
            {
                // Increment validation if result matches
                existing.ValidationCount++;
                if (elapsed < existing.BestComputationTime) existing.BestComputationTime = elapsed;
                ComboController.UpsertRecord(existing);
                return existing;
            }

            var newRecord = new ComputationRecord
            {
                ConfigurationHash = hash,
                UniverseMaskData = maskData,
                LoadoutSize = IsDisjointMode ? 10 : ResultCount,
                ComboSize = ComboSize,
                IsDisjoint = IsDisjointMode,
                WinningLoadout = results,
                ComputationDate = DateTime.Now,
                BestComputationTime = elapsed,
                ValidationCount = 1
            };
            if(IsDisjointMode)ComboController.UpsertRecord(newRecord);
            return newRecord;
        }

        private ObservableCollection<SelectableEnum<T>> InitializeEnumSelectors<T>(Action<T, bool> onChanged, string excludeName = null) where T : Enum
        {
            var collection = new ObservableCollection<SelectableEnum<T>>();
            foreach (T val in Enum.GetValues(typeof(T)))
            {
                if (excludeName != null && val.ToString().Equals(excludeName, StringComparison.OrdinalIgnoreCase)) continue;
                var selector = new SelectableEnum<T>(val, true);
                selector.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(SelectableEnum<T>.IsSelected)) onChanged(selector.Value, selector.IsSelected); };
                collection.Add(selector);
            }
            return collection;
        }

        #endregion
        /// <summary>
        /// Opens the Tag Incompatibility Manager (T.I.M.).
        /// Ensures only one instance of the window is active.
        /// </summary>
        private void ExecuteOpenTim()
        {
            if (_timWindow != null && _timWindow.IsLoaded)
            {
                _timWindow.Activate();
                _timWindow.WindowState = WindowState.Normal;
                return;
            }

            // Replace 'TimWindow' with your actual class name once created
             _timWindow = new TimWindow { Owner = Application.Current.MainWindow };
             _timWindow.ShowDialog();
        }

        /// <summary>
        /// Opens the Tag Attributes Manager (T.A.M.).
        /// Ensures only one instance of the window is active.
        /// </summary>
        private void ExecuteOpenTam()
        {
            if (_tamWindow != null && _tamWindow.IsLoaded)
            {
                _tamWindow.Activate();
                _tamWindow.WindowState = WindowState.Normal;
                return;
            }

            
             _tamWindow = new TamWindow { Owner = Application.Current.MainWindow };
             _tamWindow.Show();

            
        }
        #region Filter and Batch Logic

        private bool FilterTagsPredicate(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText) || item is not TagViewModel vm) return true;
            return vm.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || vm.Index.ToString().Contains(SearchText);
        }

        private void ApplyBatchRarity(Rarity rarity, bool shouldInclude) => BatchProcess(t => t.GetRarityEnum() == rarity, shouldInclude);
        private void ApplyBatchCategory(Category category, bool shouldInclude) => BatchProcess(t => t.CategoriesContains(category), shouldInclude);
        private void ApplyBatchSpecial(Func<TagViewModel, bool> condition, bool shouldInclude) => BatchProcess(condition, shouldInclude);
        private void BatchSetVisibleSelection(bool shouldInclude) => BatchProcess(_ => true, shouldInclude, true);

        private void BatchProcess(Func<TagViewModel, bool> condition, bool shouldInclude, bool visibleOnly = false)
        {
            if (_isRecalculating) return;
            _isRecalculating = true;
            try
            {
                var target = visibleOnly ? FilteredTagsView.Cast<TagViewModel>() : AllTags;
                foreach (var t in target) if (condition(t)) t.Include = shouldInclude;
            }
            finally { _isRecalculating = false; }
        }

        private void ResetFilters()
        {
            SearchText = string.Empty;
            IncludeControversial = true;
            IncludeStoryMission = true;
            foreach (var r in RarityBatchSelectors) r.IsSelected = true;
            foreach (var c in CategoryBatchSelectors) c.IsSelected = true;
            ComboSize = 5;
            ResultCount = 50;
            IsDisjointMode = false;
        }

        #endregion
        private LogWindow _logWindow;
        private bool _isLogVisible;
        public bool IsLogVisible
        {
            get => _isLogVisible;
            set { _isLogVisible = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// Toggles the visibility of the computation log window.
        /// Handles lazy initialization and prevents object disposal on close.
        /// </summary>
        private void ToggleLogWindow()
        {
            var owner = System.Windows.Application.Current.MainWindow;

            if (_logWindow != null && _logWindow.IsVisible)
            {
                _logWindow.Hide();
                IsLogVisible = false;

                // Unsubscribe when hidden to save resources
                if (owner != null) owner.LocationChanged -= OnMainWindowLocationChanged;
                return;
            }

            if (_logWindow == null)
            {
                _logWindow = new LogWindow
                {
                    DataContext = this.CurrentLogger,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.Manual
                };

                _logWindow.Closing += (s, e) =>
                {
                    e.Cancel = true;
                    _logWindow.Hide();
                    IsLogVisible = false;
                    if (owner != null) owner.LocationChanged -= OnMainWindowLocationChanged;
                };
            }
            else
            {
                _logWindow.DataContext = this.CurrentLogger;
            }

            // Subscribe to follow the main window's movement
            if (owner != null)
            {
                owner.LocationChanged += OnMainWindowLocationChanged;
                UpdateLogWindowPosition(owner);
            }

            _logWindow.Show();
            IsLogVisible = true;
        }

        /// <summary>
        /// Updates the LogWindow position relative to the MainWindow.
        /// Aligns to the left edge with a 50px offset.
        /// </summary>
        private void UpdateLogWindowPosition(Window owner)
        {
            if (_logWindow == null || owner == null) return;

            Point locationFromScreen = owner.PointToScreen(new Point(0, 0));
            _logWindow.Left = owner.Left + 50;

            _logWindow.Left = locationFromScreen.X + 50;
            _logWindow.Top = locationFromScreen.Y + 50;
        }
        private void ShowAbout_Click()
        {
            var about = new AboutWindow();
            about.Owner = System.Windows.Application.Current.MainWindow;
            about.ShowDialog();
        }
        /// <summary>
        /// Event handler to ensure the log window follows the parent window.
        /// </summary>
        private void OnMainWindowLocationChanged(object sender, EventArgs e)
        {
            if (sender is Window owner)
            {
                UpdateLogWindowPosition(owner);
            }
        }
        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

    }
}