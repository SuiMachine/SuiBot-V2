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

            //For whatever reason (security?) you need to jump through hoops and loops to do binding on password, so here is plain old manual way
            this.PassBox_Password.Password = ConnectionConfig.Password;
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
        }

        private void OKClicked(object sender, RoutedEventArgs e)
        {
            this.ConnectionConfig.Password = this.PassBox_Password.Password;
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
            this.ConnectionConfig.Password = this.PassBox_Password.Password;

            SuiBot_Core.SuiBot sb = new SuiBot_Core.SuiBot(this.ConnectionConfig, SuiBot_Core.Storage.CoreConfig.Load());

            var result = sb.PerformTest();

            switch(result)
            {
                case (0):
                    MessageBox.Show("Test performed successfully! You are good to go!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                case (1):
                    MessageBox.Show("Failed to connect to server", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                case (2):
                    MessageBox.Show("Incorrect login informaion", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }

        }
    }
}
