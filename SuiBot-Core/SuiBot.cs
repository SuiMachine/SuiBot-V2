using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meebey.SmartIrc4net;

namespace SuiBot_Core
{
    public class SuiBot
    {
        private Storage.ConnectionConfig BotConnectionConfig { get; set; }
        public Storage.CoreConfig BotCoreConfig { get; set; }
        internal IrcClient MeebyIrcClient { get; set; }
        public Dictionary<string, SuiBot_ChannelInstance> ActiveChannels { get; set; }
        private ChatMessage LastMessage;
        public string BotName { get; set; }
        public System.Timers.Timer IntervalTimer;
        public System.Timers.Timer StatusUpdateTimer;
        private Task BotTask;
        public bool IsRunning = false;

        #region BotEventsDeclraration
        public event Events.OnIrcFeedbackHandler OnIrcFeedback;
        public event Events.OnChatMessageReceivedHandler OnChatMessageReceived;
        public event Events.OnChannelJoiningHandler OnChannelJoining;
        public event Events.OnChannelLeavingHandler OnChannelLeaving;
        public event Events.OnChannelStatusUpdateHandler OnChannelStatusUpdate;
        public event Events.OnChatSendMessageHandler OnChatSendMessage;
        public event Events.OnModerationActionHandler OnModerationActionPerformed;
        public event Events.OnShutdownHandler OnShutdown;
        #endregion



        /// <summary>
        /// Creates a new instance of Suibot, loading required data from files (if they exist)
        /// </summary>
        public SuiBot()
        {
            this.BotConnectionConfig = Storage.ConnectionConfig.Load();
            this.BotName = BotConnectionConfig.Username;
            this.BotCoreConfig = Storage.CoreConfig.Load();
            this.IntervalTimer = new System.Timers.Timer(1000 * 60) { AutoReset = true };
            this.StatusUpdateTimer = new System.Timers.Timer(5 * 1000 * 60) { AutoReset = true };
            this.ActiveChannels = new Dictionary<string, SuiBot_ChannelInstance>();
        }


        /// <summary>
        /// Creates a new instance of SuiBot.
        /// </summary>
        /// <param name="BotConfig">Config struct object. SuiBot_Config.Load() may be used to load it from config file.</param>
        public SuiBot(Storage.ConnectionConfig BotConnectionConfig, Storage.CoreConfig BotCoreConfig)
        {
            this.BotConnectionConfig = BotConnectionConfig;
            this.BotName = BotConnectionConfig.Username;
            this.BotCoreConfig = BotCoreConfig;
            this.IntervalTimer = new System.Timers.Timer(1000 * 60) { AutoReset = true };
            this.StatusUpdateTimer = new System.Timers.Timer(5* 1000 * 60) { AutoReset = true };
            this.ActiveChannels = new Dictionary<string, SuiBot_ChannelInstance>();
            this.LastMessage = new ChatMessage() { UserRole = Role.User, Message = "", Username = "" };
        }

        #region MeebyIrcEvents
        private void MeebyIrcClient_OnRawMessage(object sender, IrcEventArgs e)
        {
            try
            {
                if (e.Data.Channel != null && e.Data.Nick != null && e.Data.Message != null && ActiveChannels.ContainsKey(e.Data.Channel))
                {
                    LastMessage.Update(GetRoleFromTags(e), e.Data.Nick, e.Data.Message);
                    this.OnChatMessageReceived?.Invoke(e.Data.Channel, LastMessage);
                    ActiveChannels[e.Data.Channel].DoWork(LastMessage);
                }
            }
            catch(Exception ex)
            {
                ErrorLogging.WriteLine("Exception on raw message " + ex.Message);
            }

        }

