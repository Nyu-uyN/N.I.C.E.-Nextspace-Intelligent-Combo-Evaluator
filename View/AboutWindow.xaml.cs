using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.View
{
    /// <summary>
    /// Logique d'interaction pour AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadAssemblyInfo();
        }
        /// <summary>
        /// Collects environment and application metadata and copies it to the clipboard.
        /// Useful for debugging and user support.
        /// </summary>
        private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var os = Environment.OSVersion;
                var is64Bit = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";

                System.Text.StringBuilder sb = new();
                sb.AppendLine("--- N.I.C.E. Diagnostic Info ---");
                sb.AppendLine($"App Version: {version}");
                sb.AppendLine($"Build Date: {File.GetLastWriteTime(assembly.Location):yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Runtime: .NET {Environment.Version}");
                sb.AppendLine($"OS: {os} ({is64Bit})");
                sb.AppendLine($"Processors: {Environment.ProcessorCount}");
                sb.AppendLine($"Memory: {GC.GetTotalMemory(false) / 1024 / 1024} MB in use");
                sb.AppendLine("--------------------------------");

                Clipboard.SetText(sb.ToString());

                // Petit feedback visuel rapide
                if (sender is Button btn && btn.Content is StackPanel sp)
                {
                    var txt = sp.Children[1] as TextBlock;
                    var oldText = txt.Text;
                    txt.Text = "Copied!";
                    Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => txt.Text = oldText));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

            /// <summary>
            /// Retrieves version and build date from the current assembly.
            /// </summary>
        private void LoadAssemblyInfo()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;

            // Format: Version 1.0.0
            VersionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";

            // To get the build date, we can look at the creation time of the DLL
            try
            {
                string filePath = assembly.Location;
                DateTime buildDate = File.GetLastWriteTime(filePath);
                BuildDateText.Text = $"Build Date: {buildDate:MMMM dd, yyyy}";
            }
            catch
            {
                BuildDateText.Text = "Build Date: Unknown";
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Standard process to open a URL in the default browser
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();



    }
    
}
