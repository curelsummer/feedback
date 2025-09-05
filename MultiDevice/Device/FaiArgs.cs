using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public class FailArgs : EventArgs
    {
        public Exception e;
        public string AddtionalStr;
        //public FailArgs(Exception ee)
        //{
        //    e = ee;
        //}
        public FailArgs(Exception ee,string addtionStr)
        {
            e = ee;
            AddtionalStr = addtionStr;
        }
    }
}