        private Role GetRoleFromTags(IrcEventArgs e)
        {
            if (ActiveChannels[e.Data.Channel].IsSuperMod(e.Data.Nick))
                return Role.SuperMod;
            else
            {
                if(e.Data.Tags != null)
                {
                    //Ref: https://dev.twitch.tv/docs/irc/tags/
                    if (e.Data.Tags.ContainsKey("badges") && e.Data.Tags["badges"].Contains("broadcaster/1"))
                        return Role.SuperMod;
                    if (e.Data.Tags.ContainsKey("mod") && e.Data.Tags["mod"] == "1")
                        return Role.Mod;
                    else if (e.Data.Tags.ContainsKey("badges") && e.Data.Tags["badges"].Contains("vip/1"))
                        return Role.VIP;
                    else if (e.Data.Tags.ContainsKey("badges") && e.Data.Tags["badges"].Contains("subscriber/1"))
                        return Role.Subscriber;
                    else
                        return Role.User;
                }
                else
                    return Role.User;
            }
        }

        private void MeebyIrcClient_OnMotd(object sender, MotdEventArgs e)
        {
            //ErrorLogging.WriteLine("MOTD: " + e.MotdMessage);
        }

        private void MeebyIrcClient_OnConnectionError(object sender, EventArgs e)
        {
            ErrorLogging.WriteLine("!!! CONNECTION ERROR!!! " + e.ToString());
        }

        private void StatusUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach(var channel in ActiveChannels)
            {
                channel.Value.UpdateTwitchStatus();
                Thread.Sleep(2000); //A dirty way to make sure we don't go over Request limit
            }
        }

