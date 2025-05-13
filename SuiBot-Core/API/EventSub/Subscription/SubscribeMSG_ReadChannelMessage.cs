namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_ReadChannelMessage
	{
		public string type = "channel.chat.message";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_ReadChannelMessage(){}

		public SubscribeMSG_ReadChannelMessage(long channelId, long user,  string sessionID)
		{
			condition = new ES_Subscribe_Condition(channelId, user);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
