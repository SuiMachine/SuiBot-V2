using System;
using System.Linq;
using System.Windows;
using static SuiBot_Core.API.EventSub.ES_ChatMessage;

namespace SuiBot_V2_Windows.Windows.EditChannel.Dialogs
{
    /// <summary>
    /// Interaction logic for AddEditCvar.xaml
    /// </summary>
    public partial class AddEditCvar : Window
    {
        public SuiBot_Core.Storage.CustomCvar ReturnedCvar { get; private set; }

        public AddEditCvar(SuiBot_Core.Storage.CustomCvar ModifiedCvar = null)
        {
            InitializeComponent();

            //This is way easier than declaring this in WPF, one line instead of like 10 really ugly ones
            this.CBox_Role.ItemsSource = Enum.GetValues(typeof(Role)).Cast<Role>();

            if (ModifiedCvar != null)
            {
                ReturnedCvar = new SuiBot_Core.Storage.CustomCvar(ModifiedCvar);
                this.Title = "Edit custom command";
            }
            else
            {
                ReturnedCvar = new SuiBot_Core.Storage.CustomCvar();
                this.Title = "Add custom command";
            }
            this.DataContext = ReturnedCvar;


        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            ReturnedCvar.Command = ReturnedCvar.Command.Trim(new char[] { ' ', '!' });
            if(ReturnedCvar.Command == "")
            {
                MessageBox.Show("Command can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (ReturnedCvar.CvarResponse == "")
            {
                MessageBox.Show("Response can not be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
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
