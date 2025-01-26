using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuiBot_Core
{
	public class TwitchAPI
	{
		public Dictionary<string, string> UserNameToId = new Dictionary<string, string>();
		private readonly string m_ChannelName;
		private string m_BotName;
		public bool IsOnline { get; private set; } = true;
		public string Game { get; private set; } = "";
		public DateTime StartTime { get; private set; }
		private string m_OldId = "x";
		private readonly Uri sUrlTwitchStatus = null;
		private readonly Dictionary<string, string> RequestHeaders;

		//Used by Leaderboards
		public bool TitleHasChanged = false;
		public string OldTitle = "";

		//For testing
		public TwitchAPI(string channelName, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
			sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + channelName);
			this.m_ChannelName = channelName;
			RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
			RequestHeaders.Add("Authorization", "Bearer " + oauth);
		}

		public TwitchAPI(SuiBot_ChannelInstance suiBot_ChannelInstance, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
			this.m_BotName = suiBot_ChannelInstance.BotName;
			this.m_ChannelName = suiBot_ChannelInstance.Channel;
			sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + suiBot_ChannelInstance.Channel);

#if DEBUG && false  //testing
            this.channelName = "tezur0";
            sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + this.channelName);

#endif
			RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
			RequestHeaders.Add("Authorization", "Bearer " + oauth);
		}

		public void GetStatus()
		{
			if (HttpWebRequestHandlers.PerformGetRequest(sUrlTwitchStatus, RequestHeaders, out string res))
			{
				try
				{
					var response = JObject.Parse(res);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						var dataNode = response["data"].First;
						if (dataNode["title"] != null)
						{
							IsOnline = true;
							var title = dataNode["title"].ToString();
							TitleHasChanged = OldTitle != title;
							OldTitle = title;

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
						IsOnline = false;
						Console.WriteLine($"{m_ChannelName} - Checked stream status. Is offline.");
					}
				}
				catch (Exception e)
				{
					ErrorLogging.WriteLine("Error trying to parse Json when doing stream update request: " + e.Message);
					IsOnline = false;
				}
			}
			else
			{
				Console.WriteLine("Error checking Json");
			}
		}

		public string ResolveNameFromId(string id)
		{
			if (m_OldId == id)
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
			}

		}

		public void RequestRemoveMessage(string channel, string messageID)
		{
			string botId = GetUserId(m_BotName);
			if (botId == "")
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return;
			}

			string channelID = GetUserId(channel);
			if (channelID == "")
			{
				ErrorLogging.WriteLine($"Can't obtain id for channel: {channel}");
				return;
			}

			_ = HttpWebRequestHandlers.PerformDelete(new Uri($"https://api.twitch.tv/helix/moderation/chat?broadcaster_id={channelID}&moderator_id={botId}&message_id={messageID}"), RequestHeaders, out string _);
		}

		public void RequestTimeout(string channel, string userId, uint length, string reason)
		{
			var botId = GetUserId(m_BotName);
			if (botId == "")
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return;
			}

			var channelID = GetUserId(channel);
			if (channelID == "")
			{
				ErrorLogging.WriteLine($"Can't obtain id for channel: {channel}");
				return;
			}

			API_Structs.API_Data data = new API_Structs.API_Data()
			{
				data = new API_Structs.API_Timeout()
				{
					user_id = userId,
					duration = length == 0 ? 1 : length,
					reason = string.IsNullOrEmpty(reason) ? null : reason
				}
			};

			string dataStr = JsonConvert.SerializeObject(data);

			if (HttpWebRequestHandlers.PerformTwitchPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={channelID}&moderator_id={botId}"), RequestHeaders, dataStr, out string _))
			{

			}
		}

		public void RequestBan(string channel, string userId, string reason)
		{
			var botId = GetUserId(m_BotName);
			if (botId == "")
			{
				ErrorLogging.WriteLine($"Can't obtain bot user id!");
				return;
			}

			var channelID = GetUserId(channel);
			if (channelID == "")
			{
				ErrorLogging.WriteLine($"Can't obtain id for channel: {channel}");
				return;
			}

			API_Structs.API_Data data = new API_Structs.API_Data()
			{
				data = new API_Structs.API_Timeout()
				{
					user_id = userId,
					duration = 0,
					reason = string.IsNullOrEmpty(reason) ? null : reason
				}
			};

			string dataStr = JsonConvert.SerializeObject(data);

			if (HttpWebRequestHandlers.PerformTwitchPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={channelID}&moderator_id={botId}"), RequestHeaders, dataStr, out string _))
			{

			}
		}

		private string GetUserId(string userName)
		{
			if (UserNameToId.TryGetValue(userName, out string userId))
				return userId;
			else
			{
				if (HttpWebRequestHandlers.PerformGetRequest(new Uri($"https://api.twitch.tv/helix/users?login={userName}"), RequestHeaders, out string res))
				{
					var response = JObject.Parse(res);
					if (response["data"] != null && response["data"].Children().Count() > 0)
					{
						var dataNode = response["data"].First;
						if (dataNode["id"] != null)
						{
							//Just to make sure some other instance hasn't added it already
							if (!UserNameToId.ContainsKey(userName))
							{
								UserNameToId.Add(userName, dataNode["id"].ToString());
								return dataNode["id"].ToString();
							}
						}
					}
				}
			}

			return "";
		}

		public void RequestUpdate(SuiBot_ChannelInstance instance)
		{
			GetStatus();
			if (Game != string.Empty)
			{
				instance.SendChatMessage($"New isOnline status is - {IsOnline} and the game is: {Game}");
			}
			else
			{
				instance.SendChatMessage($"New isOnline status is - {IsOnline}");
			}
		}
	}
}
