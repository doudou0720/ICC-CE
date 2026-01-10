using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
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
            EnableTouchSupport();
            ApplyTheme();
            _isLoaded = true;
        }
        private void EnableTouchSupport()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnableTouchSupportForControls(this);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupPanel 启用触摸支持时出�? {ex.Message}");
            }
        }
        private void EnableTouchSupportForControls(System.Windows.DependencyObject parent)
        {
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
        public void LoadSettings()
        {
            if (MainWindow.Settings == null) return;

            _isLoaded = false;

            try
            {
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
                    toggleSwitchIsAutoUpdateWithSilence.Visibility = MainWindow.Settings.Startup.IsAutoUpdate ? Visibility.Visible : Visibility.Collapsed;
                }
                {
                    AutoUpdateTimePeriodBlock.Visibility = 
                        (MainWindow.Settings.Startup.IsAutoUpdateWithSilence && MainWindow.Settings.Startup.IsAutoUpdate) ?
                        Visibility.Visible : Visibility.Collapsed;
                }
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
                var toggleSwitchRunAtStartup = FindToggleSwitch("ToggleSwitchRunAtStartup");
                if (toggleSwitchRunAtStartup != null)
                {
                    bool runAtStartup = System.IO.File.Exists(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup) + "\\Ink Canvas Annotation.lnk");
                    SetToggleSwitchState(toggleSwitchRunAtStartup, runAtStartup);
                }
                var toggleSwitchFoldAtStartup = FindToggleSwitch("ToggleSwitchFoldAtStartup");
                if (toggleSwitchFoldAtStartup != null)
                {
                    SetToggleSwitchState(toggleSwitchFoldAtStartup, MainWindow.Settings.Startup.IsFoldAtStartup);
                }
                var toggleSwitchNoFocusMode = FindToggleSwitch("ToggleSwitchNoFocusMode");
                if (toggleSwitchNoFocusMode != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchNoFocusMode, MainWindow.Settings.Advanced.IsNoFocusMode);
                }
                var toggleSwitchWindowMode = FindToggleSwitch("ToggleSwitchWindowMode");
                if (toggleSwitchWindowMode != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchWindowMode, MainWindow.Settings.Advanced.WindowMode);
                }
                var toggleSwitchAlwaysOnTop = FindToggleSwitch("ToggleSwitchAlwaysOnTop");
                if (toggleSwitchAlwaysOnTop != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchAlwaysOnTop, MainWindow.Settings.Advanced.IsAlwaysOnTop);
                }
                var toggleSwitchUIAccessTopMost = FindToggleSwitch("ToggleSwitchUIAccessTopMost");
                if (toggleSwitchUIAccessTopMost != null && MainWindow.Settings.Advanced != null)
                {
                    SetToggleSwitchState(toggleSwitchUIAccessTopMost, MainWindow.Settings.Advanced.EnableUIAccessTopMost);
                }
                if (MainWindow.Settings.Startup.UpdateChannel == UpdateChannel.Release)
                {
                    UpdateUpdateChannelButtons(true);
                }
                else
                {
                    UpdateUpdateChannelButtons(false);
                }
                var toggleSwitchMode = FindToggleSwitch("ToggleSwitchMode");
                if (toggleSwitchMode != null && MainWindow.Settings.ModeSettings != null)
                {
                    SetToggleSwitchState(toggleSwitchMode, MainWindow.Settings.ModeSettings.IsPPTOnlyMode);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载启动设置时出�? {ex.Message}");
            }

            _isLoaded = true;
        }
        private Border FindToggleSwitch(string name)
        {
            return this.FindDescendantByName(name) as Border;
        }
        private void SetToggleSwitchState(Border toggleSwitch, bool isOn)
        {
            if (toggleSwitch == null) return;
            toggleSwitch.Background = isOn 
                ? ThemeHelper.GetToggleSwitchOnBackgroundBrush() 
                : ThemeHelper.GetToggleSwitchOffBackgroundBrush();
            var innerBorder = toggleSwitch.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }
        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = ThemeHelper.IsToggleSwitchOn(border.Background);
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "IsAutoUpdate":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdate", newState);
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
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoUpdateWithSilence", newState);
                    {
                        AutoUpdateTimePeriodBlock.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "RunAtStartup":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchRunAtStartup", newState);
                    break;

                case "FoldAtStartup":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFoldAtStartup", newState);
                    break;

                case "NoFocusMode":
                    if (MainWindow.Settings.Advanced != null)
                    {
                        MainWindow.Settings.Advanced.IsNoFocusMode = newState;
                    }
                    MainWindowSettingsHelper.InvokeMainWindowMethod("ApplyNoFocusMode");
                    break;

                case "WindowMode":
                    break;

                case "AlwaysOnTop":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Advanced != null)
                        {
                            MainWindow.Settings.Advanced.IsAlwaysOnTop = newState;
                        }
                    }, "ToggleSwitchAlwaysOnTop");
                    MainWindowSettingsHelper.InvokeMainWindowMethod("SetAlwaysOnTop", newState);
                    break;

                case "UIAccessTopMost":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Advanced != null)
                        {
                            MainWindow.Settings.Advanced.EnableUIAccessTopMost = newState;
                        }
                    }, "ToggleSwitchUIAccessTopMost");
                    break;

                case "Mode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchMode", newState);
                    break;
            }
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
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Release;
                        MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                            new System.Windows.Controls.RadioButton { Tag = "Release" }, e);
                        UpdateUpdateChannelButtons(true);
                    }
                    break;

                case "UpdateChannel_Beta":
                    {
                        MainWindow.Settings.Startup.UpdateChannel = UpdateChannel.Beta;
                        MainWindowSettingsHelper.InvokeMainWindowMethod("UpdateChannelSelector_Checked", 
                            new System.Windows.Controls.RadioButton { Tag = "Beta" }, e);
                        UpdateUpdateChannelButtons(false);
                    }
                    break;
            }
        }
        private void UpdateUpdateChannelButtons(bool isReleaseSelected)
        {
            try
            {
                if (UpdateChannelReleaseBorder != null)
                {
                    ThemeHelper.SetOptionButtonSelectedState(UpdateChannelReleaseBorder, isReleaseSelected);
                }
                
                if (UpdateChannelBetaBorder != null)
                {
                    ThemeHelper.SetOptionButtonSelectedState(UpdateChannelBetaBorder, !isReleaseSelected);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新更新通道按钮状态时出错: {ex.Message}");
            }
        }
        private async void ManualUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("ManualUpdateButton_Click", sender, e);
        }
        private async void FixVersionButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("FixVersionButton_Click", sender, e);
        }
        private void HistoryRollbackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("HistoryRollbackButton_Click", sender, e);
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
        public void ApplyTheme()
        {
            try
            {
                bool isDarkTheme = ThemeHelper.IsDarkTheme;
                // 更新更新通道按钮状态
                if (MainWindow.Settings.Startup.UpdateChannel == UpdateChannel.Release)
                {
                    UpdateUpdateChannelButtons(true);
                }
                else
                {
                    UpdateUpdateChannelButtons(false);
                }
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
                ThemeHelper.ApplyThemeToControl(this);
                UpdateComboBoxDropdownTheme(AutoUpdateWithSilenceStartTimeComboBox);
                UpdateComboBoxDropdownTheme(AutoUpdateWithSilenceEndTimeComboBox);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StartupPanel 应用主题时出�? {ex.Message}");
            }
        }
        private void UpdateComboBoxDropdownTheme(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox == null) return;
            comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            comboBox.DropDownOpened += ComboBox_DropDownOpened;
        }
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ThemeHelper.UpdateComboBoxDropdownColors(comboBox);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}

