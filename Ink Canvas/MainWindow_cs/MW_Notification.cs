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
        /// 在当前应用的 MainWindow（如果存在）上显示一条通知文本。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">是否立即展示并覆盖当前正在显示的通知；为 true 时立即显示，false 表示遵从现有显示时机。</param>
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
        /// 在窗口底部以滑入并淡入的方式显示一条通知文本，并在配置的时长后自动隐藏（若未被新通知覆盖）。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">指示是否应立即显示通知；当前实现默认为立即显示。</param>
        /// <remarks>
        /// 如果用于显示的 TextBlockNotice 或 GridNotifications 为 null，则方法不执行任何操作并直接返回。
        /// </remarks>
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