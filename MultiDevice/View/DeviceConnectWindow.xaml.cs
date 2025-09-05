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

namespace MultiDevice
{
    /// <summary>
    /// DeviceConnectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DeviceConnectWindow : Window
    {
        public delegate void connectDeviceDelegate();
        public delegate void deviceConnectFinishedDelegate();
        public event connectDeviceDelegate         connectDeviceEvent = null;
        public event deviceConnectFinishedDelegate deviceConnectFinishedEvent = null;
        public string[] deviceInfo = null;
        public DeviceConnectWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 搜索设备
        /// </summary>
        public void searchDevice(ref ComSvr device)
        {
            var usbDeviceList = HIDDevice.getConnectedDevices();
            foreach (var item in usbDeviceList)
            {
                if (item.manufacturer.Contains("STM"))
                {
                    LogHelper.Log.LogDebug("搜索到脑电设备");
                    device = new ComSvr(item.devicePath);
                    // 初始化连接设备
                    device.connectFail    += Device_connectFail; 
                    device.validComFinish += Device_validComFinish; 
                    device.check();
                    break;
                }
            }
        }

        private void Device_validComFinish(object sender, FailArgs e)
        {
            if (!e.AddtionalStr.Contains("fail"))
            {
                deviceInfo = e.AddtionalStr.Split(',');
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    deviceNameLabel.Text = deviceInfo[1];
                    deviceStatusIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 250, 0));
                    connectDeviceButon.IsEnabled = true;
                    if(null != deviceConnectFinishedEvent)
                    {
                        deviceConnectFinishedEvent.Invoke();
                    }
                });
            }
            else
            {
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    deviceNameLabel.Text = "连接设备失败!";
                    deviceStatusIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    LogHelper.Log.LogError($"获取脑电设备名称失败 : {e.AddtionalStr} !");
                });
            }
        }

        private void Device_connectFail(object sender, FailArgs e)
        {
            Message.ShowError("自动连接设备", $"脑电设备连接失败: {e.e.Message},{e.AddtionalStr} !");
        }

        private void connectDeviceButon_Click(object sender, RoutedEventArgs e)
        {
            if(null != connectDeviceEvent)
            {
                connectDeviceEvent.Invoke();
            }
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
