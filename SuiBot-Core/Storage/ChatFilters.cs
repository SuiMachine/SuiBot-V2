using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
    [Serializable]
    public class ChatFilters
    {
        [XmlIgnore]
        public string Channel { get; set; }
        [XmlArrayItem]
        public List<ChatFilter> BanFilters { get; set; }
        [XmlArrayItem]
        public List<ChatFilter> TimeOutFilter { get; set; }
        [XmlArrayItem]
        public List<ChatFilter> PurgeFilters { get; set; }

        public ChatFilters()
        {
            BanFilters = new List<ChatFilter>();
            TimeOutFilter = new List<ChatFilter>();
            PurgeFilters = new List<ChatFilter>();
        }

        public enum FilterType
        {
            Purge,
            Timeout,
            Ban
        }

        public static ChatFilters Load(string Channel)
        {
            string FilePath = string.Format("Bot/Channels/{0}/ChatFilters.xml", Channel);
            ChatFilters obj;
            if (File.Exists(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ChatFilters));
                FileStream fs = new FileStream(FilePath, FileMode.Open);
                obj = (ChatFilters)serializer.Deserialize(fs);
                fs.Close();
                obj.Channel = Channel;
                return obj;
            }
            else
                return new ChatFilters() { Channel = Channel, BanFilters = new List<ChatFilter>(), TimeOutFilter = new List<ChatFilter>(), PurgeFilters = new List<ChatFilter>() };
        }

        public void Save()
        {
            string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
            string FilePath = DirectoryPath + "ChatFilters.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(ChatFilters));
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            StreamWriter fw = new StreamWriter(FilePath);
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }

    [Serializable]
    public class ChatFilter
    {
        [XmlText]
        public string Syntax { get; set; }
        [XmlAttribute]
        public string Response { get; set; }
        [XmlAttribute]
        public uint Duration { get; set; }

        public ChatFilter()
        {
            Syntax = "";
            Response = "";
            Duration = 1;
        }

        public ChatFilter(ChatFilter chatFilterToCopy)
        {
            this.Syntax = chatFilterToCopy.Syntax;
            this.Response = chatFilterToCopy.Response;
            this.Duration = chatFilterToCopy.Duration;
        }

        public ChatFilter(string Syntax, string Response, uint Duration)
        {
            this.Syntax = Syntax;
            this.Response = Response;
            this.Duration = Duration;
        }
    }
}
