using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// AppearancePanel.xaml 的交互逻辑
    /// </summary>
    public partial class AppearancePanel : UserControl
    {
        private bool _isLoaded = false;

        public AppearancePanel()
        {
            InitializeComponent();
            Loaded += AppearancePanel_Loaded;
        }

        private void AppearancePanel_Loaded(object sender, RoutedEventArgs e)
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
            if (MainWindow.Settings == null || MainWindow.Settings.Appearance == null) return;

            _isLoaded = false;

            try
            {
                var appearance = MainWindow.Settings.Appearance;

                // 浮动工具栏缩放
                if (ViewboxFloatingBarScaleTransformValueSlider != null)
                {
                    double val = appearance.ViewboxFloatingBarScaleTransformValue;
                    if (val == 0) val = 1.0;
                    ViewboxFloatingBarScaleTransformValueSlider.Value = val;
                    if (ViewboxFloatingBarScaleTransformValueText != null)
                    {
                        ViewboxFloatingBarScaleTransformValueText.Text = val.ToString("F2");
                    }
                }

                // 浮动工具栏透明度
                if (ViewboxFloatingBarOpacityValueSlider != null)
                {
                    ViewboxFloatingBarOpacityValueSlider.Value = appearance.ViewboxFloatingBarOpacityValue;
                    if (ViewboxFloatingBarOpacityValueText != null)
                    {
                        ViewboxFloatingBarOpacityValueText.Text = appearance.ViewboxFloatingBarOpacityValue.ToString("F2");
                    }
                }

                // 浮栏在PPT下透明度
                if (ViewboxFloatingBarOpacityInPPTValueSlider != null)
                {
                    ViewboxFloatingBarOpacityInPPTValueSlider.Value = appearance.ViewboxFloatingBarOpacityInPPTValue;
                    if (ViewboxFloatingBarOpacityInPPTValueText != null)
                    {
                        ViewboxFloatingBarOpacityInPPTValueText.Text = appearance.ViewboxFloatingBarOpacityInPPTValue.ToString("F2");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载外观设置时出错: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"AppearancePanel 应用主题时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// Slider值变化事件处理
        /// </summary>
        private void ViewboxFloatingBarScaleTransformValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (ViewboxFloatingBarScaleTransformValueSlider != null && ViewboxFloatingBarScaleTransformValueText != null)
            {
                double val = ViewboxFloatingBarScaleTransformValueSlider.Value;
                ViewboxFloatingBarScaleTransformValueText.Text = val.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("ViewboxFloatingBarScaleTransformValueSlider", val);
            }
        }

        private void ViewboxFloatingBarOpacityValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (ViewboxFloatingBarOpacityValueSlider != null && ViewboxFloatingBarOpacityValueText != null)
            {
                double val = ViewboxFloatingBarOpacityValueSlider.Value;
                ViewboxFloatingBarOpacityValueText.Text = val.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("ViewboxFloatingBarOpacityValueSlider", val);
            }
        }

        private void ViewboxFloatingBarOpacityInPPTValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (ViewboxFloatingBarOpacityInPPTValueSlider != null && ViewboxFloatingBarOpacityInPPTValueText != null)
            {
                double val = ViewboxFloatingBarOpacityInPPTValueSlider.Value;
                ViewboxFloatingBarOpacityInPPTValueText.Text = val.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("ViewboxFloatingBarOpacityInPPTValueSlider", val);
            }
        }
    }
}
