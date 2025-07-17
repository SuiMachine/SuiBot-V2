using SuiBot_Core.Components.Other.Gemini;
using SuiBot_Core.Components.Other.Gemini.Speedrun;
using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using SuiBotAI;
using SuiBotAI.Components;
using SuiBotAI.Components.Other.Gemini;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SuiBotAI.Components.SuiBotAIProcessor;

namespace SuiBot_Core.Components
{
	public class GeminiAI
	{
		private SuiBotAIProcessor m_AI_Instance;
		public class Config
		{
			public string API_Key { get; set; } = "";
			public string Character_And_Knowledge { get; set; } = "";
			public string Instruction_Streamer { get; set; } = "";
			public string Lurk_Instruction { get; set; } = "";
			public string AI_Filter { get; set; } = "";
			public string Model { get; set; } = "models/gemini-2.5-flash-preview-04-17";
			public int TokenLimit { get; set; } = 1_048_576 - 8096 - 512;

			public GeminiMessage GetSystemInstruction(string streamerName, bool isLive, string category, string stream_title)
			{
				var sb = new StringBuilder();
				sb.AppendLine(Character_And_Knowledge);
				sb.AppendLine(Instruction_Streamer);
				sb.AppendLine("");

				if (isLive)
				{
					sb.AppendLine($"{streamerName} is now streaming {category}.");
					sb.AppendLine($"The stream title is: {stream_title}.");
				}
				else
				{
					sb.AppendLine($"{streamerName} is currently not streaming any game.");
				}

				return new GeminiMessage()
				{
					role = Role.user,
					parts = new GeminiResponseMessagePart[]
					{
						new GeminiResponseMessagePart()
						{
							text = sb.ToString()
						}
					}
				};
			}

			public GeminiMessage GetLurkSystemInstruction(string streamerName, string userName, bool isLive, string category, string stream_title)
			{
				var sb = new StringBuilder();
				sb.AppendLine(Character_And_Knowledge);
				sb.AppendLine(Lurk_Instruction);
				sb.AppendLine("");

				System.Globalization.CultureInfo globalizationOverride = new System.Globalization.CultureInfo("en-US");
				sb.AppendLine($"The current local time is {DateTime.Now:H:mm}. The local date is {DateTime.Now.ToString("MMMM dd, yyy", globalizationOverride)}.");
				sb.AppendLine($"The current UTC time {DateTime.UtcNow:H:mm}. The UTC date is {DateTime.Now.ToString("MMMM dd, yyy", globalizationOverride)}.");

				if (isLive)
				{
					sb.AppendLine($"{streamerName} is now streaming {category}.");
					sb.AppendLine($"The stream title is: {stream_title}.");
				}
				else
				{
					sb.AppendLine($"{streamerName} is currently not streaming any game.");
				}

				sb.AppendLine();
				sb.AppendLine($"The username is {userName}.");

				return new GeminiMessage()
				{
					role = Role.user,
					parts = new GeminiResponseMessagePart[]
					{
						new GeminiResponseMessagePart()
						{
							text = sb.ToString()
						}
					}
				};
			}

			public GeminiMessage GetFilterInstruction(ES_ChatMessage lastMessage)
			{
				var sb = new StringBuilder();
				sb.AppendLine(Character_And_Knowledge);
				sb.AppendLine();
				sb.AppendLine(AI_Filter);
				sb.AppendLine($"Username is {lastMessage.chatter_user_name}.");

				return new GeminiMessage()
				{
					role = Role.user,
					parts = new GeminiResponseMessagePart[]
					{
						new GeminiResponseMessagePart()
						{
							text = sb.ToString()
						}
					}
				};
			}
		}

		private SuiBot_ChannelInstance m_ChannelInstance;
		public Config InstanceConfig;
		public string StreamerPath;
		public GeminiContent StreamerContent = new GeminiContent();

		public GeminiAI(SuiBot_ChannelInstance suiBot_ChannelInstance)
		{
			this.m_ChannelInstance = suiBot_ChannelInstance;
		}

		internal bool IsConfigured()
		{
			InstanceConfig = XML_Utils.Load<GeminiAI.Config>($"Bot/Channels/{m_ChannelInstance.Channel}/MemeComponents/AI.xml", null);
			if (InstanceConfig == null)
			{
				XML_Utils.Save($"Bot/Channels/{m_ChannelInstance.Channel}/MemeComponents/AI.xml", new Config());
				return false;
			}

			if (string.IsNullOrEmpty(InstanceConfig.API_Key))
			{
				ErrorLogging.WriteLine($"Can't setup AI for channel - {m_ChannelInstance.Channel} - no {nameof(Config.API_Key)} provided");
				return false;
			}

			StreamerPath = $"Bot/Channels/{m_ChannelInstance.Channel}/MemeComponents/AI_History.xml";
			StreamerContent = XML_Utils.Load(StreamerPath, new GeminiContent()
			{
				contents = new List<GeminiMessage>(),
				generationConfig = new GeminiContent.GenerationConfig(),
			});

			m_AI_Instance = new SuiBotAIProcessor(InstanceConfig.API_Key, InstanceConfig.Model);
			return true;
		}

