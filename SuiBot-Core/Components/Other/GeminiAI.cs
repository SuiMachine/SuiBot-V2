using Newtonsoft.Json;
using SuiBot_Core.Components.Other.Gemini;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components.Other
{
	public class GeminiAI : MemeComponent
	{
		public class Config
		{
			public string API_Key { get; set; } = "";
			public string Instruction_Streamer { get; set; } = "";
			public string Lurk_Instruction { get; set; } = "";
			public string Model { get; set; } = "models/gemini-2.5-flash-preview-04-17";
			public int TokenLimit { get; set; } = 1_048_576 - 8096 - 512;

			public GeminiMessage GetSystemInstruction(string streamerName, bool isLive, string category, string stream_title)
			{
				var sb = new StringBuilder();
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
					role = Gemini.Role.user,
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
					role = Gemini.Role.user,
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

		public Config InstanceConfig;
		public string StreamerPath;
		public GeminiContent StreamerContent = new GeminiContent();

		internal bool IsConfigured(SuiBot_ChannelInstance channelInstance)
		{
			InstanceConfig = XML_Utils.Load<GeminiAI.Config>($"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI.xml", null);
			if (InstanceConfig == null)
			{
				XML_Utils.Save($"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI.xml", new Config());
				return false;
			}

			if (string.IsNullOrEmpty(InstanceConfig.API_Key))
			{
				ErrorLogging.WriteLine($"Can't setup AI for channel - {channelInstance.Channel} - no {nameof(Config.API_Key)} provided");
				return false;
			}

			StreamerPath = $"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI_History.xml";
			StreamerContent = XML_Utils.Load(StreamerPath, new Gemini.GeminiContent()
			{
				contents = new List<GeminiMessage>(),
				generationConfig = new GeminiContent.GenerationConfig(),
				systemInstruction = new GeminiMessage()
				{
					role = Gemini.Role.user,
					parts = new Gemini.GeminiResponseMessagePart[]
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

		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			if (lastMessage.UserRole == Role.SuperMod || lastMessage.Username == channelInstance.Channel)
			{
				Task.Run(async () =>
				{
					await GetResponse(channelInstance, lastMessage);
				});
			}
			else
			{
				channelInstance.SendChatMessageResponse(lastMessage, "Sorry, this command is available to streamer only!");
			}

			return true;
		}

		private async Task GetResponse(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			try
			{
				Gemini.GeminiContent content = null;
				content = StreamerContent;
				content.systemInstruction = InstanceConfig.GetSystemInstruction(channelInstance.Channel, channelInstance.API.IsOnline, channelInstance.API.Game, channelInstance.API.StoredTitle);

				if (content == null)
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Sorry, no history for the user is setup");
					return;
				}

				content.contents.Add(Gemini.GeminiMessage.CreateUserResponse(lastMessage.Message.StripSingleWord()));

				string json = JsonConvert.SerializeObject(content);

				string result = await HttpWebRequestHandlers.PerformPost(
					new Uri($"https://generativelanguage.googleapis.com/v1beta/{InstanceConfig.Model}:generateContent?key={InstanceConfig.API_Key}"),
					new Dictionary<string, string>(),
					json
					);

				if (string.IsNullOrEmpty(result))
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
				}
				else
				{
					Gemini.GeminiResponse response = JsonConvert.DeserializeObject<Gemini.GeminiResponse>(result);
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

							while (content.generationConfig.TokenCount > InstanceConfig.TokenLimit)
							{
								if (content.contents.Count > 2)
								{
									//This isn't weird - we want to make sure we start from user message
									if (content.contents[0].role == Gemini.Role.user)
									{
										content.contents.RemoveAt(0);
									}

									if (content.contents[0].role == Gemini.Role.model)
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
						channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
					}
				}
			}
			catch (Exception ex)
			{
				channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Something was written in log. Sui help! :(");
				ErrorLogging.WriteLine($"There was an error trying to do AI: {ex}");
			}
		}

		internal void DoLurk(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			Task.Run(async () =>
			{
				await GetResponseLurk(channelInstance, lastMessage);
			});
		}

		private async Task GetResponseLurk(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			try
			{
				Gemini.GeminiContent content = null;
				content = new GeminiContent()
				{
					contents = new List<GeminiMessage>(),
					tools = GeminiContent.GetTools(),
					generationConfig = new GeminiContent.GenerationConfig(),
					systemInstruction = InstanceConfig.GetLurkSystemInstruction(channelInstance.Channel, lastMessage.Username, channelInstance.API.IsOnline, channelInstance.API.Game, channelInstance.API.StoredTitle)
				};

				content.contents.Add(GeminiMessage.CreateUserResponse(lastMessage.Message));

				string json = JsonConvert.SerializeObject(content, Formatting.Indented);

				string result = await HttpWebRequestHandlers.PerformPost(
					new Uri($"https://generativelanguage.googleapis.com/v1beta/{InstanceConfig.Model}:generateContent?key={InstanceConfig.API_Key}"),
					new Dictionary<string, string>(),
					json
					);

				if (string.IsNullOrEmpty(result))
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Failed to get a response. Please debug me, Sui :(");
				}
				else
				{
					Gemini.GeminiResponse response = JsonConvert.DeserializeObject<GeminiResponse>(result);
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
		}

		private void HandleFunctionCall(SuiBot_ChannelInstance channelInstance, ChatMessage message, GeminiResponseFunctionCall func)
		{
			if (func.name == "timeout")
				func.args.ToObject<Gemini.FunctionTypes.TimeOutUser>().Perform(channelInstance, message);
			else if (func.name == "ban")
				func.args.ToObject<Gemini.FunctionTypes.BanUser>().Perform(channelInstance, message);
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
