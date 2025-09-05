using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Threading;
using MultiDevice.DB;


// 网络模块封装

namespace MultiDevice.Net
{
    public class NetHelper
    {
        public string ServerIp       = "127.0.0.1";
        private bool  findSuccess    = false;
        private string _serverAddess = "";
        private string LogName    = "Net";
        public NetClient _SocketClient = null;

        private static readonly object _logLock = new object();
        private static NetHelper _instance = null;

        public static NetHelper Net
        {
            get
            {
                if (_instance == null)
                {
                    lock (_logLock)
                    {
                        _instance = new NetHelper();
                    }
                }
                return _instance;
            }
        }

        #region 服务端搜索实现
        /// <summary>
        /// 自动搜索中心服务端
        /// </summary>
        public async Task searchCenterServer()
        {
            await Task.Run(() =>
            {
                LogHelper.Log.LogTrace(LogName, "启动服务端搜索线程!");
                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, 11000);
                    var discoverMessage = Encoding.UTF8.GetBytes("DISCOVER");
                    sock.Blocking = false;
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                    byte[] buffer = new byte[65536];

                    IPEndPoint localAddr = new IPEndPoint(IPAddress.Any, 9999);
                    EndPoint endPoint = localAddr;

                    while (!findSuccess)
                    {
                        try
                        {
                            int nRet = sock.SendTo(discoverMessage, broadcastEndpoint);
                            LogHelper.Log.LogTrace(LogName, "搜索线程发搜索请求....");
                            Thread.Sleep(500);
                            int bytesRead = sock.ReceiveFrom(buffer, ref endPoint);
                            if (bytesRead > 0)
                            {
                                _serverAddess = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                LogHelper.Log.LogTrace(LogName, string.Format("搜索到服务端 :{0}", _serverAddess));
                                // 标记为成功
                                findSuccess = true; 
                                break;
                            }
                        }
                        catch (SocketException)
                        {
                            LogHelper.Log.LogTrace(LogName, "未搜索到服务端");
                            Thread.Sleep(1000);
                        }
                    }
                }
            });

            LogHelper.Log.LogTrace(LogName, "搜索到服务端,搜索线程退出...");
        }
        #endregion

        #region 启动连接到服务端
        private List<IPAddress> getAllServerAddress()
        {
            List<IPAddress> iPAddresses = new List<IPAddress>();
            string HostName = Dns.GetHostName();
            IEnumerable<IPAddress> availableIp = Dns.GetHostAddresses(HostName)
                .Where(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

            if (availableIp.Count() == 0)
            {
                throw new InvalidDataException("本机没有局域网IP");
            }

            foreach (IPAddress ip in availableIp)
            {
                iPAddresses.Add(ip);
                LogHelper.Log.LogDebug(string.Format("获取到本机ip:{0}", ip.ToString()));
            }

            return iPAddresses;
        }


        public void closeClient()
        {
            if(null != _SocketClient)
                _SocketClient.Stop();
        }

        public async Task connectToServer()
        {
            try
            {
                if (SQLiteDBService.DB.ReadSettings("App", "IsAutoSearchServer") == "1")
                {
                    LogHelper.Log.LogDebug("启动服务端自动搜索");
                    // 等待搜索完成
                    await searchCenterServer();
                }
                else
                {
                    LogHelper.Log.LogDebug("读取配置服务端信息");
                    findSuccess = true;
                    JObject jsonParam = new JObject();
                    jsonParam["ip"] = SQLiteDBService.DB.ReadSettings("App", "ServerAddress");
                    jsonParam["port"] = 9991;
                    _serverAddess = jsonParam.ToString();
                }

                // 在此处继续进行连接操作
                if (findSuccess)
                {
                    JObject addressMsg = JObject.Parse(_serverAddess);
                    string strSeverIp = addressMsg["ip"].ToString();
                    ushort serverPort = ushort.Parse(addressMsg["port"].ToString());
                    string clientName = SQLiteDBService.DB.ReadSettings("App", "ClientName");
                    string clientId   = SQLiteDBService.DB.ReadSettings("App", "ClientId");
                    if (string.IsNullOrEmpty(clientName))
                    {
                        clientId   = "A";
                        clientName = getAllServerAddress().First().ToString();
                    }
                    ServerIp = strSeverIp;
                    // 执行连接逻辑
                    LogHelper.Log.LogTrace(LogName, string.Format("开始连接到服务端:{0}:{1}", strSeverIp, serverPort));
                    _SocketClient = new NetClient(strSeverIp, serverPort);
                    _SocketClient.Connect();
                    App.IsConnectServer = true;
                }
                else
                {
                    App.IsConnectServer = false;
                    LogHelper.Log.LogTrace(LogName, "未找到服务端，无法连接。");
                }
            }
            catch(Exception ex)
            {
                App.IsConnectServer = false;
                LogHelper.Log.LogTrace(LogName, $"服务器连接发生异常{ex.ToString()}");
            }
        }
        #endregion

        #region 发送数据到服务端
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void sendMessageToSever(bool success, Cmd cmd, string message = "", string data = "")
        {
            if(null == _SocketClient)
            {
                return;
            }

            _SocketClient.SendMessage(success, cmd, message, data);
        }
        #endregion
    }
}
