using System;
using System.Collections.Generic;
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
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            this.Width = 400;
            this.Height = 150;
        }
        public static string OSDescription  { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
        public static string OSArchitecture { get; } = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString().Trim();

        private void veri_code_commit(object sender, RoutedEventArgs e)
        {
            var nnn  = DateTime.Now;
            var nnn2 = nnn.Minute/5;
            string info2hash = System.Environment.UserName + System.Environment.MachineName + OSDescription + OSArchitecture + "_" + nnn.ToString("yyyy-MM-dd:HH") + "-" + nnn2;
            var info_source = ASCIIEncoding.ASCII.GetBytes(info2hash);
            var info_hash = new MD5CryptoServiceProvider().ComputeHash(info_source);
            StringBuilder sOutput = new StringBuilder(info_hash.Length);
            for (int ix = 0; ix < info_hash.Length - 1; ix++)
            {
                sOutput.Append(info_hash[ix].ToString("X2"));
            }
            var hashcode_string = sOutput.ToString();
            var instr = hashcode_string.Substring(0, 6);
            info2hash = instr + "Jielian Co.Ltd.";
            info_source = ASCIIEncoding.ASCII.GetBytes(info2hash);
            info_hash = new MD5CryptoServiceProvider().ComputeHash(info_source);
            sOutput = new StringBuilder(info_hash.Length);
            for (int ix = 0; ix < info_hash.Length - 1; ix++)
            {
                sOutput.Append(info_hash[ix].ToString("X2"));
            }
            var code_gen_str = sOutput.ToString().Substring(0, 6);
            if (code_gen_str.ToLower().Equals(veri_code_tb.Text.Trim().ToLower()))
            {
                DialogResult = true;

            }

            //测试跳过
            DialogResult = true;
            Close();
        }

        private void veri_code_cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
