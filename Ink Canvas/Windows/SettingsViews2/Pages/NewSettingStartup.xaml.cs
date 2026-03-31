using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Ink_Canvas.Windows.SettingsViews2.Pages
{
    /// <summary>
    /// NewSettingStartup.xaml 的交互逻辑
    /// </summary>
    public partial class NewSettingStartup : Page
    {
        public NewSettingStartup()
        {
            InitializeComponent();
            // 初始化开关状态
            InitializeSwitchState();
        }

        /// <summary>
        /// 初始化开关状态
        /// </summary>
        private void InitializeSwitchState()
        {
            try
            {
                // 通过检查启动文件夹中是否存在快捷方式来判断开机自启动状态
                if (System.IO.File.Exists(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup) + "\\Ink Canvas Annotation.lnk"))
                {
                    ToggleSwitchRunAtStartup.IsOn = true;
                }
                else
                {
                    ToggleSwitchRunAtStartup.IsOn = false;
                }
                
                // 同步主窗口的开关状态
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.ToggleSwitchRunAtStartup != null)
                {
                    mainWindow.ToggleSwitchRunAtStartup.IsOn = ToggleSwitchRunAtStartup.IsOn;
                }
            }
            catch (Exception)
            {
                // 如果发生异常，默认设置为关闭
                ToggleSwitchRunAtStartup.IsOn = false;
            }
        }

        /// <summary>
        /// 处理开机自启动开关状态更改事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        private void ToggleSwitchRunAtStartup_Toggled(object sender, RoutedEventArgs e)
        {
            try
            {
                // 直接实现开机自启动逻辑
                if (ToggleSwitchRunAtStartup.IsOn)
                {
                    MainWindow.StartAutomaticallyDel("InkCanvas");
                    MainWindow.StartAutomaticallyCreate("Ink Canvas Annotation");
                }
                else
                {
                    MainWindow.StartAutomaticallyDel("InkCanvas");
                    MainWindow.StartAutomaticallyDel("Ink Canvas Annotation");
                }
                
                // 同步到主窗口的设置
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.ToggleSwitchRunAtStartup != null)
                {
                    mainWindow.ToggleSwitchRunAtStartup.IsOn = ToggleSwitchRunAtStartup.IsOn;
                }
            }
            catch (Exception)
            {
                // 忽略异常
            }
        }
    }
}