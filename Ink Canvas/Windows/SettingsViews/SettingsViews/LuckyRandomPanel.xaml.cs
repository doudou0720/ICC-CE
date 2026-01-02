using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class LuckyRandomPanel : UserControl
    {
        private bool _isLoaded = false;

        public LuckyRandomPanel()
        {
            InitializeComponent();
            Loaded += LuckyRandomPanel_Loaded;
        }

        private void LuckyRandomPanel_Loaded(object sender, RoutedEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"LuckyRandomPanel 启用触摸支持时出�? {ex.Message}");
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
            if (MainWindow.Settings == null || MainWindow.Settings.RandSettings == null) return;

            _isLoaded = false;

            try
            {
                var randSettings = MainWindow.Settings.RandSettings;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchDisplayRandWindowNamesInputBtn"), randSettings.DisplayRandWindowNamesInputBtn);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowRandomAndSingleDraw"), randSettings.ShowRandomAndSingleDraw);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableQuickDraw"), randSettings.EnableQuickDraw);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchExternalCaller"), randSettings.DirectCallCiRand);

                SetOptionButtonState("ExternalCallerType", randSettings.ExternalCallerType);

                if (RandWindowOnceCloseLatencySlider != null)
                {
                    RandWindowOnceCloseLatencySlider.Value = randSettings.RandWindowOnceCloseLatency;
                    if (RandWindowOnceCloseLatencyText != null)
                    {
                        RandWindowOnceCloseLatencyText.Text = $"{randSettings.RandWindowOnceCloseLatency:F1}s";
                    }
                }

                if (RandWindowOnceMaxStudentsSlider != null)
                {
                    RandWindowOnceMaxStudentsSlider.Value = randSettings.RandWindowOnceMaxStudents;
                    if (RandWindowOnceMaxStudentsText != null)
                    {
                        RandWindowOnceMaxStudentsText.Text = randSettings.RandWindowOnceMaxStudents.ToString();
                    }
                }

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchUseNewRollCallUI"), randSettings.UseNewRollCallUI);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableMLAvoidance"), randSettings.EnableMLAvoidance);

                if (MLAvoidanceHistorySlider != null)
                {
                    MLAvoidanceHistorySlider.Value = randSettings.MLAvoidanceHistoryCount;
                    if (MLAvoidanceHistoryText != null)
                    {
                        MLAvoidanceHistoryText.Text = randSettings.MLAvoidanceHistoryCount.ToString();
                    }
                }

                if (MLAvoidanceWeightSlider != null)
                {
                    MLAvoidanceWeightSlider.Value = randSettings.MLAvoidanceWeight;
                    if (MLAvoidanceWeightText != null)
                    {
                        MLAvoidanceWeightText.Text = $"{(randSettings.MLAvoidanceWeight * 100):F0}%";
                    }
                }

                SetOptionButtonState("PickNameBackground", randSettings.SelectedBackgroundIndex);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载幸运随机设置时出�? {ex.Message}");
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
            var buttons = new Dictionary<string, string[]>
            {
                { "ExternalCallerType", new[] { "ClassIsland", "SecRandom", "NamePicker" } },
                { "PickNameBackground", new[] { "Default" } }
            };

            if (!buttons.ContainsKey(group)) return;

            string[] buttonNames = buttons[group];

            for (int i = 0; i < buttonNames.Length; i++)
            {
                var button = this.FindDescendantByName($"{group}{buttonNames[i]}Border") as Border;
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

            var randSettings = MainWindow.Settings.RandSettings;
            if (randSettings == null) return;

            switch (tag)
            {
                case "DisplayRandWindowNamesInputBtn":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchDisplayRandWindowNamesInputBtn", newState);
                    break;

                case "ShowRandomAndSingleDraw":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowRandomAndSingleDraw", newState);
                    break;

                case "EnableQuickDraw":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableQuickDraw", newState);
                    break;

                case "ExternalCaller":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchExternalCaller", newState);
                    break;

                case "UseNewRollCallUI":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchUseNewRollCallUI", newState);
                    break;

                case "EnableMLAvoidance":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableMLAvoidance", newState);
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

            var randSettings = MainWindow.Settings.RandSettings;
            if (randSettings == null) return;

            switch (group)
            {
                case "ExternalCallerType":
                    int callerType;
                    switch (value)
                    {
                        case "ClassIsland":
                            callerType = 0;
                            break;
                        case "SecRandom":
                            callerType = 1;
                            break;
                        case "NamePicker":
                            callerType = 2;
                            break;
                        default:
                            callerType = 0;
                            break;
                    }
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var comboBox = mainWindow.FindName("ComboBoxExternalCallerType") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > callerType)
                        {
                            comboBox.SelectedIndex = callerType;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxExternalCallerType", comboBox.Items[callerType]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                    randSettings.ExternalCallerType = callerType;
                            }, "ComboBoxExternalCallerType");
                        }
                    }
                    break;

                case "PickNameBackground":
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                    randSettings.SelectedBackgroundIndex = 0; 
                    }, "PickNameBackground");
                    break;
            }
        }

        private void RandWindowOnceCloseLatencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (RandWindowOnceCloseLatencySlider != null && RandWindowOnceCloseLatencyText != null)
            {
                double value = RandWindowOnceCloseLatencySlider.Value;
                RandWindowOnceCloseLatencyText.Text = $"{value:F1}s";
                MainWindowSettingsHelper.InvokeSliderValueChanged("RandWindowOnceCloseLatencySlider", value);
            }
        }

        private void RandWindowOnceMaxStudentsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (RandWindowOnceMaxStudentsSlider != null && RandWindowOnceMaxStudentsText != null)
            {
                double value = RandWindowOnceMaxStudentsSlider.Value;
                RandWindowOnceMaxStudentsText.Text = ((int)value).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("RandWindowOnceMaxStudentsSlider", value);
            }
        }

        private void MLAvoidanceHistorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (MLAvoidanceHistorySlider != null && MLAvoidanceHistoryText != null)
            {
                double value = MLAvoidanceHistorySlider.Value;
                MLAvoidanceHistoryText.Text = ((int)value).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("MLAvoidanceHistorySlider", value);
            }
        }

        private void MLAvoidanceWeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (MLAvoidanceWeightSlider != null && MLAvoidanceWeightText != null)
            {
                double value = MLAvoidanceWeightSlider.Value;
                MLAvoidanceWeightText.Text = $"{(value * 100):F0}%";
                MainWindowSettingsHelper.InvokeSliderValueChanged("MLAvoidanceWeightSlider", value);
            }
        }
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                if (MainWindow.Settings?.RandSettings != null)
                {
                    var randSettings = MainWindow.Settings.RandSettings;
                    SetOptionButtonState("ExternalCallerType", randSettings.ExternalCallerType);
                    SetOptionButtonState("PickNameBackground", randSettings.SelectedBackgroundIndex);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LuckyRandomPanel 应用主题时出�? {ex.Message}");
            }
        }
    }
}

