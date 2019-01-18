using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace SuiBot_V2_Windows.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SuiBot_Core.SuiBot SuiBotInstance;
        private bool IsBotRunning { get; set; }

        public MainWindow()
        {
            DataContext = this;

            InitializeComponent();
            if (!SuiBot_Core.Storage.ConnectionConfig.ConfigExists())
            {
                var tmpConfig = new SuiBot_Core.Storage.ConnectionConfig();
                tmpConfig.Save();
            }
            IsBotRunning = false;
            SuiBotInstance = new SuiBot_Core.SuiBot();

            ReloadActiveChannels();
        }

        private void MenuItem_ConnectionSettingsClick(object sender, RoutedEventArgs e)
        {
            Settings.ConnectionSettingsWindow csw = new Settings.ConnectionSettingsWindow(SuiBot_Core.Storage.ConnectionConfig.Load());
            var result = csw.ShowDialog();
            if (result != null && result == true)
            {
                csw.ConnectionConfig.Save();
            }
        }

        private void MenuItem_ExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void EditActiveChannelsClicked(object sender, RoutedEventArgs e)
        {
            Settings.EditListOfChannels lstEdit = new Settings.EditListOfChannels(SuiBot_Core.Storage.CoreConfig.Load());
            var result = lstEdit.ShowDialog();
            if (result != null && result == true)
            {
                lstEdit.CoreConfig.Save();
                ReloadActiveChannels();
            }
        }

        private void MenuItem_RunBotClicked(object sender, RoutedEventArgs e)
        {
            IsBotRunning = !IsBotRunning;
            ((MenuItem)sender).IsChecked = IsBotRunning;
            UpdateChannelsBranchEnable();
        }

        private void UpdateChannelsBranchEnable()
        {
            ChannelsMenuBranch.IsEnabled = !IsBotRunning;
        }

        private void ReloadActiveChannels()
        {
            ActiveChannels.Items.Clear();
            var coreConfig = SuiBot_Core.Storage.CoreConfig.Load();
            foreach(var channel in coreConfig.ChannelsToJoin)
            {
                var newMenuElement = new MenuItem() { Header = channel };

                //Chat Filters
                var ChatFiltersMenuItem = new MenuItem() { Header = "Chat filters" };
                ChatFiltersMenuItem.Click += (sender, e) =>
                {
                    EditChatFilters(channel);
                };
                newMenuElement.Items.Add(ChatFiltersMenuItem);

                //Chat Filters DB
                var ChatFiltersUsersMenuItem = new MenuItem() { Header = "Chat filters users" };
                ChatFiltersUsersMenuItem.Click += (sender, e) =>
                {
                    EditChatFiltersUsers(channel);
                };
                newMenuElement.Items.Add(ChatFiltersUsersMenuItem);

                //Custom Filters
                var CustomCvarsMenuItem = new MenuItem() { Header = "Custom Cvars" };
                CustomCvarsMenuItem.Click += (sender, e) =>
                {
                    EditCvars(channel);
                };
                newMenuElement.Items.Add(CustomCvarsMenuItem);

                //Interval Messages
                var IntervalMessagesMenuItem = new MenuItem() { Header = "Interval Messages" };
                IntervalMessagesMenuItem.Click += (sender, e) =>
                {
                    EditIntervalMessages(channel);
                };
                newMenuElement.Items.Add(IntervalMessagesMenuItem);

                //Quote
                var QuotesMenuItem = new MenuItem() { Header = "Quotes" };
                QuotesMenuItem.Click += (sender, e) =>
                {
                    EditQuotes(channel);
                };
                newMenuElement.Items.Add(QuotesMenuItem);

                ActiveChannels.Items.Add(newMenuElement);
            }
        }

        private void EditChatFilters(string channel)
        {
            EditChannel.EditChatFilters dlg = new EditChannel.EditChatFilters(SuiBot_Core.Storage.ChatFilters.Load(channel));
            var result = dlg.ShowDialog();
            if(result != null && result == true)
            {
                dlg.ChatFilters.Save();
            }
        }

        private void EditChatFiltersUsers(string channel)
        {
            EditChannel.EditChatFiltersUsers dlg = new EditChannel.EditChatFiltersUsers(SuiBot_Core.Storage.ChatFilterUsersDB.Load(channel));
            var result = dlg.ShowDialog();
            if (result != null && result == true)
            {
                dlg.ChatFilterUsersDB.Save();
            }
        }

        private void EditCvars(string channel)
        {
            EditChannel.EditCvars dlg = new EditChannel.EditCvars(SuiBot_Core.Storage.CustomCvars.Load(channel));
            var result = dlg.ShowDialog();
            if (result != null && result == true)
            {
                dlg.CustomCvarsInstance.Save();
            }
        }

        private void EditIntervalMessages(string channel)
        {
            EditChannel.EditIntervalMessages dlg = new EditChannel.EditIntervalMessages(SuiBot_Core.Storage.IntervalMessages.Load(channel));
            var result = dlg.ShowDialog();
            if (result != null && result == true)
            {
                dlg.IntervalMessages.Save();
            }
        }

        private void EditQuotes(string channel)
        {
            EditChannel.EditQuotes dlg = new EditChannel.EditQuotes(SuiBot_Core.Storage.Quotes.Load(channel));
            var result = dlg.ShowDialog();
            if (result != null && result == true)
            {
                dlg.Quotes.Save();
            }
        }
    }
}
