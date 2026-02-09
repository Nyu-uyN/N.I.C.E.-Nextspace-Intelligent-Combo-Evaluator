using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.ViewModel;
using System.Diagnostics;
using System.IO;
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            List<Combo> combos = ComboController.ComputeBestLoadout_Parallel(TagController.Tags);
            sw.Stop();
            string tempsFormate = sw.Elapsed.ToString(@"hh\:mm\:ss\.fff");
            using (var writer = new StreamWriter("ttdcombos.txt", append: false, encoding: Encoding.UTF8))
            {
                writer.WriteLine($"Rapport généré le : {DateTime.Now} - Temps de calcul: {tempsFormate}");
                writer.WriteLine("========================================");
                writer.WriteLine();

                int rank = 1;
                foreach (var combo in combos)
                {
                    // En-tête de rang (ex: #1, #2...)
                    writer.WriteLine($"################# RANG {rank} #################");

                    // Utilise ta méthode ToString() détaillée
                    writer.WriteLine(combo.ToString());

                    // Espace entre les combos pour la lisibilité
                    writer.WriteLine();

                    rank++;
                }
            }
        }


    }
}