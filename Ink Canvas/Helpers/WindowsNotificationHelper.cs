using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Windows;

namespace Ink_Canvas.Helpers
{
    internal static class WindowsNotificationHelper
    {
        private const string APP_ID = "InkCanvasForClass.CE";

        /// <summary>
        /// Dispatches a user notification announcing a new application version appropriate for the current Windows release.
        /// </summary>
        /// <param name="version">The version label to display in the notification.</param>
        public static void ShowNewVersionToast(string version)
        {
            try
            {
                var os = Environment.OSVersion.Version;

                if (os.Major == 6 && os.Minor == 1)
                {
                    ShowBalloonForWin7(version);
                }
                else
                {
                    ShowToastForModernWindows(version);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Displays a Windows 7–style taskbar balloon notification announcing a new version.
        /// </summary>
        /// <param name="version">The version string to display in the notification message.</param>
        /// <remarks>
        /// The notification is invoked on the application's UI dispatcher. If the "TaskbarTrayIcon" resource is not present, no notification is shown.
        /// </remarks>
        private static void ShowBalloonForWin7(string version)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                try
                {
                    var taskbar = Application.Current.Resources["TaskbarTrayIcon"] as TaskbarIcon;
                    if (taskbar == null) return;

                    taskbar.Visibility = Visibility.Visible;

                    taskbar.ShowBalloonTip(
                        "InkCanvasForClass CE",
                        $"有新版本：{version}",
                        BalloonIcon.Info);
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Displays a modern Windows toast notification announcing a new application version.
        /// </summary>
        /// <param name="version">The version string to show in the notification.</param>
        private static void ShowToastForModernWindows(string version)
        {
            new ToastContentBuilder()
                .AddText("InkCanvasForClass CE")
                .AddText($"有新版本：{version}")
                .Show();
        }
    }
}