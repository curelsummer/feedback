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

            // 隐藏模板中的所有图表对象，避免显示内嵌图表
            HideTemplateCharts(report);

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
            // 追加竖版图片页（按指定顺序，一页一图）
            try
            {
                string resolvedBdf = bdfFilePath;
                if (string.IsNullOrWhiteSpace(resolvedBdf) && resultDic.ContainsKey("BdfPath"))
                    resolvedBdf = resultDic["BdfPath"];
                if (!string.IsNullOrWhiteSpace(resolvedBdf))
                {
                    string baseName = System.IO.Path.GetFileNameWithoutExtension(resolvedBdf);
                    string imgDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportImages", baseName);
                    AddPortraitImagePagesFromDirectory(report, imgDir);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] Add portrait image pages error: " + ex.Message);
            }
            // 按模板导出（保留原有内容）；图片已通过 ChartsPage 的 PictureObject 嵌入
            report.Prepare();
            try { LogHelper.Log.LogDebug($"[Report] PreparedPages count (template): {report.PreparedPages?.Count ?? -1}"); } catch {}
            string strPath = string.Format(".\\Data\\{0}_{1}.pdf", DateTime.Now.ToString("yyyyMMddHHmmss"), resultDic["UserName"]);
            PDFExport export = new PDFExport();
            report.Export(export, strPath);
            report.Dispose();
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

            // 兜底：图片导出为 PNG 到 Data/ReportImages/{BDF文件名}/ 下（按需求精简）
            try
            {
                string baseName = System.IO.Path.GetFileNameWithoutExtension(bdfFilePath);
                string tmpDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "ReportImages", baseName);
                if (!Directory.Exists(tmpDir)) Directory.CreateDirectory(tmpDir);

                // 应要求：移除以下图片的生成与落盘
                // - 训练专注趋势.png
                // - 频带功率.png
                // - 平均功率谱密度.png
                // - 脑电时序波形.png

                // 新增：目标通道时频图与频带趋势图（Cz优先，A1/A2作为参考不参与）
                try
                {
                    // 选择 Cz 通道
                    // 优先匹配包含 "Cz"（忽略大小写、允许后缀），若无则选择首个非注释且非A1/A2的通道
                    var sigCz = bdf.Header.Signals.FirstOrDefault(s =>
                    {
                        var lbl0 = s.Label ?? string.Empty;
                        var lbl = lbl0.Trim();
                        return !string.IsNullOrWhiteSpace(lbl) && lbl.ToUpper().StartsWith("CZ");
                    });
                    var sigFallback = bdf.Header.Signals.FirstOrDefault(s =>
                    {
                        var lbl0 = s.Label ?? string.Empty; var lbl = lbl0.Trim().ToUpper();
                        return !string.IsNullOrWhiteSpace(lbl) && !string.Equals(lbl, BDFSignal.NotationLabel, StringComparison.OrdinalIgnoreCase)
                               && !lbl.StartsWith("A1") && !lbl.StartsWith("A2");
                    });
                    var sigChosen = sigCz ?? sigFallback ?? signal;
                    string chanLabel = (sigChosen.Label ?? ("Chan" + sigChosen.IndexNumberWithLabel)).Trim();
                    // 中文文件名
                    string pTf = System.IO.Path.Combine(tmpDir, $"傅里叶时频图_{chanLabel}.png");
                    string pTr = System.IO.Path.Combine(tmpDir, $"频带能量趋势_{chanLabel}.png");

                    // 取该通道原始样本
                    List<double> sigList = new List<double>();
                    foreach (var dr in bdf.DataRecords)
                    {
                        if (dr.Signals.ContainsKey(sigChosen.IndexNumberWithLabel))
                            sigList.AddRange(dr.Signals[sigChosen.IndexNumberWithLabel]);
                    }
                    double fsLocal = fs <= 0 ? 250 : fs;
                    // 预处理：带通 1-45Hz + 50Hz 陷波
                    var sigFiltered = PreprocessBandpassNotch(sigList.ToArray(), fsLocal, 1.0, 45.0, 50.0);
                    // 舍弃前3秒
                    sigFiltered = TrimHeadSamples(sigFiltered, fsLocal, 3.0);
                    // 时频图（Welch, 10s窗口, 2s段, 50% 重叠，伪迹降权）
                    var tf = ComputeWelchSpectrogram(sigFiltered, fsLocal, 10.0, 1.0, 2.0, 0.5, 120.0, 0.2);
                    SaveSpectrogramPng(pTf, tf.timeSec, tf.freq, tf.powerDb, $"傅里叶时频图（{chanLabel}）", "时间 t/s", "频率 f/Hz");

                    var bandsDef2 = new (string name, double f1, double f2)[]{
                        ("Delta 1-4Hz",1,4),("Theta 4-8Hz",4,8),("Alpha 8-13Hz",8,13),("SMR 12-15Hz",12,15),("Beta 13-30Hz",13,30)
                    };
                    // 使用绝对功率（µV²）趋势，单位为 µV²
                    var bandAbs = ComputeBandAbsolutePowerTrends(tf.freq, tf.powerLin, tf.timeSec, bandsDef2);
                    SaveMultiLineChartPng(pTr, bandAbs.timeSec, bandAbs.series, bandAbs.labels, "时间 (s)", "绝对功率 (µV²)", $"频带能量趋势（{chanLabel}  ）");

                    // 计算并保存：Cz通道（若存在，否则为所选通道）复杂度（小波熵）单条折线
                    try
                    {
                        int Tlen = tf.timeSec?.Length ?? 0;
                        if (Tlen > 0)
                        {
                            bool hasCz = (sigCz != null);
                            // 使用 DWT-based 小波熵（与早前实现一致）
                            var ent = ComputeWaveletEntropySeries(sigFiltered, fsLocal, 10.0, 1.0, 5);
                            int TT = Math.Min(Tlen, ent.Length);
                            if (TT > 0)
                            {
                                double[] s = new double[TT];
                                Array.Copy(ent, s, TT);
                                string pEnt = System.IO.Path.Combine(tmpDir, hasCz ? "Cz通道复杂度（小波熵）.png" : $"{chanLabel}通道复杂度（小波熵）.png");
                                SaveLineChartPng(pEnt, tf.timeSec, s, "时间 (s)", "复杂度（小波熵，0–1）", $"复杂度（小波熵）（{chanLabel}）");
                            }
                        }
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError("[Report] Spectrogram/Trend error: " + ex.Message);
                }

                // 新增：全通道 目标节律（Alpha 8-13）能量/复杂度 与 Alpha峰频
                try
                {
                    // 收集所有非注释通道信号
                    var chLabels = new List<string>();
                    var chSamples = new List<double[]>();
                    foreach (var sig in bdf.Header.Signals)
                    {
                        string lbl = sig.Label?.Trim();
                        if (string.Equals(lbl, BDFSignal.NotationLabel, StringComparison.OrdinalIgnoreCase)) continue;
                        List<double> sList = new List<double>();
                        foreach (var dr in bdf.DataRecords)
                        {
                            if (dr.Signals.ContainsKey(sig.IndexNumberWithLabel))
                                sList.AddRange(dr.Signals[sig.IndexNumberWithLabel]);
                        }
                        if (sList.Count > 0)
                        {
                            chLabels.Add(lbl);
                            chSamples.Add(sList.ToArray());
                        }
                    }
                    if (chLabels.Count > 0)
                    {
                        double fsLocal = fs <= 0 ? 250 : fs;
                        // 计算时间轴参考自第一个通道（舍弃前3秒）
                        var tf0 = ComputeWelchSpectrogram(TrimHeadSamples(PreprocessBandpassNotch(chSamples[0], fsLocal, 1.0, 45.0, 50.0), fsLocal, 3.0), fsLocal, 10.0, 1.0, 2.0, 0.5, 120.0, 0.2);
                        double[] tAxis = tf0.timeSec;
                        int T = tAxis.Length; int C = chLabels.Count;
                        double[,] relPower = new double[C, T];
                        double[,] entropy = new double[C, T];
                        double[,] alphaPk = new double[C, T];
                        for (int c = 0; c < C; c++)
                        {
                            var tf = ComputeWelchSpectrogram(TrimHeadSamples(PreprocessBandpassNotch(chSamples[c], fsLocal, 1.0, 45.0, 50.0), fsLocal, 3.0), fsLocal, 10.0, 1.0, 2.0, 0.5, 120.0, 0.2);
                            // 对齐到参考长度
                            int TT = Math.Min(T, tf.timeSec.Length);
                            // 目标带：Alpha 8–13Hz
                            var bandsDef2 = new (string name, double f1, double f2)[]{ ("Alpha", 8, 13) };
                            var tr = ComputeBandTrendsFromSpectrogram(tf.freq, tf.powerLin, tf.timeSec, bandsDef2);
                            for (int i = 0; i < TT; i++) relPower[c, i] = tr.series[0][i];
                            // 复杂度：频谱熵（Alpha带内归一功率的香农熵，归一化到[0,1]）
                            int a = 0; while (a < tf.freq.Length && tf.freq[a] < 8) a++;
                            int z = tf.freq.Length - 1; while (z >= 0 && tf.freq[z] > 13) z--; z = Math.Max(z, a);
                            for (int i = 0; i < TT; i++)
                            {
                                double sum = 0; for (int k = a; k <= z; k++) sum += tf.powerLin[k, i]; if (sum <= 0) { entropy[c, i] = 0; continue; }
                                double H = 0; int N = Math.Max(1, z - a + 1);
                                for (int k = a; k <= z; k++) { double p = tf.powerLin[k, i] / sum; if (p > 0) H += -p * Math.Log(p); }
                                double Hn = H / Math.Log(N);
                                entropy[c, i] = Hn;
                            }
                            // Alpha峰频
                            for (int i = 0; i < TT; i++)
                            {
                                int idx = -1; double maxv = double.MinValue;
                                for (int k = a; k <= z; k++) if (tf.powerLin[k, i] > maxv) { maxv = tf.powerLin[k, i]; idx = k; }
                                alphaPk[c, i] = idx >= 0 ? tf.freq[idx] : double.NaN;
                            }
                        }
                        string pAllPow = System.IO.Path.Combine(tmpDir, "AllCh_TargetBand_RelPower.png");
                       
                        string pAllEnt = System.IO.Path.Combine(tmpDir, "AllCh_TargetBand_Entropy.png");
                        string pAllPk  = System.IO.Path.Combine(tmpDir, "AllCh_AlphaPeak.png");
                        // 将“全通道相对功率热力图”替换为“Cz通道相对功率折线图”（若无Cz，则使用备选通道）
                        try
                        {
                            int idxCz = -1; string chosenLbl = null;
                            for (int ci = 0; ci < chLabels.Count; ci++)
                            {
                                var u = (chLabels[ci] ?? string.Empty).Trim().ToUpper();
                                if (u.StartsWith("CZ") || u.Contains("-CZ") || u.Contains("CZ-")) { idxCz = ci; break; }
                            }
                            if (idxCz < 0)
                            {
                                for (int ci = 0; ci < chLabels.Count; ci++)
                                {
                                    var u = (chLabels[ci] ?? string.Empty).Trim().ToUpper();
                                    if (!u.StartsWith("A1") && !u.StartsWith("A2")) { idxCz = ci; break; }
                                }
                            }
                            if (idxCz < 0) idxCz = 0;
                            chosenLbl = chLabels[idxCz] ?? ("Chan" + idxCz);

                            // 计算 Cz（或备选通道）的多频带相对功率趋势，并输出多折线
                            var tfCz = ComputeWelchSpectrogram(TrimHeadSamples(PreprocessBandpassNotch(chSamples[idxCz], fsLocal, 1.0, 45.0, 50.0), fsLocal, 3.0), fsLocal, 10.0, 1.0, 2.0, 0.5, 120.0, 0.2);
                            var bandsDef2 = new (string name, double f1, double f2)[]{
                                ("Delta 1-4Hz",1,4),("Theta 4-8Hz",4,8),("Alpha 8-13Hz",8,13),("SMR 12-15Hz",12,15),("Beta 13-30Hz",13,30)
                            };
                            var trCz = ComputeBandTrendsFromSpectrogram(tfCz.freq, tfCz.powerLin, tfCz.timeSec, bandsDef2);

                            bool useCzName = ((chLabels[idxCz] ?? string.Empty).Trim().ToUpper().StartsWith("CZ"));
                            string pCzRel = System.IO.Path.Combine(tmpDir, useCzName ? "Cz通道各频带相对功率.png" : $"{chosenLbl}通道各频带相对功率.png");
                            SaveMultiLineChartPng(pCzRel, trCz.timeSec, trCz.series, trCz.labels, "时间 (s)", "相对功率", $"各频带相对功率（{chosenLbl}）");
                        }
                        catch { }
                        // 不再输出谱熵热力图
                        // SaveHeatmapPng(pAllEnt, tAxis, chLabels.ToArray(), entropy, "时间 (s)", "通道", "目标节律复杂度（谱熵）");

                        // 将“全通道Alpha峰频热力图”替换为“Cz通道个体频带相对功率”图（若无Cz，则使用备选通道）
                        try
                        {
                            int idxCz2 = -1; string chosenLbl2 = null;
                            for (int ci = 0; ci < chLabels.Count; ci++)
                            {
                                var u = (chLabels[ci] ?? string.Empty).Trim().ToUpper();
                                if (u.StartsWith("CZ") || u.Contains("-CZ") || u.Contains("CZ-")) { idxCz2 = ci; break; }
                            }
                            if (idxCz2 < 0)
                            {
                                for (int ci = 0; ci < chLabels.Count; ci++)
                                {
                                    var u = (chLabels[ci] ?? string.Empty).Trim().ToUpper();
                                    if (!u.StartsWith("A1") && !u.StartsWith("A2")) { idxCz2 = ci; break; }
                                }
                            }
                            if (idxCz2 < 0) idxCz2 = 0;
                            chosenLbl2 = chLabels[idxCz2] ?? ("Chan" + idxCz2);

                            // 计算 PSD（Welch）
                            double fsLocalCz = fs <= 0 ? 250 : fs;
                            var sigCzRaw = chSamples[idxCz2];
                            var sigCzFiltered = PreprocessBandpassNotch(sigCzRaw, fsLocalCz, 1.0, 45.0, 50.0);
                            sigCzFiltered = TrimHeadSamples(sigCzFiltered, fsLocalCz, 3.0);
                            var wp = WelchPSD(sigCzFiltered, fsLocalCz, (int)Math.Round(2.0 * fsLocalCz), (int)Math.Round(1.0 * fsLocalCz), 0, 0);
                            // 频率与 PSD 序列
                            var xsF = new List<double>(); var ysP = new List<double>();
                            for (int i = 0; i < wp.freq.Length; i++)
                            {
                                double f = wp.freq[i]; if (f < 1 || f > 45) continue; xsF.Add(f); ysP.Add(wp.psd[i]);
                            }
                            if (xsF.Count > 0)
                            {
                                // 个体 alpha 峰（8–13 Hz）
                                int aIdx = -1; double aMax = double.MinValue; for (int i = 0; i < xsF.Count; i++) { if (xsF[i] >= 8 && xsF[i] <= 13 && ysP[i] > aMax) { aMax = ysP[i]; aIdx = i; } }
                                double fAlpha = aIdx >= 0 ? xsF[aIdx] : 10.0;
                                // 比例法：θ/α = 0.8×IAF，α/β = 1.2×IAF，并进行合理夹取
                                double fTA = 0.8 * fAlpha; // theta/alpha
                                double fAB = 1.2 * fAlpha; // alpha/beta
                                // 基本边界保护：限制到常见频段范围
                                fTA = Math.Max(4.0, Math.Min(8.0, fTA));
                                fAB = Math.Max(12.0, Math.Min(30.0, fAB));
                                if (fTA >= fAlpha) fTA = Math.Max(4.0, Math.Min(7.5, fAlpha - 0.2));
                                if (fAB <= fAlpha) fAB = Math.Min(30.0, Math.Max(12.5, fAlpha + 0.2));

                                // 相对功率（以 4–30Hz 总功率归一）
                                double sum430 = 0; for (int i = 0; i < xsF.Count; i++) if (xsF[i] >= 4 && xsF[i] <= 30) sum430 += ysP[i]; if (sum430 <= 0) sum430 = 1;
                                Func<double,double,double> bandSum = (f1,f2) => { double s=0; for (int i=0;i<xsF.Count;i++){ if(xsF[i]>=f1 && xsF[i]<=f2) s+=ysP[i]; } return s; };
                                double pTheta = bandSum(4.0, fTA) / sum430;
                                double pLAlpha= bandSum(fTA, fAlpha) / sum430;
                                double pHAlpha= bandSum(fAlpha, fAB) / sum430;
                                double pBeta  = bandSum(fAB, 30.0) / sum430;

                                // 生成图像：PSD + 三条竖线 + 标题中给出相对功率
                                bool useCzName2 = ((chLabels[idxCz2] ?? string.Empty).Trim().ToUpper().StartsWith("CZ"));
                                string pIndiv = System.IO.Path.Combine(tmpDir, useCzName2 ? "Cz通道个体频带相对功率.png" : $"{chosenLbl2}通道个体频带相对功率.png");
                                string title = $"个体频带相对功率: theta={pTheta:0.00}|low alpha:{pLAlpha:0.00}|high alpha:{pHAlpha:0.00}|beta:{pBeta:0.00}";
                                var vlines = new List<Tuple<double,Color,string>>{
                                    Tuple.Create(fTA, Color.Goldenrod, $"theta/alpha:{fTA:0.00}Hz"),
                                    Tuple.Create(fAlpha, Color.Firebrick, $"alpha peak:{fAlpha:0.00}Hz"),
                                    Tuple.Create(fAB, Color.SteelBlue, $"alpha/beta:{fAB:0.00}Hz")
                                };
                                SaveIndividualBandRelPowerPng(pIndiv, xsF.ToArray(), ysP.ToArray(), vlines, title);
                            }
                        }
                        catch { }
                    }
                }
                catch(Exception ex)
                {
                    LogHelper.Log.LogError("[Report] All-channels analysis error: " + ex.Message);
                }

                // 基于 message_sent 日志生成 神经反馈状态曲线 并填充趋势位
                try
                {
                    string nfPng = GenerateNFStateCurveFromLogInternal(tmpDir, bdfFilePath);
                    if (!string.IsNullOrWhiteSpace(nfPng) && File.Exists(nfPng))
                    {
                        var trendPicLocal = report.FindObject("AttentionTrendChartPic") as PictureObject;
                        if (trendPicLocal != null)
                        {
                            SetPictureObjectImage(trendPicLocal, nfPng);
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

        // 隐藏模板中的图表对象（不再展示饼图、折线图、柱状图等）
        private void HideTemplateCharts(Report report)
        {
            try
            {
                string[] chartNames = new[] { "ResultChart", "AttentionTrendChart", "EEGChart", "PSDChart", "BandPowerChart" };
                foreach (var name in chartNames)
                {
                    var obj = report.FindObject(name) as MSChartObject;
                    if (obj != null)
                    {
                        obj.Visible = false;
                        obj.Printable = false;
                        try { obj.Chart.Series.Clear(); } catch { }
                    }
                }
                // 同步隐藏可能存在的图片占位
                string[] picNames = new[] { "AttentionTrendChartPic", "BandPowerChartPic", "PSDChartPic", "EEGChartPic" };
                foreach (var name in picNames)
                {
                    var obj = report.FindObject(name) as PictureObject;
                    if (obj != null)
                    {
                        obj.Visible = false;
                        obj.Printable = false;
                        obj.Image = null;
                    }
                }
            }
            catch { }
        }

        // 从目录按指定顺序添加竖版图片页（每页一图）
        private void AddPortraitImagePagesFromDirectory(Report report, string imgDir)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imgDir) || !Directory.Exists(imgDir)) return;

                string[] wantOrder = new[]
                {
                    "神经反馈状态曲线",
                    "傅里叶时频图_Cz-AVG",
                    "Cz通道各频带相对功率",
                    "频带能量趋势_Cz-AVG",
                    "Cz通道复杂度（小波熵）",
                    "Cz通道个体频带相对功率"
                };

                List<Tuple<string,string>> chosen = new List<Tuple<string,string>>();

                foreach (var key in wantOrder)
                {
                    string path = null;
                    switch (key)
                    {
                        case "神经反馈状态曲线":
                            path = FindBestImage(imgDir, "神经反馈状态曲线");
                            break;
                        case "傅里叶时频图_Cz-AVG":
                            path = FindBestImage(imgDir, "傅里叶时频图_Cz-AVG", "傅里叶时频图");
                            break;
                        case "Cz通道各频带相对功率":
                            path = FindBestImage(imgDir, "Cz通道各频带相对功率", "各频带相对功率");
                            break;
                        case "频带能量趋势_Cz-AVG":
                            path = FindBestImage(imgDir, "频带能量趋势_Cz-AVG", "频带能量趋势");
                            break;
                        case "Cz通道复杂度（小波熵）":
                            path = FindBestImage(imgDir, "Cz通道复杂度（小波熵）", "复杂度", "小波熵");
                            break;
                        case "Cz通道个体频带相对功率":
                            path = FindBestImage(imgDir, "Cz通道个体频带相对功率", "个体频带相对功率");
                            break;
                    }
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        chosen.Add(Tuple.Create(path, key));
                    }
                }

                // 顺序排布于同页，空间不足自动换页
                ReportPage pageCur = null; DataBand bandCur = null;
                Action newPage = () => {
                    pageCur = new ReportPage { Name = "ImgPage_" + (report.Pages.Count+1), Landscape = false, PaperWidth = 210, PaperHeight = 297 };
                    report.Pages.Add(pageCur);
                    pageCur.TopMargin = 10; pageCur.BottomMargin = 10; pageCur.LeftMargin = 10; pageCur.RightMargin = 10;
                    bandCur = new DataBand { Height = mm(260), Visible = true, Printable = true, RowCount = 1 };
                    pageCur.Bands.Add(bandCur);
                };
                newPage();
                float curY = mm(12);

                float areaXDefault = mm(10), areaWDefault = mm(190);
                for (int ci = 0; ci < chosen.Count; ci++)
                {
                    var imgPath = chosen[ci].Item1; var keyName = chosen[ci].Item2;
                    var texts = GetTextsForKey(keyName);

                  

                    // 顶部说明
                    if (!string.IsNullOrWhiteSpace(texts.descTop))
                    {
                        // 若当前页剩余高度不足放下说明+最小图片，先换页
                        float minNeeded = mm(16) + mm(3) + mm(60) + mm(7) + mm(8) + mm(10); // 顶部文+图最小+脚标+间距
                        if (curY + minNeeded > bandCur.Height - mm(5)) { newPage(); curY = mm(12); }
                        var descObj = new TextObject { Name = "DescTopText_" + ci, Bounds = new System.Drawing.RectangleF(mm(10), curY, mm(190), mm(16)), Text = EnhanceLineSpacing("　　" + texts.descTop), HorzAlign = HorzAlign.Left, VertAlign = VertAlign.Top, WordWrap = true, Font = new Font("SimSun", 12f, FontStyle.Regular), Visible = true, Printable = true };
                        bandCur.Objects.Add(descObj);
                        curY += mm(16) + mm(3);
                        // 额外向下偏移，让图片与说明之间留出约两行的间距（宋体12约 3–4mm/行）
                        float extraGap = keyName.Contains("神经反馈状态曲线") ? mm(8) : mm(6);
                        curY += extraGap;
                    }

                    // 图片区域：根据剩余高度裁定可用高度
                    float areaX = areaXDefault, areaW = areaWDefault;
                    float maxH = bandCur.Height - curY - (mm(7) + mm(8) + mm(18));
                    if (maxH < mm(60)) { newPage(); curY = mm(12); maxH = bandCur.Height - curY - (mm(7) + mm(8) + mm(18)); }

                    var pic = new PictureObject { Name = "Pic_" + Guid.NewGuid().ToString("N"), Bounds = new System.Drawing.RectangleF(areaX, curY, areaW, Math.Max(mm(60), maxH)), Visible = true, Printable = true };
                    try
                    {
                        using (var bmp = new Bitmap(imgPath))
                        {
                            pic.Image = new Bitmap(bmp);
                            float imgW = bmp.Width, imgH = bmp.Height; if (imgW <= 0 || imgH <= 0) imgW = 1;
                            float scale = Math.Min(areaW / imgW, maxH / imgH);
                            float w = imgW * scale, h = imgH * scale;
                            float x = areaX; float y = curY;
                            pic.Bounds = new System.Drawing.RectangleF(x, y, w, h);

                            // 脚标
                            var cap = new TextObject { Name = "Caption_" + ci, Bounds = new System.Drawing.RectangleF(mm(10), y + h + mm(2), mm(190), mm(7)), Text = texts.title ?? string.Empty, HorzAlign = HorzAlign.Center, VertAlign = VertAlign.Top, WordWrap = false, Font = new Font("SimSun", 12f, FontStyle.Regular), Visible = true, Printable = true };
                            bandCur.Objects.Add(cap);

                            // 观察要点
                            if (!string.IsNullOrWhiteSpace(texts.descBottom))
                            {
                                var obs = new TextObject { Name = "ObsText_" + ci, Bounds = new System.Drawing.RectangleF(mm(10), y + h + mm(10), mm(190), mm(18)), Text = EnhanceLineSpacing("　　" + texts.descBottom), HorzAlign = HorzAlign.Left, VertAlign = VertAlign.Top, WordWrap = true, Font = new Font("SimSun", 12f, FontStyle.Regular), Visible = true, Printable = true };
                                bandCur.Objects.Add(obs);
                                curY = y + h + mm(10) + mm(18) + mm(6); // 段后间距
                            }
                            else
                            {
                                curY = y + h + mm(7) + mm(6);
                            }
                        }
                    }
                    catch { pic.ImageLocation = imgPath; curY += mm(60); }
                    bandCur.Objects.Add(pic);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError("[Report] AddPortraitImagePagesFromDirectory failed: " + ex.Message);
            }
        }

        // 根据图片关键名返回标题、上方描述、观察要点（必要时可扩展）
        private (string title, string descTop, string descBottom) GetTextsForKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return (null, null, null);
            key = key.Trim();
            // 图名到标准 key 的归一
            if (key.Contains("神经反馈状态曲线"))
            {
                return ("图1｜神经反馈状态曲线（-100～100）",
                        "该曲线综合呈现训练过程中奖励、惩罚与静止事件的时序与强度，纵轴为归一化分数（-100～100），横轴为时间（秒）。分数上升代表目标状态被更频繁触发，下降则提示偏离目标。",
                        "观察要点：峰值时段、持续上升/下降的区段、分段间差异与稳定性。训练过程的即时反馈被整合为一条分数曲线，用以评估整体专注/放松目标的达成度，越高表示训练目标达成度越好。");
            }
            else if (key.Contains("傅里叶时频图"))
            {
                return ("图2｜傅里叶时频图（Cz-AVG，1.5–45 Hz）",
                        "时频图显示 Cz 通道在 1.5–45 Hz 范围内随时间变化的功率分布，颜色越亮代表能量越高。可据此定位各节律（θ/α/β 等）在不同时段的活跃程度及其波动。",
                        "观察要点：目标频段的持续高亮区、起落转折点、是否存在稳定的节律峰。该图用于快速识别训练期内主要脑节律的活跃窗口与稳定性。");
            }
            else if (key.Contains("各频带相对功率"))
            {
                return ("图3｜Cz 各频带相对功率",
                        "展示 Delta、Theta、Alpha、SMR、Beta 等频带相对于 1.5–45 Hz 总功率的比例随时间的变化。比例上升表示该频带在整体脑电中占比提高。",
                        "观察要点：目标频带的上升趋势、不同频带之间的此消彼长、分段前后差异。通过相对功率可判断训练是否将能量更集中地分配到目标节律上。");
            }
            else if (key.Contains("频带能量趋势"))
            {
                return ("图4｜频带能量趋势（绝对功率 µV²）",
                        "以绝对功率（µV²）刻画各频带在时间维度的能量变化，更适合衡量‘强度’本身的提升或抑制。",
                        "观察要点：目标频带峰值与持续时间、是否伴随非目标频带的抬升。当关注‘强度是否被真正拉高’时，请优先参考绝对功率曲线。");
            }
            else if (key.Contains("复杂度") || key.Contains("小波熵"))
            {
                return ("图5｜Cz 各频带复杂度（小波熵，0–1）",
                        "小波熵取值 0–1，数值越高代表该频带的时序更复杂、越接近随机；越低代表更规则、可预测性更强。训练中复杂度的降低常见于稳定的节律建立，但需结合任务目标解读。",
                        "观察要点：目标频带复杂度的整体趋势与阶段性变化，是否随训练趋于稳定。该指标帮助判断节律是‘更有序’还是‘更发散’，用于辅助解释功率变化背后的动态。");
            }
            else if (key.Contains("个体频带相对功率"))
            {
                return ("图6｜Cz 个体化频带相对功率",
                        "基于 PSD 计算个体 α 峰频，并以 α 峰频设定 θ/α 与 α/β 的个体化分界。",
                        "观察要点：α 峰位置是否清晰；低/高 α 的相对比例；个体化分界前后频带的能量分布。在个体α峰下各频带相对功率的分配，进而反映清醒度与专注—放松平衡及紧张倾向。");
            }
            return (null, null, null);
        }

        // 简易“加大行距”处理：在句末添加换行，提高清晰度
        private string EnhanceLineSpacing(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            return s.Replace("。", "。\n");
        }

        // 在目录中查找最佳匹配的图片
        private string FindBestImage(string imgDir, string key, params string[] fallbacks)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imgDir) || !Directory.Exists(imgDir)) return null;
                var exact = Path.Combine(imgDir, key + ".png");
                if (File.Exists(exact)) return exact;
                var files = Directory.GetFiles(imgDir, "*.png");
                string best = null; int bestScore = int.MinValue;
                foreach (var f in files)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    int score = 0;
                    if (name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
                    if (name.IndexOf("Cz-AVG", StringComparison.OrdinalIgnoreCase) >= 0) score += 5;
                    if (name.IndexOf("Cz", StringComparison.OrdinalIgnoreCase) >= 0) score += 3;
                    if (fallbacks != null)
                    {
                        foreach (var fb in fallbacks)
                        {
                            if (!string.IsNullOrEmpty(fb) && name.IndexOf(fb, StringComparison.OrdinalIgnoreCase) >= 0) score += 2;
                        }
                    }
                    if (score > bestScore) { bestScore = score; best = f; }
                }
                return best;
            }
            catch { return null; }
        }

        // 计算 Welch 时频图
        private (double[] timeSec, double[] freq, double[,] powerLin, double[,] powerDb) ComputeWelchSpectrogram(
            double[] signal, double fs, double windowSec, double stepSec, double welchSegSec, double welchOverlap,
            double artifactAmpThresh = 0, double artifactDownWeight = 0)
        {
            int win = Math.Max(1, (int)Math.Round(windowSec * fs));
            int hop = Math.Max(1, (int)Math.Round(stepSec * fs));
            int seg = Math.Max(1, (int)Math.Round(welchSegSec * fs));
            int segHop = Math.Max(1, (int)Math.Round(seg * (1.0 - welchOverlap)));
            List<double[]> psds = new List<double[]>();
            List<double> tlist = new List<double>();
            double[] freq = null;
            for (int start = 0; start + win <= signal.Length; start += hop)
            {
                var slice = new double[win]; Array.Copy(signal, start, slice, 0, win);
                var wp = WelchPSD(slice, fs, seg, segHop, artifactAmpThresh, artifactDownWeight);
                if (wp.freq.Length == 0) continue;
                if (freq == null) freq = wp.freq;
                psds.Add(wp.psd);
                tlist.Add((start + win / 2.0) / fs);
            }
            if (freq == null || psds.Count == 0) return (new double[0], new double[0], new double[0,0], new double[0,0]);
            int fStart = 0; while (fStart < freq.Length && freq[fStart] < 1) fStart++;
            int fEnd = freq.Length - 1; while (fEnd >= 0 && freq[fEnd] > 45) fEnd--; fEnd = Math.Max(fEnd, fStart);
            int F = fEnd - fStart + 1, T = psds.Count;
            double[,] P = new double[F, T]; double[,] Pdb = new double[F, T];
            for (int t = 0; t < T; t++)
            {
                var p = psds[t];
                for (int k = 0; k < F; k++) { double v = p[fStart + k]; P[k,t]=v; Pdb[k,t] = 10.0*Math.Log10(Math.Max(v,1e-12)); }
            }
            double[] fsel = new double[F]; for (int k = 0; k < F; k++) fsel[k] = freq[fStart + k];
            return (tlist.ToArray(), fsel, P, Pdb);
        }

        private (double[] freq, double[] psd) WelchPSD(double[] x, double fs, int segLen, int segHop, double artifactAmpThresh = 0, double artifactDownWeight = 0)
        {
            int nfft = 1; while (nfft < segLen) nfft <<= 1; int m = (int)Math.Log(nfft, 2);
            double[] w = Hamming(segLen); double U = 0; foreach (var a in w) U += a*a; U/=segLen;
            List<double[]> parts = new List<double[]>();
            List<double> weights = new List<double>();
            for (int s = 0; s + segLen <= x.Length; s += segHop)
            {
                double[] xr = new double[nfft]; double[] xi = new double[nfft];
                for (int i = 0; i < segLen; i++) xr[i] = x[s+i]*w[i];
                double weight = 1.0;
                if (artifactAmpThresh > 0)
                {
                    double maxAbs = 0; for (int i=0;i<segLen;i++){ double v=Math.Abs(x[s+i]); if (v>maxAbs) maxAbs=v; }
                    if (maxAbs > artifactAmpThresh) weight = Math.Max(0.0, 1.0 - artifactDownWeight); // 降权
                }
                FFTLibrary.MyComplex.FFT(1, m, xr, xi);
                int half = nfft/2; double[] p = new double[half]; double scale = 2.0/(fs*segLen*U);
                for (int k = 0; k < half; k++) p[k] = (xr[k]*xr[k]+xi[k]*xi[k]) * scale;
                parts.Add(p);
                weights.Add(weight);
            }
            if (parts.Count == 0) return (new double[0], new double[0]);
            int H = parts[0].Length; double[] psd = new double[H];
            for (int k = 0; k < H; k++)
            {
                double sum=0, wsum=0; for (int i=0;i<parts.Count;i++){ double wgt = (i<weights.Count?weights[i]:1.0); sum+=parts[i][k]*wgt; wsum+=wgt; }
                psd[k]= sum / Math.Max(1e-9, wsum);
            }
            double[] f = new double[H]; for (int k=0;k<H;k++) f[k]=(double)k*fs/(2*H);
            return (f, psd);
        }
        // 简单IIR预处理：带通(1-45Hz) + 50Hz 陷波（双二阶）
        private double[] PreprocessBandpassNotch(double[] x, double fs, double f1, double f2, double notch)
        {
            if (x == null || x.Length == 0) return x;
            // 采用双二阶巴特沃斯带通 + biquad notch，系数用双线性变换近似（简化实现）
            double[] y = (double[])x.Clone();
            // 带通（两级一阶近似）
            y = BiquadBandpass(y, fs, f1, f2, 0.707);
            // 陷波（Q≈30）
            y = BiquadNotch(y, fs, notch, 30);
            return y;
        }

        private double[] BiquadBandpass(double[] x, double fs, double f1, double f2, double q)
        {
            return Biquad(BiquadHP(x, fs, f1, q), fs, f2, q, type: "LP");
        }
        private double[] BiquadNotch(double[] x, double fs, double f0, double q)
        {
            return Biquad(x, fs, f0, q, type: "NOTCH");
        }
        private double[] BiquadHP(double[] x, double fs, double fc, double q)
        {
            return Biquad(x, fs, fc, q, type: "HP");
        }
        private double[] Biquad(double[] x, double fs, double fc, double q, string type)
        {
            if (fc <= 0 || fc >= fs/2 - 1) return x;
            double w0 = 2*Math.PI*fc/fs; double alpha = Math.Sin(w0)/(2*q); double cosw0=Math.Cos(w0);
            double b0=0,b1=0,b2=0,a0=1,a1=0,a2=0;
            switch(type)
            {
                case "HP":
                    b0=(1+cosw0)/2; b1=-(1+cosw0); b2=(1+cosw0)/2; a0=1+alpha; a1=-2*cosw0; a2=1-alpha; break;
                case "LP":
                    b0=(1-cosw0)/2; b1=1-cosw0; b2=(1-cosw0)/2; a0=1+alpha; a1=-2*cosw0; a2=1-alpha; break;
                case "NOTCH":
                    b0=1; b1=-2*cosw0; b2=1; a0=1+alpha; a1=-2*cosw0; a2=1-alpha; break;
                default: return x;
            }
            double bb0=b0/a0, bb1=b1/a0, bb2=b2/a0, aa1=a1/a0, aa2=a2/a0;
            double[] y=new double[x.Length]; double x1=0,x2=0,y1=0,y2=0;
            for(int n=0;n<x.Length;n++){ double xn=x[n]; double yn=bb0*xn+bb1*x1+bb2*x2 - aa1*y1 - aa2*y2; y[n]=yn; x2=x1; x1=xn; y2=y1; y1=yn; }
            return y;
        }

        private double[] Hamming(int n){ double[] w = new double[n]; for(int i=0;i<n;i++) w[i]=0.54-0.46*Math.Cos(2*Math.PI*i/(n-1)); return w; }

        private double[] TrimHeadSamples(double[] x, double fs, double headSec)
        {
            if (x == null || x.Length == 0) return x;
            int trim = Math.Max(0, (int)Math.Round(headSec * fs));
            if (trim >= x.Length) return new double[0];
            double[] y = new double[x.Length - trim];
            Array.Copy(x, trim, y, 0, y.Length);
            return y;
        }

        private (double[] timeSec, List<double[]> series, List<string> labels) ComputeBandTrendsFromSpectrogram(double[] freq, double[,] powerLin, double[] timeSec, (string name,double f1,double f2)[] bands)
        {
            int F=freq.Length, T=timeSec.Length; if(F==0||T==0) return (new double[0], new List<double[]>(), new List<string>());
            int iStart=0; while(iStart<F && freq[iStart]<1) iStart++; int iEnd=F-1; while(iEnd>=0 && freq[iEnd]>45) iEnd--; iEnd=Math.Max(iEnd,iStart);
            double[] total=new double[T]; for(int t=0;t<T;t++){ double s=0; for(int k=iStart;k<=iEnd;k++) s+=powerLin[k,t]; total[t]=Math.Max(s,1e-12);}            
            var series=new List<double[]>(); var labels=new List<string>();
            foreach(var b in bands){ int a=0; while(a<F && freq[a]<b.f1) a++; int z=F-1; while(z>=0 && freq[z]>b.f2) z--; z=Math.Max(z,a); double[] s=new double[T]; for(int t=0;t<T;t++){ double sum=0; for(int k=a;k<=z;k++) sum+=powerLin[k,t]; s[t]=sum/total[t]; } series.Add(s); labels.Add(b.name);}            
            return (timeSec, series, labels);
        }

        // 绝对功率趋势（µV²）：不再归一化为相对功率，直接对频带积分
        private (double[] timeSec, List<double[]> series, List<string> labels) ComputeBandAbsolutePowerTrends(double[] freq, double[,] powerLin, double[] timeSec, (string name,double f1,double f2)[] bands)
        {
            int F=freq.Length, T=timeSec.Length; if(F==0||T==0) return (new double[0], new List<double[]>(), new List<string>());
            // 频率分辨率（假设等间隔）
            double df = (F>1) ? Math.Abs(freq[1]-freq[0]) : 1.0;
            var series=new List<double[]>(); var labels=new List<string>();
            foreach(var b in bands){ int a=0; while(a<F && freq[a]<b.f1) a++; int z=F-1; while(z>=0 && freq[z]>b.f2) z--; z=Math.Max(z,a); double[] s=new double[T]; for(int t=0;t<T;t++){ double sum=0; for(int k=a;k<=z;k++) sum+=powerLin[k,t]*df; s[t]=sum; } series.Add(s); labels.Add(b.name);}            
            return (timeSec, series, labels);
        }

        private void SaveSpectrogramPng(string path, double[] ts, double[] fs, double[,] pDb, string title, string xLabel, string yLabel)
        {
            if(ts==null||fs==null||ts.Length==0||fs.Length==0) return; int w=1000,h=500,lm=60,rm=80,tm=25,bm=40; // 加大右侧留白给颜色条
            using(var bmp=new Bitmap(w,h)) using(var g=Graphics.FromImage(bmp)){
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.None; g.Clear(Color.White);
                g.DrawString(title??"时频图", new Font(SystemFonts.DefaultFont, FontStyle.Bold), Brushes.Black, w/2-60, 2);
                double vmin=double.PositiveInfinity,vmax=double.NegativeInfinity; for(int i=0;i<fs.Length;i++) for(int j=0;j<ts.Length;j++){ double v=pDb[i,j]; if(v<vmin)vmin=v; if(v>vmax)vmax=v; }
                if(double.IsNaN(vmin)||double.IsInfinity(vmin)||double.IsNaN(vmax)||double.IsInfinity(vmax)||Math.Abs(vmax-vmin)<1e-9){ vmin=-40; vmax=10; }
                Rectangle rect=new Rectangle(lm,tm,w-lm-rm,h-tm-bm);
                for(int j=0;j<ts.Length;j++){
                    for(int i=0;i<fs.Length;i++){
                        double x0=(double)j/ts.Length; double y0=(double)i/fs.Length; int x=rect.X+(int)Math.Floor(x0*rect.Width); int y=rect.Y+rect.Height-(int)Math.Ceiling(y0*rect.Height); int xx=rect.X+(int)Math.Floor(((double)(j+1)/ts.Length)*rect.Width); int yy=rect.Y+rect.Height-(int)Math.Ceiling(((double)(i+1)/fs.Length)*rect.Height); int ww=Math.Max(1,xx-x); int hh=Math.Max(1,y-yy); double v=pDb[i,j]; double t=(v-vmin)/(vmax-vmin); if(double.IsNaN(t)||double.IsInfinity(t)) t=0; t=Math.Max(0,Math.Min(1,t)); Color c=ColorMapTurbo(t); using(var br=new SolidBrush(c)) g.FillRectangle(br, x, y-hh+1, ww, hh);
                    }
                }
                DrawAxesAndTicks(g,w,h,lm,rm,tm,bm,ts.First(),ts.Last(),fs.First(),fs.Last(),xLabel,yLabel,null);
                // 颜色条（右侧）
                int cbW = 18; int cbX = w - rm + 10; int cbY = tm; int cbH = h - tm - bm; // 放在绘图区右侧
                for(int yy=0; yy<cbH; yy++){
                    double t = 1.0 - (double)yy / (cbH-1);
                    using(var br=new SolidBrush(ColorMapTurbo(t))) g.FillRectangle(br, cbX, cbY+yy, cbW, 1);
                }
                g.DrawRectangle(Pens.Black, cbX, cbY, cbW, cbH);
                // 颜色条：仅优化刻度可读性，不改变图像映射（仍用 vmin~vmax）。
                // 刻度范围取 dmin/dmax（对齐到 20 dB），位置按 dmin~dmax 线性分布，避免端点重叠。
                double dmin = Math.Floor(vmin/20.0)*20.0;
                double dmax = Math.Ceiling(vmax/20.0)*20.0;
                double range = dmax - dmin;
                int step = 20;
                if (range/step > 8) step = 40;
                if (range/step < 4) step = 10;
                for (double tk = dmin; tk <= dmax + 1e-6; tk += step)
                {
                    // 相对 dmin~dmax 的位置，避免用 vmin~vmax 导致端点重叠
                    double tt = (tk - dmin) / Math.Max(1e-9, (dmax - dmin));
                    tt = Math.Max(0, Math.Min(1, tt));
                    int y = cbY + (int)Math.Round((1-tt) * cbH);
                    g.DrawLine(Pens.Black, cbX + cbW, y, cbX + cbW + 6, y);
                    g.DrawString(((int)tk).ToString(), SystemFonts.DefaultFont, Brushes.Black, cbX + cbW + 8, y - 7);
                }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private Color ColorMapTurbo(double t){ t=Math.Max(0,Math.Min(1,t)); double h=(1-t)*240.0; return FromHSV(h,0.85,0.9); }
        private Color FromHSV(double hue,double saturation,double value){ int hi=Convert.ToInt32(Math.Floor(hue/60))%6; double f=hue/60-Math.Floor(hue/60); value=value*255; int v=(int)Math.Round(value); int p=(int)Math.Round(value*(1-saturation)); int q=(int)Math.Round(value*(1-f*saturation)); int tt=(int)Math.Round(value*(1-(1-f)*saturation)); switch(hi){ case 0:return Color.FromArgb(255,v,tt,p); case 1:return Color.FromArgb(255,q,v,p); case 2:return Color.FromArgb(255,p,v,tt); case 3:return Color.FromArgb(255,p,q,v); case 4:return Color.FromArgb(255,tt,p,v); default:return Color.FromArgb(255,v,p,q);} }

        private void SaveMultiLineChartPng(string path,double[] xs,List<double[]> ysSeries,List<string> labels,string xLabel,string yLabel,string title)
        {
            if(xs==null||ysSeries==null||ysSeries.Count==0) return; int w=1000,h=500,lm=60,rm=15,tm=20,bm=40; using(var bmp=new Bitmap(w,h)) using(var g=Graphics.FromImage(bmp)){
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.Clear(Color.White);
                double xmin=xs.Min(),xmax=xs.Max(); if(Math.Abs(xmax-xmin)<1e-9) xmax=xmin+1; double ymin=double.PositiveInfinity,ymax=double.NegativeInfinity; foreach(var s in ysSeries){ if(s==null||s.Length==0) continue; ymin=Math.Min(ymin,s.Min()); ymax=Math.Max(ymax,s.Max()); } if(double.IsNaN(ymin)||double.IsInfinity(ymin)||double.IsNaN(ymax)||double.IsInfinity(ymax)||Math.Abs(ymax-ymin)<1e-9){ ymin=0; ymax=1; }
                DrawAxesAndTicks(g,w,h,lm,rm,tm,bm,xmin,xmax,ymin,ymax,xLabel,yLabel,title);
                Color[] colors=new[]{ Color.DarkBlue, Color.OrangeRed, Color.SeaGreen, Color.MediumVioletRed, Color.SaddleBrown, Color.MidnightBlue }; int idx=0; foreach(var s in ysSeries){ Color c=colors[idx%colors.Length]; idx++; using(var pen=new Pen(c,1.6f)){ System.Drawing.PointF? prev=null; for(int i=0;i<xs.Length && i<s.Length;i++){ float X=(float)(lm+(xs[i]-xmin)/(xmax-xmin)*(w-lm-rm)); float Y=(float)(h-bm-(s[i]-ymin)/(ymax-ymin)*(h-tm-bm)); var pt=new System.Drawing.PointF(X,Y); if(prev!=null) g.DrawLine(pen, prev.Value, pt); prev=pt; } } }
                if(labels!=null && labels.Count==ysSeries.Count){ int lgx=w-rm-160, lgy=tm+8, lh=18; idx=0; foreach(var lab in labels){ Color c=colors[idx%colors.Length]; idx++; using(var br=new SolidBrush(c)) g.FillRectangle(br, lgx, lgy+(idx-1)*lh, 20, 6); g.DrawString(lab, SystemFonts.DefaultFont, Brushes.Black, lgx+28, lgy+(idx-1)*lh-4); } }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 个体频带相对功率图：将 PSD 在 4–30 Hz 范围内归一化为相对功率(%)，并叠加三条竖线
        private void SaveIndividualBandRelPowerPng(string path, double[] freq, double[] psd, List<Tuple<double,Color,string>> vlines, string title)
        {
            if (freq==null||psd==null||freq.Length==0||psd.Length==0) return; int w=1100,h=500,lm=80,rm=20,tm=45,bm=55; using(var bmp=new Bitmap(w,h)) using(var g=Graphics.FromImage(bmp)){
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.Clear(Color.White);
                // 固定横坐标显示范围 1.5–50 Hz
                double xmin=1.5, xmax=50.0;
                // 计算 4–30 Hz 总功率并将 PSD 归一为相对功率百分比
                int i4=0; while(i4<freq.Length && freq[i4]<4.0) i4++;
                int i30=freq.Length-1; while(i30>=0 && freq[i30]>30.0) i30--; i30=Math.Max(i30,i4);
                double denom=0; for(int i=i4;i<=i30 && i<psd.Length;i++) denom+=psd[i]; if(denom<=0) denom=1;
                double[] rpct = new double[psd.Length];
                for(int i=0;i<psd.Length;i++) rpct[i] = Math.Max(0.0, psd[i]/denom*100.0);
                // y 轴范围：0 到 95 分位数的 1.2 倍，兼顾可读性
                double[] rpCopy = rpct.ToArray(); Array.Sort(rpCopy);
                double q95 = rpCopy[(int)Math.Floor(0.95*(rpCopy.Length-1))];
                double ymin = 0.0;
                double ymax = Math.Max(1.0, Math.Min(100.0, q95*1.2));
                DrawAxesAndTicks(g,w,h,lm,rm,tm,bm,xmin,xmax,ymin,ymax,"频率 (Hz)","相对功率 (%)",title);
                using(var pen=new Pen(Color.SeaGreen,2.0f)){
                    System.Drawing.PointF? prev=null; 
                    for(int i=0;i<freq.Length && i<rpct.Length;i++){ 
                        if (freq[i] < xmin || freq[i] > xmax) continue;
                        float X=(float)(lm+(freq[i]-xmin)/(xmax-xmin)*(w-lm-rm)); 
                        float Y=(float)(h-bm-(rpct[i]-ymin)/(ymax-ymin)*(h-tm-bm)); 
                        var pt=new System.Drawing.PointF(X,Y); if(prev!=null) g.DrawLine(pen, prev.Value, pt); prev=pt; 
                    }
                }
                if(vlines!=null){ int idx=0; foreach(var vl in vlines){ double fx=vl.Item1; if (fx < xmin || fx > xmax) { idx++; continue; } float X=(float)(lm+(fx-xmin)/(xmax-xmin)*(w-lm-rm)); using(var p=new Pen(vl.Item2,2.0f)){ g.DrawLine(p, X, tm, X, h-bm); } if(!string.IsNullOrWhiteSpace(vl.Item3)){ float yLabel = tm + 8 + idx*18; float xLabel = X + 6; var sz = g.MeasureString(vl.Item3, SystemFonts.DefaultFont); if(xLabel + sz.Width > w - rm) xLabel = X - 6 - (float)sz.Width; g.DrawString(vl.Item3, SystemFonts.DefaultFont, Brushes.Black, xLabel, yLabel); } idx++; } }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 通道×时间热图（用于全通道相对功率/谱熵/Alpha峰）
        private void SaveHeatmapPng(string path, double[] ts, string[] channels, double[,] Z, string xLabel, string yLabel, string title, double? vmin=null, double? vmax=null)
        {
            if (ts == null || channels == null || Z == null) return;
            int T = ts.Length, C = channels.Length; if (T == 0 || C == 0) return;
            int w = 1100, h = 500, lm = 80, rm = 15, tm = 25, bm = 40;
            using (var bmp = new Bitmap(w, h))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.Clear(Color.White);
                g.DrawString(title ?? "热图", new Font(SystemFonts.DefaultFont, FontStyle.Bold), Brushes.Black, w/2 - 60, 2);
                // 取值范围
                double zmin = double.PositiveInfinity, zmax = double.NegativeInfinity;
                for (int c = 0; c < C; c++) for (int t = 0; t < T; t++) { double v = Z[c, t]; if (!double.IsNaN(v)) { if (v < zmin) zmin = v; if (v > zmax) zmax = v; } }
                if (vmin.HasValue) zmin = vmin.Value; if (vmax.HasValue) zmax = vmax.Value;
                if (double.IsNaN(zmin) || double.IsInfinity(zmin) || double.IsNaN(zmax) || double.IsInfinity(zmax) || Math.Abs(zmax - zmin) < 1e-9) { zmin = 0; zmax = 1; }
                Rectangle rect = new Rectangle(lm, tm, w - lm - rm, h - tm - bm);
                for (int t = 0; t < T; t++)
                {
                    for (int c = 0; c < C; c++)
                    {
                        double x0 = (double)t / T; double y0 = (double)c / C;
                        int x = rect.X + (int)Math.Floor(x0 * rect.Width);
                        int y = rect.Y + (int)Math.Floor(y0 * rect.Height);
                        int xx = rect.X + (int)Math.Floor(((double)(t + 1) / T) * rect.Width);
                        int yy = rect.Y + (int)Math.Floor(((double)(c + 1) / C) * rect.Height);
                        int ww = Math.Max(1, xx - x); int hh = Math.Max(1, yy - y);
                        double val = Z[c, t];
                        double tt2 = (val - zmin) / (zmax - zmin);
                        Color col = ColorMapTurbo(tt2);
                        using (var br = new SolidBrush(col)) g.FillRectangle(br, x, y, ww, hh);
                    }
                }
                // 轴
                DrawAxesAndTicks(g, w, h, lm, rm, tm, bm, ts.First(), ts.Last(), 0, C - 1, xLabel, yLabel, null);
                // 通道标签（y 轴）
                for (int c = 0; c < C; c++)
                {
                    int y = tm + (int)((double)c / C * (h - tm - bm));
                    g.DrawString(channels[c], SystemFonts.DefaultFont, Brushes.Black, 5, y);
                }
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        // 计算窗口化小波熵时间序列
        // 方法：对每个时间窗做 DWT（Haar），取前 L 层细节+最后一层近似能量，归一化能量作为概率分布，计算香农熵并用 log(M) 归一化
        private double[] ComputeWaveletEntropySeries(double[] signal, double fs, double windowSec, double stepSec, int levels)
        {
            int win = Math.Max(1, (int)Math.Round(windowSec * fs));
            int hop = Math.Max(1, (int)Math.Round(stepSec * fs));
            if (signal == null || signal.Length < win) return new double[0];
            List<double> ent = new List<double>();
            for (int start = 0; start + win <= signal.Length; start += hop)
            {
                double[] slice = new double[win]; Array.Copy(signal, start, slice, 0, win);
                var bands = DwtEnergyBands(slice, levels);
                double sum = bands.Sum(); if (sum <= 0) { ent.Add(0); continue; }
                double H = 0; foreach (var e in bands) { double p = e / sum; if (p > 0) H += -p * Math.Log(p); }
                double Hn = H / Math.Log(bands.Length);
                ent.Add(Hn);
            }
            return ent.ToArray();
        }

        // 主流做法：小波包熵（WPD-Entropy），每个窗口做 Haar 小波包到 L 层，
        // 取末层 2^L 个叶节点的能量，算香农熵并归一化
        private double[] ComputeWaveletPacketEntropySeries(double[] signal, double fs, double windowSec, double stepSec, int levels)
        {
            int win = Math.Max(1, (int)Math.Round(windowSec * fs));
            int hop = Math.Max(1, (int)Math.Round(stepSec * fs));
            if (signal == null || signal.Length < Math.Max(win, 2)) return new double[0];
            List<double> ent = new List<double>();
            for (int start = 0; start + win <= signal.Length; start += hop)
            {
                double[] slice = new double[win]; Array.Copy(signal, start, slice, 0, win);
                var energies = WaveletPacketLeafEnergiesHaar(slice, levels);
                if (energies.Length == 0) { ent.Add(0); continue; }
                double sum = energies.Sum(); if (sum <= 0) { ent.Add(0); continue; }
                double H = 0; for (int i = 0; i < energies.Length; i++) { double p = energies[i] / sum; if (p > 0) H += -p * Math.Log(p); }
                double Hn = H / Math.Log(Math.Max(1, energies.Length));
                ent.Add(Hn);
            }
            return ent.ToArray();
        }

        // Haar 小波包：返回第 L 层 2^L 个叶节点的能量
        private double[] WaveletPacketLeafEnergiesHaar(double[] x, int levels)
        {
            if (x == null || x.Length == 0 || levels <= 0) return new double[0];
            // 对齐到 2^n 长度（零填充）
            int n = 1; while (n < x.Length) n <<= 1; double[] s = new double[n]; Array.Copy(x, s, x.Length);
            List<double[]> nodes = new List<double[]>(); nodes.Add(s);
            double invSqrt2 = 1.0 / Math.Sqrt(2.0);
            for (int lev = 0; lev < levels; lev++)
            {
                var next = new List<double[]>();
                foreach (var node in nodes)
                {
                    int len = node.Length; if (len < 2) { next.Add(node); next.Add(new double[0]); continue; }
                    int half = len / 2; double[] a = new double[half]; double[] d = new double[half];
                    for (int i = 0; i < half; i++)
                    {
                        double s0 = node[2 * i]; double s1 = node[2 * i + 1];
                        a[i] = (s0 + s1) * invSqrt2;
                        d[i] = (s0 - s1) * invSqrt2;
                    }
                    next.Add(a); next.Add(d);
                }
                nodes = next;
            }
            // 叶节点能量
            double[] energies = new double[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                double e = 0; var arr = nodes[i]; if (arr != null) { for (int k = 0; k < arr.Length; k++) e += arr[k] * arr[k]; }
                energies[i] = e;
            }
            return energies;
        }

        // 简易 Haar DWT 能量分解，返回 [D1..DL, AL] 各分量能量
        private double[] DwtEnergyBands(double[] x, int levels)
        {
            // 复制并对齐到 2^n 长度
            int n = 1; while (n < x.Length) n <<= 1; double[] s = new double[n]; Array.Copy(x, s, x.Length);
            int len = n;
            List<double> detailEnergies = new List<double>();
            for (int lev = 0; lev < levels && len >= 2; lev++)
            {
                int half = len / 2;
                double[] a = new double[half];
                double[] d = new double[half];
                for (int i = 0; i < half; i++)
                {
                    double s0 = s[2 * i];
                    double s1 = s[2 * i + 1];
                    a[i] = (s0 + s1) / Math.Sqrt(2.0);
                    d[i] = (s0 - s1) / Math.Sqrt(2.0);
                }
                double eD = 0; for (int i = 0; i < d.Length; i++) eD += d[i] * d[i];
                detailEnergies.Add(eD);
                // 下一层使用近似系数
                for (int i = 0; i < half; i++) s[i] = a[i];
                len = half;
            }
            // 最后一层近似能量
            double eA = 0; for (int i = 0; i < len; i++) eA += s[i] * s[i];
            detailEnergies.Add(eA);
            return detailEnergies.ToArray();
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

                // 第二页：新增分析图片（若存在）
                try
                {
                    var extra = new List<string>();
                    if (Directory.Exists(imgDir))
                    {
                        extra.AddRange(Directory.GetFiles(imgDir, "NF_TF_*.png"));
                        extra.AddRange(Directory.GetFiles(imgDir, "NF_BandTrends_*.png"));
                        extra.AddRange(Directory.GetFiles(imgDir, "AllCh_*.png"));
                    }
                    int idx = 0;
                    while (idx < extra.Count)
                    {
                        ReportPage page2 = new ReportPage
                        {
                            Name = "ExtraPage_" + (idx / 4 + 1),
                            Landscape = true,
                            PaperWidth = 297,
                            PaperHeight = 210
                        };
                        rpt.Pages.Add(page2);
                        page2.TopMargin = 5; page2.BottomMargin = 5; page2.LeftMargin = 5; page2.RightMargin = 5;
                        DataBand b2 = new DataBand { Height = mm(190), Visible = true, Printable = true, RowCount = 1 };
                        page2.Bands.Add(b2);
                        // 依序最多4张
                        void AddPic2(string name, string path, float xMm, float yMm)
                        {
                            if (!File.Exists(path)) return; var pic = new PictureObject { Name = name, Bounds = new System.Drawing.RectangleF(mm(xMm), mm(yMm), mm(135), mm(75)) }; pic.Visible = true; pic.Printable = true; try { using (var bmp = new Bitmap(path)) pic.Image = new Bitmap(bmp); } catch { pic.ImageLocation = path; } b2.Objects.Add(pic);
                        }
                        AddPic2("Extra1", extra[idx++], 10, 0);
                        if (idx < extra.Count) AddPic2("Extra2", extra[idx++], 152, 0);
                        if (idx < extra.Count) AddPic2("Extra3", extra[idx++], 10, 85);
                        if (idx < extra.Count) AddPic2("Extra4", extra[idx++], 152, 85);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError("[Report] Add extra images pages error: " + ex.Message);
                }

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

                // 概要计数（分数峰值稍后用归一化后的 yScore 计算）
                int nReward = events.Count(e=>e.type=="reward_obtained");
                int nPenal  = events.Count(e=>e.type=="penalization_obtained");
                int nStay   = events.Count(e=>e.type=="stay_still");

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

                // 汇总概要（使用归一化分数峰值，展示更直观）
                var summary = new List<string>{ $"奖励次数: {nReward}", $"惩罚次数: {nPenal}", $"静止次数: {nStay}" };

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

                // 顶部图例：颜色含义（左上角，避开y轴刻度与标题）
                try
                {
                    int lgx = lm + 60; // 右移，避免压住刻度
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
            // 数值刻度（y轴）- 自适应格式
            string FormatTickY(double v)
            {
                double range = Math.Abs(ymax - ymin);
                double maxAbs = Math.Max(Math.Abs(ymin), Math.Abs(ymax));
                if (double.IsNaN(range) || double.IsInfinity(range) || range <= 0) return v.ToString();
                if (maxAbs >= 1e4 || maxAbs <= 1e-3)
                    return v.ToString("0.##E0");
                int dec;
                if (range < 1e-3) dec = 6;
                else if (range < 1e-2) dec = 5;
                else if (range < 1e-1) dec = 4;
                else if (range < 1) dec = 3;
                else if (range < 10) dec = 2;
                else if (range < 100) dec = 1;
                else dec = 0;
                return v.ToString("F" + dec);
            }
            for (int i = 0; i <= 6; i++)
            {
                double v = ymin + i * (ymax - ymin) / 6.0;
                float y = (float)(h - bm - (v - ymin) / (ymax - ymin) * (h - tm - bm));
                g.DrawString(FormatTickY(v), SystemFonts.DefaultFont, Brushes.Black, 5, y - 8);
            }
            // 数值刻度（x轴）- 自适应格式并居中对齐
            string FormatTickX(double v)
            {
                double range = Math.Abs(xmax - xmin);
                double maxAbs = Math.Max(Math.Abs(xmin), Math.Abs(xmax));
                if (double.IsNaN(range) || double.IsInfinity(range) || range <= 0) return v.ToString();
                if (maxAbs >= 1e4 || maxAbs <= 1e-3)
                    return v.ToString("0.##E0");
                int dec;
                if (range < 1e-3) dec = 6;
                else if (range < 1e-2) dec = 5;
                else if (range < 1e-1) dec = 4;
                else if (range < 1) dec = 3;
                else if (range < 10) dec = 2;
                else if (range < 100) dec = 1;
                else dec = 0;
                return v.ToString("F" + dec);
            }
            for (int i = 0; i <= 6; i++)
            {
                double v = xmin + i * (xmax - xmin) / 6.0;
                float x = (float)(lm + (v - xmin) / (xmax - xmin) * (w - lm - rm));
                string txt = FormatTickX(v);
                var sz = g.MeasureString(txt, SystemFonts.DefaultFont);
                g.DrawString(txt, SystemFonts.DefaultFont, Brushes.Black, x - sz.Width / 2f, h - bm + 6);
            }
            // y轴标题：放在面板左上角，避免与刻度重叠
            if (!string.IsNullOrEmpty(yLabel))
                g.DrawString(yLabel, SystemFonts.DefaultFont, Brushes.Black, lm + 8, tm - 16);
        }
    }
}
