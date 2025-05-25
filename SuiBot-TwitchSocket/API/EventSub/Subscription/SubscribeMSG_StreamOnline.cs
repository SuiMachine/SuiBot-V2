namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_StreamOnline
	{
		public string type = "stream.online";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_StreamOnline() { }

		public SubscribeMSG_StreamOnline(string channelId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcaster(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
