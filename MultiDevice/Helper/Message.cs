using Notification.Wpf;
using Notification.Wpf.Constants;
using System;

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

    }

}