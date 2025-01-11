using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SuiBot_Core.Components.Other
{
	public class CharacterAI : MemeComponent
	{
		public class Config
		{
			public string SuperModName { get; set; } = "";
			public string Auth_Token { get; set; } = "";
			public string Character_SuperMod { get; set; } = "";
			public string Character_Mods { get; set; } = "";
		}

		public Config InstanceConfig;
		CharacterAi.Client.CharacterAiClient client;

		internal bool IsConfigured(SuiBot_ChannelInstance channelInstance)
		{
			InstanceConfig = XML_Utils.Load<CharacterAI.Config>($"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI.xml", null);
			if (InstanceConfig == null)
			{
				XML_Utils.Save($"Bot/Channels/{channelInstance.Channel}/MemeComponents/AI.xml", new Config());
				return false;
			}

			if (string.IsNullOrEmpty(InstanceConfig.SuperModName))
			{
				ErrorLogging.WriteLine($"Can't setup AI for channel - {channelInstance.Channel} - no {nameof(Config.SuperModName)} (streamer?) name provided");
				return false;
			}

			if (string.IsNullOrEmpty(InstanceConfig.Auth_Token))
			{
				ErrorLogging.WriteLine($"Can't setup AI for channel - {channelInstance.Channel} - no {nameof(Config.Auth_Token)} provided");
				return false;
			}

			if (string.IsNullOrEmpty(InstanceConfig.Character_SuperMod) && string.IsNullOrEmpty(InstanceConfig.Character_Mods))
			{
				ErrorLogging.WriteLine($"Can't setup AI for channel - {channelInstance.Channel} - no {nameof(Config.Character_SuperMod)} or {nameof(Config.Character_Mods)} was provided.");
				return false;
			}

			return true;
		}

		public override bool DoWork(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			if (lastMessage.UserRole == Role.SuperMod || lastMessage.UserRole == Role.Mod)
			{
				Task.Run(async () => 
				{
					await GetResponse(channelInstance, lastMessage);
				});
			}

			return true;
		}

		private async Task GetResponse(SuiBot_ChannelInstance channelInstance, ChatMessage lastMessage)
		{
			if (client == null)
				client = new CharacterAi.Client.CharacterAiClient();

			bool superMod = channelInstance.IsSuperMod(lastMessage.Username);
			string characterId = GetCharacterUsed(superMod);
			if (characterId == null)
				return;

			var chat = await client.GetChatsAsync(characterId, InstanceConfig.Auth_Token);
			if (chat.Count < 0)
			{
				channelInstance.SendChatMessage("Can't get a response, sorry.");
				return;
			}

			string response = client.SendMessageToChat(new CharacterAi.Client.Models.CaiSendMessageInputData()
			{
				CharacterId = chat[0].character_id,
				ChatId = chat[0].chat_id.ToString(),
				Message = lastMessage.Message.StripSingleWord(),
				UserAuthToken = InstanceConfig.Auth_Token,
				UserId = chat[0].creator_id.ToString(),
				Username = superMod ? InstanceConfig.SuperModName : "Chat"
			});

			//Cleanup any emotional descriptions using regex... sorry :(
			var regex = new Regex(@"\*.+\*");
			string[] split = response.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < split.Length; i++)
			{
				if (regex.IsMatch(split[i]))
					split[i] = regex.Replace(split[i], "");
			}

			split = split.Select(x => x.Trim()).Where(x => x != "").ToArray();
			response = string.Join(" ", split);

			channelInstance.SendChatMessage(response);
		}

		private string GetCharacterUsed(bool isSuperMod)
		{
			if (!string.IsNullOrEmpty(InstanceConfig.Character_SuperMod) && !string.IsNullOrEmpty(InstanceConfig.Character_Mods))
				return isSuperMod ? InstanceConfig.Character_SuperMod : InstanceConfig.Character_Mods;
			else if (!string.IsNullOrEmpty(InstanceConfig.Character_Mods))
				return InstanceConfig.Character_Mods;
			else
			{
				return isSuperMod ? InstanceConfig.Character_SuperMod : null;
			}
		}
	}
}
