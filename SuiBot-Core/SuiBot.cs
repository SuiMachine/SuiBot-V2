using SuiBot_TwitchSocket;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBot_TwitchSocket.API.EventSub.Subscription.Responses;
using SuiBot_TwitchSocket.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SuiBot_Core
{
	public class SuiBot : IBotInstance
	{
		public const string CLIENT_ID = "rmi9m0sheo4pp5882o8s24zu7h09md";
		private static SuiBot m_Instance;
		internal TwitchSocket TwitchSocket { get; private set; }
		internal SuiBot_TwitchSocket.API.HelixAPI HelixAPI { get; private set; }
		public bool ShouldRun { get; set; }
		private bool m_IsDisposed;
		public bool IsDisposed => m_IsDisposed;
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
		public string BotName => HelixAPI.User_LoginName;

		public bool IsAfterFirstStatusUpdate = false;

		public System.Timers.Timer IntervalTimer;
		public System.Timers.Timer StatusUpdateTimer;

		#region BotEventsDeclraration
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
		public static string GetAuthenticationURL() => SuiBot_TwitchSocket.API.HelixAPI.GenerateAuthenticationURL(CLIENT_ID, "https://suimachine.github.io/twitchauthy/", new string[]
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
					"moderator:manage:banned_users",
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
				});
		public void Connect()
		{
			if (!BotConnectionConfig.IsValidConfig())
				throw new Exception("Invalid config!");
			if (BotCoreConfig.ChannelsToJoin.Count == 0)
				throw new Exception("At least 1 channel is required to join.");

#if LOCAL_API

			HelixAPI = new API.HelixAPI(this, "2ae883f289a6106");
			//var validationResult = HelixAPI.ValidateToken();
#else
			HelixAPI = new SuiBot_TwitchSocket.API.HelixAPI(CLIENT_ID, this, BotConnectionConfig.Password);
			var validationResult = HelixAPI.ValidateToken();
			if (validationResult != SuiBot_TwitchSocket.API.HelixAPI.ValidationResult.Successful)
			{
				if (validationResult == SuiBot_TwitchSocket.API.HelixAPI.ValidationResult.Failed)
				{
					ShouldRun = false;
					ErrorLogging.WriteLine("Invalid token!");
					throw new Exception("Invalid token");
				}
				else
				{
					Task.Factory.StartNew(async () =>
					{
						await Task.Delay(10000);
						if (!IsDisposed)
							Connect();
					});
					return;
				}
			}
#endif


			ShouldRun = true;
			TwitchSocket = new TwitchSocket(this);
		}

		public void TwitchSocket_Connected()
		{
			ErrorLogging.WriteLine("Connected!");

			Task.Factory.StartNew(async () =>
			{
				List<Response_SubscribeTo.Subscription_Response_Data> channelsToSubScribeAdditionalInformationTo = new List<Response_SubscribeTo.Subscription_Response_Data>();

				foreach (var channel in BotCoreConfig.ChannelsToJoin)
				{
					Response_SubscribeTo.Subscription_Response_Data result = await HelixAPI.SubscribeToChatMessage(channel, TwitchSocket.SessionID);
					if (result != null)
					{
						channelsToSubScribeAdditionalInformationTo.Add(result);
						StartedReadingChannel(channel, result);
					}

					await Task.Delay(2000);
				}

				Response_SubscribeTo currentSubscriptionChecks = await HelixAPI.GetCurrentEventSubscriptions();
				foreach (var subscription in currentSubscriptionChecks.data)
				{
					if (subscription.status != "enabled")
					{
						Console.WriteLine($"Unsubscribing from {subscription.type} ({subscription.status})");
						await HelixAPI.CloseSubscription(subscription);
						await Task.Delay(100);
					}
				}

				foreach (var channel in channelsToSubScribeAdditionalInformationTo)
				{
					Console.WriteLine($"Subscribing to additional events for {channel.condition.broadcaster_user_id}");

					var onLineSub = await HelixAPI.SubscribeToOnlineStatus(channel.condition.broadcaster_user_id, TwitchSocket.SessionID);
					await Task.Delay(2000);
					var offlineSub = await HelixAPI.SubscribeToOfflineStatus(channel.condition.broadcaster_user_id, TwitchSocket.SessionID);
					await Task.Delay(2000);

					var automodHold = await HelixAPI.SubscribeToAutoModHold(channel.condition.broadcaster_user_id, TwitchSocket.SessionID);
					await Task.Delay(2000);
					var susMessage = await HelixAPI.SubscribeToChannelSuspiciousUserMessage(channel.condition.broadcaster_user_id, TwitchSocket.SessionID);
					await Task.Delay(2000);
				}
				Console.WriteLine($"Done!");
			});

			//Timer tick
			IntervalTimer.Start();
			StatusUpdateTimer.Start();
		}

		public void TwitchSocket_Disconnected()
		{
			IntervalTimer.Stop();
			StatusUpdateTimer.Stop();

			var channels = ActiveChannels.Keys.ToList();
			foreach (var channel in channels)
				StopReadingChannel(channel);
			ActiveChannels.Clear();
		}

		public void TwitchSocket_ChatMessage(ES_ChatMessage newMessage)
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

		private void StartedReadingChannel(string channelToJoin, Response_SubscribeTo.Subscription_Response_Data result)
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
			ShouldRun = false;
			ErrorLogging.WriteLine("Planned shutdown performed ");
			Close();
			ActiveChannels = null;
			ChannelInstances = null;
			m_IsDisposed = true;
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

		public void TwitchSocket_ClosedViaSocket()
		{
			if (!IsDisposed)
				Dispose();
		}

		public string VerifyAuthy()
		{
			HelixAPI = new SuiBot_TwitchSocket.API.HelixAPI("rmi9m0sheo4pp5882o8s24zu7h09md", this, BotConnectionConfig.Password);
			var validation = HelixAPI.GetValidation();
			if (validation == null)
				return "";
			else
			{
				var expiry = TimeSpan.FromSeconds(validation.expires_in);

				if (expiry.Seconds <= 0)
				{
					return $"Token validation:\n" +
						$"User login: {validation.login}\n" +
						$"User id: {validation.user_id}\n" +
						$"Token is expired!!!!";
				}
				else
				{
					return $"Token validation:\n" +
						$"User login: {validation.login}\n" +
						$"User id: {validation.user_id}\n" +
						$"Expires in: {expiry.Days} days {expiry.Hours} hours {expiry.Minutes} minutes {expiry.Seconds} seconds\n";
				}
			}
		}

		public bool GetChannelInstanceUsingLogin(string channel_login_name, out IChannelInstance instance)
		{
			if (ActiveChannels.TryGetValue(channel_login_name, out var foundChannel))
			{
				instance = foundChannel;
				return true;
			}
			else
			{
				instance = null;
				return false;
			}
		}

		public void TwitchSocket_SuspiciousMessageReceived(ES_Suspicious_UserMessage suspiciousMessage)
		{
			if (!ActiveChannels.TryGetValue(suspiciousMessage.broadcaster_user_login, out SuiBot_ChannelInstance channel))
				return;

			if (channel.ConfigInstance.FilteringEnabled)
			{
				var asMessage = suspiciousMessage.ConvertToChatMessage();

				if (channel.ChatFiltering.FilterOutMessages(asMessage, true))
				{
					channel.UserBan(asMessage);
					channel.SendChatMessage("Bam! Banned suspicious chatter!");
					return;
				}
				else if (channel.ConfigInstance.FilterUsingAI)
				{
					if (channel.GeminiAI.IsConfigured())
						channel.GeminiAI.PerformAIFiltering(channel, asMessage);
				}
			}
		}

		public void TwitchSocket_StreamWentOnline(ES_StreamOnline onlineData)
		{
			//Nothing!
		}

		public void TwitchSocket_StreamWentOffline(ES_StreamOffline offlineData)
		{
			if (!ChannelInstances.TryGetValue(offlineData.broadcaster_user_login, out var channelInstance))
				return;

			channelInstance.StreamStatus = new SuiBot_TwitchSocket.API.Helix.Responses.Response_StreamStatus()
			{
				IsOnline = false,
				GameChangedSinceLastTime = true
			};

			if (channelInstance.ConfigInstance.LeaderboardsEnabled)
			{
				if (!channelInstance.Leaderboards.GameOverride)
					channelInstance.Leaderboards.CurrentGame = channelInstance.StreamStatus.game_name;
			}
		}

		public void TwitchSocket_AutoModMessageHold(ES_AutomodMessageHold messageHold)
		{
			//Nothing
		}

		public void TwitchSocket_ChannelPointsRedeem(ES_ChannelPoints.ES_ChannelPointRedeemRequest redeemInfo)
		{
			//Nothing?
		}

		public void TwitchSocket_OnChannelGoalEnd(ES_ChannelGoal channelGoalEnded)
		{
			//Nothing?
		}

		public void TwitchSocket_AdBreakBegin(ES_AdBreakBeginNotification infoAboutAd)
		{
		}

		public void TwitchSocket_AdBreakFinished(ES_AdBreakBeginNotification infoAboutAd)
		{
		}

		public void TwitchSocket_ChannelRaid(ES_ChannelRaid raidInfo)
		{
		}
	}
}
