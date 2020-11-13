using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SuiBot_Core.SerializableDictionary
{
	//This class is based on solution from https://stackoverflow.com/questions/495647/serialize-class-containing-dictionary-member
	//Which in turn is based on Paul Welter's class - https://weblogs.asp.net/pwelter34/444961

	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
	{
		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			XDocument doc = null;
			using (XmlReader subtreeReader = reader.ReadSubtree())
			{
				if (reader.IsEmptyElement)
				{
					doc = new XDocument();
					return;
				}
				doc = XDocument.Load(subtreeReader);
			}
			XmlSerializer serializer = new XmlSerializer(typeof(SerializableKeyValuePair<TKey, TValue>));
			foreach (XElement item in doc.Descendants(XName.Get("Item")))
			{
				using (XmlReader itemReader = item.CreateReader())
				{
					var kvp = serializer.Deserialize(itemReader) as SerializableKeyValuePair<TKey, TValue>;
					this.Add(kvp.Key, kvp.Value);
				}
			}
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(SerializableKeyValuePair<TKey, TValue>));
			XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
			ns.Add("", "");
			foreach (TKey key in this.Keys)
			{
				TValue value = this[key];
				var kvp = new SerializableKeyValuePair<TKey, TValue>(key, value);
				serializer.Serialize(writer, kvp, ns);
			}
		}
	}

	[XmlRoot("Item")]
	public class SerializableKeyValuePair<TKey, TValue>
	{
		[XmlAttribute("Key")]
		public TKey Key;

		[XmlAttribute("Value")]
		public TValue Value;

		/// <summary>
		/// Default constructor
		/// </summary>
		public SerializableKeyValuePair() { }
		public SerializableKeyValuePair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}
	}
}
