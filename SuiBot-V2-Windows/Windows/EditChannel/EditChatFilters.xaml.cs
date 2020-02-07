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

namespace SuiBot_V2_Windows.Windows.EditChannel
{
    /// <summary>
    /// Interaction logic for EditChatFilters.xaml
    /// </summary>
    public partial class EditChatFilters : Window
    {
        public SuiBot_Core.Storage.ChatFilters ChatFilters { get; private set; }

        public EditChatFilters(SuiBot_Core.Storage.ChatFilters ChatFilters)
        {
            InitializeComponent();
            this.ChatFilters = ChatFilters;
            this.DataContext = this.ChatFilters;
        }

        private void Button_OKClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Button_CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ContextMenu_AddClicked(object sender, RoutedEventArgs e)
        {
            SuiBot_Core.Storage.ChatFilters.FilterType filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Purge;
            var selectedTab = (TabItem)TabControl_FiltersSelect.SelectedItem;
            switch (selectedTab.Header)
            {
                case ("Timeout Filters"):
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Timeout;
                    break;
                case ("Ban Filters"):
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Ban;
                    break;
                default:
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Purge;
                    break;
            }
            Dialogs.AddEditFilterDialog aefd = new Dialogs.AddEditFilterDialog(filterType);

            var result = aefd.ShowDialog();
            if(result != null && result == true)
            {
                switch(filterType)
                {
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Purge):
                        ChatFilters.PurgeFilters.Add(aefd.ReturnedFilter);
                        ListView_PurgeFilters.Items.Refresh();
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Timeout):
                        ChatFilters.TimeOutFilter.Add(aefd.ReturnedFilter);
                        ListView_TimeoutFilters.Items.Refresh();
                        break;
                    case (SuiBot_Core.Storage.ChatFilters.FilterType.Ban):
                        ChatFilters.BanFilters.Add(aefd.ReturnedFilter);
                        ListView_BanFilters.Items.Refresh();
                        break;
                }
            }

        }

        private void ContextMenu_EditClicked(object sender, RoutedEventArgs e)
        {
            SuiBot_Core.Storage.ChatFilters.FilterType filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Purge;
            var selectedTab = (TabItem)TabControl_FiltersSelect.SelectedItem;
            switch(selectedTab.Header)
            {
                case ("Timeout Filters"):
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Timeout;
                    break;
                case ("Ban Filters"):
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Ban;
                    break;
                default:
                    filterType = SuiBot_Core.Storage.ChatFilters.FilterType.Purge;
                    break;
            }

            SuiBot_Core.Storage.ChatFilter editedFilter = null;
            switch(filterType)
            {
                case (SuiBot_Core.Storage.ChatFilters.FilterType.Purge):
                    if (ListView_PurgeFilters.SelectedItem == null)
                        return;
                    editedFilter = (SuiBot_Core.Storage.ChatFilter)ListView_PurgeFilters.SelectedItem;
                    break;
                case (SuiBot_Core.Storage.ChatFilters.FilterType.Timeout):
                    if (ListView_TimeoutFilters.SelectedItem == null)
                        return;
                    editedFilter = (SuiBot_Core.Storage.ChatFilter)ListView_TimeoutFilters.SelectedItem;
                    break;
                case (SuiBot_Core.Storage.ChatFilters.FilterType.Ban):
                    if (ListView_BanFilters.SelectedItem == null)
                        return;
                    editedFilter = (SuiBot_Core.Storage.ChatFilter)ListView_BanFilters.SelectedItem;
                    break;
            }

            Dialogs.AddEditFilterDialog aefd = new Dialogs.AddEditFilterDialog(filterType, editedFilter);
            var result = aefd.ShowDialog();
            if(result != null && result == true)
            {
                editedFilter = aefd.ReturnedFilter;
                ListView_PurgeFilters.Items.Refresh();
                ListView_TimeoutFilters.Items.Refresh();
                ListView_BanFilters.Items.Refresh();
            }
        }

        private void ContextMenu_RemoveClicked(object sender, RoutedEventArgs e)
        {

        }
    }
}
