namespace SuiBot_Core.API.Helix.Request
{
	public class Request_SendChatMessage
	{
		public string broadcaster_id;
		public string sender_id;
		public string message;
		public string reply_parent_message_id;
		public bool? for_source_only;

		public static Request_SendChatMessage CreateMessage(ulong channel, ulong botId, string message)
		{
			return new Request_SendChatMessage()
			{
				broadcaster_id = channel.ToString(),
				sender_id = botId.ToString(),
				message = message,
				reply_parent_message_id = null,
				for_source_only = null
			};
		}

		public static object CreateResponse(ulong broadcaster_user_id, ulong m_BotUserId, string message_id, string message)
		{
			return new Request_SendChatMessage()
			{
				broadcaster_id = broadcaster_user_id.ToString(),
				sender_id = m_BotUserId.ToString(),
				message = message,
				reply_parent_message_id = message_id,
				for_source_only = null
			};
		}
	}
}
