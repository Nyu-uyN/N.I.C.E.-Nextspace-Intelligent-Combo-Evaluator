using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Manages the collection of telemetry logs during computation.
    /// Handles thread-safe updates and translates engine events into human-readable strings.
    /// </summary>
    public class LoggerViewModel : INotifyPropertyChanged
    {
        private readonly Stopwatch _sharedStopwatch;
        private readonly DiskLogger _diskLogger;
        private readonly ObservableCollection<LogEntry> _internalLogs = new();
        private const int MaxLogCapacity = 10000;
        private bool _isDisjointMode;
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly System.Windows.Threading.DispatcherTimer _uiRefreshTimer;
        /// <summary>
        /// Read-only collection for XAML binding.
        /// </summary>
        public ReadOnlyObservableCollection<LogEntry> Logs { get; }

        /// <summary>
        /// Gets the current elapsed time formatted for the mission clock.
        /// </summary>
        public string CurrentElapsed => _sharedStopwatch.Elapsed.ToString(@"hh\:mm\:ss\.f");

        public LoggerViewModel(Stopwatch referenceClock, bool isDisjointMode)
        {
            _isDisjointMode = isDisjointMode;
            _sharedStopwatch = referenceClock ?? throw new ArgumentNullException(nameof(referenceClock));
            _diskLogger = new DiskLogger(isDisjointMode);
            Logs = new ReadOnlyObservableCollection<LogEntry>(_internalLogs);
            _uiRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1)
            };
            _uiRefreshTimer.Tick += (s, e) => OnPropertyChanged(nameof(CurrentElapsed));
            _uiRefreshTimer.Start();
        }

        /// <summary>
        /// Dispatches an engine event to the UI logs.
        /// </summary>
        /// <summary>
        /// Captures the event, generates timestamps, and dispatches processing to the UI thread.
        /// </summary>
        /// <param name="id">Engine event identifier.</param>
        /// <param name="value">Associated numeric data.</param>
        public void LogEngineEvent(LogEventId id, long value)
        {
            
            var wallClock = DateTime.Now;
            string message = TranslateEvent(id, value);
            
            DispatchLog(wallClock, id, message);
        }
        /// <summary>
        /// Internal dispatcher that handles both UI and Disk persistence.
        /// </summary>
        private void DispatchLog(DateTime wallClock, LogEventId id, string msg)
        {
            if (_isDisjointMode)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    string ts = wallClock.ToString("HH:mm:ss.fff");

                    // Update UI
                    _internalLogs.Add(new LogEntry(ts, msg, id));
                    if (_internalLogs.Count > MaxLogCapacity) _internalLogs.RemoveAt(0);

                    // Update Disk
                    _diskLogger.WriteEntry(ts, msg);

                    OnPropertyChanged(nameof(CurrentElapsed));
                }));
            }
        }
        /// <summary>
        /// Flexible logging for string-based events (e.g., Exception messages).
        /// </summary>
        public void LogEngineEvent(LogEventId id, string message)
        {
            var wallClock = DateTime.Now;
            // We can still use TranslateEvent to prefix the message based on the ID if needed
            string formattedMessage = $"{id}: {message}";
            DispatchLog(wallClock, id, formattedMessage);
        }
        private string TranslateEvent(LogEventId id, long val) => id switch
        {
            LogEventId.EngineStarted => $"engine initialized. pool size: {val} tags.",
            LogEventId.MiningPhaseStarted => $"mining phase started. indexing top {val} combinations...",
            LogEventId.PackingPhaseStarted => $"packing phase initiated. initial greedy floor: {val}.",
            LogEventId.PoolGrowth => $"diversification required. search pool expanded to {val} candidates.",
            LogEventId.SearchDepthChanged => $"branch exploration: root index {val} reached.",
            LogEventId.NewGlobalBest => $"NEW GLOBAL BEST: score updated to {val}.",
            LogEventId.ComputationCompleted => $"optimality prooved. final optimal score identified: {val}.",
            LogEventId.ComputationAborted => $"mission terminated by user.",
            _ => $"event {id}: value {val}"
        };

        /// <summary>
        /// Flattens the current logs into an array for the result context.
        /// </summary>
        public LogEntry[] GetSnapshot() => _internalLogs.ToArray();

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        /// <summary>
        /// Ensures all resources are released and file buffers are flushed.
        /// </summary>
        public void FinalizeLogging()
        {
            _uiRefreshTimer.Stop();
            _diskLogger.Dispose();
        }
    }
}

