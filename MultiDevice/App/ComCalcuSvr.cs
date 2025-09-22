using MultiDevice.DB;
using MultiDevice.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MultiDevice
{
    class ComCalcusvr
    {
        private List<Customer> clientList = null;
        private DispatcherTimer timer = null;
        private List<indices_enu> All_indeces_ordered = null;
        public delegate void FailEvent(object sender, FailArgs e);
        public event FailEvent paradigm_channloc_error;
        public event FailEvent paradigm_ref_error;
        public event FailEvent SvrListenError;
        public event FailEvent client_RcvFail;
        public event FailEvent client_Commu_Fail;

        public bool CalSvrStopped = false;
        public GameConfigModel gameConfig = null;
        private UserProfile theUserProfile;
        public string logDirPath = "";
        public UserProfile TheUserProfile
        {
            set
            {
                theUserProfile = value;
            }
        }
        private ComSvr comDevice   = null;
        private Socket serveSocket = null;
        private Dictionary<indices_enu, object> indice_dict = new Dictionary<indices_enu, object>();
        public ComCalcusvr(ComSvr comDevice)
        {
            All_indeces_ordered = new List<indices_enu>();
            clientList = new List<Customer>();
            this.comDevice = comDevice;
            this.comDevice.StopProvidingData += Svr_StopProvidingData;
            this.comDevice.Got200ms          += Svr_Got200ms;
            CalSvrStopped = false;
        }

        #region 日志记录
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        public void writeLogs(string data, string fileName = "logs")
        {
            using (StreamWriter sw = new StreamWriter($"{logDirPath}\\{fileName}.txt", true, System.Text.Encoding.Default))
            {
                sw.WriteLine($"{DateTime.Now.ToString("yyyy_MM_d_HH_mm_ss_fff")}:{data}");
                sw.Flush();
                sw.Dispose();
            }
        }

        #endregion

        #region 服端启动
        public void startServer(int serverPort, GameConfigModel gameConfig)
        {
            // 创建套接字  
            IPEndPoint ipe = new IPEndPoint(IPAddress.Any, serverPort);
            serveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 绑定端口和IP  
            serveSocket.Bind(ipe);
            // 设置监听  
            serveSocket.Listen(10);
            // 开始监听
            Thread listenThread = new Thread(ListenClientConnect);
            listenThread.IsBackground = true;
            listenThread.Start();
            LogHelper.Log.LogDebug("游戏服务端监听线程启动完毕");

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += new EventHandler(ScheduledTask);
            timer.IsEnabled = true;

            // 日志记录
            string impedances = comDevice.impedanceValue[0].ToString("F2") + "_" +
                                   comDevice.impedanceValue[1].ToString("F2") + "_" +
                                   comDevice.impedanceValue[2].ToString("F2") + "_" +
                                   comDevice.impedanceValue[3].ToString("F2") + "_" +
                                   comDevice.impedanceValue[4].ToString("F2") + "_" +
                                   comDevice.impedanceValue[5].ToString("F2") + "_" +
                                   comDevice.impedanceValue[6].ToString("F2") + "_" +
                                   comDevice.impedanceValue[7].ToString("F2") + "_" +
                                   comDevice.impedanceValue[8].ToString("F2");
            LogHelper.Log.LogDebug($"Impedances :{impedances}");

            writeLogs("CalcuSvr PowerOn");
            writeLogs($"impedance:{impedances}");
            LogHelper.Log.LogDebug("------------- select channel ------------");
            for (int nIndex = 0; nIndex < comDevice.signal_labels.Length; nIndex++)
            {
                string channel = nIndex + "_" + comDevice.signal_labels[nIndex].Replace('-', '_');
                LogHelper.Log.LogDebug($"{channel}");
                writeLogs($"chan_loc:{channel}");
            }
            LogHelper.Log.LogDebug("------------- select channel ------------");
            this.gameConfig = gameConfig;
        }
        #endregion

        #region 定时任务心跳检测
        private void ScheduledTask(object sender, EventArgs e)
        {
            List<indices_enu> tmp_all_indices = new List<indices_enu>();
            lock (clientList)
            {
                List<Customer> tmp_list = new List<Customer>();
                for (int i = 0; i < clientList.Count; i++)
                {
                    if (!clientList[i].SocketIsAlive())
                    {
                        LogHelper.Log.LogDebug(clientList[i].SocketIP + "dead");
                        if (!tmp_list.Contains(clientList[i]))
                            tmp_list.Add(clientList[i]);
                    }
                    else
                    {
                        LogHelper.Log.LogDebug(clientList[i].SocketIP + "alive");
                        if (clientList[i].indices.Count > 0)
                        {
                            foreach (var item in clientList[i].indices)
                            {
                                if (!tmp_all_indices.Contains(item))
                                    tmp_all_indices.Add(item);
                            }
                        }
                    }
                }
                for (int i = 0; i < tmp_list.Count; i++)
                {
                    if (clientList.Contains(tmp_list[i]))
                    {
                        LogHelper.Log.LogDebug("scheduled_task to remove");
                        clientList.Remove(tmp_list[i]);
                        LogHelper.Log.LogDebug("scheduled_task removed");
                    }
                }
            }

            All_indeces_ordered = tmp_all_indices;
            var all_indice_enu = indice_dict.Keys.ToList<indices_enu>();
            List<indices_enu> to_delete = new List<indices_enu>();
            for (int i = 0; i < all_indice_enu.Count; i++)
            {
                if (!All_indeces_ordered.Contains(all_indice_enu[i]))
                    to_delete.Add(all_indice_enu[i]);
            }
            for (int i = 0; i < to_delete.Count; i++)
            {
                indice_dict.Remove(to_delete[i]);
            }

            string strLog = "";
            foreach (var item in All_indeces_ordered)
            {
                strLog += " " + item.ToString();
            }
            LogHelper.Log.LogDebug("All_indeces_ordered:" + strLog);
        }
        #endregion

        #region 当脑电设备端通信出错时调用
        private void Svr_StopProvidingData(object sender, myEventArgs e)
        {
            stopServer();
        }
        #endregion

        #region Svr_Got200ms 计算脑电数据
        private void Svr_Got200ms(object sender, myEventArgs e)
        {
            // 逐个检查All_indeces_ordered中的需要计算的indices
            if (All_indeces_ordered.Contains(indices_enu.EMG_ratio))
            {
                if (indice_dict.ContainsKey(indices_enu.EMG_ratio))
                {
                    Indice_EMGPower ppp = (Indice_EMGPower)indice_dict[indices_enu.EMG_ratio];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }

            if (All_indeces_ordered.Contains(indices_enu.absolute_power))
            {
                if (indice_dict.ContainsKey(indices_enu.absolute_power))
                {
                    Indice_absolutePower ppp = (Indice_absolutePower)indice_dict[indices_enu.absolute_power];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.relative_power))
            {
                if (indice_dict.ContainsKey(indices_enu.relative_power))
                {
                    Indice_RelativePower ppp = (Indice_RelativePower)indice_dict[indices_enu.relative_power];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.power_ratio))
            {
                if (indice_dict.ContainsKey(indices_enu.power_ratio))
                {
                    Indice_PowerRatio ppp = (Indice_PowerRatio)indice_dict[indices_enu.power_ratio];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.indiv_alpha))
            {
                if (indice_dict.ContainsKey(indices_enu.indiv_alpha))
                {
                    Indice_IndivAlpha ppp = (Indice_IndivAlpha)indice_dict[indices_enu.indiv_alpha];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.the_alpha))
            {
                if (indice_dict.ContainsKey(indices_enu.the_alpha))
                {
                    Indice_Alpha ppp = (Indice_Alpha)indice_dict[indices_enu.the_alpha];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.artifacts))
            {
                if (indice_dict.ContainsKey(indices_enu.artifacts))
                {
                    Indice_Artifact ppp = (Indice_Artifact)indice_dict[indices_enu.artifacts];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
            if (All_indeces_ordered.Contains(indices_enu.sham_power))
            {
                if (indice_dict.ContainsKey(indices_enu.sham_power))
                {
                    Indice_ShamPower ppp = (Indice_ShamPower)indice_dict[indices_enu.sham_power];
                    ppp.Data200ms = comDevice.Data200ms;
                }
            }
        }
        #endregion

        #region 服务关闭
        public void stopServer()
        {
            timer.IsEnabled = false;
            try
            {
                comDevice.Got200ms          -= Svr_Got200ms;
                comDevice.StopProvidingData -= Svr_StopProvidingData;
                serveSocket.Close();
            }
            catch (NullReferenceException e)
            {
                LogHelper.Log.LogError($"服务关闭发生异常:{e.ToString()}");
            }

            CalSvrStopped = true;
            foreach (var item in clientList)
            {
                try
                {
                    if (item.socket.Connected)
                    {
                        item.socket.Shutdown(SocketShutdown.Both);
                        item.socket.Close(1);
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Log.LogError($"客户端关闭发生异常:{e.ToString()}");
                }
            }

            writeLogs("CalcuSvr shuttdown");

            clientList.Clear();
            CalSvrStopped = true;
            LogHelper.Log.LogDebug("游戏通信服务serverSocket关闭");
        }
        #endregion

        #region 客户端监听线程(接收游戏端的连接)

        void ListenClientConnect()
        {
            while (true)
            {
                try
                {
                    Socket client        = serveSocket.Accept();
                    IPEndPoint clientEndPoint = client.RemoteEndPoint as IPEndPoint;
                    if (clientEndPoint != null)
                    {
                        string clientIp = clientEndPoint.Address.ToString();
                        int clientPort  = clientEndPoint.Port;
                        LogHelper.Log.LogDebug($"接收到客户端连接: IP = {clientIp}, 端口 = {clientPort}");
                    }

                    Thread receiveThread = new Thread(ToRcvData);
                    receiveThread.IsBackground = true;
                    receiveThread.Start(client);

                    Customer clienter = new Customer(client, new List<indices_enu>());
                    clientList.Add(clienter);
                    LogHelper.Log.LogDebug("新的客户端连接添加到客户端列表");

                    writeLogs($"customer_elapse:{clienter.SocketIP}", $"{clienter.SocketIP}_elapse");
                }
                catch (SocketException e)
                {
                    if (!CalSvrStopped)
                        (new Thread(() =>
                        SvrListenError?.Invoke(this, new FailArgs(e, "游戏服务端监听线程发生异常")))).Start();

                    LogHelper.Log.LogError($"游戏服务端监听线程运行发生异常:{e.ToString()}");
                    closeGameServer();
                    break;
                }
            }
            LogHelper.Log.LogDebug("客户端监听线程退出.");
        }
        #endregion

        #region 客户端数据接收线程
        void ToRcvData(object obj)
        {
            try
            {
                Socket client = (Socket)obj;
                byte[] data   = new byte[1024];
                int length    = 0;

                try
                {
                    while (true)
                    {
                        length = client.Receive(data);
                        if (length <= 0)
                        {
                            LogHelper.Log.LogDebug("远程客户端已经关闭,数据接收线程退出");
                            break;
                        }
                        // 处理客户端数据
                        HandleClientData(client, data, length);
                    }
                }
                catch (SocketException e)
                {
                    LogHelper.Log.LogError($"客户端数据接收线程发生异常1:{e.ToString()}");
                    lock (clientList)
                    {
                        List<Customer> toRemove = new List<Customer>();
                        for (int i = 0; i < clientList.Count; i++)
                        {
                            if (clientList[i].socket == client)
                            {
                                if (!CalSvrStopped)
                                    client_RcvFail?.Invoke(this, new FailArgs(e, clientList[i].SocketIP + "客户端连接已关闭,将移除服务端队列"));
                                toRemove.Add(clientList[i]);
                                break;
                            }
                        }
                        client = null;
                        for (int i = 0; i < toRemove.Count; i++)
                        {
                            if (clientList.Contains(toRemove[i]))
                            {
                                toRemove[i].socket.Close();
                                clientList.Remove(toRemove[i]);
                            }
                        }
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                LogHelper.Log.LogError($"客户端数据接收线程发生异常2:{e.ToString()}");
            }
            GC.Collect();
        }
        #endregion

        #region 处理好的数据发送给游戏端
        private void Fea_CalculationDone(object sender, MyEventArgs_indice e)
        {
            try
            {
                var indice_code = e.indice_code;
                if (indice_dict.Keys.Contains(indice_code))
                {
                    Console.WriteLine("CalculationDone done");

                    List<Customer> errorClientList = new List<Customer>();
                    IndiceToBytes tmp = (IndiceToBytes)indice_dict[indice_code];
                    byte[] indice_bytes = tmp.toBytes();
                    int data_len = indice_bytes.Length;
                    byte[] toSe  = BitConverter.GetBytes(data_len);
                    //pack and send
                    byte[] sent_pack = new byte[indice_bytes.Length + 8];
                    sent_pack[0] = 0x55;
                    sent_pack[1] = 0x00;
                    sent_pack[2] = 22;
                    sent_pack[3] = toSe[0];
                    sent_pack[4] = toSe[1];
                    sent_pack[5] = toSe[2];
                    sent_pack[6] = toSe[3];
                    Array.Copy(indice_bytes, 0, sent_pack, 7, indice_bytes.Length);
                    int toCheck = 0;
                    for (int i = 0; i < sent_pack.Length - 1; i++)
                    {
                        toCheck = toCheck + sent_pack[i];
                    }

                    sent_pack[sent_pack.Length - 1] = (byte)toCheck;
                    lock (clientList)
                    {
                        bool thisIndiceActive = false;
                        foreach (var item in clientList)
                        {
                            if (item.indices.Contains(indice_code))
                            {
                                try
                                {
                                    item.socket.Send(sent_pack);
                                    thisIndiceActive = true;
                                }
                                catch (Exception ex)
                                {
                                    if (!CalSvrStopped)
                                    {
                                        if (item.indices.Contains(indice_code))
                                        {
                                            item.indices.Remove(indice_code);
                                        }

                                        closeGameServer();
                                        client_Commu_Fail?.Invoke(this, new FailArgs(ex, item.SocketIP + "_" + indice_code));
                                        LogHelper.Log.LogError($"Fea_CalculationDone 执行发生异常:{ex.ToString()}");
                                        errorClientList.Add(item);
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < errorClientList.Count; i++)
                        {
                            if (clientList.Contains(errorClientList[i]))
                            {
                                LogHelper.Log.LogDebug("scheduled_task to remove");
                                errorClientList[i].socket.Close();
                                clientList.Remove(errorClientList[i]);
                                LogHelper.Log.LogDebug("scheduled_task removed");
                            }
                        }
                        if (!thisIndiceActive)
                        {
                            indice_dict.Remove(indice_code);
                        }
                    }
                }
            }
            catch(Exception ex)
            {

                LogHelper.Log.LogError($"脑电数据推送到游戏端发生异常:{ex.ToString()}");
            }
        }
        #endregion

        #region 解析客户端数据包
        bool HandleClientData(Socket client, byte[] buf, int length)
        {
            bool containData = false;
            int  toCheck   = 0;
            byte CheckSum  = 0x00;
            int nIndex = 0;

            while (nIndex < length)
            {
                // 找到数据包头
                while (nIndex < length && buf[nIndex] != 0x55)
                {
                    nIndex++;
                }

                int DataLength = 0;
                if (nIndex + 7 >= length)
                {
                    nIndex++;
                    continue;
                }

                byte[] cacheBytes = new byte[] { buf[nIndex + 3], buf[nIndex + 4], buf[nIndex + 5], buf[nIndex + 6] };
                DataLength = BitConverter.ToInt32(cacheBytes, 0);
                if (DataLength < 0)
                {
                    nIndex++;
                    continue;
                }

                if (nIndex + DataLength + 7 >= length)
                {
                    nIndex++;
                    continue;
                }

                // 计算校验和
                CheckSum = 0x00; 
                toCheck = 0;
                for (int i  = 0; i < DataLength + 7; i++)
                {
                    toCheck = toCheck + buf[nIndex + i];
                }
                
                CheckSum = (byte)toCheck;
                if (CheckSum == buf[nIndex + 7 + DataLength])
                {
                    // 校验成功
                    byte[] validPack = new byte[DataLength + 8];
                    for (int i = 0; i < DataLength + 8; i++)
                    {
                        validPack[i] = buf[nIndex + i];
                    }
                    // 解析到一个完整的数据包
                    ParsePack(client, validPack, DataLength);
                    containData = true;
                    nIndex = nIndex + DataLength + 8;
                }
                else
                {
                    // 校验失败
                    nIndex++;
                }
            }
            return containData;
        }
        #endregion

        #region 公共方法
        public int getValue(byte a)
        {
            int high = 0;
            int tmp = 1;
            for (int i = 0; i < 7; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }

            if ((a >> 7 & 0x01) > 0)
            {
                return -(256 - high);

            }
            else
            {
                return high;
            }
        }
        public int getUValue(byte a)
        {
            int high = 0;
            int tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }

            return tmp;
        }
        #endregion

        #region 处理游戏端的相关Cmd指令
        private void ParsePack(Socket client, byte[] pack, int datalength)
        {
            byte cmd = pack[2];
            LogHelper.Log.LogDebug($"解析到游戏端Cmd :{cmd}");
            switch (cmd)
            {
                //添加订阅事件
                case 23:
                    {
                        #region 根据游戏端的要求订阅不同的数据处理器
                        indices_enu indices_code = (indices_enu) BitConverter.ToInt16(pack, 7);
                        LogHelper.Log.LogDebug($"游戏端请求数据处理器 : {indices_code}");

                        writeLogs($"indice_subscri:{(client.RemoteEndPoint as IPEndPoint).Address.ToString()}:{indices_code}");

                        List<int> chan_invol_EEG = new List<int>();
                        List<int> chan_invol_EMG = new List<int>();
                        List<int> chan_invol_EKG = new List<int>();
                        for (int i = 0; i < comDevice.signal_labels.Length; i++)
                        {
                            if (!comDevice.signal_labels[i].ToLower().Contains("na") && 
                                !comDevice.signal_labels[i].ToLower().Contains("d") &&
                                !comDevice.signal_labels[i].ToLower().Contains("k"))
                                chan_invol_EEG.Add(i);
                        }

                        for (int i = 0; i < comDevice.signal_labels.Length; i++)
                        {
                            if (comDevice.signal_labels[i].ToLower().Contains("d"))
                                chan_invol_EMG.Add(i);
                        }

                        for (int i = 0; i < comDevice.signal_labels.Length; i++)
                        {
                            if (comDevice.signal_labels[i].ToLower().Contains("k"))
                                chan_invol_EKG.Add(i);
                        }

                        if (!indice_dict.ContainsKey(indices_code))
                        {
                            switch (indices_code)
                            {
                                case indices_enu.EMG_ratio:
                                    {
                                        Indice_EMGPower power = new Indice_EMGPower(chan_invol_EMG);
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_EMGPower");
                                    }
                                    break;
                                case indices_enu.absolute_power:
                                    {
                                        Indice_absolutePower power = new Indice_absolutePower(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_absolutePower");
                                    }
                                    break; 
                                case indices_enu.relative_power:
                                    {
                                        Indice_RelativePower power = new Indice_RelativePower(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_RelativePower");
                                    }
                                    break;
                                case indices_enu.power_ratio:
                                    {
                                        Indice_PowerRatio power = new Indice_PowerRatio(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_PowerRatio");
                                    }
                                    break;
                                case indices_enu.indiv_alpha:
                                    {
                                        Indice_IndivAlpha power = new Indice_IndivAlpha(chan_invol_EEG);
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_IndivAlpha");
                                    }
                                    break;
                                case indices_enu.the_alpha:
                                    {
                                        Indice_Alpha power = new Indice_Alpha(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_Alpha");
                                    }
                                    break;
                                case indices_enu.artifacts:
                                    {
                                        Indice_Artifact power = new Indice_Artifact(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_Artifact");
                                    }
                                    break;
                                case indices_enu.sham_power:
                                    {
                                        Indice_ShamPower power = new Indice_ShamPower(chan_invol_EEG);
                                        power.IAPF = theUserProfile.IAPF;
                                        power.IAF_high = theUserProfile.IAF_high;
                                        power.IAF_low = theUserProfile.IAF_low;
                                        power.CalculationDone += Fea_CalculationDone;
                                        indice_dict.Add(indices_code, power);
                                        LogHelper.Log.LogDebug($"数据处理器创建:Indice_ShamPower");
                                        writeLogs($"baseFreq:{power.baseFreq.ToString("F3")}:shamPower");
                                    }
                                    break;
                            }
                        }
                          
                        foreach (var item in clientList)
                        {
                            if (item.socket == client)
                            {
                                item.indices.Add(indices_code);
                                break;
                            }
                        }
                        #endregion
                    }
                    break;

                //取消订阅事件
                case 24:
                    {
                        #region 游戏端请求删除订阅的数据处理器
                        foreach (var item in clientList)
                        {
                            if (item.socket == client)
                            {
                                indices_enu indices_code = (indices_enu)BitConverter.ToInt16(pack, 7);
                                if (item.indices.Contains(indices_code))
                                {
                                    item.indices.Remove(indices_code);
                                    writeLogs($"indice_unsubscri:{item.SocketIP}:{indices_code}");
                                    LogHelper.Log.LogDebug($"游戏端请求删除数据处理器 :{item.SocketIP},{indices_code}");
                                }        
                                break;
                            }
                        }
                        #endregion
                    }
                    break;
                case 36:
                    {
                        #region 数据序号更新？
                        byte[] dataCahce = new byte[datalength];
                        for (int i = 0; i < datalength; i++)
                        {
                            dataCahce[i] = pack[7 + i];
                        }

                        Customer tmp_cus = null;
                        bool hasExisted  = false;
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                tmp_cus = clientList[nIndex];
                                hasExisted = true;
                                break;
                            }
                        }

                        int serialNum = BitConverter.ToInt32(dataCahce, 0);
                        if (hasExisted)
                        {
                            TimeSpan timrSpan = DateTime.Now - tmp_cus.test_fetch[serialNum];
                            tmp_cus.TimeElapsed[serialNum] = timrSpan.TotalMilliseconds;
                            LogHelper.Log.LogDebug($"接收到游戏端Cmd=36数据:{serialNum}");
                        }
                        #endregion
                    }
                    break;
                case 34:
                    {
                        #region Neurofeedback 消息回复
                        byte[] indice_bytes = Encoding.ASCII.GetBytes("Neurofeedback");
                        byte[] one = BitConverter.GetBytes(indice_bytes.Length);
                        byte[] sent_pack = new byte[indice_bytes.Length + 8];
                        sent_pack[0] = 0x55;
                        sent_pack[1] = 0x00;
                        sent_pack[2] = 33;
                        sent_pack[3] = one[0];
                        sent_pack[4] = one[1];
                        sent_pack[5] = one[2];
                        sent_pack[6] = one[3];
                        Array.Copy(indice_bytes, 0, sent_pack, 7, indice_bytes.Length);
                        int toCheck = 0;
                        for (int i = 0; i < sent_pack.Length - 1; i++)
                        {
                            toCheck = toCheck + sent_pack[i];
                        }
                        sent_pack[sent_pack.Length - 1] = (byte)toCheck;
                        try
                        {
                            client.Send(sent_pack);
                            LogHelper.Log.LogDebug($"接收到游戏端Cmd=34消息,服务端回复消息:Neurofeedback");
                        }
                        catch (SocketException e)
                        {
                            if (!CalSvrStopped)
                                client_Commu_Fail?.Invoke(this, new FailArgs(e, $"回复游戏端Cmd=34消息发生异常:{e.ToString()}"));
                        }
                        #endregion
                    }
                    break;
                case 32:
                    {
                        #region Cmd32 消息回复
                        byte[] one = BitConverter.GetBytes(0);
                        byte[] sent_pack = new byte[8];
                        sent_pack[0] = 0x55;
                        sent_pack[1] = 0x00;
                        sent_pack[2] = 31;
                        sent_pack[3] = one[0];
                        sent_pack[4] = one[1];
                        sent_pack[5] = one[2];
                        sent_pack[6] = one[3];
                        int toCheck = 0;
                        for (int i = 0; i < sent_pack.Length - 1; i++)
                        {
                            toCheck = toCheck + sent_pack[i];
                        }
                        sent_pack[sent_pack.Length - 1] = (byte)toCheck;
                        try
                        {
                            client.Send(sent_pack);
                            LogHelper.Log.LogDebug("回复游戏端Cmd=32消息");
                        }
                        catch (SocketException e)
                        {
                            if (!CalSvrStopped)
                                client_Commu_Fail?.Invoke(this, new FailArgs(e, $"接收到游戏端Cmd=32消息回复发生异常:e.ToString()"));
                        }
                        #endregion
                    }
                    break;
                case 38:
                    {
                        #region 游戏端回复游戏参数
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                byte[] dataPack = new byte[datalength];
                                for (int i = 0; i < datalength; i++)
                                {
                                    dataPack[i] = pack[7 + i];
                                }

                                writeLogs($"customer_message:{clientList[nIndex].SocketIP}:{Encoding.Default.GetString(dataPack)}", $"{clientList[nIndex].SocketIP}_message_sent");
                                break;
                            }
                        }
                        #endregion
                    }
                    break;
                case 30:
                    {
                    }
                    break;
                case 40:
                    {
                    }
                    break;
                case 42:
                    {
                        #region 游戏选请求通道配置信息
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                string clientIp  = clientList[nIndex].SocketIP;
                                string chann_str = "";
                                for (int i = 0; i < comDevice.signal_labels.Length; i++)
                                {
                                    if (comDevice.signal_labels[i].Contains("Na"))
                                    {
                                        chann_str = "Na";
                                    }
                                    else
                                    {
                                        chann_str = comDevice.signal_labels[i];
                                    }

                                    byte[] indice_bytes = new byte[chann_str.Length + 1];
                                    indice_bytes[0] = (byte)i;
                                    Array.Copy(Encoding.ASCII.GetBytes(chann_str), 0, indice_bytes, 1, chann_str.Length);
                                    byte[] one       = BitConverter.GetBytes(indice_bytes.Length);
                                    byte[] sent_pack = new byte[indice_bytes.Length + 8];
                                    sent_pack[0] = 0x55;
                                    sent_pack[1] = 0x00;
                                    sent_pack[2] = 41;
                                    sent_pack[3] = one[0];
                                    sent_pack[4] = one[1];
                                    sent_pack[5] = one[2];
                                    sent_pack[6] = one[3];

                                    Array.Copy(indice_bytes, 0, sent_pack, 7, indice_bytes.Length);
                                    int toCheck = 0;
                                    for (int ii = 0; ii < sent_pack.Length - 1; ii++)
                                    {
                                        toCheck = toCheck + sent_pack[ii];
                                    }
                                    sent_pack[sent_pack.Length - 1] = (byte)toCheck;

                                    try
                                    {
                                        client.Send(sent_pack);
                                        LogHelper.Log.LogDebug($"向游戏端发送通道配置数据:{chann_str}");
                                    }
                                    catch (SocketException e)
                                    {
                                        if (!CalSvrStopped)
                                            client_Commu_Fail?.Invoke(this, new FailArgs(e, clientIp + "向游戏端发送通道配置数据发生异常"));
                                    }
                                }

                                writeLogs($"customer_message:{clientList[nIndex].SocketIP}:client_requir_chanloc", $"{clientList[nIndex].SocketIP}_message_sent");
                                break;
                            }
                        }
                        #endregion
                    }
                    break;
                case 43:
                    {
                        #region 模式与所选通道不对应
                        //error from client, 模式与所选通道不对应
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                paradigm_channloc_error?.Invoke(this, new FailArgs(new ArgumentException(), $"{clientList[nIndex].SocketIP}:模式与所选通道不对应"));
                                LogHelper.Log.LogDebug("模式与所选通道不对应");
                            }
                        }
                        #endregion
                    }
                    break;
                case 44:
                    {
                        #region 游戏端请求参考模式
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                string theIP = clientList[nIndex].SocketIP;
                                byte[] one = BitConverter.GetBytes(1);
                                byte[] sent_pack = new byte[1 + 8];
                                sent_pack[0] = 0x55;
                                sent_pack[1] = 0x00;
                                sent_pack[2] = 45;
                                sent_pack[3] = one[0];
                                sent_pack[4] = one[1];
                                sent_pack[5] = one[2];
                                sent_pack[6] = one[3];
                                sent_pack[7] = (byte)comDevice.ReferenceMode;
                                int toCheck = 0;
                                for (int i = 0; i < sent_pack.Length - 1; i++)
                                {
                                    toCheck = toCheck + sent_pack[i];
                                }
                                sent_pack[sent_pack.Length - 1] = (byte)toCheck;
                                try
                                {
                                    client.Send(sent_pack);
                                    LogHelper.Log.LogDebug($"向游戏端发送参考模式:{comDevice.ReferenceMode}");
                                }
                                catch (SocketException e)
                                {
                                    if (!CalSvrStopped)
                                        client_Commu_Fail?.Invoke(this, new FailArgs(e, "向游戏端发送参考模式发生异常"));
                                }

                                writeLogs($"customer_message:{clientList[nIndex].SocketIP}:client_requir_ref", $"{clientList[nIndex].SocketIP}_message_sent");
                                break;
                            }
                        }
                        #endregion
                    }
                    break;
                case 46:
                    {
                        #region 采用模式与参考名称不对应
                        //error from client, 采用模式与参考名称不对应
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                paradigm_ref_error?.Invoke(this, new FailArgs(new ArgumentException(), $"{clientList[nIndex].SocketIP}:采用模式与参考名称不对应"));
                                LogHelper.Log.LogDebug("采用模式与参考名称不对应");
                            }
                        }
                        #endregion
                    }
                    break;
                case 28:
                    {
                        #region 游戏端请求连接关闭
                        try
                        {
                            lock (clientList)
                            {
                                List<Customer> tmp_list = new List<Customer>();
                                for (int iaia = 0; iaia < clientList.Count; iaia++)
                                {
                                    if (clientList[iaia].socket == client)
                                    {
                                        string theIP = clientList[iaia].SocketIP;
                                        clientList[iaia].indices.Clear();
                                        clientList.RemoveAt(iaia);
                                        client = null;
                                        tmp_list.Add(clientList[iaia]);
                                        break;
                                    }
                                }
                                for (int i = 0; i < tmp_list.Count; i++)
                                {
                                    if (clientList.Contains(tmp_list[i]))
                                        clientList.Remove(tmp_list[i]);
                                }
                                LogHelper.Log.LogDebug("游戏端请求连接退出");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Log.LogError($"Cmd=28 数据处理发生异常:{ex.ToString()}");
                        }
                        GC.Collect();
                        #endregion
                    }
                    break;
                case 90:
                    {
                        #region 向游戏端发送游戏参数
                        LogHelper.Log.LogDebug("接收到游戏端游戏参数请求指令,向游戏端发送游戏请求参数");
                        LogHelper.Log.LogDebug($"User : {gameConfig.UserInfo.TestNumber},{gameConfig.UserInfo.Name}");
                      
                        string strGameConfig = "";
                        strGameConfig += ($"Game:{gameConfig.Game}#");
                        strGameConfig += ($"ParadigmType:{gameConfig.paradigmSettings.paradigmType}#");
                        strGameConfig += ($"SessionTotal:{gameConfig.SessionTotal}#");
                        strGameConfig += ($"SessionNum:{gameConfig.SessionNum}#");
                        strGameConfig += ($"EpochCount:{gameConfig.EpochCount}#");
                        strGameConfig += ($"EpochTimes:{gameConfig.EpochTimes}#");
                        strGameConfig += ($"BreakTimes:{gameConfig.BreakTimes}");
                        LogHelper.Log.LogDebug($"GameConfig:{strGameConfig}");


                        byte[] dataBytes = Encoding.ASCII.GetBytes(strGameConfig);
                        byte[] one = BitConverter.GetBytes(dataBytes.Length);
                        byte[] sent_pack = new byte[dataBytes.Length + 8];
                        sent_pack[0] = 0x55;
                        sent_pack[1] = 0x00;
                        sent_pack[2] = 0x91;
                        sent_pack[3] = one[0];
                        sent_pack[4] = one[1];
                        sent_pack[5] = one[2];
                        sent_pack[6] = one[3];
                        Array.Copy(dataBytes, 0, sent_pack, 7, dataBytes.Length);
                        int toCheck = 0;
                        for (int i = 0; i < sent_pack.Length - 1; i++)
                        {
                            toCheck = toCheck + sent_pack[i];
                        }
                        sent_pack[sent_pack.Length - 1] = (byte)toCheck;
                        try
                        {
                            client.Send(sent_pack);
                        }
                        catch (SocketException e)
                        {
                            if (!CalSvrStopped)
                                client_Commu_Fail?.Invoke(this, new FailArgs(e, $"回复游戏端Cmd=90消息发生异常:{e.ToString()}"));
                        }
                        #endregion
                    }
                    break;
                case 0x88:
                    {
                        #region 实时游戏进度
                        for (int nIndex = 0; nIndex < clientList.Count; nIndex++)
                        {
                            if (clientList[nIndex].socket == client)
                            {
                                byte[] dataPack = new byte[datalength];
                                for (int i = 0; i < datalength; i++)
                                {
                                    dataPack[i] = pack[7 + i];
                                }

                            // 详尽日志：来源IP、长度、原始HEX和解码文本
                            try
                            {
                                string dataHex = BitConverter.ToString(dataPack);
                                LogHelper.Log.LogDebug($"[0x88] From={clientList[nIndex].SocketIP} Len={datalength} Hex={dataHex}");
                            }
                            catch { }

                            string data = Encoding.UTF8.GetString(dataPack);
                            LogHelper.Log.LogDebug($"[0x88] Text='{data}'");
                                if (data.Equals("GameOver"))
                                {
                                LogHelper.Log.LogInfo("[0x88] Received GameOver, trigger StopGameSever");
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (null != App.deviceMainWindow)
                                        {
                                            App.deviceMainWindow.StopGameSever(true);
                                        }
                                    });
                                    return;
                                }

                                string measurePowerValue = "-1";
                                string strMessage = data;
                                if (data.Contains("&"))
                                {
                                    strMessage        = data.Split('&')[0];
                                    measurePowerValue = data.Split('&')[1];
                                }

                            LogHelper.Log.LogDebug($"[0x88] Message='{strMessage}', Power='{measurePowerValue}'");

                                // 只记录游戏时间的数据
                                if (!strMessage.Contains("校准") && !strMessage.Contains("静息"))
                                {
                                    LogHelper.Log.LogTrace("PowerData", $"RealPower:{measurePowerValue}");
                                    // 实时记录Power值
                                    float powerValue = 0;
                                    // 取0.8-1为注意力集中时间
                                    if (float.TryParse(measurePowerValue, out powerValue))
                                    {
                                        if (powerValue >= 0.8)
                                        {
                                            App.CurrentUser.ValidTimes += 1;
                                        }
                                        App.CurrentUser.TotalPowerTimes += 1;
                                    }
                                }

                                // 转发推送到监控服务端
                                NetHelper.Net.sendMessageToSever(true, Cmd.GameProcess, strMessage);
                                break;
                            }
                        }
                        #endregion
                    }
                    break;
                case 0x89:
                    {
                        // StopGame();
                        LogHelper.Log.LogInfo("接收到游戏关闭信号,执行游戏关闭");
                    }
                    break;
                default: 
                    break;
            }
        }
        #endregion

        #region 游戏关闭
        public void closeGameServer()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (null == App.CurrentUser)
                {
                    return;
                }

                string userName = App.CurrentUser.Name;
                if (null != App.impedanceDetectWindow)
                {
                    App.impedanceDetectWindow.Close();
                    App.impedanceDetectWindow = null;
                }

                if (null != App.deviceMainWindow)
                {
                    App.deviceMainWindow.StopGameSever();
                }
            });
        }
        #endregion
    }
}
