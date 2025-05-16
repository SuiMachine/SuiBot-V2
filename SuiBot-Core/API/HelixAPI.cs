using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_Core.API.EventSub;
using SuiBot_Core.API.EventSub.Subscription;
using SuiBot_Core.API.EventSub.Subscription.Responses;
using SuiBot_Core.API.Helix.Request;
using SuiBot_Core.API.Helix.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SuiBot_Core.API.EventSub.Subscription.Responses.Response_SubscribeTo;

namespace SuiBot_Core.API
{
	public class HelixAPI
	{
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		public string BotLoginName { get; private set; }
		public ulong BotUserId { get; private set; }
		private const string BASE_URI = "https://api.twitch.tv/helix/";
		private const string CLIENT_ID = "rmi9m0sheo4pp5882o8s24zu7h09md";
		private readonly string OAUTH = "";
		//private readonly DateTime LastRequest = DateTime.MinValue;
		private SuiBot botInstance;

		private Dictionary<string, string> BuildDefaultHeaders()
		{
			return new Dictionary<string, string>()
			{
				{ "Client-ID", CLIENT_ID },
				{ "Authorization", "Bearer " + OAUTH }
			};
		}

		public bool ValidateToken()
		{
			var res = HttpWebRequestHandlers.GetSync("https://id.twitch.tv/oauth2/", "validate", "", BuildDefaultHeaders());
			if (string.IsNullOrEmpty(res))
				return false;

			Response_ValidateToken obj = JsonConvert.DeserializeObject<Response_ValidateToken>(res);
			if (obj == null)
				return false;

			BotLoginName = obj.login;
			BotUserId = obj.user_id;
			if (obj.expires_in < 60 * 60 * 24 * 7) //expires in less than 7 days
			{
				var ts = TimeSpan.FromSeconds(obj.expires_in);
				ErrorLogging.WriteLine($"Token expires in: {ts}");
			}
			if (obj.client_id != CLIENT_ID)
			{
				ErrorLogging.WriteLine("Invalid client ID for this token!");
				return false;
			}

			return true;
		}

		//For testing
		public HelixAPI(SuiBot bot, string aouth)
		{
			this.botInstance = bot;
			this.OAUTH = aouth;
		}

		public void GetStatus(SuiBot_ChannelInstance instance)
		{
			var oldStatus = instance.StreamStatus;

			var result = HttpWebRequestHandlers.GetSync(BASE_URI, "streams", $"?user_login={instance.Channel}", BuildDefaultHeaders());
			if (result != "")
			{
				var response = JObject.Parse(result);
				if (response["data"] != null)
				{
					var data = response["data"].ToObject<Response_StreamStatus[]>();
					if (data.Length > 0)
					{
						instance.StreamStatus = data[0];
						instance.StreamStatus.IsOnline = true;
						instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
					}
					else
					{
						instance.StreamStatus = new Response_StreamStatus();
						instance.StreamStatus.IsOnline = false;
						instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
					}
				}
				else
				{
					instance.StreamStatus = new Response_StreamStatus();
					instance.StreamStatus.IsOnline = false;
					instance.StreamStatus.GameChangedSinceLastTime = oldStatus.game_id != instance.StreamStatus.game_id;
				}
			}
			else
			{
				ErrorLogging.WriteLine($"Error checking status for {instance.Channel}");
			}
		}

		public void RequestRemoveMessage(ES_ChatMessage messageID)
		{
			Task.Run(async () =>
			{
				try
				{
					_ = await HttpWebRequestHandlers.PerformDeleteAsync(BASE_URI, "moderation/chat", $"?broadcaster_id={messageID.broadcaster_user_id}&moderator_id={BotUserId}&message_id={messageID}", BuildDefaultHeaders());
				}
				catch (Exception e)
				{
					ErrorLogging.WriteLine($"Failed to remove message: {e}");
				}
			});
		}

