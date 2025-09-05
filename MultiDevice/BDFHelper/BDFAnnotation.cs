using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDF
{
    public class BDFAnnotation
    {
        public double timeDiff;//与开始时间的时间差，单位是s，
        public double LastingTime;//持续时间，单位还是s
        public List<string> Events;//事件列表
        public BDFAnnotation()
        {
            timeDiff = 0;
            LastingTime = 0;
            Events = new List<string>();
        }
        public BDFAnnotation(double diff)
        {
            timeDiff = diff;
            LastingTime = 0;
            Events = new List<string>();
        }
        public BDFAnnotation(double diff, params string[] events)
        {
            timeDiff = diff;
            this.LastingTime = 0;
            Events = new List<string>();
            foreach (string item in events)
            {
                Events.Add(item);
            }
        }

        public string toString()
        {
            string toREturn = "FromBaseTime:" + timeDiff + "s\t" + "last:" + LastingTime + "s\t" + "EVENTS:";
            foreach (var item in Events)
            {
                toREturn = toREturn + "/" + item;
            }
            return toREturn;
        }

        public BDFAnnotation(double diff, double last, params string[] events)
        {
            timeDiff = diff;
            this.LastingTime = last;
            Events = new List<string>();
            foreach (string item in events)
            {
                Events.Add(item);
            }
        }
        public List<byte> toByteList()
        {
            List<byte> ans = new List<byte>();

            //if (timeDiff < 0)
            //    ans.Add(45);
            //else
            //    ans.Add(43);

            if (timeDiff >= 0)
                ans.Add(43);

            ans.AddRange(Encoding.ASCII.GetBytes(timeDiff.ToString("F5")).ToList());

            //ans.Add(0x20); ans.Add(0x20);

            if (LastingTime > 0)
            {
                ans.Add(21);
                string doublestring = LastingTime.ToString("F5");
                List<byte> doublelist = Encoding.ASCII.GetBytes(doublestring).ToList();
                ans.AddRange(doublelist);
            }

            ans.Add(20);

            foreach (var item in Events)
            {
                ans.AddRange(Encoding.ASCII.GetBytes(item.Replace('\0', ' ').Trim()).ToList());
                ans.Add(20);
            }
            ans.Add(00);
            return ans;
        }
    }
}
