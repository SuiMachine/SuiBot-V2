using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_Core.API.EventSub;
using SuiBot_Core.API.EventSub.Subscription;
using SuiBot_Core.API.Helix.Request;
using SuiBot_Core.API.Helix.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using WebSocketSharp;
using static System.Net.Mime.MediaTypeNames;

namespace SuiBot_Core.API
{
	public class HelixAPI
	{
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		private ulong m_BotUserId = 0;
		private const string BASE_URI = "https://api.twitch.tv/helix/";
		private const string CLIENT_ID = "rmi9m0sheo4pp5882o8s24zu7h09md";
		private readonly string OAUTH = "";
		private readonly DateTime LastRequest = DateTime.MinValue;
		private SuiBot botInstance;

		private Dictionary<string, string> BuildDefaultHeaders()
		{
			return new Dictionary<string, string>()
			{
				{ "Client-ID", CLIENT_ID },
				{ "Authorization", "Bearer " + OAUTH }
			};
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
					if (!await GetBotInfo())
						return;

					_ = HttpWebRequestHandlers.PerformDelete(new Uri($"https://api.twitch.tv/helix/moderation/chat?broadcaster_id={messageID.broadcaster_user_id}&moderator_id={m_BotUserId}&message_id={messageID}"), BuildDefaultHeaders(), out string _);
				}
				catch (Exception e)
				{
					ErrorLogging.WriteLine($"Failed to remove message: {e}");
				}
			});
		}

		private async Task<bool> GetBotInfo()
		{
			if (m_BotUserId != 0)
				return true;

			Response_GetUserInfo info = await GetUserInfo(botInstance.BotName);
			if (info == null)
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return false;
			}
			else
			{
				m_BotUserId = info.id;
				return true;
			}
		}

		public void RequestTimeout(ES_ChatMessage message, uint length, string reason)
		{
			Task.Run(async () =>
			{
				if (!await GetBotInfo())
					return;


			});

			/*var botId = GetUserId(m_BotName);
			if (botId == "")
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return;
			}

			var info = channel.StreamInformation;
			if (string.IsNullOrEmpty(info.ChannelID))
			{
				ErrorLogging.WriteLine($"Can't obtain id for channel: {channel}");
				return;
			}

			API.Helix.API_Data data = new API.Helix.API_Data()
			{
				data = new API.Helix.API_Timeout()
				{
					user_id = userId,
					duration = length == 0 ? 1 : length,
					reason = string.IsNullOrEmpty(reason) ? null : reason
				}
			};

			string dataStr = JsonConvert.SerializeObject(data);

			if (HttpWebRequestHandlers.PerformTwitchPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={info.ChannelID}&moderator_id={botId}"), BuildDefaultHeaders(), dataStr, out string _))
			{
			}*/
		}

		public void RequestBan(ES_ChatMessage message, string reason)
		{
			Task.Run(async () =>
			{
				if (!await GetBotInfo())
					return;


			});

			/*var botId = GetUserId(m_BotName);
			if (botId == "")
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return;
			}

			var info = channel.StreamInformation;
			if (string.IsNullOrWhiteSpace(info.ChannelID))
			{
				ErrorLogging.WriteLine($"Can't obtain id for channel: {channel}");
				return;
			}*/

			/*			API.Helix.API_Data data = new API.Helix.API_Data()
						{
							data = new API.Helix.Ban()
							{
								user_id = userId,
								duration = 0,
								reason = string.IsNullOrEmpty(reason) ? null : reason
							}
						};

						string dataStr = JsonConvert.SerializeObject(data);

						if (HttpWebRequestHandlers.PerformTwitchPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={channelID}&moderator_id={botId}"), RequestHeaders, dataStr, out string _))
						{

						}*/
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

		internal async Task<EventSub.Subscription.Responses.Response_SubscribeToChannelMessages> SubscribeTo_ChatMessage(string channel, string sessionId)
		{
			if (!await GetBotInfo())
				return null;

			Response_GetUserInfo channelInfo = await GetUserInfo(channel);
			if (channelInfo == null)
				return null;

			var content = new SubscribeMSG_ReadChannelMessage(channelInfo.id, m_BotUserId, sessionId);
			var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serialize, BuildDefaultHeaders());
			if (result != null)
			{
				JToken deserializeObj = (JToken)JsonConvert.DeserializeObject(result);
				var data = deserializeObj["data"];
				if (data != null)
				{
					var cast = data.ToObject<EventSub.Subscription.Responses.Response_SubscribeToChannelMessages[]>();
					return cast.FirstOrDefault(x => x.condition.broadcaster_user_id == channelInfo.id);
				}
				else
					return null;
			}
			else
				return null;
		}

		public void SendMessage(SuiBot_ChannelInstance instance, string text)
		{
			Task.Run(async () =>
			{
				if (!await GetBotInfo())
					return;

				var content = Request_SendMessage.CreateMessage(instance.ChannelID, m_BotUserId, text);
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
				if (!await GetBotInfo())
					return;

				var content = Request_SendMessage.CreateResponse(messageToRespondTo.broadcaster_user_id, m_BotUserId, messageToRespondTo.message_id, message);
				var serialize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore
				});

				var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "chat/messages", "", serialize, BuildDefaultHeaders());

			});
		}
	}
}
