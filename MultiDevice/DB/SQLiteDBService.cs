using CYQ.Data;
using CYQ.Data.Table;
using MathNet.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MultiDevice.Net;
using System.Net.Http;
using SharpDX;


// 封装数据库操作方法
// by miaoxingxing
// 2024-09-10 14:29:40

namespace MultiDevice.DB
{
    public class SQLiteDBService
    {
        private static readonly SQLiteDBService _instance = new SQLiteDBService();
        public static SQLiteDBService DB
        {
            get
            {
                return _instance;
            }
        }


        #region 数据库初始化
        public SQLiteDBService()
        {
            string connectionString = string.Format("Data Source={0}AppDB\\AppDB.db;failifmissing=false", AppDomain.CurrentDomain.BaseDirectory);
            // 设置连接字符串
            CYQ.Data.AppConfig.SetConn("AppDB", connectionString);
            // 关闭数据库缓存
            CYQ.Data.AppConfig.AutoCache.IsEnable = false;
        }
        #endregion

        #region 患者信息添加相关数据库操作
        public void UpdateUserStatus(UserInfoModel person, string status)
        {
            if (App.IsConnectServer)
            {
                // 联网状态下将请求发送到服务端
                person.Status = status;
                NetHelper.Net.sendMessageToSever(true, Cmd.UpdateUserStatus, "更新服务端用户状态", JObject.FromObject(person).ToString(Formatting.None));
                return;
            }


            string where = string.Format("TestNumber = '{0}'", person.TestNumber);
            using (MAction action = new MAction("T_PersonInfo", "AppDB"))
            {
                action.Set("Status", status);
                if (action.Select(where).Rows.Count > 0)
                {
                    action.Update(where);
                }
                else
                {
                    action.Insert();
                }
            }
        }

        public string QueryUserStatus(string testNumber)
        {
            if (App.IsConnectServer)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();
                    // 联网状态下将请求发送到服务端
                    string url = $"http://{NetHelper.Net.ServerIp}:9898/api/queryUserStatus?TestNumber={testNumber}";
                    LogHelper.Log.LogDebug($"联网获取用户状态:{url}");
                    HttpResponseMessage response = httpClient.GetAsync(url).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    string reqData = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    JObject jsonRet = JObject.Parse(reqData);

                    if (jsonRet["success"].ToString() == "True")
                    {
                        return jsonRet["data"].ToString();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError($"获取用户状态发生异常 :{ex.ToString()}");
                }

                return "";
            }

            string where = string.Format("TestNumber = '{0}'", testNumber);
            using (MAction action = new MAction("T_PersonInfo", "AppDB"))
            {
                MDataTable dt = action.Select(where);
                if (dt.Rows.Count > 0)
                {
                    return dt.Rows[0]["Status"].ToString();
                }
            }
            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<UserInfoModel> QueryPersonList(string strWhere)
        {
            if (App.IsConnectServer)
            {
                // 联网状态下将请求发送到服务端
                JObject reqJson = new JObject();
                reqJson["Where"] = strWhere;
                NetHelper.Net.sendMessageToSever(true, Cmd.GetUserList, "向服务端请求用户列表", reqJson.ToString(Formatting.None));
                return null;
            }

            ObservableCollection<UserInfoModel> personInfos = new ObservableCollection<UserInfoModel>();
            using (MAction action = new MAction("T_PersonInfo", "AppDB"))
            {
                MDataTable table = action.Select(strWhere);
                if (table != null && table.Rows.Count > 0)
                {
                    personInfos = JsonConvert.DeserializeObject<ObservableCollection<UserInfoModel>>(JArray.FromObject(table.ToDataTable()).ToString());
                }
            }
            return personInfos;
        }

        /// <summary>
        /// 添加或修改患者数据
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        public bool InsertOrUpdatePerson(UserInfoModel person)
        {
            // 联网状态下将请求发送到服务端
            if (App.IsConnectServer)
            {
                NetHelper.Net.sendMessageToSever(true, Cmd.InsertOrUpdateUser, "创建或更新用户信息", JObject.FromObject(person).ToString(Formatting.None));
                return true;
            }

            string where = string.Format("TestNumber = '{0}'", person.TestNumber);
            using (MAction action = new MAction("T_PersonInfo", "AppDB"))
            {
                action.Set("TestNumber", person.TestNumber);
                action.Set("DetectNumber", person.DetectNumber);
                action.Set("Name", person.Name);
                action.Set("Sex", person.Sex);
                action.Set("BirthDay", person.BirthDay);
                action.Set("Remarks1", person.Remarks1);
                action.Set("Remarks2", person.Remarks2);
                action.Set("CreateDateTime", person.CreateDateTime);
                action.Set("Status", "未开始");
                if (action.Select(where).Rows.Count > 0)
                {
                    action.Update(where);
                }
                else
                {
                    action.Insert();
                }
            }

            return true;
        }

