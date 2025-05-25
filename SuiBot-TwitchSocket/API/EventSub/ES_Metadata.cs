using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
	[DebuggerDisplay("EventSub_Metadata - {message_type} ({message_timestamp}) - {message_id}")]
	[Serializable]
	public class ES_Metadata
	{
		public string message_id;
		[JsonConverter(typeof(StringEnumConverter))] public EventSub_MessageType message_type;
		public DateTime message_timestamp;
		public string subscription_type = null;
		public int? subscription_version = null;
	}
}
