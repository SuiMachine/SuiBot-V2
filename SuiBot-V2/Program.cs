using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            //var APITest = new SuiBot_Core.TwitchStatusUpdate("havrd");
            //APITest.GetStatus();

            bot = new SuiBot();
            bot.Connect();

            while (bot.IsRunning)
                System.Threading.Thread.Sleep(1);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            bot.Shutdown();
        }
    }
}
