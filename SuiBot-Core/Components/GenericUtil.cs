using SuiBot_Core.API;
using SuiBot_Core.API.EventSub;
using System;
using static SuiBot_Core.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core.Components
{
	public class GenericUtil
	{
		private SuiBot_ChannelInstance ChannelInstance;

		public GenericUtil(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
		}

		public void GetUpTime(ES_ChatMessage LastMessage)
		{
			DateTime startTime = this.ChannelInstance.StreamStatus.started_at;
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

		public void Shoutout(ES_ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				if (lastMessage.message.text.Contains(" "))
				{
					var split = lastMessage.message.text.Split(new char[] { ' ' }, 2)[1].Trim();
					if (split != "")
						ChannelInstance.UserShoutout(lastMessage, split);
					else
						ChannelInstance.SendChatMessageResponse(lastMessage, "Invalid syntax! Needs to be \"!so username\"");
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Invalid syntax! Needs to be \"!so username\"");
			}
		}
	}
}
