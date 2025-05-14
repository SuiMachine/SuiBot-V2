using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay(nameof(ES_ChatMessage) + " {chatter_user_name}: {message.text}")]
	[Serializable]
	public class ES_ChatMessage
	{
		/// <summary>
		/// Roles available on Twitch, where 0 is SuperMod and 5 is User
		/// </summary>
		public enum Role
		{
			SuperMod,
			Mod,
			VIP,
			Subscriber,
			User
		}

		[DebuggerDisplay(nameof(Message) + " {text}")]
		public class Message
		{
			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(Fragment) + " {text}")]
			public class Fragment
			{
				public string type;
				public string text;
				public API_ChatMessage_Fragment_Emote emote;
				public string mention;
			}

			[DebuggerDisplay(nameof(ES_ChatMessage) + "." + nameof(API_ChatMessage_Fragment_Emote) + " {id}")]
			public class API_ChatMessage_Fragment_Emote
			{
				public string id;
				public long emote_set_id;
				public long owner_id;
			}

			public string text;
			public Fragment[] fragments;
		}

		[DebuggerDisplay(nameof(Badge) + " {setinfo}")]
		public class Badge
		{
			public string set_id;
			public int id;
			public string info;
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
		public Badge[] badges = new Badge[0];
		public string message_type;
		public string cheer;
		public string reply;
		public string channel_points_custom_reward_id;
		public string channel_points_animation_id;
		[NonSerialized][JsonIgnore] public Role UserRole = Role.User;

		internal void SetupRole(SuiBot_ChannelInstance channel)
		{
			if (broadcaster_user_id == chatter_user_id)
			{
				UserRole = Role.SuperMod;
				return;
			}

			foreach (var badge in badges)
			{
				if (badge.set_id == "moderator")
				{
					UserRole = Role.Mod;
					return;
				}
				else if (badge.set_id == "vip" || badge.set_id == "artist")
				{
					UserRole = Role.VIP;
					return;
				}
				else if (badge.set_id == "subscriber")
				{
					UserRole = Role.Subscriber;
					return;
				}
			}

			if (channel != null)
			{
				if (channel.ConfigInstance.SuperMods.Contains(broadcaster_user_login))
				{
					UserRole = Role.SuperMod;
					return;
				}
			}
		}
	}
}
