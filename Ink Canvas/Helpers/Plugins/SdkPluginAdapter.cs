using System;
using System.Windows.Controls;
using InkCanvasForClass.PluginHost;
using InkCanvasForClass.PluginSdk;

namespace Ink_Canvas.Helpers.Plugins
{
    /// <summary>
    /// 将基于 <see cref="IInkCanvasPlugin"/> 的外部插件适配为宿主统一的 <see cref="IPlugin"/>。
    /// </summary>
    public sealed class SdkPluginAdapter : PluginBase
    {
        private readonly IInkCanvasPlugin _core;
        private readonly string _folderId;

        public SdkPluginAdapter(string folderId, IInkCanvasPlugin core, string mainAssemblyPath)
        {
            _folderId = folderId ?? throw new ArgumentNullException(nameof(folderId));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            PluginPath = mainAssemblyPath ?? string.Empty;
        }

        public string FolderId => _folderId;

        public IInkCanvasPlugin Core => _core;

        public override string PluginStateKey => "SdkFolder:" + _folderId;

        public override string Name => _core.Name;

        public override string Description => _core.Description;

        public override Version Version => _core.Version;

        public override string Author => _core.Author;

        public override bool IsBuiltIn => false;

        public override void Initialize()
        {
            base.Initialize();

            var ctx = PluginRuntime.SdkContext;
            if (ctx == null)
            {
                LogHelper.WriteLogToFile($"SDK 插件 {_core.Name} 初始化失败：宿主上下文未就绪", LogHelper.LogType.Error);
                return;
            }

            _core.Initialize(ctx);

            var registry = PluginManager.Instance.ExtensionRegistry;
            registry.SetCurrentPluginId(_folderId);
            try
            {
                if (_core is InkCanvasPluginBase pluginBase)
                {
                    pluginBase.RegisterExtensions(registry);
                }
            }
            finally
            {
                registry.SetCurrentPluginId(string.Empty);
            }
        }

        public override void Enable()
        {
            if (IsEnabled)
            {
                return;
            }

            if (_core is InkCanvasPluginBase b)
            {
                b.IsEnabled = true;
            }
            else
            {
                _core.Start();
            }

            base.Enable();
        }

        public override void Disable()
        {
            if (!IsEnabled)
            {
                return;
            }

            if (_core is InkCanvasPluginBase b)
            {
                b.IsEnabled = false;
            }
            else
            {
                _core.Stop();
            }

            base.Disable();
        }

        public override UserControl GetSettingsView()
        {
            return _core.GetSettingsView();
        }

        public override void Cleanup()
        {
            try
            {
                if (_core is InkCanvasPluginBase b)
                {
                    b.IsEnabled = false;
                }
                else
                {
                    _core.Stop();
                }

                _core.Cleanup();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"SDK 插件 {_core.Name} Cleanup 出错: {ex.Message}", LogHelper.LogType.Error);
            }

            base.Cleanup();
        }
    }
}
