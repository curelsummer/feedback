using FluentFTP;
using HPSocket.Base;
using MultiDevice.DB;
using MultiDevice.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public class FTPClientHelperEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public FTPClientHelperEventArgs(double progress)
        {
            Progress = progress;
        }
    }

    public class FTPClientHelper
    {
        #region 类成员
        public event EventHandler<FTPClientHelperEventArgs> ProgressEvent;
        /// <summary>
        /// 
        /// </summary>
        private FtpClient ftpClient = null;
        private bool connectSuccess = false;
        private static FTPClientHelper instane = null;
        private static readonly object threadLocker = new object();
        #endregion

        private FTPClientHelper()
        {

        }


        public static FTPClientHelper Client
        {
            get
            {
                lock (threadLocker)
                {
                    if (null == instane)
                    {
                        instane = new FTPClientHelper();
                    }
                    return instane;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool connectToFtpServer()
        {
            if (null == ftpClient)
            {
                FtpConfig ftpConfig = new FtpConfig();
                ftpConfig.ConnectTimeout = 1000 * 60;
                ftpConfig.ReadTimeout    = 1000 * 60;
                // 设置为主动模式
                ftpConfig.DataConnectionType = FtpDataConnectionType.PORT;

                ftpClient = new FtpClient(NetHelper.Net.ServerIp, "vkeline", "123", 8890, ftpConfig);
            }

            if (connectSuccess)
            {
                return true;
            }

            // 连接到ftp server
            try
            {
                ftpClient.Connect();
                connectSuccess = true;
                LogHelper.Log.LogInfo(string.Format("连接到FtpServer成功:{0},{1}", "", 8890));
            }
            catch (Exception ex)
            {
                ftpClient.Dispose();
                ftpClient = null;
                LogHelper.Log.LogError(String.Format("连接到FtpServer发生异常 : {0}", ex.Message));
                return false;
            }
            return true;
        }

        public bool DisConnectToServer()
        {
            ftpClient.Disconnect();
            connectSuccess = false;
            ftpClient.Dispose();
            ftpClient = null;
            LogHelper.Log.LogInfo("退出ftp登录");
            return true;
        }
        public bool IsConnectSuccess()
        {
            return connectSuccess;
        }

        /// <summary>
        /// 文件是否存在
        /// </summary>
        /// <returns></returns>
        public bool FileExists(string fileName)
        {
            if (!connectSuccess)
            {
                return false;
            }

            return ftpClient.FileExists(fileName);
        }

        public bool DirectoryExists(string dirPath)
        {
            if (!connectSuccess)
            {
                return false;
            }

            return ftpClient.DirectoryExists(dirPath);
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public bool DeleteFile(string fileName)
        {
            if (!connectSuccess)
            {
                return false;
            }

            ftpClient.DeleteFile(fileName);
            return true;
        }

        public bool CreateDirectory(string dirPath)
        {
            if (!connectSuccess)
            {
                return false;
            }
            return ftpClient.CreateDirectory(dirPath);
        }

        // 进度更新的方法
        private void UpdateProgress(FtpProgress progress)
        {
            ProgressEvent?.Invoke(this, new FTPClientHelperEventArgs(progress.Progress));
        }

        public bool UploadFile(DateTime createTime, string fileName, string userName, ref string reomtePath)
        {
            try
            {
                if (!connectSuccess)
                {
                    return false;
                }

                if (!System.IO.File.Exists(fileName))
                {
                    LogHelper.Log.LogError(string.Format("ftp 文件上传:{0}路径不存在!", fileName));
                    return false;
                }
                string clientName = SQLiteDBService.DB.ReadSettings("App", "ClientName");

                DateTime start = DateTime.Now;
                string strFtpDir = "";
                if(clientName != "")
                {
                    strFtpDir = string.Format("{0}/{1}/{2}", clientName, createTime.ToString("yyyyMMddHHmmss"), userName);
                }
                else
                {
                    strFtpDir = string.Format("{0}/{1}", createTime.ToString("yyyyMMddHHmmss"), userName);
                }

                if (!DirectoryExists(strFtpDir))
                {
                    // 创建远端路径
                    if (!CreateDirectory(strFtpDir))
                    {
                        LogHelper.Log.LogError(string.Format("ftp 文件上传：{0},创建远端ftp路径失败: {1}", fileName, strFtpDir));
                        return false;
                    }
                    LogHelper.Log.LogInfo(string.Format("ftp 文件上传,远端路径创建成功:{0}", strFtpDir));
                }

                // 文件上传
                FileInfo fileInfo = new FileInfo(fileName);
                string strFtpFilePath = string.Format("{0}/{1}", strFtpDir, fileInfo.Name);
                reomtePath = strFtpFilePath;

                if (FileExists(strFtpFilePath))
                {
                    LogHelper.Log.LogInfo(string.Format("ftp 文件上传远端已存在文件:{0},执行删除", fileInfo.Name));
                    DeleteFile(strFtpFilePath);
                }


                FtpStatus uploadStatus = ftpClient.UploadFile(fileName, strFtpFilePath, FtpRemoteExists.Overwrite, true, FtpVerify.None, UpdateProgress);
                if (uploadStatus != FtpStatus.Success)
                {
                    LogHelper.Log.LogError(string.Format("ftp 文件上传{0},{1}失败:{2}", fileName, strFtpFilePath, uploadStatus.ToString()));
                    return false;
                }

                LogHelper.Log.LogInfo(string.Format("ftp 文件：{0},{1},{2}上传成功", fileName, strFtpFilePath, uploadStatus.ToString()));
                LogHelper.Log.LogInfo(string.Format("上传耗时：{0}ms", (DateTime.Now - start).TotalSeconds.ToString()));
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError(string.Format("ftp 文件上传发生异常 : {0},{1}", fileName, ex.ToString()));
                return false;
            }
            return true;
        }
    }
}
