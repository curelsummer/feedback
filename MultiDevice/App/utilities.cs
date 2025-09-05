using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiDevice
{
    public class UserProfile
    {
        public bool individual = false;
        public float IAPF = 10;
        public float IAF_low = 8;
        public float IAF_high = 13;
    }

    //all enumeration of indices puts here
    public enum indices_enu
    {
        // 此处每种方法的索引需要重新定义
        absolute_power = 0,
        relative_power = 1,
        power_ratio = 2,
        indiv_alpha = 3,  // the alpha peak frequency is calculated as time-varying
        the_alpha = 4,    // the alpha peak frequency is pre-defined, fixed, not time-varying
        EMG_ratio=11,     // the relative power of EMG
        artifacts = 98,   // power supply, muscle, max_value, max_p2p
        sham_power = 99,
    }
}
