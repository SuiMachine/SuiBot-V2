using SuiBot_Core.API.EventSub;
using SuiBot_Core.API.Helix.Responses;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static SuiBot_Core.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core
{
	[DebuggerDisplay(nameof(SuiBot_ChannelInstance) + " {Channel}")]
	public class SuiBot_ChannelInstance
	{
		private const int DefaultCooldown = 30;
		public static string CommandPrefix = "!";
		public string Channel { get; private set; }
		public ulong ChannelID { get; private set; }

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
		internal Components.Other._MemeComponents MemeComponents { get; set; }
		#endregion
		#endregion
		public Response_StreamStatus StreamStatus { get; internal set; }
		private Dictionary<string, DateTime> UserCooldowns { get; set; }
		private Dictionary<string, DateTime> LastUserActivity { get; set; }

		public SuiBot_ChannelInstance(string Channel, ulong ChannelID, SuiBot SuiBotInstance, Storage.ChannelConfig ConfigInstance)
		{
			this.Channel = Channel;
			this.ChannelID = ChannelID;
			this.CoreConfigInstance = SuiBotInstance.BotCoreConfig;
			this.SuiBotInstance = SuiBotInstance;
			this.StreamStatus = new Response_StreamStatus();
			this.SuiBotInstance.HelixAPI.GetStatus(this);

			this.ConfigInstance = ConfigInstance;

			this.QuotesInstance = new Components.Quotes(this);
			this.IntervalMessagesInstance = new Components.IntervalMessages(this);
			this.Leaderboards = new Components.Leaderboards(this);
			this.ChatFiltering = new Components.ChatFiltering(this);
			this.Cvars = new Components.CustomCvars(this);
			this.UserCooldowns = new Dictionary<string, DateTime>();
			this.LastUserActivity = new Dictionary<string, DateTime>();
			this.PCGW = new Components.PCGW(this);
			this.GenericUtil = new Components.GenericUtil(this);
			this.Timezones = new Components.Timezones(this);

			//Other
			MemeComponents = new Components.Other._MemeComponents(this, ConfigInstance.MemeComponents);
		}

		internal void TimerTick()
		{
			if (ConfigInstance.IntervalMessageEnabled && StreamStatus.IsOnline)
				IntervalMessagesInstance.DoTickWork();
		}

		internal void UpdateTwitchStatus(bool vocal)
		{
			SuiBotInstance.HelixAPI.GetStatus(this);

			if (ConfigInstance.LeaderboardsEnabled && !Leaderboards.GameOverride)
				Leaderboards.CurrentGame = StreamStatus.game_name;

			if (ConfigInstance.LeaderboardsAutodetectCategory && StreamStatus.IsOnline)
			{
/*				if (StreamStatus.HasGameChanged() || !Leaderboards.LastUpdateSuccessful || vocal)
					Leaderboards.SetPreferedCategory(StreamInformation.StreamTitle, SuiBotInstance.IsAfterFirstStatusUpdate, vocal);*/
			}


			if (vocal)
			{
				var stream_status = StreamStatus.IsOnline == false ? "offline" : "online";
				var game = StreamStatus.game_name == "" ? "" : " and game is " + StreamStatus.game_name;

				SendChatMessage($"New obtained stream status is {stream_status}{game}.");
			}
		}

		public void SendChatMessage(string message)
		{
			if (message.Length <= 500)
			{
				SuiBotInstance.SendChatMessageFeedback("#" + Channel, message);
				SuiBotInstance.HelixAPI.SendMessage(this, message);
			}
			else
			{
				var messages = message.SplitMessage(500);
				foreach (var subMessage in messages)
				{
					SuiBotInstance.SendChatMessageFeedback("#" + Channel, subMessage);
					SuiBotInstance.HelixAPI.SendMessage(this, subMessage);
				}
			}
		}

		public void SendChatMessageResponse(ES_ChatMessage messageToRespondTo, string message)
		{
			SetUserCooldown(messageToRespondTo, DefaultCooldown);

			string msgResponse = $"@{messageToRespondTo.chatter_user_name}: {message}";
			SuiBotInstance.HelixAPI.SendResponse(messageToRespondTo, message);

			SuiBotInstance.SendChatMessageFeedback("#" + Channel, $"{messageToRespondTo.reply} -> {message}");
		}

		private void SetUserCooldown(ES_ChatMessage messageToRespondTo, int cooldown)
		{
			if (messageToRespondTo.UserRole <= Role.Mod)
				return;

			switch (messageToRespondTo.UserRole)
			{
				case Role.VIP:
					cooldown /= 20;
					break;
				case Role.Subscriber:
					cooldown /= 2;
					break;
				default:
					break;
			}

			if (!UserCooldowns.ContainsKey(messageToRespondTo.chatter_user_login))
				UserCooldowns.Add(messageToRespondTo.chatter_user_login, DateTime.UtcNow + TimeSpan.FromSeconds(cooldown));
			else
			{
				UserCooldowns[messageToRespondTo.chatter_user_login] = DateTime.UtcNow + TimeSpan.FromSeconds(cooldown);
			}
		}

		public void SendChatMessage_NoDelays(string message)
		{
/*			int originalDelay = SuiBotInstance.MeebyIrcClient.SendDelay;
			SuiBotInstance.MeebyIrcClient.SendDelay = 0;
			SuiBotInstance.MeebyIrcClient.SendMessage(Meebey.SmartIrc4net.SendType.Message, "#" + Channel, message);
			SuiBotInstance.MeebyIrcClient.SendDelay = originalDelay;*/
		}

		public void UserShoutout(string username)
		{
			//SuiBotInstance.MeebyIrcClient.WriteLine(string.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :.shoutout {2}", SuiBotInstance.BotName, Channel, username));
		}

		public void RemoveUserMessage(ES_ChatMessage lastMassage) => SuiBotInstance.HelixAPI.RequestRemoveMessage(lastMassage);
		public void UserTimeout(ES_ChatMessage lastMassage, uint length, string reason = null) => SuiBotInstance.HelixAPI.RequestTimeout(lastMassage, length, reason);
		public void UserBan(ES_ChatMessage lastMassage, string reason = null) => SuiBotInstance.HelixAPI.RequestBan(lastMassage, reason);

		internal void DoWork(ES_ChatMessage messageToProcess)
		{
			UpdateActiveUser(messageToProcess.chatter_user_login);

			//If Filtering is enabled and timeouted or banned, we don't need to do anything else
			if (ConfigInstance.FilteringEnabled && PerformActionFiltering(messageToProcess))
				return;

			//This is a useful optimisation trick, since commands all start with a one and the same prefix, we don't actually have to spend time comparing strings, if we know that prefix was wrong
			if (!messageToProcess.message.text.StartsWith(CommandPrefix) || CoreConfigInstance.IgnoredUsers.Contains(messageToProcess.chatter_user_login))
				return;

			//Do not perform actions if user is on cooldown
			if (UserCooldowns.ContainsKey(messageToProcess.chatter_user_login) && UserCooldowns[messageToProcess.chatter_user_login] > DateTime.UtcNow)
				return;

			//All of the commands are declared with lower cases
			var messageLazy = messageToProcess.message.text.ToLower();
			messageLazy = messageLazy.Remove(0, 1);

			//Properties
			if (messageLazy.StartsWithLazy("getproperty"))
			{
				ConfigInstance.GetProperty(this, messageToProcess);
				return;
			}

			if (messageLazy.StartsWithLazy("setproperty"))
			{
				ConfigInstance.SetPropety(this, messageToProcess);
				return;
			}

			//Quotes
			if (ConfigInstance.QuotesEnabled && (messageLazy.StartsWith("quote") || messageLazy.StartsWith("quotes")))
			{
				QuotesInstance.DoWork(messageToProcess);
				return;
			}

			//Chat Filter
			if (ConfigInstance.FilteringEnabled && (messageLazy.StartsWith("chatfilter") || messageLazy.StartsWith("filter")))
			{
				ChatFiltering.DoWork(messageToProcess);
				return;
			}

			//Leaderboards
			if (ConfigInstance.LeaderboardsEnabled)
			{
				if (messageLazy == "wr" || messageLazy.StartsWithWordLazy("wr"))
				{
					Leaderboards.DoWorkWR(messageToProcess);
					return;
				}
				else if (messageLazy == "pb" || messageLazy.StartsWithWordLazy("pb"))
				{
					Leaderboards.DoWorkPB(messageToProcess);
					return;
				}
				else if (messageToProcess.UserRole <= Role.Mod && messageLazy.StartsWithWordLazy(new string[] { "leaderboard", "leaderboards" }))
				{
					Leaderboards.DoModWork(messageToProcess);
					return;
				}
			}

			//Interval Messages
			if (ConfigInstance.IntervalMessageEnabled)
			{
				if (messageLazy.StartsWithWordLazy(new string[] { "intervalmessage", "intervalmessages" }))
				{
					if (messageToProcess.UserRole <= Role.Mod)
					{
						IntervalMessagesInstance.DoWork(messageToProcess);
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
				SetUserCooldown(messageToProcess, DefaultCooldown);
				return;
			}

			//PCGW
			if (messageLazy.StartsWith("pcgw"))
			{
				PCGW.DoWork(messageToProcess);
				return;
			}

			//Timezones
			if (messageLazy.StartsWith("time"))
			{
				Timezones.DoWork(messageToProcess);
			}

			//Twitch update
			if (messageLazy.StartsWith("updatestatus") && messageToProcess.UserRole <= Role.VIP)
			{
				UpdateTwitchStatus(true);
				return;
			}

			//Killswitch
			if (messageLazy.StartsWith("killbot") && messageToProcess.UserRole == Role.SuperMod)
			{
				ShutdownTask();
				return;
			}

			//GenericUtilComponents
			if (ConfigInstance.GenericUtil.ENABLE)
			{
				if (ConfigInstance.GenericUtil.Shoutout && messageLazy.StartsWith("so"))
				{
					GenericUtil.Shoutout(messageToProcess);
				}

				if (ConfigInstance.GenericUtil.UptimeEnabled && messageLazy.StartsWith("uptime"))
				{
					GenericUtil.GetUpTime(messageToProcess);
					return;
				}
			}


			//MemeCompoenents
			if (ConfigInstance.MemeComponents.ENABLE)
			{
				if (MemeComponents.DoWork(messageToProcess))
				{
					SetUserCooldown(messageToProcess, DefaultCooldown);
				}
			}


			//Custom Cvars
			if (ConfigInstance.CustomCvarsEnabled)
			{
				if (messageLazy.StartsWithLazy(new string[] { "cvar", "cvars" }))
				{
					if (messageToProcess.UserRole <= Role.Mod)
					{
						Cvars.DoWork(messageToProcess);
						return;
					}
					else
						return;
				}

				if (Cvars.PerformCustomCvar(messageToProcess))
					return;
			}
		}

		public void UpdateActiveUser(string username)
		{
			if (string.IsNullOrEmpty(username))
				return;

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

		private bool PerformActionFiltering(ES_ChatMessage message)
		{
			if (message.UserRole <= Role.VIP)
				return false;
			else
				return ChatFiltering.FilterOutMessages(message);
		}

		internal bool IsSuperMod(string username)
		{
			if (Channel == username)
				return true;
			return ConfigInstance.SuperMods.Contains(username);
		}

		internal void ShutdownTask()
		{
			ConfigInstance.Save();
			QuotesInstance.Dispose();
			ChatFiltering.Dispose();
			Cvars.Dispose();
			//SuiBotInstance.LeaveChannel(Channel);
		}
	}
}
