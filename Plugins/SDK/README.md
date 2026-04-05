# Ink Canvas Plugin SDK

Ink Canvas 插件开发SDK，用于开发墨迹画布应用的插件。

**命名空间**：`InkCanvasForClass.PluginSdk`（避免与 WPF 控件 `System.Windows.Controls.InkCanvas` 在引用 `InkCanvas.*` 时产生歧义）。

## 方案 B：轻量注册表（无 Microsoft.Extensions.DependencyInjection）

宿主在加载每个插件、调用 `Initialize` 之后，会调用 `InkCanvasPluginBase.RegisterExtensions(IPluginRegistry registry)`。插件可在此登记菜单项、工具栏按钮、设置页工厂；宿主窗口稍后统一挂载。注册表实现类型为 `InkCanvasForClass.PluginHost.CollectingPluginRegistry`，由主程序内 `PluginManager.Instance.ExtensionRegistry` 暴露。

## 安装

```bash
dotnet add package InkCanvas.PluginSdk
```

## 快速开始

### 1. 创建插件项目

创建一个新的类库项目，并添加对 `InkCanvas.PluginSdk` 的引用。

### 2. 实现插件接口

```csharp
using InkCanvas.PluginSdk;
using System;
using System.Windows.Controls;

namespace MyPlugin
{
    public class MyPlugin : InkCanvasPluginBase
    {
        public override string Id => "com.example.myplugin";
        public override string Name => "我的插件";
        public override string Description => "这是一个示例插件";
        public override Version Version => new Version(1, 0, 0);
        public override string Author => "插件作者";

        public override void Start()
        {
            // 插件启动时的逻辑
            ShowNotification("插件已启动！", NotificationType.Success);
        }

        public override void Stop()
        {
            // 插件停止时的逻辑
            ShowNotification("插件已停止！", NotificationType.Info);
        }

        public override UserControl GetSettingsView()
        {
            // 返回插件设置界面
            return new MyPluginSettingsView();
        }
    }
}
```

### 3. 插件功能

#### 访问主应用程序功能

通过 `Context` 属性可以访问主应用程序的各种功能：

```csharp
// 获取当前画布
var canvas = Context.CurrentCanvas;

// 设置墨迹颜色
Context.SetInkColor(Colors.Red);

// 清除画布
Context.ClearCanvas();

// 显示通知
Context.ShowNotification("操作完成！", NotificationType.Success);
```

#### 创建菜单项

```csharp
public override IEnumerable<MenuItem> GetMenuItems()
{
    var menuItem = new MenuItem
    {
        Header = "我的功能",
        Icon = new Image { Source = MyIcon }
    };
    menuItem.Click += (s, e) => {
        // 处理菜单点击
        ShowNotification("菜单被点击了！");
    };
    
    return new[] { menuItem };
}
```

#### 创建工具栏按钮

```csharp
public override IEnumerable<Button> GetToolbarButtons()
{
    var button = new Button
    {
        Content = "我的工具",
        ToolTip = "这是一个工具按钮"
    };
    button.Click += (s, e) => {
        // 处理按钮点击
        Context.SetInkColor(Colors.Blue);
    };
    
    return new[] { button };
}
```

#### 事件处理

```csharp
public override void Start()
{
    // 注册事件处理器
    RegisterEventHandler("CanvasChanged", OnCanvasChanged);
    RegisterEventHandler("DrawingModeChanged", OnDrawingModeChanged);
}

private void OnCanvasChanged(object sender, EventArgs e)
{
    ShowNotification("画布已更改");
}

private void OnDrawingModeChanged(object sender, EventArgs e)
{
    ShowNotification($"绘制模式已更改为: {Context.CurrentDrawingMode}");
}
```

## API 参考

### IInkCanvasPlugin 接口

插件必须实现的主要接口。

### IPluginContext 接口

提供对主应用程序功能的访问。

### InkCanvasPluginBase 基类

提供插件的基本实现，建议继承此类。

## 示例插件

查看 `Examples` 文件夹中的示例插件，了解如何实现各种功能。

## 许可证

MIT License
