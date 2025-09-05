 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MultiDevice.FFTLibrary;

namespace MultiDevice
{ 
    class Indice_Alpha : IndiceToBytes
    {
        const int sampling_rate = 2000;

        const int num200ms_for_cal = 10;  // 8s:200ms*40
        const int re_fs_ratio = 8;// downsample 2000 Hz to 200Hz
        double[,] data200ms = new double[9, (int)(0.2*sampling_rate)];
        double[,] data_for_calu = new double[9, num200ms_for_cal * (int)(0.2*sampling_rate)];
        bool calculation_available = false;

        public delegate void CalcuEvent(object sender, MyEventArgs_indice e);
        public event CalcuEvent CalculationDone;
        public float[][] ratios = new float[9][];
        //num of power ratio features
        int num_indice = 7;

        int current_index200ms = 0;

        public double IAF_low = 7, IAF_high = 13, IAPF = 10;

        double[] hamming_window = new double[] { 1.0, 2.0 };
        double[] hamming_window200ms = new double[] { 1.0, 2.0 };
        List<double[]> winds = new List<double[]>();
        List<int> Channels_involved = null;
        public Indice_Alpha(List<int> channels_involved)
        {
            ttt = DateTime.Now;
            Channels_involved = channels_involved;
            List<MathNet.Filtering.Windowing.Window> winds_dotnet = new List<MathNet.Filtering.Windowing.Window>();
            var tmp_var = (new MathNet.Filtering.Windowing.HammingWindow());
            tmp_var.Width = 1024;
            hamming_window = tmp_var.CopyToArray();
            var tmp_var2 = (new MathNet.Filtering.Windowing.HammingWindow());
            tmp_var2.Width = 256;
            hamming_window200ms = tmp_var2.CopyToArray();
            winds_dotnet.Add(new MathNet.Filtering.Windowing.HammingWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.HannWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.BlackmanWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.BartlettWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.BartlettHannWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.BlackmanNuttallWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.NuttallWindow());
            winds_dotnet.Add(new MathNet.Filtering.Windowing.RectangularWindow());
            for (int zzzz = 0; zzzz < winds_dotnet.Count; zzzz++)
            {
                var xxx = winds_dotnet[zzzz];
                xxx.Width = num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio;
            }
            for (int zzzz = 0; zzzz < winds_dotnet.Count; zzzz++)
            {
                var xxx = winds_dotnet[zzzz];
                winds.Add(xxx.CopyToArray());
            }
        } 
        public double[,] Data200ms
        {
            //get { }
            set
            {
                if (value.Length != 9 * (int)(0.2 * sampling_rate))
                    throw new ArgumentException("Data should be doule array:9 X 200 ");
                foreach (var i in Channels_involved)
                {
                    double tmp = 0;
                    for (int j = 0; j < (int)(0.2*sampling_rate); j++)
                    {
                        tmp += value[i, j];
                    }
                    tmp = tmp / (int)(0.2*sampling_rate);
                    for (int j = 0; j < (int)(0.2*sampling_rate); j++)
                    {
                        value[i, j] = value[i, j] - tmp;
                    }
                }

                if (true)
                {
                    foreach (var i in Channels_involved)
                    {
                        for (int j = 0; j < (int)(0.2*sampling_rate); j++)
                        {
                            data_for_calu[i, (int)(0.2*sampling_rate) * current_index200ms + j] = value[i, j];
                        }
                    }
                    current_index200ms++;
                    Console.WriteLine("current sec (200ms):" + (current_index200ms / 5.0));
                    if (current_index200ms == num200ms_for_cal)
                    {
                        current_index200ms = 0;
                        calculation_available = true;
                    }
                    if (calculation_available)
                    {
                        //计算
                        //Thread t = new Thread(cal_ans);
                        //t.Start();
                        cal_ans();
                    }
                }
                else
                {
                    //set all indices equal 0 if the data was rejected

                    for (int channel = 0; channel < 9; channel++)
                    {
                        ratios[channel] = new float[num_indice];
                        for (int qqq = 0; qqq < num_indice; qqq++)
                        {
                            ratios[channel][qqq] = 0;
                        }
                    }
                    Console.WriteLine("ts:"+(DateTime.Now-ttt).TotalMilliseconds); ;ttt = DateTime.Now;
                    CalculationDone?.Invoke(this, new MyEventArgs_indice(indices_enu.the_alpha));
                }
            }
        }
        DateTime ttt = DateTime.Now;

