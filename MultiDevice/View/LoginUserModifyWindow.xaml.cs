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
    /// LoginUserModifyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginUserModifyWindow : Window
    {
        public LoginUserModifyWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            OldUserNameEdit.Text = App.loginUser.UserName;
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            if(OldUserNameEdit.Text == "" ||
                OldPasswordEdit.Text == "")
            {
                MessageBox.Show("旧用户名或密码不能为空 !", "用户信息修改", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(NewUserNameEdit.Text == "" ||
                NewPasswrodEdit.Text == "")
            {
                MessageBox.Show("新用户名或密码不能为空 !", "用户信息修改", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(OldUserNameEdit.Text != App.loginUser.UserName ||
                OldPasswordEdit.Text != App.loginUser.Password) 
            {
                MessageBox.Show("用户名或密码不正确 !", "用户信息修改", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if(MessageBoxResult.No == MessageBox.Show("确定修改当前用户信息 ?", "用户信息修改", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                return;
            }

            App.loginUser.UserName = NewUserNameEdit.Text;
            App.loginUser.Password = NewPasswrodEdit.Text;
            App.loginUser.UserRole = UserRole.医生;

            SQLiteDBService.DB.UpdateLoginUser(App.loginUser);

            Message.ShowSuccess("信息修改", "用户信息修改成功!", TimeSpan.FromSeconds(2));
            this.Close();   
        }
    }
}
