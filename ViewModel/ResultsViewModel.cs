using Microsoft.Win32;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// ViewModel for the Results Window.
    /// Manages the display of optimal combos, pinned selections, and synergy details.
    /// Now fully integrated with the ComputationRecord architecture.
    /// </summary>
    public class ResultsViewModel : INotifyPropertyChanged
    {
        private readonly Action _closeAction;
        private readonly ComputationRecord _record;
        public ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();
        public bool HasLogs => Logs != null && Logs.Count > 0;
        private bool _isLogVisible;
        public bool IsLogVisible
        {
            get => _isLogVisible;
            set { _isLogVisible = value; OnPropertyChanged(); }
        }
        #region UI Properties

        /// <summary>
        /// Gets the title displayed in the window header.
        /// </summary>
        public string WindowTitle { get; }

        /// <summary>
        /// Gets the formatted execution time string.
        /// </summary>
        public string ExecutionTime { get; }

        /// <summary>
        /// Gets the collection of calculated combo results.
        /// </summary>
        public ObservableCollection<ComboViewModel> Results { get; }

        /// <summary>
        /// Gets the collection of combos pinned by the user.
        /// </summary>
        public ObservableCollection<ComboViewModel> PinnedCombos { get; } = new();

        private ComboViewModel _selectedCombo;
        /// <summary>
        /// Gets or sets the currently selected combo in the results list.
        /// </summary>
        public ComboViewModel SelectedCombo
        {
            get => _selectedCombo;
            set
            {
                _selectedCombo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsComboSelected));
                // UpdateSynergyDetails() would be called here if logic is present
            }
        }

        /// <summary>
        /// Indicates if a combo is currently selected.
        /// </summary>
        public bool IsComboSelected => SelectedCombo != null;

        /// <summary>
        /// Indicates if there are any pinned combos in the list.
        /// </summary>
        public bool HasPins => PinnedCombos.Count > 0;

        /// <summary>
        /// Gets the details for visual synergy representation.
        /// </summary>
        public ObservableCollection<SynergyLine> CurrentSynergyDetails { get; } = new();

        #endregion

        #region Commands

        public ICommand PinSelectionCommand { get; }
        public ICommand RemovePinCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ToggleLogCommand { get; }
        public ICommand SaveLogCommand { get; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the ResultsViewModel using a computation record.
        /// </summary>
        /// <param name="record">The record containing results and metadata.</param>
        /// <param name="closeAction">Action to trigger window closure.</param>
        public ResultsViewModel(CalculationResultPackage package, Action closeAction)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            _record = package.Record ?? throw new ArgumentNullException(nameof(package.Record));
            _closeAction = closeAction;
            Logs.Clear();
            if (package.SessionLogs != null)
            {
                foreach (var log in package.SessionLogs)
                {
                    Logs.Add(log);
                }
            }
            OnPropertyChanged(nameof(HasLogs));
            // Map record data to existing properties
            string modeDescription = _record.IsDisjoint
            ? $"{_record.LoadoutSize} disjoint":_record.LoadoutSize.ToString();

            WindowTitle = $"Best {modeDescription} combos of {_record.ComboSize} tags";
            
            ExecutionTime = package.Record.BestComputationTime.ToString(@"hh\:mm\:ss\.fff");

            // Convert Models to ViewModels for UI binding
            var comboList = _record.WinningLoadout != null
            ? _record.WinningLoadout.Select((c, index) => new ComboViewModel(c) { Rank = index + 1 })
            : Enumerable.Empty<ComboViewModel>();

            Results = new ObservableCollection<ComboViewModel>(comboList);

            if (Results.Any()) SelectedCombo = Results[0];

            // Commands initialization
            PinSelectionCommand = new RelayCommand(_ => ExecutePinSelection());
            RemovePinCommand = new RelayCommand(param => ExecuteRemovePin(param));
            CloseCommand = new RelayCommand(_ => _closeAction?.Invoke());
            ExportCommand = new RelayCommand(_ => ExportToTextFile());
            ToggleLogCommand = new RelayCommand(_ => IsLogVisible = !IsLogVisible);
            SaveLogCommand = new RelayCommand(_ => ExecuteSaveLog());
        }

        #region Command Logic

        private void ExecutePinSelection()
        {
            if (SelectedCombo != null && !PinnedCombos.Contains(SelectedCombo))
            {
                PinnedCombos.Add(SelectedCombo);
                OnPropertyChanged(nameof(HasPins));
            }
        }

        private void ExecuteRemovePin(object param)
        {
            if (param is ComboViewModel c)
            {
                PinnedCombos.Remove(c);
                OnPropertyChanged(nameof(HasPins));
            }
        }

        /// <summary>
        /// Generates an enhanced text report including MD5 configuration hash and validation metrics.
        /// </summary>
        private void ExportToTextFile()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Text File (*.txt)|*.txt",
                FileName = $"NICE_Report_{_record.ConfigurationHash.Substring(0, 8)}_{DateTime.Now:yyyyMMdd}.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    using (var writer = new StreamWriter(dialog.FileName, false, Encoding.UTF8))
                    {
                        // We use the dynamic title directly in the report header
                        writer.WriteLine("============================================================");
                        writer.WriteLine($" N.I.C.E. REPORT - {WindowTitle}");
                        writer.WriteLine("============================================================");
                        writer.WriteLine($"Generated:     {DateTime.Now}");
                        writer.WriteLine($"Config Hash:   {_record.ConfigurationHash}");
                        writer.WriteLine($"Compute Time:  {ExecutionTime}");
                        writer.WriteLine($"Validations:   {_record.ValidationCount}");
                        writer.WriteLine("============================================================");
                        writer.WriteLine();

                        int rank = 1;
                        foreach (var combo in Results)
                        {
                            writer.WriteLine($"################# RANK {rank}  #################");
                            writer.WriteLine(combo.Model.ToString());
                            writer.WriteLine(new string('-', 40));
                            writer.WriteLine();
                            rank++;
                        }
                    }
                    MessageBox.Show("Detailed report exported successfully!", "Export Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void ExecuteSaveLog()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Log Files (*.log)|*.log", FileName = "computation.log" };
            if (dialog.ShowDialog() == true)
            {
                var content = Logs.Select(l => $"[{l.Timestamp}] {l.Message}");
                File.WriteAllLines(dialog.FileName, content);
            }
        }
        #endregion

        #region Helper Classes

        /// <summary>
        /// Represents a line of synergy data for the UI.
        /// </summary>
        public class SynergyLine
        {
            public string CategoryName { get; set; }
            public int Count { get; set; }
            public double Multiplier { get; set; }
            public string VisualBars { get; set; }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion
    }
}