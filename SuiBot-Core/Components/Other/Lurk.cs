using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;

namespace SuiBot_Core.Components.Other
{
	class Lurk : MemeComponent
	{
		readonly Random rng = new Random();

		List<string> Responses;

		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			if (Responses == null)
				LoadResponses(channelInstance);

			if (lastMessage.Message.Contains(" "))
			{
				if (lastMessage.UserRole <= Role.Mod)
				{
					var split = lastMessage.Message.StripSingleWord();
					if (split != "")
					{
						if (split.StartsWithWordLazy("add"))
						{
							split = split.StripSingleWord();
							if (split.Contains("{0}"))
							{
								Responses.Add(split);
								SaveResponses(channelInstance);
								channelInstance.SendChatMessage($"Added a new response. e.g.: {string.Format(split, lastMessage.Username)}");
							}
							else
								channelInstance.SendChatMessage("Added response must contain {0}");
							return false;
						}
						else if (split.StartsWithWordLazy("get"))
						{
							split = split.StripSingleWord();
							int id = GetIdFromMessage(split);
							if (id >= 0)
								channelInstance.SendChatMessage($"Response at {id} is: {Responses[id]}");
							else
								channelInstance.SendChatMessage("Incorrect id!");
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
									channelInstance.SendChatMessage($"Removed response: {response}");
									SaveResponses(channelInstance);
								}
								else
									channelInstance.SendChatMessage("There has to be at least one response. Add one response with !lurk add RESPONSE_TEXT and then remove other one using !lurk remove 0");
							}
							else
								channelInstance.SendChatMessage("Incorrect id!");
							return false;
						}
					}
				}

				string dropWord = lastMessage.Message.StripSingleWord();
				if (channelInstance.ConfigInstance.MemeComponents.AskAILurk && dropWord != "")
				{
					var aiComponent = channelInstance.MemeComponents.GetComponentOfType<GeminiAI>();
					if (aiComponent == null)
					{
						var randomResponse = Responses[rng.Next(Responses.Count)];
						channelInstance.SendChatMessage(string.Format(randomResponse, lastMessage.Username));
						return true;
					}
					else
					{
						aiComponent.DoLurk(channelInstance, lastMessage);
						return true;
					}
				}
				else
				{
					var randomResponse = Responses[rng.Next(Responses.Count)];
					channelInstance.SendChatMessage(string.Format(randomResponse, lastMessage.Username));
					return true;
				}
			}
			else
			{
				var randomResponse = Responses[rng.Next(Responses.Count)];
				channelInstance.SendChatMessage(string.Format(randomResponse, lastMessage.Username));
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
		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			channelInstance.SendChatMessage($"Welcome back, {lastMessage.Username}!");
			return true;
		}
	}
}
