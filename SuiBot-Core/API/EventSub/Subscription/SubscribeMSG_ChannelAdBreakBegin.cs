namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_ChannelAdBreakBegin
	{
		public string type = "channel.ad_break.begin";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_ChannelAdBreakBegin() { }

		public SubscribeMSG_ChannelAdBreakBegin(ulong channelId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterOnly(channelId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
