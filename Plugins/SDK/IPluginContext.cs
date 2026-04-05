using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Ink;

namespace InkCanvasForClass.PluginSdk
{
    /// <summary>
    /// 插件上下文接口，提供对主应用程序功能的访问
    /// </summary>
    public interface IPluginContext
    {
        /// <summary>
        /// 主窗口实例
        /// </summary>
        Window MainWindow { get; }

        /// <summary>
        /// 当前画布
        /// </summary>
        System.Windows.Controls.InkCanvas CurrentCanvas { get; }

        /// <summary>
        /// 所有画布页面
        /// </summary>
        IList<System.Windows.Controls.Canvas> AllCanvasPages { get; }

        /// <summary>
        /// 当前页面索引
        /// </summary>
        int CurrentPageIndex { get; }

        /// <summary>
        /// 总页面数
        /// </summary>
        int TotalPageCount { get; }

        /// <summary>
        /// 浮动工具栏
        /// </summary>
        FrameworkElement FloatingToolBar { get; }

        /// <summary>
        /// 左侧面板
        /// </summary>
        FrameworkElement LeftPanel { get; }

        /// <summary>
        /// 右侧面板
        /// </summary>
        FrameworkElement RightPanel { get; }

        /// <summary>
        /// 顶部面板
        /// </summary>
        FrameworkElement TopPanel { get; }

        /// <summary>
        /// 底部面板
        /// </summary>
        FrameworkElement BottomPanel { get; }

        /// <summary>
        /// 当前绘制模式
        /// </summary>
        int CurrentDrawingMode { get; }

        /// <summary>
        /// 当前墨迹宽度
        /// </summary>
        double CurrentInkWidth { get; }

        /// <summary>
        /// 当前墨迹颜色
        /// </summary>
        Color CurrentInkColor { get; }

        /// <summary>
        /// 当前高亮笔宽度
        /// </summary>
        double CurrentHighlighterWidth { get; }

        /// <summary>
        /// 当前橡皮擦大小
        /// </summary>
        int CurrentEraserSize { get; }

        /// <summary>
        /// 当前橡皮擦类型
        /// </summary>
        int CurrentEraserType { get; }

        /// <summary>
        /// 当前橡皮擦形状
        /// </summary>
        int CurrentEraserShape { get; }

        /// <summary>
        /// 当前墨迹透明度
        /// </summary>
        double CurrentInkAlpha { get; }

        /// <summary>
        /// 当前墨迹样式
        /// </summary>
        int CurrentInkStyle { get; }

        /// <summary>
        /// 当前背景颜色
        /// </summary>
        string CurrentBackgroundColor { get; }

        /// <summary>
        /// 是否为深色主题
        /// </summary>
        bool IsDarkTheme { get; }

        /// <summary>
        /// 是否为白板模式
        /// </summary>
        bool IsWhiteboardMode { get; }

        /// <summary>
        /// 是否为PPT模式
        /// </summary>
        bool IsPPTMode { get; }

        /// <summary>
        /// 是否为全屏模式
        /// </summary>
        bool IsFullScreenMode { get; }

        /// <summary>
        /// 是否为画布模式
        /// </summary>
        bool IsCanvasMode { get; }

        /// <summary>
        /// 是否为选择模式
        /// </summary>
        bool IsSelectionMode { get; }

        /// <summary>
        /// 是否为橡皮擦模式
        /// </summary>
        bool IsEraserMode { get; }

        /// <summary>
        /// 是否为形状绘制模式
        /// </summary>
        bool IsShapeDrawingMode { get; }

        /// <summary>
        /// 是否为高亮笔模式
        /// </summary>
        bool IsHighlighterMode { get; }

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// 是否可以重做
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// 获取设置值
        /// </summary>
        /// <typeparam name="T">设置类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>设置值</returns>
        T GetSetting<T>(string key, T defaultValue = default(T));

        /// <summary>
        /// 设置设置值
        /// </summary>
        /// <typeparam name="T">设置类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="value">设置值</param>
        void SetSetting<T>(string key, T value);

        /// <summary>
        /// 保存设置
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// 加载设置
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// 重置设置
        /// </summary>
        void ResetSettings();

        /// <summary>
        /// 获取所有插件
        /// </summary>
        /// <returns>插件列表</returns>
        IList<IInkCanvasPlugin> GetAllPlugins();

        /// <summary>
        /// 根据名称获取插件
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        /// <returns>插件实例</returns>
        IInkCanvasPlugin GetPlugin(string pluginName);

        /// <summary>
        /// 启用插件
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        void EnablePlugin(string pluginName);

        /// <summary>
        /// 禁用插件
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        void DisablePlugin(string pluginName);

        /// <summary>
        /// 卸载插件
        /// </summary>
        /// <param name="pluginName">插件名称</param>
        void UnloadPlugin(string pluginName);

        /// <summary>
        /// 显示设置窗口
        /// </summary>
        void ShowSettingsWindow();

        /// <summary>
        /// 隐藏设置窗口
        /// </summary>
        void HideSettingsWindow();

        /// <summary>
        /// 显示插件设置窗口
        /// </summary>
        void ShowPluginSettingsWindow();

        /// <summary>
        /// 隐藏插件设置窗口
        /// </summary>
        void HidePluginSettingsWindow();

        /// <summary>
        /// 显示帮助窗口
        /// </summary>
        void ShowHelpWindow();

        /// <summary>
        /// 隐藏帮助窗口
        /// </summary>
        void HideHelpWindow();

        /// <summary>
        /// 显示关于窗口
        /// </summary>
        void ShowAboutWindow();

