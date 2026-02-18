using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class SecurityPanel : SettingsPanelBase
    {
        /// <summary>
        /// 初始化 SecurityPanel 实例并构建其 XAML 定义的界面元素。
        /// </summary>
        public SecurityPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 从应用设置加载安全配置并将其同步到面板上的相关切换开关和密码 UI 状态。
        /// </summary>
        /// <remarks>
        /// 如果应用设置或安全配置不存在，会创建默认的 Security 对象。方法在更新控件时临时抑制变更处理，完成后恢复加载状态；在同步过程中发生的异常将被静默忽略。
        /// </remarks>
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

        /// <summary>
        /// 根据当前安全设置启用或禁用与密码相关的 UI 控件。
        /// </summary>
        /// <remarks>
        /// 会读取 MainWindow.Settings.Security.PasswordEnabled 并相应地设置设置/更改密码按钮、禁用密码按钮以及依赖密码的各个切换开关的 IsEnabled 状态。
        /// </remarks>
        private void UpdatePasswordUiState()
        {
            var sec = MainWindow.Settings?.Security;
            var enabled = sec != null && sec.PasswordEnabled;

            if (BtnSetOrChangePassword != null) BtnSetOrChangePassword.IsEnabled = enabled;
            if (BtnDisablePassword != null) BtnDisablePassword.IsEnabled = enabled;

            // 用途开关：仅在启用密码功能时可操作
            var usageEnabled = enabled;
            var t1 = FindToggleSwitch("ToggleSwitchRequirePasswordOnExit");
            var t2 = FindToggleSwitch("ToggleSwitchRequirePasswordOnEnterSettings");
            var t3 = FindToggleSwitch("ToggleSwitchRequirePasswordOnResetConfig");
            if (t1 != null) t1.IsEnabled = usageEnabled;
            if (t2 != null) t2.IsEnabled = usageEnabled;
            if (t3 != null) t3.IsEnabled = usageEnabled;
        }

        /// <summary>
        /// 处理安全面板中各开关的变更：根据开关标识更新安全设置、在需要时提示或验证密码、保存设置并刷新相关 UI 或应用进程保护。
        /// </summary>
        /// <param name="tag">被切换的开关标识，支持 "PasswordEnabled"、"RequirePasswordOnExit"、"RequirePasswordOnEnterSettings"、"RequirePasswordOnResetConfig" 和 "EnableProcessProtection"。</param>
        /// <param name="newState">开关的新状态；`true` 表示启用，`false` 表示禁用。</param>
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

        /// <summary>
        /// 忽略选项组的变更 — 此面板不包含任何选项按钮组，因此传入的参数会被忽略。
        /// </summary>
        /// <param name="group">被触发的选项组标识（未使用）。</param>
        /// <param name="value">选中的选项值（未使用）。</param>
        protected override void HandleOptionChange(string group, string value)
        {
            // 本面板无选项按钮组
        }

        /// <summary>
        /// 将切换开关的点击事件传递给基类以执行默认处理。
        /// </summary>
        protected override void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            base.ToggleSwitch_Click(sender, e);
        }

        /// <summary>
        /// 在“设置/更改密码”按钮被点击时提示用户输入新密码并在用户提供密码后将其保存到应用设置中。
        /// </summary>
        /// <param name="sender">触发事件的对象（按钮）。</param>
        /// <param name="e">路由事件参数。</param>
        /// <remarks>如果用户提供了非空的新密码，则会设置该密码、启用密码保护、保存设置并更新密码相关的 UI 状态。</remarks>
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

        /// <summary>
        /// 在用户点击“禁用密码”按钮时，若当前已启用密码则将密码开关切换为关闭并触发相应的开关变更处理。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为按钮）。</param>
        /// <param name="e">事件参数。</param>
        private void BtnDisablePassword_Click(object sender, RoutedEventArgs e)
        {
            // 触发和开关一致的逻辑
            if (FindToggleSwitch("ToggleSwitchPasswordEnabled") is Border b)
            {
                // 模拟点击到 off
                if (MainWindow.Settings?.Security?.PasswordEnabled == true)
                {
                    _isLoaded = true;
                    SetToggleSwitchState(b, false);
                    HandleToggleSwitchChange("PasswordEnabled", false);
                }
            }
        }

        public event EventHandler<RoutedEventArgs> IsTopBarNeedShadowEffect;
        public event EventHandler<RoutedEventArgs> IsTopBarNeedNoShadowEffect;

        /// <summary>
        /// 根据 ScrollViewer 的垂直滚动位置在需要时触发顶部阴影或取消阴影的事件。
        /// </summary>
        /// <param name="sender">触发事件的 ScrollViewer。</param>
        /// <param name="e">滚动更改的事件参数。</param>
        private void ScrollViewerEx_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset >= 10) IsTopBarNeedShadowEffect?.Invoke(this, new RoutedEventArgs());
            else IsTopBarNeedNoShadowEffect?.Invoke(this, new RoutedEventArgs());
        }

        /// <summary>
        — 将当前面板应用主题并重新加载设置。
        /// </summary>
        /// <remarks>
        /// 尝试通过 ThemeHelper 为控件应用主题并调用 LoadSettings 来同步界面状态；任何在处理过程中发生的异常都会被捕获并忽略。
        /// </remarks>
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
