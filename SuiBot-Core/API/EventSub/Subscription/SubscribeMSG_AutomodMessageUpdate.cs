namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_AutomodMessageUpdate
	{
		public string type = "automod.message.update";
		public int version = 2;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_AutomodMessageUpdate() { }

		public SubscribeMSG_AutomodMessageUpdate(ulong channelId, ulong user, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndUserOnly(channelId, user);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
