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
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Markup;
using System.Media;
using InteractiveDataDisplay.WPF;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using AForge.Video.DirectShow;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using MathNet.Numerics.LinearAlgebra.Factorization;
using LayUI.Wpf.Global;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using MultiDevice.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MultiDevice.DB;
using Notifications.Wpf.ViewModels.Base;

namespace MultiDevice
{
    /// <summary>
    /// Interaction logic for ShowWave.xaml
    /// </summary>
    public partial class MultiDeviceMainWindow : Window
    {
        public ComSvr comDevice = null;
        public WriteableBitMap chartMap = null;
        const int MAXSEC_PER_MAP = 20;
        public TimeSpan ts = new TimeSpan();
        public Slider amplitudeSlider = null;
        public Label sliderBox = null;
        public double[] slider_ticks  = new double[] { 0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 100, 200, 500, 1000, 2000, 5000, 10000 };
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer warning_timer = new DispatcherTimer();
        // 游戏服务
        private ComCalcusvr     gameServer = null;
        private GameConfigModel gameConfigModel = null;
        private string titleUserInfo = "";

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            chartMap.amplitude = ComSvr.amplitude_range / amplitudeSlider.Value / 10;
            chartMap.tailIndex = comDevice.indexOfArray;
            SEC_COUNT.Content = (300.0 / chartMap.MaxSec).ToString("F1");
        }


        public MultiDeviceMainWindow(ComSvr device)
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            comDevice = device;
            comDevice.forwardFail += Svr_forwardFail;
            comDevice.deviceError += ComDevice_deviceError;
            comDevice.receive_event += Svr_receive_event;

            #region 电池电量检测
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += new EventHandler(plotStateBar);
            timer.IsEnabled = true;
            warning_timer.Interval = TimeSpan.FromSeconds(120);
            warning_timer.Tick += new EventHandler(warningBattery);
            warning_timer.IsEnabled = true;
            #endregion

            InitializeComponent();

