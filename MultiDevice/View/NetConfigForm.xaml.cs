using MultiDevice.DB;
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
    /// NetConfigForm.xaml 的交互逻辑
    /// </summary>
    public partial class NetConfigForm : Window
    {
        private List<string> clientIdMap = new List<string>()
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
        };

        public NetConfigForm()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ClientIdComboBox.ItemsSource = clientIdMap;
            ClientIdComboBox.SelectedIndex = 0;
            ClientNameEdit.Text    = SQLiteDBService.DB.ReadSettings("App", "ClientName");
            ServerAddressEdit.Text = SQLiteDBService.DB.ReadSettings("App", "ServerAddress");
            ClientIdComboBox.Text  = SQLiteDBService.DB.ReadSettings("App", "ClientId");
            AutoSearchServer.IsChecked = (SQLiteDBService.DB.ReadSettings("App", "IsAutoSearchServer") == "1" ? true : false);
            GameServerPort.Text        = (SQLiteDBService.DB.ReadSettings("App", "GameServerPort"));
            if (GameServerPort.Text == "")
            {
                GameServerPort.Text = "9264";
            }
        }

        private void saveConfigBtn_Click(object sender, RoutedEventArgs e)
        {
            // 权限验证
            UserCheckWindow checkForm = new UserCheckWindow();
            if (checkForm.ShowDialog() != true)
            {
                return;
            }

            SQLiteDBService.DB.WriteSettings("App", "ClientName", ClientNameEdit.Text);
            SQLiteDBService.DB.WriteSettings("App", "ClientId",   ClientIdComboBox.Text);
            SQLiteDBService.DB.WriteSettings("App", "ServerAddress", ServerAddressEdit.Text);
            SQLiteDBService.DB.WriteSettings("App", "IsAutoSearchServer", AutoSearchServer.IsChecked == true ? "1" : "0");
            SQLiteDBService.DB.WriteSettings("App", "GameServerPort", GameServerPort.Text);
            Message.ShowSuccess("配置保存", "配置信息保存成功,重启后配置生效!", TimeSpan.FromSeconds(2));
        }
    }
}
