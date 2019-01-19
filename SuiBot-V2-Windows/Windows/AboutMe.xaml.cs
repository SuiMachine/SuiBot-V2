using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuiBot_V2_Windows.Windows
{
    /// <summary>
    /// Interaction logic for AboutMe.xaml
    /// </summary>
    public partial class AboutMeWindow : Window
    {
        public AboutMeWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var snd = (Hyperlink)sender;
            Process.Start(snd.NavigateUri.ToString());
        }
    }
}
