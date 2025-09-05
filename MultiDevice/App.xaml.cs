
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using MultiDevice.DB;
using MultiDevice.Net;
using Notification.Wpf.Constants;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;


namespace MultiDevice
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string OSDescription  { get; } = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public static string OSArchitecture { get; } = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();

        public static MultiDeviceMainWindow deviceMainWindow = null;
        public static ComImpedanceDetect impedanceDetectWindow = null;
        public static MainWindow    mainWindow   = null;
        public static UserInfoModel CurrentUser  = null;
        public static LogUserInfoModel loginUser = new LogUserInfoModel();
        public static bool IsConnectDevice       = false;
        public static LogUserSelectWindow  logUserSelectWindow = null;
        public static UserRegisterWindow   userRegisterWindow  = null;
        public static UserSelectWindow     userSelectWindow    = null;

        public static string ClientName = "";
        public static string ClientId   = "";

        public static bool IsConnectServer = false;

        public static StringBuilder GetDeviceSerialNumber()
        {
            string sys_info   = System.Environment.UserName + System.Environment.MachineName + OSDescription + OSArchitecture;
            var sys_info_hash = new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(sys_info));
            StringBuilder sys_info_hash_output = new StringBuilder(sys_info_hash.Length);
            for (int ix = 0; ix < sys_info_hash.Length - 1; ix++)
            {
                sys_info_hash_output.Append(sys_info_hash[ix].ToString("X2"));
            }

            return sys_info_hash_output;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // 处理非 UI 线程的未处理异常
            if (e.ExceptionObject is Exception ex)
            {
                LogHelper.Log.LogError($"全局异常捕获非UI 线程异常: {ex.ToString()}");
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 处理 UI 线程的未处理异常
            LogHelper.Log.LogError($"全局异常捕获UI线程异常: {e.Exception.ToString()}");
            e.Handled = true;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 定义一个唯一的 Mutex 名称
            bool createdNew;
            Mutex mutex = new Mutex(true, "{E8A1FA90-0294-48E6-AFE0-6B42303C98A4}", out createdNew);
            // 如果 Mutex 已经存在，说明程序已经启动
            if (!createdNew)
            {
                MessageBox.Show("脑电生物反馈系统程序,已经在运行,请不要重复运行！", "脑电生物反馈系统");
                return; 
            }

            SetTaskSchedulerAutoStart(true);
            NotificationConstants.MessagePosition = Notification.Wpf.Controls.NotificationPosition.TopCenter;
            // 捕获未处理的线程异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            // 捕获未处理的 UI 线程异常
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            App.ClientName = SQLiteDBService.DB.ReadSettings("App", "ClientName");
            App.ClientId   = SQLiteDBService.DB.ReadSettings("App", "ClientId");

            //UserRegisterWindow userRegisterWindow = new UserRegisterWindow();
            //userRegisterWindow.Show();

            // 启动显示主界面
            if (null == mainWindow)
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }
        }

        public  void SetTaskSchedulerAutoStart(bool isAutoStart)
        {
            string appName  = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; // 获取程序名称
            string taskName = $"{appName}_AutoStart";   // 动态生成任务名称
            string appPath  = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string workingDirectory = Path.GetDirectoryName(appPath); // 设置工作目录为程序所在目录

            using (TaskService ts = new TaskService())
            {
                if (isAutoStart)
                {
                    TaskDefinition td = ts.NewTask();
                    td.RegistrationInfo.Description = $"Start {appName} on login"; // 设置任务描述

                    // 设置触发器，在用户登录时启动任务
                    td.Triggers.Add(new LogonTrigger());

                    // 设置操作，启动应用程序并指定工作目录
                    td.Actions.Add(new ExecAction(appPath, null, workingDirectory));

                    // 将任务设置为最高权限运行
                    td.Principal.RunLevel = TaskRunLevel.Highest;

                    // 注册任务
                    ts.RootFolder.RegisterTaskDefinition(taskName, td);
                }
                else
                {
                    // 删除任务
                    ts.RootFolder.DeleteTask(taskName, false);
                }
            }
        }
    }
}
