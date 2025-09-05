using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MultiDevice
{
    /// <summary>
    /// Interaction logic for WriteableBitMap.xaml
    /// </summary>
    public partial class WriteableBitMap : UserControl
    {
        int channel_no_to_save_display = 9;
        int sampling_rate = 2000;

        WriteableBitmap _eeg;
        public int[,] dataBuf;
        public double amplitude;

        public WriteableBitMap()
        {
            Channels.Add(1, "A1");
            Channels.Add(2, "A2");
            Channels.Add(3, "NaN");
            Channels.Add(4, "NaN");
            Channels.Add(5, "NaN");
            Channels.Add(6, "NaN");
            Channels.Add(7, "NaN");
            Channels.Add(8, "NaN");
            Channels.Add(9, "Cz1");

            InitializeComponent();

            dataBuf = new int[channel_no_to_save_display, sampling_rate];
            amplitude = 10;
            _eeg = BitmapFactory.New(sampling_rate, 800);

            CompositionTarget.Rendering += CompositionTarget_Rendering;

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < sampling_rate; j++)
                {
                    dataBuf[i, j] = 0;
                }
            }

            EEG.Source = _eeg;

            showSelectedChannels("1 2 3 4 5 6 7 8 9");

        }

        public void setChannelLabels(string[] str)
        {
            if (str.Length != channel_no_to_save_display)
                throw new ArgumentException("channel len==" + channel_no_to_save_display);
            Channels.Clear();

            List<int> tmp_list = new List<int>();

            for (int i = 0; i < str.Length; i++)
            {
                if (!str[i].Contains("Na"))
                {
                    tmp_list.Add((i + 1));
                    Channels.Add(i + 1, str[i]);
                }
            }
            string tmp_str = "";
            for (int i = 0; i < tmp_list.Count; i++)
            {
                tmp_str = tmp_str + tmp_list.ElementAt(i) + " ";
            }
            showSelectedChannels(tmp_str);
        }


        public void resetBitMap()
        {
            string tmp_str = "";
            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels.Values.ElementAt(i).Contains("Na"))
                    tmp_str = tmp_str + Channels.Keys.ElementAt(i) + " ";
            }
            showSelectedChannels(tmp_str);
        }

        public void ClearBipmap()
        {
            _eeg.Clear(Colors.White);
            nowSec = 0;
            //tmp00 = 0;
        }

        int nowSec = 0;
        public int MaxSec = 1;

        //int tmp00 = 0;

        public int tailIndex = 0;
        int headIndex = 0;

        int tailIndex_tmp = 0;

        public bool showBaseLine = false;
        public bool showTimeLine = true;

        bool trigger_line_rendered = true;
        public void plot_trigger_line()
        {
            trigger_line_rendered = false;
        }
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            int[] lastSec = new int[9];
            if (tailIndex_tmp != tailIndex)
            {
                tailIndex_tmp = tailIndex;

                if (tailIndex > headIndex)
                {
                    int arrayLength = 0;

                    _eeg.FillRectangle((int)((headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, 0, (int)((tailIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, (int)_eeg.Height, Colors.White);

                    arrayLength = tailIndex - headIndex;
                    for (int j = 0; j < (channelNum.Count); j++)
                    {
                        if (j % 2 == 0)
                        {
                            //if (headIndex > 0)
                            //{
                            //    int i = 0;
                            //    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex - 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                            //}
                            for (int i = -20; i < arrayLength + 2; i++)
                            {
                                if (i + headIndex + 1 < sampling_rate && i + headIndex >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                                }
                            }

                            //_eeg.DrawPolyline(plot_tmp1, Colors.Blue);
                        }
                        else
                        {
                            //if (headIndex > 0)
                            //{
                            //    int i = 0;
                            //    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex - 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                            //}
                            for (int i = -20; i < arrayLength + 2; i++)
                            {
                                if (i + headIndex + 1 < sampling_rate && i + headIndex >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                                }
                            }
                        }
                        if (showBaseLine)
                        {
                            _eeg.DrawLine(0, Convert.ToInt32(_eeg.Height / (channelNum.Count) / 2 + j * _eeg.Height / (channelNum.Count)), Convert.ToInt32(_eeg.Width), Convert.ToInt32(_eeg.Height / (channelNum.Count) / 2 + j * _eeg.Height / (channelNum.Count)), Colors.LightGreen);
                            _eeg.DrawLine(0, Convert.ToInt32(j * _eeg.Height / (channelNum.Count)), Convert.ToInt32(_eeg.Width), Convert.ToInt32(j * _eeg.Height / (channelNum.Count)), Colors.LightGray);
                        }
                    }
                    //_eeg.DrawLine(Convert.ToInt32(((tailIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), 0, Convert.ToInt32(((headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), (int)_eeg.Height, Colors.Red);
                    if (!trigger_line_rendered)
                    {
                        _eeg.DrawLine(Convert.ToInt32((headIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), 0, Convert.ToInt32((headIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), (int)_eeg.Height, Colors.Red);

                        trigger_line_rendered = true;
                    }
                    if (tailIndex >= 2)
                        headIndex = tailIndex - 2;
                    else
                        headIndex = tailIndex;

                    _eeg.DrawLine(Convert.ToInt32((tailIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), 0, Convert.ToInt32((tailIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), (int)_eeg.Height, Colors.Red);
                }
                else
                {
                    int arrayLength = 0, arrayLength2 = 0;

                    int tailIndex2 = sampling_rate;

                    _eeg.FillRectangle((int)((headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, 0, (int)((tailIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, (int)_eeg.Height, Colors.White);

                    arrayLength = tailIndex2 - headIndex;


                    for (int j = 0; j < (channelNum.Count); j++)
                    {
                        if (j % 2 == 0)
                        {
                            //if (headIndex > 0)
                            //{
                            //    int i = 0;
                            //    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex - 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                            //}

                            for (int i = -20; i < arrayLength + 2; i++)
                            {
                                if (i + headIndex + 1 == sampling_rate)
                                {
                                    lastSec[channelNum.ElementAt(j) - 1] = this.dataBuf[channelNum.ElementAt(j) - 1, sampling_rate - 1];
                                }
                                else
                                    if (i + headIndex == 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                lastSec[channelNum.ElementAt(j) - 1] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                                }
                                else
                                  if (i + headIndex + 1 < sampling_rate && i + headIndex >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                                }
                            }

                            //_eeg.DrawPolyline(plot_tmp1, Colors.Blue);
                        }
                        else
                        {
                            //if (headIndex > 0)
                            //{
                            //    int i = 0;
                            //    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex - 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                            //    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                            //}
                            for (int i = -20; i < arrayLength + 2; i++)
                            {
                                if (i + headIndex + 1 == sampling_rate)
                                {
                                    lastSec[channelNum.ElementAt(j) - 1] = this.dataBuf[channelNum.ElementAt(j) - 1, sampling_rate - 1];
                                }
                                else
                                 if (i + headIndex == 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                lastSec[channelNum.ElementAt(j) - 1] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                                }
                                else
                               if (i + headIndex + 1 < sampling_rate && i + headIndex >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                                }
                            }
                        }
                    }


                    nowSec++;
                    nowSec = nowSec % MaxSec;

                    int tailIndex3 = tailIndex;
                    int headIndex2 = 0;

                    _eeg.FillRectangle((int)((headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, 0, (int)((tailIndex3) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / MaxSec, (int)_eeg.Height, Colors.White);

                    arrayLength2 = tailIndex3 - headIndex2;
                    for (int j = 0; j < (channelNum.Count); j++)
                    {
                        if (j % 2 == 0)
                        {
                            for (int i = -20; i < arrayLength2; i++)
                            {
                                if (i + headIndex2 + 1 == sampling_rate)
                                {
                                    lastSec[channelNum.ElementAt(j) - 1] = this.dataBuf[channelNum.ElementAt(j) - 1, sampling_rate - 1];
                                }
                                else
                                 if (i + headIndex2 == 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    lastSec[channelNum.ElementAt(j) - 1] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                      this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                                }
                                else
                               if (i + headIndex2 + 1 < sampling_rate && i + headIndex2 >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2 + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Blue);
                                }
                            }

                            //_eeg.DrawPolyline(plot_tmp1, Colors.Blue);
                        }
                        else
                        {
                            for (int i = -20; i < arrayLength2; i++)
                            {
                                if (i + headIndex2 + 1 == sampling_rate)
                                {
                                    lastSec[channelNum.ElementAt(j) - 1] = this.dataBuf[channelNum.ElementAt(j) - 1, sampling_rate - 1];
                                }
                                else
                                   if (i + headIndex2 == 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                      lastSec[channelNum.ElementAt(j) - 1] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * (nowSec)) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                      this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                                }
                                else
                                  if (i + headIndex2 + 1 < sampling_rate && i + headIndex2 >= 0)
                                {
                                    _eeg.DrawLine(Convert.ToInt32(((i + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Convert.ToInt32(((i + 1 + headIndex2) * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), Convert.ToInt32(_eeg.Height / (channelNum.Count) * (j + 0.5) -
                                    this.dataBuf[channelNum.ElementAt(j) - 1, (i + headIndex2 + 1)] * amplitude * _eeg.Height / (channelNum.Count) / 2 / 8388608), Colors.Red);
                                }
                            }
                        }
                        if (showBaseLine)
                        {
                            _eeg.DrawLine(0, Convert.ToInt32(_eeg.Height / (channelNum.Count) / 2 + j * _eeg.Height / (channelNum.Count)), Convert.ToInt32(_eeg.Width), Convert.ToInt32(_eeg.Height / (channelNum.Count) / 2 + j * _eeg.Height / (channelNum.Count)), Colors.LightGreen);
                            _eeg.DrawLine(0, Convert.ToInt32(j * _eeg.Height / (channelNum.Count)), Convert.ToInt32(_eeg.Width), Convert.ToInt32(j * _eeg.Height / (channelNum.Count)), Colors.LightGray);
                        }
                    }
                    if (!trigger_line_rendered)
                    {
                        _eeg.DrawLine(Convert.ToInt32((headIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), 0, Convert.ToInt32((headIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), (int)_eeg.Height, Colors.Red);

                        trigger_line_rendered = true;
                    }
                    if (tailIndex >= 2)
                        headIndex = tailIndex - 2;
                    else
                        headIndex = tailIndex;

                    _eeg.DrawLine(Convert.ToInt32((tailIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), 0, Convert.ToInt32((tailIndex * _eeg.Width / sampling_rate + _eeg.Width * nowSec) / (MaxSec)), (int)_eeg.Height, Colors.Red);
                }

            }
            if (MaxSec > 1 && showTimeLine)
                for (int jjj = 0; jjj <= MaxSec; jjj++)
                {
                    _eeg.DrawLine(Convert.ToInt32(_eeg.Width * jjj / MaxSec), 0, Convert.ToInt32(_eeg.Width * jjj / MaxSec), Convert.ToInt32(_eeg.Height), Colors.LightSteelBlue);
                }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _eeg = BitmapFactory.New(Convert.ToInt32(EEG.ActualWidth), Convert.ToInt32(EEG.ActualHeight));
            EEG.Source = _eeg;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _eeg = BitmapFactory.New(Convert.ToInt32(EEG.ActualWidth), Convert.ToInt32(EEG.ActualHeight));
            EEG.Source = _eeg;
        }

        Dictionary<int, string> Channels = new Dictionary<int, string>();
        public List<int> channelNum = new List<int>();
        List<int> channelNum_tmp = new List<int>();
        public void showSelectedChannels(string str)
        {
            channelNum_tmp.Clear();
            string[] chs = str.Split(' ');
            int tmp;
            for (int i = 0; i < chs.Length; i++)
            {
                if (int.TryParse(chs[i], out tmp) && tmp > 0 && tmp < 10 && !channelNum_tmp.Contains(tmp))
                {
                    channelNum_tmp.Add(tmp);
                }
            }

            if (channelNum_tmp.Count > 0)
            {
                channelNum_tmp.Sort();
                channelNum.Clear();
                foreach (var item in channelNum_tmp)
                {
                    channelNum.Add(item);
                }
                Label label;
                ChannelNames.Children.Clear();
                ChannelNames.RowDefinitions.Clear();
                for (int i = 0; i < channelNum.Count; i++)
                {
                    label = new Label();
                    label.MinHeight = EEG.ActualHeight / channelNum.Count;
                    label.HorizontalContentAlignment = HorizontalAlignment.Left;
                    label.VerticalContentAlignment = VerticalAlignment.Center;
                    label.Content = Channels[channelNum.ElementAt(i)];
                    RowDefinition rd = new RowDefinition();
                    ChannelNames.RowDefinitions.Add(rd);
                    label.SetValue(Grid.RowProperty, i);
                    ChannelNames.Children.Add(label);
                }
            }
            //ChannelNames.Background = new SolidColorBrush(Colors.Pink);
            //ChannelNames.ShowGridLines = true;
            ClearBipmap();
        }


        public void showSelectedChannels_eye(string str)
        {
            List<int> effect_Chan = new List<int>();
            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels.Values.ElementAt(i).Contains("Na"))
                {
                    effect_Chan.Add(Channels.Keys.ElementAt(i));
                }
            }

            channelNum_tmp.Clear();
            string[] chs = str.Split(' ');
            int tmp;
            for (int i = 0; i < chs.Length; i++)
            {
                if (int.TryParse(chs[i], out tmp) && tmp > 0 && tmp < effect_Chan.Count + 1 && !channelNum_tmp.Contains(tmp))
                {
                    channelNum_tmp.Add(tmp);
                }
            }

            if (channelNum_tmp.Count > 0)
            {
                channelNum_tmp.Sort();
                channelNum.Clear();
                foreach (var item in channelNum_tmp)
                {
                    channelNum.Add(effect_Chan[item - 1]);
                }
                Label label;
                ChannelNames.Children.Clear();
                ChannelNames.RowDefinitions.Clear();
                for (int i = 0; i < channelNum.Count; i++)
                {
                    label = new Label();
                    label.MinHeight = EEG.ActualHeight / channelNum.Count;
                    label.HorizontalContentAlignment = HorizontalAlignment.Left;
                    label.VerticalContentAlignment = VerticalAlignment.Center;
                    label.Content = Channels[channelNum.ElementAt(i)];
                    RowDefinition rd = new RowDefinition();
                    ChannelNames.RowDefinitions.Add(rd);
                    label.SetValue(Grid.RowProperty, i);
                    ChannelNames.Children.Add(label);
                }
            }
            //ChannelNames.Background = new SolidColorBrush(Colors.Pink);
            //ChannelNames.ShowGridLines = true;
            ClearBipmap();
        }


        public void showSelectedChannels_moveUp()
        {
            List<int> effect_Chan = new List<int>();
            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels.Values.ElementAt(i).Contains("Na"))
                {
                    effect_Chan.Add(Channels.Keys.ElementAt(i));
                }
            }

            List<int> ttt = new List<int>();
            for (int i = 0; i < channelNum.Count; i++)
            {
                ttt.Add(effect_Chan.IndexOf(channelNum[i]));
            }


            channelNum_tmp.Clear();
            int tmpInt = 0;
            foreach (var item in ttt)
            {
                tmpInt = item - 1 >= 0 ? item - 1 : 0;
                if (!channelNum_tmp.Contains(tmpInt))
                    channelNum_tmp.Add(tmpInt);
            }
            if (channelNum_tmp.Count > 0)
            {
                channelNum_tmp.Sort();
                channelNum.Clear();
                foreach (var item in channelNum_tmp)
                {
                    channelNum.Add(effect_Chan[item]);
                }
                Label label;
                ChannelNames.Children.Clear();
                ChannelNames.RowDefinitions.Clear();
                for (int i = 0; i < channelNum.Count; i++)
                {
                    label = new Label();
                    label.MinHeight = EEG.ActualHeight / channelNum.Count;
                    label.HorizontalContentAlignment = HorizontalAlignment.Left;
                    label.VerticalContentAlignment = VerticalAlignment.Center;
                    label.Content = Channels[channelNum.ElementAt(i)];
                    RowDefinition rd = new RowDefinition();
                    ChannelNames.RowDefinitions.Add(rd);
                    label.SetValue(Grid.RowProperty, i);
                    ChannelNames.Children.Add(label);
                }
            }
            //ChannelNames.Background = new SolidColorBrush(Colors.Pink);
            //ChannelNames.ShowGridLines = true;
            ClearBipmap();
        }


        public void showSelectedChannels_moveDown()
        {
            List<int> effect_Chan = new List<int>();
            for (int i = 0; i < Channels.Count; i++)
            {
                if (!Channels.Values.ElementAt(i).Contains("Na"))
                {
                    effect_Chan.Add(Channels.Keys.ElementAt(i));
                }
            }

            List<int> ttt = new List<int>();
            for (int i = 0; i < channelNum.Count; i++)
            {
                ttt.Add(effect_Chan.IndexOf(channelNum[i]));
            }



            channelNum_tmp.Clear();
            int tmpInt = 0;
            foreach (var item in ttt)
            {
                tmpInt = item + 1 < effect_Chan.Count ? item + 1 : effect_Chan.Count - 1;
                if (!channelNum_tmp.Contains(tmpInt))
                    channelNum_tmp.Add(tmpInt);
            }
            if (channelNum_tmp.Count > 0)
            {
                channelNum_tmp.Sort();
                channelNum.Clear();
                foreach (var item in channelNum_tmp)
                {
                    channelNum.Add(effect_Chan[item]);
                }
                Label label;
                ChannelNames.Children.Clear();
                ChannelNames.RowDefinitions.Clear();
                for (int i = 0; i < channelNum.Count; i++)
                {
                    label = new Label();
                    label.MinHeight = EEG.ActualHeight / channelNum.Count;
                    label.HorizontalContentAlignment = HorizontalAlignment.Left;
                    label.VerticalContentAlignment = VerticalAlignment.Center;
                    label.Content = Channels[channelNum.ElementAt(i)];
                    RowDefinition rd = new RowDefinition();
                    ChannelNames.RowDefinitions.Add(rd);
                    label.SetValue(Grid.RowProperty, i);
                    ChannelNames.Children.Add(label);
                }
            }
            //ChannelNames.Background = new SolidColorBrush(Colors.Pink);
            //ChannelNames.ShowGridLines = true;
            ClearBipmap();
        }
    }
}
