using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
    [Serializable]
    public class IntervalMessages
    {
        [XmlIgnore]
        private string Channel { get; set; }
        [XmlArrayItem]
        public List<IntervalMessage> Messages { get; set; }

        public IntervalMessages()
        {
            Channel = "";
            Messages = new List<IntervalMessage>();
        }

        public static IntervalMessages Load(string Channel)
        {
            string FilePath = string.Format("Bot/Channels/{0}/IntervalMessages.xml", Channel);
            IntervalMessages obj;
            if (File.Exists(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(IntervalMessages));
                FileStream fs = new FileStream(FilePath, FileMode.Open);
                obj = (IntervalMessages)serializer.Deserialize(fs);
                fs.Close();
                obj.Channel = Channel;
                obj.SetTicksToIntervals();
                return obj;
            }
            else
                return new IntervalMessages() { Channel = Channel, Messages = new List<IntervalMessage>() };
        }

        public void Save()
        {
            string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
            string FilePath = DirectoryPath + "IntervalMessages.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(IntervalMessages));
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            StreamWriter fw = new StreamWriter(FilePath);
            serializer.Serialize(fw, this);
            fw.Close();
        }

        private void SetTicksToIntervals()
        {
            foreach(var msg in Messages)
            {
                msg.IntervalTick = msg.Interval;
            }
        }
    }

    [Serializable]
    public class IntervalMessage
    {
        [XmlAttribute]
        public int Interval { get; set; }
        [XmlIgnore]
        public int IntervalTick { get; set; }
        [XmlText]
        public string Message { get; set; }

        public IntervalMessage()
        {
            this.Interval = int.MaxValue;
            this.IntervalTick = int.MaxValue;
            this.Message = "";
        }

        public IntervalMessage(int Interval, string Message)
        {
            this.Interval = Interval;
            this.IntervalTick = Interval;
            this.Message = Message;
        }

        public IntervalMessage(IntervalMessage intervalMessageToCopy)
        {
            this.Interval = intervalMessageToCopy.Interval;
            this.IntervalTick = intervalMessageToCopy.IntervalTick;
            this.Message = intervalMessageToCopy.Message;
        }
    }
}