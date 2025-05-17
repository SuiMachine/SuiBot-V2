using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay(nameof(ES_ReconnectSession) + " {chatter_user_name}: {message.text}")]
	public class ES_ReconnectSession
	{
		public string id;
		public string status;
		public int? keepalive_timeout_seconds;
		public string reconnect_url;
		public DateTime connected_at;
	}
}
