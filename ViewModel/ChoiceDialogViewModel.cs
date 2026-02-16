using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model; // Ensure RelayCommand is available here
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel
{
    /// <summary>
    /// Represents the possible outcomes of the user's interaction with the ChoiceDialog.
    /// </summary>
    public enum ChoiceResult
    {
        /// <summary>
        /// No selection made (e.g., window closed via Alt+F4).
        /// </summary>
        None,

        /// <summary>
        /// The primary action was selected (Right-most button, typically "Save", "OK", or "Yes").
        /// </summary>
        Primary,

        /// <summary>
        /// The secondary action was selected (Middle button, typically "No" or "Discard").
        /// </summary>
        Secondary,

        /// <summary>
        /// The tertiary action was selected (Left-most button, typically "Cancel").
        /// </summary>
        Tertiary
    }

    /// <summary>
    /// ViewModel backing the ChoiceDialog window.
    /// Handles button visibility logic, styling state (Danger mode), and command execution.
    /// </summary>
    public class ChoiceDialogViewModel : INotifyPropertyChanged
    {
        // --- Data Properties ---

        /// <summary>
        /// Gets the main message body to display in the dialog.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the text for the primary button (Action/Confirm).
        /// </summary>
        public string PrimaryText { get; }

        /// <summary>
        /// Gets the text for the secondary button. If null, the button is hidden.
        /// </summary>
        public string? SecondaryText { get; }

        /// <summary>
        /// Gets the text for the tertiary button. If null, the button is hidden.
        /// </summary>
        public string? TertiaryText { get; }

        /// <summary>
        /// Gets a value indicating whether the dialog represents a destructive action.
        /// When true, the primary button styling changes to red/warning colors.
        /// </summary>
        public bool IsDanger { get; }

        /// <summary>
        /// Gets the final result selected by the user.
        /// </summary>
        public ChoiceResult UserChoice { get; private set; } = ChoiceResult.None;

        // --- Computed Visibility Logic ---

        /// <summary>
        /// Gets a value indicating whether the secondary button should be visible.
        /// </summary>
        public bool HasSecondary => !string.IsNullOrEmpty(SecondaryText);

        /// <summary>
        /// Gets a value indicating whether the tertiary button should be visible.
        /// </summary>
        public bool HasTertiary => !string.IsNullOrEmpty(TertiaryText);

        // --- Keyboard Logic (Esc Key) ---

        /// <summary>
        /// Gets a value indicating whether the Tertiary button acts as the Cancel button (mapped to Esc key).
        /// Returns true if the tertiary button exists.
        /// </summary>
        public bool IsTertiaryCancel => HasTertiary;

        /// <summary>
        /// Gets a value indicating whether the Secondary button acts as the Cancel button (mapped to Esc key).
        /// Returns true only if the tertiary button is missing but the secondary exists.
        /// </summary>
        public bool IsSecondaryCancel => !HasTertiary && HasSecondary;

        // --- Commands & Actions ---

        /// <summary>
        /// Command executed when any button is clicked.
        /// Expects a <see cref="ChoiceResult"/> as a parameter.
        /// </summary>
        public ICommand ChoiceCommand { get; }

        /// <summary>
        /// Action delegate injected by the View to allow the ViewModel to close the window.
        /// </summary>
        public Action? CloseAction { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChoiceDialogViewModel"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="primary">Text for the primary button.</param>
        /// <param name="secondary">Text for the secondary button (optional).</param>
        /// <param name="tertiary">Text for the tertiary button (optional).</param>
        /// <param name="isDanger">If set to <c>true</c>, applies danger styling.</param>
        public ChoiceDialogViewModel(string message, string primary, string? secondary, string? tertiary, bool isDanger)
        {
            Message = message;
            PrimaryText = primary;
            SecondaryText = secondary;
            TertiaryText = tertiary;
            IsDanger = isDanger;

            ChoiceCommand = new RelayCommand(param =>
            {
                if (param is ChoiceResult result)
                {
                    UserChoice = result;
                    CloseAction?.Invoke();
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}