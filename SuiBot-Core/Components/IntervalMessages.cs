using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChatMessage;

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
		internal void DoWork(ES_ChatMessage LastMessage)
		{
			if (LastMessage.UserRole <= Role.Mod)
			{
				var msg = LastMessage.message.text.StripSingleWord();

				if (msg.StartsWithWordLazy("add"))
					Add(LastMessage, msg);
				else if (msg.StartsWithWordLazy(new string[] { "remove", "delete" }))
					Remove(LastMessage, msg);
				else if (msg.StartsWithWordLazy(new string[] { "find", "search" }))
					Find(LastMessage, msg);
				else
					NotifyInvalid(LastMessage);
			}
		}

		private void NotifyInvalid(ES_ChatMessage lastMessage)
		{
			ChannelInstance.SendChatMessageResponse(lastMessage, "Invalid command. IntervalMessage commands should be followed by: add / remove / find");
		}

		/// <summary>
		/// Function ran to add a new interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Add(ES_ChatMessage lastMessage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage.Contains(":"))
			{
				string[] split = strippedMessage.Split(new char[] { ':' }, 2);
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
				ChannelInstance.SendChatMessageResponse(lastMessage, "No separator provided!");
		}

		/// <summary>
		/// Function ran to find already added interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Find(ES_ChatMessage lastMessage, string strippedMessage)
		{
			ChannelInstance.SendChatMessageResponse(lastMessage, "Not implemented. Go Away!");
		}

		/// <summary>
		/// Function ran to remove interval message
		/// </summary>
		/// <param name="lastMessage">Command</param>
		private void Remove(ES_ChatMessage lastMessage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage == "")
			{
				ChannelInstance.SendChatMessageResponse(lastMessage, "No ID provided");
			}
			else if (strippedMessage.StartsWithLazy("last"))
			{
				var msg = IntervalMessagesStorage.Messages.Last();
				IntervalMessagesStorage.Messages.Remove(msg);
				IntervalMessagesStorage.Save();
				ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed: {msg.Message}");

			}
			else
			{
				if (int.TryParse(strippedMessage, out int id))
				{
					if (id >= IntervalMessagesStorage.Messages.Count)
						ChannelInstance.SendChatMessageResponse(lastMessage, $"Interval message ID was outside the list bounds ({IntervalMessagesStorage.Messages.Count})");
					else
					{
						var msg = IntervalMessagesStorage.Messages[id];
						IntervalMessagesStorage.Messages.RemoveAt(id);
						IntervalMessagesStorage.Save();
						ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed: {msg.Message}");
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
			if (ChannelInstance.IsSafeMod)
				return;

			lock (IntervalMessagesStorage.Messages)
			{
				foreach (var message in IntervalMessagesStorage.Messages)
				{
					message.IntervalTick--;
					if (message.IntervalTick <= 0)
					{
						ChannelInstance.SendChatMessage(message.Message);
						message.IntervalTick = message.Interval;
					}
				}
			}
		}

		public string AddMessage(int interval, string message)
		{
			if (interval < 1)
				return "Interval can't be lower than 1 minute";
			else if (interval > 240)
				return "Interval can't be longer than 4 hours";

			if (message.Trim().Length <= 0)
				return "Provided interval message was empty";

			lock (IntervalMessagesStorage.Messages)
			{
				IntervalMessagesStorage.Messages.Add(new Storage.IntervalMessage(interval, message.Trim()));
				IntervalMessagesStorage.Save();
			}

			return "Successfully added a message";
		}

		internal string GetMessages()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Current messages are (in format Interval in minutes | Message):");
			lock (IntervalMessagesStorage.Messages)
			{
				foreach (var message in IntervalMessagesStorage.Messages)
				{
					sb.AppendLine($"{message.Interval} | {message.Message}");
				}
			}
			return sb.ToString();
		}

		internal string RemoveMessageByID(int index)
		{
			if (IntervalMessagesStorage.Messages.Count < 0)
				return "Can't remove the interval message, because no interval messages exist!";

			if (index >= 0 || index < IntervalMessagesStorage.Messages.Count)
				return "Can't remove the interval message, because no message with that ID exists!";
			lock (IntervalMessagesStorage.Messages)
			{
				var message = IntervalMessagesStorage.Messages[index];
				IntervalMessagesStorage.Messages.RemoveAt(index);
				IntervalMessagesStorage.Save();
				return $"Removed interval message {message.Message} with an interval of {message.Interval} minutes";
			}
		}

		internal string RemoveMessage(string interval_Message)
		{
			var message = IntervalMessagesStorage.Messages.FirstOrDefault(x => x.Message.ToLower().Trim() == interval_Message.ToLower().Trim());
			if (message != null)
			{
				lock (IntervalMessagesStorage.Messages)
				{
					IntervalMessagesStorage.Messages.Remove(message);
					IntervalMessagesStorage.Save();
				}
				return $"Removed interval message {message.Message} with an interval of {message.Interval} minutes";
			}

			var searchRegex = new Regex(interval_Message, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			List<Storage.IntervalMessage> messages = IntervalMessagesStorage.Messages.FindAll(x => searchRegex.IsMatch(x.Message));
			if (messages.Count == 0)
				return $"Nothing found:\r\n" + GetMessages();
			else if (messages.Count == 1)
				return $"No direct message was found. Found message \"{message.Message}\" with index {IntervalMessagesStorage.Messages.IndexOf(message)}";
			else
			{
				var l = string.Join(",", messages.Select(x => IntervalMessagesStorage.Messages.IndexOf(x)));
				return $"More than one message was found. Their indexes are index {l}";
			}
		}

		internal string FindMessageByID(int index)
		{
			if (IntervalMessagesStorage.Messages.Count < 0)
				return "Can't find the interval message, because no interval messages exist!";

			if (index >= 0 || index < IntervalMessagesStorage.Messages.Count)
				return "Can't find the interval message, because no message with that index exists!";
			lock (IntervalMessagesStorage.Messages)
			{
				var message = IntervalMessagesStorage.Messages[index];
				return $"Found interval message {message.Message} with an interval of {message.Interval} minutes";
			}
		}

		internal string FindMessage(string interval_Message)
		{
			var message = IntervalMessagesStorage.Messages.FirstOrDefault(x => x.Message.ToLower().Trim() == interval_Message.ToLower().Trim());
			if (message != null)
			{
				return $"Found exact message with an interval of {message.Interval} minutes and index of {IntervalMessagesStorage.Messages.IndexOf(message)}";
			}

			var searchRegex = new Regex(interval_Message, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

			List<Storage.IntervalMessage> messages = IntervalMessagesStorage.Messages.FindAll(x => searchRegex.IsMatch(x.Message));
			if (messages.Count == 0)
				return $"Nothing found:\r\n" + GetMessages();
			else if (messages.Count == 1)
				return $"Found message containing regex. Message content \"{message.Message}\" with index {IntervalMessagesStorage.Messages.IndexOf(message)}";
			else
			{
				var l = string.Join(",", messages.Select(x => IntervalMessagesStorage.Messages.IndexOf(x)));
				return $"More than one message was found. Their indexes are index {l}";
			}
		}
	}
}
