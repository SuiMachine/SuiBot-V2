using SuiBot_Core.SerializableDictionary;
using System;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
	[Serializable]
	public class ChatFilterUsersDB
	{
		[XmlIgnore]	private string Channel { get; set; }
		[XmlElement] public SerializableDictionary<string, uint> DB { get; set; }

		public ChatFilterUsersDB()
		{
			Channel = "";
			DB = new SerializableDictionary<string, uint>();
		}

		public static ChatFilterUsersDB Load(string Channel)
		{
			string FilePath = $"Bot/Channels/{Channel}/ChatFilterUsers.xml";
			var obj = XML_Utils.Load(FilePath, new ChatFilterUsersDB());
			obj.Channel = Channel;
			return obj;
		}

		public void Save() => XML_Utils.Save($"Bot/Channels/{Channel}/ChatFilterUsers.xml", this);


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