        /// <summary>
        /// 隐藏关于窗口
        /// </summary>
        void HideAboutWindow();

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        void ShowNotification(string message, NotificationType type = NotificationType.Info);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        bool ShowConfirmDialog(string message, string title = "确认");

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入内容</returns>
        string ShowInputDialog(string message, string title = "输入", string defaultValue = "");

        /// <summary>
        /// 设置全屏模式
        /// </summary>
        /// <param name="isFullScreen">是否全屏</param>
        void SetFullScreen(bool isFullScreen);

        /// <summary>
        /// 设置置顶模式
        /// </summary>
        /// <param name="isTopMost">是否置顶</param>
        void SetTopMost(bool isTopMost);

        /// <summary>
        /// 设置窗口可见性
        /// </summary>
        /// <param name="isVisible">是否可见</param>
        void SetWindowVisibility(bool isVisible);

        /// <summary>
        /// 最小化窗口
        /// </summary>
        void MinimizeWindow();

        /// <summary>
        /// 最大化窗口
        /// </summary>
        void MaximizeWindow();

        /// <summary>
        /// 还原窗口
        /// </summary>
        void RestoreWindow();

        /// <summary>
        /// 关闭窗口
        /// </summary>
        void CloseWindow();

        /// <summary>
        /// 设置窗口位置
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        void SetWindowPosition(double x, double y);

        /// <summary>
        /// 设置窗口大小
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        void SetWindowSize(double width, double height);

        /// <summary>
        /// 获取窗口位置
        /// </summary>
        /// <returns>窗口位置</returns>
        (double x, double y) GetWindowPosition();

        /// <summary>
        /// 获取窗口大小
        /// </summary>
        /// <returns>窗口大小</returns>
        (double width, double height) GetWindowSize();

        /// <summary>
        /// 清除当前画布
        /// </summary>
        void ClearCanvas();

        /// <summary>
        /// 清除所有画布
        /// </summary>
        void ClearAllCanvases();

        /// <summary>
        /// 添加新页面
        /// </summary>
        void AddNewPage();

        /// <summary>
        /// 删除当前页面
        /// </summary>
        void DeleteCurrentPage();

        /// <summary>
        /// 切换到指定页面
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        void SwitchToPage(int pageIndex);

        /// <summary>
        /// 下一页
        /// </summary>
        void NextPage();

        /// <summary>
        /// 上一页
        /// </summary>
        void PreviousPage();

        /// <summary>
        /// 设置绘制模式
        /// </summary>
        /// <param name="mode">绘制模式</param>
        void SetDrawingMode(int mode);

        /// <summary>
        /// 设置墨迹宽度
        /// </summary>
        /// <param name="width">宽度</param>
        void SetInkWidth(double width);

        /// <summary>
        /// 设置墨迹颜色
        /// </summary>
        /// <param name="color">颜色</param>
        void SetInkColor(Color color);

        /// <summary>
        /// 设置高亮笔宽度
        /// </summary>
        /// <param name="width">宽度</param>
        void SetHighlighterWidth(double width);

        /// <summary>
        /// 设置橡皮擦大小
        /// </summary>
        /// <param name="size">大小</param>
        void SetEraserSize(int size);

        /// <summary>
        /// 设置橡皮擦类型
        /// </summary>
        /// <param name="type">类型</param>
        void SetEraserType(int type);

        /// <summary>
        /// 设置橡皮擦形状
        /// </summary>
        /// <param name="shape">形状</param>
        void SetEraserShape(int shape);

        /// <summary>
        /// 设置墨迹透明度
        /// </summary>
        /// <param name="alpha">透明度</param>
        void SetInkAlpha(double alpha);

        /// <summary>
        /// 设置墨迹样式
        /// </summary>
        /// <param name="style">样式</param>
        void SetInkStyle(int style);

        /// <summary>
        /// 设置背景颜色
        /// </summary>
        /// <param name="color">颜色</param>
        void SetBackgroundColor(string color);

        /// <summary>
        /// 保存画布
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void SaveCanvas(string filePath);

        /// <summary>
        /// 加载画布
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void LoadCanvas(string filePath);

        /// <summary>
        /// 导出为图片
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">格式</param>
        void ExportAsImage(string filePath, string format);

        /// <summary>
        /// 导出为PDF
        /// </summary>
        /// <param name="filePath">文件路径</param>
        void ExportAsPDF(string filePath);

        /// <summary>
        /// 撤销操作
        /// </summary>
        void Undo();

        /// <summary>
        /// 重做操作
        /// </summary>
        void Redo();

        /// <summary>
        /// 全选
        /// </summary>
        void SelectAll();

        /// <summary>
        /// 取消选择
        /// </summary>
        void DeselectAll();

        /// <summary>
        /// 删除选中项
        /// </summary>
        void DeleteSelected();

        /// <summary>
        /// 复制选中项
        /// </summary>
        void CopySelected();

        /// <summary>
        /// 剪切选中项
        /// </summary>
        void CutSelected();

        /// <summary>
        /// 粘贴
        /// </summary>
        void Paste();

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        void RegisterEventHandler(string eventName, EventHandler handler);

        /// <summary>
        /// 注销事件处理器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        void UnregisterEventHandler(string eventName, EventHandler handler);

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        void TriggerEvent(string eventName, object sender, EventArgs args);

        /// <summary>
        /// 重启应用程序
        /// </summary>
        void RestartApplication();

        /// <summary>
        /// 退出应用程序
        /// </summary>
        void ExitApplication();

        /// <summary>
        /// 检查更新
        /// </summary>
        void CheckForUpdates();

        /// <summary>
        /// 打开帮助文档
        /// </summary>
        void OpenHelpDocument();

        /// <summary>
        /// 打开关于页面
        /// </summary>
        void OpenAboutPage();
    }
}
