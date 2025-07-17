using SuiBot_TwitchSocket.API.EventSub;
using SuiBotAI.Components.Other.Gemini;

namespace SuiBot_Core.Components.Other.Gemini
{
	public class IntervalMessageCalls
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
				if (message.UserRole > ES_ChatMessage.Role.Mod)
				{
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not add. User that request a message isn't a streamer or moderator!"));
					return;
				}
				else
				{
					if (!channelInstance.ConfigInstance.IntervalMessageEnabled)
					{
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Interval message component is disabled - it needs to be enabled first!"));
						return;
					}

					if (Interval == null)
					{
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Interval length is required to add an interval message!"));
						return;
					}

					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.AddMessage(Interval.Value, Interval_Message)));
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
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not use find. User that request a message isn't a streamer or moderator!"));
					return;
				}
				else
				{
					if(!channelInstance.ConfigInstance.IntervalMessageEnabled)
					{
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Interval message component is disabled - it needs to be enabled first!"));
						return;
					}

					if (Message_Index == null && string.IsNullOrEmpty(Interval_Message))
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(),  channelInstance.IntervalMessagesInstance.GetMessages()));
					else if (Message_Index != null)
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.FindMessageByID(Message_Index.Value)));
					else
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.FindMessage(Interval_Message)));
				}
			}
		}

		public class IntervalMessageRemoveCall : SuiBotFunctionCall
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
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not remove. User that request a message isn't a streamer or moderator!"));
					return;
				}
				else
				{
					if (!channelInstance.ConfigInstance.IntervalMessageEnabled)
					{
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Interval message component is disabled - it needs to be enabled first!"));
						return;
					}

					if (Message_Index == null && string.IsNullOrEmpty(Interval_Message))
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.GetMessages()));
					else if (Message_Index != null)
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.RemoveMessageByID(Message_Index.Value)));
					else
						channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.IntervalMessagesInstance.RemoveMessage(Interval_Message)));
				}
			}
		}
	}

	public class QuoteCalls
	{
		public class QuoteAddCall : SuiBotFunctionCall
		{
			[FunctionCallParameter(false)]
			public string Author;
			[FunctionCallParameter(true)]
			public string Quote;

			public override string FunctionName() => "Quote_Add";
			public override string FunctionDescription() => "Adds a quote to the database of quotes.";

			public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
			{
				if (message.UserRole > ES_ChatMessage.Role.Mod)
				{
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not add. User that request a message isn't a streamer or moderator!"));
					return;
				}

				channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.QuotesInstance.AddQuote(Author, Quote)));
			}
		}

		public class QuoteFindCall : SuiBotFunctionCall
		{
			[FunctionCallParameter(false)]
			public int? Index;
			[FunctionCallParameter(false)]
			public string Author;
			[FunctionCallParameter(false)]
			public string Quote;

			public override string FunctionName() => "Quote_Find";
			public override string FunctionDescription() => "Finds a quote. A quote can be found with either providing a Quote index or by searching with an author and quote content.";

			public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
			{
				if (message.UserRole > ES_ChatMessage.Role.Mod)
				{
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not perform find. User that request a message isn't a streamer or moderator!"));
					return;
				}

				if(Index != null)
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.QuotesInstance.FindQuoteByIndex(Index.Value)));
				else
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.QuotesInstance.FindQuote(Author, Quote)));
			}
		}

		public class QuoteRemoveCall : SuiBotFunctionCall
		{
			[FunctionCallParameter(false)]
			public int? Index;
			[FunctionCallParameter(false)]
			public string Author;
			[FunctionCallParameter(false)]
			public string Quote;

			public override string FunctionName() => "Quote_Remove";
			public override string FunctionDescription() => "Removes/deletes a quote. A quote can be deleted with either providing a Quote index or by searching with an author and quote content.";

			public override void Perform(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content)
			{
				if (message.UserRole > ES_ChatMessage.Role.Mod)
				{
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), "Can not remove. User that request a message isn't a streamer or moderator!"));
					return;
				}

				if(Index != null)
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.QuotesInstance.RemoveQuoteByIndex(Index.Value)));
				else
					channelInstance.GeminiAI.GetSecondaryAnswer(channelInstance, message, content, GeminiMessage.CreateFunctionCallResponse(FunctionName(), channelInstance.QuotesInstance.RemoveQuote(Author, Quote)));
			}
		}
	}
}
