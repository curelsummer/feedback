using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Globalization;
using System.Windows.Threading;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;
using MathNet.Filtering.FIR;
using BDF;
using System.Windows;
using System.Security.AccessControl;


namespace MultiDevice
{
    public class ComSvr
    {
        const int sampling_rate = 2000;
        OnlineFirFilter[] iirFilter = new OnlineFirFilter[9];

        public delegate void NetEvent(object sender, myEventArgs e);
        public delegate void FailEvent(object sender, FailArgs e);

        public delegate void DeviceError(string error);

        public const double amplitude_range = 7487.27164;

        const double LinearTimes = amplitude_range / 8388608;
        /// <summary>
        /// 收到数据
        /// </summary>
        //public event NetEvent rcvData;
        public event NetEvent GotOneSec;
        public event NetEvent Got200ms;
        public event NetEvent StopProvidingData;
        public event NetEvent Svr_stop;
        public event NetEvent receive_event;
        public event NetEvent ImpedanceDone;
        public event FailEvent connectFail;
        public event FailEvent receiveFail;
        public event FailEvent ConnectNotStable;
        public event FailEvent FileError;
        public event FailEvent forwardFail;
        public event DeviceError deviceError;

        public bool isReceiving = false;

        bool isReceiving_ID = false;
        HIDDevice client;

        string TransducerType = "LXL_EEG_DEV";

        public int indexOfArray = 0;
        int[,] dataBuf = new int[8, sampling_rate];
        int[,] dataBuf_noFilter = new int[8, sampling_rate];
        int ref_mode = 0;

        public int[] bsline_offset = new int[9];
        public int[,] dataBuf_NF = new int[9, sampling_rate];
        public int[,] dataBuf_noFilter_NF = new int[9, sampling_rate];

        BDFFile Recording_BDF;
        List<BDFSignal> signal_list = new List<BDFSignal>();
        BDFDataRecord newDataRecor;
        List<double>[] floats_BDF;

        string client_devicePath = "";

        public double[,] OneSecData = new double[9, sampling_rate];
        public double[,] Data200ms = new double[9, (int)(0.2 * sampling_rate)];

        bool forwarding = false;
        bool using_tcp_forward = false;
        IPAddress forward_IP = null;
        int forward_Port = 0;
        UdpClient UDPClient_forward = null;
        TcpClient TCPClient_forward = null;


        public bool impedanceDone = false;
        private string _commName  = "";
        public ComSvr(string CommName)
        {
            try
            {
                _commName = CommName;
               this.createDevice();
            }
            catch (Exception e)
            {
                stop();
                LogHelper.Log.LogError($"HID 设备创建发送异常:{e.ToString()}");
            }
        }

        public void createDevice()
        {
            client = new HIDDevice(_commName, false);
            client_devicePath = client.productInfo.devicePath;
        }

        BDFLocalPatientIdentification BDFp;
        BDFLocalRecordingIdentification BDFm;
        private void HeartBeatTM_Tick(object sender, EventArgs e)
        {
            try
            {
                byte[] heartbeat = new byte[5];
                heartbeat[0] = 0x55;
                heartbeat[1] = 0x00;
                heartbeat[2] = 10;
                heartbeat[3] = 0x00;
                heartbeat[4] = (byte)(heartbeat[0] + heartbeat[1] + heartbeat[2] + heartbeat[3]);
                client.write(heartbeat);
            }
            catch (Exception ex)
            {
                isReceiving = false;
                closed_identy = false;
                isReceiving_ID = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                receiveFail(this, new FailArgs(new Exception($"没有收到新的数据:{ex.ToString()}"), client_devicePath));
                stop();
                GC.Collect();
            }
        }

        public void start()
        {
            BDFp = new BDFLocalPatientIdentification("X", "X", DateTime.Parse("1970/1/1"), "X");
            BDFm = new BDFLocalRecordingIdentification(DateTime.Now.ToString(), "X", "X", "X");

            dataRecords_Stored = 0;
            index_data = 0;
            indexOfArray = 0;
            BDFSignal[] BDFSignals = new BDFSignal[10];
            for (int i = 1; i < 11; i++)
            {
                BDFSignals[i - 1] = new BDFSignal();
            }
            signal_labels = new string[9] { "A1-A1", "A2-A1", "Nan", "Nan", "Nan", "Nan", "Nan", "Nan", "Cz-A1" };
            for (int i = 0; i < signal_labels.Length; i++)
            {
                BDFSignals[i].Label = signal_labels[i];

            }
            for (int i = 1; i < 10; i++)
            {
                BDFSignals[i - 1].DigitalMaximum = 8388607;
                BDFSignals[i - 1].PhysicalMaximum = amplitude_range;
                BDFSignals[i - 1].PhysicalMinimum = -1 * amplitude_range;
                BDFSignals[i - 1].DigitalMinimum = -8388608;
                BDFSignals[i - 1].TransducerType = TransducerType;
                BDFSignals[i - 1].PhysicalDimension = "uv";
                BDFSignals[i - 1].IndexNumber = i;
                BDFSignals[i - 1].NumberOfSamplesPerDataRecord = sampling_rate;
                signal_list.Add(BDFSignals[i - 1]);
            }
            BDFSignals[9].DigitalMaximum = 8388607;
            BDFSignals[9].PhysicalMaximum = amplitude_range;
            BDFSignals[9].PhysicalMinimum = -1 * amplitude_range;
            BDFSignals[9].DigitalMinimum = -8388608;
            BDFSignals[9].IndexNumber = 10;
            BDFSignals[9].NumberOfSamplesPerDataRecord = sampling_rate;
            BDFSignals[9].Label = BDFSignal.NotationLabel;
            signal_list.Add(BDFSignals[9]);

            for (int i = 0; i < 9; i++)
            {
                var xxx_coeff = FirCoefficients.BandStop(sampling_rate, 48, 52);
                iirFilter[i] = new OnlineFirFilter(xxx_coeff);
            }

            for (int i = 0; i < impedance_waveform.Length; i++)
            {
                impedance_waveform[i] = new List<double>();
            }

            for (int i = 0; i < impedance.Length; i++)
            {
                impedance[i] = new List<double>();
            }
            impedanceDone = false;
            isSaving = false;
            saved    = true;
            filterON = false;
            timer    = new DispatcherTimer();
            HeartBeatTM = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            HeartBeatTM.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += new EventHandler(checkLink);
            HeartBeatTM.Tick += HeartBeatTM_Tick;
            allEqual = true;
            floats_BDF = new List<double>[9];
            for (int i = 0; i < 9; i++)
            {
                floats_BDF[i] = new List<double>();
            }
            Recording_BDF = new BDFFile();
            initBDF(Recording_BDF);


            try
            {
                startReceive();
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError($"start 发生异常:{ex.ToString()}");
                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                connectFail(this, new FailArgs(ex, client_devicePath));
                stop();
                GC.Collect();
            };
        }

        public void setPatientData(string patientCode, string patientSex, DateTime patientBirthDate, string patientName, List<string> patientAdditional)
        {
            if (patientCode.Length < 1)
                patientCode = "X";
            if (patientSex.Length < 1)
                patientSex = "X";
            if (patientName.Length < 1)
                patientName = "X";
            BDFp = new BDFLocalPatientIdentification(patientCode, patientSex, patientBirthDate, patientName, patientAdditional);
            Recording_BDF.Header.PatientIdentification = BDFp;
            Recording_BDF.Header.PatientIdentification = BDFp;
        }

        public void setPatientData(string patientCode, string patientSex, DateTime patientBirthDate, string patientName)
        {
            if (patientCode.Length < 1)
                patientCode = "X";
            if (patientSex.Length < 1)
                patientSex = "X";
            if (patientName.Length < 1)
                patientName = "X";
            BDFp = new BDFLocalPatientIdentification(patientCode, patientSex, patientBirthDate, patientName);
            Recording_BDF.Header.PatientIdentification = BDFp;
            Recording_BDF.Header.PatientIdentification = BDFp;
        }

