using SuiBot_Core.API.EventSub;
using SuiBot_Core.Extensions.SuiStringExtension;
using System;
using System.Text;

namespace SuiBot_Core.Components
{
	internal class Timezones
	{
		SuiBot_ChannelInstance ChannelInstance { get; set; }


		public Timezones(SuiBot_ChannelInstance ChannelInstance)
		{
			this.ChannelInstance = ChannelInstance;
		}

		public void DoWork(ES_ChatMessage lastMessage)
		{
			string strip = lastMessage.message.text.StripSingleWord();

			if (DateTime.TryParse(strip, out DateTime result))
			{
				if (result.Kind == DateTimeKind.Unspecified)
					result = new DateTime(result.Year, result.Month, result.Day, result.Hour, result.Minute, result.Second, DateTimeKind.Local);

				var japsTimeConverted = TimeZoneInfo.ConvertTime(result, GetTimeZoneWrapped("Tokyo Standard Time", "Asia/Tokyo"));
				var sydneyTimeConverted = TimeZoneInfo.ConvertTime(result, GetTimeZoneWrapped("AUS Eastern Standard Time", "Australia/Sydney"));
				var europeanTimeConverted = TimeZoneInfo.ConvertTime(result, GetTimeZoneWrapped("Central European Standard Time", "Europe/Stockholm"));
				var estTimeConverted = TimeZoneInfo.ConvertTime(result, GetTimeZoneWrapped("Eastern Standard Time", "America/New_York"));
				var calishitTime = TimeZoneInfo.ConvertTime(result, GetTimeZoneWrapped("Pacific Standard Time", "America/Los_Angeles"));

				var text = $"{result.ToShortTimeString()} ({GetUTCMark(result)}) is {japsTimeConverted.ToShortTimeString()} in Tokyo Time, {sydneyTimeConverted.ToShortTimeString()} in Sedney (East Australia), {europeanTimeConverted.ToShortTimeString()} in Central Europe (CET), {estTimeConverted.ToShortTimeString()} on Eastern Coast of US (EST) and {calishitTime.ToShortTimeString()} on the Western Coast of US (PT).";
				ChannelInstance.SendChatMessageResponse(lastMessage, text);
			}
			else
				ChannelInstance.SendChatMessageResponse(lastMessage, "Couldn't parse Date Time.");
		}

		private string GetUTCMark(DateTime dateTime)
		{
			var result = dateTime.Hour - dateTime.ToUniversalTime().Hour;
			if (result >= 0)
				return $"UTC +{result}";
			else
				return $"UTC {result}";
		}

		private TimeZoneInfo GetTimeZoneWrapped(string Windows, string Mono)
		{
			try
			{
				return TimeZoneInfo.FindSystemTimeZoneById(Windows);
			}
			catch (Exception ex)
			{
				return TimeZoneInfo.FindSystemTimeZoneById(Mono);
			}
		}
	}
}
