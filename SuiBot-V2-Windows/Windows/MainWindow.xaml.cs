using SuiBot_Core;
using SuiBot_Core.API.EventSub;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace SuiBot_V2_Windows.Windows
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		internal static MainWindow Instance { get; private set; }

		internal SuiBot_Core.SuiBot SuiBotInstance;
		private Thread SuiBotSlaveThread { get; set; }
		private bool IsBotRunning { get; set; }
		public bool MinimizeToTray { get; set; }
		private Dictionary<string, RichTextBox> ChannelTabs { get; set; }

		public MainWindow()
		{
			Instance = this;
			DataContext = this;
			ChannelTabs = new Dictionary<string, RichTextBox>();


			InitializeComponent();
			if (!SuiBot_Core.Storage.ConnectionConfig.ConfigExists())
			{
				var tmpConfig = new SuiBot_Core.Storage.ConnectionConfig();
				tmpConfig.Save();
			}
			IsBotRunning = false;
			MinimizeToTray = false;
			SuiBotInstance = SuiBot_Core.SuiBot.GetInstance();

			ReloadActiveChannels();
			RichBox_Log.IsReadOnly = true;
			LogRB_AppendLine(new Run("Welcome to SuiBot V2.") { Foreground = new SolidColorBrush(Colors.Black) });
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
			if (!IsBotRunning)
			{
				var connectionConfig = SuiBot_Core.Storage.ConnectionConfig.Load();
				var coreConfig = SuiBot_Core.Storage.CoreConfig.Load();

				if (!connectionConfig.IsValidConfig())
				{
					MessageBox.Show("Invalid connection config!", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
					return;
				}

				RemoveNonLogTabs();
				SuiBotInstance = SuiBot.GetInstance(connectionConfig, coreConfig);
				SuiBotInstance.OnChannelJoining += SuiBotInstance_OnChannelJoining;
				SuiBotInstance.OnChannelLeaving += SuiBotInstance_OnChannelLeaving;
				SuiBotInstance.OnChannelStatusUpdate += SuiBotInstance_OnChannelStatusUpdate;
				SuiBotInstance.OnChatMessageReceived += SuiBotInstance_OnChatMessageReceived;
				SuiBotInstance.OnChatSendMessage += SuiBotInstance_OnChatSendMessage;
				SuiBotInstance.OnModerationActionPerformed += SuiBotInstance_OnModerationActionPerformed;
				SuiBotInstance.OnShutdown += SuiBotInstance_OnShutdown;
				IsBotRunning = true;
				MenuItem_BotIsRunning.IsChecked = IsBotRunning;
				UpdateChannelsBranchEnable();
				SuiBotSlaveThread = new Thread(SuiBotInstance.Connect);
				SuiBotSlaveThread.Start();
			}
			else
			{
				SuiBotInstance.Shutdown();
			}
		}

		private void RemoveNonLogTabs()
		{
			for (int i = 0; i < TabControl_Channels.Items.Count; i++)
			{
				var tab = (TabItem)TabControl_Channels.Items[i];
				if ((string)tab.Header != "Log")
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

			LogRB_AppendLine("Suibot is now offline.");
			SuiBotInstance.OnChannelJoining -= SuiBotInstance_OnChannelJoining;
			SuiBotInstance.OnChannelLeaving -= SuiBotInstance_OnChannelLeaving;
			SuiBotInstance.OnChannelStatusUpdate -= SuiBotInstance_OnChannelStatusUpdate;
			SuiBotInstance.OnChatMessageReceived -= SuiBotInstance_OnChatMessageReceived;
			SuiBotInstance.OnChatSendMessage -= SuiBotInstance_OnChatSendMessage;
			SuiBotInstance.OnModerationActionPerformed -= SuiBotInstance_OnModerationActionPerformed;
			SuiBotInstance.OnShutdown -= SuiBotInstance_OnShutdown;
			IsBotRunning = false;
			MenuItem_BotIsRunning.IsChecked = IsBotRunning;
			UpdateChannelsBranchEnable();
			if (SuiBotSlaveThread.IsAlive)
			{
				SuiBotSlaveThread.Abort();
			}
			SuiBotSlaveThread = null;
		}

		private void SuiBotInstance_OnModerationActionPerformed(string channel, string user, string response, string duration)
		{
			channel = channel.Trim(new char[] { '#' });
		}

		private void LogRB_AppendLine(string text)
		{
			LogRB_AppendLine(new Run(text));
		}

		private void LogRB_AppendLine(Run text)
		{
			var p = new Paragraph();
			p.Inlines.Add(new Run(DateTime.Now + ": "));
			p.Inlines.Add(text);
			RichBox_Log.Document.Blocks.Add(p);
			RichBox_Log.ScrollToEnd();
		}

		private void SuiBotInstance_OnChatSendMessage(string channel, string message)
		{
			if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
			{
				this.Dispatcher.Invoke(() =>
				SuiBotInstance_OnChatSendMessage(channel, message));
				return;
			}

			channel = channel.Trim(new char[] { '#' });

			if (ChannelTabs.ContainsKey(channel))
			{
				var rb = ChannelTabs[channel];

				Paragraph p = new Paragraph();
				p.Inlines.Add(new Run(SuiBotInstance.BotName + ": " + message) { Foreground = new SolidColorBrush(Colors.DarkBlue), FontWeight = FontWeights.Bold });
				rb.Document.Blocks.Add(p);
			}
		}

		private void SuiBotInstance_OnChatMessageReceived(string channel, ES_ChatMessage message)
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
				p.Inlines.Add(new Run(message.chatter_user_name + ":") { Foreground = GetBrush(message.UserRole), FontWeight = FontWeights.Bold });
				p.Inlines.Add(new Run(" " + message.message.text) { Foreground = new SolidColorBrush(Colors.Black) });
				rb.Document.Blocks.Add(p);
				rb.ScrollToEnd();
			}
		}

		private Brush GetBrush(SuiBot_Core.API.EventSub.ES_ChatMessage.Role userRole)
		{
			switch (userRole)
			{
				case (SuiBot_Core.API.EventSub.ES_ChatMessage.Role.SuperMod):
					return new SolidColorBrush(Colors.Red);
				case (SuiBot_Core.API.EventSub.ES_ChatMessage.Role.Mod):
					return new SolidColorBrush(Colors.Green);
				case (SuiBot_Core.API.EventSub.ES_ChatMessage.Role.VIP):
					return new SolidColorBrush(Colors.MediumPurple);
				case (SuiBot_Core.API.EventSub.ES_ChatMessage.Role.Subscriber):
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
					)
				{ Foreground = new SolidColorBrush(Colors.DarkMagenta), FontWeight = FontWeights.Bold });
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
				RichTextBox rb = new RichTextBox() { IsReadOnly = false, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
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

			if (ChannelTabs.ContainsKey(channel))
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
			foreach (var channel in coreConfig.ChannelsToJoin)
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
			if (result != null && result == true)
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
			if (e.Key == Key.Enter)
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

		private void Window_StateChanged(object sender, EventArgs e)
		{
			if (this.WindowState == WindowState.Minimized)
			{
				if (MinimizeToTray)
				{
					this.trayIcon.Visibility = Visibility.Visible;
					this.Hide();
				}

			}
			else
			{
				this.trayIcon.Visibility = Visibility.Hidden;
			}
		}


		private void TrayIcon_RestoreClicked(object sender, RoutedEventArgs e)
		{
			this.Visibility = Visibility.Visible;
			this.Show();
			this.WindowState = WindowState.Normal;
		}
	}
}
