using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastReport.Export.Pdf;
using FastReport.MSChart;
using FastReport;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using BDF;
using FastReport.Utils;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

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
            return printReport(reportData, null);
        }

        public string createReportData(UserInfoModel user, GameConfigModel gameConfig, string bdfFilePath)
        {
            Dictionary<string, string> reportData = new Dictionary<string, string>();

            reportData["DetectNumber"] = user.DetectNumber;
            reportData["UserName"] = user.Name;
            reportData["UserSex"]  = user.Sex;
            reportData["UserAge"]  = user.Age;
            reportData["UserBrithDay"] = user.BirthDay;

            reportData["ParadigmType"] = gameConfig.paradigmSettings.paradigmType;
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

            reportData["BdfPath"] = bdfFilePath ?? string.Empty;
            reportData["ExportTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            reportData["EpochLenSec"] = gameConfig.EpochTimes;
            reportData["EpochOverlapPercent"] = "-";
            reportData["ArtifactPercent"] = "N/A";
            reportData["RewardCount"] = "N/A";
            try
            {
                var asmVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                reportData["SoftwareVersion"] = asmVer != null ? asmVer.ToString() : "";
            }
            catch { reportData["SoftwareVersion"] = ""; }

            // 从BDF头读取采样率与预滤波信息
            if (!string.IsNullOrWhiteSpace(bdfFilePath) && File.Exists(bdfFilePath))
            {
                try
                {
                    BDFFile bdf = new BDFFile(bdfFilePath);
                    bdf.readHeader();
                    var sig = bdf.Header.Signals.FirstOrDefault(s => !string.Equals(s.Label?.Trim(), BDFSignal.NotationLabel, StringComparison.OrdinalIgnoreCase));
                    if (sig != null)
                    {
                        double fs = 0;
                        if (sig.NumberOfSamplesPerDataRecord > 0 && bdf.Header.DurationOfDataRecordInSeconds > 0)
                            fs = sig.NumberOfSamplesPerDataRecord / bdf.Header.DurationOfDataRecordInSeconds;
                        if (fs <= 0) fs = 250;
                        reportData["SamplingRate"] = string.Format("{0:F0} Hz", fs);

                        string pre = sig.Prefiltering;
                        if (!string.IsNullOrWhiteSpace(pre))
                        {
                            reportData["Prefiltering"] = pre;
                        }
                        else
                        {
                            reportData["Prefiltering"] = "-";
                        }
                    }
                }
                catch
                {
                    reportData["SamplingRate"] = "-";
                    reportData["Prefiltering"] = "-";
                }
            }
            else
            {
                reportData["SamplingRate"] = "-";
                reportData["Prefiltering"] = "-";
            }
            return printReport(reportData, bdfFilePath);
        }

        public string printReport(Dictionary<string, string> reportData = null, string bdfFilePath = null)
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
                dataPoint1.AxisLabel = string.Format("训练状态良好时间{0}%", resultDic["ValidTimesPercent"]);
                dataPoint1.YValues = new double[] { double.Parse(resultDic["ValidTimesPercent"]) };

                DataPoint dataPoint2 = new DataPoint();
                dataPoint2.AxisLabel = string.Format("其他时间{0}%", resultDic["OtherTimesPercent"]);
                dataPoint2.YValues = new double[] { double.Parse(resultDic["OtherTimesPercent"]) };

                pieChart.Chart.Series[0].Points.Add(dataPoint1);
                pieChart.Chart.Series[0].Points.Add(dataPoint2);
            }
            // 确保版式与对象存在（若 FRX 未放置对象则按A4横向自动摆放）
            LogHelper.Log.LogDebug("[Report] EnsureTemplateLayout start");
            EnsureTemplateLayout(report);

            // 从BDF填充图表：EEG时序、PSD、频带功率、专注趋势
            try
            {
                string resolvedBdf = bdfFilePath;
                if (string.IsNullOrWhiteSpace(resolvedBdf) && resultDic.ContainsKey("BdfPath"))
                {
                    resolvedBdf = resultDic["BdfPath"];
                }
                LogHelper.Log.LogDebug($"[Report] Resolved BDF path: '{resolvedBdf}', exists={( !string.IsNullOrWhiteSpace(resolvedBdf) && File.Exists(resolvedBdf) )}");
                if (!string.IsNullOrWhiteSpace(resolvedBdf) && File.Exists(resolvedBdf))
                {
                    PopulateChartsFromBdf(report, resolvedBdf);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] PopulateChartsFromBdf error: " + ex.Message);
            }
            // 按模板导出（保留原有内容）；图片已通过 ChartsPage 的 PictureObject 嵌入
            report.Prepare();
            try { LogHelper.Log.LogDebug($"[Report] PreparedPages count (template): {report.PreparedPages?.Count ?? -1}"); } catch {}
            string strPath = string.Format(".\\Data\\{0}_{1}.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"), resultDic["UserName"]);
            PDFExport export = new PDFExport();
            report.Export(export, strPath);
            report.Dispose();
            try
            {
                string direct = ExportPdfDirect(resultDic, bdfFilePath);
                if (!string.IsNullOrEmpty(direct))
                {
                    LogHelper.Log.LogDebug($"[Report] Also exported images PDF: {direct}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] ExportPdfDirect post-export error: " + ex.Message);
            }
            return strPath;
        }

        private float mm(float v)
        {
            return Units.Millimeters * v;
        }

        private void EnsureTemplateLayout(Report report)
        {
            // 始终新增一个独立页面用于图表（ChartsPage），不改动现有首页
            ReportPage chartsPage = null;
            foreach (PageBase p in report.Pages)
            {
                if (p is ReportPage rp && string.Equals(rp.Name, "ChartsPage", StringComparison.OrdinalIgnoreCase))
                {
                    chartsPage = rp;
                    break;
                }
            }
            if (chartsPage == null)
            {
                chartsPage = new ReportPage();
                chartsPage.Name = "ChartsPage";
                chartsPage.Landscape = true;
                chartsPage.PaperWidth = 297;
                chartsPage.PaperHeight = 210;
                chartsPage.TopMargin = 5;
                chartsPage.BottomMargin = 5;
                chartsPage.LeftMargin = 5;
                chartsPage.RightMargin = 5;
                report.Pages.Add(chartsPage);
            }

            // 使用 DataBand(RowCount=1) 强制触发渲染，最稳妥
            BandBase targetBand = null;
            DataBand dataBand = null;
            foreach (BandBase b in chartsPage.Bands)
            {
                if (b is DataBand db)
                {
                    dataBand = db;
                    break;
                }
            }
            if (dataBand == null)
            {
                dataBand = new DataBand();
                dataBand.Height = mm(190);
                dataBand.RowCount = 1; // 关键：没有数据也打印一行
                dataBand.Visible = true;
                dataBand.Printable = true;
                chartsPage.Bands.Add(dataBand);
            }
            else
            {
                dataBand.RowCount = Math.Max(1, dataBand.RowCount);
                dataBand.Visible = true;
                dataBand.Printable = true;
            }
            targetBand = dataBand;
            LogHelper.Log.LogDebug($"[Report] ChartsPage using band: DataBand, RowCount={dataBand.RowCount}");

            // 在 ChartsPage 上放置图片占位（避免 MSChart 在 PDF 渲染缺失）
            // 确保四个图片对象存在
            if (report.FindObject("AttentionTrendChartPic") == null)
            {
                PictureObject pic = new PictureObject();
                pic.Name = "AttentionTrendChartPic";
                pic.Visible = true;
                pic.Printable = true;
                targetBand.Objects.Add(pic);
            }
            if (report.FindObject("BandPowerChartPic") == null)
            {
                PictureObject pic = new PictureObject();
                pic.Name = "BandPowerChartPic";
                pic.Visible = true;
                pic.Printable = true;
                targetBand.Objects.Add(pic);
            }
            if (report.FindObject("PSDChartPic") == null)
            {
                PictureObject pic = new PictureObject();
                pic.Name = "PSDChartPic";
                pic.Visible = true;
                pic.Printable = true;
                targetBand.Objects.Add(pic);
            }
            if (report.FindObject("EEGChartPic") == null)
            {
                PictureObject pic = new PictureObject();
                pic.Name = "EEGChartPic";
                pic.Visible = true;
                pic.Printable = true;
                targetBand.Objects.Add(pic);
            }
            // 统一设置四宫格位置与大小（无论对象是否已存在）
            try
            {
                var trendPic = report.FindObject("AttentionTrendChartPic") as PictureObject;
                var bandPic  = report.FindObject("BandPowerChartPic") as PictureObject;
                var psdPic   = report.FindObject("PSDChartPic") as PictureObject;
                var eegPic   = report.FindObject("EEGChartPic") as PictureObject;
                if (trendPic != null) trendPic.Bounds = new System.Drawing.RectangleF(mm(10),  mm(0),  mm(135), mm(75));
                if (psdPic   != null) psdPic.Bounds   = new System.Drawing.RectangleF(mm(152), mm(0),  mm(135), mm(75));
                if (bandPic  != null) bandPic.Bounds  = new System.Drawing.RectangleF(mm(10),  mm(85), mm(135), mm(75));
                if (eegPic   != null) eegPic.Bounds   = new System.Drawing.RectangleF(mm(152), mm(85), mm(135), mm(75));
                LogHelper.Log.LogDebug("[Report] Four pictures layout set to 2x2 grid (mm)");
            }
            catch { }
            try
            {
                var names = new[] { "AttentionTrendChartPic", "BandPowerChartPic", "PSDChartPic", "EEGChartPic" };
                foreach (var n in names)
                {
                    var obj = report.FindObject(n) as PictureObject;
                    if (obj == null)
                    {
                        LogHelper.Log.LogDebug($"[Report] PictureObject missing after ensure: {n}");
                    }
                    else
                    {
                        LogHelper.Log.LogDebug($"[Report] PictureObject ensured: {n}, Bounds={obj.Bounds}");
                    }
                }
            }
            catch { }

            // 不再在图表页添加任何文本，避免与图片布局冲突
        }

        private void PopulateChartsFromBdf(Report report, string bdfFilePath)
        {
            LogHelper.Log.LogDebug("[Report] PopulateChartsFromBdf begin: " + bdfFilePath);
            BDFFile bdf = new BDFFile(bdfFilePath);
            // 尽量完整读取，避免头部记录数异常导致读不到数据
            bool readOk = false;
            for (int attempt = 0; attempt < 3 && !readOk; attempt++)
            {
                try
                {
                    bdf.readFile();
                    readOk = bdf.DataRecords != null && bdf.DataRecords.Count > 0;
                    if (!readOk)
                    {
                        LogHelper.Log.LogDebug($"[Report] readFile attempt {attempt+1} got 0 records, retry...");
                        System.Threading.Thread.Sleep(150);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError($"[Report] readFile attempt {attempt+1} threw: {ex.Message}, retry...");
                    System.Threading.Thread.Sleep(150);
                }
            }
            LogHelper.Log.LogDebug($"[Report] DataRecords count: {bdf.DataRecords?.Count ?? 0}, Signals: {bdf.Header?.Signals?.Count ?? 0}");

            // 选择第一个非注释信号
            var signal = bdf.Header.Signals.FirstOrDefault(s => !string.Equals(s.Label?.Trim(), BDFSignal.NotationLabel, StringComparison.OrdinalIgnoreCase));
            if (signal == null) { LogHelper.Log.LogDebug("[Report] No non-annotation signal found"); return; }
            LogHelper.Log.LogDebug("[Report] Using signal: " + signal.Label);

            // 聚合样本
            List<double> samples = new List<double>();
            foreach (var dr in bdf.DataRecords)
            {
                if (dr.Signals.ContainsKey(signal.IndexNumberWithLabel))
                {
                    samples.AddRange(dr.Signals[signal.IndexNumberWithLabel]);
                }
            }
            if (samples.Count == 0) { LogHelper.Log.LogDebug("[Report] samples count == 0, abort"); return; }

            double fs = 0;
            if (signal.NumberOfSamplesPerDataRecord > 0 && bdf.Header.DurationOfDataRecordInSeconds > 0)
            {
                fs = signal.NumberOfSamplesPerDataRecord / bdf.Header.DurationOfDataRecordInSeconds;
            }
            if (fs <= 0) fs = 250; // 回退一个常见采样率
            LogHelper.Log.LogDebug($"[Report] fs={fs}, samples={samples.Count}");

            // EEG 时序：降采样至最多1000点
            int maxPointsTs = 1000;
            int stepTs = Math.Max(1, samples.Count / maxPointsTs);
            // 丢弃前5秒的时序数据，并从0秒重新计时
            int startIdxTs = (int)Math.Max(0, Math.Min(samples.Count, fs * 5));
            // 对齐到取样步长，避免边界抖动
            if (startIdxTs % stepTs != 0) startIdxTs += (stepTs - (startIdxTs % stepTs));
            double tTs = 0;
            double dtTs = 1.0 / fs * stepTs;
            var xsTs = new List<double>();
            var ysTs = new List<double>();
            for (int i = startIdxTs; i < samples.Count; i += stepTs)
            {
                xsTs.Add(tTs);
                ysTs.Add(samples[i]);
                tTs += dtTs;
            }
            var eegChart = (report.FindObject("EEGChart") as MSChartObject) ?? (report.FindObject("EEGChart_Page") as MSChartObject);
            if (eegChart != null)
            {
                if (eegChart.Chart.Series.Count == 0)
                {
                    eegChart.Chart.Series.Add(new Series("Series1") { ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line });
                }
                eegChart.Chart.Series[0].Points.Clear();
                for (int i = 0; i < xsTs.Count; i++)
                {
                    var p = new DataPoint();
                    p.XValue = xsTs[i];
                    p.YValues = new double[] { ysTs[i] };
                    eegChart.Chart.Series[0].Points.Add(p);
                }
                LogHelper.Log.LogDebug($"[Report] EEGChart points: {eegChart.Chart.Series[0].Points.Count}");
            }
            else LogHelper.Log.LogDebug("[Report] EEGChart object not found");

            // 专注趋势：用1秒窗口的均方根作为简化指标
            // 专注趋势：用1秒窗口的均方根作为简化指标
            var xsTrend = new List<double>();
            var ysTrend = new List<double>();
            int win = (int)Math.Max(1, fs);
            // 丢弃前5秒数据
            int idxWin = win * 5;
            int secWin = 0;
            while (idxWin + win <= samples.Count)
            {
                double rms = 0;
                for (int j = 0; j < win; j++)
                {
                    double v = samples[idxWin + j];
                    rms += v * v;
                }
                rms = Math.Sqrt(rms / win);
                xsTrend.Add(secWin);
                ysTrend.Add(rms);
                idxWin += win;
                secWin += 1;
            }
            var trendChart = (report.FindObject("AttentionTrendChart") as MSChartObject) ?? (report.FindObject("AttentionTrendChart_Page") as MSChartObject);
            if (trendChart != null)
            {
                if (trendChart.Chart.Series.Count == 0)
                {
                    trendChart.Chart.Series.Add(new Series("Series1") { ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line });
                }
                trendChart.Chart.Series[0].Points.Clear();
                for (int i = 0; i < xsTrend.Count; i++)
                {
                    var p = new DataPoint();
                    p.XValue = xsTrend[i];
                    p.YValues = new double[] { ysTrend[i] };
                    trendChart.Chart.Series[0].Points.Add(p);
                }
                LogHelper.Log.LogDebug($"[Report] AttentionTrendChart points: {trendChart.Chart.Series[0].Points.Count}");
            }
            else LogHelper.Log.LogDebug("[Report] AttentionTrendChart object not found");

            // PSD 计算（使用最近的2^m长度）
            int n = Math.Min(samples.Count, 2048);
            int m = 1;
            int pow2 = 2;
            while (pow2 < n && m < 16)
            {
                pow2 <<= 1;
                m++;
            }
            n = pow2;
            if (n > samples.Count) n = samples.Count;
            double[] xr = new double[n];
            double[] xi = new double[n];
            for (int i = 0; i < n; i++) xr[i] = samples[i];
            FFTLibrary.MyComplex.FFT(1, m, xr, xi);

            // 频谱只取正频部分 (0-50Hz) 并换算为µV²/Hz
            int half = n / 2;
            var psdXs = new List<double>();
            var psdYs = new List<double>();
            double df = fs / n;
            for (int k = 0; k < half; k++)
            {
                double freq = (double)k * fs / n;
                if (freq > 50) break;
                double power = (xr[k] * xr[k] + xi[k] * xi[k]) / (df <= 0 ? 1 : df); // µV²/Hz
                psdXs.Add(freq);
                psdYs.Add(power);
            }
            var psdChart = (report.FindObject("PSDChart") as MSChartObject) ?? (report.FindObject("PSDChart_Page") as MSChartObject);
            if (psdChart != null)
            {
                if (psdChart.Chart.Series.Count == 0)
                {
                    psdChart.Chart.Series.Add(new Series("Series1") { ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line });
                }
                psdChart.Chart.Series[0].Points.Clear();
                for (int i = 0; i < psdXs.Count; i++)
                {
                    var p = new DataPoint();
                    p.XValue = psdXs[i];
                    p.YValues = new double[] { psdYs[i] };
                    psdChart.Chart.Series[0].Points.Add(p);
                }
                LogHelper.Log.LogDebug($"[Report] PSDChart points: {psdChart.Chart.Series[0].Points.Count}");
            }
            else LogHelper.Log.LogDebug("[Report] PSDChart object not found");

            // 频带功率（1-45Hz）
            double[] freqsArr = new double[half];
            double[] powersArr = new double[half];
            for (int k = 0; k < half; k++)
            {
                freqsArr[k] = (double)k * fs / n;
                powersArr[k] = xr[k] * xr[k] + xi[k] * xi[k];
            }
            Func<double, double, double> bandSumF = (f1, f2) =>
            {
                double sum = 0;
                for (int k = 0; k < half; k++)
                {
                    if (freqsArr[k] >= f1 && freqsArr[k] < f2) sum += powersArr[k];
                }
                return sum;
            };
            double totalPow = bandSumF(1, 45);
            if (totalPow <= 0) totalPow = 1;
            var bandsDef = new (string name, double f1, double f2)[]
            {
                ("Delta", 1, 4),
                ("Theta", 4, 8),
                ("Alpha", 8, 13),
                ("SMR", 12, 15),
                ("Beta", 13, 30),
                ("Gamma", 30, 45)
            };
            string[] bandLabels = bandsDef.Select(b => b.name).ToArray();
            double[] bandValues = bandsDef.Select(b => bandSumF(b.f1, b.f2) / totalPow * 100.0).ToArray();
            var bandChart = (report.FindObject("BandPowerChart") as MSChartObject) ?? (report.FindObject("BandPowerChart_Page") as MSChartObject);
            if (bandChart != null)
            {
                if (bandChart.Chart.Series.Count == 0)
                {
                    bandChart.Chart.Series.Add(new Series("Series1") { ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column });
                }
                bandChart.Chart.Series[0].Points.Clear();
                for (int i = 0; i < bandLabels.Length; i++)
                {
                    var p = new DataPoint();
                    p.AxisLabel = bandLabels[i];
                    p.YValues = new double[] { bandValues[i] };
                    bandChart.Chart.Series[0].Points.Add(p);
                }
                LogHelper.Log.LogDebug($"[Report] BandPowerChart points: {bandChart.Chart.Series[0].Points.Count}");
            }
            else LogHelper.Log.LogDebug("[Report] BandPowerChart object not found");

            // 兜底：将三张图与波形导出为 PNG 到 Data/ReportImages/{BDF文件名}/ 下，并填充 PictureObject
            try
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(bdfFilePath);
                string tmpDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportImages", baseName);
                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);

                // 趋势图（中文命名）
                var trendPic = report.FindObject("AttentionTrendChartPic") as PictureObject;
                if (trendPic != null)
                {
                    string p = System.IO.Path.Combine(tmpDir, "训练专注趋势.png");
                    var trendSummary = new List<string>();
                    if (ysTrend.Count > 0)
                    {
                        trendSummary.Add($"最大值: {ysTrend.Max():0.00}");
                        trendSummary.Add($"最小值: {ysTrend.Min():0.00}");
                        trendSummary.Add($"均值: {ysTrend.Average():0.00}");
                    }
                    List<Tuple<double,double,string>> trendHighlights = null;
                    if (ysTrend.Count > 0)
                    {
                        int idxMax = ysTrend.IndexOf(ysTrend.Max());
                        int idxMin = ysTrend.IndexOf(ysTrend.Min());
                        trendHighlights = new List<Tuple<double,double,string>>
                        {
                            Tuple.Create(xsTrend[idxMax], ysTrend[idxMax], $"峰值 {ysTrend[idxMax]:0.00}"),
                            Tuple.Create(xsTrend[idxMin], ysTrend[idxMin], $"谷值 {ysTrend[idxMin]:0.00}")
                        };
                    }
                    SaveLineChartPng(p, xsTrend.ToArray(), ysTrend.ToArray(), "时间 (s)", "专注指数", "训练专注趋势", trendHighlights, trendSummary);
                    SetPictureObjectImage(trendPic, p);
                }

                // 频带柱状（中文命名）
                var bandPic = report.FindObject("BandPowerChartPic") as PictureObject;
                if (bandPic != null)
                {
                    string p = System.IO.Path.Combine(tmpDir, "频带功率.png");
                    var sum = new List<string>();
                    for (int i = 0; i < bandLabels.Length; i++) sum.Add($"{bandLabels[i]}: {bandValues[i]:0.0}%");
                    SaveBarChartPng(p, bandLabels, bandValues, "相对功率 (%)", "频带功率", sum);
                    SetPictureObjectImage(bandPic, p);
                }

                // PSD（中文命名）
                var psdPic = report.FindObject("PSDChartPic") as PictureObject;
                if (psdPic != null)
                {
                    string p = System.IO.Path.Combine(tmpDir, "平均功率谱密度.png");
                    // 按你的要求：不显示全局峰值摘要
                    var psdSummary = new List<string>();
                    // 仅标注 Alpha(8-13Hz) 与 Beta(13-30Hz) 的峰值
                    List<Tuple<double,double,string>> psdHighlights = new List<Tuple<double,double,string>>();
                    if (psdYs.Count > 0)
                    {
                        // Alpha峰
                        double alphaMax = double.MinValue; int aidx = -1;
                        for (int i = 0; i < psdXs.Count; i++)
                        {
                            if (psdXs[i] >= 8 && psdXs[i] <= 13 && psdYs[i] > alphaMax) { alphaMax = psdYs[i]; aidx = i; }
                        }
                        if (aidx >= 0) psdHighlights.Add(Tuple.Create(psdXs[aidx], psdYs[aidx], $"Alpha峰 {psdXs[aidx]:0.0}Hz: {psdYs[aidx]:0.00} µV²/Hz"));
                        // Beta峰
                        double betaMax = double.MinValue; int bidx = -1;
                        for (int i = 0; i < psdXs.Count; i++)
                        {
                            if (psdXs[i] >= 13 && psdXs[i] <= 30 && psdYs[i] > betaMax) { betaMax = psdYs[i]; bidx = i; }
                        }
                        if (bidx >= 0) psdHighlights.Add(Tuple.Create(psdXs[bidx], psdYs[bidx], $"Beta峰 {psdXs[bidx]:0.0}Hz: {psdYs[bidx]:0.00} µV²/Hz"));
                    }
                    // 频段着色提示（不改变数据）
                    var regions = new List<Tuple<double,double,string,Color>>
                    {
                        Tuple.Create(1.0, 4.0,  "δ (1–4)",   Color.LightBlue),
                        Tuple.Create(4.0, 8.0,  "θ (4–8)",   Color.LightGreen),
                        Tuple.Create(8.0, 13.0, "α (8–13)",  Color.LightYellow),
                        Tuple.Create(12.0,15.0, "SMR(12–15)",Color.LightPink),
                        Tuple.Create(13.0,30.0, "β (13–30)", Color.LightCoral),
                        Tuple.Create(30.0,50.0, "γ (30–50)", Color.LightGray)
                    };
                    SaveLineChartPng(p, psdXs.ToArray(), psdYs.ToArray(), "频率 (Hz)", "功率谱密度 (µV²/Hz)", "平均功率谱密度", psdHighlights, psdSummary, regions);
                    SetPictureObjectImage(psdPic, p);
                }

                // EEG（中文命名）
                var eegPic = report.FindObject("EEGChartPic") as PictureObject;
                if (eegPic != null)
                {
                    string p = System.IO.Path.Combine(tmpDir, "脑电时序波形.png");
                    var eegSummary = new List<string>();
                    if (ysTs.Count > 0)
                    {
                        eegSummary.Add($"最大幅值: {ysTs.Max():0.00} µV");
                        eegSummary.Add($"最小幅值: {ysTs.Min():0.00} µV");
                    }
                    SaveLineChartPng(p, xsTs.ToArray(), ysTs.ToArray(), "时间 (s)", "幅值 (µV)", "脑电时序波形", null, eegSummary);
                    SetPictureObjectImage(eegPic, p);
                }

                // 基于 message_sent 日志生成 神经反馈状态曲线 并填充趋势位
                try
                {
                    string nfPng = GenerateNFStateCurveFromLogInternal(tmpDir, bdfFilePath);
                    if (!string.IsNullOrWhiteSpace(nfPng) && File.Exists(nfPng))
                    {
                        trendPic = report.FindObject("AttentionTrendChartPic") as PictureObject;
                        if (trendPic != null)
                        {
                            SetPictureObjectImage(trendPic, nfPng);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError("[Report] NF state curve render error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] Fallback picture render error: " + ex.Message);
            }
        }

        private void SetPictureObjectImage(PictureObject pic, string path)
        {
            try
            {
                if (pic == null || string.IsNullOrWhiteSpace(path)) return;
                bool exists = File.Exists(path);
                LogHelper.Log.LogDebug($"[Report] SetPicture: name={pic.Name}, path='{path}', exists={exists}");
                if (exists)
                {
                    using (var bmp = new Bitmap(path))
                    {
                        var clone = new Bitmap(bmp);
                        pic.Image = clone;
                        LogHelper.Log.LogDebug($"[Report] SetPicture OK: {pic.Name}, imgSize={clone.Width}x{clone.Height}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError($"[Report] SetPicture error ({pic?.Name}): {ex.Message}");
            }
        }

        // 直接生成 PDF 并嵌入 PNG 图片，不依赖模板或 MSChart
        private string ExportPdfDirect(Dictionary<string, string> resultDic, string bdfFilePath)
        {
            try
            {
                // 计算图片路径
                string baseName = null;
                if (!string.IsNullOrWhiteSpace(bdfFilePath))
                    baseName = System.IO.Path.GetFileNameWithoutExtension(bdfFilePath);
                if (string.IsNullOrWhiteSpace(baseName)) return null;

                string imgDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportImages", baseName);
                string pNF    = System.IO.Path.Combine(imgDir, "神经反馈状态曲线.png");
                string pTrend = System.IO.Path.Combine(imgDir, "训练专注趋势.png");
                string pBand  = System.IO.Path.Combine(imgDir, "频带功率.png");
                string pPsd   = System.IO.Path.Combine(imgDir, "平均功率谱密度.png");
                string pEeg   = System.IO.Path.Combine(imgDir, "脑电时序波形.png");
                bool ok = File.Exists(pTrend) || File.Exists(pBand) || File.Exists(pPsd) || File.Exists(pEeg);
                LogHelper.Log.LogDebug($"[Report] ExportPdfDirect check images: trend={File.Exists(pTrend)}, band={File.Exists(pBand)}, psd={File.Exists(pPsd)}, eeg={File.Exists(pEeg)}");
                if (!ok) return null; // 无图则回退

                Report rpt = new Report();
                // 单页横向 A4，放文本+四图
                ReportPage page = new ReportPage
                {
                    Name = "DirectPage",
                    Landscape = true,
                    PaperWidth = 297,
                    PaperHeight = 210
                };
                rpt.Pages.Add(page);

                // 在无数据源场景下，使用 DataBand(RowCount=1) 强制打印
                page.TopMargin = 5; page.BottomMargin = 5; page.LeftMargin = 5; page.RightMargin = 5;
                DataBand band = new DataBand { Height = mm(190), Visible = true, Printable = true, RowCount = 1 };
                page.Bands.Add(band);

                // 直出页面不放标题，确保四图尺寸一致且不被挤占

                // 直出页面不再显示任何文本，避免影响四宫格布局

                // 图片四宫格
                void AddPic(string name, string path, float xMm, float yMm, float wMm, float hMm)
                {
                    if (!File.Exists(path)) return;
                    var pic = new PictureObject { Name = name, Bounds = new System.Drawing.RectangleF(mm(xMm), mm(yMm), mm(wMm), mm(hMm)) };
                    pic.Visible = true;
                    pic.Printable = true;
                    try
                    {
                        using (var bmp = new Bitmap(path)) pic.Image = new Bitmap(bmp);
                    }
                    catch { pic.ImageLocation = path; }
                    band.Objects.Add(pic);
                    try { LogHelper.Log.LogDebug($"[Report] Direct AddPic: {name}, Bounds={pic.Bounds}, exists={true}"); } catch {}
                }

                // 统一四宫格布局：左上、右上、左下、右下，尺寸一致
                AddPic("TrendPic", File.Exists(pNF) ? pNF : pTrend, 10,  0,   135, 75);
                AddPic("PsdPic",   pPsd,   152, 0,   135, 75);
                AddPic("BandPic",  pBand,  10,  85,  135, 75);
                AddPic("EegPic",   pEeg,   152, 85,  135, 75);

                rpt.Prepare();
                try { LogHelper.Log.LogDebug($"[Report] PreparedPages count (direct): {rpt.PreparedPages?.Count ?? -1}"); } catch {}
                string outPath = string.Format(".\\Data\\{0}_{1}_direct.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"), resultDic.ContainsKey("UserName") ? resultDic["UserName"] : "Report");
                PDFExport exp = new PDFExport();
                rpt.Export(exp, outPath);
                rpt.Dispose();
                LogHelper.Log.LogDebug($"[Report] ExportPdfDirect done: {outPath}");
                return outPath;
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] ExportPdfDirect failed: " + ex.Message);
                return null;
            }
        }

        private void SaveLineChartPng(
            string path,
            double[] xs,
            double[] ys,
            string xLabel = null,
            string yLabel = null,
            string title = null,
            List<Tuple<double, double, string>> highlights = null,
            List<string> summary = null,
            List<Tuple<double, double, string, Color>> regions = null)
        {
            if (xs == null || ys == null || xs.Length == 0 || ys.Length == 0) return;
            int w = 900, h = 500, lm = 50, bm = 40, rm = 15, tm = 15;
            using (var bmp = new Bitmap(w, h))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                var penAxis = new Pen(Color.Gray, 1);
                var penLine = new Pen(Color.RoyalBlue, 1.5f);
                g.DrawRectangle(penAxis, lm, tm, w - lm - rm, h - tm - bm);
                double xmin = xs.Min(), xmax = xs.Max();
                double ymin = ys.Min(), ymax = ys.Max();
                if (Math.Abs(xmax - xmin) < 1e-9) { xmax = xmin + 1; }
                if (Math.Abs(ymax - ymin) < 1e-9) { ymax = ymin + 1; }
                PointF map(double x, double y)
                {
                    float X = (float)(lm + (x - xmin) / (xmax - xmin) * (w - lm - rm));
                    float Y = (float)(h - bm - (y - ymin) / (ymax - ymin) * (h - tm - bm));
                    return new PointF(X, Y);
                }
                // 频段着色区域
                if (regions != null && regions.Count > 0)
                {
                    foreach (var rg in regions)
                    {
                        double f1 = rg.Item1, f2 = rg.Item2;
                        var fill = Color.FromArgb(48, rg.Item4); // 提高背景不透明度
                        float X1 = (float)(lm + (Math.Max(f1, xmin) - xmin) / (xmax - xmin) * (w - lm - rm));
                        float X2 = (float)(lm + (Math.Min(f2, xmax) - xmin) / (xmax - xmin) * (w - lm - rm));
                        var rect = new RectangleF(Math.Min(X1, X2), tm, Math.Abs(X2 - X1), h - tm - bm);
                        using (var br = new SolidBrush(fill)) g.FillRectangle(br, rect);
                        // 标注标签
                        string label = rg.Item3;
                        if (!string.IsNullOrEmpty(label) && rect.Width > 35)
                        {
                            var sf = new StringFormat { Alignment = StringAlignment.Center };
                            using (var fnt = new Font(SystemFonts.DefaultFont, FontStyle.Bold))
                                g.DrawString(label, fnt, Brushes.DimGray, rect.X + rect.Width / 2, tm + 2, sf);
                        }
                    }
                }
                PointF? prev = null;
                for (int i = 0; i < xs.Length; i++)
                {
                    var pt = map(xs[i], ys[i]);
                    if (prev != null) g.DrawLine(penLine, prev.Value, pt);
                    prev = pt;
                }
                // 轴刻度与标签
                DrawAxesAndTicks(g, w, h, lm, rm, tm, bm, xmin, xmax, ymin, ymax, xLabel, yLabel, title);
                // 关键点标注
                if (highlights != null)
                {
                    foreach (var hp in highlights)
                    {
                        var pt = map(hp.Item1, hp.Item2);
                        var text = hp.Item3 ?? "";
                        var size = g.MeasureString(text, SystemFonts.DefaultFont);
                        var rect = new RectangleF(pt.X + 6, pt.Y - size.Height - 4, size.Width + 6, size.Height + 4);
                        g.FillRectangle(Brushes.White, rect);
                        g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                        g.DrawString(text, SystemFonts.DefaultFont, Brushes.Black, rect.X + 3, rect.Y + 2);
                        g.FillEllipse(Brushes.OrangeRed, pt.X - 3, pt.Y - 3, 6, 6);
                    }
                }
                // 概要数值
                if (summary != null && summary.Count > 0)
                {
                    string block = string.Join("\n", summary);
                    var size = g.MeasureString(block, SystemFonts.DefaultFont);
                    var rect = new RectangleF(lm + 6, tm + 6, size.Width + 10, size.Height + 10);
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(block, SystemFonts.DefaultFont, Brushes.Black, rect.X + 5, rect.Y + 5);
                }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 解析 *_message_sent.txt 并生成 神经反馈状态曲线 图像，返回 PNG 路径；如失败返回 null
        private string GenerateNFStateCurveFromLogInternal(string imgDir, string bdfFilePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bdfFilePath) || !File.Exists(bdfFilePath)) return null;
                string sessionDir = Path.GetDirectoryName(bdfFilePath);
                if (string.IsNullOrWhiteSpace(sessionDir) || !Directory.Exists(sessionDir)) return null;
                // 优先 127.0.0.1，其次任意 *_message_sent.txt
                string logPath = Path.Combine(sessionDir, "127.0.0.1_message_sent.txt");
                if (!File.Exists(logPath))
                {
                    var cand = Directory.GetFiles(sessionDir, "*_message_sent.txt").FirstOrDefault();
                    if (cand == null) return null;
                    logPath = cand;
                }

                List<string> lines = new List<string>();
                try { lines = File.ReadAllLines(logPath, Encoding.Default).ToList(); }
                catch { lines = File.ReadAllLines(logPath, Encoding.UTF8).ToList(); }
                if (lines.Count == 0) return null;

                // 正则
                Regex rxEvent = new Regex(@"^(.+?):customer_message:[^:]*:(fr_(\d+)_(reward_obtained|penalization_obtained|stay_still):([\-\d\.eE\+]+))$", RegexOptions.Compiled);
                Regex rxBase  = new Regex(@"^(.+?):customer_message:[^:]*:(fr_(\d+)_(mean|std):([\-\d\.eE\+]+))$", RegexOptions.Compiled);
                Regex rxEpoch = new Regex(@"^(.+?):customer_message:[^:]*:(epoch_nb_(\d+)_start)$", RegexOptions.Compiled);

                var mean = new Dictionary<int, double>();
                var std  = new Dictionary<int, double>();
                var events = new List<(DateTime t, int k, string type, double x)>();
                var epochTs = new List<DateTime>();
                DateTime? t0 = null;
                foreach (var line in lines)
                {
                    try
                    {
                        var mBase = rxBase.Match(line);
                        if (mBase.Success)
                        {
                            DateTime tt; if (!TryParseTs(mBase.Groups[1].Value, out tt)) continue;
                            if (t0 == null) t0 = tt;
                            int k = int.Parse(mBase.Groups[3].Value);
                            string what = mBase.Groups[4].Value;
                            double val = double.Parse(mBase.Groups[5].Value);
                            if (what == "mean") mean[k] = val; else if (what == "std") std[k] = val;
                            continue;
                        }
                        var mEvt = rxEvent.Match(line);
                        if (mEvt.Success)
                        {
                            DateTime tt; if (!TryParseTs(mEvt.Groups[1].Value, out tt)) continue;
                            if (t0 == null) t0 = tt;
                            int k = int.Parse(mEvt.Groups[3].Value);
                            string type = mEvt.Groups[4].Value; // reward_obtained / penalization_obtained / stay_still
                            double x = double.Parse(mEvt.Groups[5].Value);
                            events.Add((tt, k, type, x));
                            continue;
                        }
                        var mEp = rxEpoch.Match(line);
                        if (mEp.Success)
                        {
                            DateTime tt; if (TryParseTs(mEp.Groups[1].Value, out tt))
                            {
                                if (t0 == null) t0 = tt;
                                epochTs.Add(tt);
                            }
                            continue;
                        }
                    }
                    catch { }
                }
                if (events.Count == 0) return null;
                events.Sort((a,b)=> a.t.CompareTo(b.t));
                DateTime tStart = t0 ?? events.First().t;
                DateTime tEnd   = events.Last().t;
                double totalSec = Math.Max(1e-3, (tEnd - tStart).TotalSeconds);

                // 阈值
                var thetaHigh = new Dictionary<int, double>();
                var thetaLow  = new Dictionary<int, double>();
                double zHigh = 1.0, zLow = 1.0;
                var allK = events.Select(e=>e.k).Distinct().ToList();
                foreach (var k in allK)
                {
                    var xsK = events.Where(e=>e.k==k).Select(e=>e.x).ToList();
                    double m = mean.ContainsKey(k) ? mean[k] : (xsK.Count>0 ? xsK.Average() : 0);
                    double s = std.ContainsKey(k)  ? std[k]  : (StdDev(xsK));
                    if (!mean.ContainsKey(k) || !std.ContainsKey(k))
                    {
                        // 分位数兜底
                        if (xsK.Count >= 5)
                        {
                            double q20 = Quantile(xsK, 0.2);
                            double q80 = Quantile(xsK, 0.8);
                            thetaLow[k]  = q20;
                            thetaHigh[k] = q80;
                        }
                        else
                        {
                            thetaHigh[k] = m + zHigh * s;
                            thetaLow[k]  = m - zLow  * s;
                        }
                    }
                    else
                    {
                        thetaHigh[k] = m + zHigh * s;
                        thetaLow[k]  = m - zLow  * s;
                    }
                }

                // 采样
                double fs = 10.0; // 10Hz
                int N = Math.Max(2, (int)Math.Ceiling(totalSec * fs) + 1);
                double dt = 1.0 / fs;
                double[] xsT = new double[N];
                double[] yS  = new double[N];
                // 每个 k 的 S_k 与 x_k(t) 以及状态序列
                var S = new Dictionary<int, double>();
                var Xhold = new Dictionary<int, double>();
                var Xseries = new Dictionary<int, double[]>();
                var stateSeriesByK = new Dictionary<int, int[]>();
                double alpha = 1.0, beta = 1.0, lambda = 0.2, Smax = 100.0;
                double tauEma = 1.2; // s
                double emaA = 1 - Math.Exp(-dt / Math.Max(0.2, tauEma));
                int ei = 0;
                // 状态：reward=+1, stay=0, penal=-1
                var stateK = new Dictionary<int, int>();
                int[] domState = new int[N];
                for (int i = 0; i < N; i++)
                {
                    double tSec = i * dt; xsT[i] = tSec;
                    DateTime ti = tStart.AddSeconds(tSec);
                    // 衰减
                    var keysK = allK;
                    foreach (var k in keysK)
                    {
                        if (!S.ContainsKey(k)) S[k] = 0;
                        S[k] = S[k] * Math.Exp(-lambda * dt);
                        if (!Xhold.ContainsKey(k)) Xhold[k] = 0;
                        if (!stateSeriesByK.ContainsKey(k)) stateSeriesByK[k] = new int[N];
                    }
                    // 处理该采样点之前到达的事件（可能有多个）
                    while (ei < events.Count && events[ei].t <= ti)
                    {
                        var ev = events[ei];
                        if (!S.ContainsKey(ev.k)) S[ev.k] = 0;
                        if (ev.type == "reward_obtained")
                        {
                            double inc = Math.Max(0, ev.x - thetaHigh[ev.k]);
                            S[ev.k] += alpha * inc;
                            stateK[ev.k] = 1;
                        }
                        else if (ev.type == "penalization_obtained")
                        {
                            double inc = Math.Max(0, thetaLow[ev.k] - ev.x);
                            S[ev.k] -= beta * inc;
                            stateK[ev.k] = -1;
                        }
                        else if (ev.type == "stay_still") { stateK[ev.k] = 0; }
                        // ZOH 更新 x_k
                        Xhold[ev.k] = ev.x;
                        // stay_still 不改变 S
                        ei++;
                    }
                    // 限幅并汇总
                    double sum = 0;
                    foreach (var k in keysK)
                    {
                        if (S[k] >  Smax) S[k] =  Smax;
                        if (S[k] < -Smax) S[k] = -Smax;
                        sum += S[k];
                        // 记录 x_k(t)
                        if (!Xseries.ContainsKey(k)) Xseries[k] = new double[N];
                        Xseries[k][i] = Xhold[k];
                        // 记录 state_k(t)
                        int cur = 0; if (stateK.ContainsKey(k)) cur = stateK[k];
                        stateSeriesByK[k][i] = cur;
                    }
                    yS[i] = sum;
                    // 主导状态：按 Σ stateK 判定
                    int ssum = 0; foreach (var kv in stateK) ssum += kv.Value;
                    domState[i] = ssum > 0 ? 1 : (ssum < 0 ? -1 : 0);
                }

                // EMA 平滑每个 k 的 Xseries
                foreach (var k in allK)
                {
                    var arr = Xseries[k];
                    double prev = arr.Length>0 ? arr[0] : 0;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        prev = prev + emaA * (arr[i] - prev);
                        arr[i] = prev;
                    }
                }

                // 概要
                int nReward = events.Count(e=>e.type=="reward_obtained");
                int nPenal  = events.Count(e=>e.type=="penalization_obtained");
                int nStay   = events.Count(e=>e.type=="stay_still");
                var summary = new List<string>{ $"奖励次数: {nReward}", $"惩罚次数: {nPenal}", $"静止次数: {nStay}", $"S_total峰值: {yS.Max():0.00}" };

                // epoch 竖线（相对秒）
                var epochSecs = epochTs.Select(t => (t - tStart).TotalSeconds).Where(v=> v>=0 && v<=totalSec).ToList();

                // 将累计反馈归一化为分数 [-100, 100]，便于用户理解
                double denom = Smax * Math.Max(1, allK.Count);
                double[] yScore = new double[N];
                for (int i = 0; i < N; i++)
                {
                    double v = 100.0 * yS[i] / denom;
                    if (v > 100) v = 100; if (v < -100) v = -100;
                    yScore[i] = v;
                }

                // 名称映射：k→中文
                var labelByK = new Dictionary<int, string>();
                var sortedK = allK.OrderBy(v=>v).ToList();
                if (sortedK.Count >= 1) labelByK[sortedK[0]] = "驱动值";
                if (sortedK.Count >= 2) labelByK[sortedK[1]] = "伪差值";
                foreach (var k in allK) if (!labelByK.ContainsKey(k)) labelByK[k] = $"k={k}";

                // 输出（复合绘图：三层）
                if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);
                string outP = Path.Combine(imgDir, "神经反馈状态曲线.png");
                SaveNFStateCurveCompositePng(outP, xsT, yScore, domState, Xseries, stateSeriesByK, thetaLow, thetaHigh, events, epochSecs, summary, labelByK);
                return outP;
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] GenerateNFStateCurveFromLogInternal failed: " + ex.Message);
                return null;
            }
        }

        private bool TryParseTs(string ts, out DateTime dt)
        {
            // 支持 yyyy_MM_d_HH_mm_ss_fff 与 yyyy_MM_dd_HH_mm_ss_fff
            string[] fmts = new[]{ "yyyy_MM_d_HH_mm_ss_fff", "yyyy_MM_dd_HH_mm_ss_fff" };
            foreach (var f in fmts)
            {
                if (DateTime.TryParseExact(ts, f, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
                    return true;
            }
            // 回退普通解析
            return DateTime.TryParse(ts.Replace('_','-').Replace('.',':'), out dt);
        }

        private double StdDev(IEnumerable<double> seq)
        {
            var arr = seq.ToArray();
            if (arr.Length == 0) return 0;
            double mean = arr.Average();
            double sum = 0; foreach (var v in arr) { double d = v - mean; sum += d * d; }
            return Math.Sqrt(sum / arr.Length);
        }

        private double Quantile(List<double> list, double q)
        {
            if (list == null || list.Count == 0) return 0;
            var arr = list.OrderBy(v=>v).ToArray();
            if (q <= 0) return arr.First(); if (q >= 1) return arr.Last();
            double pos = (arr.Length - 1) * q;
            int lo = (int)Math.Floor(pos); int hi = (int)Math.Ceiling(pos);
            if (lo == hi) return arr[lo];
            double frac = pos - lo;
            return arr[lo] * (1 - frac) + arr[hi] * frac;
        }

        // 复合绘图：上 S_total 分段着色；中 x_k 与阈值带；下 事件刻度条；含 epoch 竖线
        private void SaveNFStateCurveCompositePng(
            string path,
            double[] t,
            double[] sTotal,
            int[] domState,
            Dictionary<int, double[]> xSeriesByK,
            Dictionary<int, int[]> stateSeriesByK,
            Dictionary<int, double> thetaLow,
            Dictionary<int, double> thetaHigh,
            List<(DateTime t, int k, string type, double x)> events,
            List<double> epochSecs,
            List<string> summary,
            Dictionary<int, string> labelByK)
        {
            if (t == null || sTotal == null || t.Length == 0 || sTotal.Length == 0) return;
            int w = 1200, h = 600; int lm = 60, rm = 20, tm = 20, bm = 40; int gap = 10;
            // 三层高度分配
            int hTop = 200, hMid = 230, hBot = 120;
            using (var bmp = new Bitmap(w, h))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                float X(double tt, double xMinBound, double xMaxBound) => (float)(lm + (tt - xMinBound) / (xMaxBound - xMinBound) * (w - lm - rm));
                float Y(float boxTop, int boxH, double yy, double ymin, double ymax)
                {
                    if (Math.Abs(ymax - ymin) < 1e-9) ymax = ymin + 1;
                    return (float)(boxTop + boxH - (yy - ymin) / (ymax - ymin) * boxH);
                }

                double xmin = t.Min(), xmax = t.Max(); if (Math.Abs(xmax - xmin) < 1e-9) xmax = xmin + 1;

                // 1) 顶部：S_total 分段着色
                int topY = tm;
                var rectTop = new RectangleF(lm, topY, w - lm - rm, hTop);
                g.DrawRectangle(Pens.Gray, rectTop.X, rectTop.Y, rectTop.Width, rectTop.Height);
                double yminS = sTotal.Min(), ymaxS = sTotal.Max(); if (Math.Abs(ymaxS - yminS) < 1e-6) { ymaxS = yminS + 1; }

                Pen penReward = new Pen(Color.FromArgb(220, 46, 204, 113), 2.0f); // 绿
                Pen penStay   = new Pen(Color.FromArgb(220, 127, 140, 141), 2.0f); // 灰
                Pen penPenal  = new Pen(Color.FromArgb(220, 231, 76, 60),  2.0f); // 红

                for (int i = 1; i < t.Length; i++)
                {
                    var x1 = X(t[i-1], xmin, xmax); var x2 = X(t[i], xmin, xmax);
                    var y1 = Y(topY, hTop, sTotal[i-1], yminS, ymaxS); var y2 = Y(topY, hTop, sTotal[i], yminS, ymaxS);
                    int st = domState != null && domState.Length > i ? domState[i] : 0;
                    var pen = st > 0 ? penReward : (st < 0 ? penPenal : penStay);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }

                // 顶部图例：颜色含义（左上角）
                try
                {
                    int lgx = lm + 12;
                    int lgy = topY + 8;
                    int gapY = 18;
                    // 奖励
                    using (var p = new Pen(penReward.Color, 3f)) g.DrawLine(p, lgx, lgy + 4, lgx + 24, lgy + 4);
                    g.DrawString("奖励 (reward)", SystemFonts.DefaultFont, Brushes.Black, lgx + 30, lgy - 4);
                    // 静止
                    using (var p = new Pen(penStay.Color, 3f)) g.DrawLine(p, lgx, lgy + gapY + 4, lgx + 24, lgy + gapY + 4);
                    g.DrawString("静止 (stay)", SystemFonts.DefaultFont, Brushes.Black, lgx + 30, lgy + gapY - 4);
                    // 惩罚
                    using (var p = new Pen(penPenal.Color, 3f)) g.DrawLine(p, lgx, lgy + gapY*2 + 4, lgx + 24, lgy + gapY*2 + 4);
                    g.DrawString("惩罚 (penal)", SystemFonts.DefaultFont, Brushes.Black, lgx + 30, lgy + gapY*2 - 4);
                }
                catch {}

                // 竖线：epoch
                using (var penEpoch = new Pen(Color.FromArgb(100, Color.Black), 1))
                {
                    penEpoch.DashStyle = DashStyle.Dash;
                    foreach (var es in epochSecs)
                    {
                        float x = X(es, xmin, xmax);
                        g.DrawLine(penEpoch, x, topY, x, topY + hTop + gap + hMid + gap + hBot);
                    }
                }

                // 轴与标签（将 y 轴固定为分数区间 -100~100）
                double yMinFixed = -100, yMaxFixed = 100;
                DrawAxesAndTicks(g, w, topY + hTop + 20, lm, rm, topY, bm, xmin, xmax, yMinFixed, yMaxFixed, "时间 (s)", "综合反馈分数", "神经反馈状态曲线");

                // 2) 中部：x_k(t) 与阈值带（同时绘制图例说明两条曲线）
                int midY = topY + hTop + gap;
                var rectMid = new RectangleF(lm, midY, w - lm - rm, hMid);
                g.DrawRectangle(Pens.Gray, rectMid.X, rectMid.Y, rectMid.Width, rectMid.Height);

                // 合并所有 x 范围
                double yminX = double.PositiveInfinity, ymaxX = double.NegativeInfinity;
                foreach (var kv in xSeriesByK)
                {
                    if (kv.Value != null && kv.Value.Length > 0)
                    {
                        yminX = Math.Min(yminX, kv.Value.Min());
                        ymaxX = Math.Max(ymaxX, kv.Value.Max());
                    }
                }
                if (double.IsNaN(yminX) || double.IsInfinity(yminX) || double.IsNaN(ymaxX) || double.IsInfinity(ymaxX) || Math.Abs(ymaxX - yminX) < 1e-9) { yminX = 0; ymaxX = 1; }

                // 阈值带（每个 k 各一条，半透明）
                Random rnd = new Random(42);
                foreach (var k in xSeriesByK.Keys.OrderBy(v=>v))
                {
                    Color baseC = Color.FromArgb(60, (byte)rnd.Next(30,200), (byte)rnd.Next(30,200), (byte)rnd.Next(30,200));
                    if (thetaLow.ContainsKey(k) && thetaHigh.ContainsKey(k))
                    {
                        float yL = Y(midY, hMid, thetaLow[k], yminX, ymaxX);
                        float yH = Y(midY, hMid, thetaHigh[k], yminX, ymaxX);
                        var rect = new RectangleF(lm, Math.Min(yL,yH), w - lm - rm, Math.Abs(yH - yL));
                        using (var br = new SolidBrush(baseC)) g.FillRectangle(br, rect);
                    }
                }
                // x_k 折线（细线）。若恰好有两个 k，则分别用深灰、蓝色，以增强区分度并在右上添加图例：
                foreach (var kv in xSeriesByK.OrderBy(p=>p.Key))
                {
                    int k = kv.Key; var xs = kv.Value; if (xs == null || xs.Length != t.Length) continue;
                    Color c;
                    if (xSeriesByK.Count == 2)
                    {
                        c = (kv.Key == xSeriesByK.Keys.OrderBy(v=>v).First()) ? Color.FromArgb(200, 60, 60, 60) : Color.FromArgb(200, 70, 90, 200);
                    }
                    else
                    {
                        c = Color.FromArgb(160, (k*73)%255, (k*29)%255, (k*191)%255);
                    }
                    using (var pen = new Pen(c, 1.2f))
                    {
                        PointF? prev = null;
                        for (int i = 0; i < t.Length; i++)
                        {
                            var pt = new PointF(X(t[i], xmin, xmax), Y(midY, hMid, xs[i], yminX, ymaxX));
                            if (prev != null) g.DrawLine(pen, prev.Value, pt);
                            prev = pt;
                        }
                    }
                }

                // 图例：说明两条曲线代表不同的自由度 k
                if (xSeriesByK.Count >= 1)
                {
                    int legendX = (int)(w - rm - 200);
                    int legendY = midY + 8;
                    int lh = 18; int idx = 0;
                    foreach (var kv in xSeriesByK.OrderBy(p=>p.Key))
                    {
                        Color c;
                        if (xSeriesByK.Count == 2)
                        {
                            c = (kv.Key == xSeriesByK.Keys.OrderBy(v=>v).First()) ? Color.FromArgb(200, 60, 60, 60) : Color.FromArgb(200, 70, 90, 200);
                        }
                        else
                        {
                            c = Color.FromArgb(160, (kv.Key*73)%255, (kv.Key*29)%255, (kv.Key*191)%255);
                        }
                        using (var br = new SolidBrush(c)) g.FillRectangle(br, legendX, legendY + idx*lh, 20, 6);
                        string label = (labelByK != null && labelByK.ContainsKey(kv.Key)) ? labelByK[kv.Key] : ($"k={kv.Key}");
                        g.DrawString($"{label}", SystemFonts.DefaultFont, Brushes.Black, legendX + 28, legendY + idx*lh - 4);
                        idx++;
                    }
                    // 阈值带说明
                    g.DrawString("半透明色带：被试的阈值区", SystemFonts.DefaultFont, Brushes.Black, legendX, legendY + idx*lh + 4);
                }

                // 3) 底部：事件刻度条（每个 k 一行）
                int botY = midY + hMid + gap;
                var rectBot = new RectangleF(lm, botY, w - lm - rm, hBot);
                g.DrawRectangle(Pens.Gray, rectBot.X, rectBot.Y, rectBot.Width, rectBot.Height);
                int rowH = Math.Max(12, (hBot - 10) / Math.Max(1, xSeriesByK.Count));
                int rowIdx = 0;
                foreach (var k in xSeriesByK.Keys.OrderBy(v=>v))
                {
                    int yRowTop = botY + 5 + rowIdx * rowH;
                    // 行标签（中文映射）
                    string rowLabel = (labelByK != null && labelByK.ContainsKey(k)) ? labelByK[k] : ($"k={k}");
                    g.DrawString(rowLabel, SystemFonts.DefaultFont, Brushes.Black, 5, yRowTop);
                    // 使用每个 k 的状态序列绘制颜色条（绿=reward, 灰=stay, 红=penal），避免两行相同
                    if (stateSeriesByK != null && stateSeriesByK.ContainsKey(k))
                    {
                        var ss = stateSeriesByK[k];
                        for (int i = 1; i < t.Length && i < ss.Length; i++)
                        {
                            var x1 = X(t[i-1], xmin, xmax); var x2 = X(t[i], xmin, xmax);
                            int st = ss[i];
                            Brush br = st > 0 ? Brushes.LightGreen : (st < 0 ? Brushes.LightCoral : Brushes.LightGray);
                            var rr = new RectangleF(x1, yRowTop + 2, Math.Max(1, x2 - x1), rowH - 4);
                            g.FillRectangle(br, rr);
                        }
                    }
                    rowIdx++;
                }

                // 概要框
                if (summary != null && summary.Count > 0)
                {
                    string block = string.Join("\n", summary);
                    var size = g.MeasureString(block, SystemFonts.DefaultFont);
                    var rect = new RectangleF(w - rm - size.Width - 20, tm + 10, size.Width + 14, size.Height + 12);
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(block, SystemFonts.DefaultFont, Brushes.Black, rect.X + 7, rect.Y + 6);
                }

                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void SaveBarChartPng(string path, string[] labels, double[] values, string yLabel = null, string title = null, List<string> summary = null)
        {
            if (labels == null || values == null || labels.Length == 0 || values.Length == 0) return;
            int w = 900, h = 500, lm = 50, bm = 60, rm = 15, tm = 15;
            using (var bmp = new Bitmap(w, h))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                var penAxis = new Pen(Color.Gray, 1);
                var brushBar = new SolidBrush(Color.SteelBlue);
                g.DrawRectangle(penAxis, lm, tm, w - lm - rm, h - tm - bm);
                double ymax = Math.Max(1e-6, values.Max());
                int n = labels.Length;
                float barW = (float)(w - lm - rm) / (n * 1.5f);
                for (int i = 0; i < n; i++)
                {
                    float x = lm + i * barW * 1.5f + barW * 0.25f;
                    float barH = (float)(values[i] / ymax * (h - tm - bm));
                    g.FillRectangle(brushBar, x, h - bm - barH, barW, barH);
                    var sf = new StringFormat { Alignment = StringAlignment.Center };
                    g.DrawString(labels[i], SystemFonts.DefaultFont, Brushes.Black, x + barW / 2, h - bm + 5, sf);
                    // 顶部数值
                    g.DrawString(values[i].ToString("0.0"), SystemFonts.DefaultFont, Brushes.Black, x + barW / 2, h - bm - barH - 16, sf);
                }
                DrawAxesAndTicks(g, w, h, lm, rm, tm, bm, 0, n - 1, 0, ymax, null, yLabel ?? "值", title);
                if (summary != null && summary.Count > 0)
                {
                    string block = string.Join("\n", summary);
                    var size = g.MeasureString(block, SystemFonts.DefaultFont);
                    var rect = new RectangleF(lm + 6, tm + 6, size.Width + 10, size.Height + 10);
                    g.FillRectangle(Brushes.White, rect);
                    g.DrawRectangle(Pens.Gray, rect.X, rect.Y, rect.Width, rect.Height);
                    g.DrawString(block, SystemFonts.DefaultFont, Brushes.Black, rect.X + 5, rect.Y + 5);
                }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private void DrawAxesAndTicks(Graphics g, int w, int h, int lm, int rm, int tm, int bm,
            double xmin, double xmax, double ymin, double ymax,
            string xLabel, string yLabel, string title)
        {
            // 网格线
            using (var gridPen = new Pen(Color.LightGray, 1))
            {
                gridPen.DashStyle = DashStyle.Dot;
                for (int i = 1; i <= 5; i++)
                {
                    float y = tm + i * (h - tm - bm) / 6f;
                    g.DrawLine(gridPen, lm, y, w - rm, y);
                }
                for (int i = 1; i <= 5; i++)
                {
                    float x = lm + i * (w - lm - rm) / 6f;
                    g.DrawLine(gridPen, x, tm, x, h - bm);
                }
            }
            // 轴标签
            if (!string.IsNullOrEmpty(title))
                g.DrawString(title, new Font(SystemFonts.DefaultFont, FontStyle.Bold), Brushes.Black, w / 2 - 60, 2);
            if (!string.IsNullOrEmpty(xLabel))
                g.DrawString(xLabel, SystemFonts.DefaultFont, Brushes.Black, w / 2 - 30, h - bm + 25);
            if (!string.IsNullOrEmpty(yLabel))
                g.DrawString(yLabel, SystemFonts.DefaultFont, Brushes.Black, 5, tm - 5);
            // 数值刻度（y轴）
            for (int i = 0; i <= 6; i++)
            {
                double v = ymin + i * (ymax - ymin) / 6.0;
                float y = (float)(h - bm - (v - ymin) / (ymax - ymin) * (h - tm - bm));
                g.DrawString(v.ToString("0.0"), SystemFonts.DefaultFont, Brushes.Black, 5, y - 8);
            }
        }
    }
}
