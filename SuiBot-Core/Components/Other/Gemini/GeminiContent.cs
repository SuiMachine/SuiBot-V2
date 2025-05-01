using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SuiBot_Core.Components.Other.Gemini
{
	[Serializable]
	public class GeminiContent
	{
		[Serializable]
		public class GenerationConfig
		{
			[JsonIgnore] public int TokenCount = 0;
			public float temperature = 1f;
			[XmlIgnore] public float topK = 20;
			[XmlIgnore] public float topP = 0.95f;
			[XmlIgnore] public int maxOutputTokens = 8192;
			[XmlIgnore] public string responseMimeType = "text/plain";
		}

		public List<GeminiMessage> contents;
		[XmlIgnore]	public GeminiMessage systemInstruction;

		[XmlIgnore]
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public List<GeminiTools> tools;

		public static List<GeminiTools> GetTools()
		{
			return new List<GeminiTools>()
			{
				new GeminiTools()
			};
		}

		public GenerationConfig generationConfig;
	}

	[Serializable]
	public class GeminiResponse
	{
		[Serializable]
		public class GeminiResponseCandidates
		{
			public GeminiMessage content;
			public string finishReason = "";
			public double avgLogprobs = 0;
		}

		[Serializable]
		public class UsageMetadata
		{
			public int promptTokenCount = 0;
			public int candidatesTokenCount = 0;
			public int totalTokenCount = 0;
		}

		public GeminiResponseCandidates[] candidates;
		public UsageMetadata usageMetadata;
		public string modelVersion;
	}
}
