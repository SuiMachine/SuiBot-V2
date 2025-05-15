using SuiBot_Core.API.EventSub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			if (!HelixAPI.ValidateToken())
			{
				Close();
				throw new Exception("Invalid token");
			}

			TwitchSocket = new TwitchSocket(this);
			TwitchSocket.OnConnected += TwitchSocket_Connected;
			TwitchSocket.OnChatMessage += TwitchSocket_ChatMessage;
			TwitchSocket.OnDisconnected += TwitchSocket_Disconnected;

			IntervalTimer.Elapsed += IntervalTimer_Elapsed;
			StatusUpdateTimer.Elapsed += StatusUpdateTimer_Elapsed;

			ShouldRun = true;
		}

		private void TwitchSocket_Connected()
		{
			ErrorLogging.WriteLine("Connected!");

			Task.Factory.StartNew(async () =>
			{
				List<API.EventSub.Subscription.Responses.Response_SubscribeTo.Subscription_Response_Data> channelsToSubScribeAdditionalInformationTo = new List<API.EventSub.Subscription.Responses.Response_SubscribeTo.Subscription_Response_Data>();

				foreach (var channel in BotCoreConfig.ChannelsToJoin)
				{
					API.EventSub.Subscription.Responses.Response_SubscribeTo.Subscription_Response_Data result = await HelixAPI.SubscribeTo_ChatMessage(channel, TwitchSocket.SessionID);
					if (result != null)
					{
						channelsToSubScribeAdditionalInformationTo.Add(result);
						StartedReadingChannel(channel, result);
					}

					await Task.Delay(2000);
				}

				foreach (var channel in channelsToSubScribeAdditionalInformationTo)
				{
					if (!await HelixAPI.SubscribeToOnlineStatus(channel.condition.broadcaster_user_id, TwitchSocket.SessionID))
						continue;
					if (!await HelixAPI.SubscribeToOfflineStatus(channel.condition.broadcaster_user_id, TwitchSocket.SessionID))
						continue;
					if (!await HelixAPI.SubcribeToChannelAdBreak(channel.condition.broadcaster_user_id, TwitchSocket.SessionID))
						continue;

					await Task.Delay(2000);
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

			var channels = ActiveChannels.Keys.ToList();
			foreach (var channel in channels)
				StopReadingChannel(channel);
		}

		private void TwitchSocket_ChatMessage(ES_ChatMessage newMessage)
		{
			if (ActiveChannels.TryGetValue(newMessage.broadcaster_user_login, out var channelToProcess))
			{
				channelToProcess.DoWork(newMessage);
			}
		}

		private void StatusUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			foreach (var channel in ActiveChannels)
			{
				channel.Value.UpdateTwitchStatus(false);
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

		private void StartedReadingChannel(string channelToJoin, API.EventSub.Subscription.Responses.Response_SubscribeTo.Subscription_Response_Data result)
		{
			ErrorLogging.WriteLine($"Subscribed to read: {channelToJoin}");
			if (ChannelInstances.TryGetValue(channelToJoin, out var channel))
			{
				this.OnChannelJoining?.Invoke(channelToJoin);
				ActiveChannels[channelToJoin] = channel;
			}
			else
			{
				var channelInstance = new SuiBot_ChannelInstance(channelToJoin, result.condition.broadcaster_user_id, this, Storage.ChannelConfig.Load(channelToJoin));
				ActiveChannels.Add(channelToJoin, channelInstance);
				ChannelInstances.Add(channelToJoin, channelInstance);
				this.OnChannelJoining?.Invoke(channelToJoin);
			}
		}

		private void StopReadingChannel(string channelToLeave)
		{
			ErrorLogging.WriteLine($"Unsubscribed from reading: {channelToLeave}");

			if (ActiveChannels.Remove(channelToLeave))
			{
				this.OnChannelLeaving?.Invoke(channelToLeave);
			}
		}

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
			Debug.WriteLine("Implement dispose?");
		}

		internal void ClosedViaSocket()
		{
			if (!IsDisposed)
				Dispose();
		}
	}
}
