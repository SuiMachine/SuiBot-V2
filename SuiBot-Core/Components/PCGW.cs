using Newtonsoft.Json.Linq;
using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using System.Linq;

namespace SuiBot_Core.Components
{
	internal class PCGW
	{
		private const string PCGW_API_URI = "https://www.pcgamingwiki.com/w/api.php";

		private SuiBot_ChannelInstance ChannelInstance;

		public PCGW(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
		}

		internal void DoWork(ES_ChatMessage lastMessage)
		{
			var msg = lastMessage.message.text.StripSingleWord();

			if (msg == "")
			{
				if (string.IsNullOrEmpty(ChannelInstance.StreamStatus.game_name))
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, "Current game is empty. You can try providing the game manually by using command \"!pcgw Game Name\"");
					return;
				}
				else
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, GetPCGWUrl(ChannelInstance.StreamStatus.game_name));
					return;
				}

			}
			else
			{
				ChannelInstance.SendChatMessageResponse(lastMessage, GetPCGWUrl(msg));
				return;
			}
		}

		internal string GetPCGWUrl(string GameName)
		{
			try
			{
				if (HttpWebRequestHandlers.GrabJson(HttpWebRequestHandlers.BuildRequestUri(PCGW_API_URI, new string[] {
					HttpWebRequestHandlers.FormatParameter("action", "ask"),
					HttpWebRequestHandlers.FormatParameter("format", "json"),
					HttpWebRequestHandlers.FormatParameter("query", "[[Category:Games]] [[" + GameName + "]]")
				}), out string result))
				{
					if (result != "")
					{
						var response = JObject.Parse(result);
						if (response["query"] != null)
						{
							var queryData = response["query"];
							if (queryData["results"] != null)
							{
								var resultData = queryData["results"];
								if (resultData.Count() > 0)
								{
									return resultData.First.First["fullurl"].ToString();
								}
								else
									return "Ask query returned no results. You may want to try: " + string.Format("https://www.pcgamingwiki.com/w/index.php?search={0}", Uri.EscapeDataString(GameName));
							}
							else
								return "Query returned no result node! Something is wrong...";
						}
						else
							return "Query returned with no \"Query\" node in response!";
					}
					else
						return "Response was empty!";
				}
				else
					return "Failed to get response from PCGW.";

			}
			catch (Exception e)
			{
				ErrorLogging.WriteLine("[ERROR WITH PCGW REQUEST] - " + e.Message);
				return "Exception thrown when trying to query PCGW. See log file.";
			}
		}
	}
}
