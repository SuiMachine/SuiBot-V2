using System;

namespace SuiBot_Core.Components
{
	public class StreamInformation
	{
		public readonly Uri HelixURI;
		public string ChannelID { get; private set; }
		public StreamInformation(string channel)
		{
			HelixURI = new Uri("https://api.twitch.tv/helix/streams?user_login=" + channel);
		}

		public string StreamTitle { get; private set; }
		public string Game { get; private set; }
		public bool HasGameChanged(string newID)
		{
			return newID != GameID;
		}
		public string GameID { get; private set; }

		public void SetGameID(string ID) => GameID = ID;

		internal void SetIsOnline(bool v) => IsOnline = v;

		public bool IsOnline { get; private set; }
		public DateTime StreamStartTime { get; internal set; }
	}
}
