using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkCanvasForClass.PluginSdk
{
    /// <summary>
    /// Ink Canvas 插件接口
    /// </summary>
    public interface IInkCanvasPlugin
    {
        /// <summary>
        /// 插件唯一标识符
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 插件名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        Version Version { get; }

        /// <summary>
        /// 插件作者
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 插件主页URL
        /// </summary>
        string Homepage { get; }

        /// <summary>
        /// 插件图标
        /// </summary>
        ImageSource Icon { get; }

        /// <summary>
        /// 插件初始化
        /// </summary>
        /// <param name="context">插件上下文</param>
        void Initialize(IPluginContext context);

        /// <summary>
        /// 插件启动
        /// </summary>
        void Start();

        /// <summary>
        /// 插件停止
        /// </summary>
        void Stop();

        /// <summary>
        /// 插件清理
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 获取插件设置界面
        /// </summary>
        /// <returns>设置界面控件</returns>
        UserControl GetSettingsView();

        /// <summary>
        /// 获取插件菜单项
        /// </summary>
        /// <returns>菜单项列表</returns>
        IEnumerable<MenuItem> GetMenuItems();

        /// <summary>
        /// 获取插件工具栏按钮
        /// </summary>
        /// <returns>工具栏按钮列表</returns>
        IEnumerable<Button> GetToolbarButtons();

        /// <summary>
        /// 获取插件状态栏信息
        /// </summary>
        /// <returns>状态栏信息</returns>
        string GetStatusBarInfo();

        /// <summary>
        /// 插件是否已启用
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 插件启用状态变更事件
        /// </summary>
        event EventHandler<bool> EnabledChanged;
    }
}
