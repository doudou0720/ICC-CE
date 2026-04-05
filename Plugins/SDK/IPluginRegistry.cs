using System;
using System.Windows.Controls;

namespace InkCanvasForClass.PluginSdk
{
    /// <summary>
    /// 方案 B：不依赖 Microsoft.Extensions.DependencyInjection 的轻量扩展注册表。
    /// 宿主在适当时机将登记项挂到菜单 / 工具栏 / 设置 UI。
    /// </summary>
    public interface IPluginRegistry
    {
        /// <summary>
        /// 当前正在执行注册的插件目录 Id（由宿主在加载每个插件前设置）。
        /// </summary>
        string CurrentPluginId { get; }

        /// <summary>
        /// 注册主菜单或上下文菜单中的项；<paramref name="groupKey"/> 由宿主解释（如 "Main.Plugins"）。
        /// </summary>
        void RegisterMenuItem(string groupKey, MenuItem item);

        /// <summary>
        /// 注册工具栏按钮。
        /// </summary>
        void RegisterToolbarButton(Button button);

        /// <summary>
        /// 注册设置页；<paramref name="createView"/> 在打开设置时惰性创建。
        /// </summary>
        void RegisterSettingsPage(string pageId, string displayName, Func<UserControl> createView);
    }
}
