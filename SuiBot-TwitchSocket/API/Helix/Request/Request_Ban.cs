using System;

namespace SuiBot_Core.API.Helix.Request
{
	public class Request_Ban
	{
		public Request_Ban_Data data;

		public class Request_Ban_Data
		{
			public ulong user_id;
			public int? duration;
			public string reason;
		}		

		private Request_Ban() { }

		public static Request_Ban CreateBan(ulong user_id, string reason = null)
		{
			return new Request_Ban()
			{
				data = new Request_Ban_Data
				{
					user_id = user_id,
					reason = reason,
					duration = null
				}
			};
		}

		public static Request_Ban CreateTimeout(ulong user_id, int duration_in_seconds, string reason)
		{
			if (duration_in_seconds > 1_209_600)
				throw new Exception("Timeout can not be longer than 2 weeks!");

			return new Request_Ban()
			{
				data = new Request_Ban_Data
				{
					user_id = user_id,
					reason = reason,
					duration = duration_in_seconds
				}
			};
		}

		public static Request_Ban CreateTimeout(ulong user_id, TimeSpan duration, string reason)
		{
			if (duration > TimeSpan.FromDays(14))
				throw new Exception("Timeout can not be longer than 2 weeks!");

			return new Request_Ban()
			{
				data = new Request_Ban_Data
				{
					user_id = user_id,
					reason = reason,
					duration = (int)duration.TotalSeconds
				}
			};
		}
	}
}
