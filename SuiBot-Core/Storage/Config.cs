using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace SuiBot_Core.Storage
{
    /*
     * Comparing to first iteration one of the fundemental changes planned from get-go was offloading the login information
     * to a seperate config file and using more of a "high" level syntax. Thus serialized XML was chosen in place of custom
     * and quite frankly pointless syntax.
    */

    /// <summary>
    /// Config struct storing essential information for joining the IRC server (Twitch). New settings require creating new object.
    /// </summary>
    [Serializable]
    public class ConnectionConfig
    {
        [XmlElement]
        public string Server { get; set; }
        [XmlElement]
        public int Port { get; set; }
        [XmlElement]
        public string Username { get; set; }
        [XmlElement]
        public string Password { get; set; }

        public ConnectionConfig()
        {
            this.Server = "irc.chat.twitch.tv";
            this.Port = 6667;
            this.Username = "";
            this.Password = "";
        }

        public ConnectionConfig(string Server, int Port, string Username, string Password)
        {
            this.Server = Server;
            this.Port = Port;
            this.Username = Username;
            this.Password = Password;
        }

        [XmlIgnore]
        public bool IsValidConfig => Server != null && Port != 0 && Username != null && Password != null && Username != "" && Password != "";

        public static bool ConfigExists()
        {
            return File.Exists("Bot/ConnectionConfig.suixml");
        }

        /// <summary>
        /// Loads config from Bot/Config.suixml.
        /// </summary>
        /// <returns>SuiBot_Config object</returns>
        public static ConnectionConfig Load()
        {
            ConnectionConfig obj;
            XmlSerializer serializer = new XmlSerializer(typeof(ConnectionConfig));
            FileStream fs = new FileStream("Bot/ConnectionConfig.suixml", FileMode.Open); //Extension is NOT *.xml on purpose so that in case of streaming monitor, it's not tied to normal text editors, as it contains authy token (password).
            obj = (ConnectionConfig)serializer.Deserialize(fs);
            fs.Close();
            obj.FillEmpty();
            return obj;
        }

        /// <summary>
        /// Filling is needed in case of bot updates, since it would be a pain to write checks each time a bot function is used after bot update.
        /// </summary>
        private void FillEmpty()
        {
            if(Server == null)
                this.Server = "irc.twitch.tv";
            if(Port == 0)
                Port = 6667;
            if(Username == null)
                Username = "";
            if(Password == null)
                Password = "";
        }


        /// <summary>
        /// Saves config to Bot/Config.suixml.
        /// </summary>
        /// <param name="obj">Instance of SuiBot_Config object.</param>
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ConnectionConfig));
            if (!Directory.Exists("Bot"))
                Directory.CreateDirectory("Bot");
            StreamWriter fw = new StreamWriter("Bot/ConnectionConfig.suixml");
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }

    /// <summary>
    /// Config storing universal settings shared between all channels. This also serves as a way of isolating login information for set property functions.
    /// </summary>
    [Serializable]
    public class CoreConfig
    {
        [XmlArrayItem(ElementName = "Channel")]
        public List<string> ChannelsToJoin { get; set; }
        [XmlArrayItem(ElementName = "User")]
        public List<string> IgnoredUsers { get; set; }

        public CoreConfig()
        {
            if (ChannelsToJoin == null)
                ChannelsToJoin = new List<string>();
            if (IgnoredUsers == null)
                IgnoredUsers = new List<string>();
        }

        /// <summary>
        /// Loads config from Bot/Config.suixml.
        /// </summary>
        /// <returns>SuiBot_Config object</returns>
        public static CoreConfig Load()
        {
            if (File.Exists("Bot/Config.xml"))
            {
                CoreConfig obj;
                XmlSerializer serializer = new XmlSerializer(typeof(CoreConfig));
                FileStream fs = new FileStream("Bot/Config.xml", FileMode.Open);
                obj = (CoreConfig)serializer.Deserialize(fs);
                fs.Close();
                return obj;
            }
            else
                return new CoreConfig();

        }

        /// <summary>
        /// Saves config to Bot/Config.suixml.
        /// </summary>
        /// <param name="obj">Instance of SuiBot_Config object.</param>
        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CoreConfig));
            if (!Directory.Exists("Bot"))
                Directory.CreateDirectory("Bot");
            StreamWriter fw = new StreamWriter("Bot/Config.xml");
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }

    /*
     * Another drastic change compared to original SuiBot was standardizing names of modules and properties. In case of Channel Config, schema is:
     * ModuleEnabled, where Module is a name of module, thus for Quotes to be enabled we have QuotesEnabled. With both Words sarting with uppercase.
     * If the property is different like Amount of coins, it always follows same schema - CoinsAmount.
     */
    /// <summary>
    /// Config struct for channel specific settings (generally which functions are enabled)
    /// </summary>
    [Serializable]
    public class ChannelConfig
    {
        [XmlIgnore]
        public string ChannelName { get; set; }
        #region Properties
        #region UserLists
        [XmlArrayItem(ElementName = "SuperMod")]
        public List<string> SuperMods { get; set; }
        //Rest of roles we get from Twitch IRC
        #endregion

        [XmlElement]
        public bool QuotesEnabled { get; set; }
        [XmlElement]
        public bool CustomCvarsEnabled { get; set; }
        [XmlElement]
        public bool AskEnabled { get; set; }
        //bool AskCleverbot { get; set; }
        [XmlElement]
        public bool FilteringEnabled { get; set; }
        [XmlElement]
        public bool FilteringHarsh { get; set; }
        [XmlIgnore]
        public ChatFilters Filters { get; set; }
        [XmlElement]
        public bool ViewerPBEnabled { get; set; }
        [XmlElement]
        public bool IntervalMessageEnabled { get; set; }
        [XmlElement]
        public bool LeaderboardsEnabled { get; set; }
        [XmlElement]
        public string LeaderboardsUsername { get; set; }
        #endregion

        public ChannelConfig()
        {
            ChannelName = "";
            SuperMods = new List<string>();

            QuotesEnabled = false;
            AskEnabled = false;
            FilteringEnabled = false;
            FilteringHarsh = false;
            Filters = new ChatFilters();
            ViewerPBEnabled = false;
            IntervalMessageEnabled = false;
            LeaderboardsEnabled = false;
            CustomCvarsEnabled = false;
            LeaderboardsUsername = "";
        }

        public static ChannelConfig Load(string channel)
        {
            string FilePath = string.Format("Bot/Channels/{0}.xml", channel);
            if(File.Exists(FilePath))
            {
                ChannelConfig obj;
                XmlSerializer serializer = new XmlSerializer(typeof(ChannelConfig));
                FileStream fs = new FileStream(FilePath, FileMode.Open);
                obj = (ChannelConfig)serializer.Deserialize(fs);
                obj.ChannelName = channel;
                obj.Filters = ChatFilters.Load(channel);
                fs.Close();
                return obj;
            }
            else
            {
                var tmpRef = new ChannelConfig() { ChannelName = channel };
                tmpRef.Save();
                return tmpRef;
            }
        }

        public void Save()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ChannelConfig));
            if (!Directory.Exists("Bot/Channels"))
                Directory.CreateDirectory("Bot/Channels");
            StreamWriter fw = new StreamWriter(string.Format("Bot/Channels/{0}.xml", ChannelName));
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }
}
