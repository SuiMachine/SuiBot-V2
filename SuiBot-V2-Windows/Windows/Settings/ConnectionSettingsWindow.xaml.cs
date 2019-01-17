using System;
using System.Collections.Generic;
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

namespace SuiBot_V2_Windows.Windows.Settings
{
    /// <summary>
    /// Interaction logic for ConnectionSettingsWindow.xaml
    /// </summary>
    public partial class ConnectionSettingsWindow : Window
    {
        public SuiBot_Core.Storage.ConnectionConfig ConnectionConfig { get; private set; }

        public ConnectionSettingsWindow(SuiBot_Core.Storage.ConnectionConfig connectionConfig)
        {
            InitializeComponent();
            this.ConnectionConfig = connectionConfig;
            this.DataContext = ConnectionConfig;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TestConnectionClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
