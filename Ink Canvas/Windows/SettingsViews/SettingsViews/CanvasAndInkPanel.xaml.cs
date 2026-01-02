using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class CanvasAndInkPanel : UserControl
    {
        private bool _isLoaded = false;

        public CanvasAndInkPanel()
        {
            InitializeComponent();
            Loaded += CanvasAndInkPanel_Loaded;
        }

        private void CanvasAndInkPanel_Loaded(object sender, RoutedEventArgs e)
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
                    MainWindowSettingsHelper.EnableTouchSupportForControls(this);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CanvasAndInkPanel 启用触摸支持时出�? {ex.Message}");
            }
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
            if (MainWindow.Settings == null || MainWindow.Settings.Canvas == null) return;

            _isLoaded = false;

            try
            {
                var canvas = MainWindow.Settings.Canvas;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowCursor"), canvas.IsShowCursor);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePressureTouchMode"), canvas.EnablePressureTouchMode);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchDisablePressure"), canvas.DisablePressure);

                SetOptionButtonState("EraserSize", canvas.EraserSize);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchHideStrokeWhenSelecting"), canvas.HideStrokeWhenSelecting);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchClearCanvasAndClearTimeMachine"), canvas.ClearCanvasAndClearTimeMachine);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchClearCanvasAlsoClearImages"), canvas.ClearCanvasAlsoClearImages);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchCompressPicturesUploaded"), canvas.IsCompressPicturesUploaded);

                SetOptionButtonState("HyperbolaAsymptote", (int)canvas.HyperbolaAsymptoteOption);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowCircleCenter"), canvas.ShowCircleCenter);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchFitToCurve"), canvas.FitToCurve && !canvas.UseAdvancedBezierSmoothing);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAdvancedBezierSmoothing"), canvas.UseAdvancedBezierSmoothing);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableInkFade"), canvas.EnableInkFade);
                if (InkFadeTimePanel != null)
                {
                    InkFadeTimePanel.Visibility = canvas.EnableInkFade ? Visibility.Visible : Visibility.Collapsed;
                }
                if (InkFadeTimeSlider != null)
                {
                    InkFadeTimeSlider.Value = canvas.InkFadeTime;
                    if (InkFadeTimeText != null)
                    {
                        InkFadeTimeText.Text = $"{canvas.InkFadeTime}ms";
                    }
                }

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchUseAsyncInkSmoothing"), canvas.UseAsyncInkSmoothing);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchUseHardwareAcceleration"), canvas.UseHardwareAcceleration);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoStraightenLine"), canvas.AutoStraightenLine);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchHighPrecisionLineStraighten"), canvas.HighPrecisionLineStraighten);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchLineEndpointSnapping"), canvas.LineEndpointSnapping);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载画板和墨迹设置时出错: {ex.Message}");
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

        private void SetOptionButtonState(string group, int selectedIndex)
        {
            var buttons = new[] { "VerySmall", "Small", "Medium", "Large", "VeryLarge" };
            var hyperbolaButtons = new[] { "Yes", "No", "Ask" };
            
            string[] buttonNames = group == "EraserSize" ? buttons : hyperbolaButtons;
            
            for (int i = 0; i < buttonNames.Length; i++)
            {
                var button = this.FindDescendantByName($"{group}{buttonNames[i]}") as Border;
                if (button != null)
                {
                    ThemeHelper.SetOptionButtonSelectedState(button, i == selectedIndex);
                }
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

            var canvas = MainWindow.Settings.Canvas;
            if (canvas == null) return;

            switch (tag)
            {
                case "ShowCursor":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowCursor", newState);
                    break;

                case "EnablePressureTouchMode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePressureTouchMode", newState);
                    if (newState && canvas.DisablePressure)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchDisablePressure", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchDisablePressure"), false);
                    }
                    break;

                case "DisablePressure":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchDisablePressure", newState);
                    if (newState && canvas.EnablePressureTouchMode)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePressureTouchMode", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePressureTouchMode"), false);
                    }
                    break;

                case "HideStrokeWhenSelecting":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchHideStrokeWhenSelecting", newState);
                    break;

                case "ClearCanvasAndClearTimeMachine":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchClearCanvasAndClearTimeMachine", newState);
                    break;

                case "ClearCanvasAlsoClearImages":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchClearCanvasAlsoClearImages", newState);
                    break;

                case "CompressPicturesUploaded":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchCompressPicturesUploaded", newState);
                    break;

                case "UseAsyncInkSmoothing":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAsyncInkSmoothing", newState);
                    break;

                case "UseHardwareAcceleration":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchHardwareAcceleration", newState);
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

                case "ShowCircleCenter":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowCircleCenter", newState);
                    break;

                case "FitToCurve":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFitToCurve", newState);
                    if (newState)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAdvancedBezierSmoothing", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAdvancedBezierSmoothing"), false);
                    }
                    break;

                case "AdvancedBezierSmoothing":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAdvancedBezierSmoothing", newState);
                    if (newState)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFitToCurve", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchFitToCurve"), false);
                    }
                    break;

                case "EnableInkFade":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableInkFade", newState);
                    if (InkFadeTimePanel != null)
                    {
                        InkFadeTimePanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
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

            string[] parts = tag.Split('_');
            if (parts.Length < 2) return;

            string group = parts[0];
            string value = parts[1];

            var parent = border.Parent as Panel;
            if (parent != null)
            {
                foreach (var child in parent.Children)
                {
                    if (child is Border childBorder && childBorder != border)
                    {
                        string childTag = childBorder.Tag?.ToString();
                        if (!string.IsNullOrEmpty(childTag) && childTag.StartsWith(group + "_"))
                        {
                            ThemeHelper.SetOptionButtonSelectedState(childBorder, false);
                        }
                    }
                }
            }

            ThemeHelper.SetOptionButtonSelectedState(border, true);

            var canvas = MainWindow.Settings.Canvas;
            if (canvas == null) return;

            switch (group)
            {
                case "EraserSize":
                    int eraserSize;
                    switch (value)
                    {
                        case "VerySmall":
                            eraserSize = 0;
                            break;
                        case "Small":
                            eraserSize = 1;
                            break;
                        case "Medium":
                            eraserSize = 2;
                            break;
                        case "Large":
                            eraserSize = 3;
                            break;
                        case "VeryLarge":
                            eraserSize = 4;
                            break;
                        default:
                            eraserSize = 2;
                            break;
                    }
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var comboBox = mainWindow.FindName("ComboBoxEraserSize") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > eraserSize)
                        {
                            comboBox.SelectedIndex = eraserSize;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxEraserSize", comboBox.Items[eraserSize]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                                canvas.EraserSize = eraserSize;
                            }, "ComboBoxEraserSize");
                        }
                    }
                    break;

                case "HyperbolaAsymptote":
                    OptionalOperation option;
                    switch (value)
                    {
                        case "Yes":
                            option = OptionalOperation.Yes;
                            break;
                        case "No":
                            option = OptionalOperation.No;
                            break;
                        case "Ask":
                            option = OptionalOperation.Ask;
                            break;
                        default:
                            option = OptionalOperation.Ask;
                            break;
                    }
                    var mainWindow2 = Application.Current.MainWindow as MainWindow;
                    if (mainWindow2 != null)
                    {
                        var comboBox = mainWindow2.FindName("ComboBoxHyperbolaAsymptoteOption") as System.Windows.Controls.ComboBox;
                        if (comboBox != null)
                        {
                            int optionIndex = (int)option;
                            if (comboBox.Items.Count > optionIndex)
                            {
                                comboBox.SelectedIndex = optionIndex;
                                MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxHyperbolaAsymptoteOption", comboBox.Items[optionIndex]);
                            }
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                                canvas.HyperbolaAsymptoteOption = option;
                            }, "ComboBoxHyperbolaAsymptoteOption");
                        }
                    }
                    break;
            }
        }

        private void InkFadeTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (InkFadeTimeSlider != null && InkFadeTimeText != null)
            {
                int value = (int)InkFadeTimeSlider.Value;
                InkFadeTimeText.Text = $"{value}ms";
                MainWindowSettingsHelper.InvokeSliderValueChanged("InkFadeTimeSlider", value);
            }
        }

        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                if (MainWindow.Settings?.Canvas != null)
                {
                    var canvas = MainWindow.Settings.Canvas;
                    SetOptionButtonState("EraserSize", canvas.EraserSize);
                    SetOptionButtonState("HyperbolaAsymptote", (int)canvas.HyperbolaAsymptoteOption);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CanvasAndInkPanel 应用主题时出�? {ex.Message}");
            }
        }
    }
}