        public void setDevData(string startdate1, string recordcode2, string technician3, string equipment4, List<string> additionalRecordingIdentification)
        {
            if (recordcode2.Length < 1)
                recordcode2 = "X";
            if (technician3.Length < 1)
                technician3 = "X";
            if (equipment4.Length < 1)
                equipment4 = "X";
            BDFm = new BDFLocalRecordingIdentification(startdate1, recordcode2, technician3, equipment4, additionalRecordingIdentification);
            Recording_BDF.Header.RecordingIdentification = BDFm;
            Recording_BDF.Header.RecordingIdentification = BDFm;
        }

        public void setDevData(string startdate1, string recordcode2, string technician3, string equipment4)
        {
            if (recordcode2.Length < 1)
                recordcode2 = "X";
            if (technician3.Length < 1)
                technician3 = "X";
            if (equipment4.Length < 1)
                equipment4 = "X";
            BDFm = new BDFLocalRecordingIdentification(startdate1, recordcode2, technician3, equipment4);
            Recording_BDF.Header.RecordingIdentification = BDFm;
            Recording_BDF.Header.RecordingIdentification = BDFm;
        }

        public int ReferenceMode
        {
            get { return ref_mode; }
        }
        public string[] signal_labels = new string[9] { "A1-A1", "A2-A1", "Nan", "Nan", "Nan", "Nan", "Nan", "Nan", "Cz-A1" };
        public void setSignalLabel(string[] labels, int mref_mode)
        {
            if (labels.Length != 9 && labels[labels.Length - 1] != "1" && labels[labels.Length - 1] != "2" && labels[labels.Length - 1] != "3")
            {
                throw new ArgumentException("label length error");
            }
            for (int i = 2; i < labels.Length - 1; i++)
            {
                signal_labels[i] = labels[i];
            } 

            if (mref_mode < 0 || mref_mode > 5)
                throw new ArgumentException("ReferenceMode error");
            else
            {
                ref_mode = mref_mode;
                switch (ref_mode)
                {
                    case 0:
                        {
                            signal_labels[0] = "A1" + "-A1";
                            signal_labels[1] = "A2" + "-A1";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-A1";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D")&& !signal_labels[i].Contains("K"))
                                {
                                    signal_labels[i] = signal_labels[i] + "-A1";
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            signal_labels[0] = "A1" + "-A2";
                            signal_labels[1] = "A2" + "-A2";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-A2";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                {
                                    signal_labels[i] = signal_labels[i] + "-A2";
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            signal_labels[0] = "A1" + "-AVG";
                            signal_labels[1] = "A2" + "-AVG";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-AVG";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                {
                                    signal_labels[i] = signal_labels[i] + "-AVG";
                                }
                            }
                        }
                        break;
                    case 3:
                        {

                            signal_labels[0] = "A1" + "-A1";
                            signal_labels[1] = "A2" + "-A2";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-AVG";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                    {
                                        signal_labels[i] = signal_labels[i] + "-A1";
                                    }
                                }
                                else
                                {
                                    if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                    {
                                        signal_labels[i] = signal_labels[i] + "-A2";
                                    }
                                }
                            }
                        }
                        break;
                    case 4:
                        {
                            signal_labels[0] = "A1" + "-A2";
                            signal_labels[1] = "A2" + "-A1";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-AVG";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (i % 2 == 0)
                                {
                                    if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                    {
                                        signal_labels[i] = signal_labels[i] + "-A2";
                                    }
                                }
                                else
                                {
                                    if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                    {
                                        signal_labels[i] = signal_labels[i] + "-A1";
                                    }
                                }
                            }
                        }
                        break;
                    default:
                        {
                            signal_labels[0] = "A1" + "-Cz";
                            signal_labels[1] = "A2" + "-Cz";
                            signal_labels[signal_labels.Length - 1] = "Cz" + "-Cz";
                            for (int i = 2; i < signal_labels.Length - 1; i++)
                            {
                                if (!signal_labels[i].Contains("Nan") && !signal_labels[i].Contains("D") && !signal_labels[i].Contains("K"))
                                {
                                    signal_labels[i] = signal_labels[i] + "-Cz";
                                }
                            }
                        }
                        break;
                }
                BDFSignal[] BDFSignals = new BDFSignal[10];
                for (int i = 1; i < 11; i++)
                {
                    BDFSignals[i - 1] = new BDFSignal();
                }
                signal_list.Clear();

                for (int i = 0; i < signal_labels.Length - 1; i++)
                {
                    BDFSignals[i].Label = signal_labels[i];
                }

                BDFSignals[signal_labels.Length - 1].Label = signal_labels[signal_labels.Length - 1];

                for (int i = 1; i < 10; i++)
                {
                    BDFSignals[i - 1].DigitalMaximum = 8388607;
                    BDFSignals[i - 1].PhysicalMaximum = amplitude_range;
                    BDFSignals[i - 1].PhysicalMinimum = -1 * amplitude_range;
                    BDFSignals[i - 1].DigitalMinimum = -8388608;
                    BDFSignals[i - 1].TransducerType = TransducerType;
                    BDFSignals[i - 1].PhysicalDimension = "uv";
                    BDFSignals[i - 1].IndexNumber = i;
                    BDFSignals[i - 1].NumberOfSamplesPerDataRecord = sampling_rate;
                    signal_list.Add(BDFSignals[i - 1]);
                }
                BDFSignals[9].DigitalMaximum = 8388607;
                BDFSignals[9].PhysicalMaximum = amplitude_range;
                BDFSignals[9].PhysicalMinimum = -1 * amplitude_range;
                BDFSignals[9].DigitalMinimum = -8388608;
                BDFSignals[9].IndexNumber = 10;
                BDFSignals[9].NumberOfSamplesPerDataRecord = sampling_rate;
                BDFSignals[9].Label = BDFSignal.NotationLabel;
                signal_list.Add(BDFSignals[9]);
                Recording_BDF.Header.Signals = signal_list;

                impedanceDone = false;
            }
        }

        public bool filterON = false;

        void initBDF(BDFFile RecordingBDF)
        {
            RecordingBDF.Header.IsBDFPlus = true;
            RecordingBDF.Header.Continuous = true;
            RecordingBDF.Header.StartDateTime = DateTime.Now;
            RecordingBDF.Header.NumberOfSignalsInDataRecord = 10;
            RecordingBDF.Header.DurationOfDataRecordInSeconds = 1;
            RecordingBDF.Header.Signals = signal_list;
            RecordingBDF.Header.PatientIdentification = BDFp;
            RecordingBDF.Header.RecordingIdentification = BDFm;

        }
        bool allEqual = true;

        //int Tmp_datarecord_Rcv = 0;
        //int misCountNum_5s = 0;
        //bool gotTmp_datarecord_Rcv = false;

        private void checkLink(object sender, EventArgs e)
        {
            Console.WriteLine("checking link");

            //if (!gotTmp_datarecord_Rcv)
            //    Tmp_datarecord_Rcv = dataRecords_Stored;
            //else
            //{
            //    if(dataRecords_Stored-Tmp_datarecord_Rcv < 4)
            //    {
            //        misCountNum_5s++;
            //    }
            //    if (misCountNum_5s > 3)
            //    {
            //        ConnectNotStable(this, new FailArgs(new Exception("连接不稳定，请检查连接"), client_devicePath));
            //    }
            //    Tmp_datarecord_Rcv = dataRecords_Stored;
            //}
            if (allEqual && isReceiving)
            {
                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                receiveFail(this, new FailArgs(new Exception("没有收到新的数据"), client_devicePath));
                client = null;
                GC.Collect();

            }
            else
            {
                //data receiving normally
                allEqual = true;
                if (misCountNum > 5)
                {
                    timer.IsEnabled = false;
                    ConnectNotStable(this, new FailArgs(new Exception("连接不稳定，请检查连接"), client_devicePath));
                    stop();
                }
                else
                {
                    Interlocked.Exchange(ref misCountNum, 0);
                    Console.WriteLine("misCountNum has been set 0");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void sendGetBatteryLevel()
        {
            byte[] sendBuffer = new byte[5];
            sendBuffer[0] = 0x55;
            sendBuffer[1] = 0x00;
            sendBuffer[2] = 0x06;
            sendBuffer[3] = 0x00;
            sendBuffer[4] = (byte)(sendBuffer[0] + sendBuffer[1] + sendBuffer[2] + sendBuffer[3]);
            client.write(sendBuffer);
            LogHelper.Log.LogDebug("发送电池电量获取指令");
        }

        #region 设备数据监测线程
        private DateTime _dataRecvTime = DateTime.Now;
        private Thread _rcvThread     = null;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitorTask;
        private bool _isMonitoring = false;  // 控制监测状态
        private async Task deviceMonitor(CancellationToken cancellationToken)
        {
            while (_isMonitoring && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if(Math.Abs(DateTime.Now.Subtract(_dataRecvTime).TotalMilliseconds) > 1500)
                    {
                        string strMsg = "监测到设备数据接收超时,数据接收中断,是否重新连接设备?";
                        if(null != deviceError)
                        {
                            deviceError(strMsg);
                        }
                        LogHelper.Log.LogError("监测到设备通信异常中断!");
                        //if(MessageBoxResult.No == MessageBox.Show(strMsg, "数据通信异常", MessageBoxButton.YesNo, MessageBoxImage.Question))
                        //{
                        //    break;
                        //}
                        //else
                        //{
                        //    this.stop();
                        //    this.startReceive();
                        //    LogHelper.Log.LogDebug("设备重新连接完毕");
                        //}
                    }
                    // 每500ms进行一次检查
                    await Task.Delay(500, cancellationToken);  
                }
                catch (TaskCanceledException)
                {
                    // 任务被取消时处理
                    LogHelper.Log.LogDebug("监测任务被取消");
                }
                catch (Exception ex)
                {
                    // 其他异常处理
                    LogHelper.Log.LogDebug("监测过程中发生异常: " + ex.Message);
                }
            }
        }

        public void startMonitor()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _isMonitoring         = true;

            // 启动一个后台任务来模拟定时器
            _monitorTask = Task.Run(async () => await deviceMonitor(cancellationToken));
        }

        // 停止监测
        public void StopMonitor()
        {
            _isMonitoring = false;
            // 取消监测任务
            _cancellationTokenSource.Cancel();  
            // 等待任务完成
            _monitorTask?.Wait();
        }
        #endregion


        public void startReceive()
        {
            try
            {
                isReceiving = true;
                _rcvThread = new Thread(ToRcvData);
                _rcvThread.IsBackground = true;
                _rcvThread.Start();

                byte[] sendBuffer = new byte[5];
                sendBuffer[0] = 0x55;
                sendBuffer[1] = 0x00;
                sendBuffer[2] = 0x00;
                sendBuffer[3] = 0x00;
                sendBuffer[4] = (byte)(sendBuffer[0] + sendBuffer[1] + sendBuffer[2] + sendBuffer[3]);
                client.write(sendBuffer);

                LogHelper.Log.LogDebug("发送开始获取数据指令");
                // 发送电量读取指令
                sendGetBatteryLevel();

                timer.IsEnabled = true;
                HeartBeatTM.IsEnabled = true;

                // 启动监听
                startMonitor();
            }
            catch (Exception e)
            {
                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                receiveFail(this, new FailArgs(new Exception($"开启接收数据错误:{e.ToString()}"), client_devicePath));
                stop();
                GC.Collect();
            }
        }

        public void stop()
        {
            //等待设立了停止操作符
            if(null == client)
            {
                return;
            }

            try
            {
                _isMonitoring = false;

                byte[] toStop = new byte[5];
                toStop[0] = 0x55;
                toStop[1] = 0x00;
                toStop[2] = 12;
                toStop[3] = 0x00;
                toStop[4] = (byte)(toStop[0] + toStop[1] + toStop[2] + toStop[3]);
                client.write(toStop);

                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }

                // 等待读取线程退出
                LogHelper.Log.LogDebug("等待读取线程退出");
                _rcvThread.Join();
                LogHelper.Log.LogDebug("读取线程退出完毕");
                client.close();
                LogHelper.Log.LogDebug("向设备发送数据接收停止指令");
            }
            catch (Exception e)
            {
                isReceiving = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                LogHelper.Log.LogError($"设备停止发生异常:{e.ToString()}");
            }
            finally
            {
                StopProvidingData?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                Svr_stop(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                client.close();
                client = null;
            }
        }


        DispatcherTimer timer;
        DispatcherTimer HeartBeatTM;

        private void ToRcvData()
        {
            int length = client.productInfo.IN_reportByteLength;

            int toCheck = 0;
            string ans = client.productInfo.product + "failed";
            byte CheckSum = 0x00;
            byte[] buf = new byte[client.productInfo.IN_reportByteLength];
            byte[] buf_all = new byte[client.productInfo.IN_reportByteLength * 2];
            int bytes_left = 0;

            LogHelper.Log.LogDebug("数据接收线程运行中.....");
            int ii = 0;
            while (isReceiving)
            {
                try
                {
                    buf = client.read();
                    length = client.productInfo.IN_reportByteLength;
                    _dataRecvTime = DateTime.Now;
                }
                catch (Exception e)
                {
                    isReceiving = false;
                    timer.IsEnabled = false;
                    HeartBeatTM.IsEnabled = false;
                    if (!saved)
                    {
                        EndSaveBDF();
                        saved = true;
                    }
                    receiveFail?.Invoke(this, new FailArgs(new Exception($"接收数据中出现错误:{e.ToString()}"), client_devicePath));
                    stop();
                    GC.Collect();
                    LogHelper.Log.LogError("设备IO数据接收发生异常:{0}", e.ToString());
                    break;
                }


                int inverse_0 = 0;
                for (int jijiji = 0; jijiji < length; jijiji++)
                {
                    if (buf[length - 1 - jijiji] != 0)
                    {
                        break;
                    }
                    else
                    {
                        inverse_0++;
                    }
                }
                length = length - inverse_0;


                int end_idx = length + bytes_left;
                end_idx = Math.Min(end_idx, buf_all.Length);

                for (int ijijij = 0; ijijij < length; ijijij++)
                {
                    buf_all[end_idx - 1 - ijijij] = buf[length - 1 - ijijij];
                }
                int tail_idx = 0; bytes_left = 0;


                ii = 0;
                while (ii < end_idx)
                {
                    while (ii < end_idx && buf_all[ii] != 0x55)
                    {
                        ii++;
                    }

                    int DataLength = 0;

                    if (ii + 4 >= end_idx)
                    {
                        ii++;
                        continue;
                    }

                    DataLength = getValue(buf_all[ii + 3]);

                    if (DataLength < 0)
                    {
                        ii++;
                        continue;
                    }
                    if (ii + DataLength + 4 >= end_idx)
                    {
                        ii++;
                        continue;
                    }
                    CheckSum = 0x00; toCheck = 0;
                    for (int i = 0; i < DataLength + 4; i++)
                    {
                        toCheck = toCheck + buf_all[ii + i];
                    }
                    CheckSum = (byte)toCheck;
                    if (CheckSum == buf_all[ii + 4 + DataLength])
                    {
                        //校验成功
                        byte[] validPack = new byte[DataLength + 5];
                        for (int i = 0; i < DataLength + 5; i++)
                        {
                            validPack[i] = buf_all[ii + i];
                        }
                        //解析一个完整的数据包
                        if (DataLength != 0 && validPack[2] == 9)
                        {
                            ii = ii + DataLength + 5;
                        }
                        else
                        {
                            ParsePack(validPack, DataLength);
                            ii = ii + DataLength + 5;
                        }

                        tail_idx = ii;
                    }
                    else
                    {
                        ii++;
                    }
                }

                ii = tail_idx;
                while (ii < end_idx && ii < buf_all.Length)
                {
                    buf_all[ii - tail_idx] = buf_all[ii];
                    ii++;
                    bytes_left++;
                }
                if (bytes_left > 0)
                {
                    // Console.WriteLine("bytes_left:OHHHHHHHHHHHHHHHHH");
                }
            }
            LogHelper.Log.LogDebug("数据接收线程退出.....");
        }

        public void resetBasline()
        {
            for (int i = 0; i < 9; i++)
            {
                bsline_offset[i] = 0;
            }
        }
        public void calcuBaseline()
        {
            for (int i = 0; i < 9; i++)
            {
                double tmp_data = 0;
                for (int jjj = 0; jjj < sampling_rate; jjj++)
                {
                    tmp_data += dataBuf_NF[i, jjj];
                }
                Console.WriteLine("" + tmp_data);
                Console.WriteLine("" + (int)(tmp_data / sampling_rate));
                bsline_offset[i] = bsline_offset[i] + (int)(tmp_data / sampling_rate);
            }
        }

        public bool setFilter(double cutoff1, double cutoff2, bool bandpass)
        {
            if (cutoff1 >= cutoff2 && cutoff2 != 0)
                return false;
            if (cutoff1 == 0 && cutoff2 == 0)
                return false;

            if (cutoff1 != 0 && cutoff2 != 0)
            {

                for (int i = 0; i < 9; i++)
                {
                    if (bandpass)
                    {
                        var xxx_coeff = FirCoefficients.BandPass(sampling_rate, cutoff1, cutoff2);
                        iirFilter[i] = new OnlineFirFilter(xxx_coeff);
                    }
                    else
                    {
                        var xxx_coeff = FirCoefficients.BandStop(sampling_rate, cutoff1, cutoff2);
                        iirFilter[i] = new OnlineFirFilter(xxx_coeff);
                    }
                }
                return true;
            }
            else if (cutoff1 == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    var xxx_coeff = FirCoefficients.LowPass(sampling_rate, cutoff2);
                    iirFilter[i] = new OnlineFirFilter(xxx_coeff);

                }
                return true;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    var xxx_coeff = FirCoefficients.HighPass(sampling_rate, cutoff1);
                    iirFilter[i] = new OnlineFirFilter(xxx_coeff);

                }
                return true;
            }
        }
        public void resetFilter()
        {
            for (int i = 0; i < 9; i++)
            {
                var xxx_coeff = FirCoefficients.BandStop(sampling_rate, 48, 52);
                iirFilter[i] = new OnlineFirFilter(xxx_coeff);
            }
        }



        int dataRecords_Stored = 0;
        int index_data = 0;
        int index_data_impedance = 0;

        public int volOfDev = 0;
        public bool charging = false;

        int SerialCount;
        bool gotSerialCount = false;
        int misCountNum = 0;

        private void ParsePack(byte[] pack, int datalength)
        {
            allEqual = false;
            int ii = 0;
            int one = 0;

            switch (pack[ii + 2])
            {
                case 7:
                    {
                        volOfDev = getValueUnSigned(pack[5], pack[4]);
                        // LogHelper.Log.LogTrace("Device", $"解析到电量数据帧:{string.Join(",", pack.Select(b => $"0x{b:X2}"))}");
                        // LogHelper.Log.LogDebug($"获取到电池电量 :{volOfDev}");
                    }
                    break;
                case 1:
                    {
                        if (!gotSerialCount)
                        {
                            SerialCount = getValue(pack[ii + 1]);
                            gotSerialCount = true;
                        }
                        else
                        {
                            int nowInt = pack[ii + 1];
                            int diff = Math.Abs(SerialCount - nowInt);
                            if (nowInt != 0 && nowInt != 1 && diff > 5)
                            {
                                Console.WriteLine("SerialCunt: " + SerialCount + " now int:" + nowInt);
                                // receiveFail(this, new FailArgs(new Exception("没有收到新的数据"), client_devicePath));
                                //ConnectNotStable(this, new FailArgs(new Exception("连接不稳定，请检查连接"), client_devicePath));
                                Interlocked.Increment(ref misCountNum);
                            }
                            SerialCount = pack[ii + 1];
                            SerialCount = SerialCount % 255;
                        }


                        if (datalength == 24)
                        {
                            if (forwarding)
                            {
                                if (using_tcp_forward)
                                {
                                    try
                                    {
                                        TCPClient_forward.GetStream().Write(pack, 0, pack.Length);
                                        Console.WriteLine("TCP forwarding");

                                    }
                                    catch (Exception e)
                                    {
                                        //Forward Fail
                                        forwardFail(this, new FailArgs(e, "转发数据失败"));
                                        stopForward();
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        UDPClient_forward.Send(pack, pack.Length);
                                        Console.WriteLine("UDP forwarding");

                                    }
                                    catch (Exception e)
                                    {
                                        //Forward Fail
                                        forwardFail(this, new FailArgs(e, "转发数据失败"));
                                        stopForward();
                                    }

                                }

                            }
                            //allEqual = false;
                            for (int i = 0; i < datalength / 3; i++)
                            {
                                one = getValueSigned3(pack[ii + 4 + 3 * index_data + 2], pack[ii + 4 + 3 * index_data + 1], pack[ii + 4 + 3 * index_data]);

                                index_data++;
                                
                                if (filterON)
                                    dataBuf[index_data - 1, indexOfArray] = (int)iirFilter[i].ProcessSample(one);
                                else
                                    dataBuf[index_data - 1, indexOfArray] = one;

                                // applying no filter to the saving data
                                //dataBuf_noFilter[index_data - 1, indexOfArray] = one;

                                // applying filter to the saving data
                                dataBuf_noFilter[index_data - 1, indexOfArray] = dataBuf[index_data - 1, indexOfArray];

                                if (dataBuf_noFilter[index_data - 1, indexOfArray] > 8388607)
                                {
                                    dataBuf_noFilter[index_data - 1, indexOfArray] = 8388607;
                                    //int xxx = dataBuf[index_data - 1, indexOfArray];
                                    //Console.WriteLine(""+xxx);
                                }
                                if (dataBuf_noFilter[index_data - 1, indexOfArray] < -8388608)
                                {
                                    dataBuf_noFilter[index_data - 1, indexOfArray] = -8388608;
                                }


                                if (dataBuf[index_data - 1, indexOfArray] > 8388607)
                                {
                                    dataBuf[index_data - 1, indexOfArray] = 8388607;
                                    //int xxx = dataBuf[index_data - 1, indexOfArray];
                                    //Console.WriteLine(""+xxx);
                                }
                                if (dataBuf[index_data - 1, indexOfArray] < -8388608)
                                {
                                    dataBuf[index_data - 1, indexOfArray] = -8388608;
                                }

                                if (index_data == 8)
                                {

                                    dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray];
                                    dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray];

                                    switch (ref_mode)
                                    {
                                        case 0:
                                            {
                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray] - dataBuf[0, indexOfArray];
                                            }
                                            break;
                                        case 1:
                                            {
                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray] - dataBuf[1, indexOfArray];
                                            }
                                            break;
                                        case 2:
                                            {
                                                var tmp = (int)((dataBuf[0, indexOfArray] + dataBuf[1, indexOfArray]) / 2.0);

                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray] - tmp;
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray] - tmp;
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray] - tmp;
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray] - tmp;
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray] - tmp;
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray] - tmp;
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray] - tmp;
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray] - tmp;
                                            }
                                            break;
                                        case 3:
                                            {
                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray] - dataBuf[1, indexOfArray];

                                            }
                                            break;
                                        case 4:
                                            {
                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray] - dataBuf[0, indexOfArray];
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray] - dataBuf[1, indexOfArray];
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray] - dataBuf[0, indexOfArray];

                                            }
                                            break;
                                        default:
                                            {// refercenced to Cz, the original ref.
                                                dataBuf_NF[0, indexOfArray] = dataBuf[0, indexOfArray];
                                                dataBuf_NF[1, indexOfArray] = dataBuf[1, indexOfArray];
                                                dataBuf_NF[2, indexOfArray] = dataBuf[2, indexOfArray];
                                                dataBuf_NF[3, indexOfArray] = dataBuf[3, indexOfArray];
                                                dataBuf_NF[4, indexOfArray] = dataBuf[4, indexOfArray];
                                                dataBuf_NF[5, indexOfArray] = dataBuf[5, indexOfArray];
                                                dataBuf_NF[6, indexOfArray] = dataBuf[6, indexOfArray];
                                                dataBuf_NF[7, indexOfArray] = dataBuf[7, indexOfArray];
                                            }
                                            break;

                                    }
                                     
                                    switch (ref_mode)
                                    {

                                        case 0:
                                            {
                                                dataBuf_NF[8, indexOfArray] = -dataBuf[0, indexOfArray];
                                            }
                                            break;
                                        case 1:
                                            {
                                                dataBuf_NF[8, indexOfArray] = -dataBuf[1, indexOfArray];
                                            }
                                            break;
                                        case 2:
                                            {
                                                dataBuf_NF[8, indexOfArray] = (int)((dataBuf[0, indexOfArray] + dataBuf[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        case 3:
                                            {
                                                dataBuf_NF[8, indexOfArray] = (int)((dataBuf[0, indexOfArray] + dataBuf[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        case 4:
                                            {
                                                dataBuf_NF[8, indexOfArray] = (int)((dataBuf[0, indexOfArray] + dataBuf[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        default:
                                            {
                                                dataBuf_NF[8, indexOfArray] = 0;
                                            }
                                            break;
                                    }

                                    List<Tuple<int, int>> Diff_pairs = new List<Tuple<int, int>>();
                                    for (int ssx = 0; ssx < signal_labels.Length; ssx++)
                                    {
                                        if (signal_labels[ssx].Contains("D1"))
                                            for (int ssy = 0; ssy < signal_labels.Length; ssy++)
                                            {
                                                if (signal_labels[ssy].Contains("D2"))
                                                {
                                                    Diff_pairs.Add(Tuple.Create(ssx, ssy));
                                                }
                                            }

                                        if (signal_labels[ssx].Contains("D3"))
                                            for (int ssy = 0; ssy < signal_labels.Length; ssy++)
                                            {
                                                if (signal_labels[ssy].Contains("D4"))
                                                {
                                                    Diff_pairs.Add(Tuple.Create(ssx, ssy));
                                                }
                                            }

                                        if (signal_labels[ssx].Contains("D5"))
                                            for (int ssy = 0; ssy < signal_labels.Length; ssy++)
                                            {
                                                if (signal_labels[ssy].Contains("D6"))
                                                {
                                                    Diff_pairs.Add(Tuple.Create(ssx, ssy));
                                                }
                                            }
                                        if (signal_labels[ssx].Contains("K1"))
                                            for (int ssy = 0; ssy < signal_labels.Length; ssy++)
                                            {
                                                if (signal_labels[ssy].Contains("K2"))
                                                {
                                                    Diff_pairs.Add(Tuple.Create(ssx, ssy));
                                                }
                                            }
                                    }

                                    for (int ssx = 0; ssx < Diff_pairs.Count; ssx++)
                                    {
                                        var ppp= Diff_pairs[ssx];
                                        dataBuf_NF[ppp.Item1, indexOfArray] = dataBuf[ppp.Item1, indexOfArray] - dataBuf[ppp.Item2, indexOfArray];
                                        dataBuf_NF[ppp.Item2, indexOfArray] = dataBuf[ppp.Item2, indexOfArray] - dataBuf[ppp.Item1, indexOfArray];
                                    }

                                    for (int ffff = 0; ffff < 9; ffff++)
                                    {
                                        dataBuf_NF[ffff, indexOfArray] = dataBuf_NF[ffff, indexOfArray] - bsline_offset[ffff];
                                    }

                                    dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray];
                                    dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray];

                                    switch (ref_mode)
                                    {
                                        case 0:
                                            {
                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray] - dataBuf_noFilter[0, indexOfArray];

                                            }
                                            break;
                                        case 1:
                                            {
                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray] - dataBuf_noFilter[1, indexOfArray];

                                            }
                                            break;
                                        case 2:
                                            {
                                                var tmp = (int)((dataBuf_noFilter[0, indexOfArray] + dataBuf_noFilter[1, indexOfArray]) / 2.0);

                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray] - tmp;
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray] - tmp;
                                            }
                                            break;
                                        case 3:
                                            {
                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                            }
                                            break;
                                        case 4:
                                            {
                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray] - dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray] - dataBuf_noFilter[0, indexOfArray];
                                            }
                                            break;
                                        default:
                                            { // refercenced to Cz, the original ref.

                                                dataBuf_noFilter_NF[0, indexOfArray] = dataBuf_noFilter[0, indexOfArray];
                                                dataBuf_noFilter_NF[1, indexOfArray] = dataBuf_noFilter[1, indexOfArray];
                                                dataBuf_noFilter_NF[2, indexOfArray] = dataBuf_noFilter[2, indexOfArray];
                                                dataBuf_noFilter_NF[3, indexOfArray] = dataBuf_noFilter[3, indexOfArray];
                                                dataBuf_noFilter_NF[4, indexOfArray] = dataBuf_noFilter[4, indexOfArray];
                                                dataBuf_noFilter_NF[5, indexOfArray] = dataBuf_noFilter[5, indexOfArray];
                                                dataBuf_noFilter_NF[6, indexOfArray] = dataBuf_noFilter[6, indexOfArray];
                                                dataBuf_noFilter_NF[7, indexOfArray] = dataBuf_noFilter[7, indexOfArray];
                                            }
                                            break;
                                    }

                                    switch (ref_mode)
                                    {
                                        case 0:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = -dataBuf_noFilter[0, indexOfArray];
                                            }
                                            break;
                                        case 1:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = -dataBuf_noFilter[1, indexOfArray];
                                            }
                                            break;
                                        case 2:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = (int)((dataBuf_noFilter[0, indexOfArray] + dataBuf_noFilter[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        case 3:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = (int)((dataBuf_noFilter[0, indexOfArray] + dataBuf_noFilter[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        case 4:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = (int)((dataBuf_noFilter[0, indexOfArray] + dataBuf_noFilter[1, indexOfArray]) / 2.0);
                                            }
                                            break;
                                        default:
                                            {
                                                dataBuf_noFilter_NF[8, indexOfArray] = 0;
                                            }
                                            break;

                                    }
                                    for (int ssx = 0; ssx < Diff_pairs.Count; ssx++)
                                    {
                                        var ppp = Diff_pairs[ssx];
                                        dataBuf_noFilter_NF[ppp.Item1, indexOfArray] = dataBuf_noFilter[ppp.Item1, indexOfArray] - dataBuf_noFilter[ppp.Item2, indexOfArray];
                                        dataBuf_noFilter_NF[ppp.Item2, indexOfArray] = dataBuf_noFilter[ppp.Item2, indexOfArray] - dataBuf_noFilter[ppp.Item1, indexOfArray];
                                    }

                                    indexOfArray++;

                                    if (indexOfArray == sampling_rate)
                                    {
                                        for (int z1 = 0; z1 < 9; z1++)
                                        {
                                            for (int z2 = 0; z2 < sampling_rate; z2++)
                                            {
                                                OneSecData[z1, z2] = (dataBuf_noFilter_NF[z1, z2] * LinearTimes);
                                            }
                                        }
                                        GotOneSec?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                                    }
                                    if (indexOfArray % (int)(0.2 * sampling_rate) == 0)
                                    {
                                        for (int z1 = 0; z1 < 9; z1++)
                                        {
                                            for (int z2 = 0; z2 < (int)(0.2 * sampling_rate); z2++)
                                            {
                                                Data200ms[z1, z2] = (dataBuf_noFilter_NF[z1, (z2 + sampling_rate + indexOfArray - (int)(0.2 * sampling_rate)) % sampling_rate] * LinearTimes);
                                            }

                                        }
                                        Got200ms?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                                    }
                                    if (indexOfArray == sampling_rate && isSaving)
                                    {
                                        for (int z1 = 0; z1 < 9; z1++)
                                        {
                                            for (int z2 = 0; z2 < sampling_rate; z2++)
                                            {
                                                floats_BDF[z1].Add(dataBuf_noFilter_NF[z1, z2] * LinearTimes);
                                            }
                                        }

                                        //接收到了1s的数据

                                        newDataRecor.Signals.Add("1." + signal_labels[0], floats_BDF[0]);
                                        newDataRecor.Signals.Add("2." + signal_labels[1], floats_BDF[1]);
                                        newDataRecor.Signals.Add("3." + signal_labels[2], floats_BDF[2]);
                                        newDataRecor.Signals.Add("4." + signal_labels[3], floats_BDF[3]);
                                        newDataRecor.Signals.Add("5." + signal_labels[4], floats_BDF[4]);
                                        newDataRecor.Signals.Add("6." + signal_labels[5], floats_BDF[5]);
                                        newDataRecor.Signals.Add("7." + signal_labels[6], floats_BDF[6]);
                                        newDataRecor.Signals.Add("8." + signal_labels[7], floats_BDF[7]);
                                        newDataRecor.Signals.Add("9." + signal_labels[8], floats_BDF[8]);

                                        for (int w = 1; w < 10; w++)
                                        {
                                            floats_BDF[w - 1] = new List<double>();
                                        }
                                        try
                                        {
                                            Recording_BDF.addDataRecord(newDataRecor);
                                        }
                                        catch (IOException e)
                                        {
                                            FileError(this, new FailArgs(e, client_devicePath));
                                        }
                                        saved = false;
                                        dataRecords_Stored++;
                                        newDataRecor = new BDFDataRecord(Recording_BDF.Header.StartDateTime, Recording_BDF.Header.DurationOfDataRecordInSeconds * dataRecords_Stored);

                                    }
                                    indexOfArray = indexOfArray % sampling_rate;
                                }

                                index_data = index_data % 8;
                            }
                        }

                    }
                    break;
                case 5:
                    {
                        if (datalength == 3)
                        {
                            index_data_impedance = pack[ii + 4 + 0];
                            if (index_data_impedance < 0)
                            {
                                LogHelper.Log.LogDebug($"阻抗匹配索引异常：{index_data_impedance},修正为：0");
                                index_data_impedance = 0;
                                Console.WriteLine("wrong_idx:" + index_data_impedance);
                            }
                            if (index_data_impedance == 255)
                            {
                                // LogHelper.Log.LogDebug($"阻抗匹配索引异常：{index_data_impedance},修正为：8");
                                index_data_impedance = 8;
                            }

                            if(index_data_impedance > 8)
                            {
                                LogHelper.Log.LogDebug($"阻抗匹配索引异常：{index_data_impedance},修正为：8");
                                index_data_impedance = 8;
                            }

                            one = getValueSigned(pack[ii + 4 + 2], pack[ii + 4 + 1]);
                            impedance_waveform[index_data_impedance].Add(one * 1.0);
                            if (index_data_impedance == 8 && impedance_waveform[index_data_impedance].Count > 0 && impedance_waveform[index_data_impedance].Count % 100 == 0)
                            {
                                ComputeImpedance();
                            }
                        }
                    }
                    break;
                case 90:
                    {
                        if (forwarding)
                        {
                            if (using_tcp_forward)
                            {
                                try
                                {
                                    TCPClient_forward.GetStream().Write(pack, 0, pack.Length);

                                }
                                catch (Exception e)
                                {
                                    //Forward Fail
                                    forwardFail(this, new FailArgs(e, "转发数据失败"));
                                    stopForward();
                                }
                            }
                            else
                            {
                                try
                                {
                                    UDPClient_forward.Send(pack, pack.Length);


                                }
                                catch (Exception e)
                                {
                                    //Forward Fail
                                    forwardFail(this, new FailArgs(e, "转发数据失败"));
                                    stopForward();
                                }

                            }

                        }

                        int tmp = 0;
                        string AnotationStr = "";

                        if (datalength < 1)
                            return;

                        if (datalength > 1)
                        {
                            for (int i = 0; i < datalength; i++)
                            {
                                tmp = Convert.ToInt32(pack[4 + i]);

                                if (tmp == 9 || tmp == 10 || tmp == 13 || (tmp > 31 && tmp < 127))
                                {
                                    AnotationStr = AnotationStr + Convert.ToChar(pack[4 + i]);
                                }
                                else
                                {
                                    AnotationStr = AnotationStr + " " + tmp + " ";
                                }
                            }
                        }
                        else
                        {
                            tmp = Convert.ToInt32(pack[4]);

                            if ((tmp > 32 && tmp < 127))
                            {
                                AnotationStr = AnotationStr + Convert.ToChar(pack[4]);
                            }
                            else
                            {
                                AnotationStr = AnotationStr + " " + tmp + " ";
                            }
                        }
                        //string ID = BitConverter.ToString(tmp);
                        // string EVENT = System.Text.Encoding.ASCII.GetString(tmp);
                        if (isSaving)
                            newDataRecor.addAnnotation(Recording_BDF.Header.DurationOfDataRecordInSeconds * dataRecords_Stored + indexOfArray / 1.0 / sampling_rate, "" + AnotationStr);
                        receive_event?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                    }
                    break;
                default: break;
            }

        }

        public double[] impedanceValue = new double[9];

        public void ComputeImpedance()
        {

            for (int i = 0; i < impedance_waveform.Length; i++)
            {
                if (impedance_waveform[i].Count > 100)
                {
                    List<double> tmp = new List<double>();
                    for (int jj = 0; jj < 100; jj++)
                    {
                        tmp.Add(impedance_waveform[i][impedance_waveform[i].Count - 100 + jj]);
                    }
                    impedance_waveform[i].Clear();
                    impedanceValue[i] = computeOneImpedance(tmp);
                    ImpedanceDone?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                }
            }
        }

        private double computeOneImpedance(List<double> Channel)
        {
            Channel.RemoveRange(0, 10);
            Channel.RemoveRange(Channel.Count - 11, 10);

            List<int> pos_peak_idx = new List<int>();
            List<int> neg_peak_idx = new List<int>();

            for (int i = 2; i < Channel.Count - 2; i++)
            {
                if (Channel[i] > Channel[i - 1] && Channel[i] >= Channel[i + 1])
                    pos_peak_idx.Add(i);
                if (Channel[i] <= Channel[i - 1] && Channel[i] < Channel[i + 1])
                    neg_peak_idx.Add(i);
            }
            Console.WriteLine("neg:" + neg_peak_idx.Count + " pos" + pos_peak_idx.Count);
            //if (Math.Abs(neg_peak_idx.Count - pos_peak_idx.Count) > 2)
            //{
            //    throw new Exception("not consistent pos/neg peaks"); 
            //}
            //if (neg_peak_idx.Count < 5 || neg_peak_idx.Count <5)
            //    throw new Exception("too little peaks");

            List<double> peak_peak = new List<double>();
            List<double> tmp_series = new List<double>();
            for (int i = 1; i < Math.Min(pos_peak_idx.Count, neg_peak_idx.Count) - 1; i++)
            {

                int idx_1 = pos_peak_idx[i];
                int idx_2 = neg_peak_idx[i - 1];
                int idx_3 = neg_peak_idx[i];

                int min_idx = idx_1, max_idx = idx_2;
                if (max_idx < min_idx)
                {
                    var tmp_t = min_idx;
                    min_idx = max_idx;
                    max_idx = tmp_t;
                }
                tmp_series.Clear();
                for (int j = min_idx - 1; j < max_idx + 1; j++)
                {
                    tmp_series.Add(Channel[j]);
                }
                peak_peak.Add(tmp_series.Max() - tmp_series.Min());


                min_idx = idx_1; max_idx = idx_3;
                if (max_idx < min_idx)
                {
                    var tmp_t = min_idx;
                    min_idx = max_idx;
                    max_idx = tmp_t;
                }
                tmp_series.Clear();
                for (int j = min_idx - 1; j < max_idx + 1; j++)
                {
                    tmp_series.Add(Channel[j]);
                }
                peak_peak.Add(tmp_series.Max() - tmp_series.Min());
            }
            if (peak_peak.Count < 1)
            {
                return 450;
            }
            else
            {
                return peak_peak.Average();
            }
        }

        //private double getVmax(List<double> tmp)
        //{
        //    double max = 0;
        //    for (int i = 1; i < tmp.Count; i=i+2)
        //    {
        //        double tt1=0,tt2=0;
        //        tt1=Math.Abs((tmp[i] - tmp[i - 1]));
        //        tt2=Math.Abs((tmp[i] - tmp[i + 1]));
        //        if (max <tt1 )
        //            max=tt1;
        //        if(max<tt2)
        //            max=tt2;
        //    }
        //    return max;
        //}



        public void EndSaveBDF()
        {
            //int toCount = 0;
            //for (int i = 0; i < 8; i++)
            //{
            //    toCount = floats_BDF[i].Count;
            //    while (toCount < sampling_rate)
            //    {
            //        floats_BDF[i].Add(0);
            //        toCount++;
            //    }
            //}

            //for (int w = 1; w < 9; w++)
            //{
            //    if (!newDataRecor.ContainsKey(w + ".channel" + w))//需要修改，丢失数据
            //    newDataRecor.Add(w + ".channel" + w, floats_BDF[w - 1]);
            //    floats_BDF[w - 1] = new List<double>();
            //}

            //Recording_BDF.DataRecords.Add(newDataRecor);

            for (int qqq = 0; qqq < Recording_BDF.Header.Signals.Count - 1; qqq++)
            {
                Recording_BDF.Header.Signals[qqq].TransducerType = TransducerType;
            }

            //最后一秒的数据不要了
            dataRecords_Stored = 0; 
            try
            {
                Recording_BDF.SyncHeader();
            }
            catch (IOException e)
            {
                FileError(this, new FailArgs(e, client_devicePath));
            }
        }

        public string BDFFilePath { get { return Recording_BDF.TheFilePath; } set { Recording_BDF.UseMemCache = false; Recording_BDF.TheFilePath = value + ".BDF"; } }
        public bool isSaving = false;


        public DateTime startSaveTime = DateTime.Now;

        public void startSaveBDF()
        {
            startSaveTime = DateTime.Now;
            Recording_BDF.Header.StartDateTime = startSaveTime;
            newDataRecor = new BDFDataRecord(startSaveTime, 0);
            isSaving = true;
            Interlocked.Exchange(ref misCountNum, 0);
            gotSerialCount = false;
            if (File.Exists(Recording_BDF.TheFilePath))
                try
                {
                    File.Delete(Recording_BDF.TheFilePath);
                }
                catch (IOException e)
                {
                    FileError(this, new FailArgs(e, client_devicePath));
                }
        }

        public void StopSaveBDF()
        {
            isSaving = false;
            if (!saved)
            {
                EndSaveBDF();
                saved = true;
            }
        }

        bool saved = true;
        public int getValueSigned3(byte a, byte b, byte c)
        {
            int low = 0, med = 0, high = 0;
            //low = b & 0x01 + b >> 1 & 0x01 * 2 + b >> 2 & 0x01 * 4 + b >> 3 & 0x01 * 8 + b >> 4 & 0x01 * 16
            int tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                low = low + (c >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                med = med + (b >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            med = med * 256;

            tmp = 1;
            for (int i = 0; i < 7; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            high = high * 256 * 256;

            if ((a >> 7 & 0x01) > 0)
            {
                return -(8388608 - high - med - low);

            }
            else
            {
                return high + med + low;
            }
        }
        public int getValueSigned(byte a, byte b)
        {
            int low = 0, high = 0;
            //low = b & 0x01 + b >> 1 & 0x01 * 2 + b >> 2 & 0x01 * 4 + b >> 3 & 0x01 * 8 + b >> 4 & 0x01 * 16
            int tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                low = low + (b >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            tmp = 1;
            for (int i = 0; i < 7; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            high = high * 256;


            if ((a >> 7 & 0x01) > 0)
            {
                return -(32768 - high - low);

            }
            else
            {
                return high + low;
            }
        }


        public int getValueUnSigned(byte a, byte b)
        {
            int low = 0, high = 0;
            //low = b & 0x01 + b >> 1 & 0x01 * 2 + b >> 2 & 0x01 * 4 + b >> 3 & 0x01 * 8 + b >> 4 & 0x01 * 16
            int tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                low = low + (b >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            high = high * 256;
            return high + low;
        }

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

        List<double>[] impedance = new List<double>[9];
        List<double>[] impedance_waveform = new List<double>[9];

        //byte[] toStart = new byte[6];
        //    toStart[0] = 0x55;
        //    toStart[1] = 0x00;
        //    toStart[2] = BitConverter.GetBytes(numOfPacksSent)[0];
        //    toStart[3] = 0x03;
        //    toStart[4] = 0x00;
        //    toStart[5] = (byte)(toStart[0] + toStart[1] + toStart[2] + toStart[3] + toStart[4]);
        //    client.Send(toStart);

        public void StartImpedanceDetect()
        {
            try
            {
                byte[] startTodetect = new byte[5];
                startTodetect[0] = 0x55;
                startTodetect[1] = 0x00;
                startTodetect[2] = 4;
                startTodetect[3] = 0x00;
                startTodetect[4] = (byte)(startTodetect[0] + startTodetect[1] + startTodetect[2] + startTodetect[3]);
                client.write(startTodetect);
                LogHelper.Log.LogDebug("发送开始阻抗检测命令");
            }
            catch (Exception ex)
            {
                isReceiving = false;
                isReceiving_ID = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                receiveFail(this, new FailArgs(new Exception($"开启阻抗检测错误:{ex.ToString()}"), client_devicePath));
                stop();

                GC.Collect();
            }
        }

        public void StopImpedanceDetect()
        {
            Interlocked.Exchange(ref misCountNum, 0);
            gotSerialCount = false;
            try
            {
                byte[] toStart = new byte[5];
                toStart[0] = 0x55;
                toStart[1] = 0x00;
                toStart[2] = 0;
                toStart[3] = 0x00;
                toStart[4] = (byte)(toStart[0] + toStart[1] + toStart[2] + toStart[3]);
                client.write(toStart);
                timer.IsEnabled = true;
                HeartBeatTM.IsEnabled = true;
                ImpedanceDone(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
                impedanceDone = true;
                LogHelper.Log.LogDebug("发送停止阻抗检测命令");
            }
            catch (Exception ex)
            {
                isReceiving = false;
                isReceiving = false;
                closed_identy = false;
                timer.IsEnabled = false;
                HeartBeatTM.IsEnabled = false;
                if (!saved)
                {
                    EndSaveBDF();
                    saved = true;
                }
                receiveFail(this, new FailArgs(new Exception($"开启阻抗检测错误:{ex.ToString()}"), client_devicePath));
                stop();
                GC.Collect();
            }
        }


        #region 设备连接搜索

        public delegate void confirmDevice(object sender, FailArgs e);
        public event confirmDevice validComFinish;

        bool closed_identy = false;
        string devicePath;
        DispatcherTimer timer_identy;

        #region 设备ID解析线程
        private void ToRcvData_ID()
        {
            int length = client.productInfo.IN_reportByteLength;

            int toCheck = 0;
            string ans = client.productInfo.product + "failed";
            byte CheckSum = 0x00;
            byte[] buf = new byte[client.productInfo.IN_reportByteLength];

            int nIndex = 0;
            while (isReceiving_ID)
            {
                try
                {
                    buf = client.read();
                }
                catch (IOException e)
                {
                    Console.WriteLine(e.ToString());
                    isReceiving_ID = false;
                    closed_identy = false;
                    timer.IsEnabled = false;
                    HeartBeatTM.IsEnabled = false;
                    if (!saved)
                    {
                        EndSaveBDF();
                        saved = true;
                    }
                    receiveFail(this, new FailArgs(new Exception($"接收数据中出现错误:{e.ToString()}"), client_devicePath));
                    stop();
                    GC.Collect();
                    break;
                }

                nIndex = 0;
                while (nIndex < length)
                {
                    while (nIndex < length && buf[nIndex] != 0x55)
                    {
                        nIndex++;
                    }
                    int DataLength = 0;
                    if (nIndex + 4 >= length)
                    {
                        nIndex++;
                        continue;
                    }
                    DataLength = getValue(buf[nIndex + 3]);
                    if (DataLength < 0)
                    {
                        nIndex++;
                        continue;
                    }
                    if (nIndex + DataLength + 4 >= length)
                    {
                        nIndex++;
                        continue;
                    }

                    CheckSum = 0x00;
                    toCheck = 0;

                    for (int i = 0; i < DataLength + 4; i++)
                    {
                        toCheck = toCheck + buf[nIndex + i];
                    }
                    CheckSum = (byte)toCheck;
                    if (CheckSum == buf[nIndex + 4 + DataLength])
                    {
                        //校验成功
                        byte[] validPack = new byte[DataLength + 5];
                        for (int i = 0; i < DataLength + 5; i++)
                        {
                            validPack[i] = buf[nIndex + i];
                        }
                        //解析一个完整的数据包
                        if (DataLength != 0 && validPack[2] == 9)
                        {
                            byte[] byteDeviceId = new byte[DataLength];
                            for (int q = 0; q < DataLength; q++)
                            {
                                byteDeviceId[q] = validPack[4 + q];
                            }
                            ans = devicePath + "," + System.Text.Encoding.ASCII.GetString(byteDeviceId);
                            validComFinish(this, new FailArgs(new Exception(), ans));
                            closed_identy = true;
                            nIndex = nIndex + DataLength + 5;
                            isReceiving_ID = false;
                            string ID = System.Text.Encoding.ASCII.GetString(byteDeviceId);
                            TransducerType = ID + "_Port";
                            LogHelper.Log.LogDebug($"搜索到设备ID:{ID}");
                        }
                        else
                        {
                            nIndex = nIndex + DataLength + 5;
                            Console.WriteLine("tried to parse a pack");
                        }
                    }
                    else
                    {
                        nIndex++;
                    }
                }
            }
        }
        #endregion


        public void check()
        {
            this.devicePath = client_devicePath;
            Thread t = new Thread(searchDeviceIdWorker);
            t.IsBackground = true;
            t.Start();
            closed_identy = false;

            timer_identy = new DispatcherTimer();
            timer_identy.Interval = TimeSpan.FromSeconds(5);
            timer_identy.Tick    += new EventHandler(toCleanUp);
            timer_identy.IsEnabled = true;
        }
        void searchDeviceIdWorker()
        {
            try
            {
                byte[] sendBuffer = new byte[5];
                sendBuffer[0] = 0x55;
                sendBuffer[1] = 0x00;
                sendBuffer[2] = 8;
                sendBuffer[3] = 0x00;
                sendBuffer[4] = (byte)(sendBuffer[0] + sendBuffer[1] + sendBuffer[2] + sendBuffer[3]);
                client.write(sendBuffer);
                LogHelper.Log.LogDebug($"向脑电设备发送设备ID获取指令:{string.Join(", ", sendBuffer.Select(b => $"0x{b:X2}"))}");

                isReceiving_ID = true;
                Thread recvIdThead       = new Thread(ToRcvData_ID);
                recvIdThead.IsBackground = true;
                recvIdThead.Start();
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError($"获取设备ID发生异常:{ex.ToString()}");
                connectFail(this, new FailArgs(ex, devicePath));
                closed_identy = false;
                GC.Collect();
            }
        }

        private void toCleanUp(object sender, EventArgs e)
        {
            if(null == client) 
                return;

            timer_identy.IsEnabled = false;
            isReceiving_ID = false;
            if (!closed_identy)
            {
                validComFinish(this, new FailArgs(new Exception(), devicePath + ",failed"));
                try
                {
                    client.close();
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError($"脑设备搜索资源清理发生异常 :{ex.ToString()}");
                }
                finally
                {
                    closed_identy = false;
                    client = null;
                    GC.Collect();
                }
            }
        }
        #endregion


        public void InsertEvent(string TheEvent)
        {
            if (isSaving)
                newDataRecor.addAnnotation(Recording_BDF.Header.DurationOfDataRecordInSeconds * dataRecords_Stored + indexOfArray / 1.0 / sampling_rate, "" + TheEvent);
            receive_event?.Invoke(this, new myEventArgs(new int[] { 0, 0, 0, 0 }));
        }

        //bool forwarding = false;
        //bool using_tcp_forward = false;
        //string forward_IP = "";
        //string forward_Port = "";
        //UdpClient UDPClient_forward;
        //TcpClient TCPClient_forward;

        public void SetForwardOption(string IP, string Port, bool usingTCP)
        {
            try
            {
                forward_IP = IPAddress.Parse(IP);
            }
            catch (Exception e)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(e, "IP地址错误"));
                stopForward();

            }
            int toInt = 0;
            try
            {
                toInt = int.Parse(Port);
            }
            catch (Exception e)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(e, "端口输入错误"));
                stopForward();
            }

            if (toInt < 1024 || toInt > 65534)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(new Exception("端口范围:1024-65534"), "端口范围:1024-65534"));
                stopForward();
                return;
            }
            forward_Port = toInt;
            using_tcp_forward = usingTCP;

        }
        public void startForward()
        {
            if (forward_IP == null || forward_Port < 1024)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(new Exception("IP或端口设置错误"), "IP或端口设置错误"));
                stopForward();
            }
            try
            {
                if (using_tcp_forward)
                {
                    TCPClient_forward = new TcpClient();
                    //forwarding = true;
                    TCPClient_forward.BeginConnect(forward_IP, forward_Port, new AsyncCallback(Connected), TCPClient_forward);
                }
                else
                {
                    UDPClient_forward = new UdpClient();

                    UDPClient_forward.Connect(new IPEndPoint(forward_IP, forward_Port));

                    forwarding = true;
                }
            }
            catch (Exception e)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(e, "转发申请失败，请重试"));
                stopForward();
            }
        }

        private void Connected(IAsyncResult ar)
        {
            try
            {
                TCPClient_forward = (TcpClient)ar.AsyncState;
                TCPClient_forward.EndConnect(ar);
                if (TCPClient_forward.Connected)
                {
                    forwarding = true;

                }
                else
                {
                    //Forward Fail
                    forwardFail(this, new FailArgs(new Exception("TCP连接失败，请重试"), "TCP连接失败，请重试"));
                    stopForward();
                }
            }
            catch (Exception e)
            {
                //Forward Fail
                forwardFail(this, new FailArgs(e, "TCP连接失败，请重试"));
                stopForward();
            }
        }

        public void stopForward()
        {
            if (forwarding)
            {
                forwarding = false;
                try
                {
                    if (using_tcp_forward && TCPClient_forward != null)
                        TCPClient_forward.Close();
                    else if (UDPClient_forward != null)
                        UDPClient_forward.Close();
                }
                catch (Exception e)
                {

                }
                finally
                {
                    using_tcp_forward = false;

                    forward_IP = null;
                    forward_Port = 0;


                    UDPClient_forward = null;
                    TCPClient_forward = null;

                    GC.Collect();
                }
            }
        }
    }
}
