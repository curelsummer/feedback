using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;
using BDF;

namespace BDF
{
    /**
     * A DataRecord holds all of the signals/channels for a defined interval.  Each of the signals/channels has all of the samples for that interval bound to it.
     * 
    // */
    //public class BDFDataRecord:SortedList<string, List<double>>
    //{        
    //    //a datarecord is a SortedList where the key is the channel/signal and the value is the List of Samples (floats) within the datarecord
    //}


    public class BDFDataRecord
    {
        public SortedList<string, List<double>> Signals;
        public List<BDFAnnotation> Annotations;
        public DateTime baseTime;
        bool hasBaseTime = false;


        //public BDFDataRecord()
        //{
        //    Signals = new SortedList<string, List<double>>(); 
        //    Annotations = new List<BDFAnnotation>();
        //    hasBaseTime = false;
        //}

        public BDFDataRecord(DateTime BaseTime)
        {
            baseTime = BaseTime;
            hasBaseTime = true;
            Signals = new SortedList<string, List<double>>();
            Annotations = new List<BDFAnnotation>();
            //double diff = (DateTime.Now.Millisecond - BaseTime.Millisecond) / 1000;
            //addAnnotation(diff);
        }

        public BDFDataRecord(DateTime BaseTime, DateTime InitialTime, params string[] EVENTS)
        {
            baseTime = BaseTime;
            hasBaseTime = true;
            Signals = new SortedList<string, List<double>>();
            Annotations = new List<BDFAnnotation>();
            TimeSpan ts = InitialTime - BaseTime;
            double diff = ts.TotalMilliseconds / 1000.0  ;
            addAnnotation(diff, EVENTS);
        }
        public BDFDataRecord(DateTime BaseTime, double InitialSecondsFromBasetime, params string[] EVENTS)
        {
            baseTime = BaseTime;
            hasBaseTime = true;
            Signals = new SortedList<string, List<double>>();
            Annotations = new List<BDFAnnotation>();
            double diff = InitialSecondsFromBasetime  ;
            addAnnotation(diff, EVENTS);
        }

        public void addAnnotation(double diff, params string[] EVENTS)
        {
            if (!hasBaseTime)
                throw new ArgumentException("DataRecord must set BaseTime");
            BDFAnnotation anno = new BDFAnnotation(diff + baseTime.Millisecond / 1000.0, EVENTS);
            Annotations.Add(anno);
        }

        public void addAnnotation(double diff, double lasting, params string[] EVENTS)
        {
            if (!hasBaseTime)
                throw new ArgumentException("DataRecord must set BaseTime");
            BDFAnnotation anno = new BDFAnnotation(diff + baseTime.Millisecond / 1000.0, lasting, EVENTS);
            Annotations.Add(anno);
        }
        public void addAnnotation(DateTime annoTime, params string[] EVENTS)
        {
            if (!hasBaseTime)
                throw new ArgumentException("DataRecord must set BaseTime");


            double diff = (annoTime - baseTime).TotalMilliseconds / 1000.0;
            BDFAnnotation anno = new BDFAnnotation(diff + baseTime.Millisecond / 1000.0, EVENTS);
            Annotations.Add(anno);
        }
        public void addAnnotation(DateTime annoTime, double lasting, params string[] EVENTS)
        {
            if (!hasBaseTime)
                throw new ArgumentException("DataRecord must set BaseTime");
            double diff = (annoTime - baseTime).TotalMilliseconds / 1000.0;
            BDFAnnotation anno = new BDFAnnotation(diff + baseTime.Millisecond / 1000.0, lasting, EVENTS);
            Annotations.Add(anno);
        }
        public List<byte> getAnnotationListByte()
        {
            List<byte> ans = new List<byte>();
            BDFAnnotation tmpAnnotation = Annotations[0];
            if (tmpAnnotation.timeDiff < 0)
                ans.Add(0x2d);
            else
                ans.Add(0x2b);

            //toString的格式控制BDF文件中数据的时间间隔的精确度。
            List<byte> doublelist = Encoding.ASCII.GetBytes(tmpAnnotation.timeDiff.ToString("F5")).ToList();
            ans.AddRange(doublelist);
            ans.Add(0x14); ans.Add(0x14);
            foreach (var item in tmpAnnotation.Events)
            {
                ans.AddRange(Encoding.ASCII.GetBytes(item).ToList());
                ans.Add(0x14);
            }
            ans.Add(0x00);
            for (int i = 1; i < Annotations.Count; i++)
            {
                List<byte> tmm = Annotations[i].toByteList();
                ans.AddRange(tmm);
            }

            return ans;
        }

