using System;
using System.Diagnostics;

namespace SuiBot_Core.API.Helix.Responses
{
	[Serializable]
	[DebuggerDisplay(nameof(Response_GetUserInfo) + " {login} ({id})")]
	public class Response_GetUserInfo
	{
		public string id;
		public string login;
		public string display_name;
		public string type;
		public string broadcaster_type;
		public string description;
		public string profile_image_url;
		public string offline_image_url;
		public int view_count;
		public DateTime created_at;
	}
}
