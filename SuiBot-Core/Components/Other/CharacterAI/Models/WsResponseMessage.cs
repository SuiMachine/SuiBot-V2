﻿using CharacterAi.Client.Models.Common;
using System;

namespace CharacterAi.Client.Models.WS
{
    public class WsResponseMessage
    {
        public string command { get; set; }
        public string comment { get; set; }
        public Guid request_id { get; set; }
        public Turn turn { get; set; }

        public ChatInfo chat_info { get; set; }
        public CaiChat chat { get; set; }
    }

    public class ChatInfo
    {
        public string type { get; set; }

    }
}