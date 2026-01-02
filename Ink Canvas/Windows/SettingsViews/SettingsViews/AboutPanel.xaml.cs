using iNKORE.UI.WPF.Helpers;
using Ink_Canvas.Helpers;
using OSVersionExtension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class AboutPanel : UserControl
    {
        public AboutPanel()
        {
            InitializeComponent();

            Loaded += AboutPanel_Loaded;

            if (File.Exists(App.RootPath + "icc-about-illustrations.png"))
            {
                try
                {
                    CopyrightBannerImage.Visibility = Visibility.Visible;
                    CopyrightBannerImage.Source =
                        new BitmapImage(new Uri($"file://{App.RootPath + "icc-about-illustrations.png"}"));
                }
                catch { }
            }
            else
            {
                CopyrightBannerImage.Visibility = Visibility.Collapsed;
            }

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                var assemblyTitle = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "InkCanvasForClass";
                AboutSoftwareVersion.Text = $"{assemblyTitle} v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
            catch
            {
                AboutSoftwareVersion.Text = "InkCanvasForClass v1.7.18.0";
            }

            UpdateSystemInfo();

            // 以下残留代码已移至 UpdateSystemInfo() 方法，应删除
            /*
            if (buildTime != null)
            {
                var bt = ((DateTimeOffset)buildTime).LocalDateTime;
                var m = bt.Month.ToString().PadLeft(2, '0');
                var d = bt.Day.ToString().PadLeft(2, '0');
                var h = bt.Hour.ToString().PadLeft(2, '0');
                var min = bt.Minute.ToString().PadLeft(2, '0');
                var s = bt.Second.ToString().PadLeft(2, '0');
                AboutBuildTime.Text =
                    $"build-{bt.Year}-{m}-{d}-{h}:{min}:{s}";
            }

            AboutSystemVersion.Text = $"{OSVersion.GetOperatingSystem()} {OSVersion.GetOSVersion().Version}";

            var _t_touch = new Thread(() =>
            {
                var touchcount = TouchTabletDetectHelper.GetTouchTabletDevices().Count;
                var support = TouchTabletDetectHelper.IsTouchEnabled();
                Dispatcher.BeginInvoke(() =>
                    AboutTouchTabletText.Text = $"{touchcount}���豸��{(support ? "֧�ִ����豸" : "�޴���֧��")}");
            });
            _t_touch.Start();
            */

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "? Copyright 2024-2026";
                AboutCopyright.Text = copyright;
                
                if (AboutBottomCopyright != null)
                {
                    var company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
                    if (!string.IsNullOrEmpty(company))
                    {
                        AboutBottomCopyright.Text = $"{copyright} {company} ����";
                    }
                    else
                    {
                        AboutBottomCopyright.Text = copyright;
                    }
                }
            }
            catch
            {
                AboutCopyright.Text = "? Copyright 2024-2026";
                if (AboutBottomCopyright != null)
                {
                    AboutBottomCopyright.Text = "? Copyright 2024-2026";
                }
            }

            if (AboutUserCopyright != null)
            {
                try
                {
                    var deviceId = DeviceIdentifier.GetDeviceId();
                    AboutUserCopyright.Text = deviceId;
                }
                catch
                {
                    AboutUserCopyright.Text = "获取设备ID失败";
                }
            }

            SetupLinkClickHandlers();
            
            UpdateLinkColors();
        }

        private void SetupLinkClickHandlers()
        {
            if (AboutOfficialWebsiteLink != null)
            {
                AboutOfficialWebsiteLink.MouseLeftButtonDown += (s, e) =>
                {
                    OpenUrlInBrowser("https://forum.smart-teach.cn/t/icc-ce");
                };
                AboutOfficialWebsiteLink.Cursor = Cursors.Hand;
            }

            if (AboutGithubLink != null)
            {
                AboutGithubLink.MouseLeftButtonDown += (s, e) =>
                {
                    OpenUrlInBrowser("https://github.com/InkCanvasForClass/community");
                };
                AboutGithubLink.Cursor = Cursors.Hand;
            }

            if (AboutContributorsLink != null)
            {
                AboutContributorsLink.MouseLeftButtonDown += (s, e) =>
                {
                    OpenUrlInBrowser("https://github.com/InkCanvasForClass/community#贡献者");
                };
                AboutContributorsLink.Cursor = Cursors.Hand;
            }
        }

        private void OpenUrlInBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                try
                {
                    Process.Start("cmd", $"/c start {url}");
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine($"�޷�������: {url}, ����: {ex.Message}");
                }
            }
        }

        public static class TouchTabletDetectHelper
        {
            [DllImport("user32.dll")]
            public static extern int GetSystemMetrics(int nIndex);

            public static bool IsTouchEnabled()
            {
                const int MAXTOUCHES_INDEX = 95;
                int maxTouches = GetSystemMetrics(MAXTOUCHES_INDEX);

                return maxTouches > 0;
            }

            public class USBDeviceInfo
            {
                public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
                {
                    DeviceID = deviceID;
                    PnpDeviceID = pnpDeviceID;
                    Description = description;
                }
                public string DeviceID { get; private set; }
                public string PnpDeviceID { get; private set; }
                public string Description { get; private set; }
            }

            public static List<USBDeviceInfo> GetTouchTabletDevices()
            {
                List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

                ManagementObjectCollection collection;
                using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity"))
                    collection = searcher.Get();

                foreach (var device in collection)
                {
                    var name = new StringBuilder((string)device.GetPropertyValue("Name")).ToString();
                    if (!name.Contains("Pentablet")) continue;
                    devices.Add(new USBDeviceInfo(
                        (string)device.GetPropertyValue("DeviceID"),
                        (string)device.GetPropertyValue("PNPDeviceID"),
                        (string)device.GetPropertyValue("Description")
                    ));
                }

                collection.Dispose();
                return devices;
            }
        }

        public static class FileBuildTimeHelper
        {
            public struct _IMAGE_FILE_HEADER
            {
                public ushort Machine;
                public ushort NumberOfSections;
                public uint TimeDateStamp;
                public uint PointerToSymbolTable;
                public uint NumberOfSymbols;
                public ushort SizeOfOptionalHeader;
                public ushort Characteristics;
            };

            public static DateTimeOffset? GetBuildDateTime(Assembly assembly)
            {
                var path = assembly.Location;
                if (File.Exists(path))
                {
                    var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.Position = 0x3C;
                        fileStream.Read(buffer, 0, 4);
                        fileStream.Position = BitConverter.ToUInt32(buffer, 0); 
                        fileStream.Read(buffer, 0, 4); 
                        fileStream.Read(buffer, 0, buffer.Length);
                    }
                    var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));
                        return DateTimeOffset.FromUnixTimeSeconds(coffHeader.TimeDateStamp);
                    }
                    finally
                    {
                        pinnedBuffer.Free();
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public event EventHandler<RoutedEventArgs> IsTopBarNeedShadowEffect;
        public event EventHandler<RoutedEventArgs> IsTopBarNeedNoShadowEffect;

        private void ScrollViewerEx_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset >= 10)
            {
                IsTopBarNeedShadowEffect?.Invoke(this, new RoutedEventArgs());
            }
            else
            {
                IsTopBarNeedNoShadowEffect?.Invoke(this, new RoutedEventArgs());
            }
        }

        private void ScrollBar_Scroll(object sender, RoutedEventArgs e)
        {
            var scrollbar = (ScrollBar)sender;
            var scrollviewer = scrollbar.FindAscendant<ScrollViewer>();
            if (scrollviewer != null) scrollviewer.ScrollToVerticalOffset(scrollbar.Track.Value);
        }

        private void ScrollBarTrack_MouseEnter(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            if (border.Child is Track track)
            {
                track.Width = 16;
                track.Margin = new Thickness(0, 0, -2, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 16;
                var grid = track.FindAscendant<Grid>();
                if (grid.FindDescendantByName("ScrollBarBorderTrackBackground") is Border backgroundBorder)
                {
                    backgroundBorder.Width = 8;
                    backgroundBorder.CornerRadius = new CornerRadius(4);
                    backgroundBorder.Opacity = 1;
                }
                var thumb = track.Thumb.Template.FindName("ScrollbarThumbEx", track.Thumb);
                if (thumb != null)
                {
                    var _thumb = thumb as Border;
                    _thumb.CornerRadius = new CornerRadius(4);
                    _thumb.Width = 8;
                    _thumb.Margin = new Thickness(-0.75, 0, 1, 0);
                    _thumb.Background = new SolidColorBrush(Color.FromRgb(138, 138, 138));
                }
            }
        }

        private void ScrollBarTrack_MouseLeave(object sender, MouseEventArgs e)
        {
            var border = (Border)sender;
            border.Background = new SolidColorBrush(Colors.Transparent);
            border.CornerRadius = new CornerRadius(0);
            if (border.Child is Track track)
            {
                track.Width = 6;
                track.Margin = new Thickness(0, 0, 0, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 6;
                var grid = track.FindAscendant<Grid>();
                if (grid.FindDescendantByName("ScrollBarBorderTrackBackground") is Border backgroundBorder)
                {
                    backgroundBorder.Width = 3;
                    backgroundBorder.CornerRadius = new CornerRadius(1.5);
                    backgroundBorder.Opacity = 0;
                }
                var thumb = track.Thumb.Template.FindName("ScrollbarThumbEx", track.Thumb);
                if (thumb != null)
                {
                    var _thumb = thumb as Border;
                    _thumb.CornerRadius = new CornerRadius(1.5);
                    _thumb.Width = 3;
                    _thumb.Margin = new Thickness(0);
                    _thumb.Background = new SolidColorBrush(Color.FromRgb(195, 195, 195));
                }
            }
        }

        private void ScrollbarThumb_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var thumb = (Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            ((Border)border).Background = new SolidColorBrush(Color.FromRgb(95, 95, 95));
        }

        private void ScrollbarThumb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var thumb = (Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            ((Border)border).Background = new SolidColorBrush(Color.FromRgb(138, 138, 138));
        }
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                UpdateLinkColors();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AboutPanel Ӧ������ʱ����: {ex.Message}");
            }
        }

        private void UpdateLinkColors()
        {
            var linkColor = ThemeHelper.IsDarkTheme 
                ? Color.FromRgb(96, 205, 255)  
                : Color.FromRgb(29, 78, 216);   

            if (AboutOfficialWebsiteLink != null)
            {
                AboutOfficialWebsiteLink.Foreground = new SolidColorBrush(linkColor);
            }
            if (AboutGithubLink != null)
            {
                AboutGithubLink.Foreground = new SolidColorBrush(linkColor);
            }
            if (AboutContributorsLink != null)
            {
                AboutContributorsLink.Foreground = new SolidColorBrush(linkColor);
            }
        }

        private void AboutPanel_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateSystemInfo();
        }

        private void UpdateSystemInfo()
        {
            UpdateUpdateIconVisibility();
            
            try
            {
                AboutSystemVersion.Text = $"{OSVersion.GetOperatingSystem()} {OSVersion.GetOSVersion().Version}";
            }
            catch
            {
                AboutSystemVersion.Text = "未知系统版本";
            }

            try
            {
                var buildTime = FileBuildTimeHelper.GetBuildDateTime(Assembly.GetExecutingAssembly());
                if (buildTime != null)
                {
                    var bt = ((DateTimeOffset)buildTime).LocalDateTime;
                    var m = bt.Month.ToString().PadLeft(2, '0');
                    var d = bt.Day.ToString().PadLeft(2, '0');
                    var h = bt.Hour.ToString().PadLeft(2, '0');
                    var min = bt.Minute.ToString().PadLeft(2, '0');
                    var s = bt.Second.ToString().PadLeft(2, '0');
                    AboutBuildTime.Text = $"build-{bt.Year}-{m}-{d}-{h}:{min}:{s}";
                }
            }
            catch
            {
                AboutBuildTime.Text = "build-未知";
            }

            var _t_touch = new Thread(() =>
            {
                try
                {
                    var touchcount = TouchTabletDetectHelper.GetTouchTabletDevices().Count;
                    var support = TouchTabletDetectHelper.IsTouchEnabled();
                    Dispatcher.BeginInvoke(() =>
                        AboutTouchTabletText.Text = $"{touchcount}个设备，{(support ? "支持触摸设备" : "无触摸支持")}");
                }
                catch
                {
                    Dispatcher.BeginInvoke(() =>
                        AboutTouchTabletText.Text = "检测失败");
                }
            });
            _t_touch.Start();

            try
            {
                if (AboutUserCopyright != null)
                {
                    var deviceId = DeviceIdentifier.GetDeviceId();
                    AboutUserCopyright.Text = deviceId;
                }
            }
            catch
            {
                if (AboutUserCopyright != null)
                {
                    AboutUserCopyright.Text = "获取设备ID失败";
                }
            }
        }

        private void UpdateUpdateIconVisibility()
        {
            try
            {
                if (UpdateAvailableIcon != null)
                {
                    bool hasUpdate = false;
                    try
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            var hasNewUpdateProperty = mainWindow.GetType().GetProperty("HasNewUpdate");
                            if (hasNewUpdateProperty != null)
                            {
                                hasUpdate = (bool)(hasNewUpdateProperty.GetValue(mainWindow) ?? false);
                            }
                            else
                            {
                                var updateInfoProperty = mainWindow.GetType().GetProperty("UpdateInfo");
                                if (updateInfoProperty != null)
                                {
                                    var updateInfo = updateInfoProperty.GetValue(mainWindow);
                                    if (updateInfo != null)
                                    {
                                        var hasUpdateProperty = updateInfo.GetType().GetProperty("HasUpdate");
                                        if (hasUpdateProperty != null)
                                        {
                                            hasUpdate = (bool)(hasUpdateProperty.GetValue(updateInfo) ?? false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        try
                        {
                            var mainWindow = Application.Current.MainWindow as MainWindow;
                            if (mainWindow != null)
                            {
                                var hasUpdateProperty = mainWindow.GetType().GetProperty("HasUpdate");
                                if (hasUpdateProperty != null)
                                {
                                    hasUpdate = (bool)(hasUpdateProperty.GetValue(mainWindow) ?? false);
                                }
                            }
                        }
                        catch { }
                    }
                    
                    UpdateAvailableIcon.Visibility = hasUpdate ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch
            {
                if (UpdateAvailableIcon != null)
                {
                    UpdateAvailableIcon.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
