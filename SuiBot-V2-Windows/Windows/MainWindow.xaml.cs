using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SuiBot_Core;

namespace SuiBot_V2_Windows.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SuiBot_Core.SuiBot SuiBotInstance { get; set; }
        private Thread SuiBotThread { get; set; }
        private bool IsBotRunning { get; set; }
        private Dictionary<string, RichTextBox> ChannelTabs { get; set; }

        public MainWindow()
        {
            DataContext = this;
            ChannelTabs = new Dictionary<string, RichTextBox>();

            InitializeComponent();
            if (!SuiBot_Core.Storage.ConnectionConfig.ConfigExists())
            {
                var tmpConfig = new SuiBot_Core.Storage.ConnectionConfig();
                tmpConfig.Save();
            }
            IsBotRunning = false;
            SuiBotInstance = new SuiBot_Core.SuiBot();

            ReloadActiveChannels();
            RichBox_Log.IsEnabled = false;
            var p = new Paragraph();
            p.Inlines.Add(new Run(string.Format("{0} - Welcome to SuiBot V2.", DateTime.Now)) { Foreground = new SolidColorBrush(Colors.Black)});
            RichBox_Log.Document.Blocks.Add(p);
        }

        #region ButtonEvents
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
            if(!IsBotRunning)
            {
                var connectionConfig = SuiBot_Core.Storage.ConnectionConfig.Load();
                var coreConfig = SuiBot_Core.Storage.CoreConfig.Load();

                if(!connectionConfig.IsValidConfig)
                {
                    MessageBox.Show("Invalid connection config!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                RemoveNonLogTabs();
                SuiBotInstance = new SuiBot_Core.SuiBot(connectionConfig, coreConfig);
                SuiBotInstance.OnChannelJoining += SuiBotInstance_OnChannelJoining;
                SuiBotInstance.OnChannelLeaving += SuiBotInstance_OnChannelLeaving;
                SuiBotInstance.OnChannelStatusUpdate += SuiBotInstance_OnChannelStatusUpdate;
                SuiBotInstance.OnChatMessageReceived += SuiBotInstance_OnChatMessageReceived;
                SuiBotInstance.OnChatSendMessage += SuiBotInstance_OnChatSendMessage;
                SuiBotInstance.OnIrcFeedback += SuiBotInstance_OnIrcFeedback;
                SuiBotInstance.OnModerationActionPerformed += SuiBotInstance_OnModerationActionPerformed;
                SuiBotInstance.OnShutdown += SuiBotInstance_OnShutdown;
                IsBotRunning = true;
                MenuItem_BotIsRunning.IsChecked = IsBotRunning;
                UpdateChannelsBranchEnable();
                SuiBotThread = new Thread(SuiBotInstance.Connect);
                SuiBotThread.Start();
            }
            else
            {
                SuiBotInstance.Shutdown();
            }
        }

        private void RemoveNonLogTabs()
        {
            for(int i=0; i<TabControl_Channels.Items.Count; i++)
            {
                var tab = (TabItem)TabControl_Channels.Items[i];
                if((string)tab.Header != "Log")
                {
                    TabControl_Channels.Items.RemoveAt(i);
                    i--;
                }
            }
        }
        #endregion

        #region BotEventHandling
        private void SuiBotInstance_OnShutdown()
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnShutdown());
                return;
            }

            RichBox_Log.Document.Blocks.Add(new Paragraph(new Run("Suibot is now offline.")));
            SuiBotInstance.OnChannelJoining -= SuiBotInstance_OnChannelJoining;
            SuiBotInstance.OnChannelLeaving -= SuiBotInstance_OnChannelLeaving;
            SuiBotInstance.OnChannelStatusUpdate -= SuiBotInstance_OnChannelStatusUpdate;
            SuiBotInstance.OnChatMessageReceived -= SuiBotInstance_OnChatMessageReceived;
            SuiBotInstance.OnChatSendMessage -= SuiBotInstance_OnChatSendMessage;
            SuiBotInstance.OnIrcFeedback -= SuiBotInstance_OnIrcFeedback;
            SuiBotInstance.OnModerationActionPerformed -= SuiBotInstance_OnModerationActionPerformed;
            SuiBotInstance.OnShutdown -= SuiBotInstance_OnShutdown;
            IsBotRunning = false;
            MenuItem_BotIsRunning.IsChecked = IsBotRunning;
            UpdateChannelsBranchEnable();
            if(SuiBotThread.IsAlive)
            {
                SuiBotThread.Abort();
            }
            SuiBotThread = null;
        }

        private void SuiBotInstance_OnModerationActionPerformed(string channel, string user, string response, string duration)
        {
            channel = channel.Trim(new char[] { '#' });
        }

        private void SuiBotInstance_OnIrcFeedback(SuiBot_Core.Events.IrcFeedback feedback, string message)
        {
            if(this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnIrcFeedback(feedback, message));
                return;
            }

            var p = new Paragraph();
            p.Inlines.Add(DateTime.Now + ": " + feedback.ToString() + " " + message);
            RichBox_Log.Document.Blocks.Add(p);
        }

        private void SuiBotInstance_OnChatSendMessage(string channel, string message)
        {
            channel = channel.Trim(new char[] { '#' });

            if (ChannelTabs.ContainsKey(channel))
            {
                var rb = ChannelTabs[channel];

                Paragraph p = new Paragraph();
                p.Inlines.Add(new Run(SuiBotInstance.BotName + ": " + message) { Foreground = new SolidColorBrush(Colors.DarkBlue), FontWeight = FontWeights.Bold });
                rb.Document.Blocks.Add(p);
            }
        }

        private void SuiBotInstance_OnChatMessageReceived(string channel, ChatMessage message)
        {
            channel = channel.Trim(new char[] { '#' });

            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnChatMessageReceived(channel, message));
                return;
            }

            if (ChannelTabs.ContainsKey(channel))
            {
                var rb = ChannelTabs[channel];

                Paragraph p = new Paragraph();
                p.Inlines.Add(new Run(message.Username + ":") { Foreground = GetBrush(message.UserRole), FontWeight = FontWeights.Bold});
                p.Inlines.Add(new Run(" " + message.Message) { Foreground = new SolidColorBrush(Colors.Black)});
                rb.Document.Blocks.Add(p);
            }
        }

        private Brush GetBrush(Role userRole)
        {
            switch(userRole)
            {
                case (Role.SuperMod):
                    return new SolidColorBrush(Colors.Red);
                case (Role.Mod):
                    return new SolidColorBrush(Colors.Green);
                case (Role.VIP):
                    return new SolidColorBrush(Colors.MediumPurple);
                case (Role.Subscriber):
                    return new SolidColorBrush(Colors.Purple);
                default:
                    return new SolidColorBrush(Colors.Black);
            }
        }

        private void SuiBotInstance_OnChannelStatusUpdate(string channel, bool IsOnline, string game)
        {
            channel = channel.Trim(new char[] { '#' });


            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnChannelStatusUpdate(channel, IsOnline, game));
                return;
            }

            if (ChannelTabs.ContainsKey(channel))
            {
                var rb = ChannelTabs[channel];

                Paragraph p = new Paragraph();
                p.Inlines.Add(new Run("! Twitch status update: " +
                    (IsOnline ? "Online" : "Offline") +
                    game
                    ) { Foreground = new SolidColorBrush(Colors.DarkMagenta), FontWeight = FontWeights.Bold });
                rb.Document.Blocks.Add(p);
            }
        }

        private void SuiBotInstance_OnChannelJoining(string channel)
        {
            channel = channel.Trim(new char[] { '#' });


            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnChannelJoining(channel));
                return;
            }

            if (!ChannelTabs.ContainsKey(channel))
            {
                Grid container = new Grid() { Margin = new Thickness(0, 0, 0, 0) };
                RichTextBox rb = new RichTextBox() { IsEnabled = false };
                Style noSpaceStyle = new Style(typeof(Paragraph));
                noSpaceStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0)));
                rb.Resources.Add(typeof(Paragraph), noSpaceStyle);
                container.Children.Add(rb);
                TabControl_Channels.Items.Add(new TabItem() { Header = channel, Content = container });
                ChannelTabs.Add(channel, rb);
            }
        }

        private void SuiBotInstance_OnChannelLeaving(string channel)
        {
            channel = channel.Trim(new char[] { '#' });


            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(() =>
                SuiBotInstance_OnChannelLeaving(channel));
                return;
            }

            if (!ChannelTabs.ContainsKey(channel))
            {
                for (int i = 0; i < TabControl_Channels.Items.Count; i++)
                {
                    var cast = (TabItem)TabControl_Channels.Items[i];
                    if (cast.Header != null && (string)cast.Header == channel)
                    {
                        TabControl_Channels.Items.RemoveAt(i);
                        ChannelTabs.Remove(channel);
                        return;
                    }
                }
            }
        }
        #endregion

        private void UpdateChannelsBranchEnable()
        {
            ChannelsMenuBranch.IsEnabled = !IsBotRunning;
        }

        #region UniversalUIFunctions
        private void ReloadActiveChannels()
        {
            ActiveChannels.Items.Clear();
            var coreConfig = SuiBot_Core.Storage.CoreConfig.Load();
            foreach(var channel in coreConfig.ChannelsToJoin)
            {
                var newMenuElement = new MenuItem() { Header = channel };

                //General features
                var ChatGeneralFeaturesItem = new MenuItem() { Header = "General features" };
                ChatGeneralFeaturesItem.Click += (sender, e) =>
                {
                    EditChannelFeatures(channel);
                };
                newMenuElement.Items.Add(ChatGeneralFeaturesItem);

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

        private void EditChannelFeatures(string channel)
        {
            EditChannel.EditFeatures dlg = new EditChannel.EditFeatures(SuiBot_Core.Storage.ChannelConfig.Load(channel));
            var result = dlg.ShowDialog();
            if (result != null && result == true)
            {
                dlg.ReturnedChannelConfig.Save();
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
        #endregion

        private void OnSendClicked(object sender, RoutedEventArgs e)
        {
            var currentTab = (TabItem)TabControl_Channels.SelectedItem;
            if (ChannelTabs.ContainsKey((string)currentTab.Header))
            {
                var channel = (string)currentTab.Header;
                SuiBotInstance.ActiveChannels["#" + channel].SendChatMessage(TB_Message.Text);
                TB_Message.Clear();
            }
        }

        private void OnTBKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                var currentTab = (TabItem)TabControl_Channels.SelectedItem;
                if (ChannelTabs.ContainsKey((string)currentTab.Header))
                {
                    var channel = (string)currentTab.Header;
                    SuiBotInstance.ActiveChannels["#" + channel].SendChatMessage(TB_Message.Text);
                    TB_Message.Clear();
                }
            }
        }

        private void AboutMe_Clicked(object sender, RoutedEventArgs e)
        {
            AboutMeWindow window = new AboutMeWindow();
            window.ShowDialog();
        }
    }
}
