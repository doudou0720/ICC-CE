using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Application = System.Windows.Application;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// AutomationPanel.xaml 的交互逻辑
    /// </summary>
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
            // 添加触摸支持
            EnableTouchSupport();
            // 应用主题
            ApplyTheme();
            _isLoaded = true;
        }

        /// <summary>
        /// 加载设置
        /// </summary>
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

                // 设置所有 ToggleSwitch 的状态
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
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 加载设置时出错: {ex.Message}");
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
        /// 设置 ToggleSwitch 状态
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
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        /// <summary>
        /// ToggleSwitch 点击事件处理
        /// </summary>
        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            // 标记事件已处理，防止事件冒泡
            e.Handled = true;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = border.Background.ToString() == "#FF3584E4";
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            // 通过 MainWindowSettingsHelper 调用 MainWindow 中的方法
            string toggleSwitchName = border.Name;
            MainWindowSettingsHelper.InvokeToggleSwitchToggled(toggleSwitchName, newState);
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
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 启用触摸支持时出错: {ex.Message}");
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
        /// 应用主题
        /// </summary>
        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 应用主题时出错: {ex.Message}");
            }
        }
    }
}

