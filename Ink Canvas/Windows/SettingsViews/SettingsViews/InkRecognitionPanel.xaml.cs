using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// InkRecognitionPanel.xaml 的交互逻辑
    /// </summary>
    public partial class InkRecognitionPanel : UserControl
    {
        private bool _isLoaded = false;

        public InkRecognitionPanel()
        {
            InitializeComponent();
            Loaded += InkRecognitionPanel_Loaded;
        }

        private void InkRecognitionPanel_Loaded(object sender, RoutedEventArgs e)
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
            if (MainWindow.Settings == null || MainWindow.Settings.Canvas == null || MainWindow.Settings.InkToShape == null) return;

            _isLoaded = false;

            try
            {
                var canvas = MainWindow.Settings.Canvas;
                var inkToShape = MainWindow.Settings.InkToShape;

                // 自动拉直线阈值
                if (AutoStraightenLineThresholdSlider != null)
                {
                    AutoStraightenLineThresholdSlider.Value = canvas.AutoStraightenLineThreshold;
                    if (AutoStraightenLineThresholdText != null)
                    {
                        AutoStraightenLineThresholdText.Text = ((int)canvas.AutoStraightenLineThreshold).ToString();
                    }
                }

                // 灵敏度
                if (LineStraightenSensitivitySlider != null)
                {
                    LineStraightenSensitivitySlider.Value = inkToShape.LineStraightenSensitivity;
                    if (LineStraightenSensitivityText != null)
                    {
                        LineStraightenSensitivityText.Text = inkToShape.LineStraightenSensitivity.ToString("F2");
                    }
                }

                // 高精度直线拉直
                var toggleSwitchHighPrecisionLineStraighten = this.FindDescendantByName("ToggleSwitchHighPrecisionLineStraighten") as Border;
                if (toggleSwitchHighPrecisionLineStraighten != null)
                {
                    bool isOn = canvas.HighPrecisionLineStraighten;
                    toggleSwitchHighPrecisionLineStraighten.Background = isOn 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 132, 228)) 
                        : ThemeHelper.GetButtonBackgroundBrush();
                    var innerBorder = toggleSwitchHighPrecisionLineStraighten.Child as Border;
                    if (innerBorder != null)
                    {
                        innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    }
                }

                // 直线端点吸附
                var toggleSwitchLineEndpointSnapping = this.FindDescendantByName("ToggleSwitchLineEndpointSnapping") as Border;
                if (toggleSwitchLineEndpointSnapping != null)
                {
                    bool isOn = canvas.LineEndpointSnapping;
                    toggleSwitchLineEndpointSnapping.Background = isOn 
                        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 132, 228)) 
                        : ThemeHelper.GetButtonBackgroundBrush();
                    var innerBorder = toggleSwitchLineEndpointSnapping.Child as Border;
                    if (innerBorder != null)
                    {
                        innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                    }
                }

                // 吸附距离
                if (LineEndpointSnappingThresholdSlider != null)
                {
                    LineEndpointSnappingThresholdSlider.Value = canvas.LineEndpointSnappingThreshold;
                    if (LineEndpointSnappingThresholdText != null)
                    {
                        LineEndpointSnappingThresholdText.Text = ((int)canvas.LineEndpointSnappingThreshold).ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载墨迹识别设置时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"InkRecognitionPanel 应用主题时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// Slider值变化事件处理
        /// </summary>
        private void AutoStraightenLineThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (AutoStraightenLineThresholdSlider != null && AutoStraightenLineThresholdText != null)
            {
                double val = AutoStraightenLineThresholdSlider.Value;
                AutoStraightenLineThresholdText.Text = ((int)val).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("AutoStraightenLineThresholdSlider", val);
            }
        }

        private void LineStraightenSensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (LineStraightenSensitivitySlider != null && LineStraightenSensitivityText != null)
            {
                double val = LineStraightenSensitivitySlider.Value;
                LineStraightenSensitivityText.Text = val.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("LineStraightenSensitivitySlider", val);
            }
        }

        private void LineEndpointSnappingThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (LineEndpointSnappingThresholdSlider != null && LineEndpointSnappingThresholdText != null)
            {
                double val = LineEndpointSnappingThresholdSlider.Value;
                LineEndpointSnappingThresholdText.Text = ((int)val).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("LineEndpointSnappingThresholdSlider", val);
            }
        }
    }
}

