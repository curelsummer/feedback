using MultiDevice.DB;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// UserCheckWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UserCheckWindow : Window
    {
        public bool IsResetUser = false;
        public UserCheckWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LogUserInfoModel userInfo = SQLiteDBService.DB.GetUserInfo();
            if (null == userInfo)
            {
                App.loginUser.UserName = "doctor";
                App.loginUser.Password = "JL123@321";
                App.loginUser.UserRole = UserRole.医生;
                UserNameEdit.Text = App.loginUser.UserName;
            }
            else
            {
                App.loginUser          = userInfo;
                App.loginUser.UserRole = UserRole.医生;
                UserNameEdit.Text = userInfo.UserName;
            }
        }


        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if(UserNameEdit.Text == "" || 
               PasswordEdit.Text == "")
            {
                MessageBox.Show("用户名或密码不能为空!", "用户登录", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(UserNameEdit.Text != App.loginUser.UserName || 
                PasswordEdit.Text != App.loginUser.Password)
            {
                MessageBox.Show("用户名或密码不正确!", "用户登录", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            DialogResult = true;
            this.Close();   
        }

        private void ResetPasswordLabel_Click(object sender, RoutedEventArgs e)
        {
            PasswordResetCheckWindow passwordResetCheckWindow = new PasswordResetCheckWindow();
            if(passwordResetCheckWindow.ShowDialog() == true)
            {
                SQLiteDBService.DB.ResetUserInfo();
                Message.ShowSuccess("用户重置", "用户信息重置成功!", TimeSpan.FromSeconds(3));
            }
        }

        private void ModifyLabel_Click(object sender, RoutedEventArgs e)
        {
            LoginUserModifyWindow loginUserModifyWindow = new LoginUserModifyWindow();  
            loginUserModifyWindow.ShowDialog();
            // 更新修改
            UserNameEdit.Text = App.loginUser.UserName;
        }
    }
}
