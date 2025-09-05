using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
    /// PasswordResetCheckWindow.xaml 的交互逻辑
    /// </summary>
    public partial class PasswordResetCheckWindow : Window
    {
        public static string GetRequestCode()
        {
            string OSDescription  = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            string OSArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString().Trim();
            DateTime timer = DateTime.Now;
            string timestamp = timer.ToString("yyyy-MM-dd:HH");
            int nnn2 = timer.Minute / 5;
            string info2hash = System.Environment.UserName + System.Environment.MachineName + OSDescription + OSArchitecture + "_" + timestamp + "-" + nnn2;
            var info_source  = ASCIIEncoding.ASCII.GetBytes(info2hash);
            var hash = new MD5CryptoServiceProvider().ComputeHash(info_source);
            StringBuilder sOutput = new StringBuilder(hash.Length);
            for (int ix = 0; ix < hash.Length - 1; ix++)
            {
                sOutput.Append(hash[ix].ToString("X2"));
            }
            return sOutput.ToString().Substring(0, 6);
        }

        public PasswordResetCheckWindow()
        {
            InitializeComponent();
            MachineCodeLabel.Text = "重置码:" + GetRequestCode();
        }

        public  bool Verify(string verify_code)
        {
            string request_code = GetRequestCode();
            string text  = request_code + "Jielian Co.Ltd.";
            byte[] bytes = ASCIIEncoding.ASCII.GetBytes(text);
            var hash = new MD5CryptoServiceProvider().ComputeHash(bytes);
            StringBuilder sOutput = new StringBuilder(hash.Length);
            for (int ix = 0; ix < hash.Length - 1; ix++)
            {
                sOutput.Append(hash[ix].ToString("X2"));
            }
            string correct_answer = sOutput.ToString().Substring(0, 6).ToLower();
            return verify_code.Trim().ToLower() == correct_answer;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Verify(VerifyCode.Text))
            {
                DialogResult = true;
                this.Close();
            }
            else
            {
                Message.ShowError("验证码错误", "请重新输入", TimeSpan.FromSeconds(5));
            }
        }
    }
}
