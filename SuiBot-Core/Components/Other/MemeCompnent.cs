using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components.Other
{
    public class MemeCompnent
    {
        public MemeCompnent() { }

        public virtual void DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
        {
            channelInstance.SendChatMessageResponse(lastMessage, "This method is suppose to be overriden with child method! GET GUD!");
        }
    }
}
