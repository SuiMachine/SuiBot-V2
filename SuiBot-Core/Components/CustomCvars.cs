using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core.Components
{
	internal class CustomCvars : IDisposable
	{
		SuiBot_ChannelInstance ChannelInstance;
		Storage.CustomCvars Cvars;

		public CustomCvars(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
			Cvars = Storage.CustomCvars.Load(ChannelInstance.Channel);
		}

		public void Dispose()
		{
			Cvars.Save();
		}

		public void DoWork(ES_ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				var msg = lastMessage.message.text.StripSingleWord();
				if (msg.StartsWithWordLazy("add"))
				{
					AddCvar(lastMessage, msg);
				}
				else if (msg.StartsWithWordLazy("remove"))
				{
					//Add update
					RemoveCvar(lastMessage, msg);
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar should be followed by add or remove.");
			}

		}

		private void AddCvar(ES_ChatMessage lastMessage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (strippedMessage.Contains(" "))
			{
				var cvar = strippedMessage.Split(new char[] { ' ' }, 2);
				var newCvar = new Storage.CustomCvar(cvar[0].ToLower(), cvar[1]);
				if (Cvars.Add(newCvar))
				{
					Cvars.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, "Added " + newCvar.Command);
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, newCvar.Command + " already exists");
			}
			else
				ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar add should be followed by command name and then text, e.g. \"!cvar add test Test response\".");
		}

		private void RemoveCvar(ES_ChatMessage lastMessage, string strippedMessage)
		{
			strippedMessage = strippedMessage.StripSingleWord();
			if (int.TryParse(strippedMessage, out int id))
			{
				if (Cvars.RemoveAt(id))
				{
					Cvars.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed cvar with ID: {id}");
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar ID out of bounds");

			}
			else
			{
				if (Cvars.Remove(strippedMessage))
				{
					Cvars.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, $"Removed cvar: {strippedMessage}");
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, $"Cvar {strippedMessage} not found.");
			}
		}

		public bool PerformCustomCvar(ES_ChatMessage lastMessage)
		{
			var cvar = Cvars.GetResponse(lastMessage.message.text);
			if (cvar == null)
				return false;
			else
			{
				if (lastMessage.UserRole <= cvar.RequiredRole)
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, cvar.CvarResponse);
					return true;
				}
				else
					return false;
			}
		}
	}
}
