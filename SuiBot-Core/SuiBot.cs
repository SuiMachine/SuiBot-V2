using SuiBot_Core.API.EventSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;

namespace SuiBot_Core
{
	public class SuiBot : IDisposable
	{
		private static SuiBot m_Instance;
		internal TwitchSocket TwitchSocket { get; private set; }
		internal API.HelixAPI HelixAPI { get; private set; }
		public bool IsDisposed { get; private set; }
		public static SuiBot GetInstance()
		{
			if (m_Instance == null || m_Instance.IsDisposed)
				m_Instance = new SuiBot(Storage.ConnectionConfig.Load(), Storage.CoreConfig.Load());
			return m_Instance;
		}

		public static SuiBot GetInstance(Storage.ConnectionConfig BotConnectionConfig, Storage.CoreConfig BotCoreConfig)
		{
			if (m_Instance == null || m_Instance.IsDisposed)
				m_Instance = new SuiBot(BotConnectionConfig, BotCoreConfig);
			return m_Instance;
		}

		/// <summary>
		/// Creates a new instance of SuiBot.
		/// </summary>
		/// <param name="BotConfig">Config struct object. SuiBot_Config.Load() may be used to load it from config file.</param>
		private SuiBot(Storage.ConnectionConfig BotConnectionConfig, Storage.CoreConfig BotCoreConfig)
		{
			this.BotConnectionConfig = BotConnectionConfig;
			this.BotName = BotConnectionConfig.Username;
			this.BotCoreConfig = BotCoreConfig;
			this.IntervalTimer = new System.Timers.Timer(1000 * 60) { AutoReset = true };
			this.StatusUpdateTimer = new System.Timers.Timer(5 * 1000 * 60) { AutoReset = true };
			this.ActiveChannels = new Dictionary<string, SuiBot_ChannelInstance>();
			this.ChannelInstances = new Dictionary<string, SuiBot_ChannelInstance>();

		}

		private Storage.ConnectionConfig BotConnectionConfig { get; set; }
		public Storage.CoreConfig BotCoreConfig { get; set; }
		public Dictionary<string, SuiBot_ChannelInstance> ActiveChannels { get; set; }
		public Dictionary<string, SuiBot_ChannelInstance> ChannelInstances { get; set; }

		public bool IsAfterFirstStatusUpdate = false;

		public string BotName { get; set; }
		public System.Timers.Timer IntervalTimer;
		public System.Timers.Timer StatusUpdateTimer;
		public bool ShouldRun;

		#region BotEventsDeclraration
		public event Events.OnIrcFeedbackHandler OnIrcFeedback;
		public event Events.OnChatMessageReceivedHandler OnChatMessageReceived;
		public event Events.OnChannelJoiningHandler OnChannelJoining;
		public event Events.OnChannelLeavingHandler OnChannelLeaving;
		public event Events.OnChannelStatusUpdateHandler OnChannelStatusUpdate;
		public event Events.OnChatSendMessageHandler OnChatSendMessage;
		public event Events.OnModerationActionHandler OnModerationActionPerformed;
		public event Events.OnShutdownHandler OnShutdown;
		#endregion

		/// <summary>
		/// Returns an authentication url that is used to obtain an oauth from Twitch.
		/// </summary>
		/// <returns>Authy url.</returns>
		public static string GetAuthenticationURL()
		{
			return new Uri(string.Format("https://id.twitch.tv/oauth2/authorize?client_id=rmi9m0sheo4pp5882o8s24zu7h09md&redirect_uri=https://suimachine.github.io/twitchauthy/&response_type=token&scope={0}",
				string.Join(" ", new string[]
				{
					"bits:read",
					"channel:bot",
					"channel:manage:ads",
					"channel:read:ads",
					"channel:read:goals",
					"channel:read:guest_star",
					"channel:read:polls",
					"channel:manage:polls",
					"channel:read:predictions",
					"channel:manage:predictions",
					"channel:read:redemptions",
					"channel:manage:redemptions",
					"channel:read:subscriptions",
					"channel:moderate",
					"moderation:read",
					"moderator:manage:announcements",
					"moderator:manage:automod",
					"moderator:read:banned_users",
					"moderator:read:chat_messages",
					"moderator:manage:chat_messages",
					"moderator:manage:chat_settings",
					"moderator:read:chatters",
					"moderator:read:followers",
					"moderator:read:guest_star",
					"moderator:read:moderators",
					"moderator:manage:shoutouts",
					"moderator:read:suspicious_users",
					"moderator:read:vips",
					"moderator:manage:warnings",
					"user:bot",
					"user:read:chat",
					"user:read:subscriptions",
					"user:write:chat",
				}))).ToString();
		}

