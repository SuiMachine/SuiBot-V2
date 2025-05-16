using System;
using System.Diagnostics;

namespace SuiBot_Core.API.EventSub
{
    [DebuggerDisplay(nameof(ES_AutomodMessageHold) + " {user_name}: {message}")]
	public class ES_AutomodMessageHold
	{
        public ulong broadcaster_user_id;
        public string broadcaster_user_name;
        public string broadcaster_user_login;
        public ulong user_id;
        public string user_name;
        public string user_login;
        public string message_id;
        public ES_ChatMessage.Message message;
        public int level;
        public string category;
        public DateTime held_at;
	}
}
