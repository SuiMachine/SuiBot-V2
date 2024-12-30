using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;

namespace SuiBot_Core
{
	public class SuiBot_ChannelInstance
	{
		private const int DefaultCooldown = 30;
		public static string CommandPrefix = "!";
		public string Channel { get; set; }
		public Storage.ChannelConfig ConfigInstance { get; set; }
		Storage.CoreConfig CoreConfigInstance { get; set; }
		public string BotName => SuiBotInstance.BotName;
		SuiBot SuiBotInstance { get; set; }
		#region Components
		Components.Quotes QuotesInstance { get; set; }
		Components.IntervalMessages IntervalMessagesInstance { get; set; }
		Components.ChatFiltering ChatFiltering { get; set; }
		Components.Leaderboards Leaderboards { get; set; }
		Components.CustomCvars Cvars { get; set; }
		Components.GenericUtil GenericUtil { get; set; }
		Components.PCGW PCGW { get; set; }
		Components.Timezones Timezones { get; set; }

		#region Other
		Components.Other._MemeComponents MemeComponents { get; set; }
		#endregion
		#endregion
		TwitchAPI API { get; set; }
		Dictionary<string, DateTime> UserCooldowns { get; set; }
		Dictionary<string, DateTime> LastUserActivity { get; set; }


		//Cause of course now you have to have Oauth
		public SuiBot_ChannelInstance(string Channel, string Oauth, SuiBot SuiBotInstance, Storage.ChannelConfig ConfigInstance)
		{
			this.Channel = Channel;
			this.ConfigInstance = ConfigInstance;
			this.CoreConfigInstance = SuiBotInstance.BotCoreConfig;
			this.SuiBotInstance = SuiBotInstance;
			this.QuotesInstance = new Components.Quotes(this);
			this.IntervalMessagesInstance = new Components.IntervalMessages(this);
			this.Leaderboards = new Components.Leaderboards(this);
			this.ChatFiltering = new Components.ChatFiltering(this);
			this.API = new TwitchAPI(this, Oauth);
			this.Cvars = new Components.CustomCvars(this);
			this.UserCooldowns = new Dictionary<string, DateTime>();
			this.LastUserActivity = new Dictionary<string, DateTime>();
			this.PCGW = new Components.PCGW(this, API);
			this.GenericUtil = new Components.GenericUtil(this, API);
			this.Timezones = new Components.Timezones(this);

			//Other
			MemeComponents = new Components.Other._MemeComponents(this, ConfigInstance.MemeComponents);
		}

		internal void TimerTick()
		{
			if (ConfigInstance.IntervalMessageEnabled && API.isOnline)
				IntervalMessagesInstance.DoTickWork();
		}

		internal void UpdateTwitchStatus(bool vocal)
		{
			API.GetStatus();

			if (ConfigInstance.LeaderboardsEnabled && !Leaderboards.GameOverride)
				Leaderboards.CurrentGame = API.game;

			if (ConfigInstance.LeaderboardsAutodetectCategory && API.isOnline)
			{
				if (API.TitleHasChanged || !Leaderboards.LastUpdateSuccessful || vocal)
					Leaderboards.SetPreferedCategory(API.OldTitle, SuiBotInstance.IsAfterFirstStatusUpdate, vocal);
			}


			if (vocal)
				SendChatMessage(string.Format("New obtained stream status is {0}{1}.",
					API.isOnline == false ? "offline" : "online",
					API.game == "" ? "" : " and game is " + API.game
					));
		}

		public void SendChatMessage(string message)
		{
			SuiBotInstance.SendChatMessageFeedback("#" + Channel, message);
			SuiBotInstance.MeebyIrcClient.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#" + Channel, message);
		}

