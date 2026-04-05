using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace InkCanvasForClass.PluginSdk
{
    /// <summary>
    /// Ink Canvas 插件基类
    /// 提供插件的基本实现
    /// </summary>
    public abstract class InkCanvasPluginBase : IInkCanvasPlugin
    {
        private bool _isEnabled;
        private IPluginContext _context;

        /// <summary>
        /// 插件唯一标识符
        /// </summary>
        public abstract string Id { get; }

        /// <summary>
        /// 插件名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 插件描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 插件版本
        /// </summary>
        public abstract Version Version { get; }

        /// <summary>
        /// 插件作者
        /// </summary>
        public abstract string Author { get; }

        /// <summary>
        /// 插件主页URL
        /// </summary>
        public virtual string Homepage => string.Empty;

        /// <summary>
        /// 插件图标
        /// </summary>
        public virtual ImageSource Icon => null;

        /// <summary>
        /// 插件是否已启用
        /// </summary>
        public virtual bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnEnabledChanged(value);
                    EnabledChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// 插件启用状态变更事件
        /// </summary>
        public event EventHandler<bool> EnabledChanged;

        /// <summary>
        /// 插件上下文
        /// </summary>
        protected IPluginContext Context => _context;

        /// <summary>
        /// 插件初始化
        /// </summary>
        /// <param name="context">插件上下文</param>
        public virtual void Initialize(IPluginContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 方案 B：在 <see cref="Initialize"/> 之后由宿主调用，用于向 <see cref="IPluginRegistry"/> 登记菜单、工具栏、设置页等。
        /// </summary>
        /// <param name="registry">宿主提供的注册表</param>
        public virtual void RegisterExtensions(IPluginRegistry registry)
        {
        }

        /// <summary>
        /// 插件启动
        /// </summary>
        public virtual void Start()
        {
            // 默认实现为空
        }

        /// <summary>
        /// 插件停止
        /// </summary>
        public virtual void Stop()
        {
            // 默认实现为空
        }

        /// <summary>
        /// 插件清理
        /// </summary>
        public virtual void Cleanup()
        {
            // 默认实现为空
        }

        /// <summary>
        /// 获取插件设置界面
        /// </summary>
        /// <returns>设置界面控件</returns>
        public virtual UserControl GetSettingsView()
        {
            return new UserControl();
        }

        /// <summary>
        /// 获取插件菜单项
        /// </summary>
        /// <returns>菜单项列表</returns>
        public virtual IEnumerable<MenuItem> GetMenuItems()
        {
            return new List<MenuItem>();
        }

        /// <summary>
        /// 获取插件工具栏按钮
        /// </summary>
        /// <returns>工具栏按钮列表</returns>
        public virtual IEnumerable<Button> GetToolbarButtons()
        {
            return new List<Button>();
        }

        /// <summary>
        /// 获取插件状态栏信息
        /// </summary>
        /// <returns>状态栏信息</returns>
        public virtual string GetStatusBarInfo()
        {
            return $"{Name} v{Version} - {(IsEnabled ? "已启用" : "已禁用")}";
        }

        /// <summary>
        /// 启用状态变更时的处理
        /// </summary>
        /// <param name="isEnabled">是否启用</param>
        protected virtual void OnEnabledChanged(bool isEnabled)
        {
            if (isEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        /// <summary>
        /// 显示通知
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="type">通知类型</param>
        protected void ShowNotification(string message, NotificationType type = NotificationType.Info)
        {
            _context?.ShowNotification(message, type);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <returns>用户选择结果</returns>
        protected bool ShowConfirmDialog(string message, string title = "确认")
        {
            return _context?.ShowConfirmDialog(message, title) ?? false;
        }

        /// <summary>
        /// 显示输入对话框
        /// </summary>
        /// <param name="message">提示消息</param>
        /// <param name="title">标题</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>用户输入内容</returns>
        protected string ShowInputDialog(string message, string title = "输入", string defaultValue = "")
        {
            return _context?.ShowInputDialog(message, title, defaultValue) ?? defaultValue;
        }

        /// <summary>
        /// 获取设置值
        /// </summary>
        /// <typeparam name="T">设置类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>设置值</returns>
        protected T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_context == null) return defaultValue;
            return _context.GetSetting(key, defaultValue);
        }

        /// <summary>
        /// 设置设置值
        /// </summary>
        /// <typeparam name="T">设置类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="value">设置值</param>
        protected void SetSetting<T>(string key, T value)
        {
            _context?.SetSetting(key, value);
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        protected void SaveSettings()
        {
            _context?.SaveSettings();
        }

        /// <summary>
        /// 注册事件处理器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        protected void RegisterEventHandler(string eventName, EventHandler handler)
        {
            _context?.RegisterEventHandler(eventName, handler);
        }

        /// <summary>
        /// 注销事件处理器
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="handler">事件处理器</param>
        protected void UnregisterEventHandler(string eventName, EventHandler handler)
        {
            _context?.UnregisterEventHandler(eventName, handler);
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="sender">事件发送者</param>
        /// <param name="args">事件参数</param>
        protected void TriggerEvent(string eventName, object sender, EventArgs args)
        {
            _context?.TriggerEvent(eventName, sender, args);
        }
    }
}
