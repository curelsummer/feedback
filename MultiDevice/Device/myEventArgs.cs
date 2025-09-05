using System;
using System.IO;

namespace MultiDevice
{
    public class myEventArgs : EventArgs
    {
        int[] data = new int[256];
        public myEventArgs(int[] tmp)
        {
            data = tmp;
        }
        public int[] Data
        {
            get
            {
                return data;
            }
            set
            {
                if (value.Length == 256)
                {
                    data = value;
                }
                else
                {
                    throw new InvalidDataException("wrong numbers");
                }
            }
        }
    }
}