namespace SuiBot_Core.API.EventSub.Subscription
{
	public class ES_Subscribe_Condition
	{
		public string broadcaster_user_id = null;
		public string user_id = null;
		public string moderator_user_id = null;
		private ES_Subscribe_Condition() { }

		public static ES_Subscribe_Condition CreateBroadcaster(string broadcaster_user_id)
		{
			return new ES_Subscribe_Condition()
			{
				broadcaster_user_id = broadcaster_user_id,
				user_id = null,
				moderator_user_id = null
			};
		}

		public static ES_Subscribe_Condition CreateBroadcasterAndUser(string broadcaster_user_id, string userID)
		{
			return new ES_Subscribe_Condition()
			{
				broadcaster_user_id = broadcaster_user_id,
				user_id = userID.ToString(),
				moderator_user_id = null,
			};
		}

		public static ES_Subscribe_Condition CreateBroadcasterAndModerator(string broadcaster_user_id, string moderator_id)
		{
			return new ES_Subscribe_Condition()
			{
				broadcaster_user_id = broadcaster_user_id,
				moderator_user_id = moderator_id,
				user_id = null
			};
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
