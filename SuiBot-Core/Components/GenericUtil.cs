using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components
{
    public class GenericUtil
    {
        private SuiBot_ChannelInstance ChannelInstance;
        private TwitchStatusUpdate TwitchUpdateInstance;

        public GenericUtil(SuiBot_ChannelInstance ChannelInstance, TwitchStatusUpdate TwitchUpdateInstance)
        {
            this.ChannelInstance = ChannelInstance;
            this.TwitchUpdateInstance = TwitchUpdateInstance;
        }

        public void GetUpTime(ChatMessage LastMessage)
        {
            var startTime = this.TwitchUpdateInstance.StartTime;
            if(startTime != DateTime.MinValue)
            {
                var lenght = DateTime.UtcNow - startTime;
                ChannelInstance.SendChatMessageResponse(LastMessage, "Stream has been online for: " +
                    (lenght.Days > 0 ? lenght.Days.ToString() + "d " : "") +
                    (lenght.Hours > 0 ? lenght.Hours.ToString("") + "h " : "") +
                    lenght.Minutes.ToString("00") + "m "+
                    lenght.Seconds.ToString("00") + "s");
                return;
            }
            else
            {
                ChannelInstance.SendChatMessageResponse(LastMessage, "According to API, stream is offline");
                return;
            }
        }
    }
}