		public void RequestTimeout(ES_ChatMessage message, TimeSpan length, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", "", BuildDefaultHeaders(), serialize);

			});
		}

		public void RequestTimeout(ES_ChatMessage message, uint length, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateTimeout(message.chatter_user_id, (int)length, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", "", BuildDefaultHeaders(), serialize);

			});
		}

		public void RequestBan(ulong user_id, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", "", BuildDefaultHeaders(), serialize);

			});
		}

		public void RequestBan(ES_ChatMessage message, string reason)
		{
			Task.Run(async () =>
			{
				var serialize = JsonConvert.SerializeObject(Request_Ban.CreateBan(message.chatter_user_id, reason), Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PerformPostAsync(BASE_URI, "moderation/bans", "", BuildDefaultHeaders(), serialize);

			});
		}

		private async Task<Response_GetUserInfo> GetUserInfo(string userName)
		{
			if (UserNameToInfo.TryGetValue(userName, out Response_GetUserInfo userId))
				return userId;
			else
			{
				var result = await HttpWebRequestHandlers.GetAsync("https://api.twitch.tv/helix/", $"users?login={userName}", "", BuildDefaultHeaders());
				if (!string.IsNullOrEmpty(result))
				{
					var response = JObject.Parse(result);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						var userInfo = response["data"].First.ToObject<Response_GetUserInfo>();
						UserNameToInfo.Add(userName, userInfo);
						return userInfo;
					}
				}
			}

			return null;
		}

		public void RequestUpdate(SuiBot_ChannelInstance instance)
		{
			//var oldStatus = instance.StreamStatus;
			GetStatus(instance);
			if (instance.StreamStatus.game_name != string.Empty)
			{
				instance.SendChatMessage($"New isOnline status is - {instance.StreamStatus.IsOnline} and the game is: {instance.StreamStatus.game_name}");
			}
			else
			{
				instance.SendChatMessage($"New isOnline status is - {instance.StreamStatus.IsOnline}");
			}
		}

		internal async Task<Subscription_Response_Data> SubscribeTo_ChatMessage(string channel, string sessionId)
		{
			Response_GetUserInfo channelInfo = await GetUserInfo(channel);
			if (channelInfo == null)
				return null;

			var content = new SubscribeMSG_ReadChannelMessage(channelInfo.id, BotUserId, sessionId);
			var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (!string.IsNullOrEmpty(result))
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					if (deserialize.data.Length > 0)
					{
						return deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelInfo.id);
					}
					else
						return null;
				}
				else
					return null;
			}
			else
				return null;
		}

		//Too much code repetition - should at least be seperate
		internal async Task<bool> SubscribeToOnlineStatus(ulong channelID, string sessionID)
		{
			var request = new SubscribeMSG_StreamOnline(channelID, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		internal async Task<bool> SubscribeToOfflineStatus(ulong channelID, string sessionID)
		{
			var request = new SubscribeMSG_StreamOffline(channelID, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		internal async Task<bool> SubscribeToAutoModHold(ulong channelID, string sessionID)
		{
			var request = new SubscribeMSG_AutomodMessageHold(channelID, BotUserId, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		internal async Task<bool> SubscribeToChannelSuspiciousUserMessage(ulong channelID, string sessionID)
		{
			var request = new SubscribeMSG_ChannelSuspiciousUserMessage(channelID, BotUserId, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		internal async Task<bool> SubscribeToChannelAdBreak(ulong channelID, string sessionID)
		{
			//Idk... why this breaks with 403
			var request = new SubscribeMSG_ChannelAdBreakBegin(channelID, BotUserId, sessionID);
			var serialize = JsonConvert.SerializeObject(request, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				Response_SubscribeTo deserialize = JsonConvert.DeserializeObject<Response_SubscribeTo>(result);
				if (deserialize != null)
				{
					deserialize.PerformCostCheck();
					var channel = deserialize.data.FirstOrDefault(x => x.condition.broadcaster_user_id == channelID);
					return channel != null;
				}
				else
					return false;
			}

			return false;
		}

		public void SendMessage(SuiBot_ChannelInstance instance, string text)
		{
			Task.Run(async () =>
			{
				var content = Request_SendChatMessage.CreateMessage(instance.ChannelID, BotUserId, text);
				var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());

			});
		}

		public void SendResponse(ES_ChatMessage messageToRespondTo, string message)
		{
			Task.Run(async () =>
			{
				var content = Request_SendChatMessage.CreateResponse(messageToRespondTo.broadcaster_user_id, BotUserId, messageToRespondTo.message_id, message);
				var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());

			});
		}

		internal void RequestShoutout(ES_ChatMessage lastMessage, string username)
		{
			Task.Run(async () =>
			{

			});
		}
	}
}
