using Ink_Canvas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Media = System.Windows.Media;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public static class MainWindowSettingsHelper
    {
        private static MainWindow GetMainWindow()
        {
            return Application.Current.MainWindow as MainWindow;
        }
        public static void InvokeMainWindowMethod(string methodName, params object[] parameters)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;

                var method = mainWindow.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(mainWindow, parameters);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 MainWindow 方法 {methodName} 失败: {ex.Message}");
            }
        }
        public static void InvokeToggleSwitchToggled(string toggleSwitchName, bool isOn)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                var toggleSwitch = mainWindow.FindName(toggleSwitchName);
                if (toggleSwitch == null)
                {
                    var property = mainWindow.GetType().GetProperty(toggleSwitchName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.PropertyType.Name.Contains("ToggleSwitch"))
                    {
                        var isOnProperty = property.PropertyType.GetProperty("IsOn");
                        if (isOnProperty != null)
                        {
                            var toggleSwitchInstance = property.GetValue(mainWindow);
                            if (toggleSwitchInstance != null)
                            {
                                isOnProperty.SetValue(toggleSwitchInstance, isOn);
                            }
                        }
                    }
                    else
                    {
                        if (toggleSwitchName.StartsWith("ToggleSwitchAutoFold") || toggleSwitchName == "ToggleSwitchKeepFoldAfterSoftwareExit")
                        {
                            UpdateAutoFoldSetting(toggleSwitchName, isOn);
                            if (toggleSwitchName.StartsWith("ToggleSwitchAutoFold") && 
                                !toggleSwitchName.Contains("PPTSlideShow") && 
                                !toggleSwitchName.Contains("KeepFold") &&
                                !toggleSwitchName.Contains("IgnoreDesktopAnno"))
                            {
                                try
                                {
                                    InvokeMainWindowMethod("StartOrStoptimerCheckAutoFold");
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"调用 StartOrStoptimerCheckAutoFold 失败: {ex.Message}");
                                }
                            }
                            NotifySettingsPanelsSyncState(toggleSwitchName);
                            return;
                        }
                    }
                    var toggledMethodName = toggleSwitchName + "_Toggled";
                    var method = mainWindow.GetType().GetMethod(toggledMethodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    if (method != null)
                    {
                        try
                        {
                            InvokeMainWindowMethod(toggledMethodName, null, new RoutedEventArgs());
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"调用 {toggledMethodName} 失败: {ex.Message}");
                        }
                    }
                    NotifySettingsPanelsSyncState(toggleSwitchName);
                    return;
                }
                var toggleSwitchType = toggleSwitch.GetType();
                var isOnProp = toggleSwitchType.GetProperty("IsOn");
                if (isOnProp != null)
                {
                    isOnProp.SetValue(toggleSwitch, isOn);
                }
                var toggledMethodName2 = toggleSwitchName + "_Toggled";
                InvokeMainWindowMethod(toggledMethodName2, toggleSwitch, new RoutedEventArgs());
                NotifySettingsPanelsSyncState(toggleSwitchName);
                NotifyThemeUpdateIfNeeded(toggleSwitchName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 ToggleSwitch {toggleSwitchName} 失败: {ex.Message}");
            }
        }
        private static void UpdateAutoFoldSetting(string toggleSwitchName, bool isOn)
        {
            try
            {
                if (MainWindow.Settings?.Automation == null) return;
                var settingMap = new Dictionary<string, Action<bool>>
                {
                    { "ToggleSwitchAutoFoldInEasiNote", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiNote = val },
                    { "ToggleSwitchAutoFoldInEasiCamera", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiCamera = val },
                    { "ToggleSwitchAutoFoldInHiteTouchPro", (val) => MainWindow.Settings.Automation.IsAutoFoldInHiteTouchPro = val },
                    { "ToggleSwitchAutoFoldInEasiNote3", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiNote3 = val },
                    { "ToggleSwitchAutoFoldInEasiNote3C", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiNote3C = val },
                    { "ToggleSwitchAutoFoldInEasiNote5C", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiNote5C = val },
                    { "ToggleSwitchAutoFoldInSeewoPincoTeacher", (val) => MainWindow.Settings.Automation.IsAutoFoldInSeewoPincoTeacher = val },
                    { "ToggleSwitchAutoFoldInHiteCamera", (val) => MainWindow.Settings.Automation.IsAutoFoldInHiteCamera = val },
                    { "ToggleSwitchAutoFoldInHiteLightBoard", (val) => MainWindow.Settings.Automation.IsAutoFoldInHiteLightBoard = val },
                    { "ToggleSwitchAutoFoldInWxBoardMain", (val) => MainWindow.Settings.Automation.IsAutoFoldInWxBoardMain = val },
                    { "ToggleSwitchAutoFoldInMSWhiteboard", (val) => MainWindow.Settings.Automation.IsAutoFoldInMSWhiteboard = val },
                    { "ToggleSwitchAutoFoldInAdmoxWhiteboard", (val) => MainWindow.Settings.Automation.IsAutoFoldInAdmoxWhiteboard = val },
                    { "ToggleSwitchAutoFoldInAdmoxBooth", (val) => MainWindow.Settings.Automation.IsAutoFoldInAdmoxBooth = val },
                    { "ToggleSwitchAutoFoldInQPoint", (val) => MainWindow.Settings.Automation.IsAutoFoldInQPoint = val },
                    { "ToggleSwitchAutoFoldInYiYunVisualPresenter", (val) => MainWindow.Settings.Automation.IsAutoFoldInYiYunVisualPresenter = val },
                    { "ToggleSwitchAutoFoldInMaxHubWhiteboard", (val) => MainWindow.Settings.Automation.IsAutoFoldInMaxHubWhiteboard = val },
                    { "ToggleSwitchAutoFoldInPPTSlideShow", (val) => MainWindow.Settings.Automation.IsAutoFoldInPPTSlideShow = val },
                    { "ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno", (val) => MainWindow.Settings.Automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno = val },
                    { "ToggleSwitchAutoFoldInOldZyBoard", (val) => MainWindow.Settings.Automation.IsAutoFoldInOldZyBoard = val },
                    { "ToggleSwitchKeepFoldAfterSoftwareExit", (val) => MainWindow.Settings.Automation.KeepFoldAfterSoftwareExit = val }
                };

                if (settingMap.ContainsKey(toggleSwitchName))
                {
                    settingMap[toggleSwitchName](isOn);
                    MainWindow.SaveSettingsToFile();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新自动收纳设置失败: {ex.Message}");
            }
        }
        private class ToggleSwitchWrapper
        {
            public bool IsOn { get; set; }
            public string Name { get; set; }
        }
        public static void InvokeComboBoxSelectionChanged(string comboBoxName, object selectedItem)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                var comboBox = mainWindow.FindName(comboBoxName) as System.Windows.Controls.ComboBox;
                if (comboBox != null)
                {
                    comboBox.SelectedItem = selectedItem;
                    var selectionChangedMethodName = comboBoxName + "_SelectionChanged";
                    InvokeMainWindowMethod(selectionChangedMethodName, comboBox, new System.Windows.Controls.SelectionChangedEventArgs(
                        System.Windows.Controls.Primitives.Selector.SelectionChangedEvent, 
                        new System.Collections.IList[0], 
                        new System.Collections.IList[0]));
                }
                NotifySettingsPanelsSyncState(comboBoxName);
                NotifyThemeUpdateIfNeeded(comboBoxName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 ComboBox {comboBoxName} 失败: {ex.Message}");
            }
        }
        public static void InvokeSliderValueChanged(string sliderName, double value)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                var slider = mainWindow.FindName(sliderName) as System.Windows.Controls.Slider;
                if (slider != null)
                {
                    var oldValue = slider.Value;
                    if (Math.Abs(slider.Value - value) > double.Epsilon)
                    {
                        slider.Value = value;
                    }
                    var valueChangedMethodName = sliderName + "_ValueChanged";
                    InvokeMainWindowMethod(valueChangedMethodName, slider, 
                        new System.Windows.RoutedPropertyChangedEventArgs<double>(oldValue, value));
                }
                NotifySettingsPanelsSyncState(sliderName);
                NotifyThemeUpdateIfNeeded(sliderName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 Slider {sliderName} 失败: {ex.Message}");
            }
        }
        public static void InvokeCheckBoxCheckedChanged(string checkBoxName, bool isChecked)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                var checkBox = mainWindow.FindName(checkBoxName) as System.Windows.Controls.CheckBox;
                if (checkBox != null)
                {
                    checkBox.IsChecked = isChecked;
                    var methodNames = new[]
                    {
                        checkBoxName + "_IsCheckChanged",
                        checkBoxName + "_IsCheckChange",
                        checkBoxName + "_Checked",
                        checkBoxName + "_Unchecked"
                    };
                    
                    foreach (var methodName in methodNames)
                    {
                        var method = mainWindow.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            InvokeMainWindowMethod(methodName, checkBox, new RoutedEventArgs());
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 CheckBox {checkBoxName} 失败: {ex.Message}");
            }
        }
        public static void UpdateSettingSafely(Action action, string eventHandlerName = null, string controlName = null, params object[] eventHandlerParams)
        {
            try
            {
                if (!string.IsNullOrEmpty(eventHandlerName))
                {
                    var mainWindow = GetMainWindow();
                    if (mainWindow != null)
                    {
                        var method = mainWindow.GetType().GetMethod(eventHandlerName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(mainWindow, eventHandlerParams);
                            if (!string.IsNullOrEmpty(controlName))
                            {
                                NotifySettingsPanelsSyncState(controlName);
                            }
                            return;
                        }
                    }
                }
                action?.Invoke();
                MainWindow.SaveSettingsToFile();
                if (!string.IsNullOrEmpty(controlName))
                {
                    NotifySettingsPanelsSyncState(controlName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新设置失败: {ex.Message}");
            }
        }
        public static void UpdateSettingDirectly(Action action, string controlName = null)
        {
            try
            {
                action?.Invoke();
                MainWindow.SaveSettingsToFile();
                if (!string.IsNullOrEmpty(controlName))
                {
                    NotifySettingsPanelsSyncState(controlName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"直接更新设置失败: {ex.Message}");
            }
        }
        public static void InvokeTextBoxTextChanged(string textBoxName, string text)
        {
            try
            {
                var mainWindow = GetMainWindow();
                if (mainWindow == null) return;
                var textBox = mainWindow.FindName(textBoxName) as System.Windows.Controls.TextBox;
                if (textBox != null)
                {
                    textBox.Text = text;
                    var textChangedMethodName = textBoxName + "_TextChanged";
                    InvokeMainWindowMethod(textChangedMethodName, textBox, new System.Windows.Controls.TextChangedEventArgs(
                        System.Windows.Controls.Primitives.TextBoxBase.TextChangedEvent,
                        System.Windows.Controls.UndoAction.None));
                }
                NotifySettingsPanelsSyncState(textBoxName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用 TextBox {textBoxName} 失败: {ex.Message}");
            }
        }
        public static void NotifySettingsPanelsSyncState(string controlName)
        {
            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window.GetType().Name == "SettingsWindow")
                        {
                            var panelToSync = GetPanelForControl(controlName);
                            
                            if (panelToSync != null)
                            {
                                var panelProp = window.GetType().GetProperty(panelToSync, BindingFlags.Public | BindingFlags.Instance);
                                if (panelProp != null)
                                {
                                    var panel = panelProp.GetValue(window) as System.Windows.Controls.UserControl;
                                    if (panel != null)
                                    {
                                        var loadMethod = panel.GetType().GetMethod("LoadSettings", 
                                            BindingFlags.Public | BindingFlags.Instance);
                                        if (loadMethod != null)
                                        {
                                            loadMethod.Invoke(panel, null);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                SyncAllPanels(window);
                            }
                            break; 
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知设置面板同步状态失�? {ex.Message}");
            }
        }
        private static string GetPanelForControl(string controlName)
        {
            var controlToPanel = new Dictionary<string, string>
            {
                { "ToggleSwitchIsAutoUpdate", "StartupPanel" },
                { "ToggleSwitchIsAutoUpdateWithSilence", "StartupPanel" },
                { "ToggleSwitchRunAtStartup", "StartupPanel" },
                { "ToggleSwitchFoldAtStartup", "StartupPanel" },
                { "AutoUpdateWithSilenceStartTimeComboBox", "StartupPanel" },
                { "AutoUpdateWithSilenceEndTimeComboBox", "StartupPanel" },
                { "ComboBoxTheme", "ThemePanel" },
                { "ToggleSwitchEnableSplashScreen", "ThemePanel" },
                { "ComboBoxSplashScreenStyle", "ThemePanel" },
                { "ToggleSwitchEnableTrayIcon", "ThemePanel" },
                { "ComboBoxFloatingBarImg", "ThemePanel" },
                { "ComboBoxUnFoldBtnImg", "ThemePanel" },
                { "ComboBoxChickenSoupSource", "ThemePanel" },
                { "ComboBoxQuickColorPaletteDisplayMode", "ThemePanel" },
                { "ComboBoxEraserDisplayOption", "ThemePanel" },
                { "ToggleSwitchEnableQuickPanel", "ThemePanel" },
                { "ViewboxFloatingBarScaleTransformValueSlider", "ThemePanel" },
                { "ViewboxFloatingBarOpacityValueSlider", "ThemePanel" },
                { "ViewboxFloatingBarOpacityInPPTValueSlider", "ThemePanel" },
                { "ToggleSwitchSupportPowerPoint", "PowerPointPanel" },
                { "ToggleSwitchShowPPTButton", "PowerPointPanel" },
                { "ToggleSwitchEnablePPTButtonPageClickable", "PowerPointPanel" },
                { "ToggleSwitchShowCanvasAtNewSlideShow", "PowerPointPanel" },
                { "PPTButtonLeftPositionValueSlider", "PowerPointPanel" },
                { "PPTButtonRightPositionValueSlider", "PowerPointPanel" },
                { "ToggleSwitchEnableTwoFingerRotationOnSelection", "GesturesPanel" },
                { "ToggleSwitchEnablePalmEraser", "GesturesPanel" },
                { "ComboBoxPalmEraserSensitivity", "GesturesPanel" },
                { "ToggleSwitchShowCursor", "CanvasAndInkPanel" },
                { "ToggleSwitchDisablePressure", "CanvasAndInkPanel" },
                { "ToggleSwitchEnablePressureTouchMode", "CanvasAndInkPanel" },
                { "ComboBoxEraserSize", "CanvasAndInkPanel" },
                { "ComboBoxHyperbolaAsymptoteOption", "CanvasAndInkPanel" },
                { "ComboBoxAutoSaveStrokesInterval", "CanvasAndInkPanel" },
                { "AutoSavedStrokesLocation", "SnapshotPanel" },
                { "ComboBoxAutoDelSavedFilesDaysThreshold", "SnapshotPanel" },
                { "ToggleSwitchAutoDelSavedFiles", "SnapshotPanel" },
                { "ComboBoxAutoBackupInterval", "AdvancedPanel" },
                { "ToggleSwitchIsQuadIR", "AdvancedPanel" },
                { "ToggleSwitchIsLogEnabled", "AdvancedPanel" },
                { "ToggleSwitchIsSaveLogByDate", "AdvancedPanel" },
                { "ToggleSwitchIsSecondConfimeWhenShutdownApp", "AdvancedPanel" },
                { "ToggleSwitchIsEnableFullScreenHelper", "AdvancedPanel" },
                { "ToggleSwitchIsEnableAvoidFullScreenHelper", "AdvancedPanel" },
                { "ToggleSwitchIsEnableEdgeGestureUtil", "AdvancedPanel" },
                { "ToggleSwitchIsEnableForceFullScreen", "AdvancedPanel" },
                { "ToggleSwitchIsEnableDPIChangeDetection", "AdvancedPanel" },
                { "ToggleSwitchIsEnableResolutionChangeDetection", "AdvancedPanel" },
                { "ToggleSwitchIsAutoBackupBeforeUpdate", "AdvancedPanel" },
                { "ToggleSwitchIsAutoBackupEnabled", "AdvancedPanel" },
                { "ToggleSwitchDisplayRandWindowNamesInputBtn", "LuckyRandomPanel" },
                { "ToggleSwitchShowRandomAndSingleDraw", "LuckyRandomPanel" },
                { "ToggleSwitchEnableQuickDraw", "LuckyRandomPanel" },
                { "ToggleSwitchExternalCaller", "LuckyRandomPanel" },
                { "ComboBoxExternalCallerType", "LuckyRandomPanel" },
                { "RandWindowOnceCloseLatencySlider", "LuckyRandomPanel" },
                { "RandWindowOnceMaxStudentsSlider", "LuckyRandomPanel" },
            };
            foreach (var kvp in controlToPanel)
            {
                if (controlName.Contains(kvp.Key) || kvp.Key.Contains(controlName))
                {
                    return kvp.Value;
                }
            }

            return null;
        }
        private static void SyncAllPanels(Window settingsWindow)
        {
            try
            {
                var panelProperties = settingsWindow.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.Name.EndsWith("Panel") && 
                               p.PropertyType.IsSubclassOf(typeof(System.Windows.Controls.UserControl)));

                foreach (var panelProp in panelProperties)
                {
                    try
                    {
                        var panel = panelProp.GetValue(settingsWindow) as System.Windows.Controls.UserControl;
                        if (panel != null)
                        {
                            var loadMethod = panel.GetType().GetMethod("LoadSettings", 
                                BindingFlags.Public | BindingFlags.Instance);
                            if (loadMethod != null)
                            {
                                loadMethod.Invoke(panel, null);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"同步面板 {panelProp.Name} 状态失�? {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"同步所有面板状态失�? {ex.Message}");
            }
        }
        public static void NotifyThemeUpdateIfNeeded(string controlName)
        {
            try
            {
                if (string.IsNullOrEmpty(controlName)) return;
                var themeRelatedControls = new[]
                {
                    "ComboBoxTheme",           
                    "ToggleSwitchEnableTrayIcon", 
                    "ComboBoxFloatingBarImg",   
                    "ComboBoxUnFoldBtnImg",     
                    "ComboBoxSplashScreenStyle", 
                    "ToggleSwitchEnableSplashScreen", 
                    "ViewboxFloatingBarScaleTransformValueSlider", 
                    "ViewboxFloatingBarOpacityValueSlider", 
                    "ViewboxFloatingBarOpacityInPPTValueSlider", 
                    "UnFoldBtnImg",             
                    "FloatingBarImg",            
                    "Theme"                      
                };
                bool isThemeRelated = false;
                string controlNameLower = controlName.ToLower();
                
                foreach (var themeControl in themeRelatedControls)
                {
                    string themeControlLower = themeControl.ToLower();
                    if (controlNameLower.Contains(themeControlLower) || 
                        themeControlLower.Contains(controlNameLower) ||
                        controlNameLower == themeControlLower)
                    {
                        isThemeRelated = true;
                        break;
                    }
                }

                if (isThemeRelated)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        NotifySettingsWindowThemeUpdate();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知主题更新失败: {ex.Message}");
            }
        }
        private static void NotifySettingsWindowThemeUpdate()
        {
            try
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType().Name == "SettingsWindow")
                    {
                        var method = window.GetType().GetMethod("ApplyThemeToAllPanels", 
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(window, null);
                        }
                        var applyThemeMethod = window.GetType().GetMethod("ApplyTheme", 
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                        if (applyThemeMethod != null)
                        {
                            applyThemeMethod.Invoke(window, null);
                        }
                        
                        break; 
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"通知设置窗口主题更新失败: {ex.Message}");
            }
        }
        public static void ForceUpdateAllPanelsTheme()
        {
            NotifySettingsWindowThemeUpdate();
        }
        public static void InvokeComboBoxSelectionChangedWithThemeCheck(string comboBoxName, object selectedItem)
        {
            InvokeComboBoxSelectionChanged(comboBoxName, selectedItem);
            NotifyThemeUpdateIfNeeded(comboBoxName);
        }
        public static void InvokeToggleSwitchToggledWithThemeCheck(string toggleSwitchName, bool isOn)
        {
            InvokeToggleSwitchToggled(toggleSwitchName, isOn);
            NotifyThemeUpdateIfNeeded(toggleSwitchName);
        }
        public static void EnableTouchSupportForControls(DependencyObject parent)
        {
            if (parent == null) return;

            for (int i = 0; i < Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = Media.VisualTreeHelper.GetChild(parent, i);
                if (child is Border border)
                {
                    if (border.Tag != null || border.Cursor == Cursors.Hand)
                    {
                        border.IsManipulationEnabled = true;
                        border.TouchDown += (s, e) =>
                        {
                            var touchPoint = e.GetTouchPoint(border);
                            var mouseEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                            {
                                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                                Source = border
                            };
                            border.RaiseEvent(mouseEvent);
                            border.CaptureTouch(e.TouchDevice);
                            e.Handled = true;
                        };
                        border.TouchUp += (s, e) =>
                        {
                            border.ReleaseTouchCapture(e.TouchDevice);
                            e.Handled = true;
                        };
                    }
                }
                else if (child is Button button)
                {
                    button.IsManipulationEnabled = true;
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.IsManipulationEnabled = true;
                }
                else if (child is Slider slider)
                {
                    slider.IsManipulationEnabled = true;
                }
                else if (child is TextBox textBox)
                {
                    textBox.IsManipulationEnabled = true;
                }
                else if (child is CheckBox checkBox)
                {
                    checkBox.IsManipulationEnabled = true;
                }
                else if (child is RadioButton radioButton)
                {
                    radioButton.IsManipulationEnabled = true;
                }
                EnableTouchSupportForControls(child);
            }
        }
    }
}

