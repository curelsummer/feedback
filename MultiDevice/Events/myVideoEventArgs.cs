using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDevice
{
    public class myVideoEventArgs : EventArgs
    {
        string message = "";
        public myVideoEventArgs(string msg)
        {
            message = msg;
        }
        public string Message
        {
            get { return message; }
        }
    }
}
