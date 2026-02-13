using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            this.DataContext = vm;

            // Subscribe to the event to open the separate Results Window
            vm.OnCalculationCompleted += (package) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    
                    var resultWindow = new ResultsWindow(package);
                    resultWindow.Owner = this;
                    resultWindow.Show();
                });
            };
        }
        /// <summary>
        /// Validates text input to ensure only numeric characters are accepted.
        /// Utilizes a regular expression to match non-digit characters and marks the event as handled if found.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The TextCompositionEventArgs containing the input text.</param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Regex "[^0-9]+" matches any character that is NOT a digit between 0 and 9.
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
        /// <summary>
        /// Validates pasted content. If the content contains non-numeric characters, the paste operation is canceled.
        /// </summary>
        /// <param name="sender">The object where the command is being executed.</param>
        /// <param name="e">The DataObjectPastingEventArgs containing the data to be pasted.</param>
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        /// <summary>
        /// Helper method to verify if a string consists strictly of numeric digits.
        /// </summary>
        /// <param name="text">The string to validate.</param>
        /// <returns>True if the string is numeric; otherwise, false.</returns>
        private static bool IsTextAllowed(string text)
        {
            return Regex.IsMatch(text, "^[0-9]*$");
        }
    }
}