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

namespace SuiBot_V2_Windows.Windows.Settings.Dialogs
{
    /// <summary>
    /// Interaction logic for EditListOfChannelsAdd.xaml
    /// </summary>
    public partial class EditListOfChannelsAdd : Window
    {
        public string NewChannel { get; private set; }

        public EditListOfChannelsAdd(string editElement = null)
        {
            InitializeComponent();
            NewChannel = "";
            this.DataContext = this;
            if(editElement != null)
            {
                this.NewChannel = editElement;
                this.Title = "Edit channel name";
            }
        }

        private void B_OKClicked(object sender, RoutedEventArgs e)
        {
            NewChannel = NewChannel.Trim(new char[] { '#', ' ', '!' }).ToLower();
            if(NewChannel == "")
            {
                MessageBox.Show("Channel name can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void B_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TB_Channel_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                B_OKClicked(null, null);
        }
    }
}