            InitUI();
            // 设备连接已完成
            App.IsConnectDevice = true;
        }

        private void ComDevice_deviceError(string error)
        {
            // 重新启动连接
            LogHelper.Log.LogDebug("设备通信异常准备重新连接设备");
            comDevice.stop();
            comDevice.createDevice();
            comDevice.start();
            LogHelper.Log.LogDebug("设备重新连接完毕");
        }

        #region 转发失败处理
        // 转发失败处理

        private void Svr_forwardFail(object sender, FailArgs e)
        {
            comDevice.stopForward();
        }
        #endregion

        #region 界面初始化
        private void InitUI()
        {
            if (null == amplitudeSlider)
            {
                amplitudeSlider = new Slider();
                if (null == sliderBox)
                {
                    sliderBox = new Label();
                    sliderBox.ToolTip = "幅值标尺（uV/mm）";
                }
                amplitudeSlider.ValueChanged += AmplitudeSlider_ValueChanged;
            }

            amplitudeSlider.Maximum = 5000;
            amplitudeSlider.Minimum = 0.1;
            // amplitudeSlider.Ticks = new DoubleCollection(slider_ticks);
            amplitudeSlider.Value = 50;
            amplitudeSlider.ToolTip = "幅值标尺（uV/mm）";
            amplitudeSlider.IsSnapToTickEnabled = true;
            SliderAndText.Children.Add(amplitudeSlider);
            SliderAndText.HorizontalAlignment = HorizontalAlignment.Center;
            SliderAndText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(amplitudeSlider, 0);
            Grid.SetRow(amplitudeSlider, 0);

            chartMap = new WriteableBitMap();
            chartMap.MaxSec = 2;
            chartMap.dataBuf = comDevice.dataBuf_NF;
            chartMap.setChannelLabels(comDevice.signal_labels);
            chartMap.amplitude = ComSvr.amplitude_range / amplitudeSlider.Value / 10;
            ImagGrid.Children.Add(chartMap);

            if (null == sliderBox)
            {
                sliderBox = new Label(); sliderBox.ToolTip = "幅值标尺（uV/mm）";
            }

            SliderAndText.Children.Add(sliderBox);
            SliderAndText.HorizontalAlignment = HorizontalAlignment.Center;
            SliderAndText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(sliderBox, 1);
            Grid.SetRow(sliderBox, 0);
            CompositionTarget.Rendering += CompositionTarget_Rendering;

            if (!comDevice.filterON)
            {
                Image ib_filterON = new Image();
                string packUri = "pack://application:,,,/icons/filter_off.png";
                ib_filterON.Source = new BitmapImage(new Uri(packUri));
                mFilterCheck.Content = ib_filterON;
                mFilterCheck.ToolTip = "滤波器关闭";
            }
            else
            {
                Image ib_filterOff = new Image();
                string packUri = "pack://application:,,,/icons/filter_on.png";
                ib_filterOff.Source = new BitmapImage(new Uri(packUri));
                mFilterCheck.Content = ib_filterOff;
                mFilterCheck.ToolTip = "滤波器开启";
            }


            if (App.loginUser.UserRole == UserRole.医生)
            {
                titleUserInfo = $"登录用户:{App.loginUser.UserName}({App.loginUser.UserRole.ToString()})";
            }
            else
            {
                titleUserInfo = $"登录用户:{App.loginUser.UserRole.ToString()}";
            }
            Title = $"脑电数据实时采集中【{titleUserInfo}】";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sliderBox.Content = amplitudeSlider.Value.ToString("F0");
        }
        #endregion

        #region 电池电量检测
        private void plotStateBar(object sender, EventArgs e)
        {
            double vol = 0;

            if (comDevice.volOfDev < 3100)
                vol = 0;
            else
                if (comDevice.volOfDev > 4100)
                vol = 100;
            else
            {
                if (comDevice.volOfDev >= 3100 && this.comDevice.volOfDev < 3400)
                {
                    vol = ((comDevice.volOfDev - 2900) / 300.0 * 10.0);
                }
                else
                {
                    if (this.comDevice.volOfDev >= 3400 && this.comDevice.volOfDev <= 4100)
                    {
                        vol = ((this.comDevice.volOfDev - 3400) / 700.0 * 90.0 + 10);
                    }
                }
            }
            this.Dispatcher.BeginInvoke((Action)delegate ()
            {
                Random rnd = new Random();
                battery.Content = vol.ToString("F1") + "%";
                battery.ToolTip = "大约可使用" + (vol * 4 + rnd.NextDouble() * 10).ToString("F1") + "分钟";
                if (this.comDevice.charging)
                {
                    Image ib = new Image();
                    string packUri = "pack://application:,,,/icons/charging.png";
                    ib.Source = new BitmapImage(new Uri(packUri));
                    isCharging.Content = ib;
                    isCharging.ToolTip = "正在充电";
                }
                else
                {
                    isCharging.Content = "";
                    isCharging.ToolTip = "充电状态";
                }
            }
           );
        }
        private void warningBattery(object sender, EventArgs e)
        {
            string vol = "";
            double batteryNumber = 0;
            if (this.comDevice.volOfDev < 3100)
                vol = "0%";
            else
                if (this.comDevice.volOfDev > 4100)
            {
                vol = "100%";
                batteryNumber = 100;
            }
            else
            {
                if (this.comDevice.volOfDev >= 3100 && this.comDevice.volOfDev < 3400)
                {
                    vol = ((this.comDevice.volOfDev - 2900) / 300.0 * 10.0).ToString("F1") + "%";
                    batteryNumber = ((this.comDevice.volOfDev - 2900) / 300.0 * 10.0);
                }
                else
                {
                    if (this.comDevice.volOfDev >= 3400 && this.comDevice.volOfDev <= 4100)
                    {
                        vol = ((this.comDevice.volOfDev - 3400) / 700.0 * 90.0 + 10).ToString("F1") + "%";
                        batteryNumber = ((this.comDevice.volOfDev - 3400) / 700.0 * 90.0 + 10);
                    }

                }
            }
            this.Dispatcher.BeginInvoke((Action)delegate ()
            {
                if (batteryNumber < 5)
                {
                    WaringLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    WaringLabel.Visibility = Visibility.Hidden;
                }
            });
        }
        #endregion

        #region 采样曲线恢复实时显示
        /// <summary>
        /// 采样曲线恢复实时显示
        /// </summary>
        public void RecoverShowWave()
        {
            this.ResizeMode = System.Windows.ResizeMode.CanResize;
            ImagGrid.Children.Clear();

            chartMap.MaxSec = 2;
            chartMap.setChannelLabels(comDevice.signal_labels);
            ImagGrid.Children.Add(chartMap);
            ImagGrid.Children.Add(SliderAndText);
            SliderAndText.Children.Clear();

            if (null == amplitudeSlider)
            {
                amplitudeSlider = new Slider();
                if (null == sliderBox)
                {
                    sliderBox = new Label();
                    amplitudeSlider.ValueChanged += AmplitudeSlider_ValueChanged;
                }
            }
            amplitudeSlider.Maximum = 10000;
            amplitudeSlider.Minimum = 0.1;
            amplitudeSlider.Ticks = new DoubleCollection(slider_ticks);
            amplitudeSlider.Value = 50;
            amplitudeSlider.ToolTip = "幅值标尺（uV/mm）";
            amplitudeSlider.IsSnapToTickEnabled = true;
            SliderAndText.Children.Add(amplitudeSlider);
            SliderAndText.HorizontalAlignment = HorizontalAlignment.Center;
            SliderAndText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(amplitudeSlider, 0);
            Grid.SetRow(amplitudeSlider, 0);
            chartMap.amplitude = ComSvr.amplitude_range / amplitudeSlider.Value / 10;

            SliderAndText.Children.Add(sliderBox);
            SliderAndText.HorizontalAlignment = HorizontalAlignment.Center;
            SliderAndText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(sliderBox, 1);
            Grid.SetRow(sliderBox, 0);

            Button bsline_btn = new Button();
            bsline_btn.Click += calcuBaseline_Click;
            Image ib = new Image();
            string packUri = "pack://application:,,,/icons/baseline.png";
            ib.Source = new BitmapImage(new Uri(packUri));
            bsline_btn.Content = ib;
            bsline_btn.ToolTip = "基准线关闭";
            SliderAndText.Children.Add(bsline_btn);
            SliderAndText.HorizontalAlignment = HorizontalAlignment.Center;
            SliderAndText.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(bsline_btn, 2);
            Grid.SetRow(bsline_btn, 0);
        }
        #endregion

        #region 波形放大
        private void plusSec_Click(object sender, RoutedEventArgs e)
        {
            chartMap.ClearBipmap();
            chartMap.MaxSec = chartMap.MaxSec % MAXSEC_PER_MAP + 2;
        }
        #endregion

        #region 波形缩小
        private void minusSec_Click(object sender, RoutedEventArgs e)
        {
            chartMap.ClearBipmap();
            chartMap.MaxSec = chartMap.MaxSec - 2;
            if (chartMap.MaxSec <= 0)
                chartMap.MaxSec = MAXSEC_PER_MAP;
        }
        #endregion

        #region 选择通道
        private void selectChn_Click(object sender, RoutedEventArgs e)
        {
            chartMap.showSelectedChannels_eye(chs_input.Text);
        }
        #endregion

        #region 关闭滤波器
        private void ResetMy_Click(object sender, RoutedEventArgs e)
        {
            chartMap.showBaseLine = false;
            chartMap.setChannelLabels(comDevice.signal_labels);
            chartMap.resetBitMap();
            chartMap.MaxSec = 1;

            comDevice.resetBasline();
            if (comDevice.filterON)
            {
                comDevice.filterON = false;
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/filter_off.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                mFilterCheck.Content = ib;
                mFilterCheck.ToolTip = "滤波器关闭";

                flt_input.Text = "";
                comDevice.resetFilter();
            }
        }
        #endregion

        #region 基准线显示控制
        private void showBaseLine_Click(object sender, RoutedEventArgs e)
        {
            chartMap.showBaseLine = !chartMap.showBaseLine;
            if (chartMap.showBaseLine)
            {
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/baseline_on.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                showBaseLine.Content = ib;
                showBaseLine.ToolTip = "基准线开启";
            }
            else
            {
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/baseline_off.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                showBaseLine.Content = ib;
                showBaseLine.ToolTip = "基准线关闭";
            }
        }
        #endregion

        #region 实验文件保存
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SvrBDF_control_Click(object sender, RoutedEventArgs e)
        {
            if (!comDevice.isSaving)
            {
                if (!comDevice.impedanceDone)
                {
                    MessageBox.Show("请先进行阻抗检测 !", "提示", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = $"实验文件_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                dlg.DefaultExt = ".BDF";
                dlg.Filter = "BDF files|*.BDF"; // Filter files by extension
                Nullable<bool> result = dlg.ShowDialog();

                if (result == true)
                {
                    detectMenuItem.IsEnabled = false;
                    settingsMenuItem.IsEnabled = false;
                    start_Calusvr.IsEnabled = false;
                    settingsMenuItem.Opacity = 0.5;
                    start_Calusvr.Opacity = 0.5;
                    detectMenuItem.Opacity = 0.5;
                    comDevice.BDFFilePath = dlg.FileName;
                    comDevice.startSaveBDF();

                    string packUri = "pack://application:,,,/icons/saving.png";
                    BDFSaveMenuItem.ToolTip = "正在接收数据";
                    saveDataIcon.Source = new BitmapImage(new Uri(packUri));
                }
            }
            else
            {
                detectMenuItem.IsEnabled = true;
                settingsMenuItem.IsEnabled = true;
                start_Calusvr.IsEnabled = true;
                settingsMenuItem.Opacity = 1;
                start_Calusvr.Opacity = 1;
                detectMenuItem.Opacity = 1;
                comDevice.StopSaveBDF();

                string packUri = "pack://application:,,,/icons/save.png";
                saveDataIcon.Source = new BitmapImage(new Uri(packUri));
                BDFSaveMenuItem.ToolTip = "保存文件";
                TimeCount.Content = " ";
            }
        }
        #endregion

        #region 滤波器开关控制
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterCheck_Click(object sender, RoutedEventArgs e)
        {
            if (comDevice.filterON)
            {
                comDevice.filterON = false;
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/filter_off.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                mFilterCheck.Content = ib;
                mFilterCheck.ToolTip = "滤波器关闭";

                flt_input.Text = "";
                comDevice.resetFilter();
            }
            else
            {
                List<double> flt_para = new List<double>();
                var one_chan = Regex.Split(flt_input.Text, "\\s+", RegexOptions.IgnoreCase);
                double tmp_double = 0;
                bool flt_para_ava = false;
                for (int iii = 0; iii < one_chan.Length; iii++)
                {

                    if (double.TryParse(one_chan[iii], out tmp_double))
                        flt_para.Add(tmp_double);
                }
                if (flt_para.Count == 0 || flt_para.Sum() == 0)
                {
                    flt_para_ava = this.comDevice.setFilter(48, 52, false);
                }
                else
                if (flt_para.Count == 1)
                {
                    flt_para_ava = this.comDevice.setFilter(0, flt_para[0], true);
                }
                else
                {
                    flt_para_ava = this.comDevice.setFilter(flt_para[0], flt_para[1], true);
                }
                if (!flt_para_ava)
                {
                    return;
                }

                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/filter_on.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                mFilterCheck.Content = ib;
                mFilterCheck.ToolTip = "滤波器开启";
                comDevice.filterON = true;
            }
        }
        #endregion

        #region 曲线移动控制
        private void moveup_Click(object sender, RoutedEventArgs e)
        {
            string names = "";
            chartMap.showSelectedChannels_moveUp();
            for (int i = 0; i < chartMap.channelNum.Count; i++)
            {
                names += chartMap.channelNum[i] + " ";
            }

            chs_input.Text = names;
        }

        private void movedown_Click(object sender, RoutedEventArgs e)
        {
            chartMap.showSelectedChannels_moveDown();

            string names = "";
            for (int i = 0; i < chartMap.channelNum.Count; i++)
            {
                names += chartMap.channelNum[i] + " ";
            }

            chs_input.Text = names;
        }
        #endregion

        #region 时间刻度开启
        private void showTimeLine_Click(object sender, RoutedEventArgs e)
        {
            chartMap.showTimeLine = !chartMap.showTimeLine;
            if (chartMap.showTimeLine)
            {
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/timeline_on.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                showTimeLine.Content = ib;
                showTimeLine.ToolTip = "时间刻度开启";
            }
            else
            {
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/timeline_off.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                showTimeLine.Content = ib;
                showTimeLine.ToolTip = "时间刻度关闭";
            }
        }

        #endregion

        #region 事件菜单响应
        private void Svr_receive_event(object sender, myEventArgs e)
        {
            if (chartMap != null)
            {
                try {
                    chartMap.plot_trigger_line();
                }
                catch (Exception ex)
                {

                }
            }
        }


        private void MenuItemA_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("event_A");
        }
        private void MenuItemB_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("event_B");
        }
        private void MenuItemC_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("event_C");
        }
        private void MenuItemD_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("event_D");
        }

        private void MenuItemE_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("sound_A");
            string packUri = "./icons/Windows Ringout.wav";
            SoundPlayer simpleSound = new SoundPlayer(packUri);
            simpleSound.Play();

        }
        private void MenuItemF_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.InsertEvent("sound_B");
            string packUri = "./icons/Windows Ding.wav";
            SoundPlayer simpleSound = new SoundPlayer(packUri);
            simpleSound.Play();
        }
        #endregion

        #region 运算服务
        void startCalusvr_Click(object sender, RoutedEventArgs e)
        {
            if (!comDevice.impedanceDone)
            {
                Message.ShowError("启动运算服务", "请先进行阻抗检测!", TimeSpan.FromSeconds(3));
                return;
            }

            // 启动游戏数据交互服务
            startGameSever();
        }
        #endregion

        #region 参数设置
        private void settingMenu_Click(object sender, RoutedEventArgs e)
        {
            ParadigmSettingsWindow paradigmSettingsWindow =  new ParadigmSettingsWindow();
            paradigmSettingsWindow.ShowDialog();

            // 刷新通道配置
            ParadigmSettingsModel paradigmSettingsModel = paradigmSettingsWindow.getCurrentSelectParadigmSettings();
            if (null != paradigmSettingsModel)
            {
                comDevice.setSignalLabel(paradigmSettingsModel.signalsLabels, paradigmSettingsModel.refMode);
                ResetMy_Click(null, null);
            }
        }
        #endregion

        #region 用户信息录入
        private void UserRegister_Click(object sender, RoutedEventArgs e)
        {
            App.userRegisterWindow = new UserRegisterWindow();
            App.userRegisterWindow.ShowDialog();
        }
        #endregion

        #region 视频录制
        private void videoRecordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool has_video_window = false;
            for (int i = 0; i < OwnedWindows.Count; i++)
            {
                if (OwnedWindows[i].Title == "VideoRecord")
                {
                    has_video_window = true; break;
                }
            }
            if (!has_video_window)
            {
                if (!comDevice.isSaving)
                {
                    MessageBox.Show("请先存储脑电数据", "警告");
                    return;
                }
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Capacity == 0)
                {
                    MessageBox.Show("没有视频输入设备", "警告");
                    return;
                }

                VideoCapture video_capture = new VideoCapture();

                video_capture.Title = "VideoRecord";
                video_capture.FileName = $"Video_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                video_capture.FileDir = "./";

                video_capture.sendMessage += Video_capture_sendMessage;
                video_capture.errorMessage += Video_capture_errorMessage;

                video_capture.Show();
                video_capture.Owner = this;

                VideoRecordMenuItem.ToolTip = "正在接收视频";
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/video_on.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                VideoRecordMenuItem.Header = ib;
            }
            else
            {
                for (int i = 0; i < this.OwnedWindows.Count; i++)
                {
                    if (OwnedWindows[i].Title == "VideoRecord")
                    {
                        OwnedWindows[i].Show();
                    }
                }
            }
        }

        private void Video_capture_errorMessage(object sender, myVideoEventArgs e)
        {
            comDevice.InsertEvent(e.Message);
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                VideoRecordMenuItem.ToolTip = "开始视频记录";
                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/video_off.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                VideoRecordMenuItem.Header = ib;
            });
        }

        private void Video_capture_sendMessage(object sender, myVideoEventArgs e)
        {
            comDevice.InsertEvent(e.Message);
        }

        private void calcuBaseline_Click(object sender, RoutedEventArgs e)
        {
            this.comDevice.calcuBaseline();
        }
        #endregion

        #region 关于系统
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();

            //string strHash = "Author:ChenHe;Supervisor:LiXiaoli;Date:20231228;Jiangxi Jielian Co. Ltd.";
            //var hashBytes = ASCIIEncoding.ASCII.GetBytes(strHash);
            //var hashBytesMd5 = new MD5CryptoServiceProvider().ComputeHash(hashBytes);
            //StringBuilder sOutput = new StringBuilder(hashBytesMd5.Length);
            //for (int ix = 0; ix < hashBytesMd5.Length - 1; ix++)
            //{
            //    sOutput.Append(hashBytesMd5[ix].ToString("X2"));
            //}

            //var hashCodeString  = sOutput.ToString();
            //string aboutMessage = "";
            //aboutMessage += "1.Hash值：" + hashCodeString;
            //aboutMessage += "\n2.软件版本号：V1.0.0";
            //aboutMessage += "\n3.注册人名称/生产企业名称：江西杰联医疗设备有限公司";
            //aboutMessage += "\n4.注册人住所/生产企业住所：江西省赣江新区直管区中医药科创城公共服务及公共研发中心10号楼12层 ";
            //aboutMessage += "\n5.注册人联系方式/生产企业联系方式：0791-83062456";
            //MessageBox.Show(aboutMessage, "关于软件", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        #endregion

        #region 窗口关闭
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (gameServer != null && !gameServer.CalSvrStopped)
            {
                gameServer.stopServer();
                gameServer = null;
            }
            GC.Collect();
            App.IsConnectDevice = false;
            App.CurrentUser     = null;

            // 关闭设备
            comDevice.stop();
            // 关闭游戏
            GameAppRunHelper.App.CloseGameApp();
            App.deviceMainWindow = null;
            LogHelper.Log.LogInfo("上位机取值接收,设备关闭、游戏关闭、连接服务关闭");
        }
        #endregion

        #region 阻抗检测
        private void detectBtn_Click(object sender, RoutedEventArgs e)
        {
            App.userSelectWindow = new UserSelectWindow();
            if (App.userSelectWindow.ShowDialog() != true)
            {
                return;
            }

            gameConfigModel = App.userSelectWindow.gameConfigModel;
            startImpedanceDetect();
        }

        public void startImpedanceDetect()
        {
            App.CurrentUser = gameConfigModel.UserInfo;
            // 用户登录
            NetHelper.Net.sendMessageToSever(true, Cmd.UserLogin, "用户连接登录", JObject.FromObject(App.CurrentUser).ToString(Formatting.None));
            NetHelper.Net.sendMessageToSever(true, Cmd.Message, $"{gameConfigModel.UserInfo.Name},阻抗匹配中...");

            // 更新设备通道
            comDevice.setSignalLabel(gameConfigModel.paradigmSettings.signalsLabels, gameConfigModel.paradigmSettings.refMode);
            ResetMy_Click(null,null);

            SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "阻抗匹配");
            App.impedanceDetectWindow = new ComImpedanceDetect(gameConfigModel);
            App.impedanceDetectWindow.startDetect = false;
            App.impedanceDetectWindow.Owner = this;
            App.impedanceDetectWindow.OnFinishedDeleaget += DetectWindow_OnFinishedDeleaget;
            App.impedanceDetectWindow.svr = this.comDevice;
            App.impedanceDetectWindow.doingJOB();
            App.impedanceDetectWindow.Show();
        }

        public void setGameConfig(GameConfigModel gameConfigModel)
        {
            this.gameConfigModel = gameConfigModel;
        }
        /// <summary>
        ///  
        /// </summary>
        private void DetectWindow_OnFinishedDeleaget()
        {
            // 启动游戏服务
            startGameSever();
        }
        #endregion

        #region 上位机端与游戏数据交换相关
        public void startGameSever()
        {
            if(null == App.CurrentUser)
            {
                Message.ShowWarning("游戏服务启动", "请先选择用户!", TimeSpan.FromSeconds(3));
                return;
            }

            App.CurrentUser.TotalPowerTimes = 0;
            App.CurrentUser.ValidTimes = 0;

            SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "开始游戏");
            string logFilePath = "";
            if (null == gameServer)
            {
                string dataPath    = AppDomain.CurrentDomain.BaseDirectory + "Data";
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }

                UserProfile theUserProfile = new UserProfile();
                theUserProfile.individual = false;
                theUserProfile.IAF_high = 13;
                theUserProfile.IAF_low  = 8;
                theUserProfile.IAPF     = 10;

                string formattedDate = DateTime.Now.ToString("yyyy_MM_d_");
                if(gameConfigModel.UserInfo != null)
                {
                    formattedDate = $"{formattedDate}{gameConfigModel.UserInfo.Name}_";
                }

                int folderCount = 1;
                while (true)
                {
                    string BDFFilePath = @"" + dataPath + "\\" + formattedDate + folderCount;
                    if (Directory.Exists(BDFFilePath))
                    {
                        folderCount++;
                    }
                    else
                    {
                        try
                        {
                            Directory.CreateDirectory(BDFFilePath);
                        }
                        catch (IOException ee)
                        {
                            MessageBox.Show("文件夹选择错误:" + ee.Message, "警告");
                            return;
                        }

                        logFilePath = BDFFilePath;
                        detectMenuItem.IsEnabled   = false;
                        settingsMenuItem.IsEnabled = false;
                        BDFSaveMenuItem.IsEnabled = false;
                        detectMenuItem.Opacity   = 0.5;
                        settingsMenuItem.Opacity = 0.5;
                        BDFSaveMenuItem.Opacity  = 0.5;
                        comDevice.BDFFilePath    = BDFFilePath + "\\" + DateTime.Now.ToString("yyyyMMddHHmm");
                        if(null != gameConfigModel.UserInfo)
                        {
                            comDevice.BDFFilePath = BDFFilePath + "\\" + DateTime.Now.ToString("yyyyMMddHHmm") + "_" + gameConfigModel.UserInfo.Name;
                        }
                        comDevice.startSaveBDF();
                        break;
                    }
                }

                gameServer = new ComCalcusvr(comDevice);
                gameServer.TheUserProfile = theUserProfile;
                gameServer.logDirPath = logFilePath;
                start_Calusvr.ToolTip = "运算服务已开启";

                gameServer.paradigm_channloc_error += CalcusvrParadigmChannelError;
                gameServer.paradigm_ref_error      += CalcusvrParadigmRefError;

                gameServer.SvrListenError += GameSvrListenError;
                gameServer.client_Commu_Fail += CalcusvrClientCommuFail;
                gameServer.client_RcvFail += CalcusvrClientRcvFail;

             
                int serverPort  = 9264;
                gameServer.startServer(serverPort, gameConfigModel);
                Title = $"{gameConfigModel.UserInfo.TestNumber}-{gameConfigModel.UserInfo.Name} 正在游戏中...【{titleUserInfo}】-【终端名称:{App.ClientName}({App.ClientId})】";
                LogHelper.Log.LogDebug($"游戏通信服务启动:{serverPort}!");

                string packUri = "pack://application:,,,/icons/Calculatoring.png";
                GameSeverIcon.Source = new BitmapImage(new Uri(packUri));
                // 记录开始时间
                gameConfigModel.UserInfo.GameStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // 启动游戏
                GameAppRunHelper.App.AutoRunGameApp();
                SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "游戏中");
                NetHelper.Net.sendMessageToSever(true, Cmd.Message,   $"{gameConfigModel.UserInfo.Name},游戏中...");
                NetHelper.Net.sendMessageToSever(true, Cmd.GameStart, $"{gameConfigModel.UserInfo.Name},游戏开始成功!", 
                    JObject.FromObject(gameConfigModel.UserInfo).ToString(Formatting.None));
            }
            else
            {
                StopGameSever(true);
            }
        }

        public void StopGameSever(bool gameOver = false)
        {
            if(null == gameServer)
            {
                return;
            }

            comDevice.StopSaveBDF();
            TimeCount.Content = " ";
            detectMenuItem.IsEnabled = true;
            settingsMenuItem.IsEnabled = true;
            BDFSaveMenuItem.IsEnabled = true;
            settingsMenuItem.Opacity = 1;
            detectMenuItem.Opacity = 1;
            BDFSaveMenuItem.Opacity = 1;

            gameServer.stopServer();
            start_Calusvr.ToolTip = "开启运算服务";
            gameServer = null;
            LogHelper.Log.LogDebug($"游戏通信服务关闭!");

            string packUri = "pack://application:,,,/icons/Calculator.png";
            GameSeverIcon.Source = new BitmapImage(new Uri(packUri));
            Title = $"脑电数据实时采集中【{titleUserInfo}】-【终端名称:{App.ClientName}({App.ClientId})】";
            // 关闭游戏
            GameAppRunHelper.App.CloseGameApp();
            // 记录开始时间
            gameConfigModel.UserInfo.GameEndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (gameOver)
            {
                SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "游戏完成");
                NetHelper.Net.sendMessageToSever(true, Cmd.GameEnd, "游戏完成");
            }
            else
            {
                SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "游戏终止");
                NetHelper.Net.sendMessageToSever(true, Cmd.GameAbort,"游戏终止");
            }

            // 上传BDF文件到服务端
            // 通知服务端需要上传结果文件
            if (null != gameConfigModel.UserInfo)
            {
                JObject resultData = new JObject();
                resultData["userName"] = gameConfigModel.UserInfo.Name;
                resultData["filePath"] = comDevice.BDFFilePath;
                NetHelper.Net.sendMessageToSever(true, Cmd.UploadResultFile, "客户端请求结果文件上传", resultData.ToString(Newtonsoft.Json.Formatting.None));
            }

            // 生成结果报告
            if (null != gameConfigModel.UserInfo)
            {
                FastReportHelper fastReportHelper = new FastReportHelper();
                string filePath = fastReportHelper.createReportData(App.CurrentUser, this.gameConfigModel);

                JObject resultData = new JObject();
                resultData["TestNumber"] = gameConfigModel.UserInfo.TestNumber;
                resultData["UserName"] = gameConfigModel.UserInfo.Name;
                resultData["DetectNumber"] = gameConfigModel.UserInfo.DetectNumber;
                // 装换游戏字段
                string game = "";
                foreach (var item in gameConfigModel.paradigmSettings.GameList)
                {
                    if (item.Value == gameConfigModel.Game)
                    {
                        game = item.Key;
                        break;
                    }
                }
                resultData["Game"] = game;
                resultData["UserSex"] = gameConfigModel.UserInfo.Sex;
                resultData["UserAge"] = gameConfigModel.UserInfo.Age;
                resultData["CreateTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                resultData["FilePath"] = filePath;
                resultData["Client"] = $"{SQLiteDBService.DB.ReadSettings("App", "ClientName")}_{SQLiteDBService.DB.ReadSettings("App", "ClientId")}";
                NetHelper.Net.sendMessageToSever(true, Cmd.UploadResultFilePdf, "客户端请求结果文件上传", resultData.ToString(Newtonsoft.Json.Formatting.None));
            }


            App.CurrentUser = null;
        }

        private void CalcusvrClientRcvFail(object sender, FailArgs e)
        {
            Message.ShowError("游戏服务异常", e.AddtionalStr + "接收数据错误：" + e.e.Message, TimeSpan.FromSeconds(4));
        }

        private void CalcusvrClientCommuFail(object sender, FailArgs e)
        {
            Message.ShowError("游戏服务异常", e.AddtionalStr + "远程连接错误：" + e.e.Message, TimeSpan.FromSeconds(4));
        }

        private void GameSvrListenError(object sender, FailArgs e)
        {
            var result = MessageBox.Show("游戏通信服务启动错误,是否关闭游戏服务?", "提示", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                comDevice.StopSaveBDF();
                TimeCount.Content = " ";
                detectMenuItem.IsEnabled = true;
                settingsMenuItem.IsEnabled = true;
                BDFSaveMenuItem.IsEnabled = true;
                settingsMenuItem.Opacity = 1;
                detectMenuItem.Opacity = 1;
                BDFSaveMenuItem.Opacity = 1;
                start_Calusvr.ToolTip = "开启运算服务";
                if (gameServer != null)
                {
                    gameServer.stopServer();
                    gameServer = null;
                }

                Image ib = new Image();
                string packUri = "pack://application:,,,/icons/Calculator.png";
                ib.Source = new BitmapImage(new Uri(packUri));
                start_Calusvr.Header = ib;
            }
        }

        private void CalcusvrParadigmChannelError(object sender, FailArgs e)
        {
            Message.ShowError("游戏服务异常", e.AddtionalStr + "通道位置选择与所选训练模式不符!", TimeSpan.FromSeconds(4));
        }
        private void CalcusvrParadigmRefError(object sender, FailArgs e)
        {
            Message.ShowError("游戏服务异常", e.AddtionalStr + "参考选择与所选训练模式不符!", TimeSpan.FromSeconds(4));
        }
        #endregion

    }
}
