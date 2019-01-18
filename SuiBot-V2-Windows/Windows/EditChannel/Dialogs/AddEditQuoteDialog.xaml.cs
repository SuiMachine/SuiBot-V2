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
    /// Interaction logic for AddEditQuoteDialog.xaml
    /// </summary>
    public partial class AddEditQuoteDialog : Window
    {
        public SuiBot_Core.Storage.Quote ReturnedQuote { get; private set; }

        public AddEditQuoteDialog(SuiBot_Core.Storage.Quote QuoteToEdit = null)
        {
            InitializeComponent();

            if(QuoteToEdit == null)
            {
                this.Title = "Add new quote";
                this.ReturnedQuote = new SuiBot_Core.Storage.Quote();
            }
            else
            {
                this.Title = "Edit quote";
                this.ReturnedQuote = new SuiBot_Core.Storage.Quote(QuoteToEdit);
            }

            this.DataContext = ReturnedQuote;
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            if(ReturnedQuote.Text == "")
            {
                MessageBox.Show("Quote can not be empty string!", "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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
