using SuiBot_TwitchSocket.API.EventSub;
using System;
using static SuiBot_Core.Components.Other.Gemini.Parameters;
using static SuiBotAI.Components.Other.Gemini.GeminiTools;

namespace SuiBot_Core.Components.Other.Gemini
{
	public abstract class FunctionCall
	{
		public abstract void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message);
	}

	[Serializable]
	public class TimeOutUser : FunctionCall
	{
		public double duration_in_seconds = 1;
		public string text_response = null;

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message)
		{
			if (message.UserRole >= ES_ChatMessage.Role.VIP)
			{
				channelInstance.UserTimeout(message, (uint)duration_in_seconds, text_response);
			}
		}
	}

	[Serializable]
	public class BanUser : FunctionCall
	{
		public string response = null;

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message)
		{
			if (message.UserRole >= ES_ChatMessage.Role.VIP)
			{
				channelInstance.UserBan(message, response);
			}
		}
	}

	public static class GeminiFunctionCall
	{
		public static GeminiFunction CreateBanFunction() => new GeminiFunction("ban", "bans a user", new BanParameters());
		public static GeminiFunction CreateTimeoutFunction() => new GeminiFunction("timeout", "time outs a user in the chat", new TimeOutParameters());
	}
}
