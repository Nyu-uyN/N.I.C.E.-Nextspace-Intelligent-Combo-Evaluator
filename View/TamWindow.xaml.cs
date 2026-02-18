using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.ComponentModel;
using System.Windows;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View
{
    /// <summary>
    /// Interaction logic for TamWindow.xaml.
    /// Manages the lifecycle of the Tag Attribute Manager.
    /// </summary>
    public partial class TamWindow : Window
    {
        private readonly TamViewModel _viewModel;

        public TamWindow()
        {
            InitializeComponent();

            // Initialize ViewModel and set as DataContext
            _viewModel = new TamViewModel();
            this.DataContext = _viewModel;
        }

        /// <summary>
        /// Intercepts the window closing event to check for unsaved changes.
        /// Offers the user to save, discard, or cancel the exit process.
        /// </summary>
        /// <param name="e">Cancel event arguments.</param>
        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.DataContext is TamViewModel vm)
            {
                // If no changes are pending, let the window close normally
                if (!vm.HasPendingChanges)
                {
                    base.OnClosing(e);
                    return;
                }

                // Show the triple-choice dialog to the user
                var choice = ChoiceDialog.Show(
                    "You have unsaved changes in the Tag Administration Manager.\n" +
                    "Closing without saving will lose all your modifications.\n"+
                    "Saving will trigger a recalcultation of max potential score if you modified any rarities or categories",
                    "Save & Close",   // Primary
                    "Discard Changes", // Secondary
                    "Cancel"           // Tertiary
                );

                switch (choice)
                {
                    case ChoiceResult.Primary: // SAVE & CLOSE
                                               // Abort the immediate synchronous close to allow async save
                        e.Cancel = true;

                        // Visual feedback: disable the window during the process
                        this.IsEnabled = false;

                        try
                        {
                            // Triggers the full commit and potential recalculation
                            await vm.ExecuteCommit();

                            // If commit was successful, HasPendingChanges is now false.
                            // Re-trigger Close() to exit the window.
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            // On failure, allow the user to see the error and keep the window open
                            ChoiceDialog.Show($"Critical error during exit save: {ex.Message}", "Close", isDanger: true);
                            this.IsEnabled = true;
                        }
                        break;

                    case ChoiceResult.Secondary: // DISCARD CHANGES
                                                 // e.Cancel is false by default, the window will close without further action.
                        break;

                    case ChoiceResult.Tertiary: // CANCEL
                    case ChoiceResult.None:      // Window closed or Alt+F4 on dialog
                                                 // Stop the closing process and return to the editor
                        e.Cancel = true;
                        break;
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }
    }
}