using Ink_Canvas;
using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class AdvancedPanel : UserControl
    {
        private bool _isLoaded = false;

        public AdvancedPanel()
        {
            InitializeComponent();
            Loaded += AdvancedPanel_Loaded;
        }

        private void AdvancedPanel_Loaded(object sender, RoutedEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"AdvancedPanel 启用触摸支持时出�? {ex.Message}");
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
            if (MainWindow.Settings == null || MainWindow.Settings.Advanced == null) return;

            _isLoaded = false;

            try
            {
                var advanced = MainWindow.Settings.Advanced;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsSpecialScreen"), advanced.IsSpecialScreen);

                if (TouchMultiplierSlider != null)
                {
                    TouchMultiplierSlider.Value = advanced.TouchMultiplier;
                    if (TouchMultiplierText != null)
                    {
                        TouchMultiplierText.Text = advanced.TouchMultiplier.ToString("F2");
                    }
                }

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEraserBindTouchMultiplier"), advanced.EraserBindTouchMultiplier);

                if (NibModeBoundsWidthSlider != null)
                {
                    NibModeBoundsWidthSlider.Value = advanced.NibModeBoundsWidth;
                    if (NibModeBoundsWidthText != null)
                    {
                        NibModeBoundsWidthText.Text = advanced.NibModeBoundsWidth.ToString();
                    }
                }

                if (FingerModeBoundsWidthSlider != null)
                {
                    FingerModeBoundsWidthSlider.Value = advanced.FingerModeBoundsWidth;
                    if (FingerModeBoundsWidthText != null)
                    {
                        FingerModeBoundsWidthText.Text = advanced.FingerModeBoundsWidth.ToString();
                    }
                }

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsQuadIR"), advanced.IsQuadIR);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsLogEnabled"), advanced.IsLogEnabled);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsSaveLogByDate"), advanced.IsSaveLogByDate);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsSecondConfimeWhenShutdownApp"), advanced.IsSecondConfirmWhenShutdownApp);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableFullScreenHelper"), advanced.IsEnableFullScreenHelper);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableAvoidFullScreenHelper"), advanced.IsEnableAvoidFullScreenHelper);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableEdgeGestureUtil"), advanced.IsEnableEdgeGestureUtil);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableForceFullScreen"), advanced.IsEnableForceFullScreen);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableDPIChangeDetection"), advanced.IsEnableDPIChangeDetection);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsEnableResolutionChangeDetection"), advanced.IsEnableResolutionChangeDetection);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsAutoBackupBeforeUpdate"), advanced.IsAutoBackupBeforeUpdate);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIsAutoBackupEnabled"), advanced.IsAutoBackupEnabled);
                SetOptionButtonState("AutoBackupInterval", advanced.AutoBackupIntervalDays);

                if (MainWindow.Settings.Automation != null && MainWindow.Settings.Automation.FloatingWindowInterceptor != null)
                {
                    var interceptor = MainWindow.Settings.Automation.FloatingWindowInterceptor;
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchFloatingWindowInterceptorEnabled"), interceptor.IsEnabled);
                    
                    if (interceptor.InterceptRules != null)
                    {
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoWhiteboard3Floating"), interceptor.InterceptRules.ContainsKey("SeewoWhiteboard3Floating") && interceptor.InterceptRules["SeewoWhiteboard3Floating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoWhiteboard5Floating"), interceptor.InterceptRules.ContainsKey("SeewoWhiteboard5Floating") && interceptor.InterceptRules["SeewoWhiteboard5Floating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoWhiteboard5CFloating"), interceptor.InterceptRules.ContainsKey("SeewoWhiteboard5CFloating") && interceptor.InterceptRules["SeewoWhiteboard5CFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoPincoSideBarFloating"), interceptor.InterceptRules.ContainsKey("SeewoPincoSideBarFloating") && interceptor.InterceptRules["SeewoPincoSideBarFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoPincoDrawingFloating"), interceptor.InterceptRules.ContainsKey("SeewoPincoDrawingFloating") && interceptor.InterceptRules["SeewoPincoDrawingFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoPPTFloating"), interceptor.InterceptRules.ContainsKey("SeewoPPTFloating") && interceptor.InterceptRules["SeewoPPTFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAiClassFloating"), interceptor.InterceptRules.ContainsKey("AiClassFloating") && interceptor.InterceptRules["AiClassFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchHiteAnnotationFloating"), interceptor.InterceptRules.ContainsKey("HiteAnnotationFloating") && interceptor.InterceptRules["HiteAnnotationFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchChangYanFloating"), interceptor.InterceptRules.ContainsKey("ChangYanFloating") && interceptor.InterceptRules["ChangYanFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchChangYanPptFloating"), interceptor.InterceptRules.ContainsKey("ChangYanPptFloating") && interceptor.InterceptRules["ChangYanPptFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchIntelligentClassFloating"), interceptor.InterceptRules.ContainsKey("IntelligentClassFloating") && interceptor.InterceptRules["IntelligentClassFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoDesktopAnnotationFloating"), interceptor.InterceptRules.ContainsKey("SeewoDesktopAnnotationFloating") && interceptor.InterceptRules["SeewoDesktopAnnotationFloating"]);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSeewoDesktopSideBarFloating"), interceptor.InterceptRules.ContainsKey("SeewoDesktopSideBarFloating") && interceptor.InterceptRules["SeewoDesktopSideBarFloating"]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载高级设置时出�? {ex.Message}");
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

        private void SetOptionButtonState(string group, int selectedValue)
        {
            var buttons = new Dictionary<string, Dictionary<int, string>>
            {
                { "AutoBackupInterval", new Dictionary<int, string> { { 1, "1Day" }, { 3, "3Days" }, { 7, "7Days" }, { 14, "14Days" }, { 30, "30Days" } } }
            };

            if (!buttons.ContainsKey(group)) return;

            var buttonNames = buttons[group];
            if (!buttonNames.ContainsKey(selectedValue)) return;

            string buttonName = buttonNames[selectedValue];
            var button = this.FindDescendantByName($"{group}{buttonName}Border") as Border;
            if (button != null)
            {
                var parent = button.Parent as Panel;
                if (parent != null)
                {
                    foreach (var child in parent.Children)
                    {
                        if (child is Border childBorder && childBorder != button)
                        {
                            string childTag = childBorder.Tag?.ToString();
                            if (!string.IsNullOrEmpty(childTag) && childTag.StartsWith(group + "_"))
                            {
                                ThemeHelper.SetOptionButtonSelectedState(childBorder, false);
                            }
                        }
                    }
                }

                ThemeHelper.SetOptionButtonSelectedState(button, true);
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

            var advanced = MainWindow.Settings.Advanced;
            if (advanced == null) return;

            switch (tag)
            {
                case "IsSpecialScreen":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsSpecialScreen", newState);
                    break;

                case "EraserBindTouchMultiplier":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEraserBindTouchMultiplier", newState);
                    break;

                case "IsQuadIR":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsQuadIR", newState);
                    break;

                case "IsLogEnabled":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsLogEnabled", newState);
                    break;

                case "IsSaveLogByDate":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsSaveLogByDate", newState);
                    break;

                case "IsSecondConfirmWhenShutdownApp":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsSecondConfimeWhenShutdownApp", newState);
                    break;

                case "IsEnableFullScreenHelper":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableFullScreenHelper", newState);
                    break;

                case "IsEnableAvoidFullScreenHelper":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableAvoidFullScreenHelper", newState);
                    break;

                case "IsEnableEdgeGestureUtil":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableEdgeGestureUtil", newState);
                    break;

                case "IsEnableForceFullScreen":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableForceFullScreen", newState);
                    break;

                case "IsEnableDPIChangeDetection":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableDPIChangeDetection", newState);
                    break;

                case "IsEnableResolutionChangeDetection":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsEnableResolutionChangeDetection", newState);
                    break;

                case "IsAutoBackupBeforeUpdate":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoBackupBeforeUpdate", newState);
                    break;

                case "IsAutoBackupEnabled":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIsAutoBackupEnabled", newState);
                    break;

                case "FloatingWindowInterceptorEnabled":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFloatingWindowInterceptorEnabled", newState);
                    break;

                case "SeewoWhiteboard3Floating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoWhiteboard3Floating", newState);
                    break;

                case "SeewoWhiteboard5Floating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoWhiteboard5Floating", newState);
                    break;

                case "SeewoWhiteboard5CFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoWhiteboard5CFloating", newState);
                    break;

                case "SeewoPincoSideBarFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoPincoSideBarFloating", newState);
                    break;

                case "SeewoPincoDrawingFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoPincoDrawingFloating", newState);
                    break;

                case "SeewoPPTFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoPPTFloating", newState);
                    break;

                case "AiClassFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAiClassFloating", newState);
                    break;

                case "HiteAnnotationFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchHiteAnnotationFloating", newState);
                    break;

                case "ChangYanFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchChangYanFloating", newState);
                    break;

                case "ChangYanPptFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchChangYanPptFloating", newState);
                    break;

                case "IntelligentClassFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchIntelligentClassFloating", newState);
                    break;

                case "SeewoDesktopAnnotationFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoDesktopAnnotationFloating", newState);
                    break;

                case "SeewoDesktopSideBarFloating":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSeewoDesktopSideBarFloating", newState);
                    break;
            }
        }

        private void TouchMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (TouchMultiplierSlider != null && TouchMultiplierText != null)
            {
                double value = TouchMultiplierSlider.Value;
                TouchMultiplierText.Text = value.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("TouchMultiplierSlider", value);
            }
        }

        private void NibModeBoundsWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (NibModeBoundsWidthSlider != null && NibModeBoundsWidthText != null)
            {
                double value = NibModeBoundsWidthSlider.Value;
                NibModeBoundsWidthText.Text = ((int)value).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("NibModeBoundsWidthSlider", value);
            }
        }

        private void FingerModeBoundsWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (FingerModeBoundsWidthSlider != null && FingerModeBoundsWidthText != null)
            {
                double value = FingerModeBoundsWidthSlider.Value;
                FingerModeBoundsWidthText.Text = ((int)value).ToString();
                MainWindowSettingsHelper.InvokeSliderValueChanged("FingerModeBoundsWidthSlider", value);
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

            if (MainWindow.Settings.Advanced == null) return;

            switch (group)
            {
                case "AutoBackupInterval":
                    int days;
                    if (int.TryParse(value, out days))
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            var comboBox = mainWindow.FindName("ComboBoxAutoBackupInterval") as System.Windows.Controls.ComboBox;
                            if (comboBox != null)
                            {
                                foreach (ComboBoxItem item in comboBox.Items)
                                {
                                    if (item.Tag != null && int.TryParse(item.Tag.ToString(), out int tagValue) && tagValue == days)
                                    {
                                        comboBox.SelectedItem = item;
                                        MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxAutoBackupInterval", item);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                                {
                                    MainWindow.Settings.Advanced.AutoBackupIntervalDays = days;
                                }, "ComboBoxAutoBackupInterval");
                            }
                        }
                    }
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            string name = button.Name;
            switch (name)
            {
                case "BtnManualBackup":
                    break;

                case "BtnRestoreBackup":
                    break;

                case "BtnUnregisterFileAssociation":
                    break;

                case "BtnCheckFileAssociation":
                    CheckFileAssociationStatus();
                    break;

                case "BtnRegisterFileAssociation":
                    break;

                case "BtnDlassSettingsManage":
                    break;
            }
        }

        private void CheckFileAssociationStatus()
        {
            try
            {
                bool isRegistered = FileAssociationManager.IsFileAssociationRegistered();
                
                if (FileAssociationStatusText != null)
                {
                    FileAssociationStatusText.Visibility = Visibility.Visible;
                    
                    if (isRegistered)
                    {
                        FileAssociationStatusText.Text = "✓ .icstk文件关联已注册";
                        FileAssociationStatusText.Foreground = new SolidColorBrush(Colors.LightGreen);
                    }
                    else
                    {
                        FileAssociationStatusText.Text = "✗ .icstk文件关联未注册";
                        FileAssociationStatusText.Foreground = new SolidColorBrush(Colors.LightCoral);
                    }
                }
            }
            catch (Exception ex)
            {
                if (FileAssociationStatusText != null)
                {
                    FileAssociationStatusText.Visibility = Visibility.Visible;
                    FileAssociationStatusText.Text = $"✗ 检查文件关联状态时出错: {ex.Message}";
                    FileAssociationStatusText.Foreground = new SolidColorBrush(Colors.LightCoral);
                }
            }
        }
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                if (MainWindow.Settings?.Advanced != null)
                {
                    SetOptionButtonState("AutoBackupInterval", MainWindow.Settings.Advanced.AutoBackupIntervalDays);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AdvancedPanel 应用主题时出�? {ex.Message}");
            }
        }
    }
}
