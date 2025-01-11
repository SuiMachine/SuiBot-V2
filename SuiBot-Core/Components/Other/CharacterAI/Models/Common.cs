using System;

namespace CharacterAi.Client.Models.Common
{
    //Adopted from https://github.com/drizzle-mizzle/CharacterAI-Net-Client/tree/master
    //See https://github.com/drizzle-mizzle/CharacterAI-Net-Client/blob/master/LICENSE

    public class AuthorizedUser
	{
		public string Token { get; set; }
		public string UserId { get; set; }
		public string Username { get; set; }
		public string UserEmail { get; set; }
		public string UserImageUrl { get; set; } // uploaded/...
	}

    public class CaiCharacter
    {
        public string external_id { get; set; }
        public string participant__name { get; set; }

        public string title { get; set; }
        public string description { get; set; }
        public string definition { get; set; }
        public string greeting { get; set; }
        public string avatar_file_name { get; set; }

        public int? participant__num_interactions { get; set; }
        public bool img_gen_enabled { get; set; } = false;

        public int priority { get; set; }
        public string user__username { get; set; }
        public string visibility { get; set; }
    }

    public class CaiChat
    {
        public string character_avatar_uri { get; set; }
        public string character_id { get; set; }
        public string character_name { get; set; }
        // public List<CharacterTranslation>? character_translations { get; set; }
        public string character_visibility { get; set; }
        public Guid chat_id { get; set; }
        public DateTime? create_time { get; set; }
        public int creator_id { get; set; }
        public string state { get; set; }
        public string type { get; set; }
        public string visibility { get; set; }
    }

    public class CaiChatShort
    {
        public string character_id { get; set; }
        public Guid chat_id { get; set; }
        public string creator_id { get; set; }
        public string type { get; set; }
        public string visibility { get; set; }
    }
}


