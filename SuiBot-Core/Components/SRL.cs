using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace SuiBot_Core.Components
{
	internal static class SRL
	{
		static readonly Uri BaseApiURL = new Uri("http://api.speedrunslive.com:81/");

		public static void GetRaces(SuiBot_ChannelInstance ChannelInstance)
		{
			try
			{
				if (JsonGrabber.GrabJson(GetUri("races"), out string response))
				{
					string[] twitches = GetEntrantsTwitches(response, ChannelInstance.Channel);
					if (twitches != null)
					{
						ChannelInstance.SendChatMessage("http://kadgar.net/live/" + string.Join("/", twitches));
					}
					else
					{
						ChannelInstance.SendChatMessage("Nothing found");
					}
				}
			}
			catch
			{
				ChannelInstance.SendChatMessage("Some kind of error. Go, poke Sui to fix that");
			}
		}

		private static string[] GetEntrantsTwitches(string jsonTxt, string channel)
		{
			channel = channel.ToLower();
			var races = JObject.Parse(jsonTxt)["races"];
			foreach (var race in races)
			{
				int status = race["state"].ToObject<int>();
				var entrants = race["entrants"];
				if (status == 1 || status == 2 || status == 3 || status == 4)
				{
					foreach (var entrant in entrants)
					{
						var twitch = entrant.First["twitch"].Value<string>();
						if (twitch.ToLower() == channel)
						{
							List<string> twitches = new List<string>();
							foreach (var twitchEntrant in entrants)
							{
								if (twitchEntrant.First["twitch"].Value<string>() != "")
								{
									twitches.Add(twitchEntrant.First["twitch"].Value<string>());
								}
							}
							return twitches.ToArray();
						}
					}

				}
			}
			return null;
		}

		private static Uri GetUri(string op)
		{
			return new Uri(BaseApiURL, op);
		}
	}
}
