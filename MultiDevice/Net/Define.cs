using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice.Net
{
    // 通信命令
    public enum Cmd
    {
        Connect = 0,
        Heartbeat,
        Message,
        ShowTips,

        GetUserList,
        ShowUserList,
        InsertOrUpdateUser,
        DeleteUser,
        UpdateUserStatus,

        PrepareGame,
        StartGame,
        AbortGame,
        DesktopSharingStart,
        DesktopSharingEnd,
        // 游戏开始后往服务端发送的信号
        GamePrepare,
        GameStart,
        GameEnd,
        GameAbort,
        ResetUserStatus,
        ImpedanceMatchingSuccess,
        UploadResultFile,
        UploadResultFilePdf,
        GameProcess,
        UserLogin,

        GetParadigmTypeList,
        GetGameList,
        SaveReport,
        StartSave,
        AbortSave,
        CloseClient
    }

    public class NetData
    {
        public bool success {  get; set; }    
        public string cmd { get; set; }
        public string msg { get; set; } 
        public string data { get; set; }
        public string clientId   { get { return App.ClientId; } }    
        public string clientName { get{ return App.ClientName; } }

        public static string Return(bool success, Cmd cmd, string msg, string data = "")
        {
            NetData response = new NetData();
            response.success = success;
            response.cmd  = cmd.ToString();
            response.msg  = msg;
            response.data = data;
            string value  = JsonConvert.SerializeObject(response, Formatting.None);
            return value + "#";
        }

        public static NetData Parser(string data)
        {
            if (string.IsNullOrEmpty(data))
                return null;

            NetData response = null;
            try
            {
                response = JsonConvert.DeserializeObject<NetData>(data);
            }
            catch (Exception e)
            {
                LogHelper.Log.LogError($"数据包解析发生异常 :{e.ToString()}");
            }
            return response;
        }
    }
}
