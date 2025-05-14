using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_Core.API.EventSub;
using SuiBot_Core.Storage;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WebSocketSharp;

namespace SuiBot_Core
{
	public class TwitchSocket
	{
		private const string WEBSOCKET_URI = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";

		private SuiBot BotInstance;
		private ConnectionConfig botConnectionConfig;

		internal TwitchSocket(SuiBot botInstance)
		{
			this.BotInstance = botInstance;
			CreateSessionAndSocket();
		}

		private Task SubscribingTask;
		private volatile bool m_Connected;
		private volatile bool m_Connecting;

		public string SessionID { get; private set; }
		public bool Connected => m_Connected;
		public volatile bool AutoReconnect;
		public DateTime LastMessageAt { get; private set; }
		public WebSocket Socket { get; private set; }

		private System.Timers.Timer KeepAliveCheck;
		public Action OnConnected;
		public Action<ES_ChatMessage> OnChatMessage;


		private void CreateSessionAndSocket()
		{
			m_Connecting = true;

			Socket = new WebSocket(WEBSOCKET_URI);
			Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
			Socket.OnMessage += Socket_OnMessage;
			Socket.OnOpen += Socket_OnOpen;
			Socket.OnClose += Socket_OnClose;
			Socket.EmitOnPing = true;
			Socket.ConnectAsync();
		}

		private void Socket_OnOpen(object sender, EventArgs e)
		{
			m_Connected = true;
			Debug.WriteLine("Opened Twitch socket");
			KeepAliveCheck = new System.Timers.Timer(30 * 1000);
			KeepAliveCheck.Elapsed += KeepAliveCheck_Elapsed;
			KeepAliveCheck.Start();
		}

		private void KeepAliveCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			var currentTime = DateTime.UtcNow;
			if (LastMessageAt + TimeSpan.FromSeconds(31) < currentTime)
			{
				Debug.WriteLine("Should reconnect?");
			}
		}

		private void Socket_OnClose(object sender, CloseEventArgs e)
		{
			EventSubClose_Code closeType = (EventSubClose_Code)e.Code;
			m_Connected = false;
			KeepAliveCheck?.Stop();

			if (AutoReconnect)
			{
				CreateSessionAndSocket();
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
				default:
					Debug.WriteLine($"Unhandled message: {message}");
					break;
			}
		}

		private void ProcessNotification(ES_ServerMessage message)
		{
			switch (message.metadata.subscription_type)
			{
				case null:
					return;
				case "channel.channel_points_custom_reward_redemption.add":
					ProcessChannelRedeem(message.payload);
					break;
				case "channel.chat.message":
					ProcessChatMessage(message.payload);
					break;

			}
		}

		private void ProcessChatMessage(JToken payload)
		{
			var eventText = payload["event"];

			var dbg = eventText.ToString();
			var msg = eventText.ToObject<ES_ChatMessage>();

			if (!BotInstance.ChannelInstances.TryGetValue(msg.broadcaster_user_login, out SuiBot_ChannelInstance instance))
				instance = null; //Not needed, but makes VS shutup
			msg.SetupRole(instance);

			OnChatMessage?.Invoke(msg);
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

		private void ProcessWelcome(JToken payload)
		{
			var content = payload["session"].ToObject<ES_SessionMessage>();
			SessionID = content.id;
			AutoReconnect = true;
			OnConnected?.Invoke();

			/*			if (!Connected)
						{
							//Do reconnect
							if (AutoReconnect && !m_Connecting)
							{
								Task.Factory.StartNew(async () =>
								{
									await CreateSessionAndSocket();
								});
							}
							return;
						}

						//THIS IS NOT SAFE!
						Task.Factory.StartNew(async () =>
						{
							await MainForm.Instance.TwitchBot.Irc.KrakenConnection.EventSub_SubscribeToChannelPoints(MainForm.Instance.TwitchBot.Irc.KrakenConnection.BroadcasterID, SessionID);
						});*/
		}

		internal void Close()
		{
			AutoReconnect = false;
			if (Socket != null)
				Socket.Close();
		}

		internal void ProvideAuthentication(ConnectionConfig botConnectionConfig) => this.botConnectionConfig = botConnectionConfig;
	}
}
