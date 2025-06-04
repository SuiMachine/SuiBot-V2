using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuiBot_Core.Storage
{
	[Serializable]
	public class IntervalMessages
	{
		/// <summary>
		/// Name of the channel - used for figuring out config paths etc.
		/// </summary>
		[XmlIgnore]	private string Channel { get; set; }

		/// <summary>
		/// List of interval messages for the channel
		/// </summary>
		[XmlArrayItem] public List<IntervalMessage> Messages { get; set; }

		public IntervalMessages()
		{
			Channel = "";
			Messages = new List<IntervalMessage>();
		}

		/// <summary>
		/// Load function that serializes XML and returns IntervalMessage component
		/// </summary>
		/// <param name="Channel">Channel for which to serialize messages for</param>
		/// <returns>Component storing all interval messages for the channel</returns>
		public static IntervalMessages Load(string Channel)
		{
			string FilePath = $"Bot/Channels/{Channel}/IntervalMessages.xml";
			var newObj = XML_Utils.Load(FilePath, new IntervalMessages() { Messages = new List<IntervalMessage>() });
			newObj.Channel = Channel;
			newObj.SetTicksToIntervals();
			return newObj;
		}

		/// <summary>
		/// Function for storing interval messages back to XML
		/// </summary>
		public void Save() => XML_Utils.Save($"Bot/Channels/{Channel}/IntervalMessages.xml", this);

		/// <summary>
		/// Sets local intervalTick values based on Interval value and IntervalOffset when bot componenent is initalized with Load function
		/// </summary>
		private void SetTicksToIntervals()
		{
			foreach (var msg in Messages)
			{
				msg.IntervalTick = msg.Interval + msg.IntervalOffset;
			}
		}
	}

	[Serializable]
	public class IntervalMessage
	{
		/// <summary>
		/// Main interval value
		/// </summary>
		[XmlAttribute]
		public int Interval { get; set; }
		/// <summary>
		/// Offset of interval when initializing bot (used for example if user needs multiple messages posted in 30m interval, but doesn't want them posted all at once)
		/// </summary>
		[XmlAttribute]
		public int IntervalOffset { get; set; }
		/// <summary>
		/// Local interval value used on timer ticks
		/// </summary>
		[XmlIgnore]
		public int IntervalTick { get; set; }
		/// <summary>
		/// Message posted
		/// </summary>
		[XmlText]
		public string Message { get; set; }

		/// <summary>
		/// Default constructor for Interval Message - do not use!
		/// </summary>
		public IntervalMessage()
		{
			this.Interval = int.MaxValue;
			this.IntervalTick = int.MaxValue;
			this.IntervalOffset = 0;
			this.Message = "";
		}

		/// <summary>
		/// Basic constructor for interval message, which sets interval offset to 0min.
		/// </summary>
		/// <param name="Interval">Interval amount in minutes</param>
		/// <param name="Message">Message to be posted on each interval</param>
		public IntervalMessage(int Interval, string Message)
		{
			this.Interval = Interval;
			this.IntervalTick = Interval;
			this.IntervalOffset = 0;
			this.Message = Message;
		}

		/// <summary>
		/// A constructor that copies the object - used for handling settings etc. without overriding original object.
		/// </summary>
		/// <param name="intervalMessageToCopy">An object to copy</param>
		public IntervalMessage(IntervalMessage intervalMessageToCopy)
		{
			this.Interval = intervalMessageToCopy.Interval;
			this.IntervalTick = intervalMessageToCopy.IntervalTick;
			this.IntervalOffset = intervalMessageToCopy.IntervalOffset;
			this.Message = intervalMessageToCopy.Message;
		}
	}
}