		public void SendChatMessageResponse(ChatMessage messageToRespondTo, string message, bool noPersonMention = false)
		{
			SetUserCooldown(messageToRespondTo, DefaultCooldown);
			if (!noPersonMention)
			{
				var msgResponse = string.Format("@{0}: {1}", messageToRespondTo.Username, message);
				SuiBotInstance.SendChatMessageFeedback("#" + Channel, msgResponse);
				SuiBotInstance.MeebyIrcClient.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#" + Channel, msgResponse);

			}
			else
			{
				SuiBotInstance.SendChatMessageFeedback("#" + Channel, message);
				SuiBotInstance.MeebyIrcClient.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#" + Channel, message);
			}
		}

		private void SetUserCooldown(ChatMessage messageToRespondTo, int coodown)
		{
			if (messageToRespondTo.UserRole <= Role.Mod)
				return;

			switch (messageToRespondTo.UserRole)
			{
				case Role.VIP:
					coodown /= 20;
					break;
				case Role.Subscriber:
					coodown /= 2;
					break;
				default:
					break;
			}

			if (!UserCooldowns.ContainsKey(messageToRespondTo.Username))
				UserCooldowns.Add(messageToRespondTo.Username, DateTime.UtcNow + TimeSpan.FromSeconds(coodown));
			else
			{
				UserCooldowns[messageToRespondTo.Username] = DateTime.UtcNow + TimeSpan.FromSeconds(coodown);
			}
		}

		public void SendChatMessage_NoDelays(string message)
		{
			int originalDelay = SuiBotInstance.MeebyIrcClient.SendDelay;
			SuiBotInstance.MeebyIrcClient.SendDelay = 0;
			SuiBotInstance.MeebyIrcClient.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#" + Channel, message);
			SuiBotInstance.MeebyIrcClient.SendDelay = originalDelay;
		}

