using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SuiBot_Core.Components
{
    class ClipLogger
    {
        SuiBot_ChannelInstance ChannelInstance { get; set; }
        string clipUrl { get; set; }


        public ClipLogger(SuiBot_ChannelInstance ChannelInstance)
        {
            this.ChannelInstance = ChannelInstance;
            clipUrl = string.Format("https://www.twitch.tv/{0}/clip/", ChannelInstance.Channel).ToLower();

            //Create a file just in case
            if (!File.Exists(string.Format("Bot/Channels/{0}/Clips.txt", ChannelInstance.Channel)))
                File.WriteAllText(string.Format("Bot/Channels/{0}/Clips.txt", ChannelInstance.Channel), "");
        }

        public void DoWork(ChatMessage lastMessage)
        {
            var words = lastMessage.Message.Split(' ');

            var twitchClip = words.FirstOrDefault(x => x.StartsWith("https://clips.twitch.tv/", StringComparison.CurrentCultureIgnoreCase) || x.StartsWith(clipUrl, StringComparison.CurrentCultureIgnoreCase));

            if(twitchClip != null)
            {
                twitchClip = twitchClip.ToLower();
                var clips = File.ReadAllLines(string.Format("Bot/Channels/{0}/Clips.txt", ChannelInstance.Channel)).ToList();
                if (!clips.Contains(twitchClip))
                {
                    clips.Add(twitchClip);
                    File.WriteAllLines(string.Format("Bot/Channels/{0}/Clips.txt", ChannelInstance.Channel), clips);
                }
            }
        }
    }
}
