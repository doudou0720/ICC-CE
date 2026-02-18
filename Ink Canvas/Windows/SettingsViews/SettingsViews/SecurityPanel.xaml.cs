using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class SecurityPanel : SettingsPanelBase
    {
        public SecurityPanel()
        {
            InitializeComponent();
        }

        public override void LoadSettings()
        {
            if (MainWindow.Settings == null) return;
            if (MainWindow.Settings.Security == null) MainWindow.Settings.Security = new Security();

            _isLoaded = false;
            try
            {
                var sec = MainWindow.Settings.Security;

                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchPasswordEnabled"), sec.PasswordEnabled);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchRequirePasswordOnExit"), sec.RequirePasswordOnExit);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchRequirePasswordOnEnterSettings"), sec.RequirePasswordOnEnterSettings);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchRequirePasswordOnResetConfig"), sec.RequirePasswordOnResetConfig);
                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchEnableProcessProtection"), sec.EnableProcessProtection);

                UpdatePasswordUiState();
            }
            catch
            {
            }
            _isLoaded = true;
        }

        private void UpdatePasswordUiState()
        {
            var sec = MainWindow.Settings?.Security;
            var enabled = sec != null && sec.PasswordEnabled;

            if (BtnSetOrChangePassword != null) BtnSetOrChangePassword.IsEnabled = enabled;

            // 用途开关：仅在启用密码功能时可操作
            var usageEnabled = enabled;
            var t1 = FindToggleSwitch("ToggleSwitchRequirePasswordOnExit");
            var t2 = FindToggleSwitch("ToggleSwitchRequirePasswordOnEnterSettings");
            var t3 = FindToggleSwitch("ToggleSwitchRequirePasswordOnResetConfig");
            if (t1 != null) t1.IsEnabled = usageEnabled;
            if (t2 != null) t2.IsEnabled = usageEnabled;
            if (t3 != null) t3.IsEnabled = usageEnabled;
        }

        protected override async void HandleToggleSwitchChange(string tag, bool newState)
        {
            if (MainWindow.Settings == null) return;
            if (MainWindow.Settings.Security == null) MainWindow.Settings.Security = new Security();
            var sec = MainWindow.Settings.Security;

            switch (tag)
            {
                case "PasswordEnabled":
                    if (newState)
                    {
                        var havePassword = SecurityManager.HasPasswordConfigured(MainWindow.Settings);

                        if (!havePassword)
                        {
                            var pwd = await SecurityManager.PromptSetNewPasswordAsync(Window.GetWindow(this));
                            if (string.IsNullOrEmpty(pwd))
                            {
                                _isLoaded = false;
                                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchPasswordEnabled"), false);
                                _isLoaded = true;
                                return;
                            }
                            SecurityManager.SetPassword(MainWindow.Settings, pwd);
                        }

                        sec.PasswordEnabled = true;
                        MainWindow.SaveSettingsToFile();
                        UpdatePasswordUiState();
                    }
                    else
                    {
                        // 关闭：需要输入当前密码确认（已设置密码时）
                        if (SecurityManager.HasPasswordConfigured(MainWindow.Settings))
                        {
                            bool ok = await SecurityManager.PromptAndVerifyAsync(MainWindow.Settings, Window.GetWindow(this),
                                "关闭安全密码", "请输入当前密码以关闭安全密码功能。");
                            if (!ok)
                            {
                                _isLoaded = false;
                                SetToggleSwitchState(FindToggleSwitch("ToggleSwitchPasswordEnabled"), true);
                                _isLoaded = true;
                                return;
                            }
                        }

                        sec.PasswordEnabled = false;
                        SecurityManager.ClearPassword(MainWindow.Settings);
                        MainWindow.SaveSettingsToFile();
                        UpdatePasswordUiState();
                    }
                    break;

                case "RequirePasswordOnExit":
                    sec.RequirePasswordOnExit = newState;
                    MainWindow.SaveSettingsToFile();
                    break;
                case "RequirePasswordOnEnterSettings":
                    sec.RequirePasswordOnEnterSettings = newState;
                    MainWindow.SaveSettingsToFile();
                    break;
                case "RequirePasswordOnResetConfig":
                    sec.RequirePasswordOnResetConfig = newState;
                    MainWindow.SaveSettingsToFile();
                    break;
                case "EnableProcessProtection":
                    sec.EnableProcessProtection = newState;
                    MainWindow.SaveSettingsToFile();
                    ProcessProtectionManager.SetEnabled(newState);
                    break;
            }
        }

        protected override void HandleOptionChange(string group, string value)
        {
            // 本面板无选项按钮组
        }

        protected override void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            base.ToggleSwitch_Click(sender, e);
        }

        private async void BtnSetOrChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Settings == null) return;
            if (MainWindow.Settings.Security == null) MainWindow.Settings.Security = new Security();

            var owner = Window.GetWindow(this);

            var newPwd = await SecurityManager.PromptChangePasswordAsync(MainWindow.Settings, owner);
            if (!string.IsNullOrEmpty(newPwd))
            {
                SecurityManager.SetPassword(MainWindow.Settings, newPwd);
                MainWindow.Settings.Security.PasswordEnabled = true;
                MainWindow.SaveSettingsToFile();
                UpdatePasswordUiState();
            }
        }

        public event EventHandler<RoutedEventArgs> IsTopBarNeedShadowEffect;
        public event EventHandler<RoutedEventArgs> IsTopBarNeedNoShadowEffect;

        private void ScrollViewerEx_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset >= 10) IsTopBarNeedShadowEffect?.Invoke(this, new RoutedEventArgs());
            else IsTopBarNeedNoShadowEffect?.Invoke(this, new RoutedEventArgs());
        }

        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
                LoadSettings();
            }
            catch { }
        }
    }
}

