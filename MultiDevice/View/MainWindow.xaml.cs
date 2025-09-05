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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Media.Animation;
using System.Collections.Specialized;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO;
using Microsoft.Maps.MapControl.WPF.Overlays;
using MultiDevice.Net;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;
using Image = System.Windows.Controls.Image;
using MultiDevice.DB;


namespace MultiDevice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, ComSvr> ComDevices = new Dictionary<string, ComSvr>();
        private Label backgroundLabel = new Label();
        private DoubleAnimation backgroundAnimation     = null;
        private DeviceConnectWindow deviceConnectWindow = null;
        private ComSvr device = null;
        private GameConfigModel gameConfigModel = null;
        private bool IsAutoStart = false;
        public MainWindow()
        {
            #region 界面初始化
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            DoubleAnimation daAnimation = new DoubleAnimation();
            daAnimation.From = 0;
            daAnimation.To = 1;
            daAnimation.Duration = TimeSpan.FromSeconds(1);
            deviceSearchBtn.BeginAnimation(Button.OpacityProperty, daAnimation);

            backgroundAnimation = new DoubleAnimation();
            backgroundAnimation.From = 0;
            backgroundAnimation.To = 1;
            backgroundAnimation.Duration = TimeSpan.FromMilliseconds(500);

            Image splashImage = new Image();
            Random rd = new Random();
            string packUri = "pack://application:,,,/icons/splash" + rd.Next(1, 6) + ".png";
            splashImage.Source = new BitmapImage(new Uri(packUri));

            backgroundLabel.Background = new SolidColorBrush(Color.FromRgb(0xff, 0xff, 0xff));
            splashImage.Stretch = Stretch.Fill;
            backgroundLabel.Content = splashImage;
            backgroundLabel.HorizontalAlignment = HorizontalAlignment.Right;
            backgroundLabel.VerticalAlignment = VerticalAlignment.Center;

            centerPanel.Children.Add(backgroundLabel);
            Grid.SetRow(backgroundLabel, 0);
            Grid.SetColumn(backgroundLabel, 0);
            Grid.SetColumnSpan(backgroundLabel, 3);
            backgroundLabel.BeginAnimation(Label.OpacityProperty, backgroundAnimation);
            #endregion
            IsAutoStart = false;
            verifiReg();
        }

        #region 主界面启动
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.logUserSelectWindow       = new LogUserSelectWindow();
            App.logUserSelectWindow.Owner = this;
            App.logUserSelectWindow.Closed += LogUserSelectWindow_Closed;
            if (App.logUserSelectWindow.ShowDialog() == false)
            {
                this.Close();
                return;
            }

            // 连接服务端
            NetHelper.Net.connectToServer();

            if (App.loginUser.UserRole == UserRole.医生)
            {
                Title = $"自适应反馈系统-【登录用户:{App.loginUser.UserName}({App.loginUser.UserRole.ToString()})】";

                // 启动监控服务端
                GameAppRunHelper.App.AutoRunSeverApp();
                // 关闭自己
                this.Close(); 
            }
            else
            {
                Title = $"自适应反馈系统-【登录用户:{App.loginUser.UserRole.ToString()}】-【终端名称:{App.ClientName}({App.ClientId})】";
                // 自动进入到设备采集界面
                connectToDevice(true);
            }
        }

        private void LogUserSelectWindow_Closed(object sender, EventArgs e)
        {
            App.logUserSelectWindow = null;
        }
        #endregion

        #region 启动机器码校验
        private void verifiReg()
        {

            StringBuilder sys_info_hash_output = App.GetDeviceSerialNumber();
            if (!File.Exists(sys_info_hash_output.ToString() + ".key"))
            {
                Login mylogin = new Login();
                this.Hide();
                mylogin.ShowDialog();
                if (mylogin.DialogResult == true)
                {

                    (new Thread(() =>
                    {
                        string content_str  = "abc";
                        string content_str2 = "abc";

                        for (int ix = 0; ix < 10; ix++)
                        {
                            content_str = content_str + content_str2 + sys_info_hash_output.ToString();
                            content_str2 = content_str2 + System.Text.Encoding.Default.GetString(new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(content_str))).ToString();
                        }
                        try
                        {

                            StreamWriter sw = new StreamWriter(sys_info_hash_output.ToString() + ".key");
                            sw.WriteLine(content_str2);
                            sw.Close();
                        }
                        catch (Exception eee)
                        {
                            Environment.Exit(Environment.ExitCode);
                        }
                    })).Start();

                    Show();
                }
                else
                {
                    Environment.Exit(Environment.ExitCode);
                }
            }
        }
        #endregion

        #region 窗口关闭事件
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach(var value in ComDevices.Values)
            {
                value.stop();
                LogHelper.Log.LogDebug($"程序关闭,执行设备退出");
            }
        }
        #endregion

        #region 设备搜索
        private void deviceSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            connectToDevice();
        }

        public void connectToDevice(bool autoStart = false, GameConfigModel gameConfig = null)
        {
            if(null != App.deviceMainWindow)
            {
                return;
            }

            // 向服务端发送自动连接设备消息
            NetHelper.Net.sendMessageToSever(true, Cmd.Message, "自动连接设备...");          
            deviceConnectWindow = new DeviceConnectWindow();
            deviceConnectWindow.connectDeviceEvent += DeviceConnectWindow_connectDeviceEvent;
            if (autoStart)
            {
                IsAutoStart= autoStart;
                gameConfigModel = gameConfig;
                deviceConnectWindow.deviceConnectFinishedEvent += DeviceConnectWindow_deviceConnectFinishedEvent;
            }
            deviceConnectWindow.searchDevice(ref device);
            deviceConnectWindow.Show();
        }

        /// <summary>
        /// 设备连接完成后自动开始
        /// </summary>
        private void DeviceConnectWindow_deviceConnectFinishedEvent()
        {
            showDeviceMainWindow();
            deviceConnectWindow.Close();
        }

        private void DeviceConnectWindow_connectDeviceEvent()
        {
            showDeviceMainWindow();
        }

        private void showDeviceMainWindow()
        {
            ComDevices.Clear();

            string[] deviceInfo = deviceConnectWindow.deviceInfo;
            if(deviceInfo.Count() < 2)
            {
                Message.ShowError("设备连接异常", "设备连接异常,设备信息返回有误!");
                return;
            }

            ComDevices[deviceInfo[1]] = device;
            device.receiveFail += Device_receiveFail;
            device.ConnectNotStable += Device_ConnectNotStable;
            device.FileError += Device_FileError;
            device.Svr_stop += Device_Svr_stop;
            device.connectFail += Device_connectFail;
            Thread deviceRunThread = new Thread(device.start);
            deviceRunThread.IsBackground = true;
            deviceRunThread.Start();

            App.deviceMainWindow = new MultiDeviceMainWindow(device);
            App.deviceMainWindow.Owner = this;
            App.deviceMainWindow.Show();

            // 自动进入阻抗匹配界面
            if (IsAutoStart && null != App.deviceMainWindow && null != gameConfigModel)
            {
                App.deviceMainWindow.setGameConfig(gameConfigModel);
                App.deviceMainWindow.startImpedanceDetect();
            }
            else
            {
                NetHelper.Net.sendMessageToSever(true, Cmd.Message, "设备连接成功,数据采集中...");
            }
        }

        private void Device_connectFail(object sender, FailArgs e)
        {

        }

        private void Device_Svr_stop(object sender, myEventArgs e)
        {

        }

        private void Device_FileError(object sender, FailArgs e)
        {

        }

        private void Device_ConnectNotStable(object sender, FailArgs e)
        {

        }

        private void Device_receiveFail(object sender, FailArgs e)
        {
            this.Dispatcher.BeginInvoke((Action)delegate ()
            {
                App.deviceMainWindow.Close();
                App.deviceMainWindow = null;
            });

            Message.ShowError("通信异常", $"脑电设备数据接收发生异常:{e.e.Message}");
        }
        #endregion

    }
}
