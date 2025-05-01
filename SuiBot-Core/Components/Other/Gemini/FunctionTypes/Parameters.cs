using System;
using System.Collections.Generic;

namespace SuiBot_Core.Components.Other.Gemini.FunctionTypes
{
	[Serializable]
	public class ParametersContainer
	{
		public string type = "object"; //return type
		public GeminiProperty properties = null;
		public List<string> required = new List<string>();

		public ParametersContainer() { }
	}

	[Serializable]
	public abstract class GeminiProperty
	{
		public class Parameter_String
		{
			public string type = "string";
		}

		public class Parameter_Number
		{
			public string type = "number";
		}
	}

	[Serializable]
	public class TimeOutParameters : GeminiProperty
	{
		public Parameter_String username;
		public Parameter_Number duration_in_seconds;

		public TimeOutParameters()
		{
			this.username = new Parameter_String();
			this.duration_in_seconds = new Parameter_Number();
		}
	}

	[Serializable]
	public class BanParameters : GeminiProperty
	{
		public Parameter_String username;

		public BanParameters()
		{
			this.username = new Parameter_String();
		}
	}
}
