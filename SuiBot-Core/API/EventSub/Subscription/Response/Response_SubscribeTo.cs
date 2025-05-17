using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub.Subscription.Responses
{
	[DebuggerDisplay(nameof(Response_SubscribeTo) + " {data[0].@type} - {total_cost} / {max_total_cost}")]
	public class Response_SubscribeTo
	{
		[DebuggerDisplay(nameof(Subscription_Response_Data) + " {type}: {status}")]
		public class Subscription_Response_Data
		{
			public class Condition
			{
				public ulong? broadcaster_user_id;
				public ulong? user_id;
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

		public Subscription_Response_Data[] data;
		public int total;
		public int max_total_cost;
		public int total_cost;

		public void PerformCostCheck()
		{
			//Not sure if this is how it works
			if (total_cost > max_total_cost)
				throw new Exception("Subscribed to too many things!");
		}
	}
}
