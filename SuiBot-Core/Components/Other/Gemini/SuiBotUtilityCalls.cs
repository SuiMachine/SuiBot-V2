using SuiBot_TwitchSocket.API.EventSub;
using SuiBotAI.Components.Other.Gemini;

namespace SuiBot_Core.Components.Other.Gemini
{
	public class IntervalMessageAddCall : SuiBotFunctionCall
	{
		[FunctionCallParameter(true)]
		public int? Interval;
		[FunctionCallParameter(true)]
		public string Interval_Message;

		public override string FunctionDescription() => "Adds interval message. Interval value is provided in minutes and has to be in the range between 1-240.";

		public override string FunctionName() => "Interval_Message_Add";

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
		{
			if(message.UserRole > ES_ChatMessage.Role.Mod)
			{
				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, "Can not add. User that request a message isn't a streamer or moderator!", Role.tool);
				return;
			}
			else
			{
				if (Interval == null)
				{
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, "Interval length is required to add an interval message!", Role.tool);
					return;
				}

				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.AddMessage(Interval.Value, Interval_Message), Role.tool);
			}
		}
	}

	public class IntervalMessageFindCall : SuiBotFunctionCall
	{
		[FunctionCallParameter(false)]
		public int? Message_Index;
		[FunctionCallParameter(false)]
		public string Interval_Message;

		public override string FunctionDescription() => $"Finds an interval message. If no {nameof(Message_Index)} or {nameof(Interval_Message)} a list of messages will be provided.";

		public override string FunctionName() => "Interval_Message_Find";

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
		{
			if (message.UserRole > ES_ChatMessage.Role.Mod)
			{
				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, "Can not use find. User that request a message isn't a streamer or moderator!", Role.tool);
				return;
			}
			else
			{
				if (Message_Index == null && string.IsNullOrEmpty(Interval_Message))
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.GetMessages(), Role.tool);
				else if (Message_Index != null)
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.FindMessageByID(Message_Index.Value), Role.tool);
				else
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.FindMessage(Interval_Message), Role.tool);
			}
		}
	}

	public class IntervalMessageRemoveCall: SuiBotFunctionCall
	{
		[FunctionCallParameter(false)]
		public int? Message_Index;
		[FunctionCallParameter(false)]
		public string Interval_Message;

		public override string FunctionDescription() => $"Removes interval message. If no {nameof(Message_Index)} or {nameof(Interval_Message)} a list of messages will be provided.";

		public override string FunctionName() => "Interval_Message_Remove";

		public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
		{
			if (message.UserRole > ES_ChatMessage.Role.Mod)
			{
				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, "Can not remove. User that request a message isn't a streamer or moderator!", Role.tool);
				return;
			}
			else
			{
				if(Message_Index == null && string.IsNullOrEmpty(Interval_Message))
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.GetMessages(), Role.tool);
				else if (Message_Index != null)
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.RemoveMessageByID(Message_Index.Value), Role.tool);
				else
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, channelInstance.IntervalMessagesInstance.RemoveMessage(Interval_Message), Role.tool);
			}
		}
	}
}
