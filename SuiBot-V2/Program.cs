using SuiBot_Core;
using SuiBot_TwitchSocket;
using System;
using System.IO;

namespace SuiBot_V2
{
	class Program
	{
		static SuiBot bot;

		static void Main(string[] args)
		{
			Console.CancelKeyPress += Console_CancelKeyPress;

			if (File.Exists("Bot/ConnectionConfig.suixml"))
			{
				bot = SuiBot.GetInstance();
				bot.Connect();

				while (!bot.IsDisposed)
					System.Threading.Thread.Sleep(15);
			}
			else
			{
				var configFile = new ConnectionConfig();
				configFile.Save();
				Console.WriteLine("No connection config was found, so a new file was created.");

				if (!File.Exists("Bot/Config.xml"))
				{
					var coreConfig = new SuiBot_Core.Storage.CoreConfig();
					coreConfig.ChannelsToJoin.Add("ExampleChannel");
					coreConfig.Save();
					Console.WriteLine("No core config was found, so a new file was created.");
				}

				Console.ReadKey();
			}
		}

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			bot.Shutdown();
		}
	}
}
