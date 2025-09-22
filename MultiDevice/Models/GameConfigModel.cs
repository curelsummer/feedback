using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDevice
{
    public class GameConfigModel
    {
        public ParadigmSettingsModel paradigmSettings { get; set; }
        public string Game {  get; set; }
        public List<string> GameSequence { get; set; }
        public string SessionTotal { get; set; }
        public string SessionNum { get; set; }
        public string EpochCount { get; set; }
        public string EpochTimes { get; set; }
        public string BreakTimes { get; set; }
        // 当前的用户信息
        public UserInfoModel UserInfo { get; set; }
    }
}
