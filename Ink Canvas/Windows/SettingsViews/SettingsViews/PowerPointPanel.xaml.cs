using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class PowerPointPanel : UserControl
    {
        private bool _isLoaded = false;

        public PowerPointPanel()
        {
            InitializeComponent();
            Loaded += PowerPointPanel_Loaded;
        }

        private void PowerPointPanel_Loaded(object sender, RoutedEventArgs e)
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
                System.Diagnostics.Debug.WriteLine($"PowerPointPanel 启用触摸支持时出�? {ex.Message}");
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
            if (MainWindow.Settings == null || MainWindow.Settings.PowerPointSettings == null) return;

            _isLoaded = false;

            try
            {
                var pptSettings = MainWindow.Settings.PowerPointSettings;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSupportPowerPoint"), pptSettings.PowerPointSupport);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchPowerPointEnhancement"), pptSettings.EnablePowerPointEnhancement);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSupportWPS"), pptSettings.IsSupportWPS);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableWppProcessKill"), pptSettings.EnableWppProcessKill);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowPPTButton"), pptSettings.ShowPPTButton);
                if (PPTButtonSettingsPanel != null)
                {
                    PPTButtonSettingsPanel.Visibility = pptSettings.ShowPPTButton ? Visibility.Visible : Visibility.Collapsed;
                }

                var dops = pptSettings.PPTButtonsDisplayOption.ToString();
                var dopsc = dops.ToCharArray();
                if (dopsc.Length >= 4)
                {
                    if (CheckboxEnableLBPPTButton != null) CheckboxEnableLBPPTButton.IsChecked = dopsc[0] == '2';
                    if (CheckboxEnableRBPPTButton != null) CheckboxEnableRBPPTButton.IsChecked = dopsc[1] == '2';
                    if (CheckboxEnableLSPPTButton != null) CheckboxEnableLSPPTButton.IsChecked = dopsc[2] == '2';
                    if (CheckboxEnableRSPPTButton != null) CheckboxEnableRSPPTButton.IsChecked = dopsc[3] == '2';
                }

                if (PPTButtonLeftPositionValueSlider != null)
                {
                    PPTButtonLeftPositionValueSlider.Value = pptSettings.PPTLSButtonPosition;
                    if (PPTButtonLeftPositionValueText != null)
                    {
                        PPTButtonLeftPositionValueText.Text = pptSettings.PPTLSButtonPosition.ToString();
                    }
                }
                if (PPTButtonRightPositionValueSlider != null)
                {
                    PPTButtonRightPositionValueSlider.Value = pptSettings.PPTRSButtonPosition;
                    if (PPTButtonRightPositionValueText != null)
                    {
                        PPTButtonRightPositionValueText.Text = pptSettings.PPTRSButtonPosition.ToString();
                    }
                }
                if (PPTButtonLBPositionValueSlider != null)
                {
                    PPTButtonLBPositionValueSlider.Value = pptSettings.PPTLBButtonPosition;
                    if (PPTButtonLBPositionValueText != null)
                    {
                        PPTButtonLBPositionValueText.Text = pptSettings.PPTLBButtonPosition.ToString();
                    }
                }
                if (PPTButtonRBPositionValueSlider != null)
                {
                    PPTButtonRBPositionValueSlider.Value = pptSettings.PPTRBButtonPosition;
                    if (PPTButtonRBPositionValueText != null)
                    {
                        PPTButtonRBPositionValueText.Text = pptSettings.PPTRBButtonPosition.ToString();
                    }
                }

                var sops = pptSettings.PPTSButtonsOption.ToString();
                var sopsc = sops.ToCharArray();
                if (sopsc.Length >= 3)
                {
                    if (CheckboxSPPTDisplayPage != null) CheckboxSPPTDisplayPage.IsChecked = sopsc[0] == '2';
                    if (CheckboxSPPTHalfOpacity != null) CheckboxSPPTHalfOpacity.IsChecked = sopsc[1] == '2';
                    if (CheckboxSPPTBlackBackground != null) CheckboxSPPTBlackBackground.IsChecked = sopsc[2] == '2';
                }

                var bops = pptSettings.PPTBButtonsOption.ToString();
                var bopsc = bops.ToCharArray();
                if (bopsc.Length >= 3)
                {
                    if (CheckboxBPPTDisplayPage != null) CheckboxBPPTDisplayPage.IsChecked = bopsc[0] == '2';
                    if (CheckboxBPPTHalfOpacity != null) CheckboxBPPTHalfOpacity.IsChecked = bopsc[1] == '2';
                    if (CheckboxBPPTBlackBackground != null) CheckboxBPPTBlackBackground.IsChecked = bopsc[2] == '2';
                }

                if (PPTLSButtonOpacityValueSlider != null)
                {
                    PPTLSButtonOpacityValueSlider.Value = pptSettings.PPTLSButtonOpacity > 0 ? pptSettings.PPTLSButtonOpacity : 0.5;
                    if (PPTLSButtonOpacityValueText != null)
                    {
                        PPTLSButtonOpacityValueText.Text = PPTLSButtonOpacityValueSlider.Value.ToString("F1");
                    }
                }
                if (PPTRSButtonOpacityValueSlider != null)
                {
                    PPTRSButtonOpacityValueSlider.Value = pptSettings.PPTRSButtonOpacity > 0 ? pptSettings.PPTRSButtonOpacity : 0.5;
                    if (PPTRSButtonOpacityValueText != null)
                    {
                        PPTRSButtonOpacityValueText.Text = PPTRSButtonOpacityValueSlider.Value.ToString("F1");
                    }
                }
                if (PPTLBButtonOpacityValueSlider != null)
                {
                    PPTLBButtonOpacityValueSlider.Value = pptSettings.PPTLBButtonOpacity > 0 ? pptSettings.PPTLBButtonOpacity : 0.5;
                    if (PPTLBButtonOpacityValueText != null)
                    {
                        PPTLBButtonOpacityValueText.Text = PPTLBButtonOpacityValueSlider.Value.ToString("F1");
                    }
                }
                if (PPTRBButtonOpacityValueSlider != null)
                {
                    PPTRBButtonOpacityValueSlider.Value = pptSettings.PPTRBButtonOpacity > 0 ? pptSettings.PPTRBButtonOpacity : 0.5;
                    if (PPTRBButtonOpacityValueText != null)
                    {
                        PPTRBButtonOpacityValueText.Text = PPTRBButtonOpacityValueSlider.Value.ToString("F1");
                    }
                }

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePPTButtonPageClickable"), pptSettings.EnablePPTButtonPageClickable);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePPTButtonLongPressPageTurn"), pptSettings.EnablePPTButtonLongPressPageTurn);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchSkipAnimationsWhenGoNext"), pptSettings.SkipAnimationsWhenGoNext);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowCanvasAtNewSlideShow"), pptSettings.IsShowCanvasAtNewSlideShow);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableTwoFingerGestureInPresentationMode"), pptSettings.IsEnableTwoFingerGestureInPresentationMode);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableFingerGestureSlideShowControl"), pptSettings.IsEnableFingerGestureSlideShowControl);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchShowGestureButtonInSlideShow"), pptSettings.ShowGestureButtonInSlideShow);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnablePPTTimeCapsule"), pptSettings.EnablePPTTimeCapsule);

                SetOptionButtonState("PPTTimeCapsulePosition", pptSettings.PPTTimeCapsulePosition);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchNotifyPreviousPage"), pptSettings.IsNotifyPreviousPage);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAlwaysGoToFirstPageOnReenter"), pptSettings.IsAlwaysGoToFirstPageOnReenter);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchNotifyHiddenPage"), pptSettings.IsNotifyHiddenPage);

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchNotifyAutoPlayPresentation"), pptSettings.IsNotifyAutoPlayPresentation);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdatePPTBtnPreview();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载PowerPoint设置时出�? {ex.Message}");
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
                { "PPTTimeCapsulePosition", new[] { "TopLeft", "TopRight", "TopCenter" } }
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

            var pptSettings = MainWindow.Settings.PowerPointSettings;
            if (pptSettings == null) return;

            switch (tag)
            {
                case "SupportPowerPoint":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSupportPowerPoint", newState);
                    break;

                case "PowerPointEnhancement":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchPowerPointEnhancement", newState);
                    break;

                case "SupportWPS":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSupportWPS", newState);
                    break;

                case "EnableWppProcessKill":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableWppProcessKill", newState);
                    break;

                case "ShowPPTButton":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowPPTButton", newState);
                    if (PPTButtonSettingsPanel != null)
                    {
                        PPTButtonSettingsPanel.Visibility = newState ? Visibility.Visible : Visibility.Collapsed;
                    }
                    UpdatePPTBtnPreview();
                    break;

                case "EnablePPTButtonPageClickable":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePPTButtonPageClickable", newState);
                    break;

                case "EnablePPTButtonLongPressPageTurn":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePPTButtonLongPressPageTurn", newState);
                    break;

                case "SkipAnimationsWhenGoNext":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchSkipAnimationsWhenGoNext", newState);
                    break;

                case "ShowCanvasAtNewSlideShow":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowCanvasAtNewSlideShow", newState);
                    break;

                case "EnableTwoFingerGestureInPresentationMode":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableTwoFingerGestureInPresentationMode", newState);
                    break;

                case "EnableFingerGestureSlideShowControl":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnableFingerGestureSlideShowControl", newState);
                    break;

                case "ShowGestureButtonInSlideShow":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchShowGestureButtonInSlideShow", newState);
                    break;

                case "EnablePPTTimeCapsule":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchEnablePPTTimeCapsule", newState);
                    break;

                case "NotifyPreviousPage":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchNotifyPreviousPage", newState);
                    break;

                case "AlwaysGoToFirstPageOnReenter":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchAlwaysGoToFirstPageOnReenter", newState);
                    break;

                case "NotifyHiddenPage":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchNotifyHiddenPage", newState);
                    break;

                case "NotifyAutoPlayPresentation":
                    MainWindowSettingsHelper.InvokeToggleSwitchToggled("ToggleSwitchNotifyAutoPlayPresentation", newState);
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

            var pptSettings = MainWindow.Settings.PowerPointSettings;
            if (pptSettings == null) return;

            switch (group)
            {
                case "PPTTimeCapsulePosition":
                    int position;
                    switch (value)
                    {
                        case "TopLeft":
                            position = 0;
                            break;
                        case "TopRight":
                            position = 1;
                            break;
                        case "TopCenter":
                            position = 2;
                            break;
                        default:
                            position = 1;
                            break;
                    }
                    MainWindowSettingsHelper.UpdateSettingDirectly(() =>
                    {
                    pptSettings.PPTTimeCapsulePosition = position;
                    }, "PPTTimeCapsulePosition");
                    break;
            }
            
            if (group.Contains("PPT") || group.Contains("Button"))
            {
                UpdatePPTBtnPreview();
            }
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var checkBox = sender as CheckBox;
            if (checkBox == null) return;

            string name = checkBox.Name;
            var pptSettings = MainWindow.Settings.PowerPointSettings;
            if (pptSettings == null) return;

            MainWindowSettingsHelper.InvokeCheckBoxCheckedChanged(name, checkBox.IsChecked ?? false);
            
            if (name.Contains("SPPT") || name.Contains("BPPT") || name == "CheckboxSPPTDisplayPage" || 
                name == "CheckboxSPPTHalfOpacity" || name == "CheckboxSPPTBlackBackground" ||
                name == "CheckboxBPPTDisplayPage" || name == "CheckboxBPPTHalfOpacity" || name == "CheckboxBPPTBlackBackground")
            {
                UpdatePPTBtnPreview();
            }
        }


        private void PPTButtonPositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;

            var slider = sender as Slider;
            if (slider == null) return;

            string name = slider.Name;
            double value = slider.Value;

            switch (name)
            {
                case "PPTButtonLeftPositionValueSlider":
                    if (PPTButtonLeftPositionValueText != null)
                    {
                        PPTButtonLeftPositionValueText.Text = ((int)value).ToString();
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTButtonLeftPositionValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTButtonRightPositionValueSlider":
                    if (PPTButtonRightPositionValueText != null)
                    {
                        PPTButtonRightPositionValueText.Text = ((int)value).ToString();
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTButtonRightPositionValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTButtonLBPositionValueSlider":
                    if (PPTButtonLBPositionValueText != null)
                    {
                        PPTButtonLBPositionValueText.Text = ((int)value).ToString();
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTButtonLBPositionValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTButtonRBPositionValueSlider":
                    if (PPTButtonRBPositionValueText != null)
                    {
                        PPTButtonRBPositionValueText.Text = ((int)value).ToString();
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTButtonRBPositionValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;
            }
        }

        private void PPTButtonOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;

            var slider = sender as Slider;
            if (slider == null) return;

            string name = slider.Name;
            double value = slider.Value;

            switch (name)
            {
                case "PPTLSButtonOpacityValueSlider":
                    if (PPTLSButtonOpacityValueText != null)
                    {
                        PPTLSButtonOpacityValueText.Text = value.ToString("F1");
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTLSButtonOpacityValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTRSButtonOpacityValueSlider":
                    if (PPTRSButtonOpacityValueText != null)
                    {
                        PPTRSButtonOpacityValueText.Text = value.ToString("F1");
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTRSButtonOpacityValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTLBButtonOpacityValueSlider":
                    if (PPTLBButtonOpacityValueText != null)
                    {
                        PPTLBButtonOpacityValueText.Text = value.ToString("F1");
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTLBButtonOpacityValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;

                case "PPTRBButtonOpacityValueSlider":
                    if (PPTRBButtonOpacityValueText != null)
                    {
                        PPTRBButtonOpacityValueText.Text = value.ToString("F1");
                    }
                    MainWindowSettingsHelper.InvokeSliderValueChanged("PPTRBButtonOpacityValueSlider", value);
                    UpdatePPTBtnPreview();
                    break;
            }
        }
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                if (MainWindow.Settings?.PowerPointSettings != null)
                {
                    SetOptionButtonState("PPTTimeCapsulePosition", MainWindow.Settings.PowerPointSettings.PPTTimeCapsulePosition);
                }
                
                UpdatePPTBtnPreview();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PowerPointPanel 应用主题时出�? {ex.Message}");
            }
        }


        private void UpdatePPTBtnPreview()
        {
            try
            {
                if (MainWindow.Settings?.PowerPointSettings == null) return;

                var pptSettings = MainWindow.Settings.PowerPointSettings;

                var bopt = pptSettings.PPTBButtonsOption.ToString();
                char[] boptc = bopt.ToCharArray();
                
                if (PPTBtnPreviewLB != null)
                {
                    PPTBtnPreviewLB.Opacity = pptSettings.PPTLBButtonOpacity;
                }
                if (PPTBtnPreviewRB != null)
                {
                    PPTBtnPreviewRB.Opacity = pptSettings.PPTRBButtonOpacity;
                }

                if (boptc.Length >= 3 && boptc[2] == '2')
                {
                    if (PPTBtnPreviewLB != null)
                    {
                        PPTBtnPreviewLB.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/bottombar-white.png"));
                    }
                    if (PPTBtnPreviewRB != null)
                    {
                        PPTBtnPreviewRB.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/bottombar-white.png"));
                    }
                }
                else
                {
                    if (PPTBtnPreviewLB != null)
                    {
                        PPTBtnPreviewLB.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/bottombar-white.png"));
                    }
                    if (PPTBtnPreviewRB != null)
                    {
                        PPTBtnPreviewRB.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/bottombar-white.png"));
                    }
                }

                var sopt = pptSettings.PPTSButtonsOption.ToString();
                char[] soptc = sopt.ToCharArray();
                
                if (PPTBtnPreviewLS != null)
                {
                    PPTBtnPreviewLS.Opacity = pptSettings.PPTLSButtonOpacity;
                }
                if (PPTBtnPreviewRS != null)
                {
                    PPTBtnPreviewRS.Opacity = pptSettings.PPTRSButtonOpacity;
                }

                if (soptc.Length >= 3 && soptc[2] == '2')
                {
                    if (PPTBtnPreviewLS != null)
                    {
                        PPTBtnPreviewLS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
                    }
                    if (PPTBtnPreviewRS != null)
                    {
                        PPTBtnPreviewRS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
                    }
                }
                else
                {
                    if (PPTBtnPreviewLS != null)
                    {
                        PPTBtnPreviewLS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
                    }
                    if (PPTBtnPreviewRS != null)
                    {
                        PPTBtnPreviewRS.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/PresentationExample/sidebar-white.png"));
                    }
                }

                var dopt = pptSettings.PPTButtonsDisplayOption.ToString();
                char[] doptc = dopt.ToCharArray();

                if (pptSettings.ShowPPTButton)
                {
                    if (PPTBtnPreviewLB != null)
                    {
                        PPTBtnPreviewLB.Visibility = doptc.Length > 0 && doptc[0] == '2' ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (PPTBtnPreviewRB != null)
                    {
                        PPTBtnPreviewRB.Visibility = doptc.Length > 1 && doptc[1] == '2' ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (PPTBtnPreviewLS != null)
                    {
                        PPTBtnPreviewLS.Visibility = doptc.Length > 2 && doptc[2] == '2' ? Visibility.Visible : Visibility.Collapsed;
                    }
                    if (PPTBtnPreviewRS != null)
                    {
                        PPTBtnPreviewRS.Visibility = doptc.Length > 3 && doptc[3] == '2' ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                else
                {
                    if (PPTBtnPreviewLB != null) PPTBtnPreviewLB.Visibility = Visibility.Collapsed;
                    if (PPTBtnPreviewRB != null) PPTBtnPreviewRB.Visibility = Visibility.Collapsed;
                    if (PPTBtnPreviewLS != null) PPTBtnPreviewLS.Visibility = Visibility.Collapsed;
                    if (PPTBtnPreviewRS != null) PPTBtnPreviewRS.Visibility = Visibility.Collapsed;
                }

                var actualScreenWidth = SystemParameters.PrimaryScreenWidth;
                var actualScreenHeight = SystemParameters.PrimaryScreenHeight;

                const double previewWidth = 324.0;
                const double previewHeight = 182.0;

                double scaleX = previewWidth / actualScreenWidth;
                double scaleY = previewHeight / actualScreenHeight;

                double rsPosition = pptSettings.PPTRSButtonPosition;
                double lsPosition = pptSettings.PPTLSButtonPosition;
                double lbPosition = pptSettings.PPTLBButtonPosition;
                double rbPosition = pptSettings.PPTRBButtonPosition;

                bool showSidePageButton = sopt.Length >= 1 && sopt[0] == '2';
                bool showBottomPageButton = bopt.Length >= 1 && bopt[0] == '2';

                const double pageButtonWidth = 50.0;
                const double pageButtonHeight = 50.0;

                double sideOffsetY = showSidePageButton ? pageButtonHeight * scaleY : 0;
                if (PPTBtnPreviewRSTransform != null)
                {
                    PPTBtnPreviewRSTransform.Y = -(rsPosition * scaleY) - sideOffsetY;
                }
                if (PPTBtnPreviewLSTransform != null)
                {
                    PPTBtnPreviewLSTransform.Y = -(lsPosition * scaleY) - sideOffsetY;
                }

                const double bottomMarginOffset = 6.0;
                double scaledMarginOffset = bottomMarginOffset * scaleX;

                double bottomOffsetX = showBottomPageButton ? pageButtonWidth * scaleX : 0;
                if (PPTBtnPreviewLBTransform != null)
                {
                    PPTBtnPreviewLBTransform.X = scaledMarginOffset + (lbPosition * scaleX) + bottomOffsetX;
                }
                if (PPTBtnPreviewRBTransform != null)
                {
                    PPTBtnPreviewRBTransform.X = -(scaledMarginOffset + (rbPosition * scaleX) + bottomOffsetX);
                }

                var dpiScaleX = 1.0;
                var dpiScaleY = 1.0;
                try
                {
                    var source = PresentationSource.FromVisual(this);
                    if (source?.CompositionTarget != null)
                    {
                        var transform = source.CompositionTarget.TransformToDevice;
                        dpiScaleX = transform.M11;
                        dpiScaleY = transform.M22;
                    }
                }
                catch
                {
                    dpiScaleX = 1.0;
                    dpiScaleY = 1.0;
                }

                const double baseToolbarHeight = 24.0;
                double actualToolbarHeight = baseToolbarHeight * dpiScaleY;
                double scaledToolbarHeight = actualToolbarHeight * scaleY;
                double scaledToolbarWidth = previewWidth;

                var toolbar = this.FindDescendantByName("PPTBtnPreviewToolbar") as System.Windows.Controls.Image;
                if (toolbar != null)
                {
                    toolbar.Height = scaledToolbarHeight;
                    toolbar.Width = scaledToolbarWidth;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新PPT按钮预览时出�? {ex.Message}");
            }
        }
    }
}
