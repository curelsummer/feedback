using MultiDevice.DB;
using MultiDevice.Net;
using Notifications.Wpf.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// UserRegisterWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserRegisterWindow : Window
    {
        public UserRegisterWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTestNumber();
            LoadUserInfos();
        }

        private void LoadUserInfos()
        {
            string text =   searchEdit.Text.TrimEnd();
            string strWhere = $"TestNumber like '%%{text}%%' or Name like '%%{text}%%' order by TestNumber desc";
            userGrid.ItemsSource = SQLiteDBService.DB.QueryPersonList(strWhere);
        }

        public void showUserList(ObservableCollection<UserInfoModel> userInfo)
        {
            userGrid.ItemsSource = userInfo;
        }

        public void UpdateTestNumber()
        {
            TestNumberEdit.Text = DateTime.Now.ToString("yyyyMMddHHmmss");
            DetectNumberEdit.Text = DateTime.Now.ToString("MMddHHmmss");
        }


        private void userGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(null == userGrid.SelectedItem)
            {
                return;
            }
            UserInfoModel selectUser = (UserInfoModel)userGrid.SelectedItem;
            TestNumberEdit.Text = selectUser.TestNumber;
            DetectNumberEdit.Text = selectUser.DetectNumber == null ? DateTime.Now.ToString("MMddHHmmss") : selectUser.DetectNumber;
            NameEdit.Text = selectUser.Name;
            SexComboBox.Text = selectUser.Sex;
            BirthDayDateTimePicker.Text = selectUser.BirthDay;
            Remarks1.Text = selectUser.Remarks1;
            Remarks2.Text = selectUser.Remarks2;
        }


        public bool CheckUserInfo(UserInfoModel userInfo)
        {
            if (string.IsNullOrEmpty(userInfo.TestNumber))
            {
                Message.ShowWarning("添加患者", "测试编号不能为空 !", TimeSpan.FromSeconds(3));
                return false;
            }

            if (string.IsNullOrEmpty(userInfo.Name))
            {
                Message.ShowWarning("添加患者", "测试姓名不能为空 !", TimeSpan.FromSeconds(3));
                return false;
            }

            if (string.IsNullOrEmpty(userInfo.Sex))
            {
                Message.ShowWarning("添加患者", "测试性别不能为空 !", TimeSpan.FromSeconds(3));
                return false;
            }

            if (string.IsNullOrEmpty(userInfo.BirthDay))
            {
                Message.ShowWarning("添加患者", "测试出生日期不能为空 !", TimeSpan.FromSeconds(3));
                return false;
            }

            if (string.IsNullOrEmpty(userInfo.DetectNumber))
            {
                Message.ShowWarning("添加患者", "测试病历号不能为空 !", TimeSpan.FromSeconds(3));
                return false;
            }

            return true;
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInfoModel userInfo = new UserInfoModel();

            userInfo.TestNumber = TestNumberEdit.Text;
            userInfo.DetectNumber = DetectNumberEdit.Text;
            userInfo.Name = NameEdit.Text;
            userInfo.Sex = SexComboBox.Text;
            userInfo.BirthDay = BirthDayDateTimePicker.Text;
            userInfo.Remarks1 = Remarks1.Text;
            userInfo.Remarks2 = Remarks2.Text;
            userInfo.Status   = "未开始";
            userInfo.CreateDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (!CheckUserInfo(userInfo))
            {
                return;
            }

            SQLiteDBService.DB.InsertOrUpdatePerson(userInfo);
            Message.ShowSuccess("用户录入", "用户信息录入成功!", TimeSpan.FromSeconds(3));
            LoadUserInfos();
            UpdateTestNumber();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            TestNumberEdit.Text = "";
            NameEdit.Text = "";
            SexComboBox.Text = "男";
            BirthDayDateTimePicker.Text = DateTime.Now.ToString("yyyy-MM-dd");
            Remarks1.Text = "";
            Remarks2.Text = "";
            UpdateTestNumber();
        }

         
        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.No == MessageBox.Show("确定提交当前修改信息 ?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                return;
            }

            UserInfoModel selectUser = (UserInfoModel)userGrid.SelectedItem;
            string userStatus = SQLiteDBService.DB.QueryUserStatus(selectUser.TestNumber);
            if (!App.IsConnectServer && (App.CurrentUser == null || App.CurrentUser.Name != selectUser.Name))
            {
                userStatus = "允许修改";
            }

            if (userStatus == "开始游戏" || userStatus == "阻抗匹配" || userStatus == "游戏中")
            {
                MessageBox.Show($"用户:{selectUser.Name},当前处于:{userStatus}状态不能修改用户信息!", "信息修改", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            selectUser.Name = NameEdit.Text;
            selectUser.Sex = SexComboBox.Text;
            selectUser.BirthDay = BirthDayDateTimePicker.Text;
            selectUser.Remarks1 = Remarks1.Text;
            selectUser.Remarks2 = Remarks2.Text;

            SQLiteDBService.DB.InsertOrUpdatePerson(selectUser);
            Message.ShowSuccess("信息修改", "用户信息修改成功!", TimeSpan.FromSeconds(3));
            LoadUserInfos();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBoxResult.No == MessageBox.Show("确定删除当前用户 ?", "提示", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                return;
            }


            UserInfoModel selectUser = (UserInfoModel)userGrid.SelectedItem;
            SQLiteDBService.DB.DeletePerson(selectUser);
            Message.ShowSuccess("用户删除", "用户信息删除成功!", TimeSpan.FromSeconds(3));
            LoadUserInfos();
        }

        private void NameEdit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 允许的字符（仅字母和汉字）
            string pattern = @"^[a-zA-Z\u4e00-\u9fa5]+$";
            e.Handled = !Regex.IsMatch(e.Text, pattern);
        }

        private void TestNumberEdit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 正则表达式匹配数字
            string pattern = @"^\d+$";
            e.Handled = !Regex.IsMatch(e.Text, pattern); // 若不是数字，则阻止输入
        }

        private void DetectNumberEdit_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 正则表达式匹配数字
            string pattern = @"^\d+$";
            e.Handled = !Regex.IsMatch(e.Text, pattern); // 若不是数字，则阻止输入
        }

        private void searchEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadUserInfos();
        }
    }
}
