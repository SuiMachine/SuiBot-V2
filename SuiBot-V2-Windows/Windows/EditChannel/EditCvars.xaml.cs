using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace SuiBot_V2_Windows.Windows.EditChannel
{
    /// <summary>
    /// Interaction logic for EditCvars.xaml
    /// </summary>
    public partial class EditCvars : Window
    {
        public SuiBot_Core.Storage.CustomCvars CustomCvarsInstance { get; private set; }

        public EditCvars(SuiBot_Core.Storage.CustomCvars CustomCvarsInstance)
        {
            InitializeComponent();
            this.CustomCvarsInstance = CustomCvarsInstance;
            this.DataContext = CustomCvarsInstance;
        }

        private void Button_OKClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Button_CancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ContexMenu_AddItemClicked(object sender, RoutedEventArgs e)
        {
            Dialogs.AddEditCvar dialog = new Dialogs.AddEditCvar();
            var result = dialog.ShowDialog();
            if(result != null && result == true)
            {
                CustomCvarsInstance.Cvars.Add(dialog.ReturnedCvar);
            }
            ListView_Cvars.Items.Refresh();
        }

        private void ContexMenu_EditItemClicked(object sender, RoutedEventArgs e)
        {
            if(ListView_Cvars.SelectedItem != null)
            {
                var selectedItemId = ListView_Cvars.SelectedIndex;
                var item = (SuiBot_Core.Storage.CustomCvar)ListView_Cvars.Items[selectedItemId];

                Dialogs.AddEditCvar dialog = new Dialogs.AddEditCvar(item);
                var result = dialog.ShowDialog();
                if (result != null && result == true)
                {
                    CustomCvarsInstance.Cvars[selectedItemId] = dialog.ReturnedCvar;
                }
                ListView_Cvars.Items.Refresh();
            }

        }

        private void ContexMenu_RemoveItemClicked(object sender, RoutedEventArgs e)
        {
            if (ListView_Cvars.SelectedItem != null)
            {
                var selectedItemId = ListView_Cvars.SelectedIndex;
                CustomCvarsInstance.Cvars.RemoveAt(selectedItemId);
                ListView_Cvars.Items.Refresh();
            }
        }
    }
}