		public void Connect()
		{
			if (!BotConnectionConfig.IsValidConfig())
				throw new Exception("Invalid config!");
			if (BotCoreConfig.ChannelsToJoin.Count == 0)
				throw new Exception("At least 1 channel is required to join.");

			HelixAPI = new API.HelixAPI(this, BotConnectionConfig.Password);

			TwitchSocket = new TwitchSocket(this);
			TwitchSocket.OnConnected += TwitchSocket_Connected;
			TwitchSocket.OnChatMessage += TwitchSocket_ChatMessage;

			IntervalTimer.Elapsed += IntervalTimer_Elapsed;
			StatusUpdateTimer.Elapsed += StatusUpdateTimer_Elapsed;

			//MeebyIrc events
			/*			MeebyIrcClient.OnError += IrcClient_OnError;
						MeebyIrcClient.OnErrorMessage += IrcClient_OnErrorMessage;
						MeebyIrcClient.OnConnecting += IrcClient_OnConnecting;
						MeebyIrcClient.OnConnected += IrcClient_OnConnected;
						MeebyIrcClient.OnAutoConnectError += IrcClient_OnAutoConnectError;
						MeebyIrcClient.OnDisconnected += IrcClient_OnDisconnected;
						MeebyIrcClient.OnRegistered += IrcClient_OnRegistered;
						MeebyIrcClient.OnPart += IrcClient_OnPart;
						MeebyIrcClient.OnJoin += IrcClient_OnJoin;
						MeebyIrcClient.OnConnectionError += MeebyIrcClient_OnConnectionError;
						MeebyIrcClient.OnRawMessage += MeebyIrcClient_OnRawMessage;*/



			/*			MeebyIrcClient.Connect(BotConnectionConfig.Server, BotConnectionConfig.Port);
						BotTask = Task.Factory.StartNew(() =>
						{
							MeebyIrcClient.Listen();
						});

						if (!MeebyIrcClient.IsConnected)
							throw new Exception("Failed to connect");*/
			ShouldRun = true;
		}

		private void TwitchSocket_Connected()
		{
			Console.WriteLine("Connected!");
			ErrorLogging.WriteLine("Connected!");


			Task.Factory.StartNew(async () =>
			{
				foreach (var channel in BotCoreConfig.ChannelsToJoin)
				{
					var result = await HelixAPI.SubscribeTo_ChatMessage(channel, TwitchSocket.SessionID);
					if (result)
					{
						StartedReadingChannel(channel);
					}

					Thread.Sleep(2000);
				}
			});


			//Timer tick
			IntervalTimer.Start();
			StatusUpdateTimer.Start();
		}

		private void TwitchSocket_Disconnected()
		{
			ActiveChannels.Clear();
			IntervalTimer.Stop();
			StatusUpdateTimer.Stop();
		}

		private void TwitchSocket_ChatMessage(ES_ChatMessage newMessage)
		{
			if (ActiveChannels.TryGetValue(newMessage.broadcaster_user_login, out var channelToProcess))
			{
				channelToProcess.DoWork(newMessage);
			}
		}


		#region MeebyIrcEvents
		/*		private void MeebyIrcClient_OnRawMessage(object sender, IrcEventArgs e)
				{
					try
					{
						if (e.Data.Channel != null && e.Data.Nick != null && e.Data.Message != null && ActiveChannels.TryGetValue(e.Data.Channel, out SuiBot_ChannelInstance channel))
						{
							string messageId = e.Data.Tags["id"];

							Role role = GetRoleFromTags(e);
							string userName = e.Data.Nick;
							if (!e.Data.Tags.TryGetValue("display-name", out string displayName))
								displayName = e.Data.Nick;

							string userID = e.Data.Tags["user-id"];
							string messageContent = e.Data.Message;
							bool messageHighlighted = e.Data.Tags.ContainsKey("msg-id") ? e.Data.Tags["msg-id"] == "highlighted-message" : false;
							string customReward = e.Data.Tags.ContainsKey("custom-reward-id") ? e.Data.Tags["custom-reward-id"] : null;
							bool isFirstMessage = e.Data.Tags["first-msg"] == "1";

							ChatMessage LastMessage = new ChatMessage(messageId,
								role,
								displayName,
								userName,
								userID,
								messageContent,
								isFirstMessage,
								messageHighlighted, //if message is highlighted using Twitch points
								customReward //custom reward using viewer points
								);
							this.OnChatMessageReceived?.Invoke(e.Data.Channel, LastMessage);
							channel.DoWork(LastMessage);
						}
					}
					catch (Exception ex)
					{
						ErrorLogging.WriteLine("Exception on raw message " + ex.Message);
					}
				}*/

