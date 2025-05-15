namespace SuiBot_Core.API.EventSub.Subscription
{
	internal class SubscribeMSG_ChannelChatUserMessageUpdate
	{
		public string type = "channel.chat.user_message_update";
		public int version = 1;
		public ES_Subscribe_Condition condition;
		public ES_Subscribe_Transport_Websocket transport;

		public SubscribeMSG_ChannelChatUserMessageUpdate() { }

		public SubscribeMSG_ChannelChatUserMessageUpdate(ulong channelId, ulong user, string sessionID)
		{
			condition = ES_Subscribe_Condition.CreateBroadcasterAndUserOnly(channelId, user);
			transport = new ES_Subscribe_Transport_Websocket(sessionID);
		}
	}
}
