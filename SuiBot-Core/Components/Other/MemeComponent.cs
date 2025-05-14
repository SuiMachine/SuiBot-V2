using SuiBot_Core.API.EventSub;

namespace SuiBot_Core.Components.Other
{
	public abstract class MemeComponent
    {
        public MemeComponent() { }

        public abstract bool DoWork(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage);
    }
}
