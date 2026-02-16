using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View
{
    /// <summary>
    /// Logique d'interaction pour TimWindow.xaml
    /// </summary>
    public partial class TimWindow : Window
    {
        public TimWindow()
        {
            InitializeComponent();
            this.DataContext = new TimViewModel(this.Close);
        }

        protected override  async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {

            if (this.DataContext is TimViewModel vm)
            {

                if (!vm.HasPendingChanges)
                {
                    base.OnClosing(e);
                    return;
                }


                var choice = ChoiceDialog.Show(
                    "You have unsaved changes in the Incompatibility Manager.\n" +
                    "Saving these modifications will trigger a full recalculation of potential scores.",

                    "Save & Close",      // Primary (Action)
                    "Discard Changes",   // Secondary (Destructive but safe)
                    "Cancel"             // Tertiary (Escape)
                );

                switch (choice)
                {
                    case ChoiceResult.Primary: // SAVE
                        // Stop the immediate closing to perform async work
                        e.Cancel = true;

                        // Disable UI to prevent interaction during save
                        this.IsEnabled = false;

                        try
                        {
                            await vm.CommitAsync();
                            // Once saved, close for real (HasPendingChanges will be false)
                            this.Close();
                        }
                        catch (Exception ex)
                        {
                            // If save fails, re-enable UI and stay open so user can retry or check error
                            ChoiceDialog.Show($"Save failed: {ex.Message}", "Close", isDanger: true);
                            this.IsEnabled = true;
                        }
                        break;

                    case ChoiceResult.Secondary: // DISCARD
                        // Do nothing. e.Cancel remains false. 
                        // The window will close, ignoring the changes in memory.
                        break;

                    case ChoiceResult.Tertiary: // CANCEL
                    case ChoiceResult.None:     // ALT+F4 on dialog
                        // Abort the closing process
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
