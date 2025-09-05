
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using static MultiDevice.FFTLibrary;

namespace MultiDevice
{

    class Indice_Artifact : IndiceToBytes
    {
        const int sampling_rate = 2000;

        const int num200ms_for_cal = 10;  // 8s:200ms*40
        const int re_fs_ratio =2;// downsample 1000 Hz to 250Hz
        double[,] data200ms = new double[9, (int)(0.2*sampling_rate)];
        double[,] data_for_calu = new double[9, num200ms_for_cal * (int)(0.2*sampling_rate)];
        bool calculation_available = false;

        public delegate void CalcuEvent(object sender, MyEventArgs_indice e);
        public event CalcuEvent CalculationDone;
        public float[][] ratios = new float[9][];
        //num of artifact features
        int num_indice = 4;

        int current_index200ms = 0;

        public double IAF_low = 7, IAF_high = 13, IAPF = 10;

        double[] hamming_window = new double[] { 1.0, 2.0 };
        double[] hamming_window200ms = new double[] { 1.0, 2.0 };
        List<double[]> winds = new List<double[]>();
        List<int> Channels_involved = null;
        public Indice_Artifact(List<int> channels_involved)
        {
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
                    throw new ArgumentException("Data should be doule array:9 X (int)(0.2*sampling_rate) ");
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
                    //foreach (var i in Channels_involved)
                    //{
                    //    using (FileStream fs = new FileStream(Path.Combine("data_raw_" + i + ".txt"), FileMode.Append, FileAccess.Write))
                    //    using (StreamWriter writer = new StreamWriter(fs))
                    //    {
                    //        for (int j = 0; j < (int)(0.2*sampling_rate); j++)
                    //        {
                    //            writer.WriteLine(value[i, j].ToString("F3"));

                    //        }
                    //    }
                    //}
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

                    CalculationDone?.Invoke(this, new MyEventArgs_indice(indices_enu.artifacts));
                }
            }
        }

        //void cal_ans()
        //{
        //    var power = (int)Math.Floor(Math.Log(num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio) / Math.Log(2)) + 1;
        //    int lengthofpower = (int)Math.Pow(2, power);
        //    int units_length = (int)(lengthofpower / sampling_rate.0 * re_fs_ratio);

        //    double[] data2cal = new double[lengthofpower];
        //    double[] data2cal_img = new double[lengthofpower];
        //    double[] data2cal_tmp = new double[lengthofpower];
        //    foreach (var channel in Channels_involved)
        //    {
        //        for (int i = 0; i < lengthofpower; i++)
        //        {
        //            data2cal[i] = 0;
        //            data2cal_img[i] = 0;
        //            data2cal_tmp[i] = 0;
        //        }

        //        int floating_index = 0;
        //        for (int i = 0; i < num200ms_for_cal; i++)
        //        {
        //            for (int j = 0; j < (int)(0.2*sampling_rate) / re_fs_ratio; j++)
        //            {
        //                floating_index = (i + current_index200ms) % num200ms_for_cal;
        //                double tmpppp = 0;
        //                for (int qz = 0; qz < re_fs_ratio; qz++)
        //                {
        //                    var xxx = (i + current_index200ms) * (int)(0.2*sampling_rate) + j * re_fs_ratio + qz;
        //                    xxx = xxx % (num200ms_for_cal * (int)(0.2*sampling_rate));
        //                    tmpppp += data_for_calu[channel, xxx];
        //                }
        //                tmpppp = tmpppp / re_fs_ratio;

        //                data2cal_tmp[i * (int)(0.2*sampling_rate) / re_fs_ratio + j] = tmpppp;
        //            }
        //        } 
        //        using (FileStream fs = new FileStream(Path.Combine("data_"+channel+".txt"), FileMode.Append, FileAccess.Write))
        //        using (StreamWriter writer = new StreamWriter(fs))
        //        { 
        //            foreach (var item in data2cal_tmp)
        //            {
        //                writer.WriteLine(item.ToString("F3"));
        //            }
        //        }

        //        double[] data2cal_all = new double[lengthofpower];
        //        double[] data2cal_all_img = new double[lengthofpower];
        //        for (int ijij = 0; ijij < lengthofpower; ijij++)
        //        {
        //            data2cal_all[ijij] = 0;
        //            data2cal_all_img[ijij] = 0;
        //        }

        //        for (int zzzz = 0; zzzz < winds.Count; zzzz++)
        //        {
        //            var s4 = winds[zzzz];
        //            for (int i = 0; i < num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio; i++)
        //            {
        //                data2cal[i] = data2cal_tmp[i] * s4[i];
        //                data2cal_img[i] = 0;
        //            }
        //            for (int i = num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio; i < lengthofpower; i++)
        //            {
        //                data2cal[i] = 0;
        //                data2cal_img[i] = 0;
        //            }
        //            MyComplex.FFT(1, power, data2cal, data2cal_img);

        //            for (int ijij = 0; ijij < lengthofpower; ijij++)
        //            {
        //                data2cal_all[ijij] += data2cal[ijij];
        //                data2cal_all_img[ijij] += data2cal_img[ijij];
        //            }
        //        }
        //        for (int ijij = 0; ijij < lengthofpower; ijij++)
        //        {
        //            data2cal_all[ijij] = data2cal_all[ijij] / winds.Count;
        //            data2cal_all_img[ijij] = data2cal_all_img[ijij] / winds.Count;
        //        }
        //        double[] abs_value = abs_real_img(data2cal_all, data2cal_all_img);
        //        using (FileStream fs = new FileStream(Path.Combine("data_psd_" + channel + ".txt"), FileMode.Append, FileAccess.Write))
        //        using (StreamWriter writer = new StreamWriter(fs))
        //        {
        //            foreach (var item in abs_value)
        //            {
        //                writer.WriteLine(item.ToString("F3"));
        //            }
        //        }

        //        ratios[channel] = new float[num_indice];
        //        ratios[channel] = power_ratio(data2cal_all, data2cal_all_img, sampling_rate / re_fs_ratio);
        //    }
        //    for (int i = 0; i < 9; i++)
        //    {
        //        if (!Channels_involved.Contains(i))
        //        {

        //            ratios[i] = new float[num_indice];
        //        }
        //    } 
        //   CalculationDone?.Invoke(this, new MyEventArgs_indice("calculation of relative power done,refer to: ratios"));
        //} 



        void cal_ans()
        {
            var power = (int)Math.Floor(Math.Log(num200ms_for_cal * (int)(0.2*sampling_rate) / re_fs_ratio) / Math.Log(2)) + 1;
            int lengthofpower = (int)Math.Pow(2, power);
            int units_length = (int)(lengthofpower / 1.0/sampling_rate * re_fs_ratio);

            double[] data2cal = new double[lengthofpower];
            double[] data2cal_img = new double[lengthofpower];
            double[] data2cal_tmp = new double[lengthofpower];

            int numOfCriteria = 2;
            int[,] Chan_avail = new int[Channels_involved.Count, numOfCriteria];
            int[] Chan_avail_confirm = new int[Channels_involved.Count];


            foreach (var channel in Channels_involved)
            {
                ratios[channel] = new float[num_indice];

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

                // the third indice of artifact
                double max_max = -999;
                for (int iii = 0; iii < lengthofpower; iii++)
                {
                    double tmp_abssss = Math.Abs(data2cal_tmp[iii]);
                    if (tmp_abssss > max_max)
                        max_max = tmp_abssss;
                }
                ratios[channel][2] = (float)max_max;


                int length_of_sliding_window = (int)(30.0 / re_fs_ratio);

                List<double> p2p_list = new List<double>();
                for (int iii = 0; iii < lengthofpower / length_of_sliding_window * 2 - 2; iii++)
                {
                    double min_a = 999, max_a = -999;
                    for (int jjj = 0; jjj < length_of_sliding_window; jjj++)
                    {
                        if (data2cal_tmp[iii * length_of_sliding_window / 2 + jjj] > max_a)
                            max_a = data2cal_tmp[iii * length_of_sliding_window / 2 + jjj];
                        if (data2cal_tmp[iii * length_of_sliding_window / 2 + jjj] < min_a)
                            min_a = data2cal_tmp[iii * length_of_sliding_window / 2 + jjj];
                    }

                    p2p_list.Add(max_a - min_a);
                }

                ratios[channel][3] = (float)p2p_list.Max();
                Console.WriteLine("max_ser:" + p2p_list.Max());

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

                double[] abs_value = abs_real_img(data2cal_all, data2cal_all_img);

                // the first and second indice of artifact
                ratios[channel][0] = power_50_supply(abs_value, sampling_rate / re_fs_ratio);
                ratios[channel][1] = power_muscle(abs_value, sampling_rate / re_fs_ratio);

                Console.WriteLine(" power_muscle :" + ratios[channel][1]);
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
                CalculationDone?.Invoke(this, new MyEventArgs_indice(indices_enu.artifacts));
                // LogHelper.Log.LogInfo($"Indice_Artifact 数据推送:{indices_enu.artifacts}");
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
        double[] abs_real_img(double[] real, double[] img)
        {
            int data_length = real.Length;
            double[] data_abs = new double[data_length];
            for (int i = 0; i < data_length; i++)
            {
                //data_abs[i] = Math.Sqrt(Math.Pow(real[i], 2) + Math.Pow(img[i], 2));
                data_abs[i] = Math.Pow(real[i], 2) + Math.Pow(img[i], 2);
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

            //前两位是方法的enumeration代码，见：Calcusvr.cs,注意字节序，是高位在后
            c[0] = Convert.ToByte(98);

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
