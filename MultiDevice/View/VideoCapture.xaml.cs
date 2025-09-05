using Accord.Audio;
using Accord.Audio.Formats;
using Accord.DirectSound;
using Accord.Video.FFMPEG;
using AForge.Controls;
using AForge.Video;

using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MultiDevice
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class VideoCapture : Window
    {
        public delegate void VideoEvent(object sender, myVideoEventArgs e);
        public event VideoEvent sendMessage;
        public event VideoEvent errorMessage;


        VideoSourcePlayer videoSourcePlayer;
        private FilterInfoCollection videoDevices;
        private AudioDeviceCollection audioDevices;
        private VideoCaptureDevice camera;
        private AudioCaptureDevice Mic;
        private FileStream stream;
        private WaveEncoder encoder;

        private VideoFileWriter videoWriter = null;
        private bool recording_video = false;
        Stopwatch stw = new Stopwatch();
        bool need_audio_r = false;
        bool video_flushed = false;
        bool need_shot = false;

        public bool VideoRecording
        {
            get { return recording_video; }
        }
        public string FileDir
        {
            get
            {
                return video_file_path;
            }
            set
            {
                if (!recording_video)
                {
                    video_file_path = value;
                }
            }
        }
        public string FileName
        {
            get
            {
                return video_file_name;
            }
            set
            {
                if (!recording_video)
                {
                    video_file_name = value;
                }
            }
        }

        System.Drawing.Bitmap bmp1 = null;
        int fps = 30;

        double num_of_frame_rcvd = 0;
        double num_of_frame_rcvd_old = 0;
        double a_num_of_frame_rcvd = 0;
        double a_num_of_frame_rcvd_old = 0;
        private DispatcherTimer timer;


        string video_file_path = "";
        string video_file_name = "";
        string date_str = "";


        int video_width, video_height;

        public VideoCapture()
        {
            InitializeComponent();
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            audioDevices = new AudioDeviceCollection(AudioDeviceCategory.Capture);
            if (audioDevices.Count() == 0)
            {
                combo_a_devices.IsEnabled = false;
                need_audio.IsEnabled = false;
            }
            if (videoDevices.Capacity == 0)
            {
                MessageBox.Show("没有视频输入设备");
                this.Close();
            }
            else
            {
                video_grid.Children.Clear();
                combo_v_devices.Items.Clear();
                foreach (FilterInfo device in videoDevices)
                {
                    combo_v_devices.Items.Add(device.Name);
                }

                combo_a_devices.Items.Clear();
                foreach (var device in audioDevices)
                {
                    combo_a_devices.Items.Add(device);
                }

            }

        }

        private void video_Start_Click(object sender, RoutedEventArgs e)
        {
            if (videoDevices.Count != combo_v_devices.Items.Count)
            {
                MessageBox.Show("视频采集设备有改变，请关闭此界面重新开始", "硬件设备问题");
                return;
            }
            if (audioDevices.Count() != combo_a_devices.Items.Count)
            {

                MessageBox.Show("音频采集设备有改变，请关闭此界面重新开始", "硬件设备问题");
                return;
            }


            //System.Windows.Forms.FolderBrowserDialog dilog = new System.Windows.Forms.FolderBrowserDialog();
            //dilog.Description = "请选择数据存储文件夹";
            //System.Windows.Forms.DialogResult results = dilog.ShowDialog();
            //if (results == System.Windows.Forms.DialogResult.OK || results == System.Windows.Forms.DialogResult.Yes)
            //{
            //    video_file_path = dilog.SelectedPath;
            //    video_file_name = DateTime.Now.ToString("yyyyMMdd_HH-mm-ss");
            //}
            //else
            //    return;


            var date_strz = DateTime.Now.ToString("_dd-MMM-yyyy_HH.mm.ss.fff", System.Globalization.CultureInfo.CreateSpecificCulture("en-GB"));
            date_strz = date_strz.ToUpper() + "s";

            var new_str = date_strz.ToCharArray();
            new_str[new_str.Length - 8] = 'm';
            new_str[new_str.Length - 11] = 'h';

            date_str = new string(new_str); ;

            if (!Directory.Exists(video_file_path))
            {
                MessageBox.Show("文件夹不存在");
                return;
            }
            if (File.Exists(System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".avi")))
            {
                MessageBox.Show("视频文件已存在");
                return;
            }

            if (need_audio.IsChecked == true && audioDevices.Count() > 0)
            {
                AudioDeviceInfo info = combo_a_devices.SelectedItem as AudioDeviceInfo;
                Mic = new AudioCaptureDevice(info)
                {
                    DesiredFrameSize = 4096 * 5,
                    SampleRate = 22050,

                    // We will be reading 16-bit PCM
                    Format = SampleFormat.Format16Bit
                };
                Mic.SampleRate = 22050;
                Mic.NewFrame += source_NewFrame;
                Mic.AudioSourceError += mic_AudioSourceError;
                stream = new FileStream(System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".mp3"), FileMode.OpenOrCreate);
                encoder = new WaveEncoder(stream);
                Mic.Start();
            }
            else
            {
                Mic = null;
            }


            String video_name = System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".avi");
            video_name = System.IO.Path.ChangeExtension(video_name, "mpeg");


            hint_label.Content = video_name;
            Console.WriteLine("" + video_name);
            videoWriter = new VideoFileWriter();
            if (videoSourcePlayer.IsRunning)
            {
                videoWriter.Width = video_width;
                videoWriter.Height = video_height;

                Console.WriteLine("writer size:" + video_width + "x" + video_height);

                videoWriter.FrameRate = fps;
                videoWriter.BitRate = videoWriter.BitRate * (combo_v_quality.SelectedIndex * 4 + 1);
                videoWriter.VideoCodec = VideoCodec.Mpeg4;

                videoWriter.Open(System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".avi"));
            }
            else
            {
                //throw new Exception("没有视频源输入，无法录制视频。");
                errorMessage?.Invoke(this, new myVideoEventArgs("No Input Video"));
                save_and_close();
            }
            video_time.Content = "";
            need_audio_r = (need_audio.IsChecked == true);
            stw.Reset();
            stw.Start();
            recording_video = true;

            record_end.IsEnabled = true;
            take_shot.IsEnabled = true;
            combo_v_devices.IsEnabled = false;
            combo_a_devices.IsEnabled = false;
            combo_resolution.IsEnabled = false;
            combo_v_quality.IsEnabled = false;
            video_Start.IsEnabled = false;
            need_audio.IsEnabled = false;


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += Timer_Tick; timer.IsEnabled = true;
            sendMessage?.Invoke(this, new myVideoEventArgs("Video Start"));
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (num_of_frame_rcvd - num_of_frame_rcvd_old < 30)
            {
                //throw new Exception("frame not updated.");
                errorMessage?.Invoke(this, new myVideoEventArgs("Video Frame Not Updated"));
                save_and_close();
            }
            else
            {
                num_of_frame_rcvd_old = num_of_frame_rcvd;
            }
            if (a_num_of_frame_rcvd - a_num_of_frame_rcvd_old < 1 && recording_video && need_audio_r)
            {
                //throw new Exception("audio frame not updated.");

                errorMessage?.Invoke(this, new myVideoEventArgs("Audio Frame Not Updated"));
                save_and_close();
            }
            else
            {
                a_num_of_frame_rcvd_old = a_num_of_frame_rcvd;
            }
        }

        private void videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            num_of_frame_rcvd++;
            if (recording_video)
            {
                var ccc = stw.Elapsed;
                Console.WriteLine("Milliseconds:" + ccc.TotalMilliseconds);
                bmp1 = AForge.Imaging.Image.Clone(eventArgs.Frame);
                if (need_shot)
                {
                    String pic_name = System.IO.Path.Combine(video_file_path, video_file_name + "_" + DateTime.Now.ToString("HH-mm-ss") + "_snapshot.png");
                    bmp1.Save(pic_name, System.Drawing.Imaging.ImageFormat.Png);
                    sendMessage?.Invoke(this, new myVideoEventArgs("shot"));
                    need_shot = false;
                }

                try
                {

                    videoWriter.WriteVideoFrame(bmp1, ccc);
                    bmp1.Dispose();
                }
                catch (IOException ee)
                {
                    Console.WriteLine("error:" + ee.Message);
                    errorMessage?.Invoke(this, new myVideoEventArgs("Writing Frame Error"));
                    save_and_close();
                }
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    video_time.Content = ccc.ToString("c");
                });

                if (ccc.Minutes % 5 == 0 && !video_flushed)
                {
                    videoWriter.Flush();
                    video_flushed = true;
                    GC.Collect();
                }
                if (ccc.Minutes % 5 > 0)
                {
                    video_flushed = false;
                }
            }
        }
        private void source_NewFrame(object sender, Accord.Audio.NewFrameEventArgs e)
        {
            if (recording_video && need_audio_r)
            {
                encoder.Encode(e.Signal);
                a_num_of_frame_rcvd++;
            }
        }

        private void mic_AudioSourceError(object sender, AudioSourceErrorEventArgs e)
        {
            Console.WriteLine("error:" + e.Exception.Message);
            errorMessage?.Invoke(this, new myVideoEventArgs("Audio Source Error"));
            save_and_close();
        }


        private void take_shot_Click(object sender, RoutedEventArgs e)
        {
            need_shot = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (recording_video)
            {
                //this.Hide();
                MessageBox.Show("请先停止视频录制");
                e.Cancel = true;
            }
            else
            {
                recording_video = false;
                if (videoSourcePlayer != null)
                {
                    videoSourcePlayer.SignalToStop();
                    videoSourcePlayer.WaitForStop();
                }
                if (need_audio.IsChecked == true && Mic != null)
                {
                    Mic.SignalToStop();
                    Mic.WaitForStop();
                }
                errorMessage?.Invoke(this, new myVideoEventArgs("video window closed"));
            }
        }

        private void combo_resolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.SignalToStop();
                videoSourcePlayer.WaitForStop();
                if (camera != null)
                {
                    camera.NewFrame -= new NewFrameEventHandler(videoSource_NewFrame);
                    camera.VideoSourceError -= Camera_VideoSourceError;
                }
                if (combo_resolution.Items.Count > 0)
                {
                    camera = new VideoCaptureDevice(videoDevices[combo_v_devices.SelectedIndex].MonikerString);
                    camera.VideoResolution = camera.VideoCapabilities[combo_resolution.SelectedIndex];
                    videoSourcePlayer.VideoSource = camera;
                    camera.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                    camera.VideoSourceError += Camera_VideoSourceError;
                    num_of_frame_rcvd = 0;
                    num_of_frame_rcvd_old = 0;
                    videoSourcePlayer.Start();

                    video_width = camera.VideoResolution.FrameSize.Width;
                    video_height = camera.VideoResolution.FrameSize.Height;

                    videoSourcePlayer.Height = video_height;
                    videoSourcePlayer.Width = video_width;

                    this.Width = video_width;
                    this.Height = video_height * 12 / 10.0;

                    Console.WriteLine("combo_resolution.SelectedIndex:" + combo_resolution.SelectedIndex);
                }
            }
            GC.Collect();
        }

        private void Camera_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            errorMessage?.Invoke(this, new myVideoEventArgs("Video Source Error"));
            save_and_close();
        }

        private void record_end_Click(object sender, RoutedEventArgs e)
        {
            sendMessage?.Invoke(this, new myVideoEventArgs("Video End:end_click"));
            timer.IsEnabled = false;
            timer.Tick -= Timer_Tick;

            num_of_frame_rcvd = 0;
            num_of_frame_rcvd_old = 0;
            a_num_of_frame_rcvd = 0;
            a_num_of_frame_rcvd_old = 0;
            recording_video = false;
            if (videoWriter != null)
            {
                videoWriter.Close();
                videoWriter.Dispose();
            }
            videoWriter = null;
            var tmp_str = System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".avi");
            Console.WriteLine(File.Exists(tmp_str));
            if (File.Exists(tmp_str))
            {
                var new_name = System.IO.Path.ChangeExtension(tmp_str, "mpeg");
                FileInfo f = new FileInfo(tmp_str);
                f.MoveTo(new_name);
            }


            try
            {
                stream.Close();
                encoder.Close();
                stream.Dispose();
            }
            catch (Exception ee) { }

            stream = null;
            encoder = null;
            if (need_audio.IsChecked == true && Mic != null)
            {
                Mic.SignalToStop();
                Mic.WaitForStop();
            }
            hint_label.Content = "";
            record_end.IsEnabled = false;
            combo_v_devices.IsEnabled = true;
            combo_resolution.IsEnabled = true;
            combo_v_quality.IsEnabled = true;
            take_shot.IsEnabled = false;
            video_Start.IsEnabled = true;

            if (audioDevices.Count() == 0)
            {
                combo_a_devices.IsEnabled = false;
                need_audio.IsEnabled = false;
            }
            else
            {
                need_audio.IsEnabled = true;
                combo_a_devices.IsEnabled = true;
            }
            stream = null;
            encoder = null;
            Mic = null;

            GC.Collect();


        }

        private void combo_v_devices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (videoSourcePlayer != null)
            {
                videoSourcePlayer.SignalToStop();
                videoSourcePlayer.WaitForStop();
            }
            if (camera != null)
            {
                camera.NewFrame -= new NewFrameEventHandler(videoSource_NewFrame);
                camera.VideoSourceError -= Camera_VideoSourceError;

            }
            camera = new VideoCaptureDevice(videoDevices[combo_v_devices.SelectedIndex].MonikerString);
            videoSourcePlayer = new VideoSourcePlayer();
            WindowsFormsHost WFH = new WindowsFormsHost();
            WFH.Child = videoSourcePlayer;

            video_grid.Children.Clear();
            video_grid.Children.Add(WFH);

            combo_resolution.Items.Clear();
            for (int iii = 0; iii < camera.VideoCapabilities.Length; iii++)
            {
                combo_resolution.Items.Add(camera.VideoCapabilities[iii].FrameSize.Width + "X" + camera.VideoCapabilities[iii].FrameSize.Height);
                Console.WriteLine("size:" + camera.VideoCapabilities[iii].FrameSize.Width + "X" + camera.VideoCapabilities[iii].FrameSize.Height);
            }
            combo_resolution.SelectedIndex = 0;

            GC.Collect();
        }



        public void save_and_close()
        {
            if (recording_video)
            {
                errorMessage?.Invoke(this, new myVideoEventArgs("Video End:evoked_end"));
                timer.IsEnabled = false;
            }
            recording_video = false;
            if (videoSourcePlayer != null)
            {
                this.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    videoSourcePlayer.SignalToStop();
                    videoSourcePlayer.WaitForStop();
                });
            }
            if (videoWriter != null)
            {
                videoWriter.Close();
                videoWriter.Dispose();
                videoWriter = null;
            }
            try
            {
                stream.Close();
                encoder.Close();
                stream.Dispose();
            }
            catch (Exception e) { }

            stream = null;
            encoder = null;
            if (Mic != null)
            {
                Mic.SignalToStop();
                Mic.WaitForStop();
            }


            if (need_audio.IsChecked == true && Mic != null)
            {
                Mic.SignalToStop();
                Mic.WaitForStop();
            }
            stream = null;
            encoder = null;
            Mic = null;

            var tmp_str = System.IO.Path.Combine(video_file_path, video_file_name + date_str + ".avi");
            Console.WriteLine(File.Exists(tmp_str));
            if (File.Exists(tmp_str))
            {
                var new_name = System.IO.Path.ChangeExtension(tmp_str, "mpeg");
                FileInfo f = new FileInfo(tmp_str);
                f.MoveTo(new_name);
            }
            this.Close();
        }
    }
}