        private void IntervalTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var filteredOutChannels = ActiveChannels.Values.Where(x => x.ConfigInstance.IntervalMessageEnabled);
            foreach (var channel in filteredOutChannels)
            {
                channel.TimerTick();
            }
        }

        public void ConnectToChannel(string channelToJoin, Storage.ChannelConfig channelcfg)
        {
            MeebyIrcClient.RfcJoin("#" + channelToJoin);
            this.OnChannelJoining?.Invoke(channelToJoin);
            ActiveChannels.Add("#" +channelToJoin, new SuiBot_ChannelInstance(channelToJoin, this, channelcfg));
        }


        private void IrcClient_OnJoin(object sender, JoinEventArgs e)
        {
            //ErrorLogging.WriteLine(e.Channel + "! JOINED: " + e.Data.Nick);
            Console.WriteLine("! JOINED: " + e.Data.Nick);
        }

        internal void LeaveChannel(string channelToLeave)
        {
            this.OnChannelLeaving?.Invoke(channelToLeave);
            MeebyIrcClient.RfcPart("#" + channelToLeave);
        }

        private void IrcClient_OnPart(object sender, PartEventArgs e)
        {
            //Console.WriteLine("! PART: " + e.Data.Nick);
        }

        private void IrcClient_OnRegistered(object sender, EventArgs e)
        {
            this.OnIrcFeedback?.Invoke(Events.IrcFeedback.Verified, "");
            Console.WriteLine("! LOGIN VERIFIED");
            ErrorLogging.WriteLine("! LOGIN VERIFIED");
        }

        private void IrcClient_OnDisconnected(object sender, EventArgs e)
        {
            Console.WriteLine("! Disconnected");
            ErrorLogging.WriteLine("! Disconnected");
        }

        private void IrcClient_OnAutoConnectError(object sender, AutoConnectErrorEventArgs e)
        {
            ErrorLogging.WriteLine("Auto connect error: " + e.Exception);

        }

        private void IrcClient_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected!");
            ErrorLogging.WriteLine("Connected!");
            MeebyIrcClient.Login(BotConnectionConfig.Username, BotConnectionConfig.Username, 4, BotConnectionConfig.Username, "oauth:" + BotConnectionConfig.Password);

            Thread.Sleep(2000);

            //Request capabilities - https://dev.twitch.tv/docs/irc/guide/#twitch-irc-capabilities
            MeebyIrcClient.WriteLine("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            Thread.Sleep(2000);

            foreach (var channel in BotCoreConfig.ChannelsToJoin)
            {
                ConnectToChannel(channel, Storage.ChannelConfig.Load(channel));
                Thread.Sleep(2000);    //Since Twitch doesn't like mass joining
            }
        }

        private void IrcClient_OnConnecting(object sender, EventArgs e)
        {
            ErrorLogging.WriteLine("Connecting...");
        }

        private void IrcClient_OnErrorMessage(object sender, IrcEventArgs e)
        {
            ErrorLogging.WriteLine("Error: !" + e.Data.Message);

            //Console.WriteLine("Error: " + e.Data.Message);
        }

        private void IrcClient_OnError(object sender, ErrorEventArgs e)
        {
            ErrorLogging.WriteLine("Error: !" + e.ErrorMessage);
        }
        #endregion

        #region PublicFunctions
        public int PerformTest()
        {
            //This should be async, but whatever

            if (!BotConnectionConfig.IsValidConfig)
                throw new Exception("Invalid config!");

            MeebyIrcClient = new IrcClient()
            {
                Encoding = Encoding.UTF8,
                SendDelay = 200,
                AutoRetry = true,
                AutoReconnect = true,
                EnableUTF8Recode = true
            };

            try
            {
                MeebyIrcClient.Connect(BotConnectionConfig.Server, BotConnectionConfig.Port);
            }
            catch
            {
                return 1;
            }
            if (!MeebyIrcClient.IsConnected)
                return 1;

            MeebyIrcClient.Login(BotConnectionConfig.Username, BotConnectionConfig.Username, 4, BotConnectionConfig.Username, "oauth:" + BotConnectionConfig.Password);

            DateTime Deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

            while (DateTime.UtcNow <= Deadline)
            {
                if (MeebyIrcClient.IsRegistered)
                    return 0;
                MeebyIrcClient.ListenOnce();
            }
            return 2;
        }

        public void Connect()
        {
            if (!BotConnectionConfig.IsValidConfig)
                throw new Exception("Invalid config!");

            MeebyIrcClient = new IrcClient()
            {
                Encoding = Encoding.UTF8,
                SendDelay = 200,
                AutoRetry = true,
                AutoReconnect = true,
                EnableUTF8Recode = true
            };

            //MeebyIrc events
            MeebyIrcClient.OnError += IrcClient_OnError;
            MeebyIrcClient.OnErrorMessage += IrcClient_OnErrorMessage;
            MeebyIrcClient.OnConnecting += IrcClient_OnConnecting;
            MeebyIrcClient.OnConnected += IrcClient_OnConnected;
            MeebyIrcClient.OnAutoConnectError += IrcClient_OnAutoConnectError;
            MeebyIrcClient.OnDisconnected += IrcClient_OnDisconnected;
            MeebyIrcClient.OnRegistered += IrcClient_OnRegistered;
            MeebyIrcClient.OnPart += IrcClient_OnPart;
            MeebyIrcClient.OnJoin += IrcClient_OnJoin;
            MeebyIrcClient.OnConnectionError += MeebyIrcClient_OnConnectionError;
            MeebyIrcClient.OnRawMessage += MeebyIrcClient_OnRawMessage;

            if (BotCoreConfig.ChannelsToJoin.Count == 0)
                throw new Exception("At least 1 channel is required to join.");

            IsRunning = true;
            MeebyIrcClient.Connect(BotConnectionConfig.Server, BotConnectionConfig.Port);
            BotTask = Task.Factory.StartNew(() =>
            {
                MeebyIrcClient.Listen();
            });

            if (!MeebyIrcClient.IsConnected)
                throw new Exception("Failed to connect");

            //Timer tick
            IntervalTimer.Elapsed += IntervalTimer_Elapsed;
            IntervalTimer.Start();
            StatusUpdateTimer.Elapsed += StatusUpdateTimer_Elapsed;
            StatusUpdateTimer.Start();
        }

        public void Shutdown()
        {
            ErrorLogging.WriteLine("Planned shutdown performed ");
            Close();
            ErrorLogging.Close();
        }

        public void Close()
        {
            foreach (var channel in ActiveChannels)
            {
                channel.Value.ShutdownTask();
            }

            MeebyIrcClient.Disconnect();
            System.Threading.Thread.Sleep(2000);
            IsRunning = false;
            this.OnShutdown();
        }

        public void SendChatMessageFeedback(string channel, string message)
        {
            this.OnChatSendMessage?.Invoke(channel, message);
        }
        #endregion
    }
}
