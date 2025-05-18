using Newtonsoft.Json;
using SuiBot_Core.API.EventSub;
using SuiBot_Core.Components.Other.Gemini;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components
{
	public class GeminiAI
	{
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
				sb.AppendLine();
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
			StreamerContent = XML_Utils.Load(StreamerPath, new Other.Gemini.GeminiContent()
			{
				contents = new List<GeminiMessage>(),
				generationConfig = new GeminiContent.GenerationConfig(),
				systemInstruction = new GeminiMessage()
				{
					role = Role.user,
					parts = new GeminiResponseMessagePart[]
					{
						new GeminiResponseMessagePart()
						{
							text = ""
						}
					}
				}
			});

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
						GeminiContent content = null;
						content = StreamerContent;
						var info = m_ChannelInstance.StreamStatus;
						content.systemInstruction = InstanceConfig.GetSystemInstruction(m_ChannelInstance.Channel, info.IsOnline, info.game_name, info.title);

						if (content == null)
						{
							m_ChannelInstance.SendChatMessageResponse(lastMessage, "Sorry, no history for the user is setup");
							return;
						}

						content.contents.Add(GeminiMessage.CreateUserResponse(lastMessage.message.text.StripSingleWord()));

						string json = JsonConvert.SerializeObject(content);

						string result = await HttpWebRequestHandlers.PerformPostAsync("https://generativelanguage.googleapis.com/", $"v1beta/{InstanceConfig.Model}:generateContent", $"?key={InstanceConfig.API_Key}",
							json,
							new Dictionary<string, string>()							
						);

						if (string.IsNullOrEmpty(result))
						{
							m_ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
						}
						else
						{
							GeminiResponse response = JsonConvert.DeserializeObject<GeminiResponse>(result);
							content.generationConfig.TokenCount = response.usageMetadata.totalTokenCount;

							if (response.candidates.Length > 0 && response.candidates.Last().content.parts.Length > 0)
							{
								var lastResponse = response.candidates.Last().content;
								content.contents.Add(lastResponse);
								var text = lastResponse.parts.Last().text;
								if (text != null)
								{
									CleanupResponse(ref text);

									m_ChannelInstance.SendChatMessageResponse(lastMessage, text);

									while (content.generationConfig.TokenCount > InstanceConfig.TokenLimit)
									{
										if (content.contents.Count > 2)
										{
											//This isn't weird - we want to make sure we start from user message
											if (content.contents[0].role == Role.user)
											{
												content.contents.RemoveAt(0);
											}

											if (content.contents[0].role == Role.model)
											{
												content.contents.RemoveAt(0);
											}
										}
									}

									XML_Utils.Save(StreamerPath, content);
								}
							}
							else
							{
								m_ChannelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
							}
						}
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
						tools = GeminiContent.GetTools(),
						generationConfig = new GeminiContent.GenerationConfig(),
						systemInstruction = InstanceConfig.GetLurkSystemInstruction(channelInstance.Channel, lastMessage.chatter_user_name, streamInfo.IsOnline, streamInfo.game_name, streamInfo.title)
					};

					content.contents.Add(GeminiMessage.CreateUserResponse(lastMessage.message.text));

					string json = JsonConvert.SerializeObject(content, Formatting.Indented);

					string result = await HttpWebRequestHandlers.PerformPostAsync("https://generativelanguage.googleapis.com/", $"v1beta/{InstanceConfig.Model}:generateContent", $"?key={InstanceConfig.API_Key}",
						json,
						new Dictionary<string, string>()						
						);

					if (string.IsNullOrEmpty(result))
					{
						channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
					}
					else
					{
						GeminiResponse response = JsonConvert.DeserializeObject<GeminiResponse>(result);
						content.generationConfig.TokenCount = response.usageMetadata.totalTokenCount;

						if (response.candidates.Length > 0 && response.candidates.Last().content.parts.Length > 0)
						{
							var lastResponse = response.candidates.Last().content;
							content.contents.Add(lastResponse);
							var text = lastResponse.parts.Last().text;
							if (text != null)
							{
								CleanupResponse(ref text);
								channelInstance.SendChatMessageResponse(lastMessage, text);
							}
							var func = lastResponse.parts.Last().functionCall;
							if (func != null)
							{
								HandleFunctionCall(channelInstance, lastMessage, func);
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
						tools = GeminiContent.GetTools(),
						generationConfig = new GeminiContent.GenerationConfig(),
						systemInstruction = InstanceConfig.GetFilterInstruction(lastMessage)
					};

					content.contents.Add(GeminiMessage.CreateUserResponse(lastMessage.message.text));

					string json = JsonConvert.SerializeObject(content, Formatting.Indented);

					string result = await HttpWebRequestHandlers.PerformPostAsync("https://generativelanguage.googleapis.com/", $"v1beta/{InstanceConfig.Model}:generateContent", $"?key={InstanceConfig.API_Key}",
						json,
						new Dictionary<string, string>()						
						);

					if (string.IsNullOrEmpty(result))
					{
						return;
					}
					else
					{
						GeminiResponse response = JsonConvert.DeserializeObject<GeminiResponse>(result);
						content.generationConfig.TokenCount = response.usageMetadata.totalTokenCount;

						if (response.candidates.Length > 0 && response.candidates.Last().content.parts.Length > 0)
						{
							var lastResponse = response.candidates.Last().content;
							if (lastResponse.parts.Last().text != null)
								return;

							var func = lastResponse.parts.Last().functionCall;
							if (func != null)
							{
								HandleFunctionCall(channelInstance, lastMessage, func);
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

		private void HandleFunctionCall(SuiBot_ChannelInstance channelInstance, ES_ChatMessage message, GeminiResponseFunctionCall func)
		{
			if (func.name == "timeout")
				func.args.ToObject<Other.Gemini.FunctionTypes.TimeOutUser>().Perform(channelInstance, message);
			else if (func.name == "ban")
				func.args.ToObject<Other.Gemini.FunctionTypes.BanUser>().Perform(channelInstance, message);
		}

		private void CleanupResponse(ref string text)
		{
			List<string> splitText = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			for (int i = splitText.Count - 1; i >= 0; i--)
			{
				var line = splitText[i].Trim();
				if (line.StartsWith("*") && line.StartsWith("*"))
				{
					var count = line.Count(x => x == '*');
					if (count == 2)
					{
						splitText.RemoveAt(i);
						continue;
					}
				}

				if (line.Contains("*"))
				{
					line = CleanDescriptors(line);
					splitText[i] = line;
				}
			}

			text = string.Join(" ", splitText);
		}

		private string CleanDescriptors(string text)
		{
			int endIndex = text.Length;
			bool isDescription = false;

			for (int i = text.Length - 1; i >= 0; i--)
			{
				if (text[i] == '*')
				{
					if (!isDescription)
					{
						endIndex = i;
						isDescription = true;
					}
					else
					{
						var length = i - endIndex;
						var substring = text.Substring(i + 1, endIndex - i - 1);
						if (substring.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 5)
						{
							text = text.Remove(i, endIndex - i + 1);
						}
						isDescription = false;
					}
				}
			}

			while (text.Contains("  "))
			{
				text = text.Replace("  ", " ");
			}

			text = text.Trim();
			return text;
		}
	}
}