        void cal_ans()
        {
            var power = (int)Math.Floor(Math.Log(num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio) / Math.Log(2)) + 1;
            int lengthofpower = (int)Math.Pow(2, power);
            int units_length = (int)(lengthofpower /1.0/ sampling_rate * re_fs_ratio);

            double[] data2cal = new double[lengthofpower];
            double[] data2cal_img = new double[lengthofpower];
            double[] data2cal_tmp = new double[lengthofpower];

            int numOfCriteria = 2;
            int[,] Chan_avail = new int[Channels_involved.Count, numOfCriteria];
            int[] Chan_avail_confirm = new int[Channels_involved.Count];


            foreach (var channel in Channels_involved)
            {
                for (int i = 0; i < lengthofpower; i++)
                {
                    data2cal[i] = 0;
                    data2cal_img[i] = 0;
                    data2cal_tmp[i] = 0;
                }

                int floating_index = 0;
                for (int i = 0; i < num200ms_for_cal; i++)
                {
                    for (int j = 0; j < (int)(0.2*sampling_rate) / re_fs_ratio; j++)
                    {
                        floating_index = (i + current_index200ms) % num200ms_for_cal;
                        double tmpppp = 0;
                        for (int qz = 0; qz < re_fs_ratio; qz++)
                        {
                            var xxx = (i + current_index200ms) * (int)(0.2*sampling_rate) + j * re_fs_ratio + qz;
                            xxx = xxx % (num200ms_for_cal * (int)(0.2*sampling_rate));
                            tmpppp += data_for_calu[channel, xxx];
                        }
                        tmpppp = tmpppp / re_fs_ratio;

                        data2cal_tmp[i * (int)(0.2*sampling_rate) / re_fs_ratio + j] = tmpppp;
                    }
                }
                //using (FileStream fs = new FileStream(Path.Combine("data_" + channel + ".txt"), FileMode.Append, FileAccess.Write))
                //using (StreamWriter writer = new StreamWriter(fs))
                //{
                //    foreach (var item in data2cal_tmp)
                //    {
                //        writer.WriteLine(item.ToString("F3"));
                //    }
                //}

                double[] data2cal_all = new double[lengthofpower];
                double[] data2cal_all_img = new double[lengthofpower];
                for (int ijij = 0; ijij < lengthofpower; ijij++)
                {
                    data2cal_all[ijij] = 0;
                    data2cal_all_img[ijij] = 0;
                }

                for (int zzzz = 0; zzzz < winds.Count; zzzz++)
                {
                    var s4 = winds[zzzz];
                    for (int i = 0; i < num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio; i++)
                    {
                        data2cal[i] = data2cal_tmp[i] * s4[i];
                        data2cal_img[i] = 0;
                    }
                    for (int i = num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio; i < lengthofpower; i++)
                    {
                        data2cal[i] = 0;
                        data2cal_img[i] = 0;
                    }
                    MyComplex.FFT(1, power, data2cal, data2cal_img);

                    for (int ijij = 0; ijij < lengthofpower; ijij++)
                    {
                        data2cal_all[ijij] += data2cal[ijij];
                        data2cal_all_img[ijij] += data2cal_img[ijij];
                    }
                }
                for (int ijij = 0; ijij < lengthofpower; ijij++)
                {
                    data2cal_all[ijij] = data2cal_all[ijij] / winds.Count;
                    data2cal_all_img[ijij] = data2cal_all_img[ijij] / winds.Count;
                } 
                ratios[channel] = new float[num_indice];
                ratios[channel] = power_ratio(data2cal_all, data2cal_all_img, sampling_rate / re_fs_ratio); double[] abs_value = abs_real_img(data2cal_all, data2cal_all_img);
                //if (power_50_supply(abs_value, sampling_rate / re_fs_ratio) > 0.0574 + 3 * 0.0431)
                //{
                //    Chan_avail[Channels_involved.IndexOf(channel), 0] = 1;
                //}
                //if (power_muscle(abs_value, sampling_rate / re_fs_ratio) > 0.7099 + 3 * 0.0503)
                //{
                //    Chan_avail[Channels_involved.IndexOf(channel), 1] = 1;
                //}
            }
            for (int i = 0; i < Channels_involved.Count; i++)
            {
                for (int j = 0; j < numOfCriteria; j++)
                {
                    Chan_avail_confirm[i] += Chan_avail[i, j];
                }
            }
            int Bad_chann = 0;
            for (int i = 0; i < Channels_involved.Count; i++)
            {
                if (Chan_avail_confirm[i] > 0)
                    Bad_chann++;
            }
            //   more than half of channels are bad
            if (Bad_chann / ((double)Channels_involved.Count) > 0.4)
            {
                Console.WriteLine("rejected epoch");
            }
            else
            {
                Console.WriteLine("admitted epoch");
                for (int i = 0; i < 9; i++)
                {
                    if (!Channels_involved.Contains(i))
                    {

                        ratios[i] = new float[num_indice];
                    }
                }
                CalculationDone?.Invoke(this, new MyEventArgs_indice(indices_enu.the_alpha));
                // LogHelper.Log.LogInfo($"Indice_Alpha 数据推送:{indices_enu.the_alpha}");
            }
        }

