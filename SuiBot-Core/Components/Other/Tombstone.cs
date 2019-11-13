using SuiBot_Core.Extensions.SuiStringExtension;

namespace SuiBot_Core.Components.Other
{
    class Tombstone : MemeCompnent
    {
        public override void DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
        {
            var chunks = lastMessage.Message.GetChunks('\"');
            if (chunks.Length > 4)
            {
                channelInstance.SendChatMessageResponse(lastMessage, "Up to 4 lines allowed!");
            }
            else if(chunks.Length < 1)
            {
                channelInstance.SendChatMessageResponse(lastMessage, "Tombstome requires at least 1 (and max. 4) lines. e.g. !tombstone \"Line1\" \"Blah line 2\" etc.");

            }
            else
            {
                var urlBuilder = string.Format("http://www.tombstonebuilder.com/generate.php?top1={0}&top2={1}&top3={2}&top4={3}",
                    chunks[0],
                    chunks.Length > 1 ? chunks[1].GetUrlSafeString() : "",
                    chunks.Length > 2 ? chunks[2].GetUrlSafeString() : "",
                    chunks.Length > 3 ? chunks[3].GetUrlSafeString() : ""
                    );
                channelInstance.SendChatMessageResponse(lastMessage, urlBuilder);
            }
        }
    }
}
