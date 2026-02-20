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
        /// 显示一条通知文本并以从下方滑入并淡入的动画展示，若在指定显示时长后未被覆盖则以滑出并淡出的动画隐藏。
        /// </summary>
        /// <param name="notice">要显示的通知文本。</param>
        /// <param name="isShowImmediately">指示是否立即显示通知。</param>
        /// <remarks>
        /// - 如果用于显示的 UI 元素未初始化（null），方法会直接返回且不抛出异常。 
        /// - 方法内部捕获所有异常并将错误写入日志，不会向调用方抛出异常。 
        /// - 隐藏动作在当前通知显示至少持续设定时长（由类内的 notificationShowTime 控制）后触发；若有新通知到来，会延后隐藏时间以保证最新通知可见。
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