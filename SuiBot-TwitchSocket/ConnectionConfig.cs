using SuiBot_Core;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SuiBot_TwitchSocket
{
	/// <summary>
	/// Config struct storing essential information for joining the IRC server (Twitch). New settings require creating new object.
	/// </summary>
	[Serializable]
	public class ConnectionConfig
	{
		[XmlElement] public string Password { get; set; }

		public ConnectionConfig()
		{
			this.Password = "";
		}

		public ConnectionConfig(string Username, string Password)
		{
			this.Password = Password;
		}

		public bool IsValidConfig() => !string.IsNullOrEmpty(Password);

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
			var obj = XML_Utils.Load("Bot/ConnectionConfig.suixml", new ConnectionConfig());
			obj.FillEmpty();
			return obj;
		}

		/// <summary>
		/// Filling is needed in case of bot updates, since it would be a pain to write checks each time a bot function is used after bot update.
		/// </summary>
		private void FillEmpty()
		{
			if (Password == null)
				Password = "";
		}


		/// <summary>
		/// Saves config to Bot/Config.suixml.
		/// </summary>
		/// <param name="obj">Instance of SuiBot_Config object.</param>
		public void Save() => XML_Utils.Save("Bot/ConnectionConfig.suixml", this);
	}
}
