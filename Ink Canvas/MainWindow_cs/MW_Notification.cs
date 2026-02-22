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

        /// <summary>
        /// 静态方法，用于在主窗口中显示通知
        /// </summary>
        /// <param name="notice">要显示的通知文本</param>
        /// <param name="isShowImmediately">指示是否应立即显示通知</param>
        /// <remarks>
        /// 该方法会：
        /// 1. 获取应用程序中的主窗口实例
        /// 2. 调用主窗口的ShowNotification方法显示通知
        /// <summary>
        /// 将通知消息转发到应用程序的主窗口以显示。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">是否立即显示通知；为 true 时优先展示，false 时可允许正常延迟或替换策略。</param>
        public static void ShowNewMessage(string notice, bool isShowImmediately = true)
        {
            (Application.Current?.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow)
                ?.ShowNotification(notice, isShowImmediately);
        }

        /// <summary>
        /// 在窗口中显示带从底部滑入并淡入的通知文本，并在配置的时长后自动隐藏（若未被新通知覆盖）。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <summary>
        /// 在主窗口显示一条带动画的临时通知并在超时后自动隐藏。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">指示是否应立即显示通知；默认为 true。若通知容器不可用则不执行任何操作。</param>
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