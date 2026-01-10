using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// SnapshotPanel.xaml 的交互逻辑
    /// </summary>
    public partial class SnapshotPanel : System.Windows.Controls.UserControl
    {
        private bool _isLoaded = false;

        public SnapshotPanel()
        {
            InitializeComponent();
            Loaded += SnapshotPanel_Loaded;
        }

        private void SnapshotPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            // 添加触摸支持
            EnableTouchSupport();
            // 应用主题
            ApplyTheme();
            _isLoaded = true;
        }

        /// <summary>
        /// 为面板中的所有交互控件启用触摸支持
        /// </summary>
        private void EnableTouchSupport()
        {
            try
            {
                // 延迟执行，确保所有控件都已加载
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MainWindowSettingsHelper.EnableTouchSupportForControls(this);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SnapshotPanel 启用触摸支持时出错: {ex.Message}");
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

        /// <summary>
        /// 加载设置到UI
        /// </summary>
        public void LoadSettings()
        {
            if (MainWindow.Settings == null) return;

            _isLoaded = false;

            try
            {
                // 清屏时自动截图
                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveStrokesAtClear"), MainWindow.Settings.Automation.IsAutoSaveStrokesAtClear);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSaveScreenshotsInDateFolders"), MainWindow.Settings.Automation.IsSaveScreenshotsInDateFolders);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveStrokesAtScreenshot"), MainWindow.Settings.Automation.IsAutoSaveStrokesAtScreenshot);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoDelSavedFiles"), MainWindow.Settings.Automation.AutoDelSavedFiles);

                    // 自动截图最小墨迹量
                    if (SideControlMinimumAutomationSlider != null)
                    {
                        double minValue = MainWindow.Settings.Automation.MinimumAutomationStrokeNumber;
                        if (minValue == 0) minValue = 1.0;
                        SideControlMinimumAutomationSlider.Value = minValue;
                        if (SideControlMinimumAutomationText != null)
                        {
                            SideControlMinimumAutomationText.Text = minValue.ToString("F2");
                        }
                    }

                    // 保存路径
                    if (AutoSavedStrokesLocation != null)
                    {
                        AutoSavedStrokesLocation.Text = MainWindow.Settings.Automation.AutoSavedStrokesLocation;
                    }

                    // 自动删除保存时长
                    if (ComboBoxAutoDelSavedFilesDaysThreshold != null)
                    {
                        int days = MainWindow.Settings.Automation.AutoDelSavedFilesDaysThreshold;
                        int selectedIndex = 4; // 默认15天
                        switch (days)
                        {
                            case 1: selectedIndex = 0; break;
                            case 3: selectedIndex = 1; break;
                            case 5: selectedIndex = 2; break;
                            case 7: selectedIndex = 3; break;
                            case 15: selectedIndex = 4; break;
                            case 30: selectedIndex = 5; break;
                            case 60: selectedIndex = 6; break;
                            case 100: selectedIndex = 7; break;
                            case 365: selectedIndex = 8; break;
                        }
                        ComboBoxAutoDelSavedFilesDaysThreshold.SelectedIndex = selectedIndex;
                    }

                    if (AutoDelIntervalPanel != null)
                    {
                        AutoDelIntervalPanel.Visibility = MainWindow.Settings.Automation.AutoDelSavedFiles ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                // 自动幻灯片截屏
                if (MainWindow.Settings.PowerPointSettings != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveScreenShotInPowerPoint"), MainWindow.Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint);
                }

                // 墨迹设置
                if (MainWindow.Settings.Canvas != null)
                {
                    var canvas = MainWindow.Settings.Canvas;
                    
                    // 绘制圆时显示圆心位置
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowCircleCenter"), canvas.ShowCircleCenter);

                    // 使用WPF默认贝塞尔曲线平滑
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchFitToCurve"), canvas.FitToCurve && !canvas.UseAdvancedBezierSmoothing);

                    // 使用高级贝塞尔曲线平滑
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAdvancedBezierSmoothing"), canvas.UseAdvancedBezierSmoothing);

                    // 启用墨迹渐隐功能
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
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载截图设置时出错: {ex.Message}");
            }

            _isLoaded = true;
        }

        /// <summary>
        /// 查找ToggleSwitch控件
        /// </summary>
        private Border FindToggleSwitch(string name)
        {
            return this.FindDescendantByName(name) as Border;
        }

        /// <summary>
        /// 设置ToggleSwitch状态
        /// </summary>
        private void SetToggleSwitchState(Border toggleSwitch, bool isOn)
        {
            if (toggleSwitch == null) return;
            toggleSwitch.Background = isOn 
                ? new SolidColorBrush(Color.FromRgb(53, 132, 228)) 
                : ThemeHelper.GetButtonBackgroundBrush();
            var innerBorder = toggleSwitch.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// ToggleSwitch点击事件处理
        /// </summary>
        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = border.Background.ToString() == "#FF3584E4";
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            switch (tag)
            {
                case "AutoSaveStrokesAtClear":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveStrokesAtClear", newState);
                    break;

                case "SaveScreenshotsInDateFolders":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSaveScreenshotsInDateFolders", newState);
                    break;

                case "AutoSaveStrokesAtScreenshot":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveStrokesAtScreenshot", newState);
                    break;

                case "AutoSaveScreenShotInPowerPoint":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveScreenShotInPowerPoint", newState);
                    break;

                case "AutoDelSavedFiles":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoDelSavedFiles", newState);
                    // 更新UI状态
                    if (AutoDelIntervalPanel != null)
                    {
                        AutoDelIntervalPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "ShowCircleCenter":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowCircleCenter", newState);
                    break;

                case "FitToCurve":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFitToCurve", newState);
                    // 处理互斥逻辑
                    if (newState)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAdvancedBezierSmoothing", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAdvancedBezierSmoothing"), false);
                    }
                    break;

                case "AdvancedBezierSmoothing":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAdvancedBezierSmoothing", newState);
                    // 处理互斥逻辑
                    if (newState)
                    {
                        MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchFitToCurve", false);
                        SetToggleSwitchState(FindToggleSwitch("ToggleSwitchFitToCurve"), false);
                    }
                    break;

                case "EnableInkFade":
                    // 调用 MainWindow 中的方法
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableInkFade", newState);
                    // 更新UI状态
                    if (InkFadeTimePanel != null)
                    {
                        InkFadeTimePanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;
            }
        }

        /// <summary>
        /// Slider值变化事件处理
        /// </summary>
        private void SideControlMinimumAutomationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (SideControlMinimumAutomationSlider != null && SideControlMinimumAutomationText != null)
            {
                double value = SideControlMinimumAutomationSlider.Value;
                SideControlMinimumAutomationText.Text = value.ToString("F2");
                // 调用 MainWindow 中的方法
                MainWindowSettingsHelper.InvokeSliderValueChanged("SideControlMinimumAutomationSlider", value);
            }
        }

        /// <summary>
        /// 保存路径文本框变化事件处理
        /// </summary>
        private void AutoSavedStrokesLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (AutoSavedStrokesLocation != null)
            {
                // 调用 MainWindow 中的方法
                MainWindowSettingsHelper.InvokeTextBoxTextChanged("AutoSavedStrokesLocation", AutoSavedStrokesLocation.Text);
            }
        }

        /// <summary>
        /// 浏览按钮点击事件处理
        /// </summary>
        private void AutoSavedStrokesLocationButton_Click(object sender, RoutedEventArgs e)
        {
            // 调用 MainWindow 中的方法
            MainWindowSettingsHelper.InvokeMainWindowMethod("AutoSavedStrokesLocationButton_Click", sender, e);
            // 同步新面板中的文本框
            if (AutoSavedStrokesLocation != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
            {
                    var textBox = mainWindow.FindName("AutoSavedStrokesLocation") as System.Windows.Controls.TextBox;
                    if (textBox != null)
                {
                        AutoSavedStrokesLocation.Text = textBox.Text;
                    }
                }
            }
        }

        /// <summary>
        /// 设置保存到 D:\Ink Canvas 按钮点击事件处理
        /// </summary>
        private void SetAutoSavedStrokesLocationToDiskDButton_Click(object sender, RoutedEventArgs e)
        {
            // 调用 MainWindow 中的方法
            MainWindowSettingsHelper.InvokeMainWindowMethod("SetAutoSavedStrokesLocationToDiskDButton_Click", sender, e);
            // 同步新面板中的文本框
            if (AutoSavedStrokesLocation != null)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var textBox = mainWindow.FindName("AutoSavedStrokesLocation") as System.Windows.Controls.TextBox;
                    if (textBox != null)
                    {
                        AutoSavedStrokesLocation.Text = textBox.Text;
                    }
                }
            }
        }

        /// <summary>
        /// 设置保存到文档按钮点击事件处理
        /// </summary>
        private void SetAutoSavedStrokesLocationToDocumentFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (AutoSavedStrokesLocation != null)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                AutoSavedStrokesLocation.Text = Path.Combine(documentsPath, "Ink Canvas");
            }
        }

        /// <summary>
        /// 墨迹渐隐时间滑块值变化事件处理
        /// </summary>
        private void InkFadeTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (InkFadeTimeSlider != null && InkFadeTimeText != null)
            {
                int value = (int)InkFadeTimeSlider.Value;
                InkFadeTimeText.Text = $"{value}ms";
                // 调用 MainWindow 中的方法
                MainWindowSettingsHelper.InvokeSliderValueChanged("InkFadeTimeSlider", value);
            }
        }

        /// <summary>
        /// ComboBox选择变化事件处理
        /// </summary>
        private void ComboBoxAutoDelSavedFilesDaysThreshold_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (ComboBoxAutoDelSavedFilesDaysThreshold?.SelectedItem is ComboBoxItem selectedItem)
            {
                // 调用 MainWindow 中的方法
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var comboBox = mainWindow.FindName("ComboBoxAutoDelSavedFilesDaysThreshold") as System.Windows.Controls.ComboBox;
                    if (comboBox != null)
                    {
                        // 找到对应的选项并设置
                        string tag = selectedItem.Tag?.ToString();
                        if (!string.IsNullOrEmpty(tag) && tag.StartsWith("AutoDelSavedFilesDaysThreshold_"))
                        {
                            string daysStr = tag.Replace("AutoDelSavedFilesDaysThreshold_", "");
                            foreach (ComboBoxItem item in comboBox.Items)
                            {
                                if (item.Tag?.ToString() == tag || item.Content?.ToString() == daysStr + "天")
                                {
                                    comboBox.SelectedItem = item;
                                    MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxAutoDelSavedFilesDaysThreshold", item);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // 如果找不到控件，直接更新设置
                string tag = selectedItem.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag) && tag.StartsWith("AutoDelSavedFilesDaysThreshold_"))
                {
                    string daysStr = tag.Replace("AutoDelSavedFilesDaysThreshold_", "");
                    if (int.TryParse(daysStr, out int days))
                            {
                                MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Automation != null)
                        {
                            MainWindow.Settings.Automation.AutoDelSavedFilesDaysThreshold = days;
                                    }
                                }, "ComboBoxAutoDelSavedFilesDaysThreshold");
                            }
                        }
                    }
                }
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
                
                // 为所有 ComboBox 添加 DropDownOpened 事件处理，以便在下拉菜单打开时更新颜色
                UpdateComboBoxDropdownTheme(ComboBoxAutoDelSavedFilesDaysThreshold);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SnapshotPanel 应用主题时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 为 ComboBox 添加下拉菜单主题更新
        /// </summary>
        private void UpdateComboBoxDropdownTheme(System.Windows.Controls.ComboBox comboBox)
        {
            if (comboBox == null) return;
            
            // 移除旧的事件处理（如果存在）
            comboBox.DropDownOpened -= ComboBox_DropDownOpened;
            // 添加新的事件处理
            comboBox.DropDownOpened += ComboBox_DropDownOpened;
        }

        /// <summary>
        /// ComboBox 下拉菜单打开事件处理
        /// </summary>
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox comboBox)
            {
                // 延迟更新，确保 Popup 已经完全创建
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ThemeHelper.UpdateComboBoxDropdownColors(comboBox);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}

