using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace SuiBot_Core
{
    static class ErrorLogging
    {
        static StreamWriter sr = new StreamWriter("SuiBot-Core.log", true);

        public static void WriteLine(string Error)
        {
            string errorToSave = string.Format("{0}: {1}", DateTime.Now.ToString(), Error);
#if DEBUG
            Debug.WriteLine(errorToSave);
#endif 
            sr.WriteLine(errorToSave);
        }

        internal static void Close()
        {
            sr.Close();
        }
    }
}
