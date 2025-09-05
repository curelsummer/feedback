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
    /// LogUserSelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LogUserSelectWindow : Window
    {
        public LogUserSelectWindow()
        {
            InitializeComponent();
        }

        private void DoctorButton_Click(object sender, RoutedEventArgs e)
        {
            UserCheckWindow userCheckWindow = new UserCheckWindow();    
            if(userCheckWindow.ShowDialog() == true)
            {
                this.DialogResult = true;
                this.Close();
            }

            if (userCheckWindow.IsResetUser)
            {
                // 关闭自己
                this.Close();
            }
        }

        public void UserButton_Click(object sender, RoutedEventArgs e)
        {
            App.loginUser.UserRole = UserRole.患者;
            this.DialogResult = true;
            this.Close();
        }
    }
}
