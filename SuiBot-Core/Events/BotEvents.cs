using SuiBot_TwitchSocket.API.EventSub;

namespace SuiBot_Core.Events
{
	public delegate void OnChannelStatusUpdateHandler(string channel, bool IsOnline, string game);
	public delegate void OnChannelJoiningHandler(string channel);
	public delegate void OnChannelLeavingHandler(string channel);
	public delegate void OnChatSendMessageHandler(string channel, string message);
	public delegate void OnChatMessageReceivedHandler(string channel, ES_ChatMessage message);
	public delegate void OnLoginRegisteredHandler(string text);
	public delegate void OnModerationActionHandler(string channel, string user, string response, string duration);
	public delegate void OnShutdownHandler();
}
