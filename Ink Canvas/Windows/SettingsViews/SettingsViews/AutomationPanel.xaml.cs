using System;
using System.Windows;
using System.Windows.Controls;
using Ink_Canvas.Windows.SettingsViews;

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
        private void LoadSettings()
        {
            try
            {
                if (MainWindow.Settings == null || MainWindow.Settings.Automation == null) return;

                _isLoaded = false;

                var automation = MainWindow.Settings.Automation;

                // 设置所有 ToggleSwitch 的状态
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiNote", automation.IsAutoFoldInEasiNote);
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiCamera", automation.IsAutoFoldInEasiCamera);
                SetToggleSwitchState("ToggleSwitchAutoFoldInHiteTouchPro", automation.IsAutoFoldInHiteTouchPro);
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiNote3", automation.IsAutoFoldInEasiNote3);
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiNote3C", automation.IsAutoFoldInEasiNote3C);
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiNote5C", automation.IsAutoFoldInEasiNote5C);
                SetToggleSwitchState("ToggleSwitchAutoFoldInSeewoPincoTeacher", automation.IsAutoFoldInSeewoPincoTeacher);
                SetToggleSwitchState("ToggleSwitchAutoFoldInHiteCamera", automation.IsAutoFoldInHiteCamera);
                SetToggleSwitchState("ToggleSwitchAutoFoldInHiteLightBoard", automation.IsAutoFoldInHiteLightBoard);
                SetToggleSwitchState("ToggleSwitchAutoFoldInWxBoardMain", automation.IsAutoFoldInWxBoardMain);
                SetToggleSwitchState("ToggleSwitchAutoFoldInMSWhiteboard", automation.IsAutoFoldInMSWhiteboard);
                SetToggleSwitchState("ToggleSwitchAutoFoldInAdmoxWhiteboard", automation.IsAutoFoldInAdmoxWhiteboard);
                SetToggleSwitchState("ToggleSwitchAutoFoldInAdmoxBooth", automation.IsAutoFoldInAdmoxBooth);
                SetToggleSwitchState("ToggleSwitchAutoFoldInQPoint", automation.IsAutoFoldInQPoint);
                SetToggleSwitchState("ToggleSwitchAutoFoldInYiYunVisualPresenter", automation.IsAutoFoldInYiYunVisualPresenter);
                SetToggleSwitchState("ToggleSwitchAutoFoldInMaxHubWhiteboard", automation.IsAutoFoldInMaxHubWhiteboard);
                SetToggleSwitchState("ToggleSwitchAutoFoldInPPTSlideShow", automation.IsAutoFoldInPPTSlideShow);
                SetToggleSwitchState("ToggleSwitchAutoFoldInEasiNoteIgnoreDesktopAnno", automation.IsAutoFoldInEasiNoteIgnoreDesktopAnno);
                SetToggleSwitchState("ToggleSwitchAutoFoldInOldZyBoard", automation.IsAutoFoldInOldZyBoard);
                SetToggleSwitchState("ToggleSwitchKeepFoldAfterSoftwareExit", automation.KeepFoldAfterSoftwareExit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 加载设置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置 ToggleSwitch 状态
        /// </summary>
        private void SetToggleSwitchState(string name, bool isOn)
        {
            try
            {
                var border = FindName(name) as System.Windows.Controls.Border;
                if (border != null)
                {
                    bool currentState = border.Background.ToString().Contains("3584e4");
                    if (currentState != isOn)
                    {
                        border.Background = isOn ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x35, 0x84, 0xe4)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xe1, 0xe1, 0xe1));
                        var innerBorder = border.Child as System.Windows.Controls.Border;
                        if (innerBorder != null)
                        {
                            innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutomationPanel 设置 ToggleSwitch {name} 状态时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// ToggleSwitch 点击事件处理
        /// </summary>
        private void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as System.Windows.Controls.Border;
            if (border == null) return;

            bool currentState = border.Background.ToString().Contains("3584e4");
            bool newState = !currentState;
            SetToggleSwitchState(border.Name, newState);

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

