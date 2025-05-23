﻿using System;
using System.Diagnostics;

namespace SuiBot_Core.Components.Other.Gemini.FunctionTypes
{
	public abstract class FunctionCall
	{
		public abstract void Perform(SuiBot_ChannelInstance channelInstance, ChatMessage message);
	}

	[DebuggerDisplay(nameof(TimeOutUser) + " {username} ({user_id}) for {duration_in_seconds} seconds")]
	[Serializable]
	public class TimeOutUser : FunctionCall
	{
		public string username = null;
		public double duration_in_seconds = 1;
		public string text_response = null;

		public override void Perform(SuiBot_ChannelInstance channelInstance, ChatMessage message)
		{
			if (message.UserRole >= SuiBot_Core.Role.VIP)
			{
				channelInstance.UserTimeout(message, (uint)duration_in_seconds, text_response);
			}
		}
	}

	[DebuggerDisplay(nameof(BanUser) + " {username} ({user_id})")]
	[Serializable]
	public class BanUser : FunctionCall
	{
		public string username = null;
		public string text_response = null;

		public override void Perform(SuiBot_ChannelInstance channelInstance, ChatMessage message)
		{
			if (message.UserRole >= SuiBot_Core.Role.VIP)
			{
				channelInstance.UserBan(message.UserID);
				
			}
		}
	}
}
