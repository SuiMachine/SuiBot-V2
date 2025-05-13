using System;

namespace SuiBot_Core.API.EventSub.Subscription.Responses
{

	[Serializable]
	public class Response_SubscribeToChannelMessages
	{
		[Serializable]
		public class Condition
		{
			public long broadcaster_user_id;
			public long user_id;
		}

		public string id;
		public string status;
		public string type;
		public int version;
		public Condition condition;
		public DateTime created_at;
		public Helix.Responses.UniversalResponse_Transport transport;
		public int cost;
	}
}
