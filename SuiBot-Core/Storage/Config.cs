﻿using SuiBot_Core.Extensions.SuiStringExtension;
using SuiBot_TwitchSocket.API.EventSub;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using static SuiBot_TwitchSocket.API.EventSub.ES_ChatMessage;

namespace SuiBot_Core.Storage
{
	/*
     * Comparing to first iteration one of the fundamental changes planned from get-go was offloading the login information
     * to a separate config file and using more of a "high" level syntax. Thus serialized XML was chosen in place of custom
     * and quite frankly pointless syntax.
    */	

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
				return XML_Utils.Load<CoreConfig>("Bot/Config.xml", null);
			else
				return new CoreConfig();
		}

		/// <summary>
		/// Saves config to Bot/Config.suixml.
		/// </summary>
		/// <param name="obj">Instance of SuiBot_Config object.</param>
		public void Save() => XML_Utils.Save("Bot/Config.xml", this);
	}

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

		[XmlElement] public bool QuotesEnabled { get; set; }
		[XmlElement] public bool CustomCvarsEnabled { get; set; }
		[XmlElement] public MemeConfig MemeComponents { get; set; }
		[XmlElement] public GenericUtilConfig GenericUtil { get; set; }
		[XmlElement] public bool AskAI { get; set; }

		[XmlElement] public bool FilteringEnabled { get; set; }
		[XmlElement] public bool FilterLinks { get; set; }
		[XmlElement] public bool FilterUsingAI { get; set; }
		[XmlIgnore]
		public ChatFilters Filters { get; set; }
		[XmlElement]
		public bool IntervalMessageEnabled { get; set; }
		[XmlElement]
		public bool LeaderboardsEnabled { get; set; }
		[XmlElement]
		public string LeaderboardsUsername { get; set; }
		[XmlElement]
		public bool LeaderboardsAutodetectCategory { get; set; }
		[XmlElement]
		public bool LeaderboardsUpdateProxyNames { get; set; }
		#endregion

		public ChannelConfig()
		{
			ChannelName = "";
			SuperMods = new List<string>();

			QuotesEnabled = false;
			MemeComponents = new MemeConfig();
			GenericUtil = new GenericUtilConfig();
			FilteringEnabled = false;
			FilterLinks = false;
			FilterUsingAI = false;
			Filters = new ChatFilters();
			IntervalMessageEnabled = false;
			LeaderboardsEnabled = false;
			CustomCvarsEnabled = false;
			LeaderboardsUsername = "";
			LeaderboardsAutodetectCategory = false;
			LeaderboardsUpdateProxyNames = true;
		}

		internal void GetProperty(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
		{
			if (lastMessage.UserRole <= Role.Mod)
			{
				var msg = lastMessage.message.text.StripSingleWord().ToLower();
				if (msg != "")
				{
					try
					{
						Type _type = this.GetType();
						var properties = _type.GetProperties();
						var foundProperty = properties.FirstOrDefault(x => x.Name.ToLower() == msg);
						if (foundProperty != null)
						{
							string result = foundProperty.GetValue(this, null).ToString();
							channelInstance.SendChatMessageResponse(lastMessage, string.Format("{0} == {1}", foundProperty.Name, result));
						}
						else
						{
							channelInstance.SendChatMessageResponse(lastMessage, "No property was found");
						}
					}
					catch
					{
						channelInstance.SendChatMessageResponse(lastMessage, "Error looking for property");
					}
				}
			}
		}

		internal void SetPropety(SuiBot_ChannelInstance channelInstance, ES_ChatMessage lastMessage)
		{
			var msg = lastMessage.message.text.StripSingleWord();
			if (msg != "")
			{
				try
				{
					Object target;
					Type _type;
					PropertyInfo[] properties;
					PropertyInfo foundProperty = null;

					if (msg.StartsWithLazy("memecomponents."))
					{
						msg = msg.Split(new char[] { '.' }, 2)[1];
						target = MemeComponents;
					}
					else if (msg.StartsWithLazy("genericutil."))
					{
						msg = msg.Split(new char[] { '.' }, 2)[1];
						target = GenericUtil;
					}
					else
					{
						target = this;
					}

					_type = target.GetType();
					properties = _type.GetProperties();

					foundProperty = properties.FirstOrDefault(x => msg.ToLower().FirstWord() == x.Name.ToLower());

					if (foundProperty != null)
					{
						msg = msg.StripSingleWord();
						if (foundProperty.PropertyType == typeof(bool))
						{
							if (bool.TryParse(msg, out bool res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse bool value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(byte))
						{
							if (byte.TryParse(msg, out byte res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse byte value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(short))
						{
							if (short.TryParse(msg, out short res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to short bool value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(int))
						{
							if (int.TryParse(msg, out int res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse int value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(float))
						{
							if (float.TryParse(msg, out float res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse float value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(long))
						{
							if (long.TryParse(msg, out long res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse long value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(double))
						{
							if (double.TryParse(msg, out double res))
							{
								var old = foundProperty.GetValue(target, null);
								foundProperty.SetValue(target, res, null);
								channelInstance.SendChatMessageResponse(lastMessage, $"Set {foundProperty.Name} to {res} (was {old}).");
								Save();
							}
							else
							{
								channelInstance.SendChatMessageResponse(lastMessage, "Failed to parse double value.");
							}
						}
						else if (foundProperty.PropertyType == typeof(string))
						{
							var old = foundProperty.GetValue(target, null);
							foundProperty.SetValue(target, msg, null);
							channelInstance.SendChatMessageResponse(lastMessage, $"Set \"{foundProperty.Name}\" to \"{msg}\" (was \"{old}\").");
							Save();
						}
					}
					else
					{
						channelInstance.SendChatMessageResponse(lastMessage, "No property was found");
					}
				}
				catch (Exception ex)
				{
					channelInstance.SendChatMessageResponse(lastMessage, "Error setting property");
					ErrorLogging.WriteLine(ex.ToString());
				}
			}
		}

		public static ChannelConfig Load(string channel)
		{
			string FilePath = $"Bot/Channels/{channel}.xml";
			if (File.Exists(FilePath))
			{
				var obj = XML_Utils.Load(FilePath, new ChannelConfig());
				obj.ChannelName = channel;
				obj.Filters = ChatFilters.Load(channel);
				return obj;
			}
			else
			{
				var tmpRef = new ChannelConfig() { ChannelName = channel };
				tmpRef.Save();
				return tmpRef;
			}
		}

		public void Save() => XML_Utils.Save($"Bot/Channels/{ChannelName}.xml", this);
	}

	/// <summary>
	/// Used only for Meme Components
	/// </summary>
	[Serializable]
	public class MemeConfig
	{
		[XmlElement] public bool ENABLE { get; set; }
		[XmlElement] public bool AskAILurk { get; set; }
		[XmlElement] public bool RatsBirthday { get; set; }
		[XmlElement] public bool Lurk { get; set; }
		[XmlElement] public bool Hug { get; set; }

		public MemeConfig()
		{
			ENABLE = false;
			AskAILurk = false;
			RatsBirthday = false;
			Lurk = false;
			Hug = false;
		}
	}

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

	[Serializable]
	public class GenericUtilConfig
	{
		[XmlElement] public bool ENABLE { get; set; }
		[XmlElement] public bool UptimeEnabled { get; set; }
		[XmlElement] public bool Shoutout { get; set; }

		public GenericUtilConfig()
		{
			ENABLE = false;
			UptimeEnabled = false;
			Shoutout = false;
		}
	}
}
