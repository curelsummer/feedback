using Notification.Wpf;
using Notification.Wpf.Constants;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MultiDevice
{
    public static class Message
    {
        private static readonly NotificationManager __NotificationManager = new NotificationManager();

        // 显示一个过一段时间会消失的气泡
        #region Show

        public static void ShowWarning(string title, string message, TimeSpan? duration = null, Action callback = null)
        {
            __NotificationManager.Show(title, message, NotificationType.Warning, "",
                duration == null ? TimeSpan.MaxValue : duration,

                ShowXbtn: false, CloseOnClick: true,
                onClick: callback
            );
        }
        public static void ShowError(string title, string message, TimeSpan? duration = null, Action callback = null)
        {
            __NotificationManager.Show(title, message, NotificationType.Error, "",
                duration == null ? TimeSpan.MaxValue : duration,
                ShowXbtn: false, 
                CloseOnClick: true,
                onClick: callback
                
            );
        }

        public static void ShowSuccess(string title, string message, TimeSpan? duration = null, Action callback = null)
        {
            __NotificationManager.Show(title, message, NotificationType.Success, "",
                duration == null ? TimeSpan.MaxValue : duration,

                ShowXbtn: false, CloseOnClick: true,
                onClick: callback
            );
        }
        public static void ShowInfo(string title, string message, TimeSpan? duration = null, Action callback = null)
        {
            __NotificationManager.Show(title, message, NotificationType.Notification, "",
                duration == null ? TimeSpan.MaxValue : duration,

                ShowXbtn: false, CloseOnClick: true,
                onClick: callback
            );
        }
        public static void ShowPlain(string title, string message, TimeSpan? duration = null, Action callback = null)
        {
            __NotificationManager.Show(title, message, NotificationType.None, "",
                duration == null ? TimeSpan.MaxValue : duration,

                ShowXbtn: false, CloseOnClick: true,
                onClick: callback
            );
        }
        #endregion

        // 显示一个有明白按钮的气泡，作为【通知】
        #region Notify

        public static void NotifyWarning(string title, string message, Action btn_callback, string btn_text = "明白")
        {
            __NotificationManager.Show(title, message, NotificationType.Warning, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: btn_text,
                LeftButton: btn_callback
            );
        }
        public static void NotifyError(string title, string message, Action btn_callback, string btn_text = "明白")
        {
            __NotificationManager.Show(title, message, NotificationType.Error, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: btn_text,
                LeftButton: btn_callback
            );
        }
        public static void NotifySuccess(string title, string message, Action btn_callback, string btn_text = "明白")
        {
            __NotificationManager.Show(title, message, NotificationType.Success, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: btn_text,
                LeftButton: btn_callback
            );
        }
        public static void NotifyInfo(string title, string message, Action btn_callback, string btn_text = "明白")
        {
            __NotificationManager.Show(title, message, NotificationType.Notification, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: btn_text,
                LeftButton: btn_callback
            );
        }
        public static void NotifyPlain(string title, string message, Action btn_callback, string btn_text = "明白")
        {
            __NotificationManager.Show(title, message, NotificationType.None, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: btn_text,
                LeftButton: btn_callback
            );
        }

        #endregion

        // 显示有确定和取消按钮的气泡，作为【确认】
        #region Confirm

        public static void ConfirmWarning(string title, string message, Action confirm_callback, Action cancel_callback, string confirm_text = "确定", string cancel_text = "取消")
        {
            __NotificationManager.Show(title, message, NotificationType.Warning, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: confirm_text,
                LeftButton: confirm_callback,
                RightButtonText: cancel_text,
                RightButton: cancel_callback
            );
        }
        public static void ConfirmError(string title, string message, Action confirm_callback, Action cancel_callback, string confirm_text = "确定", string cancel_text = "取消")
        {
            __NotificationManager.Show(title, message, NotificationType.Error, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: confirm_text,
                LeftButton: confirm_callback,
                RightButtonText: cancel_text,
                RightButton: cancel_callback
            );
        }
        public static void ConfirmSuccess(string title, string message, Action confirm_callback, Action cancel_callback, string confirm_text = "确定", string cancel_text = "取消")
        {
            __NotificationManager.Show(title, message, NotificationType.Success, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: confirm_text,
                LeftButton: confirm_callback,
                RightButtonText: cancel_text,
                RightButton: cancel_callback
            );
        }
        public static void ConfirmNotification(string title, string message, Action confirm_callback, Action cancel_callback, string confirm_text = "确定", string cancel_text = "取消")
        {
            __NotificationManager.Show(title, message, NotificationType.Notification, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: confirm_text,
                LeftButton: confirm_callback,
                RightButtonText: cancel_text,
                RightButton: cancel_callback
            );
        }
        public static void ConfirmPlain(string title, string message, Action confirm_callback, Action cancel_callback, string confirm_text = "确定", string cancel_text = "取消")
        {
            __NotificationManager.Show(title, message, NotificationType.None, "",
                TimeSpan.MaxValue,

                ShowXbtn: false, CloseOnClick: false,
                LeftButtonText: confirm_text,
                LeftButton: confirm_callback,
                RightButtonText: cancel_text,
                RightButton: cancel_callback
            );
        }


        #endregion

        // 显示一个置顶的简易浮层提示，确保不被全屏遮挡
        public static void ShowSuccessTopMost(string title, string message, TimeSpan? duration = null)
        {
            ShowTopMostToast(title, message, duration ?? TimeSpan.FromSeconds(2), Colors.SeaGreen);
        }
        public static void ShowInfoTopMost(string title, string message, TimeSpan? duration = null)
        {
            ShowTopMostToast(title, message, duration ?? TimeSpan.FromSeconds(2), Colors.SteelBlue);
        }

        private static void ShowTopMostToast(string title, string message, TimeSpan duration, Color accent)
        {
            void show()
            {
                var toast = new Window
                {
                    Width = 420,
                    Height = 90,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };

                var border = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30)),
                    BorderBrush = new SolidColorBrush(accent),
                    BorderThickness = new Thickness(2),
                    Padding = new Thickness(14)
                };

                var panel = new StackPanel { Orientation = Orientation.Vertical };
                var tbTitle = new TextBlock { Text = title, Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold };
                var tbMsg = new TextBlock { Text = message, Foreground = Brushes.White, Margin = new Thickness(0, 6, 0, 0) };
                panel.Children.Add(tbTitle);
                panel.Children.Add(tbMsg);
                border.Child = panel;
                toast.Content = border;

                // 位置：屏幕顶部居中（工作区）
                Rect wa = SystemParameters.WorkArea;
                toast.Left = wa.Left + (wa.Width - toast.Width) / 2;
                toast.Top = wa.Top + 20;

                // 自动关闭
                var timer = new DispatcherTimer { Interval = duration };
                timer.Tick += (s, e) => { timer.Stop(); toast.Close(); };
                toast.Loaded += (s, e) => timer.Start();
                toast.MouseDown += (s, e) => { timer.Stop(); toast.Close(); };

                toast.Show();
            }

            if (Application.Current != null)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    show();
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(show);
                }
            }
            else
            {
                show();
            }
        }

    }

}