		internal void GetAIResponse(ES_ChatMessage lastMessage)
		{
			if (lastMessage.UserRole == ES_ChatMessage.Role.SuperMod || lastMessage.chatter_user_name == m_ChannelInstance.Channel)
			{
				Task.Run(async () =>
				{
					try
					{
						var info = m_ChannelInstance.StreamStatus;
						var systemInstruction = InstanceConfig.GetSystemInstruction(m_ChannelInstance.Channel, info.IsOnline, info.game_name, info.title);
						StreamerContent.tools = new List<GeminiTools>()
						{
							new GeminiTools(
								new SpeedrunWRCall(),
								new SpeedrunPBCall(),
								new CurrentDateTimeCall(),
								new IntervalMessageCalls.IntervalMessageAddCall(),
								new IntervalMessageCalls.IntervalMessageFindCall(),
								new IntervalMessageCalls.IntervalMessageRemoveCall(),
								new QuoteCalls.QuoteAddCall(),
								new QuoteCalls.QuoteFindCall(),
								new QuoteCalls.QuoteRemoveCall()
								)
						};
						StreamerContent.StorePath = StreamerPath;

						var full = AIMessageUtils.AppendDateTimePrefix(lastMessage.message.text.StripSingleWord());
						GeminiResponse result = await m_AI_Instance.GetAIResponse(StreamerContent, systemInstruction, GeminiMessage.CreateMessage(full, Role.user));

						if (result == null)
						{
							m_ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
						}
						else
						{
							if (result.candidates.Length == 0)
							{
								m_ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Response candidates were 0! Please debug me :(");
								return;
							}

							foreach (var candidate in result.candidates)
							{
								StreamerContent.generationConfig.TokenCount = result.usageMetadata.totalTokenCount;
								StreamerContent.contents.Add(candidate.content);
								foreach(var part in candidate.content.parts)
								{
									if (part.text != null)
									{
										var text = part.text;

										SuiBotAIProcessor.CleanupResponse(ref text);
										m_ChannelInstance.SendChatMessageResponse(lastMessage, text);

										while (StreamerContent.generationConfig.TokenCount > InstanceConfig.TokenLimit)
										{
											if (StreamerContent.contents.Count > 2)
											{
												//This isn't weird - we want to make sure we start from user message
												if (StreamerContent.contents[0].role == Role.user)
												{
													StreamerContent.contents.RemoveAt(0);
												}

												if (StreamerContent.contents[0].role == Role.model)
												{
													StreamerContent.contents.RemoveAt(0);
												}
											}
										}

										XML_Utils.Save(StreamerPath, StreamerContent);
									}

									if(part.functionCall != null)
									{
										var type = StreamerContent.tools[0].Calls[part.functionCall.name];
										if (type == null)
											return;
										var converted = part.functionCall.args.ToObject(type);
										if (converted.GetType().IsSubclassOf(typeof(SuiBotFunctionCall)))
										{
											var callableCast = (SuiBotFunctionCall)converted;
											callableCast.Perform(this.m_ChannelInstance, lastMessage, StreamerContent);
										}
									}
								}
							}
						}
					}
					catch (SuiBotAIProcessor.FailedToGetResponseException ex)
					{
						m_ChannelInstance.SendChatMessageResponse(lastMessage, ex.PublicMessage);
						ErrorLogging.WriteLine($"Private exception was: {ex.Private}");
					}
					catch (Exception ex)
					{
						m_ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Something was written in log. Sui help! :(");
						ErrorLogging.WriteLine($"There was an error trying to do AI: {ex}");
					}
				});
			}
			else
			{
				m_ChannelInstance.SendChatMessageResponse(lastMessage, "Sorry, this command is available to streamer only!");
			}
		}

		internal void GetSecondaryAnswer(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiContent content, GeminiMessage appendContent)
		{
			SuiBot bot = channelInstance.SuiBotInstance;

			Task.Run(async () =>
			{
				try
				{
					var result = await m_AI_Instance.GetAIResponse(content, content.systemInstruction, appendContent);
					if (result == null)
					{
						channelInstance.SendChatMessage($"{message.chatter_user_name} - Failed to get secondary response. :(");
						return;
					}
					else
					{
						content.generationConfig.TokenCount = result.usageMetadata.totalTokenCount;
						var candidate = result?.candidates.LastOrDefault();
						if (candidate != null)
						{
							content.contents.Add(candidate.content);
							foreach (var part in candidate.content.parts)
							{
								var text = part.text;
								if (text != null)
								{
									SuiBotAIProcessor.CleanupResponse(ref text);

									channelInstance?.SendChatMessageResponse(message, text);
									if (!string.IsNullOrEmpty(content.StorePath))
										XML_Utils.Save(content.StorePath, content);
								}

								var func = part.functionCall;
								if (func != null)
								{
									var type = content.tools[0].Calls[part.functionCall.name];
									if (type == null)
										return;
									var converted = part.functionCall.args.ToObject(type);
									if (converted.GetType().IsSubclassOf(typeof(SuiBotFunctionCall)))
									{
										var callableCast = (SuiBotFunctionCall)converted;
										callableCast.Perform(this.m_ChannelInstance, message, content);
									}
								}
							}

						}
					}
				}
				catch (SafetyFilterTrippedException ex)
				{
					channelInstance?.SendChatMessage($"Failed to get a response. Safety filter tripped!");
					//ErrorLogging.WriteLine($"Safety was tripped {ex}", LineType.GeminiAI));
				}
				catch (Exception ex)
				{
					channelInstance?.SendChatMessage($"Failed to get a response. Something was written in log. Help! :(");
					//MainForm.Instance.ThreadSafeAddPreviewText($"There was an error trying to do AI: {ex}", LineType.GeminiAI);
				}
			});
		}


		internal void GetResponseLurk(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage, string strippedMessage)
		{
			Task.Run(async () =>
			{
				try
				{
					GeminiContent content = null;
					var streamInfo = channelInstance.StreamStatus;
					content = new GeminiContent()
					{
						contents = new List<GeminiMessage>(),
						tools = new List<GeminiTools>()
						{
							new GeminiTools(
								new TimeOutUserCall(),
								new BanUserCall()
							)
						},
						generationConfig = new GeminiContent.GenerationConfig(),
					};

					var instruction = InstanceConfig.GetLurkSystemInstruction(channelInstance.Channel, lastMessage.chatter_user_name, streamInfo.IsOnline, streamInfo.game_name, streamInfo.title);
					var result = await m_AI_Instance.GetAIResponse(content, instruction, GeminiMessage.CreateMessage(lastMessage.message.text, Role.user));

					if (result == null)
					{
						channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
					}
					else
					{
						content.generationConfig.TokenCount = result.usageMetadata.totalTokenCount;

						if (result.candidates.Length > 0 && result.candidates.Last().content.parts.Length > 0)
						{
							var lastResponse = result.candidates.Last().content;
							content.contents.Add(lastResponse);
							var text = lastResponse.parts.Last().text;
							if (text != null)
							{
								SuiBotAIProcessor.CleanupResponse(ref text);
								channelInstance.SendChatMessageResponse(lastMessage, text);
							}
							var func = lastResponse.parts.Last().functionCall;
							if (func != null)
							{
								var type = content.tools[0].Calls[func.name];
								if (type == null)
									return;
								var converted = func.args.ToObject(type);
								if (converted.GetType().IsSubclassOf(typeof(SuiBotFunctionCall)))
								{
									var callableCast = (SuiBotFunctionCall)converted;
									callableCast.Perform(this.m_ChannelInstance, lastMessage, content);
								}
							}
						}
						else
						{
							channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
						}
					}
				}
				catch (Exception ex)
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Something was written in log. Sui help! :(");
					ErrorLogging.WriteLine($"There was an error trying to do AI: {ex}");
				}
			});
		}

		internal void PerformAIFiltering(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
		{
			Task.Run(async () =>
			{
				try
				{
					GeminiContent content = null;
					var streamInfo = channelInstance.StreamStatus;
					content = new GeminiContent()
					{
						contents = new List<GeminiMessage>(),
						tools = new List<GeminiTools>()
						{
							new GeminiTools(
								new BanUserCall(),
								new TimeOutUserCall()
								)
						},
						generationConfig = new GeminiContent.GenerationConfig(),
					};

					GeminiResponse result = await m_AI_Instance.GetAIResponse(content, InstanceConfig.GetFilterInstruction(lastMessage), GeminiMessage.CreateMessage(lastMessage.message.text, Role.user));

					if (result == null)
					{
						return;
					}
					else
					{
						if (result.candidates.Length > 0 && result.candidates.Last().content.parts.Length > 0)
						{
							var lastResponse = result.candidates.Last().content;
							if (lastResponse.parts.Last().text != null)
								return;

							var func = lastResponse.parts.Last().functionCall;
							if (func != null)
							{
								var type = content.tools[0].Calls[func.name];
								if (type == null)
									return;
								var converted = func.args.ToObject(type);
								if (converted.GetType().IsSubclassOf(typeof(SuiBotFunctionCall)))
								{
									var callableCast = (SuiBotFunctionCall)converted;
									callableCast.Perform(this.m_ChannelInstance, lastMessage, content);
								}
							}
						}
						else
						{
							channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
						}
					}
				}
				catch (Exception ex)
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Something was written in log. Sui help! :(");
					ErrorLogging.WriteLine($"There was an error trying to do AI: {ex}");
				}
			});
		}
	}
}