		public void UserShoutout(string username)
		{
			SuiBotInstance.MeebyIrcClient.WriteLine(string.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :.shoutout {2}", SuiBotInstance.BotName, Channel, username));
		}

		public void RemoveUserMessage(ChatMessage lastMassage) => API.RequestRemoveMessage(Channel, lastMassage.MessageID);

		public void UserTimetout(ChatMessage lastMassage, uint length, string reason = null) => API.RequestTimeout(Channel, lastMassage.UserID, length, reason);

		public void UserBan(ChatMessage lastMassage, string reason = null) => API.RequestBan(Channel, lastMassage.UserID, reason);

		internal void DoWork(ChatMessage lastMessage)
		{
			UpdateActiveUser(lastMessage.Username);

			//If Filtering is enabled and timeouted or banned, we don't need to do anything else
			if (ConfigInstance.FilteringEnabled && PerformActionFiltering(lastMessage))
				return;

			//This is a useful optimisation trick, since commands all start with a one and the same prefix, we don't actually have to spend time comparing strings, if we know that prefix was wrong
			if (!lastMessage.Message.StartsWith(CommandPrefix) || CoreConfigInstance.IgnoredUsers.Contains(lastMessage.Username))
				return;

			//Do not perform actions if user is on cooldown
			if (UserCooldowns.ContainsKey(lastMessage.Username) && UserCooldowns[lastMessage.Username] > DateTime.UtcNow)
				return;

			//All of the commands are declared with lower cases
			var messageLazy = lastMessage.Message.ToLower();
			messageLazy = messageLazy.Remove(0, 1);

			//Properties
			if (messageLazy.StartsWithLazy("getproperty"))
			{
				ConfigInstance.GetProperty(this, lastMessage);
				return;
			}

			if (messageLazy.StartsWithLazy("setproperty"))
			{
				ConfigInstance.SetPropety(this, lastMessage);
				return;
			}

			//Quotes
			if (ConfigInstance.QuotesEnabled && (messageLazy.StartsWith("quote") || messageLazy.StartsWith("quotes")))
			{
				QuotesInstance.DoWork(lastMessage);
				return;
			}

			//Chat Filter
			if (ConfigInstance.FilteringEnabled && (messageLazy.StartsWith("chatfilter") || messageLazy.StartsWith("filter")))
			{
				ChatFiltering.DoWork(lastMessage);
				return;
			}

			//Leaderboards
			if (ConfigInstance.LeaderboardsEnabled)
			{
				if (messageLazy == "wr" || messageLazy.StartsWithWordLazy("wr"))
				{
					Leaderboards.DoWorkWR(lastMessage);
					return;
				}
				else if (messageLazy == "pb" || messageLazy.StartsWithWordLazy("pb"))
				{
					Leaderboards.DoWorkPB(lastMessage);
					return;
				}
				else if (lastMessage.UserRole <= Role.Mod && messageLazy.StartsWithWordLazy(new string[] { "leaderboard", "leaderboards" }))
				{
					Leaderboards.DoModWork(lastMessage);
					return;
				}
			}

			//Interval Messages
			if (ConfigInstance.IntervalMessageEnabled)
			{
				if (messageLazy.StartsWithWordLazy(new string[] { "intervalmessage", "intervalmessages" }))
				{
					if (lastMessage.UserRole <= Role.Mod)
					{
						IntervalMessagesInstance.DoWork(lastMessage);
						return;
					}
					else
						return;
				}
			}

			//Srl
			if (messageLazy.StartsWith("srl"))
			{
				Components.SRL.GetRaces(this);
				SetUserCooldown(lastMessage, DefaultCooldown);
				return;
			}

			//PCGW
			if (messageLazy.StartsWith("pcgw"))
			{
				PCGW.DoWork(lastMessage);
				return;
			}

			//Timezones
			if (messageLazy.StartsWith("time"))
			{
				Timezones.DoWork(lastMessage);
			}

			//Twitch update
			if (messageLazy.StartsWith("updatestatus") && lastMessage.UserRole <= Role.VIP)
			{
				UpdateTwitchStatus(true);
				return;
			}

			//Killswitch
			if (messageLazy.StartsWith("killbot") && lastMessage.UserRole == Role.SuperMod)
			{
				ShutdownTask();
				return;
			}

			//GenericUtilComponents
			if (ConfigInstance.GenericUtil.ENABLE)
			{
				if (ConfigInstance.GenericUtil.Shoutout && messageLazy.StartsWith("so"))
				{
					GenericUtil.Shoutout(lastMessage);
				}

				if (ConfigInstance.GenericUtil.UptimeEnabled && messageLazy.StartsWith("uptime"))
				{
					GenericUtil.GetUpTime(lastMessage);
					return;
				}
			}


			//MemeCompoenents
			if (ConfigInstance.MemeComponents.ENABLE)
			{
				if (MemeComponents.DoWork(lastMessage))
				{
					SetUserCooldown(lastMessage, DefaultCooldown);
				}
			}


			//Custom Cvars
			if (ConfigInstance.CustomCvarsEnabled)
			{
				if (messageLazy.StartsWithLazy(new string[] { "cvar", "cvars" }))
				{
					if (lastMessage.UserRole <= Role.Mod)
					{
						Cvars.DoWork(lastMessage);
						return;
					}
					else
						return;
				}

				if (Cvars.PerformCustomCvar(lastMessage))
					return;
			}
		}

		public void UpdateActiveUser(string username)
		{
			if (string.IsNullOrEmpty(username))
				return;

			username = username.ToLower();

			if (LastUserActivity.ContainsKey(username))
				LastUserActivity[username] = DateTime.UtcNow;
			else
				LastUserActivity.Add(username, DateTime.UtcNow);
		}

		public bool ActiveUsersContains(string username)
		{
			username = username.ToLower();

			if (LastUserActivity.TryGetValue(username, out DateTime userActivity))
				return userActivity + TimeSpan.FromMinutes(30) > DateTime.UtcNow;
			else
				return false;
		}

		private bool PerformActionFiltering(ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.VIP)
				return false;
			else
				return ChatFiltering.FilterOutMessages(lastMessage);
		}

		internal bool IsSuperMod(string username)
		{
			return ConfigInstance.SuperMods.Contains(username);
		}

		internal void ShutdownTask()
		{
			ConfigInstance.Save();
			QuotesInstance.Dispose();
			ChatFiltering.Dispose();
			Cvars.Dispose();
			SuiBotInstance.LeaveChannel(Channel);
		}
	}
}
