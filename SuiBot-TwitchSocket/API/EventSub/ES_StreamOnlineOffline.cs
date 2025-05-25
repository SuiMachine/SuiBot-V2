using System;

namespace SuiBot_Core.API.EventSub
{
	public class ES_StreamOnline
	{
        public enum StreamType
        {
            live,
            playlist,
            watch_party,
            premiere,
            rerun
        }

        public string id;
        public ulong broadcaster_user_id;
        public string broadcaster_user_name;
        public string broadcaster_user_login;
        public StreamType type;
        //public string description;
        public DateTime started_at;
		//"id": "12345-cool-event",
  //      "broadcaster_user_id": "141981764",
  //      "broadcaster_user_name": "TwitchDev",
  //      "broadcaster_user_login": "twitchdev",
  //      "type": "subscription",
  //      "description": "Help me get partner!",
  //      "current_amount": 100,
  //      "target_amount": 220,
  //      "started_at": "2021-07-15T17:16:03.17106713Z"
	}

	public class ES_StreamOffline
	{
		public ulong broadcaster_user_id;
		public string broadcaster_user_name;
		public string broadcaster_user_login;
	}
}
