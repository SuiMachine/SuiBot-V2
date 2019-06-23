using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components.Other
{
    class KickPeople
    {
        SuiBot_ChannelInstance ChannelInstance { get; set; }

        public KickPeople (SuiBot_ChannelInstance channelInstance)
        {
            this.ChannelInstance = channelInstance;
        }

        public void DoWork(ChatMessage lastMessage)
        {
            if (lastMessage.Message.Contains(" "))
            {
                var target = lastMessage.Message.Split(new char[] { ' ' }, 2)[1];
                var answer = Answers[Rng.RNG.Next(Answers.Length)];
                ChannelInstance.SendChatMessageResponse(lastMessage, string.Format(answer, lastMessage.Username, target), true);
            }
            else
                ChannelInstance.SendChatMessageResponse(lastMessage, "Kick requires a target specified after spacebar. E.g. !kick suicidemachinebot");
        }

        //Note:
        //{0} is person initiating it
        //{1} is target
        public static string[] Answers = new string[]
        {
            "{0} tried to kick {1} in the balls, but misses!",
            "{0} tries to kick {1} in the head and succeeds!"
        };
    }
}
