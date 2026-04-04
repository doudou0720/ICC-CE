using System;
using System.Windows;
using System.Windows.Controls;
using Ink_Canvas.Helpers;

namespace Ink_Canvas.Windows.SettingsViews2.Pages
{
    /// <summary>
    /// StartupPage.xaml 的交互逻辑
    /// </summary>
    public partial class StartupPage : Page
    {
        private bool _isLoaded = false;

        public StartupPage()
        {
            InitializeComponent();
            Loaded += StartupPage_Loaded;
        }

        private void StartupPage_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            _isLoaded = true;
        }

        /// <summary>
        /// 加载设置到UI
        /// </summary>
        private void LoadSettings()
        {
            _isLoaded = false;

            try
            {
                var settings = SettingsService.Current;

                // 窗口无焦点模式
                ToggleSwitchNoFocusMode.IsOn = settings.Advanced?.IsNoFocusMode ?? true;

                // 窗口无边框模式
                ToggleSwitchWindowMode.IsOn = settings.Advanced?.WindowMode ?? true;

                // 窗口置顶
                ToggleSwitchAlwaysOnTop.IsOn = settings.Advanced?.IsAlwaysOnTop ?? true;

                // UIA置顶
                ToggleSwitchUIAccessTopMost.IsOn = settings.Advanced?.EnableUIAccessTopMost ?? false;

                // 开机时运行
                bool runAtStartup = System.IO.File.Exists(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup) + "\\Ink Canvas Annotation.lnk");
                ToggleSwitchRunAtStartup.IsOn = runAtStartup;

                // 启动时折叠
                ToggleSwitchFoldAtStartup.IsOn = settings.Startup?.IsFoldAtStartup ?? false;

                // 仅PPT模式
                ToggleSwitchPPTOnlyMode.IsOn = settings.ModeSettings?.IsPPTOnlyMode ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载启动设置时出错: {ex.Message}");
            }

            _isLoaded = true;
        }

        #region 窗口设置事件处理

        /// <summary>
        /// 窗口无焦点模式开关事件
        /// </summary>
        private void ToggleSwitchNoFocusMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchNoFocusMode.IsOn;
                SettingsService.UpdateSetting("Advanced.IsNoFocusMode", newState);
                ApplyWindowSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置窗口无焦点模式时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口无边框模式开关事件
        /// </summary>
        private void ToggleSwitchWindowMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchWindowMode.IsOn;
                SettingsService.UpdateSetting("Advanced.WindowMode", newState);
                ApplyWindowSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置窗口无边框模式时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗口置顶开关事件
        /// </summary>
        private void ToggleSwitchAlwaysOnTop_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchAlwaysOnTop.IsOn;
                SettingsService.UpdateSetting("Advanced.IsAlwaysOnTop", newState);
                ApplyWindowSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置窗口置顶时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// UIA置顶开关事件
        /// </summary>
        private void ToggleSwitchUIAccessTopMost_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchUIAccessTopMost.IsOn;
                SettingsService.UpdateSetting("Advanced.EnableUIAccessTopMost", newState);
                ApplyWindowSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置UIA置顶时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 应用窗口设置到主窗口
        /// </summary>
        private void ApplyWindowSettings()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                var settings = SettingsService.Current;

                // 应用窗口置顶
                mainWindow.Topmost = settings.Advanced?.IsAlwaysOnTop ?? true;

                // 应用窗口模式（无边框/有边框）
                if (settings.Advanced?.WindowMode ?? true)
                {
                    mainWindow.WindowStyle = WindowStyle.None;
                }
                else
                {
                    mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                }

                // 应用无焦点模式
                if (settings.Advanced?.IsNoFocusMode ?? true)
                {
                    // 使用反射调用主窗口的无焦点模式设置方法
                    var method = mainWindow.GetType().GetMethod("SetNoFocusMode",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, new object[] { true });
                }

                // 通知其他面板同步状态
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchNoFocusMode");
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchWindowMode");
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchAlwaysOnTop");
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchUIAccessTopMost");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用窗口设置时出错: {ex.Message}");
            }
        }

        #endregion

        #region 启动设置事件处理

        /// <summary>
        /// 开机时运行开关事件
        /// </summary>
        private void ToggleSwitchRunAtStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchRunAtStartup.IsOn;
                SetRunAtStartup(newState);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置开机启动时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置开机启动
        /// </summary>
        private void SetRunAtStartup(bool enable)
        {
            try
            {
                string startupPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup);
                string shortcutPath = System.IO.Path.Combine(startupPath, "Ink Canvas Annotation.lnk");
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                if (enable)
                {
                    // 创建快捷方式
                    if (!System.IO.File.Exists(shortcutPath))
                    {
                        CreateShortcut(shortcutPath, exePath);
                    }
                }
                else
                {
                    // 删除快捷方式
                    if (System.IO.File.Exists(shortcutPath))
                    {
                        System.IO.File.Delete(shortcutPath);
                    }
                }

                // 通知其他面板同步状态
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchRunAtStartup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置开机启动失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建快捷方式
        /// </summary>
        private void CreateShortcut(string shortcutPath, string targetPath)
        {
            try
            {
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(targetPath);
                shortcut.Save();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建快捷方式失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 开机运行后收纳到侧边栏开关事件
        /// </summary>
        private void ToggleSwitchFoldAtStartup_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchFoldAtStartup.IsOn;
                SettingsService.UpdateSetting("Startup.IsFoldAtStartup", newState);

                // 通知其他面板同步状态
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchFoldAtStartup");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置开机折叠时出错: {ex.Message}");
            }
        }

        #endregion

        #region 模式设置事件处理

        /// <summary>
        /// 仅PPT模式开关事件
        /// </summary>
        private void ToggleSwitchPPTOnlyMode_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            try
            {
                bool newState = ToggleSwitchPPTOnlyMode.IsOn;
                SettingsService.UpdateSetting("ModeSettings.IsPPTOnlyMode", newState);

                // 通知其他面板同步状态
                Windows.SettingsViews.MainWindowSettingsHelper.NotifySettingsPanelsSyncState("ToggleSwitchMode");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"设置仅PPT模式时出错: {ex.Message}");
            }
        }

        #endregion

        #region 插件管理事件处理

        /// <summary>
        /// 打开插件管理器按钮点击事件
        /// </summary>
        private void BtnOpenPluginManager_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 创建并显示插件设置窗口
                var pluginSettingsWindow = new Windows.PluginSettingsWindow();
                pluginSettingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开插件管理器时出错: {ex.Message}");
            }
        }

        #endregion
    }
}
