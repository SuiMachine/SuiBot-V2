using System;
using System.Diagnostics;
using System.IO;

namespace SuiBot_Core
{
	internal static class ErrorLogging
	{
		const string FILENAME = "SuiBot-Core.log";

		public static void WriteLine(string text)
		{
			Console.WriteLine(text);
			string textToSave = $"{DateTime.Now}: {text}";
#if DEBUG
            Debug.WriteLine(textToSave);
#endif
			File.AppendAllText(FILENAME, textToSave + "\n");
		}
	}
}
