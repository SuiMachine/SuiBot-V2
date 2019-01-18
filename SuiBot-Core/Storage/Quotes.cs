using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
    [Serializable]
    public class Quotes
    {
        [XmlIgnore]
        public string Channel { get; set; }

        [XmlArrayItem]
        public List<Quote> QuotesList { get; set; }

        public static Quotes Load(string Channel)
        {
            string FilePath = string.Format("Bot/Channels/{0}/Quotes.xml", Channel);
            Quotes obj;
            if (File.Exists(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Quotes));
                FileStream fs = new FileStream(FilePath, FileMode.Open);
                obj = (Quotes)serializer.Deserialize(fs);
                fs.Close();
                obj.Channel = Channel;
                return obj;
            }
            else
                return new Quotes() { Channel = Channel, QuotesList = new List<Quote>() };
        }

        public void Save()
        {
            string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
            string FilePath = DirectoryPath + "Quotes.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(Quotes));
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            StreamWriter fw = new StreamWriter(FilePath);
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }

    [Serializable]
    public class Quote
    {
        [XmlText]
        public string Text { get; set; }
        [XmlAttribute]
        public string Author { get; set; }

        public Quote()
        {
            Text = "";
            Author = "";
        }

        public Quote(string Author, string Text)
        {
            this.Author = Author;
            this.Text = Text;
        }

        public Quote(Quote QuoteToCopy)
        {
            this.Text = QuoteToCopy.Text;
            this.Author = QuoteToCopy.Author;
        }

        public override string ToString()
        {
            if(Author == "")
            {
                return string.Format("\"{0}\"", Text);
            }
            else
            {
                return string.Format("\"{0}\" - {1}", Text, Author);
            }
        }
    }
}
