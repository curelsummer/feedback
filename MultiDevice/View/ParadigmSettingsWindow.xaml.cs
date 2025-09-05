using LayUI.Wpf.Controls;
using MultiDevice.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiDevice
{
    /// <summary>
    /// ParadigmSettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ParadigmSettingsWindow : Window
    {
        public ParadigmSettingsModel paradigmSettingsModel = null;
        private List<LayComboBox > signalComboBoxes        = new List<LayComboBox>();
        public ParadigmSettingsWindow()
        {
            InitializeComponent();
            DataContext = this;
            paradigmSettingsModel = new ParadigmSettingsModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadParadigmList();

            signalComboBoxes.Add(channel3ComboBox);
            signalComboBoxes.Add(channel4ComboBox);
            signalComboBoxes.Add(channel5ComboBox);
            signalComboBoxes.Add(channel6ComboBox);
            signalComboBoxes.Add(channel7ComboBox);
            signalComboBoxes.Add(channel8ComboBox);
            ParadigmComboBox.SelectedIndex = 0;
        }

        public ParadigmSettingsModel getCurrentSelectParadigmSettings()
        {
            int paradigmIndex = ParadigmComboBox.SelectedIndex;
            if (paradigmIndex < 0)
            {
                return null;
            }

            string paradigmConfig = SQLiteDBService.DB.ReadSettings("paradigmSetting", ParadigmComboBox.Text);
            if (string.IsNullOrEmpty(paradigmConfig))
            {
                return null;
            }

            paradigmSettingsModel = JsonConvert.DeserializeObject<ParadigmSettingsModel>(paradigmConfig);
            return paradigmSettingsModel;
        }

        public void LoadParadigmList()
        {
            // 范式列表
            ParadigmComboBox.ItemsSource = SQLiteDBService.DB.ReadConfigKeys("paradigmSetting");
        }

        private void LoadEmptyConfig()
        {
            foreach (var checkBox in FindVisualChildren<LayCheckBox>(gameList))
            {
                checkBox.IsChecked = false;
            }
            refModeComboBox.SelectedIndex = 0;
            foreach(var signalComboBox in signalComboBoxes)
            {
                signalComboBox.Text = "Nan";
            }
        }

        // 加载配置信息
        private void ParadigmComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int paradigmIndex = ParadigmComboBox.SelectedIndex;
            if (paradigmIndex < 0)
            {
                return;
            }

            LoadEmptyConfig();
            string paradigmConfig = SQLiteDBService.DB.ReadSettings("paradigmSetting", e.AddedItems[0].ToString());
            if (string.IsNullOrEmpty(paradigmConfig))
            {
                return;
            }

            paradigmSettingsModel = JsonConvert.DeserializeObject<ParadigmSettingsModel>(paradigmConfig);

            refModeComboBox.SelectedIndex = paradigmSettingsModel.refMode;
            for (int nIndex = 2; nIndex < 8; nIndex++)
            {
                signalComboBoxes[nIndex - 2].Text = paradigmSettingsModel.signalsLabels[nIndex];
            }

            foreach (var checkBox in FindVisualChildren<LayCheckBox>(gameList))
            {
                if (paradigmSettingsModel.gameList.Contains(checkBox.Content.ToString()))
                {
                    checkBox.IsChecked = true;
                }
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string paradigm = ParadigmComboBox.Text;
            if(paradigm == "")
            {
                Message.ShowError("配置保存", "范式不能为空!");
                return;
            }

            int selectRefMode     = refModeComboBox.SelectedIndex;
            string[] signalLabels = new string[] { "A1", "A2", "Nan", "Nan", "Nan", "Nan", "Nan", "Nan", "Cz" };

            int channel3Index = channel3ComboBox.SelectedIndex;
            int channel4Index = channel4ComboBox.SelectedIndex;
            int channel5Index = channel5ComboBox.SelectedIndex;
            int channel6Index = channel6ComboBox.SelectedIndex;
            int channel7Index = channel7ComboBox.SelectedIndex;
            int channel8Index = channel8ComboBox.SelectedIndex;

            if (channel3Index != 0 && (channel3Index == channel5Index || channel3Index == channel7Index))
            {
                Message.ShowError("通道重复", $"通道3不能与通道5/通道7选择重复!");
                return;
            }

            if(channel4Index != 0 && (channel4Index == channel6Index || channel4Index == channel8Index))
            {
                Message.ShowError("通道重复", $"通道4不能与通道6/通道8选择重复!");
                return;
            }

            if (channel5Index != 0 && (channel5Index == channel3Index || channel5Index == channel7Index))
            {
                Message.ShowError("通道重复", $"通道5不能与通道3/通道7选择重复!");
                return;
            }

            if (channel6Index != 0 && (channel6Index == channel4Index || channel6Index == channel8Index))
            {
                Message.ShowError("通道重复", $"通道6不能与通道4/通道8选择重复!");
                return;
            }

            if (channel8Index != 0 && (channel8Index == channel4Index || channel8Index == channel6Index))
            {
                Message.ShowError("通道重复", $"通道8不能与通道4/通道6选择重复!");
                return;
            }

            if ((channel3Index > 5 && channel3Index < 9) && channel3Index == channel4Index)
            {
                Message.ShowError("通道重复", "通道3不能与通道4选择重复!");
                return;
            }
            if ((channel5Index > 5 && channel5Index < 9) && channel5Index == channel6Index)
            {
                Message.ShowError("通道重复", "通道5不能与通道6选择重复!");
                return;
            }
            if ((channel7Index > 5 && channel7Index < 9) && channel7Index == channel8Index)
            {
                Message.ShowError("通道重复", "通道7不能与通道8选择重复!");
                return;
            }

            if (selectRefMode == 3 || selectRefMode == 4)
            {
                if ((channel3Index > 5 && channel3Index < 9) || 
                    (channel5Index > 5 && channel5Index < 9) ||
                    (channel7Index > 5 && channel7Index < 9) ||
                    (channel4Index > 5 && channel4Index < 9) || 
                    (channel6Index > 5 && channel6Index < 9) ||
                    (channel8Index > 5 && channel8Index < 9))
                {
                    Message.ShowError("提示", "当选择中央区电极时，不可选'同侧/对侧参考'");
                    return;
                }
            }

            if (channel3Index == 9 || channel5Index == 9 || channel7Index == 9)
            {
                if ((channel4Index != 9) && (channel6Index != 9) && (channel8Index != 9))
                {
                    Message.ShowError("提示", "差分信号必须匹配");
                    return;
                }
            }

            if (channel3Index == 10 || channel5Index == 10 || channel7Index == 10)
            {
                if ((channel4Index != 10) && (channel6Index != 10) && (channel8Index != 10))
                {
                    Message.ShowError("提示", "差分信号必须匹配");
                    return;
                }
            }

            if (channel3Index == 11 || channel5Index == 11 || channel7Index == 11)
            {
                if ((channel4Index != 11) && (channel6Index != 11) && (channel8Index != 11))
                {
                    Message.ShowError("提示","差分信号必须匹配");
                    return;
                }
            }
            if (channel3Index == 12 || channel5Index == 12 || channel7Index == 12)
            {
                if ((channel4Index != 12) && (channel6Index != 12) && (channel8Index != 12))
                {
                    Message.ShowError("提示","差分信号必须匹配");
                    return;
                }
            }

            string[] leftLabels  = new string[] { "Nan", "F3", "C3", "T3", "P3", "O1", "Fz", "Pz", "Oz", "D1", "D3", "D5", "K1" };
            string[] rightLabels = new string[] { "Nan", "F4", "C4", "T4", "P4", "O2", "Fz", "Pz", "Oz", "D2", "D4", "D6", "K2" };

            signalLabels[2] = leftLabels[channel3Index];
            signalLabels[4] = leftLabels[channel5Index];
            signalLabels[6] = leftLabels[channel7Index];

            signalLabels[3] = rightLabels[channel4Index];
            signalLabels[5] = rightLabels[channel6Index];
            signalLabels[7] = rightLabels[channel8Index];

            paradigmSettingsModel.signalsLabels = signalLabels;
            paradigmSettingsModel.refMode = selectRefMode;

            // 往数据库存的时候中文注释不存
            string paradigmType = ParadigmComboBox.Text;
            if (paradigmType.Contains("("))
            {
                paradigmType = paradigmType.Substring(0, paradigm.IndexOf('('));
            }

            paradigmSettingsModel.paradigmType = ParadigmComboBox.Text;

            // 游戏列表
            paradigmSettingsModel.gameList.Clear();
            foreach (var checkBox in FindVisualChildren<LayCheckBox>(gameList))
            {
                if (checkBox.IsChecked == true)
                {
                    paradigmSettingsModel.gameList.Add(checkBox.Content.ToString());
                }
                Console.WriteLine($"CheckBox: {checkBox.Content}, IsChecked: {checkBox.IsChecked}");
            }

            // 保存配置
            string configSettings = JObject.FromObject(paradigmSettingsModel).ToString(Newtonsoft.Json.Formatting.None);
            SQLiteDBService.DB.WriteSettings("paradigmSetting", paradigmSettingsModel.paradigmType, configSettings);
            LoadParadigmList();

            Message.ShowSuccess("范式保存", "配置信息保存成功!", TimeSpan.FromSeconds(3));
        }

        // 遍历子控件的辅助方法
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    yield return (T)child;
                }

                foreach (T childOfChild in FindVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        /// <summary>
        /// NetConfig 网络设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NetConfigButton_Click(object sender, RoutedEventArgs e)
        {
            NetConfigForm form = new NetConfigForm();
            form.ShowDialog();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            string currentParadigm = ParadigmComboBox.Text.Trim();
            if(MessageBoxResult.No == MessageBox.Show($"确定删除范式:{currentParadigm} ？", "配置删除", MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                return;
            }

            SQLiteDBService.DB.RemoveSettings("paradigmSetting", currentParadigm);
            LoadParadigmList();
            ParadigmComboBox.SelectedIndex = 0;
            Message.ShowSuccess("配置删除", "操作成功!", TimeSpan.FromSeconds(2));
        }
    }
}
