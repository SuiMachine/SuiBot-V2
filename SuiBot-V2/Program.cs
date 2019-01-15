using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SuiBot_Core;

namespace SuiBot_V2
{
    class Program
    {
        static SuiBot bot;

        static void Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            //var APITest = new SuiBot_Core.TwitchStatusUpdate("havrd");
            //APITest.GetStatus();

            if(File.Exists("Bot/ConnectionConfig.suixml"))
            {
                bot = new SuiBot();
                bot.Connect();

                while (bot.IsRunning)
                    System.Threading.Thread.Sleep(1);
            }
            else
            {
                var configFile = new SuiBot_Core.Storage.ConnectionConfig();
                configFile.Save();
                Console.WriteLine("No connection config was found, so a new file was created.");

                if(!File.Exists("Bot/Config.xml"))
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
