using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.Windows;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View
{
    /// <summary>
    /// Interaction logic for ChoiceDialog.xaml.
    /// Acts as a factory wrapper around the MVVM implementation.
    /// </summary>
    public partial class ChoiceDialog : Window
    {
        /// <summary>
        /// Prevents direct instantiation. Use <see cref="Show"/> method instead.
        /// </summary>
        private ChoiceDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Displays a modal dialog with 1 to 3 buttons.
        /// </summary>
        /// <param name="message">The question or message to display to the user.</param>
        /// <param name="primary">The text for the primary/right-most button (e.g., "Save", "OK", "Yes").</param>
        /// <param name="secondary">The text for the secondary/middle button (e.g., "Don't Save", "No"). Pass null to hide.</param>
        /// <param name="tertiary">The text for the tertiary/left-most button (e.g., "Cancel"). Pass null to hide.</param>
        /// <param name="isDanger">If set to <c>true</c>, the primary button uses a red warning style instead of blue.</param>
        /// <returns>The <see cref="ChoiceResult"/> corresponding to the button clicked by the user.</returns>
        public static ChoiceResult Show(string message, string primary, string? secondary = null, string? tertiary = null, bool isDanger = false)
        {
            // 1. Instantiate View
            var dialog = new ChoiceDialog();

            // 2. Instantiate ViewModel
            var vm = new ChoiceDialogViewModel(message, primary, secondary, tertiary, isDanger);

            // 3. Inject Close Action so VM can close the View
            vm.CloseAction = new System.Action(() => dialog.Close());

            // 4. Bind ViewModel
            dialog.DataContext = vm;

            // 5. Handle Ownership and Positioning
            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                dialog.Owner = Application.Current.MainWindow;
            }
            else
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // 6. Show Modal
            dialog.ShowDialog();

            // 7. Return Result from VM
            return vm.UserChoice;
        }
    }
}