		/*private void MeebyIrcClient_OnConnectionError(object sender, EventArgs e)
		{
			Console.WriteLine("!!! CONNECTION ERROR!!! " + e.ToString());
			ErrorLogging.WriteLine("!!! CONNECTION ERROR!!! " + e.ToString());
		}*/

		private void StatusUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			foreach (var channel in ActiveChannels)
			{
				//channel.Value.UpdateTwitchStatus(false);
				Thread.Sleep(2000); //A dirty way to make sure we don't go over Request limit
			}
			IsAfterFirstStatusUpdate = true;
		}

		private void IntervalTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var filteredOutChannels = ActiveChannels.Values.Where(x => x.ConfigInstance.IntervalMessageEnabled);
			foreach (var channel in filteredOutChannels)
			{
				channel.TimerTick();
			}
		}

		public void StartedReadingChannel(string channelToJoin)
		{
			if (ChannelInstances.TryGetValue(channelToJoin, out var channel))
			{
				this.OnChannelJoining?.Invoke(channelToJoin);
				ActiveChannels[channelToJoin] = channel;
			}
			else
			{
				var channelInstance = new SuiBot_ChannelInstance(channelToJoin, this, Storage.ChannelConfig.Load(channelToJoin));
				ActiveChannels.Add(channelToJoin, channelInstance);
				ChannelInstances.Add(channelToJoin, channelInstance);
				this.OnChannelJoining?.Invoke(channelToJoin);
			}
		}

		public void StopReadingChannel(string channelToLeave)
		{
			if (ActiveChannels.Remove(channelToLeave))
			{
				this.OnChannelLeaving?.Invoke(channelToLeave);
			}
		}


		/*private void IrcClient_OnJoin(object sender, JoinEventArgs e)
		{
			if (e.Data.Channel != null && e.Data.Nick != null && ActiveChannels.TryGetValue(e.Data.Channel, out SuiBot_ChannelInstance channel))
			{
#if DEBUG
				channel.UpdateActiveUser(e.Data.Nick);
#endif
				Console.WriteLine($"{e.Data.Nick} joined {e.Data.Channel}");
			}
		}

		internal void LeaveChannel(string channelToLeave)
		{
			this.OnChannelLeaving?.Invoke(channelToLeave);
			MeebyIrcClient.RfcPart("#" + channelToLeave);
		}

		private void IrcClient_OnPart(object sender, PartEventArgs e)
		{
			//Console.WriteLine("! PART: " + e.Data.Nick);
		}

		private void IrcClient_OnRegistered(object sender, EventArgs e)
		{
			this.OnIrcFeedback?.Invoke(Events.IrcFeedback.Verified, "");
			Console.WriteLine("! LOGIN VERIFIED");
			ErrorLogging.WriteLine("! LOGIN VERIFIED");
		}

		private void IrcClient_OnDisconnected(object sender, EventArgs e)
		{
			ActiveChannels.Clear();
			Console.WriteLine("! Disconnected");
			ErrorLogging.WriteLine("! Disconnected");
		}*/

		/*		private void IrcClient_OnAutoConnectError(object sender, AutoConnectErrorEventArgs e)
				{
					Console.WriteLine("Auto connect error: " + e.Exception);
					ErrorLogging.WriteLine("Auto connect error: " + e.Exception);

				}*/



		/*		private void IrcClient_OnConnecting(object sender, EventArgs e)
				{
					ErrorLogging.WriteLine("Connecting...");
				}*/

		/*		private void IrcClient_OnErrorMessage(object sender, IrcEventArgs e)
				{
					ErrorLogging.WriteLine("Error: !" + e.Data.Message);
					Console.WriteLine("Error: " + e.Data.Message);
				}

				private void IrcClient_OnError(object sender, ErrorEventArgs e)
				{
					Console.WriteLine("Error: !" + e.ErrorMessage);
					ErrorLogging.WriteLine("Error: !" + e.ErrorMessage);
				}*/
		#endregion



		public void Shutdown()
		{
			ErrorLogging.WriteLine("Planned shutdown performed ");
			Close();
			ErrorLogging.Close();
		}

		public void Close()
		{
			foreach (var channel in ActiveChannels)
			{
				channel.Value.ShutdownTask();
			}

			//MeebyIrcClient.Disconnect();
			System.Threading.Thread.Sleep(2000);
			this.OnShutdown();
		}

		public void SendChatMessageFeedback(string channel, string message)
		{
			this.OnChatSendMessage?.Invoke(channel, message);
		}

		public void Dispose()
		{

		}

		internal void ClosedViaSocket()
		{
			throw new NotImplementedException();
		}
	}
}
