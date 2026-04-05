using System;

namespace Ink_Canvas.Helpers.Plugins
{
    /// <summary>
    /// 兼容旧代码的薄门面：与 <see cref="PluginSdkHostContext"/> 为同一实例，实现 <see cref="IPluginService"/>。
    /// </summary>
    public static class PluginServiceManager
    {
        public static IPluginService Instance
        {
            get
            {
                var ctx = PluginRuntime.SdkContext;
                if (ctx == null)
                {
                    throw new InvalidOperationException("插件宿主尚未初始化：请先调用 PluginRuntime.Initialize(MainWindow)。");
                }

                return (IPluginService)ctx;
            }
        }
    }
}
