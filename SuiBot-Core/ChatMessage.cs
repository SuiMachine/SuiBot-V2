namespace SuiBot_Core
{
	/// <summary>
	/// Struct for handling Username, Message and Channel. Can be updated with Update(v,v,v) function.
	/// </summary>
	public struct ChatMessage
	{
		public Role UserRole;
		public string MessageID;
		public string DisplayName;
		public string Username;
		public string UserID;
		public string Message;
		public bool HighlightedMessage;
		public bool IsFirstMessage;
		public string RewardID;

		public ChatMessage(string MessageID, Role UserRole, string DisplayName, string Username, string UserID, string Message, bool IsFirstMessage, bool HighlightedMessage = false, string RewardID = null)
		{
			this.MessageID = MessageID;
			this.UserRole = UserRole;
			this.DisplayName = DisplayName;
			this.Username = Username;
			this.UserID = UserID;
			this.Message = Message;
			this.HighlightedMessage = HighlightedMessage;
			this.RewardID = RewardID;
			this.IsFirstMessage = IsFirstMessage;
		}
	}

	/// <summary>
	/// Roles available on Twitch, where 0 is SuperMod and 5 is User
	/// </summary>
	public enum Role
	{
		SuperMod,
		Mod,
		VIP,
		Subscriber,
		User
	}
}
