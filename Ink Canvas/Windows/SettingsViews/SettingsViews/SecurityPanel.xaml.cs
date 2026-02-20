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
        /// 初始化 SecurityPanel 实例并构建其界面元素。
        /// </summary>
        public SecurityPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 将应用的安全配置载入并同步到面板上的开关控件和相关密码 UI 状态。
        /// </summary>
        /// <remarks>
        /// 如果 MainWindow.Settings.Security 为 null，会创建一个新的 Security 实例。函数在更新控件期间会将内部标志 <c>_isLoaded</c> 设置为 false/true 以防止事件误触发；发生任何异常时会被捕获并忽略，方法仍会将 <c>_isLoaded</c> 复原为 true。
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
        /// 根据当前应用设置的密码启用状态，启用或禁用与密码相关的 UI 控件。
        /// </summary>
        /// <remarks>
        /// 如果安全设置中启用了密码，则启用“设置/更改密码”按钮以及依赖密码功能的三个选项开关；否则将它们置为不可用。
        /// </remarks>
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

        /// <summary>
        /// 处理安全设置面板中各切换开关的状态变更，并根据不同选项执行相应的持久化或安全操作。
        /// </summary>
        /// <param name="tag">标识要更改的选项；支持的值包括：
        /// "PasswordEnabled"（启用/禁用密码保护；启用时若未设置密码会提示设置，禁用时若已设置密码会提示输入以确认；变更会更新安全配置并刷新相关 UI）,
        /// "RequirePasswordOnExit"（退出时是否要求密码）,
        /// "RequirePasswordOnEnterSettings"（进入设置时是否要求密码）,
        /// "RequirePasswordOnResetConfig"（重置配置时是否要求密码）,
        /// "EnableProcessProtection"（启用/禁用进程保护，变更后会立即应用进程保护管理器的设置）。</param>
        /// <param name="newState">目标布尔状态；表示所选项应被设置为启用（true）或禁用（false）。</param>
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
        /// 处理选项组的更改请求（本面板不包含选项组，因此不执行任何操作）。
        /// </summary>
        /// <param name="group">触发更改的选项组标识。</param>
        /// <param name="value">选中的选项值。</param>
        protected override void HandleOptionChange(string group, string value)
        {
            // 本面板无选项按钮组
        }

        /// <summary>
        /// 处理切换开关的点击事件，并将事件处理委托给基类实现。
        /// </summary>
        /// <param name="sender">触发事件的对象（通常是切换开关控件）。</param>
        /// <param name="e">包含路由事件数据的 <see cref="RoutedEventArgs"/> 实例。</param>
        protected override void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            base.ToggleSwitch_Click(sender, e);
        }

        /// <summary>
        /// 提示用户设置或更改密码；若用户提供了新密码，则保存该密码、启用密码功能并持久化与刷新相关设置状态。
        /// </summary>
        /// <remarks>
        /// 在用户输入非空新密码后，会通过 SecurityManager 保存密码、将 Settings.Security.PasswordEnabled 设为 true、调用 MainWindow.SaveSettingsToFile() 并更新密码相关的界面状态。
        /// </remarks>
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

        /// <summary>
        /// 根据 ScrollViewer 的垂直偏移决定并触发顶部栏显示或移除阴影的事件（阈值为 10 像素）。
        /// </summary>
        /// <param name="sender">触发事件的 ScrollViewer 控件。</param>
        /// <param name="e">包含滚动更改信息的事件参数。</param>
        private void ScrollViewerEx_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset >= 10) IsTopBarNeedShadowEffect?.Invoke(this, new RoutedEventArgs());
            else IsTopBarNeedNoShadowEffect?.Invoke(this, new RoutedEventArgs());
        }

        /// <summary>
        /// 将当前主题应用到此面板并重新加载面板设置。
        /// </summary>
        /// <remarks>
        /// 在应用主题或重新加载设置时发生的任何异常将被吞噬，不会向上抛出。
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
