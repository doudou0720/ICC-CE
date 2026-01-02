using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class AutomationPanel : UserControl
    {
        private bool _isLoaded = false;

        public AutomationPanel()
        {
            InitializeComponent();
            Loaded += AutomationPanel_Loaded;
        }

        private void AutomationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            EnableTouchSupport();
            ApplyTheme();
            _isLoaded = true;
        }

        public void LoadSettings()
        {
            if (MainWindow.Settings == null || MainWindow.Settings.Automation == null)
            {
                _isLoaded = true;
                return;
            }

            _isLoaded = false;

            try
            {
                var automation = MainWindow.Settings.Automation;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiNote"), automation.IsAutoFoldInEasiNote);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiCamera"), automation.IsAutoFoldInEasiCamera);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInHiteTouchPro"), automation.IsAutoFoldInHiteTouchPro);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiNote3"), automation.IsAutoFoldInEasiNote3);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiNote3C"), automation.IsAutoFoldInEasiNote3C);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiNote5C"), automation.IsAutoFoldInEasiNote5C);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInSeewoPincoTeacher"), automation.IsAutoFoldInSeewoPincoTeacher);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInHiteCamera"), automation.IsAutoFoldInHiteCamera);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInHiteLightBoard"), automation.IsAutoFoldInHiteLightBoard);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInWxBoardMain"), automation.IsAutoFoldInWxBoardMain);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInMSWhiteboard"), automation.IsAutoFoldInMSWhiteboard);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInAdmoxWhiteboard"), automation.IsAutoFoldInAdmoxWhiteboard);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInAdmoxBooth"), automation.IsAutoFoldInAdmoxBooth);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInQPoint"), automation.IsAutoFoldInQPoint);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInYiYunVisualPresenter"), automation.IsAutoFoldInYiYunVisualPresenter);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInMaxHubWhiteboard"), automation.IsAutoFoldInMaxHubWhiteboard);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInPPTSlideShow"), automation.IsAutoFoldInPPTSlideShow);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno"), automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoFoldInOldZyBoard"), automation.IsAutoFoldInOldZyBoard);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchKeepFoldAfterSoftwareExit"), automation.KeepFoldAfterSoftwareExit);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillPptService"), automation.IsAutoKillPptService);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillEasiNote"), automation.IsAutoKillEasiNote);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillHiteAnnotation"), automation.IsAutoKillHiteAnnotation);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoEnterAnnotationAfterKillHite"), automation.IsAutoEnterAnnotationAfterKillHite);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillVComYouJiao"), automation.IsAutoKillVComYouJiao);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillSeewoLauncher2DesktopAnnotation"), automation.IsAutoKillSeewoLauncher2DesktopAnnotation);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillInkCanvas"), automation.IsAutoKillInkCanvas);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillICA"), automation.IsAutoKillICA);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchAutoKillIDT"), automation.IsAutoKillIDT);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 加载设置时出�? {ex.Message}");
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

            e.Handled = true;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = ThemeHelper.IsToggleSwitchOn(border.Background);
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string toggleSwitchName = border.Name;
            MainWindowSettingsHelper.InvokeToggleSwitchToggled(toggleSwitchName, newState);
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
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 启用触摸支持时出�? {ex.Message}");
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
        
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 应用主题时出�? {ex.Message}");
            }
        }
    }
}

