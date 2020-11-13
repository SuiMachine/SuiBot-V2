namespace SuiBot_Core
{
	/// <summary>
	/// Struct for handling Username, Message and Channel. Can be updated with Update(v,v,v) function.
	/// </summary>
	public struct ChatMessage
	{
		public Role UserRole;
		public string Username;
		public string Message;
		public bool HighlightedMessage;
		public string RewardID;

		/// <summary>
		/// Updates the ChatMessage object with new set of data. Can be done manually instead.
		/// </summary>
		/// <param name="UserRole">Role of a user.</param>
		/// <param name="Username">Name of a user.</param>
		/// <param name="Message">Message posted by a user.</param>
		public void Update(Role UserRole, string Username, string Message, bool HighlightedMessage = false, string RewardID = null)
		{
			this.UserRole = UserRole;
			this.Username = Username;
			this.Message = Message;
			this.HighlightedMessage = HighlightedMessage;
			this.RewardID = RewardID;
		}
	}

	/// <summary>
	/// Roles available on Twitch, where 0 is SuperMod and 5 is User
	/// </summary>
	public enum Role
	{
		SuperMod,
		Mod,
		VIP,    //previously trusted
		Subscriber,
		User
	}
}
