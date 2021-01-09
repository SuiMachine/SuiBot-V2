using System;
using System.Diagnostics;
using System.IO;

namespace SuiBot_Core
{
	static class ErrorLogging
	{
		const string FILENAME = "SuiBot-Core.log";

		public static void WriteLine(string Error)
		{

			string errorToSave = string.Format("{0}: {1}", DateTime.Now.ToString(), Error);
#if DEBUG
            Debug.WriteLine(errorToSave);
#endif
			File.AppendAllText(FILENAME, errorToSave + "\n");

		}

		internal static void Close()
		{
		}
	}
}
