using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
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
            MainWindowSettingsHelper.EnableTouchSupportForControls(this);
            ApplyTheme();
            _isLoaded = true;
        }
        public void LoadSettings()
        {
            if (MainWindow.Settings == null || MainWindow.Settings.Canvas == null || MainWindow.Settings.InkToShape == null) return;

            _isLoaded = false;

            try
            {
                var canvas = MainWindow.Settings.Canvas;
                var inkToShape = MainWindow.Settings.InkToShape;
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableInkToShape"), inkToShape.IsInkToShapeEnabled);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableInkToShapeNoFakePressureRectangle"), inkToShape.IsInkToShapeNoFakePressureRectangle);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableInkToShapeNoFakePressureTriangle"), inkToShape.IsInkToShapeNoFakePressureTriangle);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoStraightenLine"), canvas.AutoStraightenLine);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchHighPrecisionLineStraighten"), canvas.HighPrecisionLineStraighten);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchLineEndpointSnapping"), canvas.LineEndpointSnapping);
                if (AutoStraightenLineThresholdSlider != null)
                {
                    AutoStraightenLineThresholdSlider.Value = canvas.AutoStraightenLineThreshold;
                    if (AutoStraightenLineThresholdText != null)
                    {
                        AutoStraightenLineThresholdText.Text = ((int)canvas.AutoStraightenLineThreshold).ToString();
                    }
                }
                if (LineStraightenSensitivitySlider != null)
                {
                    LineStraightenSensitivitySlider.Value = inkToShape.LineStraightenSensitivity;
                    if (LineStraightenSensitivityText != null)
                    {
                        LineStraightenSensitivityText.Text = inkToShape.LineStraightenSensitivity.ToString("F2");
                    }
                }
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
                System.Diagnostics.Debug.WriteLine($"加载墨迹识别设置时出�? {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"InkRecognitionPanel 应用主题时出�? {ex.Message}");
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
                case "EnableInkToShape":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableInkToShape", newState);
                    break;

                case "EnableInkToShapeNoFakePressureRectangle":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableInkToShapeNoFakePressureRectangle", newState);
                    break;

                case "EnableInkToShapeNoFakePressureTriangle":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableInkToShapeNoFakePressureTriangle", newState);
                    break;

                case "AutoStraightenLine":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoStraightenLine", newState);
                    break;

                case "HighPrecisionLineStraighten":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchHighPrecisionLineStraighten", newState);
                    break;

                case "LineEndpointSnapping":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchLineEndpointSnapping", newState);
                    break;
            }
        }
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

