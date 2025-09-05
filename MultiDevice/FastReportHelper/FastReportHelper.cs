using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastReport.Export.Pdf;
using FastReport.MSChart;
using FastReport;
using System.Windows.Forms.DataVisualization.Charting;

namespace MultiDevice
{
    public class FastReportHelper
    {
        public FastReportHelper()
        {

        }

        public string createReportData(UserInfoModel user, GameConfigModel gameConfig)
        {
            Dictionary<string, string> reportData = new Dictionary<string, string>();

            reportData["DetectNumber"] = user.DetectNumber;
            reportData["UserName"] = user.Name;
            reportData["UserSex"]  = user.Sex;
            reportData["UserAge"]  = user.Age;
            reportData["UserBrithDay"] = user.BirthDay;

            reportData["ParadigmType"] = gameConfig.paradigmSettings.paradigmType;
            // 装换游戏字段
            string game = "";
            foreach (var item in gameConfig.paradigmSettings.GameList)
            {
                if(item.Value == gameConfig.Game)
                {
                    game = item.Key;
                    break;
                }
            }
            reportData["Game"] = game;
            reportData["SessionTotal"] = gameConfig.SessionTotal;
            reportData["SessionNum"] = gameConfig.SessionNum;
            reportData["EpochCount"] = gameConfig.EpochCount;
            reportData["EpochTimes"] = gameConfig.EpochTimes;
            reportData["BreakTimes"] = gameConfig.BreakTimes;
            string channls = "";
            foreach(var vaule in gameConfig.paradigmSettings.signalsLabels)
            {
                if(vaule != "Nan")
                {
                    channls += vaule + ",";
                }
            }
            reportData["Channels"] = channls.TrimEnd(',');
            string refType = "";
            if (gameConfig.paradigmSettings.refMode == 0)
            {
                refType = "左耳";
            }
            else if(gameConfig.paradigmSettings.refMode == 1)
            {
                refType = "右耳";
            }
            else if (gameConfig.paradigmSettings.refMode == 2)
            {
                refType = "双耳平均";
            }
            else if (gameConfig.paradigmSettings.refMode == 3)
            {
                refType = "同侧";
            }
            else if (gameConfig.paradigmSettings.refMode == 4)
            {
                refType = "对侧";
            }
            else if (gameConfig.paradigmSettings.refMode == 5)
            {
                refType = "中央";
            }
            reportData["ReferenceType"] = refType;

            reportData["TestDate"]  = DateTime.Now.ToString("yyyy-MM-dd");
            reportData["TestTimes"] = $"{gameConfig.UserInfo.GameStartTime} 至 {gameConfig.UserInfo.GameEndTime}";

            // 计算注意力百分比
            float ValidTimesPercent = 0;
            float OtherTimesPercent = 100;
            float RealTotalTimes = 0;
            float ValidTimes = 0;
            float OtherTimes = 0;

            if (user.TotalPowerTimes > 0)
            {
                RealTotalTimes = user.TotalPowerTimes / 60.0f;
                ValidTimes     = user.ValidTimes / 60.0f;
                OtherTimes     = RealTotalTimes - ValidTimes;

                OtherTimesPercent = (OtherTimes / RealTotalTimes) * 100;
                ValidTimesPercent = 100 - OtherTimesPercent;
            }

            reportData["RealTotalTimes"] = string.Format("{0:F2}", RealTotalTimes);
            reportData["ValidTimes"] = string.Format("{0:F2}", ValidTimes);
            reportData["OtherTimes"] = string.Format("{0:F2}", OtherTimes);

            reportData["ValidTimesPercent"] = string.Format("{0:F1}", ValidTimesPercent);
            reportData["OtherTimesPercent"] = string.Format("{0:F1}", OtherTimesPercent);
            return printReport(reportData);
        }

        public string printReport(Dictionary<string, string> reportData = null)
        {
            Report report = new Report();
            report.Load(AppDomain.CurrentDomain.BaseDirectory + "ReportDesigner\\实验结果.frx");

            Dictionary<string, string> resultDic = new Dictionary<string, string>();
            if (reportData == null)
            {
                #region 测试数据
                resultDic["UserName"] = "钱瑾怡";
                resultDic["UserSex"] = "女";
                resultDic["UserAge"] = "5";
                resultDic["UserBrithDay"] = "2016-08-13";

                resultDic["ParadigmType"] = "alpha";
                resultDic["Game"] = "方块旋转";
                resultDic["SessionTotal"] = "1";
                resultDic["SessionNum"] = "1";
                resultDic["EpochCount"] = "6";
                resultDic["EpochTimes"] = "4";
                resultDic["BreakTimes"] = "2";
                resultDic["Channels"] = "A1、A2、Cz";
                resultDic["ReferenceType"] = "双耳平均";

                resultDic["TestDate"] = "2024-11-25";
                resultDic["TestTimes"] = "2023-04-13 15:30:00 至 2023-04-13 15:32:001";
                resultDic["RealTotalTimes"] = "100";
                resultDic["ValidTimes"] = "20";
                resultDic["OtherTimes"] = "80";

                resultDic["ValidTimesPercent"] = "40";
                resultDic["OtherTimesPercent"] = "60";
                #endregion
            }
            else
            {
                resultDic = reportData;
            }

            // 设置显示字段
            foreach (var pair in resultDic)
            {
                report.SetParameterValue(pair.Key, pair.Value);
            }

            if (resultDic.ContainsKey("ValidTimesPercent") &&
               resultDic.ContainsKey("OtherTimesPercent"))
            {
                // 设置饼图数据
                MSChartObject pieChart = report.FindObject("ResultChart") as MSChartObject;
                pieChart.Chart.Series[0].Points.Clear();


                DataPoint dataPoint1 = new DataPoint();
                dataPoint1.AxisLabel = string.Format("注意集中时间{0}%", resultDic["ValidTimesPercent"]);
                dataPoint1.YValues = new double[] { double.Parse(resultDic["ValidTimesPercent"]) };

                DataPoint dataPoint2 = new DataPoint();
                dataPoint2.AxisLabel = string.Format("其他时间{0}%", resultDic["OtherTimesPercent"]);
                dataPoint2.YValues = new double[] { double.Parse(resultDic["OtherTimesPercent"]) };

                pieChart.Chart.Series[0].Points.Add(dataPoint1);
                pieChart.Chart.Series[0].Points.Add(dataPoint2);
            }
            report.Prepare();
            // 存储记录(图片、PDF)
            string strPath = string.Format(".\\Data\\{0}_{1}.pdf",
                DateTime.Now.ToString("yyyyMMddHHmmss"), resultDic["UserName"]);
            PDFExport export = new PDFExport();
            report.Export(export, strPath);
            report.Dispose();
            return strPath;
        }
    }
}
