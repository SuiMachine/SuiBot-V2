using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Events
{
    public enum IrcFeedback
    {
        Connecting,
        Connected,
        Verified,
        Disconnected,
        Error
    }

    public delegate void OnIrcFeedbackHandler(IrcFeedback feedback, string message);
    public delegate void OnChannelStatusUpdateHandler(string channel, bool IsOnline, string game);
    public delegate void OnChannelJoiningHandler(string channel);
    public delegate void OnChannelLeavingHandler(string channel);
    public delegate void OnChatSendMessageHandler(string channel, string message);
    public delegate void OnChatMessageReceivedHandler(string channel, ChatMessage message);
    public delegate void OnLoginRegisteredHandler(string text);
    public delegate void OnModerationActionHandler(string channel, string user, string response, string duration);
    public delegate void OnShutdownHandler();
}
