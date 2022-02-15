using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SuiBot_Core
{
	public class TwitchStatusUpdate
	{
		readonly string channelName;
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
		public TwitchStatusUpdate(string channelName, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
			sUrlTwitchStatus = new Uri("https://api.twitch.tv/helix/streams?user_login=" + channelName);
			this.channelName = channelName;
			RequestHeaders.Add("Client-ID", "rmi9m0sheo4pp5882o8s24zu7h09md");
			RequestHeaders.Add("Authorization", "Bearer " + oauth);
		}

		public TwitchStatusUpdate(SuiBot_ChannelInstance suiBot_ChannelInstance, string oauth)
		{
			RequestHeaders = new Dictionary<string, string>();
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
			if (JsonGrabber.GrabJson(sUrlTwitchStatus, RequestHeaders, "application/json", "application/vnd.twitchtv.v3+json", "GET", out string res))
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
				if (JsonGrabber.GrabJson(new Uri("https://api.twitch.tv/helix/games?id=" + id), RequestHeaders, "application/json", "application/vnd.twitchtv.v3+json", "GET", out string res))
				{
					oldId = id;
					var jObjectNode = JObject.Parse(res);
					var dataNode = jObjectNode["data"].First;
					if (dataNode["name"] != null)
					{
						return dataNode["name"].ToString();
					}
				}
				return "";
			}

		}

		internal void RequestUpdate(SuiBot_ChannelInstance instance)
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
