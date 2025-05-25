namespace SuiBot_Core.API.EventSub.Subscription
{
	public class SubscribeMSG_ChannelSuspiciousUserMessage
	{
		public string type = "channel.suspicious_user.message";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_ChannelSuspiciousUserMessage() { }

		public SubscribeMSG_ChannelSuspiciousUserMessage(string channelId, string moderatorId, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndModerator(channelId, moderatorId);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
