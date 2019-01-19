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
    /// Interaction logic for EditListOfChannels.xaml
    /// </summary>
    public partial class EditListOfChannels : Window
    {
        public SuiBot_Core.Storage.CoreConfig CoreConfig { get; private set; }

        public EditListOfChannels(SuiBot_Core.Storage.CoreConfig CoreConfig)
        {
            InitializeComponent();
            this.CoreConfig = CoreConfig;
            this.DataContext = CoreConfig;
        }

        private void B_Contex_Add_Click(object sender, RoutedEventArgs e)
        {
            Dialogs.EditListOfChannelsAdd elc = new Dialogs.EditListOfChannelsAdd();
            var result = elc.ShowDialog();
            if(result != null && result == true)
            {
                if(elc.NewChannel != "")
                {
                    var kek = (List<string>)ListB_Channels.ItemsSource;
                    kek.Add(elc.NewChannel);
                    Refresh();
                }
            }
        }

        private void B_Contex_Edit_Click(object sender, RoutedEventArgs e)
        {
            int id = ListB_Channels.SelectedIndex;
            if (id >= 0)
            {
                var tempCast = (List<string>)ListB_Channels.ItemsSource;
                var element = tempCast[id];
                Dialogs.EditListOfChannelsAdd elc = new Dialogs.EditListOfChannelsAdd(element);
                var result = elc.ShowDialog();
                if(result != null && result == true)
                {
                    if(elc.NewChannel != "")
                    {
                        tempCast[id] = elc.NewChannel;
                        Refresh();
                    }
                }
            }
        }

        private void B_Contex_Remove_Click(object sender, RoutedEventArgs e)
        {
            var item = ListB_Channels.SelectedItem;
            if (item != null)
            {
                var id = ListB_Channels.SelectedIndex;
                CoreConfig.ChannelsToJoin.RemoveAt(id);
                this.ListB_Channels.Items.Refresh();
            }
        }

        private void Refresh()
        {
            ListB_Channels.Items.Refresh();
        }

        private void B_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void B_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();

        }

        private void ListB_Channels_PressedKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                B_Contex_Remove_Click(null, null);
            }
        }
    }
}
