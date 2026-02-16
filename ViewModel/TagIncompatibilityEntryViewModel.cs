using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Encapsulates a relationship between the selected master tag and a target tag.
    /// Tracks three levels of state: Core (factory), Saved (initial session), and Current (UI).
    /// </summary>
    public sealed class TagIncompatibilityEntryViewModel : INotifyPropertyChanged
    {
        private bool _currentIncompatible;
        private readonly bool _initialSessionIncompatible;
        private readonly bool _coreDefaultIncompatible;

        /// <summary>
        /// Gets the metadata-rich ViewModel of the target tag.
        /// </summary>
        public TagViewModel Target { get; }

        /// <summary>
        /// Gets or sets the incompatibility state currently manipulated in the UI.
        /// </summary>
        public bool IsIncompatible
        {
            get => _currentIncompatible;
            set
            {
                if (_currentIncompatible == value) return;
                _currentIncompatible = value;
                NotifyStateChanges();
            }
        }

        /// <summary>
        /// Returns true if the current state differs from the state at session start.
        /// Used to display the pending change indicator (*).
        /// </summary>
        public bool IsPendingChange => _currentIncompatible != _initialSessionIncompatible;

        /// <summary>
        /// Returns true if the current state differs from the factory default (tags.json).
        /// </summary>
        public bool IsDifferentFromDefault => _currentIncompatible != _coreDefaultIncompatible;

        /// <summary>
        /// Provides a localized string for the status column, including the pending marker.
        /// </summary>
        public string StatusDisplay => $"{(IsIncompatible ? "Incompatible" : "Compatible")}{(IsPendingChange ? " *" : "")}";

        /// <summary>
        /// Initializes a new relationship entry.
        /// </summary>
        /// <param name="target">The ViewModel of the target tag.</param>
        /// <param name="sessionStart">The state loaded when the T.I.M. window opened.</param>
        /// <param name="coreDefault">The absolute factory default state.</param>
        public TagIncompatibilityEntryViewModel(TagViewModel target, bool sessionStart, bool coreDefault)
        {
            Target = target;
            _currentIncompatible = sessionStart;
            _initialSessionIncompatible = sessionStart;
            _coreDefaultIncompatible = coreDefault;
        }

        /// <summary>
        /// Resets the current state to the value it had when the window was opened.
        /// </summary>
        public void RevertToSessionStart() => IsIncompatible = _initialSessionIncompatible;

        /// <summary>
        /// Resets the current state to the factory default value.
        /// </summary>
        public void RevertToDefault() => IsIncompatible = _coreDefaultIncompatible;

        private void NotifyStateChanges()
        {
            OnPropertyChanged(nameof(IsIncompatible));
            OnPropertyChanged(nameof(IsPendingChange));
            OnPropertyChanged(nameof(IsDifferentFromDefault));
            OnPropertyChanged(nameof(StatusDisplay));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}