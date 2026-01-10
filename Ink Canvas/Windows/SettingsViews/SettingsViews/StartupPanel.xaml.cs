using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// StartupPanel.xaml 的交互逻辑
    /// </summary>
    public partial class StartupPanel : UserControl
    {
        private bool _isLoaded = false;

        public StartupPanel()
        {
            InitializeComponent();
            Loaded += StartupPanel_Loaded;
        }

        private void StartupPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            // 添加触摸支持
            EnableTouchSupport();
            // 应用主题
            ApplyTheme();
            _isLoaded = true;
        }

        /// <summary>
        /// 为面板中的所有交互控件启用触摸支持
        /// </summary>
        private void EnableTouchSupport()
        {
            try
            {
                // 延迟执行，确保所有控件都已加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnableTouchSupportForControls(this);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupPanel 启用触摸支持时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 为控件树中的所有交互控件启用触摸支持
        /// </summary>
        private void EnableTouchSupportForControls(System.Windows.DependencyObject parent)
        {
            // 使用 MainWindowSettingsHelper 的通用方法
            MainWindowSettingsHelper.EnableTouchSupportForControls(parent);
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

        /// <summary>
        /// 加载设置到UI
        /// </summary>
        public void LoadSettings()
        {
            if (MainWindow.Settings == null) return;

            _isLoaded = false;

            try
            {
                // 自动更新设置
                var toggleSwitchIsAutoUpdate = FindToggleSwitch("ToggleSwitchIsAutoUpdate");
                if (toggleSwitchIsAutoUpdate != null)
                {
                    bool isAutoUpdate = MainWindow.Settings.Startup.IsAutoUpdate;
                    SetToggleSwitchState(toggleSwitchIsAutoUpdate, isAutoUpdate);
                }

                // 静默更新设置
                var toggleSwitchIsAutoUpdateWithSilence = FindToggleSwitch("ToggleSwitchIsAutoUpdateWithSilence");
                if (toggleSwitchIsAutoUpdateWithSilence != null)
                {
                    bool isAutoUpdateWithSilence = MainWindow.Settings.Startup.IsAutoUpdateWithSilence;
                    SetToggleSwitchState(toggleSwitchIsAutoUpdateWithSilence, isAutoUpdateWithSilence);
                    toggleSwitchIsAutoUpdateWithSilence.Visibility = MainWindow.Settings.Startup.IsAutoUpdate ? Visibility.Visible : Visibility.Collapsed;
                }

                // 静默更新时间段
                if (AutoUpdateTimePeriodBlock != null)
                {
                    AutoUpdateTimePeriodBlock.Visibility = 
                        (MainWindow.Settings.Startup.IsAutoUpdateWithSilence && MainWindow.Settings.Startup.IsAutoUpdate) ?
                        Visibility.Visible : Visibility.Collapsed;
                }

                // 设置时间选择器
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

                // 开机时运行
                var toggleSwitchRunAtStartup = FindToggleSwitch("ToggleSwitchRunAtStartup");
                if (toggleSwitchRunAtStartup != null)
                {
                    // 检查启动项是否存在
                    bool runAtStartup = System.IO.File.Exists(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\Ink Canvas Annotation.lnk");
                    SetToggleSwitchState(toggleSwitchRunAtStartup, runAtStartup);
                }

                // 启动时折叠
                var toggleSwitchFoldAtStartup = FindToggleSwitch("ToggleSwitchFoldAtStartup");
                if (toggleSwitchFoldAtStartup != null)
                {
                    SetToggleSwitchState(toggleSwitchFoldAtStartup, MainWindow.Settings.Startup.IsFoldAtStartup);
                }

                // 窗口无焦点模式
                var toggleSwitchNoFocusMode = FindToggleSwitch("ToggleSwitchNoFocusMode");
                if (toggleSwitchNoFocusMode != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchNoFocusMode, MainWindow.Settings.Advanced.IsNoFocusMode);
                }

                // 窗口无边框模式
                var toggleSwitchWindowMode = FindToggleSwitch("ToggleSwitchWindowMode");
                if (toggleSwitchWindowMode != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchWindowMode, MainWindow.Settings.Advanced.WindowMode);
                }

                // 窗口置顶
                var toggleSwitchAlwaysOnTop = FindToggleSwitch("ToggleSwitchAlwaysOnTop");
                if (toggleSwitchAlwaysOnTop != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchAlwaysOnTop, MainWindow.Settings.Advanced.IsAlwaysOnTop);
                }

                // UIA置顶
                var toggleSwitchUIAccessTopMost = FindToggleSwitch("ToggleSwitchUIAccessTopMost");
                if (toggleSwitchUIAccessTopMost != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchUIAccessTopMost, MainWindow.Settings.Advanced.EnableUIAccessTopMost);
                }

                // 更新通道
                if (MainWindow.Settings.Startup.UpdateChannel == UpdateChannel.Release)
                {
                    UpdateUpdateChannelButtons(true);
                }
                else
                {
                    UpdateUpdateChannelButtons(false);
                }

                // 仅PPT模式
                var toggleSwitchMode = FindToggleSwitch("ToggleSwitchMode");
                if (toggleSwitchMode != null && MainWindow.Settings.ModeSettings != null)
                {
                    SetToggleSwitchState(toggleSwitchMode, MainWindow.Settings.ModeSettings.IsPPTOnlyMode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载启动设置时出错: {ex.Message}");
            }

            _isLoaded = true;
        }

        /// <summary>
        /// 查找ToggleSwitch控件
        /// </summary>
        private Border FindToggleSwitch(string name)
        {
            return this.FindDescendantByName(name) as Border;
        }

        /// <summary>
        /// 设置ToggleSwitch状态
        /// </summary>
        private void SetToggleSwitchState(Border toggleSwitch, bool isOn)
        {
            if (toggleSwitch == null) return;
            toggleSwitch.Background = isOn 
                ? new SolidColorBrush(Color.FromRgb(53, 132, 228)) 
                : ThemeHelper.GetButtonBackgroundBrush();
            var innerBorder = toggleSwitch.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// ToggleSwitch点击事件处理
        /// </summary>
        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = border.Background.ToString() == "#FF3584E4";
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "IsAutoUpdate":
                    // 直接调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdate", newState);
                    // 更新UI状态
                    var toggleSwitchIsAutoUpdateWithSilence = FindToggleSwitch("ToggleSwitchIsAutoUpdateWithSilence");
                    if (toggleSwitchIsAutoUpdateWithSilence != null)
                    {
                        toggleSwitchIsAutoUpdateWithSilence.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (AutoUpdateTimePeriodBlock != null)
                    {
                        AutoUpdateTimePeriodBlock.Visibility = 
                            (MainWindow.Settings.Startup.IsAutoUpdateWithSilence && MainWindow.Settings.Startup.IsAutoUpdate) ?
                            Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "IsAutoUpdateWithSilence":
                    // 直接调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdateWithSilence", newState);
                    // 更新UI状态
                    if (AutoUpdateTimePeriodBlock != null)
                    {
                        AutoUpdateTimePeriodBlock.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "RunAtStartup":
                    // 直接调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchRunAtStartup", newState);
                    break;

                case "FoldAtStartup":
                    // 直接调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFoldAtStartup", newState);
                    break;

                case "NoFocusMode":
                    // 窗口无焦点模式
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Advanced != null)
                        {
                            MainWindow.Settings.Advanced.IsNoFocusMode = newState;
                        }
                    }, "ToggleSwitchNoFocusMode");
                    // 调用 ApplyNoFocusMode 方法
                    MainWindowSettingsHelper.InvokeMainWindowMethod("ApplyNoFocusMode");
                    break;

                case "WindowMode":
                    // 窗口无边框模式
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchWindowMode", newState);
                    break;

                case "AlwaysOnTop":
                    // 窗口置顶
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Advanced != null)
                        {
                            MainWindow.Settings.Advanced.IsAlwaysOnTop = newState;
                        }
                    }, "ToggleSwitchAlwaysOnTop");
                    // 调用 SetAlwaysOnTop 方法（如果存在）
                    MainWindowSettingsHelper.InvokeMainWindowMethod("SetAlwaysOnTop", newState);
                    break;

                case "UIAccessTopMost":
                    // UIA置顶
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Advanced != null)
                        {
                            MainWindow.Settings.Advanced.EnableUIAccessTopMost = newState;
                        }
                    }, "ToggleSwitchUIAccessTopMost");
                    break;

                case "Mode":
                    // 仅PPT模式
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchMode", newState);
                    break;
            }
        }

        /// <summary>
        /// 选项按钮点击事件处理
        /// </summary>
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
                    // 选择稳定版
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Release;
                    }, "UpdateChannelSelector");
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                        new System.Windows.Controls.RadioButton { Tag = "Release" }, e);
                    // 更新UI状态
                    UpdateUpdateChannelButtons(true);
                    break;

                case "UpdateChannel_Beta":
                    // 选择测试版
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Beta;
                    }, "UpdateChannelSelector");
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                        new System.Windows.Controls.RadioButton { Tag = "Beta" }, e);
                    // 更新UI状态
                    UpdateUpdateChannelButtons(false);
                    break;
            }
        }

        /// <summary>
        /// 更新更新通道按钮状态
        /// </summary>
        private void UpdateUpdateChannelButtons(bool isReleaseSelected)
        {
            try
            {
                bool isDarkTheme = ThemeHelper.IsDarkTheme;
                
                if (UpdateChannelReleaseBorder != null)
                {
                    UpdateChannelReleaseBorder.Background = isReleaseSelected
                        ? (isDarkTheme ? ThemeHelper.GetButtonBackgroundBrush() : new SolidColorBrush(Color.FromRgb(225, 225, 225)))
                        : (isDarkTheme ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Colors.Transparent));
                    var textBlock = UpdateChannelReleaseBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontWeight = isReleaseSelected ? FontWeights.Bold : FontWeights.Normal;
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
                
                if (UpdateChannelBetaBorder != null)
                {
                    UpdateChannelBetaBorder.Background = !isReleaseSelected
                        ? (isDarkTheme ? ThemeHelper.GetButtonBackgroundBrush() : new SolidColorBrush(Color.FromRgb(225, 225, 225)))
                        : (isDarkTheme ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) : new SolidColorBrush(Colors.Transparent));
                    var textBlock = UpdateChannelBetaBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.FontWeight = !isReleaseSelected ? FontWeights.Bold : FontWeights.Normal;
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新更新通道按钮状态时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 手动更新按钮点击事件
        /// </summary>
        private async void ManualUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("ManualUpdateButton_Click", sender, e);
        }

        /// <summary>
        /// 版本修复按钮点击事件
        /// </summary>
        private async void FixVersionButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("FixVersionButton_Click", sender, e);
        }

        /// <summary>
        /// 历史版本回滚按钮点击事件
        /// </summary>
        private void HistoryRollbackButton_Click(object sender, RoutedEventArgs e)
        {
            // 查找 MainWindow 中的历史版本回滚方法
            MainWindowSettingsHelper.InvokeMainWindowMethod("HistoryRollbackButton_Click", sender, e);
        }

        /// <summary>
        /// ComboBox选择变化事件处理
        /// </summary>
        private void AutoUpdateWithSilenceStartTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            // 直接调用 MainWindow 中的方法
            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("AutoUpdateWithSilenceStartTimeComboBox", AutoUpdateWithSilenceStartTimeComboBox?.SelectedItem);
        }

        private void AutoUpdateWithSilenceEndTimeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            // 直接调用 MainWindow 中的方法
            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("AutoUpdateWithSilenceEndTimeComboBox", AutoUpdateWithSilenceEndTimeComboBox?.SelectedItem);
        }
        
        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme()
        {
            try
            {
                bool isDarkTheme = ThemeHelper.IsDarkTheme;

                // 更新更新通道按钮
                if (UpdateChannelReleaseBorder != null)
                {
                    UpdateChannelReleaseBorder.Background = isDarkTheme 
                        ? ThemeHelper.GetButtonBackgroundBrush() 
                        : new SolidColorBrush(Color.FromRgb(225, 225, 225));
                    var textBlock = UpdateChannelReleaseBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }
                if (UpdateChannelBetaBorder != null)
                {
                    UpdateChannelBetaBorder.Background = isDarkTheme 
                        ? new SolidColorBrush(Color.FromRgb(35, 35, 35)) 
                        : new SolidColorBrush(Colors.Transparent);
                    var textBlock = UpdateChannelBetaBorder.Child as TextBlock;
                    if (textBlock != null)
                    {
                        textBlock.Foreground = ThemeHelper.GetTextPrimaryBrush();
                    }
                }

                // 更新按钮
                if (ManualUpdateButton != null)
                {
                    ManualUpdateButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    ManualUpdateButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (FixVersionButton != null)
                {
                    FixVersionButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    FixVersionButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (HistoryRollbackButton != null)
                {
                    HistoryRollbackButton.Background = ThemeHelper.GetButtonBackgroundBrush();
                    HistoryRollbackButton.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }

                // 使用 ThemeHelper 递归更新其他元素
                ThemeHelper.ApplyThemeToControl(this);
                
                // 为所有 ComboBox 添加 DropDownOpened 事件处理，以便在下拉菜单打开时更新颜色
                UpdateComboBoxDropdownTheme(AutoUpdateWithSilenceStartTimeComboBox);
                UpdateComboBoxDropdownTheme(AutoUpdateWithSilenceEndTimeComboBox);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupPanel 应用主题时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 为 ComboBox 添加下拉菜单主题更新
        /// </summary>
        private void UpdateComboBoxDropdownTheme(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox == null) return;
            
            // 移除旧的事件处理（如果存在）
            comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            // 添加新的事件处理
            comboBox.DropDownOpened += ComboBox_DropDownOpened;
        }

        /// <summary>
        /// ComboBox 下拉菜单打开事件处理
        /// </summary>
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                // 延迟更新，确保 Popup 已经完全创建
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ThemeHelper.UpdateComboBoxDropdownColors(comboBox);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}

