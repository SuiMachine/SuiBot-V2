using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay(nameof(ES_Suspicious_UserMessage) + " {user_name}: {message.text}")]
	public class ES_Suspicious_UserMessage
	{
		public class Message
		{
			public string message_id;
			public string text;
		}

		public ulong broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
		public ulong user_id;
		public string user_name;
		public string user_login;
		public string low_trust_status;
		public ulong[] shared_ban_channel_ids;
		public string[] types;
		public string ban_evasion_evaluation;
		public Message message;

		public ES_ChatMessage ConvertToChatMessage()
		{
			return new ES_ChatMessage()
			{
				broadcaster_user_id = broadcaster_user_id,
				broadcaster_user_name = broadcaster_user_name,
				broadcaster_user_login = broadcaster_user_login,
				channel_points_animation_id = null,
				reply = null,
				UserRole = ES_ChatMessage.Role.User,
				chatter_user_id = user_id,
				chatter_user_name = user_name,
				chatter_user_login = user_login,
				channel_points_custom_reward_id = null,
				message_id = message.message_id,
				message_type = "",
				badges = null,
				cheer = null,
				color = "#FFFFFF",
				source_message_id = null,
				message = new ES_ChatMessage.Message()
				{
					text = message.text,
					fragments = new ES_ChatMessage.Message.Fragment[0]
				}
			};
		}
	}
}
