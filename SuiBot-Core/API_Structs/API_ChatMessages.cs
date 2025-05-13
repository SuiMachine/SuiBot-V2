using System;

namespace SuiBot_Core.API_Structs
{
	[Serializable]
	public class API_ChatMessage
	{
		public class API_ChatMessage_Fragment
		{
			public string type;
			public string text;
			public API_ChatMessage_Fragment_Emote emote;
			public string mention;
		}

		public class API_ChatMessage_Fragment_Emote
		{
			public long id;
			public long emote_set_id;
			public long owner_id;
		}


		public string text;
		public API_ChatMessage_Fragment[] fragments;
	}
}
