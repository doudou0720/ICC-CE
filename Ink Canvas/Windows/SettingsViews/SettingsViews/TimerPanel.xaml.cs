using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// TimerPanel.xaml 的交互逻辑
    /// </summary>
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
            // 添加触摸支持
            MainWindowSettingsHelper.EnableTouchSupportForControls(this);
            // 应用主题
            ApplyTheme();
            _isLoaded = true;
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        public void LoadSettings()
        {
            if (MainWindow.Settings == null || MainWindow.Settings.RandSettings == null) return;

            _isLoaded = false;

            try
            {
                var randSettings = MainWindow.Settings.RandSettings;

                // 定时器音量
                if (TimerVolumeSlider != null)
                {
                    TimerVolumeSlider.Value = randSettings.TimerVolume;
                    if (TimerVolumeText != null)
                    {
                        TimerVolumeText.Text = (randSettings.TimerVolume * 100).ToString("F0") + "%";
                    }
                }

                // 渐进提醒
                var toggleSwitchEnableProgressiveReminder = this.FindDescendantByName("ToggleSwitchEnableProgressiveReminder") as Border;
                if (toggleSwitchEnableProgressiveReminder != null)
                {
                    bool isOn = randSettings.EnableProgressiveReminder;
                    toggleSwitchEnableProgressiveReminder.Background = isOn 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 132, 228)) 
                        : ThemeHelper.GetButtonBackgroundBrush();
                    var innerBorder = toggleSwitchEnableProgressiveReminder.Child as Border;
                    if (innerBorder != null)
                    {
                        innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    }
                }

                // 渐进提醒音量面板可见性
                var progressiveReminderVolumePanel = this.FindDescendantByName("ProgressiveReminderVolumePanel") as Grid;
                if (progressiveReminderVolumePanel != null)
                {
                    progressiveReminderVolumePanel.Visibility = randSettings.EnableProgressiveReminder 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }

                // 渐进提醒音量
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
        
        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TimerPanel 应用主题时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// Slider值变化事件处理
        /// </summary>
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
    }
}

