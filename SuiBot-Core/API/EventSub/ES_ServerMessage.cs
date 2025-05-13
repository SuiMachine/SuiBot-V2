using Newtonsoft.Json.Linq;
using System;

namespace SuiBot_Core.API.EventSub
{
	[Serializable]
	public class ES_ServerMessage
	{
		public ES_Metadata metadata;
		public JToken payload;
	}
}
