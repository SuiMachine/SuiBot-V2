namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_ReadChannelMessage
	{
		public string type = "channel.chat.message";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		private SubscribeMSG_ReadChannelMessage(){}

		public SubscribeMSG_ReadChannelMessage(string channelId, string user,  string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndUser(channelId, user);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
