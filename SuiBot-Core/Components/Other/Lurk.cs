using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using System.Collections.Generic;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core.Components.Other
{
	class Lurk : MemeComponent
	{
		public Dictionary<ulong, DateTime> UsersLurking = new Dictionary<ulong, DateTime>();
		readonly Random rng = new Random();

		List<string> Responses;

		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
		{
			if (Responses == null)
				LoadResponses(channelInstance);

			if (UsersLurking.TryGetValue(lastMessage.chatter_user_id, out var lurkedAt))
			{
				if (lurkedAt + TimeSpan.FromMinutes(10) > DateTime.UtcNow)
					return true;
			}

			if (lastMessage.message.text.Contains(" "))
			{
				if (lastMessage.UserRole <= Role.Mod)
				{
					var split = lastMessage.message.text.StripSingleWord();
					if (split != "")
					{
						if (split.StartsWithWordLazy("add"))
						{
							split = split.StripSingleWord();
							if (split.Contains("{0}"))
							{
								Responses.Add(split);
								SaveResponses(channelInstance);
								channelInstance.SendChatMessageResponse(lastMessage, $"Added a new response. e.g.: {string.Format(split, lastMessage.chatter_user_name)}");
							}
							else
								channelInstance.SendChatMessageResponse(lastMessage, "Added response must contain {0}");
							return false;
						}
						else if (split.StartsWithWordLazy("get"))
						{
							split = split.StripSingleWord();
							int id = GetIdFromMessage(split);
							if (id >= 0)
								channelInstance.SendChatMessageResponse(lastMessage, $"Response at {id} is: {Responses[id]}");
							else
								channelInstance.SendChatMessageResponse(lastMessage, "Incorrect id!");
							return false;
						}
						else if (split.StartsWithWordLazy("remove"))
						{
							split = split.StripSingleWord();

							int id = GetIdFromMessage(split);
							if (id >= 0)
							{
								if (Responses.Count > 1)
								{
									var response = Responses[id];
									Responses.RemoveAt(id);
									channelInstance.SendChatMessageResponse(lastMessage, $"Removed response: {response}");
									SaveResponses(channelInstance);
								}
								else
									channelInstance.SendChatMessageResponse(lastMessage, "There has to be at least one response. Add one response with !lurk add RESPONSE_TEXT and then remove other one using !lurk remove 0");
							}
							else
								channelInstance.SendChatMessageResponse(lastMessage, "Incorrect id!");
							return false;
						}
					}
				}

				string dropWord = lastMessage.message.text.StripSingleWord();
				if (channelInstance.ConfigInstance.MemeComponents.AskAILurk && dropWord != "" && channelInstance.GeminiAI.IsConfigured())
				{
					var aiComponent = channelInstance.GeminiAI;
					if (aiComponent == null)
					{
						var randomResponse = Responses[rng.Next(Responses.Count)];
						channelInstance.SendChatMessageResponse(lastMessage, string.Format(randomResponse, lastMessage.broadcaster_user_name));
						return true;
					}
					else
					{
						UsersLurking[lastMessage.chatter_user_id] = DateTime.UtcNow;
						aiComponent.GetResponseLurk(channelInstance, lastMessage, dropWord);
						return true;
					}
				}
				else
				{
					var randomResponse = Responses[rng.Next(Responses.Count)];
					UsersLurking[lastMessage.chatter_user_id] = DateTime.UtcNow;
					channelInstance.SendChatMessageResponse(lastMessage, string.Format(randomResponse, lastMessage.chatter_user_name));
					return true;
				}
			}
			else
			{
				var randomResponse = Responses[rng.Next(Responses.Count)];
				UsersLurking[lastMessage.chatter_user_id] = DateTime.UtcNow;
				channelInstance.SendChatMessageResponse(lastMessage, string.Format(randomResponse, lastMessage.chatter_user_name));
				return true;
			}
		}

		private int GetIdFromMessage(string split)
		{
			if (int.TryParse(split, out int id))
			{
				if (id >= 0 && id < Responses.Count)
					return id;
				else
					return -1;
			}
			else if (split.StartsWithLazy("last"))
				return Responses.Count - 1;
			else
				return -1;
		}

		private void LoadResponses(SuiBot_ChannelInstance channelInstance)
		{
			Responses = XML_Utils.Load($"Bot/Channels/{channelInstance.Channel}/MemeComponents/LurkMessages.xml", new List<string> { "Enjoy the lurk, {0}" });
		}

		private void SaveResponses(SuiBot_ChannelInstance channelInstance)
		{
			XML_Utils.Save($"Bot/Channels/{channelInstance.Channel}/MemeComponents/LurkMessages.xml", Responses);
		}
	}

	class Unlurk : MemeComponent
	{
		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
		{
			channelInstance.SendChatMessage($"Welcome back, {lastMessage.chatter_user_name}!");
			return true;
		}
	}
}
