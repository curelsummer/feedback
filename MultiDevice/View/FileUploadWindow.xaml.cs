using MultiDevice.Net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace MultiDevice
{
    /// <summary>
    /// FileUploadWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FileUploadWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        public FileUploadWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="uploadUrl"></param>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        private async Task uploadFileAsync(string uploadUrl, string filePath, string data, string description, string type)
        {
            var fileInfo   = new FileInfo(filePath);
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            // 自定义进度条
            var progress = new Progress<double>(percent =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    progressBar.Value = percent;
                    txtProgress.Text = string.Format("{0:F2}%", percent);
                });
            });

            // 创建上传流
            var uploadStream = new FileUploadProgressableStream(fileStream, progress, fileInfo.Length);
            // 创建 MultipartFormDataContent 并关联上传流
            var content = new MultipartFormDataContent
            {
                { new StreamContent(uploadStream), "file", Path.GetFileName(filePath) },
                { new StringContent(description), "description" },
                { new StringContent(data), "data" },
                { new StringContent(type), "type" },
                { new StringContent(App.ClientId), "clientId" }
            };

            // 使用 HttpClient 上传文件
            var response = await client.PostAsync(uploadUrl, content);
            if (response.IsSuccessStatusCode)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txtProgress.Text = "上传成功！";
                });
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txtProgress.Text = $"上传失败: {response.StatusCode}";
                });
            }
        }


        public void startUploadFile(string fileName, string info, JObject resultData, string type)
        {
            if (!App.IsConnectServer)
            {
                // 关闭窗口
                Close(); 
                return;
            }

            if(type == "2")
            {
                Title = "实验报告上传";
            }

            Task.Run(async () =>
            {
                // 开始上传文件
                string url = $"http://{NetHelper.Net.ServerIp}:9898/api/upload";
                LogHelper.Log.LogDebug($"提交结果文件:{url},{fileName},{type}");
                await uploadFileAsync(url, fileName, resultData.ToString(), info, type);

                // 上传完成后关闭窗口
                Dispatcher.Invoke(() =>
                {
                    Task.Delay(1000).Wait();
                    Message.ShowSuccess("文件上传", "实验数据文件上传服务端成功!", TimeSpan.FromSeconds(3));
                    Close();
                });
            });
        }

        private void Client_ProgressEvent(object sender, FTPClientHelperEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Progress;
                txtProgress.Text = string.Format("{0:F2}%", e.Progress);
            });
        }
    }
}
