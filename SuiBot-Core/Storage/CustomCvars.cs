using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
	[Serializable]
	public class CustomCvars
	{
		[XmlIgnore]
		string Channel { get; set; }
		[XmlArrayItem]
		public List<CustomCvar> Cvars { get; set; }

		public CustomCvars()
		{
			Cvars = new List<CustomCvar>();
		}

		public CustomCvar GetResponse(string lookUpCvar)
		{
			lookUpCvar = lookUpCvar.Remove(0, 1).Trim().ToLower();
			return Cvars.FirstOrDefault(x => x.Command == lookUpCvar);
		}

		public bool Add(CustomCvar newCvar)
		{
			newCvar.Command = newCvar.Command.ToLower().Trim();
			foreach (var cvar in Cvars)
			{
				if (cvar.Command == newCvar.Command)
					return false;
			}
			Cvars.Add(newCvar);
			return true;
		}

		public bool Remove(string command)
		{
			command = command.ToLower().Trim();
			foreach (var cvar in Cvars)
			{
				if (cvar.Command == command)
				{
					Cvars.Remove(cvar);
					return true;
				}
			}
			return false;
		}

		public bool RemoveAt(int id)
		{
			if (id < 0)
				return false;
			else if (id >= Cvars.Count)
				return false;
			else
			{
				Cvars.RemoveAt(id);
				return true;
			}
		}

		[XmlIgnore]
		public int Count => Cvars.Count;

		public static CustomCvars Load(string Channel)
		{
			string FilePath = string.Format("Bot/Channels/{0}/Cvars.xml", Channel);
			CustomCvars obj;
			if (File.Exists(FilePath) && File.ReadAllText(FilePath).Length > 0)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(CustomCvars));
				FileStream fs = new FileStream(FilePath, FileMode.Open);
				obj = (CustomCvars)serializer.Deserialize(fs);
				fs.Close();
				obj.Channel = Channel;
				return obj;
			}
			else
				return new CustomCvars() { Channel = Channel };
		}

		public void Save()
		{
			string DirectoryPath = string.Format("Bot/Channels/{0}/", Channel);
			string FilePath = DirectoryPath + "Cvars.xml";
			XmlSerializer serializer = new XmlSerializer(typeof(CustomCvars));
			if (!Directory.Exists(DirectoryPath))
				Directory.CreateDirectory(DirectoryPath);
			StreamWriter fw = new StreamWriter(FilePath);
			serializer.Serialize(fw, this);
		}
	}

	[Serializable]
	public class CustomCvar
	{
		[XmlAttribute]
		public Role RequiredRole { get; set; }
		[XmlAttribute]
		public string Command { get; set; }
		[XmlText]
		public string CvarResponse { get; set; }

		public CustomCvar()
		{
			RequiredRole = Role.User;
			Command = "";
			CvarResponse = "";
		}

		public CustomCvar(string Command, string CvarResponse)
		{
			RequiredRole = Role.User;
			this.Command = Command;
			this.CvarResponse = CvarResponse;
		}

		public CustomCvar(CustomCvar cvarToCopy)
		{
			this.RequiredRole = cvarToCopy.RequiredRole;
			this.Command = cvarToCopy.Command;
			this.CvarResponse = cvarToCopy.CvarResponse;
		}
	}
}
