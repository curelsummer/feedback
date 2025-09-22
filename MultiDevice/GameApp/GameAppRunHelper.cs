using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;


namespace MultiDevice
{
    public class GameAppRunHelper
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        private const int SW_RESTORE = 9;

        private static readonly GameAppRunHelper _instance = new GameAppRunHelper();
        public static GameAppRunHelper App
        {
            get
            {
                return _instance;
            }
        }

        public GameAppRunHelper()
        {

        }

        public void AutoRunGameApp()
        {
            CheckAndStartProcess("Trainer", AppDomain.CurrentDomain.BaseDirectory + "GameAppBin\\Trainer.exe", null);
        }

        public void AutoRunGameApp(string processName, string exePath, string args)
        {
            if (string.IsNullOrWhiteSpace(processName)) processName = "Trainer";
            if (string.IsNullOrWhiteSpace(exePath)) exePath = AppDomain.CurrentDomain.BaseDirectory + "GameAppBin\\Trainer.exe";
            CheckAndStartProcess(processName, exePath, args);
        }

        public void AutoRunSeverApp()
        {
            CheckAndStartProcess("AdaptiveNFBTerminalSever", AppDomain.CurrentDomain.BaseDirectory + "AppServerBin\\AdaptiveNFBTerminalSever.exe", null);
        }


        public void SetGameAppToFront()
        {
            BringProcessToFront("Trainer");
            LogHelper.Log.LogDebug("将游戏App提升到桌面前台");
        }

        public void CloseGameApp()
        {
            CloseProcessByName("Trainer");
        }

        private void CheckAndStartProcess(string processName, string processPath, string args)
        {
            if (!IsProcessRunning(processName))
            {
                if(!File.Exists(processPath))
                {
                    return;
                }
                Process process = new Process();
                process.StartInfo.FileName = processPath;
                if (!string.IsNullOrWhiteSpace(args))
                {
                    process.StartInfo.Arguments = args;
                }
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.Start();
                LogHelper.Log.LogDebug("检测到游戏App未启动,自动启动游戏App");
            }
            else
            {
                // 提到前面
                BringProcessToFront(processName);
            }
        }

        private bool IsProcessRunning(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        private void BringProcessToFront(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                IntPtr handle = processes[0].MainWindowHandle;

                // 如果窗口最小化，则恢复
                ShowWindow(handle, SW_RESTORE);

                // 设置窗口为前台
                SetForegroundWindow(handle);

                // 设置窗口置顶显示
                SetTopMost(handle, true);
            }
        }
        private static readonly IntPtr HWND_TOPMOST   = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private static void SetTopMost(IntPtr handle, bool topMost)
        {
            // 设置窗口为 TopMost 或取消
            if (topMost)
            {
                // 设置为 TopMost，窗口将显示在最顶层
                SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
            else
            {
                // 取消 TopMost
                SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            }
        }

        public void CloseProcessByName(string processName)
        {
            // 获取所有与指定名称匹配的进程
            Process[] processes = Process.GetProcessesByName(processName);
            // 遍历并关闭所有找到的进程
            foreach (Process process in processes)
            {
                try
                {
                    process.Kill(); // 终止进程
                    process.WaitForExit(); // 等待进程退出
                    Debug.WriteLine($"{processName} 进程已关闭");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"无法关闭进程 {processName}: {ex.Message}");
                }
            }
        }
    }
}
