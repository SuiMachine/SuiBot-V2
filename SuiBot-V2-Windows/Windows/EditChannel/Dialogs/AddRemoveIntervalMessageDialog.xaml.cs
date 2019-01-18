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
    /// Interaction logic for AddRemoveIntervalMessageDialog.xaml
    /// </summary>
    public partial class AddRemoveIntervalMessageDialog : Window
    {
        public SuiBot_Core.Storage.IntervalMessage ReturnedIntervalMessage;

        public AddRemoveIntervalMessageDialog(SuiBot_Core.Storage.IntervalMessage IntervalMessage = null)
        {
            InitializeComponent();
            if (IntervalMessage == null)
            {
                this.ReturnedIntervalMessage = new SuiBot_Core.Storage.IntervalMessage(30, "");
                this.Title = "Add new interval message";
            }
            else
            {
                this.ReturnedIntervalMessage = new SuiBot_Core.Storage.IntervalMessage(IntervalMessage);
                this.Title = "Edit interval message";
            }

            this.DataContext = ReturnedIntervalMessage;
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            if(ReturnedIntervalMessage.Interval <= 0)
            {
                MessageBox.Show("Interval has to be equal or higher 1 min!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if(ReturnedIntervalMessage.Message == "")
            {
                MessageBox.Show("A message can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.DialogResult = true;
        }

        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
