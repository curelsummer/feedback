using HPSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MultiDevice.Net
{
    public class NetClient 
    {
        private string LogName = "Net";
        private ITcpClient _TcpClient = null;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(1); // 心跳包发送间隔
        private TimeSpan _heartbeatTimeout  = TimeSpan.FromSeconds(10); // 心跳超时时间
        private TimeSpan _reconnectDelay = TimeSpan.FromSeconds(5);    // 重连间隔
        private DateTime _lastHeartbeatReceived = DateTime.MinValue;   // 最后收到心跳的时间
        private bool _isConnected = false;
        private MessageProcessCenter _messageCenter = null;
        private string ReceivedData = "";
        public NetClient(string serverIp, ushort serverPort)
        {
            // 初始化TCP客户端
            _TcpClient = new HPSocket.Tcp.TcpClient
            {
                Async = false, // 同步连接
                Address = serverIp,
                Port = serverPort
            };

            _TcpClient.KeepAliveInterval = 2000; 
            _TcpClient.KeepAliveTime     = 1000;    

            // 订阅连接和断开事件
            _TcpClient.OnClose   += _TcpClient_OnClose;
            _TcpClient.OnConnect += _TcpClient_OnConnect;
            _TcpClient.OnReceive += _TcpClient_OnReceive;

            // 消息中心
            _messageCenter = new MessageProcessCenter();
            _messageCenter.SendDataToServerEvent += _messageCenter_SendDataToServerEvent;
        }

        #region 消息中心的数据发送请求
        private void _messageCenter_SendDataToServerEvent(object sender, MessageCenterEventArgs e)
        {
            SendData(e.Data);
        }
        #endregion

        // 连接到服务器
        public void Connect()
        {
            Task.Run(() => StartConnection(_cts.Token));
        }

        // 停止客户端
        public void Stop()
        {
            _cts.Cancel();
            _TcpClient.Stop();
            LogHelper.Log.LogTrace(LogName, "客户端已停止");
        }

        // 启动连接
        private async Task StartConnection(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!_isConnected)
                {
                    LogHelper.Log.LogTrace(LogName, "正在尝试连接服务器...");
                    if (_TcpClient.Connect())
                    {
                        LogHelper.Log.LogTrace(LogName, "连接成功！");
                        _isConnected = true;

                        // 发送连接注册请求包
                        JObject register = new JObject();
                        register["clientName"] = App.ClientName;
                        register["clientId"]   = App.ClientId;
                        string registerData = NetData.Return(true, Cmd.Connect, "连接注册", register.ToString(Formatting.None));
                        SendData(registerData);
                        App.IsConnectServer = true;
                    }
                    else
                    {
                        LogHelper.Log.LogTrace(LogName, "连接失败，等待重连...");
                    }
                }

                await Task.Delay(_reconnectDelay, token);
            }
            LogHelper.Log.LogTrace(LogName, "心跳线程退出...");
        }

        // 心跳发送和监控任务
        private async Task MonitorHeartbeat(CancellationToken token)
        {
            while (_isConnected && !token.IsCancellationRequested)
            {
                // 检测心跳超时
                if (DateTime.Now - _lastHeartbeatReceived > _heartbeatTimeout)
                {
                    LogHelper.Log.LogTrace(LogName, "心跳超时，准备重连...");
                    _isConnected = false;
                    _TcpClient.Stop(); // 断开当前连接
                    await Task.Delay(_reconnectDelay, token); // 等待一段时间后重连
                    await StartConnection(token); // 重连
                }

                // 等待心跳间隔
                await Task.Delay(_heartbeatInterval, token);
            }
            LogHelper.Log.LogTrace(LogName,"重连线程退出...");
        }

        private HandleResult _TcpClient_OnReceive(IClient sender, byte[] data)
        {
            ReceivedData += Encoding.UTF8.GetString(data);
            LogHelper.Log.LogTrace(LogName, $"收到服务器的数据: {ReceivedData}");
            if (!ReceivedData.EndsWith("#"))
            {
                LogHelper.Log.LogDebug("数据包尾不是#,本次数据稍后处理");
                return HandleResult.Ignore;
            }

            // 每个数据以#结尾
            string[] messages = ReceivedData.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            ReceivedData = "";
            foreach (var message in messages)
            {
                if (string.IsNullOrWhiteSpace(message)) 
                    continue;

                NetData NetData = NetData.Parser(message);
                if(null == NetData)
                {
                    continue;
                }
                HandleNetData(NetData);
            }
            return HandleResult.Ok;
        }

        private void HandleNetData(NetData NetData)
        {
            switch (NetData.cmd)
            {
                case "Connect":
                    HandleConnect(NetData);
                    break;
                case "Heartbeat":
                    UpdateHeartbeat();
                    break;
                default:
                    {
                        if (Application.Current.Dispatcher.CheckAccess())
                        {
                            // 其他消息给消息中心处理
                            _messageCenter.ProcessServerMessage(NetData);
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _messageCenter.ProcessServerMessage(NetData);
                            });
                        }
                    }
                    break;
            }
        }

        #region 处理连接注册请求
        private void HandleConnect(NetData NetData)
        {
            if (NetData.success)
            {
                LogHelper.Log.LogTrace(LogName, "与服务端连接注册成功, 心跳线程启动!");
                // 更新最后心跳时间
                _lastHeartbeatReceived = DateTime.Now;

                // 启动心跳发送和监控
                //_ = MonitorHeartbeat(_cts.Token);

                // 如果当前正在进行实验,向客户端推送客户登录信息
                if (App.CurrentUser != null)
                {
                    SendMessage(true, Cmd.UserLogin, "用户连接登录", JObject.FromObject(App.CurrentUser).ToString(Formatting.None));
                }
            }
            else
            {
                LogHelper.Log.LogError("连接注册失败，停止客户端连接 :" + NetData.msg);
                // 连接注册失败，停止客户端
                _cts.Cancel();    // 取消连接任务
                _TcpClient.Stop(); // 停止客户端
                _isConnected = false;
                Message.ShowError("连接注册", NetData.msg);
            }
        }
        #endregion

        #region 处理心跳请求
        private void UpdateHeartbeat()
        {
            // 发送心跳包
            SendHeartbeat();
            // 更新最后收到心跳的时间
            _lastHeartbeatReceived = DateTime.Now;
            LogHelper.Log.LogTrace("Heartbeat", "接收到服务端心跳信息,并回应");
        }
        #endregion

        #region socket 回调方法
        private HandleResult _TcpClient_OnConnect(IClient sender)
        {
            LogHelper.Log.LogTrace(LogName, "已连接到服务器");
            _isConnected = true;
            return HandleResult.Ok;
        }

        private HandleResult _TcpClient_OnClose(IClient sender, SocketOperation socketOperation, int errorCode)
        {
            LogHelper.Log.LogTrace(LogName, "连接已关闭");
            _isConnected = false;
            App.IsConnectServer = false;
            return HandleResult.Ok;
        }

        // 发送心跳包
        private void SendHeartbeat()
        {
            string heartbeatMessage = NetData.Return(true, Cmd.Heartbeat, "定时心跳(客户端回应)", $"{App.ClientName},{App.ClientId}");
            if (SendData(heartbeatMessage))
            {
                LogHelper.Log.LogTrace(LogName, "发送心跳包成功V1.0.1");
            }
            else
            {
                LogHelper.Log.LogTrace(LogName, "发送心跳包失败V1.0.1");
                _isConnected = false; // 发送失败，标记为断开连接
            }
        }
        #endregion

        #region 发送消息到服务端
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(bool success, Cmd cmd, string message = "", string data = "") 
        {
            if (_isConnected)
            {
                SendData(NetData.Return(success, cmd, message, data));
            }
        }
        #endregion

        #region 发送数据
        private bool SendData(string data)
        {
            if (_isConnected)
            {
                byte[] dataSend = Encoding.UTF8.GetBytes(data);
                LogHelper.Log.LogTrace(LogName, string.Format("向服务端发送数据:{0}", data));
                return _TcpClient.Send(dataSend, dataSend.Length);
            }
            return false;
        }
        #endregion

    }
}
