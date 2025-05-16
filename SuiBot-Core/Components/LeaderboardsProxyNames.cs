using System;
using System.Xml.Serialization;

namespace SuiBot_Core.Components
{
	[Serializable]
	public struct ProxyNameInFile
	{
		[XmlAttribute] public string Game; //Game on Twitch
		[XmlAttribute] public string ProxyName; //Game on Speedrun.com
		[XmlAttribute] public string Category;
		//[XmlAttribute] public string Subcategory;
	}

	[Serializable]
	public class ProxyNameInMemory
	{
		public string ProxyName { get; private set; }
		public string Category { get; private set; }
		//public string Subcategory { get; private set; }


		public static explicit operator ProxyNameInMemory(ProxyNameInFile v)
		{
			return new ProxyNameInMemory
			{
				ProxyName = v.ProxyName != null ? v.ProxyName : "",
				Category = v.Category != null ? v.Category : "",
			};
		}
	}
}
