using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core
{
    public class SuiBot_ChannelInstance
    {
        private const int DefaultCooldown = 30;
        public static string CommandPrefix = "!";
        public string Channel { get; set; }
        public Storage.ChannelConfig ConfigInstance { get; set; }
        Storage.CoreConfig CoreConfigInstance { get; set; }
        SuiBot SuiBotInstance { get; set; }
        #region Components
        Components.Quotes QuotesInstance { get; set; }
        Components.IntervalMessages IntervalMessagesInstance { get; set; }
        Components.ChatFiltering ChatFiltering { get; set; }
        Components.Leaderboards Leaderboards { get; set; }
        Components.CustomCvars Cvars { get; set; }
        Components.ViewerPB ViewerPb { get; set; }
        Components.GenericUtil GenericUtil { get; set; }
        Components.PCGW PCGW { get; set; }
        #region Other
        Components.Other._MemeComponents MemeComponents { get; set; }
        #endregion
        #endregion
        TwitchStatusUpdate TwitchStatus { get; set; }
        Dictionary<string, DateTime> UserCooldowns { get; set; }

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
            this.TwitchStatus = new TwitchStatusUpdate(this, Oauth);
            this.Cvars = new Components.CustomCvars(this);
            this.UserCooldowns = new Dictionary<string, DateTime>();
            this.ViewerPb = new Components.ViewerPB(this);
            this.PCGW = new Components.PCGW(this, TwitchStatus);
            this.GenericUtil = new Components.GenericUtil(this, TwitchStatus);

            //Other
            MemeComponents = new Components.Other._MemeComponents(this, ConfigInstance.MemeComponents);
        }

        internal void TimerTick()
        {
            if(ConfigInstance.IntervalMessageEnabled && TwitchStatus.isOnline)
                IntervalMessagesInstance.DoTickWork();
        }

        internal void UpdateTwitchStatus(bool vocal = false)
        {
            TwitchStatus.GetStatus();

            if (ConfigInstance.ViewerPBEnabled)
                ViewerPb.UpdateViewerPB(TwitchStatus.LastViewers);





            if (ConfigInstance.LeaderboardsEnabled && !Leaderboards.GameOverride)
                Leaderboards.CurrentGame = TwitchStatus.game;

            if (ConfigInstance.LeaderboardsAutodetectCategory && TwitchStatus.TitleHasChanged)
                Leaderboards.SetPreferedCategory(TwitchStatus.OldTitle);

            if (vocal)
                SendChatMessage(string.Format("New obtained stream status is {0}{1}.",
                    TwitchStatus.isOnline == false ? "offline" : "online",
                    TwitchStatus.game == "" ? "" : " and game is " + TwitchStatus.game
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
            if(!noPersonMention)
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

        public void SaveConfig()
        {

        }

        private void SetUserCooldown(ChatMessage messageToRespondTo, int coodown)
        {
            if (messageToRespondTo.UserRole <= Role.Mod)
                return;

            switch (messageToRespondTo.UserRole)
            {
                case (Role.VIP):
                    coodown /= 20;
                    break;
                case (Role.Subscriber):
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

        public void UserPurge(string username, string message = "")
        {
            UserTimeout(username, 1, message);
        }

        public void UserTimeout(string username, uint duration, string message = "")
        {
            SuiBotInstance.MeebyIrcClient.WriteLine(string.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :.timeout {2} {3} {4})", SuiBotInstance.BotName, Channel, username, duration, message));
        }

        public void UserBan(string username, string message = "")
        {
            SuiBotInstance.MeebyIrcClient.WriteLine(string.Format(":{0}!{0}@{0}.tmi.twitch.tv PRIVMSG #{1} :.ban {2} {3})", SuiBotInstance.BotName, Channel, username, message));
        }

        internal void DoWork(ChatMessage lastMessage)
        {
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
            if(ConfigInstance.QuotesEnabled && (messageLazy.StartsWith("quote") || messageLazy.StartsWith("quotes")))
            {
                QuotesInstance.DoWork(lastMessage);
                return;
            }

            //Chat Filter
            if(ConfigInstance.FilteringEnabled && (messageLazy.StartsWith("chatfilter") || messageLazy.StartsWith("filter")))
            {
                ChatFiltering.DoWork(lastMessage);
                return;
            }

            //Leaderboards
            if(ConfigInstance.LeaderboardsEnabled)
            {
                if(messageLazy == "wr" || messageLazy.StartsWithWordLazy("wr"))
                {
                    Leaderboards.DoWorkWR(lastMessage);
                    return;
                }
                else if(messageLazy == "pb" || messageLazy.StartsWithWordLazy("pb"))
                {
                    Leaderboards.DoWorkPB(lastMessage);
                    return;
                }
                else if(lastMessage.UserRole <= Role.Mod && messageLazy.StartsWithWordLazy(new string[] { "leaderboard", "leaderboards" }))
                {
                    Leaderboards.DoModWork(lastMessage);
                    return;
                }
            }
            
            //Interval Messages
            if(ConfigInstance.IntervalMessageEnabled)
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
            if(messageLazy.StartsWith("srl"))
            {
                Components.SRL.GetRaces(this);
                SetUserCooldown(lastMessage, DefaultCooldown);
                return;
            }

            //PCGW
            if(messageLazy.StartsWith("pcgw"))
            {
                PCGW.DoWork(lastMessage);
                return;
            }

            //Twitch update
            if(messageLazy.StartsWith("updatestatus") && lastMessage.UserRole <= Role.VIP)
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
            if(ConfigInstance.GenericUtil.ENABLE)
            {
                if(ConfigInstance.GenericUtil.UptimeEnabled && messageLazy.StartsWith("uptime"))
                {
                    GenericUtil.GetUpTime(lastMessage);
                    return;
                }
            }

            
            //MemeCompoenents
            if (ConfigInstance.MemeComponents.ENABLE)
            {
                MemeComponents.DoWork(lastMessage);
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
            IntervalMessagesInstance.Dispose();
            ChatFiltering.Dispose();
            Cvars.Dispose();
            SuiBotInstance.LeaveChannel(Channel);
        }
    }
}
