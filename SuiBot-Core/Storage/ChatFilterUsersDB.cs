using SuiBot_Core.SerializableDictionary;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
	[Serializable]
	public class ChatFilterUsersDB
	{
		[XmlIgnore]
		private string Channel { get; set; }
		[XmlElement]
		public SerializableDictionary<string, uint> DB { get; set; }

		public ChatFilterUsersDB()
		{
			Channel = "";
			DB = new SerializableDictionary<string, uint>();
		}

		public static ChatFilterUsersDB Load(string Channel)
		{
			string FilePath = string.Format("Bot/Channels/{0}/ChatFilterUsers.xml", Channel);
			ChatFilterUsersDB obj;
			if (File.Exists(FilePath))
			{
				XmlSerializer serializer = new XmlSerializer(typeof(ChatFilterUsersDB));
				FileStream fs = new FileStream(FilePath, FileMode.Open);
				obj = (ChatFilterUsersDB)serializer.Deserialize(fs);
				fs.Close();
				obj.Channel = Channel;
				return obj;
			}
			else
				return new ChatFilterUsersDB() { Channel = Channel };
		}

		public void Save()
		{
			string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
			string FilePath = DirectoryPath + "ChatFilterUsers.xml";
			XmlSerializer serializer = new XmlSerializer(typeof(ChatFilterUsersDB));
			if (!Directory.Exists(DirectoryPath))
				Directory.CreateDirectory(DirectoryPath);
			StreamWriter fw = new StreamWriter(FilePath);
			serializer.Serialize(fw, this);
		}

		internal bool CanPostLinks(string UserName)
		{
			if (DB.TryGetValue(UserName, out uint value))
			{
				if (value >= 3)
					return true;
				else
				{
					DB[UserName] = value + 1;
					return false;
				}
			}
			else
			{
				DB.Add(UserName, 1);
				return false;
			}
		}

		internal void ResetCounter(string username)
		{
			DB[username] = 0;
		}
	}
}
