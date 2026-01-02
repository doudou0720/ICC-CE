using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class ThemePanel : UserControl
    {
        private bool _isLoaded = false;

        public ThemePanel()
        {
            InitializeComponent();
            Loaded += ThemePanel_Loaded;
        }

        private void ThemePanel_Loaded(object sender, RoutedEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"ThemePanel 启用触摸支持时出�? {ex.Message}");
            }
        }

        public event EventHandler<RoutedEventArgs> IsTopBarNeedShadowEffect;
        public event EventHandler<RoutedEventArgs> IsTopBarNeedNoShadowEffect;
        public event EventHandler<RoutedEventArgs> ThemeChanged;

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
            if (MainWindow.Settings == null || MainWindow.Settings.Appearance == null) return;

            _isLoaded = false;

            try
            {
                var appearance = MainWindow.Settings.Appearance;

                SetOptionButtonState("Theme", appearance.Theme);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableSplashScreen"), appearance.EnableSplashScreen);
                if (SplashScreenStylePanel != null)
                {
                    SplashScreenStylePanel.Visibility = appearance.EnableSplashScreen ? Visibility.Visible : Visibility.Collapsed;
                }

                if (ComboBoxSplashScreenStyle != null)
                {
                    ComboBoxSplashScreenStyle.SelectedIndex = Math.Min(appearance.SplashScreenStyle, ComboBoxSplashScreenStyle.Items.Count - 1);
                }

                if (ComboBoxFloatingBarImg != null)
                {
                    int selectedIndex = Math.Min(appearance.FloatingBarImg, ComboBoxFloatingBarImg.Items.Count - 1);
                    ComboBoxFloatingBarImg.SelectedIndex = selectedIndex;
                }

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

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableDisPlayNibModeToggle"), appearance.IsEnableDisPlayNibModeToggler);

                if (CheckBoxUseLegacyFloatingBarUI != null)
                {
                    CheckBoxUseLegacyFloatingBarUI.IsChecked = appearance.UseLegacyFloatingBarUI;
                }

                if (CheckBoxShowShapeButton != null) CheckBoxShowShapeButton.IsChecked = appearance.IsShowShapeButton;
                if (CheckBoxShowUndoButton != null) CheckBoxShowUndoButton.IsChecked = appearance.IsShowUndoButton;
                if (CheckBoxShowRedoButton != null) CheckBoxShowRedoButton.IsChecked = appearance.IsShowRedoButton;
                if (CheckBoxShowClearButton != null) CheckBoxShowClearButton.IsChecked = appearance.IsShowClearButton;
                if (CheckBoxShowWhiteboardButton != null) CheckBoxShowWhiteboardButton.IsChecked = appearance.IsShowWhiteboardButton;
                if (CheckBoxShowHideButton != null) CheckBoxShowHideButton.IsChecked = appearance.IsShowHideButton;
                if (CheckBoxShowQuickColorPalette != null) CheckBoxShowQuickColorPalette.IsChecked = appearance.IsShowQuickColorPalette;
                if (CheckBoxShowLassoSelectButton != null) CheckBoxShowLassoSelectButton.IsChecked = appearance.IsShowLassoSelectButton;
                if (CheckBoxShowClearAndMouseButton != null) CheckBoxShowClearAndMouseButton.IsChecked = appearance.IsShowClearAndMouseButton;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableTrayIcon"), appearance.EnableTrayIcon);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableViewboxBlackBoardScaleTransform"), appearance.EnableViewboxBlackBoardScaleTransform);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableTimeDisplayInWhiteboardMode"), appearance.EnableTimeDisplayInWhiteboardMode);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableChickenSoupInWhiteboardMode"), appearance.EnableChickenSoupInWhiteboardMode);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableQuickPanel"), appearance.IsShowQuickPanel);

                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoEnterAnnotationModeWhenExitFoldMode"), MainWindow.Settings.Automation.IsAutoEnterAnnotationModeWhenExitFoldMode);
                }

                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldAfterPPTSlideShow"), MainWindow.Settings.Automation.IsAutoFoldAfterPPTSlideShow);
                }

                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldWhenExitWhiteboard"), MainWindow.Settings.Automation.IsAutoFoldWhenExitWhiteboard);
                }

                SetOptionButtonState("ChickenSoupSource", appearance.ChickenSoupSource);

                SetOptionButtonState("UnFoldBtnImg", appearance.UnFoldButtonImageType);

                SetOptionButtonState("QuickColorPaletteDisplayMode", appearance.QuickColorPaletteDisplayMode);

                SetOptionButtonState("EraserDisplayOption", appearance.EraserDisplayOption);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载个性化设置时出�? {ex.Message}");
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

            var appearance = MainWindow.Settings.Appearance;
            if (appearance == null) return;

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
                    var taskbar = Application.Current.Resources["TaskbarTrayIcon"] as TaskbarIcon;
                    if (taskbar != null)
                    {
                        taskbar.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
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

        private void ComboBoxSplashScreenStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (ComboBoxSplashScreenStyle?.SelectedIndex >= 0)
            {
                MainWindowSettingsHelper.InvokeComboBoxSelectionChangedWithThemeCheck("ComboBoxSplashScreenStyle", ComboBoxSplashScreenStyle.SelectedItem);
            }
        }

        private void ComboBoxFloatingBarImg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (ComboBoxFloatingBarImg?.SelectedIndex >= 0)
            {
                MainWindowSettingsHelper.InvokeComboBoxSelectionChangedWithThemeCheck("ComboBoxFloatingBarImg", ComboBoxFloatingBarImg.SelectedItem);
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

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            string name = checkBox.Name;
            var appearance = MainWindow.Settings.Appearance;
            if (appearance == null) return;

            switch (name)
            {
                case "CheckBoxUseLegacyFloatingBarUI":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxUseLegacyFloatingBarUI", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowShapeButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowShapeButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowUndoButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowUndoButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowRedoButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowRedoButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowClearButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowClearButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowWhiteboardButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowWhiteboardButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowHideButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowHideButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowQuickColorPalette":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowQuickColorPalette", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowLassoSelectButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowLassoSelectButton", checkBox.IsChecked ?? false);
                    break;

                case "CheckBoxShowClearAndMouseButton":
                    MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged("CheckBoxShowClearAndMouseButton", checkBox.IsChecked ?? false);
                    break;
            }
        }

        private void SetOptionButtonState(string group, int selectedIndex)
        {
            var buttons = new Dictionary<string, string[]>
            {
                { "Theme", new[] { "Light", "Dark", "System" } },
                { "ChickenSoupSource", new[] { "Osu", "Motivational", "Gaokao", "Hitokoto" } },
                { "UnFoldBtnImg", new[] { "Arrow", "Pen" } },
                { "QuickColorPaletteDisplayMode", new[] { "Single", "Double" } },
                { "EraserDisplayOption", new[] { "Both", "Area", "Line", "None" } }
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

            var appearance = MainWindow.Settings.Appearance;
            if (appearance == null) return;

            switch (group)
            {
                case "Theme":
                    try
                    {
                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow != null)
                        {
                            var comboBox = mainWindow.FindName("ComboBoxTheme") as System.Windows.Controls.ComboBox;
                            if (comboBox != null)
                            {
                    int themeIndex;
                    switch (value)
                    {
                        case "Light":
                            themeIndex = 0;
                            break;
                        case "Dark":
                            themeIndex = 1;
                            break;
                        case "System":
                            themeIndex = 2;
                            break;
                        default:
                            themeIndex = 2;
                            break;
                    }
                                
                                if (comboBox.Items.Count > themeIndex)
                                {
                                    var selectedItem = comboBox.Items[themeIndex];
                                    MainWindowSettingsHelper.InvokeComboBoxSelectionChangedWithThemeCheck("ComboBoxTheme", selectedItem);
                                }
                                else
                    {
                                    MainWindowSettingsHelper.UpdateSettingSafely(() =>
                                    {
                                        appearance.Theme = themeIndex;
                                    }, "ComboBoxTheme_SelectionChanged", "ComboBoxTheme");
                                    MainWindowSettingsHelper.NotifyThemeUpdateIfNeeded("ComboBoxTheme");
                                    
                                    ThemeChanged?.Invoke(this, new RoutedEventArgs());
                                }
                            }
                            else
                            {
                                int themeIndex;
                                switch (value)
                                {
                                    case "Light":
                                        themeIndex = 0;
                                        break;
                                    case "Dark":
                                        themeIndex = 1;
                                        break;
                                    case "System":
                                        themeIndex = 2;
                                        break;
                                    default:
                                        themeIndex = 2;
                                        break;
                                }
                                MainWindowSettingsHelper.UpdateSettingSafely(() =>
                                {
                                    appearance.Theme = themeIndex;
                                }, "ComboBoxTheme_SelectionChanged", "ComboBoxTheme");
                                MainWindowSettingsHelper.NotifyThemeUpdateIfNeeded("ComboBoxTheme");
                        
                        ThemeChanged?.Invoke(this, new RoutedEventArgs());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"切换主题时出�? {ex.Message}");
                    }
                    break;

                case "ChickenSoupSource":
                    int sourceIndex;
                    switch (value)
                    {
                        case "Osu":
                            sourceIndex = 0;
                            break;
                        case "Motivational":
                            sourceIndex = 1;
                            break;
                        case "Gaokao":
                            sourceIndex = 2;
                            break;
                        case "Hitokoto":
                            sourceIndex = 3;
                            break;
                        default:
                            sourceIndex = 3;
                            break;
                    }
                    var mainWindow6 = Application.Current.MainWindow as MainWindow;
                    if (mainWindow6 != null)
                    {
                        var comboBox = mainWindow6.FindName("ComboBoxChickenSoupSource") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > sourceIndex)
                        {
                            comboBox.SelectedIndex = sourceIndex;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxChickenSoupSource", comboBox.Items[sourceIndex]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                    appearance.ChickenSoupSource = sourceIndex;
                            }, "ComboBoxChickenSoupSource");
                        }
                    }
                    break;

                case "UnFoldBtnImg":
                    int imgType;
                    switch (value)
                    {
                        case "Arrow":
                            imgType = 0;
                            break;
                        case "Pen":
                            imgType = 1;
                            break;
                        default:
                            imgType = 0;
                            break;
                    }
                    var mainWindow3 = Application.Current.MainWindow as MainWindow;
                    if (mainWindow3 != null)
                    {
                        var comboBox = mainWindow3.FindName("ComboBoxUnFoldBtnImg") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > imgType)
                        {
                            comboBox.SelectedIndex = imgType;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChangedWithThemeCheck("ComboBoxUnFoldBtnImg", comboBox.Items[imgType]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingSafely(() =>
                            {
                    appearance.UnFoldButtonImageType = imgType;
                            }, "ComboBoxUnFoldBtnImg_SelectionChanged", "ComboBoxUnFoldBtnImg");
                            MainWindowSettingsHelper.NotifyThemeUpdateIfNeeded("ComboBoxUnFoldBtnImg");
                        }
                    }
                    break;

                case "QuickColorPaletteDisplayMode":
                    int displayMode;
                    switch (value)
                    {
                        case "Single":
                            displayMode = 0;
                            break;
                        case "Double":
                            displayMode = 1;
                            break;
                        default:
                            displayMode = 1;
                            break;
                    }
                    var mainWindow4 = Application.Current.MainWindow as MainWindow;
                    if (mainWindow4 != null)
                    {
                        var comboBox = mainWindow4.FindName("ComboBoxQuickColorPaletteDisplayMode") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > displayMode)
                        {
                            comboBox.SelectedIndex = displayMode;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxQuickColorPaletteDisplayMode", comboBox.Items[displayMode]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                    appearance.QuickColorPaletteDisplayMode = displayMode;
                            }, "ComboBoxQuickColorPaletteDisplayMode");
                        }
                    }
                    break;

                case "EraserDisplayOption":
                    int eraserOption;
                    switch (value)
                    {
                        case "Both":
                            eraserOption = 0;
                            break;
                        case "Area":
                            eraserOption = 1;
                            break;
                        case "Line":
                            eraserOption = 2;
                            break;
                        case "None":
                            eraserOption = 3;
                            break;
                        default:
                            eraserOption = 0;
                            break;
                    }
                    var mainWindow5 = Application.Current.MainWindow as MainWindow;
                    if (mainWindow5 != null)
                    {
                        var comboBox = mainWindow5.FindName("ComboBoxEraserDisplayOption") as System.Windows.Controls.ComboBox;
                        if (comboBox != null && comboBox.Items.Count > eraserOption)
                        {
                            comboBox.SelectedIndex = eraserOption;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxEraserDisplayOption", comboBox.Items[eraserOption]);
                        }
                        else
                        {
                            MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                            {
                    appearance.EraserDisplayOption = eraserOption;
                            }, "ComboBoxEraserDisplayOption");
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
                if (MainWindow.Settings?.Appearance != null)
                {
                    var appearance = MainWindow.Settings.Appearance;
                    SetOptionButtonState("Theme", appearance.Theme);
                    SetOptionButtonState("ChickenSoupSource", appearance.ChickenSoupSource);
                    SetOptionButtonState("UnFoldBtnImg", appearance.UnFoldButtonImageType);
                    SetOptionButtonState("QuickColorPaletteDisplayMode", appearance.QuickColorPaletteDisplayMode);
                    SetOptionButtonState("EraserDisplayOption", appearance.EraserDisplayOption);
                }
                
                UpdateComboBoxDropdownTheme(ComboBoxSplashScreenStyle);
                UpdateComboBoxDropdownTheme(ComboBoxFloatingBarImg);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ThemePanel 应用主题时出�? {ex.Message}");
            }
        }

        private void UpdateComboBoxDropdownTheme(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox == null) return;
            
            comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            comboBox.DropDownOpened += ComboBox_DropDownOpened;
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ThemeHelper.UpdateComboBoxDropdownColors(comboBox);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}

