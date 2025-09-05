using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDevice
{

    public class MyEventArgs_indice : EventArgs
    { 
        public string hint;
        public indices_enu indice_code;
       
        //public MyEventArgs_indice(string obj)
        //{
        //    hint = obj;
        //}
        public MyEventArgs_indice(indices_enu indice_code)
        {
            this.indice_code = indice_code;
        }
    }
}
