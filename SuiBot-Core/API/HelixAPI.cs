using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SuiBot_Core.API.EventSub.Subscription;
using SuiBot_Core.API.Helix.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;

namespace SuiBot_Core.API
{
	public class HelixAPI
	{
		public Dictionary<string, Response_GetUserInfo> UserNameToInfo = new Dictionary<string, Response_GetUserInfo>();
		private long m_BotUserId;
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
			//TODO: Deserialize this - this is a mess!
			if (HttpWebRequestHandlers.PerformGetRequest(new Uri($"https://api.twitch.tv/helix/streams?user_login={instance.Channel}"), BuildDefaultHeaders(), out string res))
			{
				var response = JObject.Parse(res);
				if (response["data"] != null)
				{
					var data = response["data"].ToObject<Response_StreamStatus[]>();
					if (data.Length > 0)
						instance.StreamStatus = data[0];
				}
				else
				{
					instance.StreamStatus = new Response_StreamStatus();
				}
				/*				try
								{
									if (response["data"] != null && response["data"].Children().Count() > 0)
									{
										var dataNode = response["data"].First;
										if (dataNode["title"] != null)
										{
											instance.StreamInformation.SetIsOnline(true);
											var title = dataNode["title"].ToString();
											TitleHasChanged = StoredTitle != title;
											StoredTitle = title;

											if (dataNode["type"] != null)
											{
												var streamType = dataNode["type"].ToString();
												if (streamType == "live")
													IsOnline = true;
												else
													IsOnline = false;
											}

											if (dataNode["started_at"] != null)
											{
												string dateTimeStartAsString = dataNode["started_at"].ToString();
												if (DateTime.TryParse(dateTimeStartAsString, out DateTime ParsedTime))
												{
													StartTime = ParsedTime.ToUniversalTime();
												}
												else
													StartTime = DateTime.MinValue;
											}

											if (dataNode["game_id"] != null)
											{
												var gameIdAsString = dataNode["game_id"].ToString();
												Game = ResolveNameFromId(gameIdAsString);
												if (Game == "ul")
												{
													Game = String.Empty;
												}
												Console.WriteLine($"{m_ChannelName} - Checked stream status. Is online, playing {Game}");
											}
											else
											{
												Console.WriteLine($"{m_ChannelName} - Checked stream status. Is online (no game?)");
											}
										}
										else
										{
											IsOnline = false;
											Console.WriteLine($"{m_ChannelName} - Checked stream status. Is offline.");
										}
									}
									else
									{
										//IsOnline = false;
										Console.WriteLine($"{m_ChannelName} - Checked stream status. Is offline.");
									}
								}
								catch (Exception e)
								{
									ErrorLogging.WriteLine("Error trying to parse Json when doing stream update request: " + e.Message);
									//IsOnline = false;
								}
				*/
			}
			else
			{
				Console.WriteLine("Error checking Json");
			}
		}

		public string ResolveNameFromId(string id)
		{
			/*			if (m_OldId == id)
						{
							return Game;
						}
						else
						{
							if (HttpWebRequestHandlers.PerformGetRequest(new Uri("https://api.twitch.tv/helix/games?id=" + id), RequestHeaders, out string res))
							{
								m_OldId = id;
								JObject jObjectNode = JObject.Parse(res);
								JToken dataNode = jObjectNode["data"].First;
								if (dataNode["name"] != null)
								{
									return dataNode["name"].ToString();
								}
							}
							return "";
						}*/
			return "";
		}

		public void RequestRemoveMessage(SuiBot_ChannelInstance channel, string messageID)
		{
			/*			string botId = GetUserId(m_BotName);
						if (botId == "")
						{
							ErrorLogging.WriteLine($"Can't obtain bot user id!");
							return;
						}

						var info = channel.StreamInformation;
						if (string.IsNullOrEmpty(info.ChannelID))
						{
							ErrorLogging.WriteLine($"Can't obtain id for channel: {channel.Channel}");
							return;
						}

						_ = HttpWebRequestHandlers.PerformDelete(new Uri($"https://api.twitch.tv/helix/moderation/chat?broadcaster_id={info.ChannelID}&moderator_id={botId}&message_id={messageID}"), BuildDefaultHeaders(), out string _);*/
		}

		public void RequestTimeout(SuiBot_ChannelInstance channel, string userId, uint length, string reason)
		{
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

		public void RequestBan(SuiBot_ChannelInstance channel, string userId, string reason)
		{
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

		internal async Task<bool> SubscribeTo_ChatMessage(string channel, string sessionId)
		{
			if (m_BotUserId == 0)
			{
				var info = await GetUserInfo(botInstance.BotName);
				if (info == null)
					return false;

				m_BotUserId = info.id;
			}

			var channelInfo = await GetUserInfo(channel);
			if (channelInfo == null)
				return false;


			var content = new SubscribeMSG_ReadChannelMessage(channelInfo.id, m_BotUserId, sessionId);
			var serilize = JsonConvert.SerializeObject(content, Formatting.Indented, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});

			var result = await HttpWebRequestHandlers.PostAsync(BASE_URI, "eventsub/subscriptions", "", serilize, BuildDefaultHeaders());
			if (result != null)
			{
				JToken deserializeObj = (JToken)JsonConvert.DeserializeObject(result);
				var data = deserializeObj["data"];
				if (data != null)
				{
					var cast = data.ToObject<EventSub.Subscription.Responses.Response_SubscribeToChannelMessages[]>();
					if (cast.Any(x => x.condition.broadcaster_user_id == channelInfo.id))
						return true;
					else
						return false;
				}
				else
					return false;
			}
			else
				return false;
		}
	}
}
