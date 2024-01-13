using System;

namespace SuiBot_Core.Components
{
	public class GenericUtil
	{
		private SuiBot_ChannelInstance ChannelInstance;
		private TwitchStatusUpdate TwitchUpdateInstance;

		public GenericUtil(SuiBot_ChannelInstance ChannelInstance, TwitchStatusUpdate TwitchUpdateInstance)
		{
			this.ChannelInstance = ChannelInstance;
			this.TwitchUpdateInstance = TwitchUpdateInstance;
		}

		public void GetUpTime(ChatMessage LastMessage)
		{
			var startTime = this.TwitchUpdateInstance.StartTime;
			if (startTime != DateTime.MinValue)
			{
				var lenght = DateTime.UtcNow - startTime;
				ChannelInstance.SendChatMessageResponse(LastMessage, "Stream has been online for: " +
					(lenght.Days > 0 ? lenght.Days.ToString() + "d " : "") +
					(lenght.Hours > 0 ? lenght.Hours.ToString("") + "h " : "") +
					lenght.Minutes.ToString("00") + "m " +
					lenght.Seconds.ToString("00") + "s");
				return;
			}
			else
			{
				ChannelInstance.SendChatMessageResponse(LastMessage, "According to API, stream is offline");
				return;
			}
		}

		public void Shoutout(ChatMessage LastMessage)
		{
			if (LastMessage.UserRole <= Role.Mod)
			{
				if (LastMessage.Message.Contains(" "))
				{
					var split = LastMessage.Message.Split(new char[] { ' ' }, 2)[1].Trim();
					if (split != "")
					{
						ChannelInstance.UserShoutout(split);
					}
					else
						ChannelInstance.SendChatMessageResponse(LastMessage, "Invalid syntax! Needs to be \"!so  username\"");
				}
				else
					ChannelInstance.SendChatMessageResponse(LastMessage, "Invalid syntax! Needs to be \"!so  username\"");
			}
		}
	}
}
