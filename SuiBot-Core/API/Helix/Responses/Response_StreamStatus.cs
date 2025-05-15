using Newtonsoft.Json;
using System;

namespace SuiBot_Core.API.Helix.Responses
{
	[Serializable]
	public class Response_StreamStatus
	{
		public string id;
		public ulong user_id;
		public string user_login;
		public string user_name;
		public ulong game_id;
		public string game_name;
		public string type;
		public string title;
		public uint viewer_count;
		public DateTime started_at;
		public string language;
		public string thumbnail_url;
		public string[] tags_id;
		public string[] tags;
		public bool is_mature;

		[JsonIgnore][NonSerialized] public bool IsOnline = false;
		[JsonIgnore][NonSerialized] public bool GameChangedSinceLastTime = false;
	}
}
