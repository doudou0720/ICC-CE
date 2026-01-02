using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
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
                System.Diagnostics.Debug.WriteLine($"SnapshotPanel 启用触摸支持时出�? {ex.Message}");
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
                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveStrokesAtClear"), MainWindow.Settings.Automation.IsAutoSaveStrokesAtClear);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSaveScreenshotsInDateFolders"), MainWindow.Settings.Automation.IsSaveScreenshotsInDateFolders);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveStrokesAtScreenshot"), MainWindow.Settings.Automation.IsAutoSaveStrokesAtScreenshot);
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoDelSavedFiles"), MainWindow.Settings.Automation.AutoDelSavedFiles);

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

                    if (AutoSavedStrokesLocation != null)
                    {
                        AutoSavedStrokesLocation.Text = MainWindow.Settings.Automation.AutoSavedStrokesLocation;
                    }

                    if (ComboBoxAutoDelSavedFilesDaysThreshold != null)
                    {
                        int days = MainWindow.Settings.Automation.AutoDelSavedFilesDaysThreshold;
                        int selectedIndex = 4;
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

                if (MainWindow.Settings.PowerPointSettings != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveScreenShotInPowerPoint"), MainWindow.Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint);
                }

                if (MainWindow.Settings.Automation != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableAutoSaveStrokes"), MainWindow.Settings.Automation.IsEnableAutoSaveStrokes);
                    if (AutoSaveIntervalPanel != null)
                    {
                        AutoSaveIntervalPanel.Visibility = MainWindow.Settings.Automation.IsEnableAutoSaveStrokes ? Visibility.Visible : Visibility.Collapsed;
                    }
                    LoadAutoSaveIntervalSettings();

                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSaveFullPageStrokes"), MainWindow.Settings.Automation.IsSaveFullPageStrokes);

                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSaveStrokesAsXML"), MainWindow.Settings.Automation.IsSaveStrokesAsXML);
                }

                if (MainWindow.Settings.PowerPointSettings != null)
                {
                    SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoSaveStrokesInPowerPoint"), MainWindow.Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载截图设置时出�? {ex.Message}");
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
                innerBorder.HorizontalAlignment = isOn ? System.Windows.HorizontalAlignment.Right : System.Windows.HorizontalAlignment.Left;
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
                case "AutoSaveStrokesAtClear":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveStrokesAtClear", newState);
                    break;

                case "SaveScreenshotsInDateFolders":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSaveScreenshotsInDateFolders", newState);
                    break;

                case "AutoSaveStrokesAtScreenshot":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveStrokesAtScreenshot", newState);
                    break;

                case "AutoSaveScreenShotInPowerPoint":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveScreenShotInPowerPoint", newState);
                    break;

                case "AutoDelSavedFiles":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoDelSavedFiles", newState);
                    if (AutoDelIntervalPanel != null)
                    {
                        AutoDelIntervalPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "EnableAutoSaveStrokes":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableAutoSaveStrokes", newState);
                    if (AutoSaveIntervalPanel != null)
                    {
                        AutoSaveIntervalPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    break;

                case "SaveFullPageStrokes":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSaveFullPageStrokes", newState);
                    break;

                case "SaveStrokesAsXML":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSaveStrokesAsXML", newState);
                    break;

                case "AutoSaveStrokesInPowerPoint":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAutoSaveStrokesInPowerPoint", newState);
                    break;
            }
        }

        private void SideControlMinimumAutomationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;
            if (SideControlMinimumAutomationSlider != null && SideControlMinimumAutomationText != null)
            {
                double value = SideControlMinimumAutomationSlider.Value;
                SideControlMinimumAutomationText.Text = value.ToString("F2");
                MainWindowSettingsHelper.InvokeSliderValueChanged("SideControlMinimumAutomationSlider", value);
            }
        }

        private void AutoSavedStrokesLocation_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (AutoSavedStrokesLocation != null)
            {
                MainWindowSettingsHelper.InvokeTextBoxTextChanged("AutoSavedStrokesLocation", AutoSavedStrokesLocation.Text);
            }
        }

        private void AutoSavedStrokesLocationButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("AutoSavedStrokesLocationButton_Click", sender, e);
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

        private void SetAutoSavedStrokesLocationToDiskDButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindowSettingsHelper.InvokeMainWindowMethod("SetAutoSavedStrokesLocationToDiskDButton_Click", sender, e);
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

        private void SetAutoSavedStrokesLocationToDocumentFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (AutoSavedStrokesLocation != null)
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                AutoSavedStrokesLocation.Text = Path.Combine(documentsPath, "Ink Canvas");
            }
        }

        private void ComboBoxAutoDelSavedFilesDaysThreshold_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (ComboBoxAutoDelSavedFilesDaysThreshold?.SelectedItem is ComboBoxItem selectedItem)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var comboBox = mainWindow.FindName("ComboBoxAutoDelSavedFilesDaysThreshold") as System.Windows.Controls.ComboBox;
                    if (comboBox != null)
                    {
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
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                LoadAutoSaveIntervalSettings();
                
                UpdateComboBoxDropdownTheme(ComboBoxAutoDelSavedFilesDaysThreshold);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SnapshotPanel 应用主题时出�? {ex.Message}");
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

        private void LoadAutoSaveIntervalSettings()
        {
            if (MainWindow.Settings?.Automation == null) return;

            int interval = MainWindow.Settings.Automation.AutoSaveStrokesIntervalMinutes;
            var intervalButtons = new[] {
                AutoSaveStrokesInterval1MinBorder,
                AutoSaveStrokesInterval3MinBorder,
                AutoSaveStrokesInterval5MinBorder,
                AutoSaveStrokesInterval10MinBorder,
                AutoSaveStrokesInterval15MinBorder,
                AutoSaveStrokesInterval30MinBorder,
                AutoSaveStrokesInterval60MinBorder
            };

            foreach (var button in intervalButtons)
            {
                if (button != null)
                {
                    ThemeHelper.SetOptionButtonSelectedState(button, false);
                }
            }

            System.Windows.Controls.Border selectedButton = null;
            switch (interval)
            {
                case 1: selectedButton = AutoSaveStrokesInterval1MinBorder; break;
                case 3: selectedButton = AutoSaveStrokesInterval3MinBorder; break;
                case 5: selectedButton = AutoSaveStrokesInterval5MinBorder; break;
                case 10: selectedButton = AutoSaveStrokesInterval10MinBorder; break;
                case 15: selectedButton = AutoSaveStrokesInterval15MinBorder; break;
                case 30: selectedButton = AutoSaveStrokesInterval30MinBorder; break;
                case 60: selectedButton = AutoSaveStrokesInterval60MinBorder; break;
                default: selectedButton = AutoSaveStrokesInterval5MinBorder; break;
            }

            if (selectedButton != null)
            {
                ThemeHelper.SetOptionButtonSelectedState(selectedButton, true);
            }
        }

        private void AutoSaveIntervalButton_Click(object sender, MouseButtonEventArgs e)
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

            int interval = int.Parse(value);
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var comboBox = mainWindow.FindName("ComboBoxAutoSaveStrokesInterval") as System.Windows.Controls.ComboBox;
                if (comboBox != null)
                {
                    foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
                    {
                        if (item.Tag != null && int.TryParse(item.Tag.ToString(), out int tagValue) && tagValue == interval)
                        {
                            comboBox.SelectedItem = item;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxAutoSaveStrokesInterval", item);
                            break;
                        }
                        else if (item.Content != null && item.Content.ToString().Contains(interval.ToString()))
                        {
                            comboBox.SelectedItem = item;
                            MainWindowSettingsHelper.InvokeComboBoxSelectionChanged("ComboBoxAutoSaveStrokesInterval", item);
                            break;
                        }
                    }
                }
                else
                {
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                        if (MainWindow.Settings.Automation != null)
                        {
                            MainWindow.Settings.Automation.AutoSaveStrokesIntervalMinutes = interval;
                        }
                    }, "ComboBoxAutoSaveStrokesInterval");
                }
            }
        }
    }
}

