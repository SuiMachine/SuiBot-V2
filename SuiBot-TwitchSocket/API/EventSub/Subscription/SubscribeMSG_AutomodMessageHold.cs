namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_AutomodMessageHold
	{
		public string type = "automod.message.hold";
		public int version = 2;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_AutomodMessageHold() {}

		public SubscribeMSG_AutomodMessageHold(string channelId, string moderatorId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndModerator(channelId, moderatorId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
