namespace SuiBot_Core.Components.Other
{
	public class MemeComponent
    {
        public MemeComponent() { }

        public virtual void DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
        {
            channelInstance.SendChatMessageResponse(lastMessage, "This method is suppose to be overriden with child method! GET GUD!");
        }
    }
}
