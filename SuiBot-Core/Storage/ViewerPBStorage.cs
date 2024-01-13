using System;
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
			var newObj = XML_Utils.Load($"Bot/Channels/{Channel}/ViewerPB.xml", new ViewerPBStorage() { ViewerPB = 0 });
			newObj.Channel = Channel;
			return newObj;
		}

		public void Save() => XML_Utils.Save($"Bot/Channels/{Channel}/ViewerPB.xml", this);
	}
}
