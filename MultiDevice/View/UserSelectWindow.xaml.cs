using MultiDevice.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using static System.Net.Mime.MediaTypeNames;

namespace MultiDevice
{
    /// <summary>
    /// UserSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserSelectWindow : Window
    {
        public ParadigmSettingsModel paradigmSettingsModel = null;
        public GameConfigModel gameConfigModel             = null;

        public UserSelectWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitUI();
            paradigmSettingsModel = new ParadigmSettingsModel();
            ParadigmComboBox.ItemsSource   = SQLiteDBService.DB.ReadConfigKeys("paradigmSetting");
            ParadigmComboBox.SelectedIndex = 0;

            GameListBox.ItemsSource = paradigmSettingsModel.GameList.Keys;
        }

        private void InitUI()
        {
            loadUserList();
        }

        private void loadUserList()
        {
            string text     = searchEdit.Text.TrimEnd();
            string strWhere = $"TestNumber like '%%{text}%%' or Name like '%%{text}%%' order by TestNumber desc";
            ObservableCollection<UserInfoModel> userList = SQLiteDBService.DB.QueryPersonList(strWhere);
            userGrid.ItemsSource = userList;
        }

        private void searchEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            loadUserList();
        }

        public void showUserList(ObservableCollection<UserInfoModel> userInfo)
        {
            userGrid.ItemsSource = userInfo;
        }

        private void RestStatusButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = userGrid.SelectedItem;
            if (selectedItem != null)
            {
                UserInfoModel userInfo = (UserInfoModel)selectedItem;
                string userStatus = SQLiteDBService.DB.QueryUserStatus(userInfo.TestNumber);
                if(!App.IsConnectServer && (App.CurrentUser == null || App.CurrentUser.Name != userInfo.Name))
                {
                    userStatus = "允许重置";
                }
                if (userStatus == "开始游戏" || userStatus == "阻抗匹配" || userStatus == "游戏中")
                {
                    MessageBox.Show($"用户:{userInfo.Name},当前处于:{userStatus}状态不能重置!", "状态重置", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (MessageBoxResult.No == MessageBox.Show(string.Format("确定重置:{0},的训练状态 ?", userInfo.Name), "提示", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    return;
                }

                SQLiteDBService.DB.UpdateUserStatus(userInfo, "未开始");
                Message.ShowSuccess("用户状态重置", "用户状态重置成功!", TimeSpan.FromSeconds(3));
                loadUserList();
            }
        }

        private void userGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
         
        }

        private void ParadigmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 临时使用
            string paradigmKey = e.AddedItems[0].ToString();
            //if(paradigmKey == "New Alpha Power(焦虑、抑郁症)")
            //{
            //    paradigmKey = "SMR Power(失眠症)";
            //}

            string paradigmConfig = SQLiteDBService.DB.ReadSettings("paradigmSetting", paradigmKey);
            if (string.IsNullOrEmpty(paradigmConfig))
            {
                return;
            }

            paradigmSettingsModel = JsonConvert.DeserializeObject<ParadigmSettingsModel>(paradigmConfig);
        }


        private void StartDetectBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = userGrid.SelectedItem;
            if(null == selectedItem)
            {
                Message.ShowWarning("游戏准备", "请选择需要游戏的用户!", TimeSpan.FromSeconds(3));
                return;
            }

            UserInfoModel userInfo = (UserInfoModel)selectedItem;
            // 校验用户状态
            if (userInfo.Status != "未开始")
            {
                Message.ShowWarning("游戏准备", $"{userInfo.Name},处于:{userInfo.Status}状态,不能开始游戏!", TimeSpan.FromSeconds(3));
                return;
            }

            // 获取选择参数
            gameConfigModel = new GameConfigModel();
            gameConfigModel.paradigmSettings = paradigmSettingsModel;

            // 设置游戏参数（支持多选）
            var selectedDisplayNames = GameListBox.SelectedItems.Cast<string>().ToList();
            if (selectedDisplayNames.Count == 0)
            {
                Message.ShowWarning("游戏准备", "请至少选择一个游戏!", TimeSpan.FromSeconds(3));
                return;
            }
            var selectedGameCodes = selectedDisplayNames.Select(d => paradigmSettingsModel.GameList[d]).ToList();
            gameConfigModel.GameSequence = selectedGameCodes;
            // 保持向后兼容：若只选一个，仍写入 Game
            gameConfigModel.Game = selectedGameCodes.First();
            gameConfigModel.SessionTotal = SessionTotal.Value.ToString();
            gameConfigModel.SessionNum = SessionNum.Value.ToString();
            gameConfigModel.EpochCount = EpochCount.Value.ToString();
            gameConfigModel.EpochTimes = EpochTimes.Value.ToString();   
            gameConfigModel.BreakTimes = BreakTimes.Value.ToString();

            if (int.Parse(gameConfigModel.SessionNum) <= 0)
            {
                MessageBox.Show("训练序号不能小于 1", "游戏参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (int.Parse(gameConfigModel.SessionTotal) <= 0)
            {
                MessageBox.Show("训练总数不能小于 1", "游戏参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (int.Parse(gameConfigModel.EpochCount) <= 0)
            {
                MessageBox.Show("训练个数不能小于 1", "游戏参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (int.Parse(gameConfigModel.EpochTimes) <= 0)
            {
                MessageBox.Show("训练时长不能小于 1", "游戏参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (int.Parse(gameConfigModel.BreakTimes) < 0)
            {
                MessageBox.Show("训练休息时长不能小于 0", "游戏参数错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            // 用户信息
            gameConfigModel.UserInfo   = userInfo;
            DialogResult = true;
        }
    }
}
