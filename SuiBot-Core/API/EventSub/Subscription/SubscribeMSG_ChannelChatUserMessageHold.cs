namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_ChannelChatUserMessageHold
	{
		public string type = "channel.chat.user_message_hold";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_ChannelChatUserMessageHold() { }

		public SubscribeMSG_ChannelChatUserMessageHold(ulong channelId, ulong user, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndUserOnly(channelId, user);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
