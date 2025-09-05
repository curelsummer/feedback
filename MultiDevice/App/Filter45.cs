using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDevice
{
    class Filter45
    {
        const int para_len = 100;

        static double[] para = new double[para_len];
        double[] data_cache = new double[para_len];
        int curr_idx = 0;
        Filter45()
        {
            for (int i = 0; i < para_len; i++)
            {
                data_cache[i] = 0;
            }
            //para=?
        }

        public double DoFiltering(double x)
        {
            data_cache[(curr_idx + para_len) % 100] = x;
            curr_idx++;
            curr_idx = curr_idx % 100;

            double toReturn = 0;
            for (int i = 0; i < para_len; i++)
            {
                toReturn = toReturn + (para[i] * data_cache[(i + curr_idx) % 100]);
            }

            return toReturn;
        }
    }
}
