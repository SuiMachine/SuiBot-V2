using System;
using System.Collections.Generic;
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
			string FilePath = $"Bot/Channels/{Channel}/Quotes.xml";
			var obj = XML_Utils.Load(FilePath, new Quotes() { QuotesList = new List<Quote>() });
			obj.Channel = Channel;
			return obj;
		}

		public void Save() => XML_Utils.Save($"Bot/Channels/{Channel}/Quotes.xml", this);
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
			if (Author == "")
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
