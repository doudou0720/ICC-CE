using iNKORE.UI.WPF.Helpers;
using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class UpdateCenterPanel : UserControl
    {
        private bool _isLoaded = false;
        private string _currentTab = "Update";
        private List<(string version, string downloadUrl, string releaseNotes)> _historyVersions = new List<(string, string, string)>();

        public UpdateCenterPanel()
        {
            InitializeComponent();
            Loaded += UpdateCenterPanel_Loaded;
        }

        private void UpdateCenterPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded)
            {
                _isLoaded = true;
                LoadSettings();
                SwitchTab("Update");
                CheckUpdateStatus();
                LoadUpdateLog(MainWindow.Settings?.Startup?.UpdateChannel ?? UpdateChannel.Release);
                ApplyTheme();
            }
        }

        private void LoadSettings()
        {
            if (MainWindow.Settings == null) return;

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var platform = Environment.Is64BitOperatingSystem ? "windows-x64" : "windows-x86";
                CurrentVersionText.Text = $"当前版本: v{version} | {platform}";

                LoadLastCheckTime();

                var toggleSwitchIsAutoUpdate = FindToggleSwitch("ToggleSwitchIsAutoUpdate");
                if (toggleSwitchIsAutoUpdate != null)
                {
                    bool isAutoUpdate = MainWindow.Settings.Startup.IsAutoUpdate;
                    SetToggleSwitchState(toggleSwitchIsAutoUpdate, isAutoUpdate);
                }

                var toggleSwitchIsAutoUpdateWithSilence = FindToggleSwitch("ToggleSwitchIsAutoUpdateWithSilence");
                if (toggleSwitchIsAutoUpdateWithSilence != null)
                {
                    bool isAutoUpdateWithSilence = MainWindow.Settings.Startup.IsAutoUpdateWithSilence;
                    SetToggleSwitchState(toggleSwitchIsAutoUpdateWithSilence, isAutoUpdateWithSilence);
                    AutoUpdateWithSilencePanel.Visibility = MainWindow.Settings.Startup.IsAutoUpdate ? Visibility.Visible : Visibility.Collapsed;
                    AutoUpdateTimePeriodSeparator.Visibility = MainWindow.Settings.Startup.IsAutoUpdate ? Visibility.Visible : Visibility.Collapsed;
                }

                if (AutoUpdateTimePeriodBlock != null)
                {
                    AutoUpdateTimePeriodBlock.Visibility = 
                        (MainWindow.Settings.Startup.IsAutoUpdateWithSilence && MainWindow.Settings.Startup.IsAutoUpdate) ?
                        Visibility.Visible : Visibility.Collapsed;
                }

                if (AutoUpdateWithSilenceStartTimeComboBox != null)
                {
                    var startTime = MainWindow.Settings.Startup.AutoUpdateWithSilenceStartTime ?? "06:00";
                    var startItem = AutoUpdateWithSilenceStartTimeComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Tag?.ToString() == startTime.Replace(":", ""));
                    if (startItem != null)
                    {
                        AutoUpdateWithSilenceStartTimeComboBox.SelectedItem = startItem;
                    }
                }

                if (AutoUpdateWithSilenceEndTimeComboBox != null)
                {
                    var endTime = MainWindow.Settings.Startup.AutoUpdateWithSilenceEndTime ?? "22:00";
                    var endItem = AutoUpdateWithSilenceEndTimeComboBox.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Tag?.ToString() == endTime.Replace(":", ""));
                    if (endItem != null)
                    {
                        AutoUpdateWithSilenceEndTimeComboBox.SelectedItem = endItem;
                    }
                }

                if (MainWindow.Settings.Startup.UpdateChannel == UpdateChannel.Release)
                {
                    UpdateUpdateChannelButtons(UpdateChannel.Release);
                }
                else if (MainWindow.Settings.Startup.UpdateChannel == UpdateChannel.Preview)
                {
                    UpdateUpdateChannelButtons(UpdateChannel.Preview);
                }
                else
                {
                    UpdateUpdateChannelButtons(UpdateChannel.Beta);
                }
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
                var lastCheckTime = DeviceIdentifier.GetLastUpdateCheck();
                if (lastCheckTime != DateTime.MinValue)
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
                DeviceIdentifier.RecordUpdateCheck();
                LoadLastCheckTime();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存上次检查时间失败: {ex.Message}");
            }
        }

        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            SwitchTab(tag);
        }

        private void SwitchTab(string tabName)
        {
            _currentTab = tabName;
            bool isDarkTheme = ThemeHelper.IsDarkTheme;
            var selectedBrush = isDarkTheme ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Color.FromRgb(225, 225, 225));
            var unselectedBrush = new SolidColorBrush(Colors.Transparent);

            TabUpdate.Background = tabName == "Update" ? selectedBrush : unselectedBrush;
            var updateText = TabUpdate.Child as TextBlock;
            if (updateText != null)
            {
                updateText.FontWeight = tabName == "Update" ? FontWeights.Bold : FontWeights.Normal;
                updateText.Foreground = ThemeHelper.GetTextPrimaryBrush();
            }

            TabRollback.Background = tabName == "Rollback" ? selectedBrush : unselectedBrush;
            var rollbackText = TabRollback.Child as TextBlock;
            if (rollbackText != null)
            {
                rollbackText.FontWeight = tabName == "Rollback" ? FontWeights.Bold : FontWeights.Normal;
                rollbackText.Foreground = ThemeHelper.GetTextPrimaryBrush();
            }

            TabSettings.Background = tabName == "Settings" ? selectedBrush : unselectedBrush;
            var settingsText = TabSettings.Child as TextBlock;
            if (settingsText != null)
            {
                settingsText.FontWeight = tabName == "Settings" ? FontWeights.Bold : FontWeights.Normal;
                settingsText.Foreground = ThemeHelper.GetTextPrimaryBrush();
            }

            UpdateTabContent.Visibility = tabName == "Update" ? Visibility.Visible : Visibility.Collapsed;
            RollbackTabContent.Visibility = tabName == "Rollback" ? Visibility.Visible : Visibility.Collapsed;
            SettingsTabContent.Visibility = tabName == "Settings" ? Visibility.Visible : Visibility.Collapsed;

            if (tabName == "Rollback" && _historyVersions.Count == 0)
            {
                LoadHistoryVersions();
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
                MainWindowSettingsHelper.InvokeMainWindowMethod("ManualUpdateButton_Click", sender, e);
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

                        if (!string.IsNullOrEmpty(releaseNotes))
                        {
                            UpdateLogViewer.Markdown = releaseNotes;
                        }
                        else
                        {
                            LoadUpdateLog(updateChannel);
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

        private async void LoadUpdateLog(UpdateChannel channel)
        {
            try
            {
                var updateLog = await AutoUpdateHelper.GetUpdateLog(channel);
                if (!string.IsNullOrEmpty(updateLog))
                {
                    UpdateLogViewer.Markdown = updateLog;
                }
                else
                {
                    UpdateLogViewer.Markdown = "暂无更新日志";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载更新日志失败: {ex.Message}");
                UpdateLogViewer.Markdown = "加载更新日志失败";
            }
        }

        private async void LoadHistoryVersions()
        {
            try
            {
                HistoryLogViewer.Markdown = "正在加载历史版本...";
                RollbackVersionComboBox.Items.Clear();

                var updateChannel = UpdateChannel.Release;
                if (MainWindow.Settings?.Startup != null)
                {
                    updateChannel = MainWindow.Settings.Startup.UpdateChannel;
                }

                _historyVersions = await AutoUpdateHelper.GetAllGithubReleases(updateChannel);
                
                if (_historyVersions.Count > 0)
                {
                    var markdown = new System.Text.StringBuilder();
                    markdown.AppendLine("# 历史版本列表\n");
                    
                    foreach (var (version, downloadUrl, releaseNotes) in _historyVersions)
                    {
                        markdown.AppendLine($"## {version}\n");
                        if (!string.IsNullOrEmpty(releaseNotes))
                        {
                            var notes = releaseNotes.Length > 200 ? releaseNotes.Substring(0, 200) + "..." : releaseNotes;
                            markdown.AppendLine(notes);
                        }
                        markdown.AppendLine("\n---\n");
                    }
                    
                    HistoryLogViewer.Markdown = markdown.ToString();

                    foreach (var versionInfo in _historyVersions)
                    {
                        var item = new ComboBoxItem
                        {
                            Content = versionInfo.version,
                            Tag = versionInfo
                        };
                        RollbackVersionComboBox.Items.Add(item);
                    }
                }
                else
                {
                    HistoryLogViewer.Markdown = "未获取到历史版本信息。";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载历史版本失败: {ex.Message}");
                HistoryLogViewer.Markdown = "加载历史版本失败";
            }
        }

        private void RollbackVersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RollbackVersionComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is ValueTuple<string, string, string> versionInfo)
            {
                var version = versionInfo.Item1;
                var downloadUrl = versionInfo.Item2;
                var releaseNotes = versionInfo.Item3;
                
                if (!string.IsNullOrEmpty(releaseNotes))
                {
                    HistoryLogViewer.Markdown = $"# {version}\n\n{releaseNotes}";
                }
                else
                {
                    HistoryLogViewer.Markdown = $"# {version}\n\n无更新日志";
                }
            }
        }

        private void RollbackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var updateChannel = UpdateChannel.Release;
                if (MainWindow.Settings?.Startup != null)
                {
                    updateChannel = MainWindow.Settings.Startup.UpdateChannel;
                }
                var rollbackWindow = new HistoryRollbackWindow(updateChannel);
                rollbackWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开历史版本回滚窗口失败: {ex.Message}");
            }
        }

        private void FixVersionButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("FixVersionButton_Click", sender, e);
        }

        private void OptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "UpdateChannel_Release":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Release;
                    }, "UpdateChannelSelector");
                    MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                        new System.Windows.Controls.RadioButton { Tag = "Release" }, e);
                    UpdateUpdateChannelButtons(UpdateChannel.Release);
                    LoadHistoryVersions();
                    LoadUpdateLog(UpdateChannel.Release);
                    break;

                case "UpdateChannel_Preview":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Preview;
                    }, "UpdateChannelSelector");
                    MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                        new System.Windows.Controls.RadioButton { Tag = "Preview" }, e);
                    UpdateUpdateChannelButtons(UpdateChannel.Preview);
                    LoadHistoryVersions();
                    LoadUpdateLog(UpdateChannel.Preview);
                    break;

                case "UpdateChannel_Beta":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Beta;
                    }, "UpdateChannelSelector");
                    MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                        new System.Windows.Controls.RadioButton { Tag = "Beta" }, e);
                    UpdateUpdateChannelButtons(UpdateChannel.Beta);
                    LoadHistoryVersions();
                    LoadUpdateLog(UpdateChannel.Beta);
                    break;
            }
        }

        private void UpdateUpdateChannelButtons(UpdateChannel selectedChannel)
        {
            try
            {
                bool isDarkTheme = ThemeHelper.IsDarkTheme;
                var selectedBrush = isDarkTheme ? new SolidColorBrush(Color.FromRgb(25, 25, 25)) : new SolidColorBrush(Color.FromRgb(225, 225, 225));
                var unselectedBrush = new SolidColorBrush(Colors.Transparent);
                
                if (UpdateChannelReleaseBorder != null)
                {
                    bool isSelected = selectedChannel == UpdateChannel.Release;
                    UpdateChannelReleaseBorder.Background = isSelected ? selectedBrush : unselectedBrush;
                    var textBlock = UpdateChannelReleaseBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
                
                if (UpdateChannelPreviewBorder != null)
                {
                    bool isSelected = selectedChannel == UpdateChannel.Preview;
                    UpdateChannelPreviewBorder.Background = isSelected ? selectedBrush : unselectedBrush;
                    var textBlock = UpdateChannelPreviewBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
                
                if (UpdateChannelBetaBorder != null)
                {
                    bool isSelected = selectedChannel == UpdateChannel.Beta;
                    UpdateChannelBetaBorder.Background = isSelected ? selectedBrush : unselectedBrush;
                    var textBlock = UpdateChannelBetaBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal;
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新更新通道按钮状态时出错: {ex.Message}");
            }
        }

        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            bool currentState = GetCurrentSettingValue(tag);
            bool newState = !currentState;
            SetToggleSwitchState(border, newState);

            switch (tag)
            {
                case "IsAutoUpdate":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdate", newState);
                    AutoUpdateWithSilencePanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    AutoUpdateTimePeriodSeparator.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    if (AutoUpdateTimePeriodBlock != null)
                    {
                        AutoUpdateTimePeriodBlock.Visibility = 
                            (MainWindow.Settings.Startup.IsAutoUpdateWithSilence && MainWindow.Settings.Startup.IsAutoUpdate) ?
                            Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "IsAutoUpdateWithSilence":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdateWithSilence", newState);
                    if (AutoUpdateTimePeriodBlock != null)
                    {
                        AutoUpdateTimePeriodBlock.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
            }
        }

        private bool GetCurrentSettingValue(string tag)
        {
            if (MainWindow.Settings == null) return false;

            try
            {
                switch (tag)
                {
                    case "IsAutoUpdate":
                        return MainWindow.Settings.Startup?.IsAutoUpdate ?? false;
                    case "IsAutoUpdateWithSilence":
                        return MainWindow.Settings.Startup?.IsAutoUpdateWithSilence ?? false;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private Border FindToggleSwitch(string name)
        {
            return this.FindDescendantByName(name) as Border;
        }

        private void SetToggleSwitchState(Border toggleSwitch, bool isOn)
        {
            if (toggleSwitch == null) return;
            toggleSwitch.Background = isOn
                ? new SolidColorBrush(Color.FromRgb(53, 132, 228))
                : (ThemeHelper.IsDarkTheme ? ThemeHelper.GetButtonBackgroundBrush() : new SolidColorBrush(Color.FromRgb(225, 225, 225)));
            var innerBorder = toggleSwitch.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                innerBorder.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void AutoUpdateWithSilenceStartTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("AutoUpdateWithSilenceStartTimeComboBox", AutoUpdateWithSilenceStartTimeComboBox?.SelectedItem);
        }

        private void AutoUpdateWithSilenceEndTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("AutoUpdateWithSilenceEndTimeComboBox", AutoUpdateWithSilenceEndTimeComboBox?.SelectedItem);
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

                if (RollbackButton != null)
                {
                    RollbackButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    RollbackButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }

                if (FixVersionButton != null)
                {
                    FixVersionButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    FixVersionButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }

                SwitchTab(_currentTab);
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

