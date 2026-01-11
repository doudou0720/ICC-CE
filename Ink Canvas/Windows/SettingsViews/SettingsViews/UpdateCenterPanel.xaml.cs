using iNKORE.UI.WPF.Helpers;
using Ink_Canvas.Helpers;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class UpdateCenterPanel : UserControl
    {
        private bool isLoaded = false;

        public UpdateCenterPanel()
        {
            InitializeComponent();
            Loaded += UpdateCenterPanel_Loaded;
        }

        private void UpdateCenterPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                isLoaded = true;
                LoadSettings();
                CheckUpdateStatus();
                ApplyTheme();
            }
        }

        private void LoadSettings()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var platform = Environment.Is64BitOperatingSystem ? "windows-x64" : "windows-x86";
                CurrentVersionText.Text = $"当前版本: v{version} | {platform}";

                LoadLastCheckTime();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCenterPanel 加载设置失败: {ex.Message}");
            }
        }

        private void LoadLastCheckTime()
        {
            try
            {
                var lastCheckTimePath = Path.Combine(App.RootPath, "last_update_check.dat");
                if (File.Exists(lastCheckTimePath))
                {
                    var timeStr = File.ReadAllText(lastCheckTimePath);
                    if (DateTime.TryParse(timeStr, out var lastCheckTime))
                    {
                        var now = DateTime.Now;
                        var timeDiff = now - lastCheckTime;
                        
                        if (timeDiff.TotalDays < 1 && lastCheckTime.Date == now.Date)
                        {
                            LastCheckTimeText.Text = $"上次检查时间: 今天, {lastCheckTime:HH:mm}";
                        }
                        else if (timeDiff.TotalDays < 2 && lastCheckTime.Date == now.Date.AddDays(-1))
                        {
                            LastCheckTimeText.Text = $"上次检查时间: 昨天, {lastCheckTime:HH:mm}";
                        }
                        else
                        {
                            LastCheckTimeText.Text = $"上次检查时间: {lastCheckTime:yyyy-MM-dd HH:mm}";
                        }
                        return;
                    }
                }
                LastCheckTimeText.Text = "上次检查时间: 从未";
            }
            catch
            {
                LastCheckTimeText.Text = "上次检查时间: 从未";
            }
        }

        private void SaveLastCheckTime()
        {
            try
            {
                var lastCheckTimePath = Path.Combine(App.RootPath, "last_update_check.dat");
                File.WriteAllText(lastCheckTimePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                LoadLastCheckTime();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存上次检查时间失败: {ex.Message}");
            }
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateStatus(true);
        }

        private void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var method = typeof(MainWindow).GetMethod("CheckForUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(mainWindow, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"立即更新失败: {ex.Message}");
            }
        }

        private void CheckUpdateStatus(bool manualCheck = false)
        {
            UpdateStatusText.Text = "正在检查更新...";
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            CheckUpdateButton.IsEnabled = false;
            StartLoadingAnimation();

            Task.Run(async () =>
            {
                try
                {
                    var updateChannel = UpdateChannel.Release;
                    if (MainWindow.Settings?.Startup != null)
                    {
                        updateChannel = MainWindow.Settings.Startup.UpdateChannel;
                    }

                    var (remoteVersion, lineGroup, releaseNotes) = await AutoUpdateHelper.CheckForUpdates(updateChannel, manualCheck, false);
                    
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        StopLoadingAnimation();
                        
                        if (!string.IsNullOrEmpty(remoteVersion))
                        {
                            var localVersion = Assembly.GetExecutingAssembly().GetName().Version;
                            var localVersionStr = localVersion.ToString();
                            var remoteVersionStr = remoteVersion.TrimStart('v', 'V');
                            
                            Version local = new Version(localVersionStr);
                            Version remote = new Version(remoteVersionStr);
                            
                            if (remote > local)
                            {
                                UpdateStatusText.Text = "有可用更新";
                                LatestVersionText.Text = $"版本 {remoteVersion} 现已可用";
                                UpdateAvailablePanel.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                UpdateStatusText.Text = "你使用的是最新版本";
                                UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                            }
                        }
                        else
                        {
                            UpdateStatusText.Text = "你使用的是最新版本";
                            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                        }
                        
                        if (manualCheck)
                        {
                            SaveLastCheckTime();
                        }
                        
                        CheckUpdateButton.IsEnabled = true;
                    }));
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        StopLoadingAnimation();
                        UpdateStatusText.Text = "检查更新失败";
                        UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                        CheckUpdateButton.IsEnabled = true;
                    }));
                    System.Diagnostics.Debug.WriteLine($"检查更新状态失败: {ex.Message}");
                }
            });
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

        private void ScrollBarTrack_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            if (border.Child is Track track)
            {
                track.Width = 16;
                track.Margin = new Thickness(0, 0, -2, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 16;
            }
        }

        private void ScrollBarTrack_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            if (border.Child is Track track)
            {
                track.Width = 6;
                track.Margin = new Thickness(0, 0, 0, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 6;
            }
        }

        private void ScrollbarThumb_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            if (border is Border borderElement)
            {
                borderElement.Background = new SolidColorBrush(Color.FromRgb(95, 95, 95));
            }
        }

        private void ScrollbarThumb_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            if (border is Border borderElement)
            {
                borderElement.Background = new SolidColorBrush(Color.FromRgb(195, 195, 195));
            }
        }

        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                
                if (CheckUpdateButton != null)
                {
                    CheckUpdateButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    CheckUpdateButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    CheckUpdateButton.BorderBrush = ThemeHelper.GetBorderPrimaryBrush();
                }
                
                if (UpdateNowButton != null)
                {
                    UpdateNowButton.Background = new SolidColorBrush(Color.FromRgb(0, 120, 212));
                    UpdateNowButton.Foreground = Brushes.White;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCenterPanel 应用主题时出错: {ex.Message}");
            }
        }

        private void StartLoadingAnimation()
        {
            try
            {
                if (LoadingSpinner != null)
                {
                    LoadingSpinner.IsActive = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动加载动画失败: {ex.Message}");
            }
        }

        private void StopLoadingAnimation()
        {
            try
            {
                if (LoadingSpinner != null)
                {
                    LoadingSpinner.IsActive = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"停止加载动画失败: {ex.Message}");
            }
        }
    }
}
