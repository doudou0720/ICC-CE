namespace Ink_Canvas.Helpers.Plugins
{
    /// <summary>
    /// 在加载任何 SDK 插件之前初始化宿主上下文（实现 <see cref="IPluginContext"/> 与 <see cref="IPluginService"/>）。
    /// </summary>
    public static class PluginRuntime
    {
        private static PluginSdkHostContext _context;

        public static PluginSdkHostContext SdkContext => _context;

        /// <summary>与 <see cref="SdkContext"/> 相同实例，便于旧代码通过 <see cref="PluginServiceManager"/> 访问。</summary>
        public static IPluginService Services => SdkContext != null ? (IPluginService)SdkContext : null;

        public static void Initialize(MainWindow mainWindow)
        {
            if (_context == null)
            {
                _context = new PluginSdkHostContext();
            }

            _context.SetMainWindow(mainWindow);
        }
    }
}