        public void parseAnnotations(byte[] input)
        {
            hasBaseTime = true;
            bool gotFirst = false;
            byte[] tmp = new byte[input.Length];
            int ii = 0, index = 0;
            while (ii < input.Length - 2 && (!(input[ii] == 0x00 && input[ii + 1] == 0x00)))
            {
                tmp.Initialize(); index = 0;
                while (input[ii] != 0x00)
                {
                    tmp[index] = input[ii];
                    index++; ii++;
                }
                ii++;
                byte[] tmpp = new byte[index + 1];
                for (int i = 0; i < tmpp.Length; i++)
                {
                    tmpp[i] = tmp[i];
                }
                tmpp[index] = 0x00;
                if (gotFirst)
                    Annotations.Add(parseOneAnnotation(tmpp));
                else
                {
                    Annotations.Add(parseFirstAnnotation(tmpp));
                    gotFirst = true;
                }
            }
        }

        private BDFAnnotation parseOneAnnotation(byte[] input)
        {
            BDFAnnotation annotation = new BDFAnnotation();
            int ii = 1;
            byte[] tmp = new byte[input.Length];
            while (!(input[ii] == 0x14 || input[ii] == 0x15))
            {
                tmp.Initialize();
                tmp[ii - 1] = input[ii];
                ii++;
            }
            double timediff = Convert.ToDouble(System.Text.Encoding.Default.GetString(tmp).Trim());

            if (input[0] == 0x2b)
            {
                annotation.timeDiff = timediff;
            }
            else
            {
                if (input[0] == 0x2d)
                    annotation.timeDiff = -timediff;
                else
                    throw new ArgumentException();
            }

            int index = 0;
            if (input[ii] == 0x15)
            {
                ii++;
                tmp.Initialize();
                while (input[ii] != 0x14)
                {
                    tmp[index] = input[ii];
                    ii++;
                }
                annotation.LastingTime = Convert.ToDouble(System.Text.Encoding.Default.GetString(tmp).Trim());
            }

            while (input[ii] == 0x14)
            {
                ii++;
            }
            while (input[ii] != 0x00)
            {
                index = 0;
                tmp.Initialize();
                while (input[ii] != 0x14)
                {
                    tmp[index] = input[ii];
                    ii++; index++;
                }
                ii++;

                annotation.Events.Add(System.Text.Encoding.Default.GetString(tmp).Replace('\0', ' ').Trim());
            }

            return annotation;
        }

        private BDFAnnotation parseFirstAnnotation(byte[] input)
        {
            BDFAnnotation annotation = new BDFAnnotation();

            int ii = 1;
            byte[] tmp = new byte[input.Length];
            tmp.Initialize();
            while (input[ii] != 0x14)
            {
                tmp[ii - 1] = input[ii];
                ii++;
            }
            double timediff = Convert.ToDouble(System.Text.Encoding.Default.GetString(tmp).Trim());

            if (input[0] == 0x2b)
            {
                annotation.timeDiff = timediff;
            }
            else
            {
                if (input[0] == 0x2d)
                    annotation.timeDiff = -timediff;
                else
                    throw new ArgumentException();
            }

            ii += 2;
            int index = 0;

            while (input[ii] != 0x00)
            {
                tmp.Initialize();
                index = 0;
                while (input[ii] != 0x14)
                {
                    tmp[index] = input[ii];
                    index++; ii++;
                }
                annotation.Events.Add(System.Text.Encoding.Default.GetString(tmp).Replace('\0', ' ').Trim());
                ii++;
            }

            return annotation;
        }
    }
}
