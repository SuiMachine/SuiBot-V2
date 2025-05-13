using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay(nameof(ES_ChatMessage) + " {chatter_user_name}: {message.text}")]
	[Serializable]
	public class ES_ChatMessage
	{
		public class Message
		{
			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(Fragment) + "{text}")]
			public class Fragment
			{
				public string type;
				public string text;
				public API_ChatMessage_Fragment_Emote emote;
				public string mention;
			}

			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(API_ChatMessage_Fragment_Emote) + "{id}")]
			public class API_ChatMessage_Fragment_Emote
			{
				public string id;
				public long emote_set_id;
				public long owner_id;
			}

			public string text;
			public Fragment[] fragments;
		}

		public ulong broadcaster_user_id;
		public string broadcaster_user_login;
		public string broadcaster_user_name;
		public ulong chatter_user_id;
		public string chatter_user_login;
		public string chatter_user_name;
		public string message_id;
		public string source_message_id;
		public Message message;
		public string color;
		public string message_type;
		public string cheer;
		public string reply;
		public string channel_points_custom_reward_id;
		public string channel_points_animation_id;
	}
}
