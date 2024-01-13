using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Linq;

namespace SuiBot_Core.Components
{
	internal class IntervalMessages
	{
		/// <summary>
		/// Local copy of referance to SuiBot_ChannelInstance
		/// </summary>
		private SuiBot_ChannelInstance ChannelInstance;
		/// <summary>
		/// Reference to Storage container for Interval Messages
		/// </summary>
		private Storage.IntervalMessages IntervalMessagesStorage;

		/// <summary>
		/// Constructor for IntervalMessages component created for channel
		/// </summary>
		/// <param name="ChannelInstance">Instance of a SuiBot_ChannelInstance</param>
		public IntervalMessages(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
			IntervalMessagesStorage = Storage.IntervalMessages.Load(ChannelInstance.Channel);
		}

		/// <summary>
		/// Function used for running moderation actions related to interval messages (like adding, removing, finding interval messages)
		/// </summary>
		/// <param name="LastMessage">Command posted in chat</param>
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

		/// <summary>
		/// Function ran to add a new interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Add(ChatMessage lastMessage)
		{
			lastMessage.Message = lastMessage.Message.StripSingleWord();
			if (lastMessage.Message.Contains(":"))
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

		/// <summary>
		/// Function ran to find already added interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Find(ChatMessage lastMessage)
		{
			ChannelInstance.SendChatMessageResponse(lastMessage, "Not implemented. Go Away!");
		}

		/// <summary>
		/// Function ran to remove interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Remove(ChatMessage lastMessage)
		{
			lastMessage.Message = lastMessage.Message.StripSingleWord();
			if (lastMessage.Message == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMessage, "No ID provided");
			}
			else if (lastMessage.Message.StartsWithLazy("last"))
			{
				var msg = IntervalMessagesStorage.Messages.Last();
				IntervalMessagesStorage.Messages.Remove(msg);
				IntervalMessagesStorage.Save();
				ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Removed: {0}", msg.Message));

			}
			else
			{
				if (int.TryParse(lastMessage.Message, out int id))
				{
					if (id >= IntervalMessagesStorage.Messages.Count)
						ChannelInstance.SendChatMessageResponse(lastMessage, string.Format("Interval message ID was outside the list bounds ({0})", IntervalMessagesStorage.Messages.Count));
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

		/// <summary>
		/// Function ran on each timer tick (every minute) that is responsible for posting chat messages
		/// </summary>
		internal void DoTickWork()
		{
			foreach (var message in IntervalMessagesStorage.Messages)
			{
				message.IntervalTick--;
				if (message.IntervalTick <= 0)
				{
					if (message.IntervalTick <= 0)
					{
						ChannelInstance.SendChatMessage(message.Message);
						message.IntervalTick = message.Interval;
					}
				}
			}
		}
	}
}
