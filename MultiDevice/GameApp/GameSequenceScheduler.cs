using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MultiDevice
{
    public static class GameSequenceScheduler
    {
        private static readonly object _syncRoot = new object();
        private static bool _isRunning = false;

        private class SequenceWrapper
        {
            public List<GameRunTask> Tasks { get; set; }
        }

        public static bool StartIfConfigured(MultiDeviceMainWindow mainWindow)
        {
            List<GameRunTask> tasks = LoadSequenceOrNull();
            if (tasks == null || tasks.Count == 0)
            {
                return false;
            }

            lock (_syncRoot)
            {
                if (_isRunning)
                {
                    return true;
                }
                _isRunning = true;
            }

            Task.Run(() => RunSequence(mainWindow, tasks));
            return true;
        }

        public static bool StartForSelectedGames(MultiDeviceMainWindow mainWindow, GameConfigModel config)
        {
            if (config == null || config.GameSequence == null || config.GameSequence.Count == 0)
            {
                return false;
            }

            var tasks = new List<GameRunTask>();
            foreach (var gameCode in config.GameSequence)
            {
                tasks.Add(new GameRunTask
                {
                    ExePath = Path.Combine("GameAppBin", "Trainer.exe"),
                    ProcessName = "Trainer",
                    Args = $"--scene {gameCode}",
                    GameCode = gameCode,
                    MinDuration = TimeSpan.Zero,
                    Timeout = TimeSpan.FromMinutes(30)
                });
            }

            lock (_syncRoot)
            {
                if (_isRunning)
                {
                    return true;
                }
                _isRunning = true;
            }

            Task.Run(() => RunSequence(mainWindow, tasks));
            return true;
        }

        public static bool IsRunning
        {
            get
            {
                lock (_syncRoot)
                {
                    return _isRunning;
                }
            }
        }

        public static bool HasSequenceConfigured()
        {
            var tasks = LoadSequenceOrNull();
            return tasks != null && tasks.Count > 0;
        }

        private static List<GameRunTask> LoadSequenceOrNull()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string cfgPath1 = Path.Combine(baseDir, "GameSequence.json");
                string cfgPath2 = Path.Combine(baseDir, "Config", "GameSequence.json");
                string path = File.Exists(cfgPath1) ? cfgPath1 : (File.Exists(cfgPath2) ? cfgPath2 : null);
                if (path == null)
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                // 兼容两种结构：直接数组或包在 {"Tasks": [...]} 中
                if (json.TrimStart().StartsWith("["))
                {
                    var list = JsonConvert.DeserializeObject<List<GameRunTask>>(json);
                    return NormalizeTasks(list);
                }
                else
                {
                    var wrapper = JsonConvert.DeserializeObject<SequenceWrapper>(json);
                    return NormalizeTasks(wrapper?.Tasks);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError($"加载 GameSequence.json 失败: {ex.Message}");
                return null;
            }
        }

        private static List<GameRunTask> NormalizeTasks(List<GameRunTask> tasks)
        {
            if (tasks == null)
            {
                return null;
            }
            foreach (var t in tasks)
            {
                if (string.IsNullOrWhiteSpace(t.ProcessName))
                {
                    t.ProcessName = "Trainer";
                }
                if (t.MinDuration == TimeSpan.Zero)
                {
                    t.MinDuration = TimeSpan.FromSeconds(0);
                }
                if (t.Timeout == TimeSpan.Zero)
                {
                    t.Timeout = TimeSpan.FromMinutes(10);
                }
            }
            return tasks;
        }

        private static void RunSequence(MultiDeviceMainWindow mainWindow, List<GameRunTask> tasks)
        {
            try
            {
                // 确保沿用首次用户：如果已存在 CurrentUser，则每轮启动前确保不为 null
                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];

                    // 开始单次运行：保持现有逻辑不变，由 UI 线程启动
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            LogHelper.Log.LogInfo($"[序列] 启动第{ i + 1 }个游戏: {task}");
                            if (App.CurrentUser == null && mainWindow != null && mainWindow.gameConfigModel != null)
                            {
                                App.CurrentUser = mainWindow.gameConfigModel.UserInfo;
                            }
                            if (mainWindow != null && mainWindow.gameConfigModel != null && !string.IsNullOrWhiteSpace(task.GameCode))
                            {
                                // 切换当前要下发给游戏端的 Game 配置（影响 Cmd=90 内容）
                                mainWindow.gameConfigModel.Game = task.GameCode;
                            }
                            mainWindow.PendingGameTask = task;
                            mainWindow.startGameSever();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log.LogError($"[序列] 启动失败，跳过本项: {ex.Message}");
                        }
                    });

                    DateTime startAt = DateTime.Now;
                    // 等待最短时长
                    if (task.MinDuration > TimeSpan.Zero)
                    {
                        SleepWithCancellation(task.MinDuration);
                    }

                    // 等待进程退出或超时
                    TimeSpan remain = task.Timeout;
                    if (remain <= TimeSpan.Zero)
                    {
                        remain = TimeSpan.FromMinutes(10);
                    }

                    bool exited = WaitUntilProcessExit(task.ProcessName, remain);
                    // 无论是否超时，都统一收口到 StopGameSever(true)，以保持单次结束逻辑一致
                    if (!exited)
                    {
                        LogHelper.Log.LogError($"[序列] 超时未退出，准备触发 StopGameSever");
                    }
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            mainWindow.StopGameSever(true);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log.LogError($"[序列] StopGameSever 失败: {ex.Message}");
                        }
                    });

                    // 再等待进程退出一小段时间（以防结束逻辑是异步的）
                    WaitUntilProcessExit(task.ProcessName, TimeSpan.FromSeconds(30));

                    // 间隔1-2秒再启动下一项，避免资源未释放
                    SleepWithCancellation(TimeSpan.FromSeconds(2));
                }
            }
            finally
            {
                lock (_syncRoot)
                {
                    _isRunning = false;
                }
                LogHelper.Log.LogInfo("[序列] 全部任务执行完成");
            }
        }

        private static bool WaitUntilProcessExit(string processName, TimeSpan timeout)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try
                {
                    var procs = Process.GetProcessesByName(processName);
                    if (procs == null || procs.Length == 0)
                    {
                        return true;
                    }
                }
                catch { }
                Thread.Sleep(1000);
            }
            return false;
        }

        private static void SleepWithCancellation(TimeSpan span)
        {
            if (span <= TimeSpan.Zero) return;
            int ms = (int)Math.Max(0, span.TotalMilliseconds);
            Thread.Sleep(ms);
        }
    }
}


