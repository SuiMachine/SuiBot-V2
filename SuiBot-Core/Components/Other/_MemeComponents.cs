using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core.Components.Other
{
	class _MemeComponents
	{
		SuiBot_ChannelInstance channelInstance;
		Dictionary<string, MemeComponent> memeComponents;
		Storage.MemeConfig memeConfig;


		public _MemeComponents(SuiBot_ChannelInstance channelInstance, Storage.MemeConfig memeConfig)
		{
			this.channelInstance = channelInstance;
			this.memeConfig = memeConfig;
			this.memeComponents = new Dictionary<string, MemeComponent>();
			ReloadComponents(false);
		}

		private void ReloadComponents(bool notify = true)
		{
			if (memeConfig.ENABLE)
			{
				memeComponents.Clear();

				if (memeConfig.RatsBirthday)
					memeComponents.Add("ratsbirthday", new RatsBirthday());

				if (memeConfig.Tombstone)
					memeComponents.Add("tombstone", new Tombstone());

				if (memeConfig.Lurk)
				{
					memeComponents.Add("lurk", new Lurk());
					memeComponents.Add("unlurk", new Unlurk());
				}

				if (notify)
					channelInstance.SendChatMessage("Meme components reloaded!");
			}
			else
			{
				if (notify)
					channelInstance.SendChatMessage("Meme components are off");
				memeComponents.Clear();
			}
		}

		public bool DoWork(ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				if (lastMessage.Message.StartsWithLazy("!reloadmemes"))
					ReloadComponents();
			}

			if (memeComponents.Count > 0)
			{
				var component = memeComponents.FirstOrDefault(x => lastMessage.Message.StartsWithLazy("!" + x.Key));
				if (component.Value != null)
				{
					return component.Value.DoWork(channelInstance, lastMessage);
				}
			}
			return false;
		}
	}
}
