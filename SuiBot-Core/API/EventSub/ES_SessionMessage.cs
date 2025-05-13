using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay(nameof(ES_SessionMessage) + " {status}")]
	[Serializable]
	internal class ES_SessionMessage
	{
		public enum SessionStatus
		{
			invalid,
			connected
		}

		public string id = "";
		[JsonConverter(typeof(StringEnumConverter))] public SessionStatus status = SessionStatus.invalid;
		public DateTime connected_at = DateTime.MinValue;
		public int keepalive_timeout_seconds = 10;
		public string reconnect_url = null;
		public string recovery_url = null;
	}
}
