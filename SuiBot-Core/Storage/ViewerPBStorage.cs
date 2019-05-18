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
    public class ViewerPBStorage
    {
        [XmlIgnore]
        public string Channel { get; set; }

        [XmlElement]
        public uint ViewerPB { get; set; }

        public static ViewerPBStorage Load(string Channel)
        {
            string FilePath = string.Format("Bot/Channels/{0}/ViewerPB.xml", Channel);
            ViewerPBStorage obj;
            if (File.Exists(FilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ViewerPBStorage));
                FileStream fs = new FileStream(FilePath, FileMode.Open);
                obj = (ViewerPBStorage)serializer.Deserialize(fs);
                fs.Close();
                obj.Channel = Channel;
                return obj;
            }
            else
                return new ViewerPBStorage() { Channel = Channel, ViewerPB = 0 };
        }

        public void Save()
        {
            string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
            string FilePath = DirectoryPath + "ViewerPB.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(ViewerPBStorage));
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);
            StreamWriter fw = new StreamWriter(FilePath);
            serializer.Serialize(fw, this);
            fw.Close();
        }
    }
}
