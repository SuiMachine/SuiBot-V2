using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_Core.API.EventSub;
using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_Core.Storage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WebSocketSharp;

namespace SuiBot_Core
{
	public class TwitchSocket
	{
		private string WEBSOCKET_URI = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";

		private SuiBot BotInstance;
		private ConnectionConfig botConnectionConfig;

		internal TwitchSocket(SuiBot botInstance)
		{
			this.BotInstance = botInstance;
			CreateSessionAndSocket(0);
		}

		private Task SubscribingTask;
		private volatile bool m_Connected;
		private volatile bool m_Connecting;
		private volatile bool m_ExternalReconnect;

		public string SessionID { get; private set; }
		public bool Connected => m_Connected;
		public bool Connecting => m_Connecting;
		public volatile bool AutoReconnect;
		public DateTime LastMessageAt { get; private set; }
		public WebSocket Socket { get; private set; }
		private System.Timers.Timer DelayConnectionTimer;
		private System.Timers.Timer KeepAliveCheck;

		private void CreateSessionAndSocket(int delay)
		{
			if (!m_ExternalReconnect)
				BotInstance?.TwitchSocket_Disconnected();
			m_Connected = false;
			m_Connecting = true;

			Socket = new WebSocket(WEBSOCKET_URI);
			Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;

			Socket.OnMessage += Socket_OnMessage;
			Socket.OnOpen += Socket_OnOpen;
			Socket.OnClose += Socket_OnClose;
			Socket.OnError += Socket_OnError;
			Socket.EmitOnPing = true;
			DelayConnectionTimer?.Dispose();

			if (delay <= 0)
				delay = 1;

			DelayConnectionTimer = new System.Timers.Timer
			{
				AutoReset = false,
				Interval = delay,
				Enabled = true
			};
			DelayConnectionTimer.Elapsed += ((sender,e) =>
			{
				Socket.ConnectAsync();
			});
		}

		private void Socket_OnError(object sender, ErrorEventArgs e)
		{
			ErrorLogging.WriteLine($"Got error: {e}");
		}

		private void ReconnectWithUrl(string reconnect_url)
		{
			this.m_ExternalReconnect = true;
			this.AutoReconnect = true;
			this.WEBSOCKET_URI = reconnect_url;
			this.Socket.Close();
		}

		private void Socket_OnOpen(object sender, EventArgs e)
		{
			m_Connected = true;
			m_Connecting = false;
			Debug.WriteLine("Opened Twitch socket");
			KeepAliveCheck = new System.Timers.Timer(5 * 1000);
			KeepAliveCheck.Elapsed += KeepAliveCheck_Elapsed;
			KeepAliveCheck.Start();
		}

		private void KeepAliveCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var currentTime = DateTime.UtcNow;
			if (LastMessageAt + TimeSpan.FromSeconds(45) < currentTime)
				Socket.Close();
		}

		private void Socket_OnClose(object sender, CloseEventArgs e)
		{
			EventSubClose_Code closeType = (EventSubClose_Code)e.Code;
			m_Connected = false;
			m_Connecting = false;
			KeepAliveCheck?.Stop();
			ErrorLogging.WriteLine($"Closed due to code {e.Code} - {e.Reason}");
			Socket.OnMessage -= Socket_OnMessage;
			Socket.OnOpen -= Socket_OnOpen;
			Socket.OnClose -= Socket_OnClose;
			Socket.OnError -= Socket_OnError;

			if (AutoReconnect)
			{
				int delay = 0;
				switch (e.Code)
				{
					case (ushort)CloseStatusCode.Normal:
						delay = 0;
						break;
					case (ushort)CloseStatusCode.Away:
					case (ushort)CloseStatusCode.UnsupportedData:
						delay = 1_000;
						break;
					case (ushort)CloseStatusCode.ProtocolError:
					case (ushort)CloseStatusCode.Undefined:
					case (ushort)CloseStatusCode.Abnormal:
					case (ushort)CloseStatusCode.TooBig:
					case (ushort)CloseStatusCode.ServerError:
					case (ushort)CloseStatusCode.TlsHandshakeFailure:
						delay = 60_000;
						break;
					case (ushort)CloseStatusCode.InvalidData:
					case (ushort)CloseStatusCode.PolicyViolation:
					case (ushort)CloseStatusCode.MandatoryExtension:
						AutoReconnect = false;
						BotInstance?.ClosedViaSocket();
						return;
					default:
						delay = 10_000;
						break;
				}

				CreateSessionAndSocket(delay);
			}
			else
			{
				Socket = null;

				if (BotInstance != null)
				{
					BotInstance.ClosedViaSocket();
				}
			}
		}

		private void Socket_OnMessage(object sender, MessageEventArgs e)
		{
			var message = JsonConvert.DeserializeObject<ES_ServerMessage>(e.Data);
			if (message == null)
			{
				Socket.Ping();
				return;
			}

			LastMessageAt = message.metadata.message_timestamp;
			switch (message.metadata.message_type)
			{
				case EventSub_MessageType.session_welcome:
					ProcessWelcome(message.payload);
					break;
				case EventSub_MessageType.session_keepalive:
					break;
				case EventSub_MessageType.notification:
					ProcessNotification(message);
					break;
				case EventSub_MessageType.session_reconnect:
					ProcessReconnect(message.payload);
					break;
				default:
					Debug.WriteLine($"Unhandled message: {message}");
					break;
			}
		}

