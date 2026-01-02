using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class GesturesPanel : UserControl
    {
        private bool _isLoaded = false;

        public GesturesPanel()
        {
            InitializeComponent();
            Loaded += GesturesPanel_Loaded;
        }

        private void GesturesPanel_Loaded(object sender, RoutedEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"GesturesPanel 启用触摸支持时出�? {ex.Message}");
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
            if (MainWindow.Settings == null) return;

            _isLoaded = false;

            try
            {
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSwitchTwoFingerGesture"), MainWindow.Settings.Gesture.AutoSwitchTwoFingerGesture);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableTwoFingerRotationOnSelection"), MainWindow.Settings.Gesture.IsEnableTwoFingerRotationOnSelection);
                }
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePalmEraser"), MainWindow.Settings.Canvas.EnablePalmEraser);
                    if (PalmEraserSensitivityPanel != null)
                    {
                        PalmEraserSensitivityPanel.Visibility = MainWindow.Settings.Canvas.EnablePalmEraser ? Visibility.Visible : Visibility.Collapsed;
                    }
                    SetOptionButtonState("PalmEraserSensitivity", MainWindow.Settings.Canvas.PalmEraserSensitivity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载手势操作设置时出�? {ex.Message}");
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
                { "PalmEraserSensitivity", new[] { "Low", "Medium", "High" } }
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

            switch (tag)
            {
                case "AutoSwitchTwoFingerGesture":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSwitchTwoFingerGesture", newState);
                    break;

                case "EnableTwoFingerRotationOnSelection":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableTwoFingerRotationOnSelection", newState);
                    break;

                case "EnablePalmEraser":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePalmEraser", newState);
                    if (PalmEraserSensitivityPanel != null)
                    {
                        PalmEraserSensitivityPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
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

            switch (group)
            {
                case "PalmEraserSensitivity":
                    int sensitivity;
                    switch (value)
                    {
                        case "Low":
                            sensitivity = 0;
                            break;
                        case "Medium":
                            sensitivity = 1;
                            break;
                        case "High":
                            sensitivity = 2;
                            break;
                        default:
                            sensitivity = 0;
                            break;
                    }
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var comboBox = mainWindow.FindName("ComboBoxPalmEraserSensitivity") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > sensitivity)
                        {
                            comboBox.SelectedIndex = sensitivity;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxPalmEraserSensitivity", comboBox.Items[sensitivity]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                                if (MainWindow.Settings.Canvas != null)
                    {
                        MainWindow.Settings.Canvas.PalmEraserSensitivity = sensitivity;
                                }
                            }, "ComboBoxPalmEraserSensitivity");
                        }
                    }
                    break;
            }
        }
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                
                if (MainWindow.Settings?.Canvas != null)
                {
                    SetOptionButtonState("PalmEraserSensitivity", MainWindow.Settings.Canvas.PalmEraserSensitivity);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GesturesPanel 应用主题时出�? {ex.Message}");
            }
        }
    }
}

