using SuiBot_Core.API.EventSub;
using SuiBot_Core.API.Helix.Responses;

namespace SuiBot_TwitchSocket.Interfaces
{
	public interface IBotInstance
	{
		bool GetChannelInstanceUsingLogin(string login, out IChannelInstance channel);
		void TwitchSocket_Connected();
		void TwitchSocket_Disconnected();
		void TwitchSocket_ClosedViaSocket();
		void TwitchSocket_ChatMessage(ES_ChatMessage chatMessage);
		void TwitchSocket_StreamWentOnline(ES_StreamOnline onlineData);
		void TwitchSocket_StreamWentOffline(ES_StreamOffline offlineData);
		void TwitchSocket_AutoModMessageHold(ES_AutomodMessageHold messageHold);
		void TwitchSocket_SuspiciousMessageReceived(ES_Suspicious_UserMessage suspiciousMessage);

	}

	public interface IChannelInstance
	{
		string ChannelID { get; set; }
		string Channel { get; set; }
		Response_StreamStatus StreamStatus { get; set; }
		bool IsSuperMod(string username);
		void SendChatMessage(string message);
	}
}
