using MultiDevice.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Notifications.Wpf.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;


// 客户端消息中心,用于分发处理客户端消息

namespace MultiDevice.Net
{
    public class MessageCenterEventArgs : EventArgs
    {
        public string Data { get; set; }
        public MessageCenterEventArgs(string data)
        {
            Data = data;
        }
    }

    public class MessageProcessCenter
    {
        private DesktopSharingHelper _DesktopSharingHelper    = null;
        public event EventHandler<MessageCenterEventArgs> SendDataToServerEvent;
        // 发送数据事件
        public MessageProcessCenter() 
        { 
        
        }

        public void ProcessServerMessage(NetData message)
        {
            switch(message.cmd)
            {
                case "Message":
                    break;
                case "ShowUserList":
                    {
                        ObservableCollection<UserInfoModel> userList = JsonConvert.DeserializeObject<ObservableCollection<UserInfoModel>>(message.data);

                        if (null != App.userRegisterWindow && App.userRegisterWindow.IsVisible)
                        {
                            if (Application.Current.Dispatcher.CheckAccess())
                            {
                                App.userRegisterWindow.showUserList(userList);
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    App.userRegisterWindow.showUserList(userList);
                                });
                            }
                        }

                        if (null != App.userSelectWindow && App.userSelectWindow.IsVisible)
                        {
                            if (Application.Current.Dispatcher.CheckAccess())
                            {
                                App.userSelectWindow.showUserList(userList);
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    App.userSelectWindow.showUserList(userList);
                                });
                            }
                        }
                    }
                    break;
                case "PrepareGame":
                    {
                        GamePrepare(message.data);
                        LogHelper.Log.LogDebug("接收到服务端游戏准备指令,触发游戏准备");
                    }
                    break;
                case "StartGame":
                    {
                        GameStart(message.data);
                    }
                    break;
                case "AbortGame":
                    {
                        StopGame(message.data);
                        LogHelper.Log.LogDebug("接收到服务端游戏停止指令,触发游戏停止");
                    }
                    break;
                case "DesktopSharingStart":
                    startDesktopSharing(message.data);
                    break;
                case "DesktopSharingEnd":
                    stopDesktopSharing();
                    break;
                case "ResetUserStatus":
                    RestUserStatus(message.data);
                    break;
                case "ImpedanceMatchingSuccess":
                    ImpedanceMatchingSuccess();
                    break;
                case "UploadResultFile":
                    UploadDataFileToServer(message.data);
                    break;
                case "UploadResultFilePdf":
                    {
                        UploadPdfDataFileToServer(message.data);
                    }
                    break;
                case "GetParadigmTypeList":
                    {
                        List<string > paradigmList = SQLiteDBService.DB.ReadConfigKeys("paradigmSetting"); 
                        NetHelper.Net.sendMessageToSever(true, Cmd.GetParadigmTypeList, "范式列表获取成功", 
                            JArray.FromObject(paradigmList).ToString(Formatting.None));
                    }
                    break;
                case "GetGameList":
                    {
                        SendGameListToServer(message.data); 
                    }
                    break;
                case "CloseClient":
                    {
                        shutdownClient();
                    }
                    break;
                default:
                    break;
            }
        }

        #region 开始游戏准备
        // 游戏开始
        public void GamePrepare(string gameData)
        {
            if (null != App.CurrentUser)
            {
                // 已有游戏再进行中
                NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, "当前客户端已经正在进行游戏!");
                return;
            }

            JObject  gameConfig    = JObject.Parse(gameData);
            UserInfoModel userInfo = JsonConvert.DeserializeObject<UserInfoModel>(gameConfig["UserInfo"].ToString());

            string userStatus = SQLiteDBService.DB.QueryUserStatus(userInfo.TestNumber);
            if (userStatus != "未开始")
            {
                NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, string.Format("{0},处于:[{1}]状态,不能开始游戏!", userInfo.Name, userStatus));
                return;
            }

            // 解析游戏参数
            GameConfigModel gameConfigModel = new GameConfigModel();
            JToken gameConfigToken = gameConfig["GameConfig"];

            // 临时使用
            string paradigmKey = gameConfigToken["ParadigmType"].ToString();
            //if (paradigmKey == "New Alpha Power(焦虑、抑郁症)")
            //{
            //    paradigmKey = "SMR Power(失眠症)";
            //}

            string paradigmConfig = SQLiteDBService.DB.ReadSettings("paradigmSetting", paradigmKey);
            if (string.IsNullOrEmpty(paradigmConfig))
            {
                string error = $"数据库中未查找到范式:{paradigmKey}的配置信息";
                NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, error);
                return;
            }
            // 组合游戏信息  
            gameConfigModel.paradigmSettings = JsonConvert.DeserializeObject<ParadigmSettingsModel>(paradigmConfig);
            gameConfigModel.Game = gameConfigModel.paradigmSettings.GameList[gameConfigToken["Game"].ToString()];
            gameConfigModel.SessionNum = gameConfigToken["SessionNum"].ToString(); 
            gameConfigModel.SessionTotal = gameConfigToken["SessionTotal"].ToString(); 
            gameConfigModel.EpochCount = gameConfigToken["EpochCount"].ToString(); 
            gameConfigModel.EpochTimes = gameConfigToken["EpochTimes"].ToString(); 
            gameConfigModel.BreakTimes = gameConfigToken["BreakTimes"].ToString(); 
            gameConfigModel.UserInfo = userInfo;

            // 连接设备开始阻抗匹配
            if (!App.IsConnectDevice)
            {
                App.mainWindow.connectToDevice(true, gameConfigModel);
            }
            else
            {
                // 设备已经连接,直接进入阻抗检测
                App.deviceMainWindow.setGameConfig(gameConfigModel);
                App.deviceMainWindow.startImpedanceDetect();
            }
        }
        #endregion

        #region 开始游戏
        private void GameStart(string data)
        {           
            JObject userData  = JObject.Parse(data);
            UserInfoModel userInfo = JsonConvert.DeserializeObject<UserInfoModel>(userData["UserInfo"].ToString());
            if (null == App.CurrentUser ||
                App.CurrentUser.Name != userInfo.Name)
            {
                NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, string.Format("{0},未在游戏状态,不能开始游戏!", userInfo.Name));
                return;
            }

            string userStatus = SQLiteDBService.DB.QueryUserStatus(userInfo.TestNumber);
            if (userStatus != "阻抗匹配")
            {
                NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, string.Format("{0},未进入阻抗匹配状态,游戏准备未完成,不能开始游戏!", userInfo.Name));
                return;
            }

            App.CurrentUser = userInfo;
            if (null != App.deviceMainWindow && null != App.impedanceDetectWindow)
            {
                App.impedanceDetectWindow.startDetect = true;
                App.impedanceDetectWindow.Close();
                App.deviceMainWindow.startGameSever();
            }
        }
        #endregion

        #region 结束游戏
        public void StopGame(string gameData)
        {
            if(null == App.CurrentUser)
            {
                return;
            }
            // 通知游戏终止
            SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser, "游戏终止");

            if (null != App.impedanceDetectWindow)
            {
                App.impedanceDetectWindow.Close();
                App.impedanceDetectWindow = null;
            }

            if (null != App.deviceMainWindow)
            {
                App.deviceMainWindow.StopGameSever();
            }
            NetHelper.Net.sendMessageToSever(true, Cmd.GameAbort, "游戏停止");
            App.CurrentUser = null;
        }
        #endregion

        #region 开始屏幕共享
        private void startDesktopSharing(string serverData)
        {
            if (null == _DesktopSharingHelper)
            {
                JObject serverAddress = JObject.Parse(serverData);
                int serverPort = int.Parse(serverAddress["serverPort"].ToString());
                _DesktopSharingHelper = new DesktopSharingHelper();
                _DesktopSharingHelper.Start(NetHelper.Net.ServerIp, serverPort);
            }
        }
        #endregion

        #region 结束屏幕共享
        public void stopDesktopSharing()
        {
            if(null != _DesktopSharingHelper)
            {
                _DesktopSharingHelper.Stop();
                _DesktopSharingHelper = null;
            }
        }
        #endregion

        #region 用户状态重置
        private void RestUserStatus(string data)
        {
            JObject userData  = JObject.Parse(data);
            UserInfoModel userInfo = JsonConvert.DeserializeObject<UserInfoModel>(userData["UserInfo"].ToString());

            string status = SQLiteDBService.DB.QueryUserStatus(userInfo.TestNumber);
            if (null != App.CurrentUser &&
                App.CurrentUser.Name == userInfo.Name)
            {
                if (status != "游戏完成" ||
                    status != "游戏终止" ||
                    status != "设备异常")
                {
                    NetHelper.Net.sendMessageToSever(false, Cmd.ShowTips, string.Format("{0},在客户端{1},{2}中当前处于:{3}状态,不能重置 !", 
                        userInfo.Name, App.ClientName, App.ClientId, status));
                    return;
                }
            }

            SQLiteDBService.DB.UpdateUserStatus(userInfo, "未开始");
            NetHelper.Net.sendMessageToSever(true, Cmd.ShowTips, string.Format("{0},状态重置成功 !", userInfo.Name));
        }
        #endregion

        #region 回传客户端用户列表
        private void HandleGetUserList()
        {
            // ObservableCollection<UserInfoModel> userInfo = SQLiteDBService.DB.QueryPersonList();
            // string userList = JsonConvert.SerializeObject(userInfo, Formatting.None);
            // SendDataToServerEvent?.Invoke(this, new MessageCenterEventArgs(NetData.Return(true, Cmd.GetUserList, "回传人员列表", userList)));
        }
        #endregion

        #region 向服务端发送游戏列表
        public void SendGameListToServer(string data)
        {
            string paradigmConfig = SQLiteDBService.DB.ReadSettings("paradigmSetting", data);
            if (string.IsNullOrEmpty(paradigmConfig))
            {
                return;
            }

            ParadigmSettingsModel paradigmSettingsModel = JsonConvert.DeserializeObject<ParadigmSettingsModel>(paradigmConfig);
            paradigmSettingsModel.gameList = paradigmSettingsModel.GameList.Keys.ToList();
            NetHelper.Net.sendMessageToSever(true, Cmd.GetGameList, "游戏列表获取成功",
                           JArray.FromObject(paradigmSettingsModel.gameList).ToString(Formatting.None));

        }
        #endregion

        #region 判断阻抗匹配是否成功
        private void ImpedanceMatchingSuccess()
        {
          
        }
        #endregion

        #region 联网上传实验文件到服务端
        public void UploadDataFileToServer(string data)
        {
            JObject resultData = JObject.Parse(data);
            FileUploadWindow window = new FileUploadWindow();
            window.Owner = App.deviceMainWindow;
            window.startUploadFile(resultData["filePath"].ToString(), "实验文件BDF上传", resultData, "1");
            window.ShowDialog();
        }
        #endregion

        #region 联网上传实验文件到服务端
        public void UploadPdfDataFileToServer(string data)
        {
            JObject resultData = JObject.Parse(data);
            FileUploadWindow window = new FileUploadWindow();
            window.Owner = App.deviceMainWindow;
            window.startUploadFile(resultData["FilePath"].ToString(), "实验结果文件上传", resultData, "2");
            window.ShowDialog();
        }
        #endregion

        #region 关机
        public async void shutdownClient()
        {
            Message.ShowSuccess("设备关机", "接收到客户端关机指令,5s后设备即将关机!", TimeSpan.FromSeconds(3));
            // 异步延时 5 秒，不阻塞 UI
            await Task.Delay(5000);
            // 执行系统关机命令
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/s /t 0",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        #endregion
    }
}
