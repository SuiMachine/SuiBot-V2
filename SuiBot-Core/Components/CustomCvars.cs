using System;
using SuiBot_Core.Extensions.SuiStringExtension;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public void DoWork(ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				lastMessage.Message = lastMessage.Message.StripSingleWord();
				if (lastMessage.Message.StartsWithWordLazy("add"))
				{
					AddCvar(lastMessage);
				}
				else if (lastMessage.Message.StartsWithWordLazy("remove"))
				{
					//Add update
					RemoveCvar(lastMessage);
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar should be followed by add or remove.");
			}

		}

		private void AddCvar(ChatMessage lastMessage)
		{
			lastMessage.Message = lastMessage.Message.StripSingleWord();
			if (lastMessage.Message.Contains(" "))
			{
				var cvar = lastMessage.Message.Split(new char[] { ' ' }, 2);
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

		private void RemoveCvar(ChatMessage lastMessage)
		{
			lastMessage.Message = lastMessage.Message.StripSingleWord();
			if (int.TryParse(lastMessage.Message, out int id))
			{
				if (Cvars.RemoveAt(id))
				{
					Cvars.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, "Removed cvar with ID:" + id.ToString());
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar ID out of bounds");

			}
			else
			{
				if (Cvars.Remove(lastMessage.Message))
				{
					Cvars.Save();
					ChannelInstance.SendChatMessageResponse(lastMessage, "Removed cvar: " + lastMessage.Message);
				}
				else
					ChannelInstance.SendChatMessageResponse(lastMessage, "Cvar " + lastMessage.Message + " not found.");
			}
		}

		public bool PerformCustomCvar(ChatMessage lastMessage)
		{
			var cvar = Cvars.GetResponse(lastMessage.Message);
			if (cvar == null)
				return false;
			else
			{
				if (lastMessage.UserRole <= cvar.RequiredRole)
				{
					ChannelInstance.SendChatMessageResponse(lastMessage, cvar.CvarResponse, true);
					return true;
				}
				else
					return false;
			}
		}
	}
}
