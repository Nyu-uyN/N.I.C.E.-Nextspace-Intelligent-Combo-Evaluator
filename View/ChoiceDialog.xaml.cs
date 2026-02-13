using System;
using System.Collections.Generic;
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
    public partial class ChoiceDialog : Window
    {
        public bool Result { get; private set; }

        public ChoiceDialog(string message, string confirmText, string cancelText, bool isDanger = false)
        {
            InitializeComponent();
            TxtMessage.Text = message;
            BtnConfirm.Content = confirmText;
            BtnCancel.Content = cancelText;

            if (isDanger)
            {
                // If it's a danger action (like Abort), we swap colors to Red
                BtnConfirm.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 50, 50));
                BtnConfirm.Foreground = System.Windows.Media.Brushes.Tomato;
                BtnConfirm.BorderBrush = System.Windows.Media.Brushes.Tomato;
            }
        }

        /// <summary>
        /// Static helper to call the dialog in a single line.
        /// </summary>
        public static bool Show(string message, string confirm, string cancel, bool isDanger = false)
        {
            var dialog = new ChoiceDialog(message, confirm, cancel, isDanger)
            {
                Owner = Application.Current.MainWindow
            };
            dialog.ShowDialog();
            return dialog.Result;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e) { Result = true; Close(); }
        private void Cancel_Click(object sender, RoutedEventArgs e) { Result = false; Close(); }
    }
}
