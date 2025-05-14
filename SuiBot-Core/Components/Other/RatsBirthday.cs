using SuiBot_Core.API.EventSub;

namespace SuiBot_Core.Components.Other
{
    class RatsBirthday : MemeComponent
    {
        public override bool DoWork(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
        {
            //Blame Jerma - https://www.youtube.com/watch?v=vdVnnMOTe3Q
            if (lastMessage.message.text.Contains(" "))
            {
                var target = lastMessage.message.text.Split(new char[] { ' ' }, 2)[1];
                if (target != " ")
                {
                    channelInstance.SendChatMessageResponse(lastMessage, string.Format("🐀 Rats rats we are the rats - Celebrating yet another birthday bash! {0} it's your birthday today - Cake and Ice cream is on it's way! (and) {0} been such a good boy this year - Open up your gifts while we all cheer! 🐀",
                        target));
                }
                else
                    channelInstance.SendChatMessageResponse(lastMessage, "!RatsBirthday requires a target to be specified after \"spacebar\", e.g. !ratsbirthday SuiBot");
            }
            else
            {
                channelInstance.SendChatMessageResponse(lastMessage, "!RatsBirthday requires a target to be specified after \"spacebar\", e.g. !ratsbirthday SuiBot");
            }
            return true;
        }
    }
}
