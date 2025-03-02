using SuiBot_Core.Extensions.SuiStringExtension;
using System.Collections.Generic;
using System.Linq;

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

				if (memeConfig.Hug)
				{
					memeComponents.Add("hug", new Hug());
				}

				if (memeConfig.AskAI)
				{
					var geminiAI = new GeminiAI();
					if (geminiAI.IsConfigured(channelInstance))
					{
						memeComponents.Add("ai", geminiAI);
						memeComponents.Add("ask", geminiAI);
					}
					else
						memeConfig.AskAI = false;
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
