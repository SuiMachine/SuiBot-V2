using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;

namespace SuiBot_Core.Components.Other.Gemini
{
	[Serializable]
	public class GeminiMessage
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public Role role;
		public GeminiMessagePart[] parts;

		public static GeminiMessage CreateUserResponse(string contentToAsk)
		{
			return new GeminiMessage()
			{
				role = Role.user,
				parts = new GeminiMessagePart[]
				{
					new GeminiMessagePart()
					{
						text = contentToAsk.Trim()
					}
				}
			};
		}
	}

	[Serializable]
	public class GeminiMessagePart
	{
		public string text;
	}

	public enum Role
	{
		user,
		model
	}
}
