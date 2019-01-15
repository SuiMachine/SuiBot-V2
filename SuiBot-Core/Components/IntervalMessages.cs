using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SuiBot_Core.Extensions.SuiStringExtension;

namespace SuiBot_Core.Components
{
    class IntervalMessages : IDisposable
    {
        private SuiBot_ChannelInstance ChannelInstance;
        private Storage.IntervalMessages IntervalMessagesStorage;

        public IntervalMessages(SuiBot_ChannelInstance ChannelInstance)
        {
            this.ChannelInstance = ChannelInstance;
            IntervalMessagesStorage = Storage.IntervalMessages.Load(ChannelInstance.Channel);
        }

        internal void DoWork(ChatMessage LastMessage)
        {
            if (LastMessage.UserRole <= Role.Mod)
            {
                LastMessage.Message = LastMessage.Message.StripSingleWord();

                if (LastMessage.Message.StartsWithWordLazy("add"))
                    Add(LastMessage);
                else if (LastMessage.Message.StartsWithWordLazy(new string[] { "remove", "delete" }))
                    Remove(LastMessage);
                else if (LastMessage.Message.StartsWithWordLazy(new string[] { "find", "search" }))
                    Find(LastMessage);
                else
                    NotifyInvalid(LastMessage);
            }
        }

        private void NotifyInvalid(ChatMessage lastMessage)
        {
            ChannelInstance.SendChatMessageResponse(lastMessage, "Invalid command. IntervalMessage commands should be followed by: add / remove / find");
        }

        private void Add(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();
            if(lastMessage.Message.Contains(":"))
            {
                string[] split = lastMessage.Message.Split(new char[] { ':' }, 2);
                if (int.TryParse(split[0].Trim(), out int interval))
                {
                    string message = split[1].Trim();
                    if (message != "")
                    {
                        IntervalMessagesStorage.Messages.Add(new Storage.IntervalMessage(interval, message));
                        IntervalMessagesStorage.Save();
                        ChannelInstance.SendChatMessageResponse(lastMessage, "Added interval message!");
                    }
                    else
                        ChannelInstance.SendChatMessageResponse(lastMessage, "Interval Message was empty!");
                }
                else
                    ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to parse interval value.");
            }
            else
                ChannelInstance.SendChatMessageResponse(lastMessage, "No seperator provided!");
        }

        private void Find(ChatMessage lastMessage)
        {
            ChannelInstance.SendChatMessageResponse(lastMessage, "Not implemented. Go Away!");
        }

        private void Remove(ChatMessage lastMessage)
        {
            lastMessage.Message = lastMessage.Message.StripSingleWord();
            if(lastMessage.Message == "")
            {
                ChannelInstance.SendChatMessageResponse(lastMessage, "No ID provided");
            }
            else if(lastMessage.Message.StartsWithLazy("last"))
            {
                var msg = IntervalMessagesStorage.Messages.Last();
                IntervalMessagesStorage.Messages.Remove(msg);
                IntervalMessagesStorage.Save();
                ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Removed: {0}", msg.Message));

            }
            else
            {
                if(int.TryParse(lastMessage.Message, out int id))
                {
                    if(id >= IntervalMessagesStorage.Messages.Count)
                        ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Interval message ID was outside the list bounds ({0})", IntervalMessagesStorage.Messages.Count ));
                    else
                    {
                        var msg = IntervalMessagesStorage.Messages[id];
                        IntervalMessagesStorage.Messages.RemoveAt(id);
                        IntervalMessagesStorage.Save();
                        ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Removed: {0}", msg.Message));
                    }
                }
                else
                    ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to parse interval message ID");
            }
        }

        internal void DoTickWork()
        {
            foreach(var message in IntervalMessagesStorage.Messages)
            {
                message.IntervalTick--;
                if(message.IntervalTick<=0)
                {
                    if(message.IntervalTick <= 0)
                    {
                        ChannelInstance.SendChatMessage(message.Message);
                        message.IntervalTick = message.Interval;
                    }
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
