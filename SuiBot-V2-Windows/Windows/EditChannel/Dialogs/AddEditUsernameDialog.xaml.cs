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

namespace SuiBot_V2_Windows.Windows.EditChannel.Dialogs
{
    /// <summary>
    /// Interaction logic for AddEditUsernameDialog.xaml
    /// </summary>
    public partial class AddEditUsernameDialog : Window
    {
        public string ReturnedUsername { get; private set; }

        public AddEditUsernameDialog(string Username = null)
        {
            InitializeComponent();
            if(Username != null)
            {
                this.Title = "Edit username";
                this.ReturnedUsername = Username;
            }
            else
            {
                this.Title = "Add username";
                this.ReturnedUsername = "";
            }

            this.DataContext = this;
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            this.ReturnedUsername = this.ReturnedUsername.Trim(new char[] { ' ', '#' });

            if(ReturnedUsername == "")
            {
                MessageBox.Show("Username can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
