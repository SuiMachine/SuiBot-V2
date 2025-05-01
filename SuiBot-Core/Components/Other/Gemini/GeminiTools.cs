using SuiBot_Core.Components.Other.Gemini.FunctionTypes;
using System;
using System.Collections.Generic;

namespace SuiBot_Core.Components.Other.Gemini
{
	public class GeminiTools
	{
		[Serializable]
		public class GeminiFunction
		{
			public string name;
			public string description;
			public ParametersContainer parameters;

			public GeminiFunction()
			{
				name = "";
				description = "";
				parameters = null;
			}

			public GeminiFunction(string name, string description, GeminiProperty functionDefinition)
			{
				this.name = name;
				this.description = description;
				this.parameters = new ParametersContainer()
				{
					type = "object",
					properties = functionDefinition,
					required = new List<string>(),
				};
			}
		}

		public List<GeminiFunction> functionDeclarations;

		public GeminiTools()
		{
			functionDeclarations = new List<GeminiFunction>()
			{
				new GeminiFunction("timeout", "time outs a user in the chat", new TimeOutParameters()),
				new GeminiFunction("ban", "bans a user", new BanParameters()),
			};
		}
	}
}
