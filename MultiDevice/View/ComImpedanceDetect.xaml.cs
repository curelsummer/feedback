using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MultiDevice.DB;
using MultiDevice.Net;

namespace MultiDevice
{
    /// <summary>
    /// Interaction logic for ImpedanceDetect.xaml
    /// </summary>
    /// 
    public partial class ComImpedanceDetect : Window
    {
        public delegate void ComImpedanceDetectFinishedDeleaget();
        public event ComImpedanceDetectFinishedDeleaget OnFinishedDeleaget;

        public ComSvr svr;
        private ObservableCollection<ElectrodeImpedance> electrode = new ObservableCollection<ElectrodeImpedance>();
        private List<int> electrode_channel = new List<int>();
        public bool startDetect = false;

        public ComImpedanceDetect(GameConfigModel gameConfigModel)
        {
            InitializeComponent();
            ImpedanceList.ItemsSource = electrode;
            Title = $"{gameConfigModel.UserInfo.TestNumber}-{gameConfigModel.UserInfo.Name} 阻抗匹配中...";
        }

        private void whenImpedanceDone(object sender, myEventArgs e)
        {
            ////阻抗检测完成
            for (int i = 0; i < electrode.Count; i++)
            {
                if (electrode_channel[i] != 8)
                    electrode[i].Impedance = impedanceToOhm( this.svr.impedanceValue[electrode_channel[i]]).ToString("F2");
                else
                    electrode[i].Impedance = impedanceToOhm(this.svr.impedanceValue[electrode_channel[i]]).ToString("F2");

            } 
            //UIHelper.DoEvents();
        }
        private double impedanceToOhm(double impedanc)
        {
            Console.WriteLine("impedanc:" + impedanc);
            impedanc = impedanc > 500 ? 500 : impedanc;
            double tmp;
            tmp = (30.0 * impedanc / (510.0 - impedanc)) - 10;
            if (tmp <= 0)
            {
                tmp = 0;
            }
            return tmp;
        }

        public void doingJOB()
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.svr.StartImpedanceDetect();
            this.svr.ImpedanceDone += whenImpedanceDone;

            double Act_width = canvasBoss.Width;
            double Act_height = canvasBoss.Height;

            double cir_R = 40;


