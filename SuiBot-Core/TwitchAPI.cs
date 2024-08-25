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
		readonly string channelName;
		private string botName;
		public bool isOnline = true;
		public string game = "";
		public DateTime StartTime;
		private string oldId = "x";
		public uint LastViewers = 0;
		readonly Uri sUrlTwitchStatus = null;
		readonly Dictionary<string, string> RequestHeaders;

		//Used by Leaderboards
		public bool TitleHasChanged = false;
		public string OldTitle = "";

		//For testing
		public TwitchAPI(string channelName, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
			sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + channelName);
			this.channelName = channelName;
			RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
			RequestHeaders.Add("Authorization", "Bearer " + oauth);
		}

		public TwitchAPI(SuiBot_ChannelInstance suiBot_ChannelInstance, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
			this.botName = suiBot_ChannelInstance.BotName;
			this.channelName = suiBot_ChannelInstance.Channel;
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
							isOnline = true;
							var title = dataNode["title"].ToString();
							TitleHasChanged = OldTitle != title;
							OldTitle = title;

							if (dataNode["type"] != null)
							{
								var streamType = dataNode["type"].ToString();
								if (streamType == "live")
									isOnline = true;
								else
									isOnline = false;
							}

							if (dataNode["started_at"] != null)
							{
								var dateTimeStartAsstring = dataNode["started_at"].ToString();
								//"2020-02-25t13:42:32z"
								if (DateTime.TryParse(dateTimeStartAsstring, out DateTime ParsedTime))
								{
									StartTime = ParsedTime.ToUniversalTime();
								}
								else
									StartTime = DateTime.MinValue;
							}

							if (dataNode["game_id"] != null)
							{
								var gameIdAsString = dataNode["game_id"].ToString();
								game = ResolveNameFromId(gameIdAsString);
								if (game == "ul")
								{
									game = String.Empty;
								}
								Console.WriteLine(string.Format("{0} - Checked stream status. Is online, playing {1}", channelName, game));
							}
							else
							{
								Console.WriteLine(string.Format("{0} - Checked stream status. Is online (no game?)", channelName));
							}

							if (dataNode["viewer_count"] != null)
							{
								var viewers = dataNode["viewer_count"].ToString();
								if (uint.TryParse(viewers, out uint Value))
								{
									this.LastViewers = Value;
								}
							}

						}
						else
						{
							isOnline = false;
							Console.WriteLine(string.Format("{0} - Checked stream status. Is offline.", channelName));
						}
					}
					else
					{
						isOnline = false;
						Console.WriteLine(string.Format("{0} - Checked stream status. Is offline.", channelName));
					}
				}
				catch (Exception e)
				{
					ErrorLogging.WriteLine("Error trying to parse Json when doing stream update request: " + e.Message);
					isOnline = false;
				}
			}
			else
			{
				Console.WriteLine("Error checking Json");
			}
		}

		public string ResolveNameFromId(string id)
		{
			if (oldId == id)
			{
				return game;
			}
			else
			{
				if (HttpWebRequestHandlers.PerformGetRequest(new Uri("https://api.twitch.tv/helix/games?id=" + id), RequestHeaders, out string res))
				{
					oldId = id;
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
			string botId = GetUserId(botName);
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
			var botId = GetUserId(botName);
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

			if (HttpWebRequestHandlers.PerformPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={channelID}&moderator_id={botId}"), RequestHeaders, dataStr, out string _))
			{

			}
		}

		public void RequestBan(string channel, string userId, string reason)
		{
			var botId = GetUserId(botName);
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

			if (HttpWebRequestHandlers.PerformPost(new Uri($"https://api.twitch.tv/helix/moderation/bans?broadcaster_id={channelID}&moderator_id={botId}"), RequestHeaders, dataStr, out string _))
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
			if (game != string.Empty)
			{
				instance.SendChatMessage("New isOnline status is - \'" + isOnline.ToString() + "\' and the game is: " + game);
			}
			else
			{
				instance.SendChatMessage("New isOnline status is - " + isOnline.ToString());
			}
		}
	}
}