        float power_50_supply(double[] abs_value1, int fs)
        {
            int fft_length = abs_value1.Length;
            int units_length = fft_length / fs;
            double all_power = 0;
            double power_supply_power = 0;

            for (int i = units_length * 1; i < 125 * units_length; i++)
            {
                all_power += abs_value1[i];
            }

            for (int i = units_length * 46; i < 55 * units_length; i++)
            {
                power_supply_power += abs_value1[i];
            }
            for (int i = units_length * 96; i < 105 * units_length; i++)
            {
                power_supply_power += abs_value1[i];
            }

            return (float)(power_supply_power / all_power);
        }
        float power_muscle(double[] abs_value1, int fs)
        {
            int fft_length = abs_value1.Length;
            int units_length = fft_length / fs;
            double all_power = 0;
            double power_supply_power = 0;
            double power_muscle_muscle = 0;

            for (int i = units_length; i < 125 * units_length; i++)
            {
                all_power += abs_value1[i];
            }

            for (int i = units_length * 46; i < 55 * units_length; i++)
            {
                power_supply_power += abs_value1[i];
            }
            for (int i = units_length * 96; i < 105 * units_length; i++)
            {
                power_supply_power += abs_value1[i];
            }


            for (int i = units_length * 30; i < 125 * units_length; i++)
            {
                power_muscle_muscle += abs_value1[i];
            }

            return (float)((power_muscle_muscle - power_supply_power) / (all_power - power_supply_power));
        }
        float[] power_ratio(double[] real, double[] img, int fs)
        {
            float[] ratios = new float[num_indice];
            int fft_length = real.Length;
            int units_length = fft_length / fs;
            double[] abs_value = abs_real_img(real, img);
            double all_power = 0;

            int freqs_len = 0;
            //band of interest:1-45 Hz
            for (int i = units_length * 1; i < 45 * units_length; i++)
            {
                all_power += abs_value[i];
                freqs_len++;
            }
            for (int i = 0; i < abs_value.Length; i++)
            {
                abs_value[i] = abs_value[i] / all_power;
            }
            all_power = 0;
            for (int i = units_length * 1; i < 45 * units_length; i++)
            {
                all_power += abs_value[i];
            }
             
            double alpha_p = 0, alpha_a = 0, low_alpha_p = 0, low_alpha_a = 0, high_alpha_p = 0, high_alpha_a = 0;
            for (int i = (int)(units_length * IAF_low); i < (int)(units_length * IAF_high); i++)
            {
                alpha_p += abs_value[i];
            }
            alpha_a = (alpha_p / ((IAF_high - IAF_low) * units_length)) / (all_power / (44 * units_length));

            for (int i = (int)(units_length * IAF_low); i < (int)(units_length * IAPF  ); i++)
            {
                low_alpha_p += abs_value[i];
            }
            low_alpha_a = (low_alpha_p / ((IAPF - IAF_low) * units_length)) / (all_power / (44 * units_length));

            for (int i = (int)(units_length * IAPF); i < (int)(units_length * IAF_high); i++)
            {
                high_alpha_p += abs_value[i];
            }
            high_alpha_a = (high_alpha_p / ((IAF_high-IAPF) * units_length)) / (all_power / (44 * units_length));


            ratios[0] = (float)(high_alpha_p/low_alpha_p);

            ratios[1] = (float)alpha_p;
            ratios[2] = (float)alpha_a;

            ratios[3] = (float)low_alpha_p;
            ratios[4] = (float)low_alpha_a;

            ratios[5] = (float)high_alpha_p;
            ratios[6] = (float)high_alpha_a;

            return ratios;
        }
        double[] abs_real_img(double[] real, double[] img)
        {
            int data_length = real.Length;
            double[] data_abs = new double[data_length];
            for (int i = 0; i < data_length; i++)
            {
                //data_abs[i] = Math.Sqrt(Math.Pow(real[i], 2) + Math.Pow(img[i], 2));
                data_abs[i] =  Math.Pow(real[i], 2) + Math.Pow(img[i], 2) ;
            }
            return data_abs;
        }

        public byte[] toBytes()
        {

            byte[][] a = new byte[num_indice * 9][];

            int doules_length = 0;
            for (int channel = 0; channel < 9; channel++)
            {
                for (int i = 0; i < num_indice; i++)
                {
                    a[num_indice * channel + i] = BitConverter.GetBytes(ratios[channel][i]);
                    doules_length += a[num_indice * channel + i].Length;
                }
            }

            byte[] c = new byte[doules_length + 2];

            //前两位是方法的enumeration代码，见：indices_enu ,注意字节序，是高位在后
            c[0] = Convert.ToByte(4);

            //高位
            c[1] = Convert.ToByte(0);


            int all_length = 2;
            for (int i = 0; i < num_indice * 9; i++)
            {
                System.Array.Copy(a[i], 0, c, all_length, a[i].Length);
                all_length += a[i].Length;
            }
            return c;
        }
    }
}
