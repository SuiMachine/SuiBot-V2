namespace SuiBot_Core.API.EventSub.Subscription
{
	public class ES_Subscribe_Condition
	{
		public string broadcaster_user_id = null;
		public string user_id = null;
		public ES_Subscribe_Condition() { }

		public ES_Subscribe_Condition(long broadcaster_user_id, long userID)
		{
			this.broadcaster_user_id = broadcaster_user_id.ToString();
			this.user_id = userID.ToString();
		}
	}

	public class ES_Subscribe_Transport_Websocket
	{
		public string method = "websocket";
		public string session_id = "";

		public ES_Subscribe_Transport_Websocket()
		{
			method = "websocket";
			this.session_id = "";
		}

		public ES_Subscribe_Transport_Websocket(string session_id)
		{
			this.method = "websocket";
			this.session_id = session_id;
		}
	}
}
