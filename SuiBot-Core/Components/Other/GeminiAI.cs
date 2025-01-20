using Newtonsoft.Json;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace SuiBot_Core.Components.Other
{
	public class GeminiAI : MemeComponent
	{
		public class Config
		{
			public string API_Key { get; set; } = "";
			public string Instruction_Streamer { get; set; } = "";
			public string Instruction_Generic { get; set; } = "";
			public string Model { get; set; } = "models/gemini-2.0-flash-exp";
			public int TokenLimit { get; set; } = 1_048_576 - 8096 - 512;
		}

		public Config InstanceConfig;
		public string StreamerPath;
		public Gemini.GeminiContent StreamerContent = new Gemini.GeminiContent();
		public Dictionary<string, Gemini.GeminiContent> UserContents = new Dictionary<string, Gemini.GeminiContent>();

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
				contents = new List<Gemini.GeminiMessage>(),
				generationConfig = new Gemini.GeminiContent.GenerationConfig(),
				systemInstruction = new Gemini.GeminiMessage()
				{
					role = Gemini.Role.user,
					parts = new Gemini.GeminiMessagePart[]
					{
						new Gemini.GeminiMessagePart()
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
			if (lastMessage.UserRole == Role.SuperMod || lastMessage.UserRole == Role.Mod)
			{
				Task.Run(async () =>
				{
					//TODO: Add throttling!
					await GetResponse(channelInstance, lastMessage);
				});
			}

			return true;
		}

		private async Task GetResponse(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			try
			{
				bool isStreamer = channelInstance.Channel == lastMessage.Username; //Streamer responses are stored

				Gemini.GeminiContent content = null;
				if (channelInstance.Channel == lastMessage.Username)
				{
					content = StreamerContent;
				}
				else if (!UserContents.TryGetValue(lastMessage.Username, out content))
				{
					string instruction = null;
					if (channelInstance.Channel == lastMessage.Username)
					{
						instruction = InstanceConfig.Instruction_Streamer;
						isStreamer = true;
					}
					else
					{
						var path = $"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI_UserInstructions/{lastMessage.Username}.txt";
						if (File.Exists(path))
						{
							instruction = File.ReadAllText(path);
						}

						if (instruction == null)
						{
							instruction = string.Format(InstanceConfig.Instruction_Generic, lastMessage.Username);
						}
					}

					if (instruction == null)
					{
						channelInstance.SendChatMessageResponse(lastMessage, "Sorry, there is no response configured for non-streamer or you specifically");
					}

					content = new Gemini.GeminiContent()
					{
						contents = new List<Gemini.GeminiMessage>(),
						generationConfig = new Gemini.GeminiContent.GenerationConfig(),
						systemInstruction = new Gemini.GeminiMessage()
						{
							role = Gemini.Role.user,
							parts = new Gemini.GeminiMessagePart[]
							{
								new Gemini.GeminiMessagePart()
								{
									text = instruction
								}
							}
						}
					};
					UserContents.Add(lastMessage.Username, content);
				}

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
						List<string> splitText = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
						for (int i = splitText.Count - 1; i >= 0; i--)
						{
							var line = splitText[i].Trim();
							if (line.StartsWith("*") && line.StartsWith("*"))
							{
								var count = line.Count(x => x == '*');
								if (count == 2)
									splitText.RemoveAt(i);
							}
						}

						text = string.Join(" ", splitText);

						if(text.Contains('*'))
						{
							var regex = new Regex("\\*.+?\\*");
							var matches = regex.Matches(text);
						}

						text = CleanDescriptors(text);

						channelInstance.SendChatMessageResponse(lastMessage, text);

						if (isStreamer)
						{
							//TODO: Make sure we reduce the amount of tokens with time!
							XML_Utils.Save(StreamerPath, content);
						}
						else
						{
							while (content.contents.Count > 10)
							{
								content.contents.RemoveAt(0);
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
