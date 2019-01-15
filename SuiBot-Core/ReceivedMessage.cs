using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuiBot_Core
{
    public class ReceivedMessage
    {
        public string Channel { get; set; }
        public Role UserRole { get; set; }
        public string User { get; set; }
        public string Message { get; set; }
    }
}