        /// <summary>
        /// 删除患者数据
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        public bool DeletePerson(UserInfoModel person)
        {
            // 联网状态下将请求发送到服务端
            if (App.IsConnectServer)
            {
                NetHelper.Net.sendMessageToSever(true, Cmd.DeleteUser, "删除用户信息", JObject.FromObject(person).ToString(Formatting.None));
                return true;
            }

            string where = string.Format("TestNumber = '{0}'", person.TestNumber);
            using (MAction action = new MAction("T_PersonInfo", "AppDB"))
            {
                if (action.Select(where).Rows.Count > 0)
                {
                    action.Delete(where);
                }
            }
            return true;
        }
        #endregion

        #region 客户端信息保存
        public void WriteSettings(string section, string key, string value)
        {
            string strWhere = string.Format("Section = '{0}' and Key = '{1}'", section, key);
            using(MAction action = new MAction("T_AppSettings", "AppDB"))
            {
                action.Set("Section", section);
                action.Set("Key", key);
                action.Set("Value", value);
                MDataTable dataTable = action.Select(strWhere);
                if(dataTable.Rows.Count > 0)
                {
                    action.Update(strWhere);
                }
                else
                {
                    action.Insert();
                }
            }
        }

        public string ReadSettings(string section, string key)
        {
            string strWhere = string.Format("Section = '{0}' and Key = '{1}'", section, key);
            using (MAction action = new MAction("T_AppSettings", "AppDB"))
            {
                MDataTable dataTable = action.Select(strWhere);
                if (dataTable.Rows.Count > 0)
                {
                    return dataTable.Rows[0]["Value"].ToString();
                }
            }

            return "";
        }

        public void RemoveSettings(string section, string key)
        {
            string strWhere = string.Format("Section = '{0}' and Key = '{1}'", section, key);
            using (MAction action = new MAction("T_AppSettings", "AppDB"))
            {
                MDataTable dataTable = action.Select(strWhere);
                if (dataTable.Rows.Count > 0)
                {
                    action.Delete(strWhere);    
                }
            }
        }

        public List<string > ReadConfigKeys(string section)
        {
            List<string> keys = new List<string>();

            string strWhere = string.Format("Section = '{0}'", section);
            using (MAction action = new MAction("T_AppSettings", "AppDB"))
            {
                MDataTable dataTable = action.Select(strWhere);
                if (dataTable.Rows.Count > 0)
                {
                    foreach(MDataRow dataRow in dataTable.Rows)
                    {
                        keys.Add(dataRow["Key"].ToString());
                    }
                }
            }
            return keys;
        }
        #endregion

        #region 用户登录相关操作
        public LogUserInfoModel GetUserInfo()
        {
            ObservableCollection<LogUserInfoModel> userInfos = new ObservableCollection<LogUserInfoModel>();
            using (MAction action = new MAction("T_User", "AppDB"))
            {
                MDataTable table = action.Select();
                if (table != null && table.Rows.Count > 0)
                {
                    userInfos = JsonConvert.DeserializeObject<ObservableCollection<LogUserInfoModel>>(JArray.FromObject(table.ToDataTable()).ToString());
                    return userInfos[0];
                }
           }

            return null;
        }

        public void ResetUserInfo()
        {
            string sql = "delete from T_User where 1=1";
            MProc proc = new MProc(sql, "AppDB");
            proc.ExeNonQuery();
        }

        public void UpdateLoginUser(LogUserInfoModel logUserInfoModel)
        {
            ResetUserInfo();
            using (MAction action = new MAction("T_User", "AppDB"))
            {
                action.Set("UserName", logUserInfoModel.UserName);
                action.Set("Password", logUserInfoModel.Password);
                action.Set("UserRole", logUserInfoModel.UserRole.ToString());
                action.Insert();
            }
        }
        #endregion
    }
}
