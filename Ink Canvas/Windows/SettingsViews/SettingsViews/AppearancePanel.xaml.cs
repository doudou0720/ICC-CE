using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
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
            MainWindowSettingsHelper.EnableTouchSupportForControls(this);
            ApplyTheme();
            _isLoaded = true;
        }
        public void LoadSettings()
        {
            if (MainWindow.Settings == null || MainWindow.Settings.Appearance == null) return;

            _isLoaded = false;

            try
            {
                var appearance = MainWindow.Settings.Appearance;
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
                if (ViewboxFloatingBarOpacityValueSlider != null)
                {
                    ViewboxFloatingBarOpacityValueSlider.Value = appearance.ViewboxFloatingBarOpacityValue;
                    if (ViewboxFloatingBarOpacityValueText != null)
                    {
                        ViewboxFloatingBarOpacityValueText.Text = appearance.ViewboxFloatingBarOpacityValue.ToString("F2");
                    }
                }
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
                System.Diagnostics.Debug.WriteLine($"�����������ʱ����: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"AppearancePanel Ӧ������ʱ����: {ex.Message}");
            }
        }
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
                case "EnableSplashScreen":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggledWithThemeCheck("ToggleSwitchEnableSplashScreen", newState);
                    if (SplashScreenStylePanel != null)
                    {
                        SplashScreenStylePanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "EnableDisPlayNibModeToggle":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableDisPlayNibModeToggle", newState);
                    break;

                case "EnableTrayIcon":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggledWithThemeCheck("ToggleSwitchEnableTrayIcon", newState);
                    break;

                case "EnableViewboxBlackBoardScaleTransform":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableViewboxBlackBoardScaleTransform", newState);
                    break;

                case "EnableTimeDisplayInWhiteboardMode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableTimeDisplayInWhiteboardMode", newState);
                    break;

                case "EnableChickenSoupInWhiteboardMode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableChickenSoupInWhiteboardMode", newState);
                    break;

                case "EnableQuickPanel":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableQuickPanel", newState);
                    break;

                case "AutoEnterAnnotationModeWhenExitFoldMode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoEnterAnnotationModeWhenExitFoldMode", newState);
                    break;

                case "AutoFoldAfterPPTSlideShow":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoFoldAfterPPTSlideShow", newState);
                    break;

                case "AutoFoldWhenExitWhiteboard":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoFoldWhenExitWhiteboard", newState);
                    break;
            }
        }
    }
}
