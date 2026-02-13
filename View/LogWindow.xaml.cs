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
    /// <summary>
    /// Logique d'interaction pour LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public LogWindow()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Implements "Sticky Scroll" (Magnetism).
        /// If the user is at the bottom, stay at the bottom when new logs arrive.
        /// </summary>
        private void LogScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (e.ExtentHeightChange > 0)
            {

                double bottomOffset = e.ViewportHeight + e.VerticalOffset;
                bool isAtBottom = bottomOffset >= e.ExtentHeight - 30;

                if (isAtBottom)
                {
                    (sender as ScrollViewer).ScrollToBottom();
                }
            }
        }
    }
}
