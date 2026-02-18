using Ink_Canvas.Helpers;
using System;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private int lastNotificationShowTime;
        private int notificationShowTime = 2500;

        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)
                ?.ShowNotification(notice, isShowImmediately);
        }

        /// <summary>
        /// 显示一条带滑动和淡入动画的通知文本，并在预设显示时长后以滑动和淡出动画自动隐藏。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">保留参数，当前未使用（用于控制是否立即显示的标志）。</param>
        public void ShowNotification(string notice, bool isShowImmediately = true)
        {
            try
            {
                if (TextBlockNotice == null || GridNotifications == null)
                {
                    return;
                }
                lastNotificationShowTime = Environment.TickCount;

                TextBlockNotice.Text = notice;
                AnimationsHelper.ShowWithSlideFromBottomAndFade(GridNotifications);

                new Thread(() =>
                {
                    Thread.Sleep(notificationShowTime + 300);
                    if (Environment.TickCount - lastNotificationShowTime >= notificationShowTime)
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AnimationsHelper.HideWithSlideAndFade(GridNotifications);
                        });
                }).Start();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ShowNotification 异常: {ex.Message}", LogHelper.LogType.Error);
            }
        }
    }
}