            Thickness A1 = new Thickness(cir_R * 0 + cir_R / 2, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness A2 = new Thickness(cir_R * 12 + cir_R / 2, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness REF = new Thickness(cir_R * 6 + cir_R / 2, cir_R * 6 + cir_R / 2, 0, 0);


            Thickness F3 = new Thickness(cir_R * 4.5, cir_R * 3 + cir_R, 0, 0);
            Thickness F4 = new Thickness(cir_R * 7.5 + cir_R, cir_R * 3 + cir_R, 0, 0);
            Thickness Fz=new Thickness(cir_R * 6.5  , cir_R * 3 + cir_R, 0, 0);
            Thickness T3 = new Thickness(cir_R * 2, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness C3 = new Thickness(cir_R * 4, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness C4 = new Thickness(cir_R * 8 + cir_R, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness T4 = new Thickness(cir_R * 10 + cir_R, cir_R * 6 + cir_R / 2, 0, 0);
            Thickness P3 = new Thickness(cir_R * 4.5, cir_R * 8 + cir_R, 0, 0);
            Thickness P4 = new Thickness(cir_R * 7.5 + cir_R, cir_R * 8 + cir_R, 0, 0);
            Thickness Pz = new Thickness(cir_R * 6.5  , cir_R * 8 + cir_R, 0, 0);
            Thickness O1 = new Thickness(cir_R * 5, cir_R * 10 + cir_R / 2, 0, 0);
            Thickness O2 = new Thickness(cir_R * 7 + cir_R, cir_R * 10 + cir_R / 2, 0, 0);
            Thickness Oz = new Thickness(cir_R * 6.5, cir_R * 11 + cir_R / 2, 0, 0);

            Thickness D1 = new Thickness(cir_R * 1.5, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness D2 = new Thickness(cir_R * 2.5, cir_R * 13 + cir_R / 2, 0, 0); 
            Thickness D3 = new Thickness(cir_R * 4, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness D4 = new Thickness(cir_R * 5, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness D5 = new Thickness(cir_R * 6.5, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness D6 = new Thickness(cir_R * 7.5, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness K1 = new Thickness(cir_R * 9, cir_R * 13 + cir_R / 2, 0, 0);
            Thickness K2 = new Thickness(cir_R * 10, cir_R * 13 + cir_R / 2, 0, 0);


            Thickness[] locationList = new Thickness[] { F3, F4, Fz, T3, C3, C4, T4, P3, P4, Pz, O1, O2, Oz, D1, D2, D3, D4, D5, D6,K1,K2 };
            string[] locationNameList = new string[] { "F3", "F4","Fz", "T3", "C3", "C4", "T4", "P3", "P4","Pz", "O1", "O2","Oz", "D1", "D2", "D3", "D4", "D5", "D6","K1","K2" };

            List<int> tmp_list = new List<int>();
            for (int i = 2; i < 8; i++)
            {
                if (!svr.signal_labels[i].Contains("Na"))
                {
                    tmp_list.Add(i);
                }
            }


            Canvas canvasFellow = new Canvas();
            var tmpCanvas = new Canvas();
            var circle = new Ellipse { Width = cir_R, Height = cir_R, Stroke = Brushes.Black };
            ElectrodeImpedance tmp = new ElectrodeImpedance("1", "No Signal", "A1", "0");
            electrode.Add(tmp);
            electrode_channel.Add(0);
            Binding binding = new Binding();
            binding.Source = tmp;
            binding.Path = new PropertyPath("Impedance");
            binding.Converter = new ComImpedanceToPicConverter();
            circle.SetBinding(Ellipse.FillProperty, binding);

            canvasFellow.Children.Add(circle);
            TextBlock text = new  TextBlock { Text = "A1", FontSize = 14 };
            text.Margin = new Thickness(cir_R * 0.25, cir_R, 0, 0);
            canvasFellow.Children.Add(text);
            canvasFellow.Margin = A1;

            canvasBoss.Children.Add(canvasFellow);

            Canvas canvasFellow2 = new Canvas();
            var tmpCanvas2 = new Canvas();
            var circle2 = new Ellipse { Width = cir_R, Height = cir_R, Stroke = Brushes.Black };
            ElectrodeImpedance tmp2 = new ElectrodeImpedance("2", "No Signal", "A2", "0");
            electrode.Add(tmp2);
            electrode_channel.Add(1);
            Binding binding2 = new Binding();
            binding2.Source = tmp2;
            binding2.Path = new PropertyPath("Impedance");
            binding2.Converter = new ComImpedanceToPicConverter();
            circle2.SetBinding(Ellipse.FillProperty, binding2);

            canvasFellow2.Children.Add(circle2);
            TextBlock text2 = new TextBlock { Text = "A2", FontSize = 14 };
            text2.Margin = new Thickness(cir_R * 0.25, cir_R, 0, 0);
            canvasFellow2.Children.Add(text2);
            canvasFellow2.Margin = A2;

            canvasBoss.Children.Add(canvasFellow2);


            for (int i = 0; i < tmp_list.Count; i++)
            {
                string electrode_str = "";
                Thickness electro_thick = REF;

                for (int iii = 0; iii < locationList.Length; iii++)
                {
                    if (svr.signal_labels[tmp_list[i]].Contains(locationNameList[iii]))                             
                    {
                        electro_thick = locationList[iii];
                        electrode_str = locationNameList[iii];
                        break;
                    }
                } 

                Canvas canvasFellow_t = new Canvas();
                var tmpCanvas_t = new Canvas();
                var circle_t = new Ellipse { Width = cir_R, Height = cir_R, Stroke = Brushes.Black };
                ElectrodeImpedance tmp_t = new ElectrodeImpedance((tmp_list[i] + 1).ToString(), "No Signal", electrode_str, "0");
                electrode.Add(tmp_t);
                electrode_channel.Add(tmp_list[i]);
                Binding binding_t = new Binding();
                binding_t.Source = tmp_t;
                binding_t.Path = new PropertyPath("Impedance");
                binding_t.Converter = new ComImpedanceToPicConverter();
                circle_t.SetBinding(Ellipse.FillProperty, binding_t);

                canvasFellow_t.Children.Add(circle_t);
                TextBlock text_t = new TextBlock { Text = electrode_str, FontSize = 14 };
                text_t.Margin = new Thickness(cir_R * 0.25, cir_R, 0, 0);
                canvasFellow_t.Children.Add(text_t);
                canvasFellow_t.Margin = electro_thick;


                canvasBoss.Children.Add(canvasFellow_t);
            }

            Canvas canvasFellow_REF = new Canvas();
            var tmpCanvas_REF = new Canvas();
            var circle_REF = new Ellipse { Width = cir_R, Height = cir_R, Stroke = Brushes.Black };
            ElectrodeImpedance tmp_REF = new ElectrodeImpedance("9", "No Signal", "Cz", "0");
            electrode.Add(tmp_REF);
            electrode_channel.Add(8);
            Binding binding_REF = new Binding();
            binding_REF.Source = tmp_REF;
            binding_REF.Path = new PropertyPath("Impedance");
            binding_REF.Converter = new ComImpedanceToPicConverterFOR9();
            circle_REF.SetBinding(Ellipse.FillProperty, binding_REF);

            canvasFellow_REF.Children.Add(circle_REF);
            TextBlock text_REF = new TextBlock { Text = "Cz", FontSize = 14 };
            text_REF.Margin = new Thickness(cir_R * 0.25, cir_R, 0, 0);
            canvasFellow_REF.Children.Add(text_REF);
            canvasFellow_REF.Margin = REF;

            canvasBoss.Children.Add(canvasFellow_REF);

            Line myline = new Line();
            myline.X1 = cir_R * 7;
            myline.Y1 = 0;
            myline.X2 = cir_R * 6;
            myline.Y2 = cir_R * 1.1;
            myline.Stroke = Brushes.Green;
            myline.StrokeThickness = 2;
            myline.Margin = new Thickness(0, 0, 0, 0);
            canvasBoss.Children.Add(myline);

            Line mySecline = new Line();
            mySecline.X1 = cir_R * 7;
            mySecline.Y1 = 0;
            mySecline.X2 = cir_R * 8;
            mySecline.Y2 = cir_R * 1.1;
            mySecline.Stroke = Brushes.Green;
            mySecline.StrokeThickness = 2;
            mySecline.Margin = new Thickness(0);
            canvasBoss.Children.Add(mySecline);

            Ellipse head = new Ellipse { Height = 12 * cir_R, Width = 12 * cir_R, Stroke = Brushes.Green, StrokeThickness = 2 };
            head.Margin = new Thickness(cir_R, cir_R, 0, 0);
            canvasBoss.Children.Add(head);
        }

        private void BtnStopMeasuring(object sender, RoutedEventArgs e)
        {
            startDetect = true;
            this.svr.StopImpedanceDetect();
            if(null != OnFinishedDeleaget)
            {
                OnFinishedDeleaget.Invoke();
            }
            this.Close();
        }
        private void BtnStopMeasuringold(object sender, RoutedEventArgs e)
        {
            this.svr.StopImpedanceDetect();
            this.Close();
        }
        
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!startDetect)
            {
                // 通知游戏终止
                SQLiteDBService.DB.UpdateUserStatus(App.CurrentUser,  "游戏终止");
                NetHelper.Net.sendMessageToSever(true, Cmd.GameAbort, "游戏终止");
                App.CurrentUser = null;
            }
            this.svr.StopImpedanceDetect();
        }
    }


    public class ComImpedanceToPicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int impedanceValue = (int)double.Parse(value.ToString());
            if (impedanceValue > 216)
            {
                return (Brushes.Red);
            }
            else
                if (216 >= impedanceValue && impedanceValue > 108)
            {
                return (Brushes.Yellow);
            }
            else
                    if (108 >= impedanceValue && impedanceValue > 29.406)
            {
                return (Brushes.Aqua);
            }
            else
                        if (29.406 >= impedanceValue)
            {
                return (Brushes.Chartreuse);
            }
            throw (new Exception("hehe"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class ComImpedanceToPicConverterFOR9 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int impedanceValue = (int)double.Parse(value.ToString());
            if (impedanceValue > 200)
            {
                return (Brushes.Red);
            }
            else
                if (200 >= impedanceValue && impedanceValue > 100)
            {
                return (Brushes.Yellow);
            }
            else
                    if (100 >= impedanceValue && impedanceValue > 30)
            {
                return (Brushes.Aqua);
            }
            else
            
            //if (100 <= impedanceValue && impedanceValue < 1000)
            if (30 >= impedanceValue)
            {
                return (Brushes.Chartreuse);
            }
            throw (new Exception("hehe"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