		private void ProcessReconnect(JToken payload)
		{
			var sessionField = payload["session"];
			if (sessionField == null)
			{
				ErrorLogging.WriteLine($"Something when wrong with reconnect, debug this message:\n{payload}");
				return;
			}

			var reconnect = sessionField.ToObject<ES_ReconnectSession>();
			if (reconnect == null)
			{
				ErrorLogging.WriteLine($"Something when wrong with reconnect, debug this message:\n{sessionField}");
				return;
			}

			if (reconnect.id != SessionID)
			{
				ErrorLogging.WriteLine("Wrong session ID?!");
				return;
			}

			this.ReconnectWithUrl(reconnect.reconnect_url);
		}

		private void ProcessNotification(ES_ServerMessage message)
		{
			switch (message.metadata.subscription_type)
			{
				case null:
					return;
				case "channel.chat.message":
					ProcessChatMessage(message.payload);
					return;
				case "channel.channel_points_custom_reward_redemption.add":
					ProcessChannelRedeem(message.payload);
					return;
				case "stream.online":
					ProcessStreamOnline(message.payload);
					return;
				case "stream.offline":
					ProcessStreamOffline(message.payload);
					return;
				case "automod.message.hold":
					ProcessAutomodMessageHold(message.payload);
					return;
				case "channel.suspicious_user.message":
					ProcessSuspiciousUserMessage(message.payload);
					return;
				default:
					Console.WriteLine($"Unhandled message type: {message.metadata.subscription_type}");
					return;
			}
		}

		private void ProcessChatMessage(JToken payload)
		{
			var eventText = payload["event"];

			var dbg = eventText.ToString();
			var msg = eventText.ToObject<ES_ChatMessage>();

			//"text"
			//"channel_points_highlighted"
			//"channel_points_sub_only"
			//"power_ups_message_effect"
			//"power_ups_gigantified_emote"
			//"user_intro"
			if (msg.message_type == "user_intro")
			{
				ErrorLogging.WriteLine($"Verify this potential first message:\n{dbg}");
			}

			if (!BotInstance.ChannelInstances.TryGetValue(msg.broadcaster_user_login, out SuiBot_ChannelInstance instance))
				instance = null; //Not needed, but makes VS shutup
			msg.SetupRole(instance);
			BotInstance.TwitchSocket_ChatMessage(msg);
		}

		private void ProcessChannelRedeem(JToken payload)
		{
			if (payload["event"] == null)
				return;
			ES_ChannelPoints obj = payload["event"].ToObject<ES_ChannelPoints>();
			//var passedData = new API.ChannelPointRedeemRequest(obj.user_name, obj.user_id, obj.reward.id, obj.id, obj.status, obj.user_input);

			//Debug.WriteLine($"Received payload with text: {obj.user_input}");
			//OnChannelPointsRedeem?.Invoke(passedData);
		}

		private void ProcessStreamOnline(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_StreamOnline>();
			if (msg == null)
				return;

			if (!BotInstance.ChannelInstances.TryGetValue(msg.broadcaster_user_login, out var channelInstance))
				return;
		}

		private void ProcessStreamOffline(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			//var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_StreamOffline>();
			if (msg == null)
				return;

			if (!BotInstance.ChannelInstances.TryGetValue(msg.broadcaster_user_login, out var channelInstance))
				return;

			channelInstance.StreamStatus = new API.Helix.Responses.Response_StreamStatus()
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

		private void ProcessAutomodMessageHold(JToken payload)
		{
			//We actually don't do anything with them for now
			//cause technically you are supposed to have them handled by a human
			//and not a bot... but maybe in the future...
			var eventText = payload["event"];
			if (eventText == null)
				return;

			var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_AutomodMessageHold>();
			if (msg == null)
				return;
		}

		private void ProcessSuspiciousUserMessage(JToken payload)
		{
			var eventText = payload["event"];
			if (eventText == null)
				return;

			var dbgTxt = payload.ToString();
			var msg = eventText.ToObject<ES_Suspicious_UserMessage>();
			if (msg == null)
				return;

			if (!BotInstance.ActiveChannels.TryGetValue(msg.broadcaster_user_login, out SuiBot_ChannelInstance channel))
				return;

			if (channel.ConfigInstance.FilteringEnabled)
			{
				var asMessage = msg.ConvertToChatMessage();

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

		private void ProcessWelcome(JToken payload)
		{
			var content = payload["session"].ToObject<ES_SessionMessage>();
			SessionID = content.id;
			AutoReconnect = true;
			if (m_ExternalReconnect == false)
				BotInstance?.TwitchSocket_Connected();
			m_ExternalReconnect = false;
		}

		internal void Close()
		{
			AutoReconnect = false;
			if (Socket != null)
				Socket.Close();
			DelayConnectionTimer?.Dispose();
		}

		internal void ProvideAuthentication(ConnectionConfig botConnectionConfig) => this.botConnectionConfig = botConnectionConfig;
	}
}
