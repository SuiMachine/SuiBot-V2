using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
			string FilePath = $"Bot/Channels/{Channel}/ChatFilters.xml";
			var newObj = XML_Utils.Load(FilePath, new ChatFilters()
			{
				BanFilters = new List<ChatFilter>(),
				TimeOutFilter = new List<ChatFilter>(),
				PurgeFilters = new List<ChatFilter>()
			});
			newObj.Channel = Channel;
			for (int i = 0; i < newObj.PurgeFilters.Count; i++)
				newObj.PurgeFilters[i].CompiledSyntax = new Regex(newObj.PurgeFilters[i].Syntax, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			for (int i = 0; i < newObj.TimeOutFilter.Count; i++)
				newObj.TimeOutFilter[i].CompiledSyntax = new Regex(newObj.TimeOutFilter[i].Syntax, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			for (int i = 0; i < newObj.BanFilters.Count; i++)
				newObj.BanFilters[i].CompiledSyntax = new Regex(newObj.BanFilters[i].Syntax, RegexOptions.IgnoreCase | RegexOptions.Compiled);
			return newObj;
		}

		public void Save() => XML_Utils.Save($"Bot/Channels/{Channel}/ChatFilters.xml", this);
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
		[XmlIgnore]
		public Regex CompiledSyntax { get; set; }

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
			this.CompiledSyntax = new Regex(chatFilterToCopy.Syntax, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}

		public ChatFilter(string Syntax, string Response, uint Duration)
		{
			this.Syntax = Syntax;
			this.Response = Response;
			this.Duration = Duration;
			this.CompiledSyntax = new Regex(Syntax, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		}
	}
}
