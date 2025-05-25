using System;
using System.Diagnostics;
using System.IO;

namespace SuiBot_Core
{
	static class ErrorLoggingSocket
	{
		const string FILENAME = "SuiBot-TwitchSocket.log";

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
