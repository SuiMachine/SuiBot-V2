namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_StreamOffline
	{
		public string type = "stream.offline";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_StreamOffline() { }

		public SubscribeMSG_StreamOffline(ulong channelId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterOnly(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
