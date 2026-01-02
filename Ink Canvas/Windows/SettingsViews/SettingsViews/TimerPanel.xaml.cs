using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class TimerPanel : UserControl
    {
        private bool _isLoaded = false;

        public TimerPanel()
        {
            InitializeComponent();
            Loaded += TimerPanel_Loaded;
        }

        private void TimerPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            MainWindowSettingsHelper.EnableTouchSupportForControls(this);
            ApplyTheme();
            _isLoaded = true;
        }
        public void LoadSettings()
        {
            if (MainWindow.Settings == null || MainWindow.Settings.RandSettings == null) return;

            _isLoaded = false;

            try
            {
                var randSettings = MainWindow.Settings.RandSettings;
                if (TimerVolumeSlider != null)
                {
                    TimerVolumeSlider.Value = randSettings.TimerVolume;
                    if (TimerVolumeText != null)
                    {
                        TimerVolumeText.Text = (randSettings.TimerVolume * 100).ToString("F0") + "%";
                    }
                }
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchUseLegacyTimerUI"), randSettings.UseLegacyTimerUI);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchUseNewStyleUI"), randSettings.UseNewStyleUI);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableOvertimeCountUp"), randSettings.EnableOvertimeCountUp);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableOvertimeRedText"), randSettings.EnableOvertimeRedText);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableProgressiveReminder"), randSettings.EnableProgressiveReminder);
                var progressiveReminderVolumePanel = this.FindDescendantByName("ProgressiveReminderVolumePanel") as Grid;
                if (progressiveReminderVolumePanel != null)
                {
                    progressiveReminderVolumePanel.Visibility = randSettings.EnableProgressiveReminder 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
                if (ProgressiveReminderVolumeSlider != null)
                {
                    ProgressiveReminderVolumeSlider.Value = randSettings.ProgressiveReminderVolume;
                    if (ProgressiveReminderVolumeText != null)
                    {
                        ProgressiveReminderVolumeText.Text = (randSettings.ProgressiveReminderVolume * 100).ToString("F0") + "%";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载定时器设置时出错: {ex.Message}");
            }

            _isLoaded = true;
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimerPanel 应用主题时出�? {ex.Message}");
            }
        }
        private void TimerVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (TimerVolumeSlider != null && TimerVolumeText != null)
            {
                double val = TimerVolumeSlider.Value;
                TimerVolumeText.Text = (val * 100).ToString("F0") + "%";
                MainWindowSettingsHelper.InvokeSliderValueChanged("TimerVolumeSlider", val);
            }
        }

        private void ProgressiveReminderVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (ProgressiveReminderVolumeSlider != null && ProgressiveReminderVolumeText != null)
            {
                double val = ProgressiveReminderVolumeSlider.Value;
                ProgressiveReminderVolumeText.Text = (val * 100).ToString("F0") + "%";
                MainWindowSettingsHelper.InvokeSliderValueChanged("ProgressiveReminderVolumeSlider", val);
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
            e.Handled = true;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = ThemeHelper.IsToggleSwitchOn(border.Background);
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "UseLegacyTimerUI":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchUseLegacyTimerUI", newState);
                    break;

                case "UseNewStyleUI":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchUseNewStyleUI", newState);
                    break;

                case "EnableOvertimeCountUp":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableOvertimeCountUp", newState);
                    break;

                case "EnableOvertimeRedText":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableOvertimeRedText", newState);
                    break;

                case "EnableProgressiveReminder":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableProgressiveReminder", newState);
                    if (ProgressiveReminderVolumePanel != null)
                    {
                        ProgressiveReminderVolumePanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (ProgressiveReminderSoundPanel != null)
                    {
                        ProgressiveReminderSoundPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
            }
        }
    }
}

