using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastReport.DevComponents.DotNetBar;
using System.Windows.Documents;

namespace MultiDevice
{
    public class ParadigmSettingsModel
    {
        public Dictionary<string, string> GameList = new Dictionary<string, string>()
        {   { "电影院","Theater1" },
            { "方块旋转","Cube1"},
            { "汽车竞速", "RunningCar" },
            { "拔河比赛" ,"TugOfWar1"},
            { "飞向月球", "Moontravel1" },
            { "记忆扫描" ,"homepage"},
            { "观察萤火虫", "Firefly" },
            { "黑洞逃逸", "BlackHole"},
            { "安检", "AnJian"},
            { "察言观色", "Chayanguanse"},
            { "星际飞船", "Starship"},
            { "攀爬比赛", "Climb"},
            { "海洋之心", "Ocean"},
            { "喷火龙",   "Dragon"},
            /*{ "摩托竞速",  "Motor"},
            { "小鸟飞翔",  "Bird"},
            { "飞向火星", "Mars" },
            { "游艇竞速", "Boat" },
            { "沙漠绿植", "Desert" },
            { "制作灯笼", "Lamp" },
            { "小猴登云", "Monkey" },
            { "维护花园", "Flower" },
            { "蝴蝶飞飞", "Butterfly" },
            { "勇闯迷宫", "Maze" },
            { "小猫钓鱼", "Fish" },
            { "投篮比赛", "basketball" },
            { "游泳比赛", "Swim" },
            { "大胃王", "eat" },
            { "滑雪比赛", "Skate" },
            { "涂鸦比赛", "Paint" }*/

        };

        public string paradigmType    = "";
        public string[] signalsLabels = null;
        public int refMode            = 0;
        public List<string > gameList = new List<string>();
    }
}
