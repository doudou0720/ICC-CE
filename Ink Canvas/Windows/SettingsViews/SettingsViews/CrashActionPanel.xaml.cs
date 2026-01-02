using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ink_Canvas;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class CrashActionPanel : UserControl
    {
        public CrashActionPanel()
        {
            InitializeComponent();
            Loaded += CrashActionPanel_Loaded;
        }

        private void CrashActionPanel_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            ApplyTheme();
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
            try
            {
                if (App.CrashAction == App.CrashActionType.SilentRestart)
                {
                    ThemeHelper.SetOptionButtonSelectedState(CrashActionSilentRestartBorder, true);
                    ThemeHelper.SetOptionButtonSelectedState(CrashActionNoActionBorder, false);
                }
                else
                {
                    ThemeHelper.SetOptionButtonSelectedState(CrashActionSilentRestartBorder, false);
                    ThemeHelper.SetOptionButtonSelectedState(CrashActionNoActionBorder, true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrashActionPanel 加载设置时出错: {ex.Message}");
            }
        }
        
        private void OptionButton_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                string tag = border.Tag?.ToString();
                if (!string.IsNullOrEmpty(tag))
                {
                    // 清除同组其他按钮的选中状态
                    if (tag == "CrashAction_SilentRestart")
                    {
                        ThemeHelper.SetOptionButtonSelectedState(CrashActionSilentRestartBorder, true);
                        ThemeHelper.SetOptionButtonSelectedState(CrashActionNoActionBorder, false);
                        App.CrashAction = App.CrashActionType.SilentRestart;
                        if (MainWindow.Settings?.Startup != null)
                        {
                            MainWindow.Settings.Startup.CrashAction = (int)App.CrashActionType.SilentRestart;
                        }
                    }
                    else if (tag == "CrashAction_NoAction")
                    {
                        ThemeHelper.SetOptionButtonSelectedState(CrashActionSilentRestartBorder, false);
                        ThemeHelper.SetOptionButtonSelectedState(CrashActionNoActionBorder, true);
                        App.CrashAction = App.CrashActionType.NoAction;
                        if (MainWindow.Settings?.Startup != null)
                        {
                            MainWindow.Settings.Startup.CrashAction = (int)App.CrashActionType.NoAction;
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
                // 重新应用按钮状态以适配主题
                LoadSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrashActionPanel 应用主题时出错: {ex.Message}");
            }
        }
    }
}
