using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View
{
    /// <summary>
    /// Interaction logic for ResultsWindow.xaml.
    /// Presents the final loadout and allows report generation.
    /// </summary>
    public partial class ResultsWindow : Window
    {
        /// <summary>
        /// Initializes the window with a specific computation record.
        /// </summary>
        /// <param name="record">The data record to be displayed.</param>
        public ResultsWindow(CalculationResultPackage package)
        {
            InitializeComponent();

            // Wiring the ViewModel with the record and a close delegate
            this.DataContext = new ResultsViewModel(package, this.Close);
        }

        /// <summary>
        /// Automatically numbers the rows in the results DataGrid.
        /// </summary>
        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }
    }
}