using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDevice
{
    class MarkerEventArgs : EventArgs
    {
        public DateTime onset = DateTime.Now;
        public double mill_sec_lasting = 0;
        public string anno_str = "";

        public MarkerEventArgs(DateTime dateTime, double ms, string annotation)
        {
            onset = dateTime;
            mill_sec_lasting = ms;
            anno_str = annotation;
        }
    }
}
