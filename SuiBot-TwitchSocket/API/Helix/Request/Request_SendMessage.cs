namespace SuiBot_Core.API.Helix.Request
{
	public class Request_SendChatMessage
	{
		public string broadcaster_id;
		public string sender_id;
		public string message;
		public string reply_parent_message_id;
		public bool? for_source_only;

		public static Request_SendChatMessage CreateMessage(string channelID, string botId, string message)
		{
			return new Request_SendChatMessage()
			{
				broadcaster_id = channelID,
				sender_id = botId,
				message = message,
				reply_parent_message_id = null,
				for_source_only = null
			};
		}

		public static object CreateResponse(string broadcaster_user_id, string m_BotUserId, string message_id, string message)
		{
			return new Request_SendChatMessage()
			{
				broadcaster_id = broadcaster_user_id,
				sender_id = m_BotUserId,
				message = message,
				reply_parent_message_id = message_id,
				for_source_only = null
			};
		}
	}
}
