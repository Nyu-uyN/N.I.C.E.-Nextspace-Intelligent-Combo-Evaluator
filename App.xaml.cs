using System.Configuration;
using System.Data;
using System.Windows;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets a value indicating whether the application is running in Administrator/Developer mode.
        /// When true, changes to tags are saved directly to the Core JSON file instead of the User Overrides file.
        /// </summary>
        public static bool IsAdminMode { get; private set; } = false;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Check for the specific command-line argument to enable Admin Mode.
            if (e.Args.Any(arg => arg.Equals("--admin", StringComparison.OrdinalIgnoreCase)))
            {
                IsAdminMode = true;
            }
        }
    }

}
