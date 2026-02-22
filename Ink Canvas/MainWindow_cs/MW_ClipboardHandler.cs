using Ink_Canvas.Helpers;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Clipboard = System.Windows.Clipboard;
using ContextMenu = System.Windows.Controls.ContextMenu;
using Cursors = System.Windows.Input.Cursors;
using MenuItem = System.Windows.Controls.MenuItem;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 剪贴板更新消息常量
        /// </summary>
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        /// <summary>
        /// 添加剪贴板格式监听器
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <summary>
        /// 向指定窗口注册以接收剪贴板内容更改的系统通知。
        /// </summary>
        /// <param name="hwnd">将接收剪贴板更新消息的窗口句柄（HWND）。</param>
        /// <returns>`true` 表示注册成功，`false` 表示注册失败。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// 移除剪贴板格式监听器
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <summary>
        /// 从指定窗口取消注册剪贴板格式监听器。
        /// </summary>
        /// <param name="hwnd">目标窗口的句柄（HWND），用于移除其剪贴板格式监听。</param>
        /// <returns>`true` 表示操作成功，`false` 表示失败（失败时可通过 GetLastError 检查具体错误码）。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// 剪贴板监控启用状态
        /// </summary>
        private bool isClipboardMonitoringEnabled;

        /// <summary>
        /// 最后一次剪贴板图像
        /// </summary>
        private BitmapSource lastClipboardImage;

        /// <summary>
        /// 剪贴板窗口句柄源
        /// </summary>
        private HwndSource _clipboardHwndSource;

        /// <summary>
        /// 最后一次粘贴通知时间
        /// </summary>
        private DateTime _lastPasteNotificationTime = DateTime.MinValue;

        /// <summary>
        /// 粘贴通知防抖时间（秒）
        /// </summary>
        private const int PasteNotificationDebounceSeconds = 4;

        /// <summary>
        /// 启用并初始化对系统剪贴板变更的监控，确保窗口消息钩子在可用时安装并订阅剪贴板更新事件。
        /// </summary>
        /// <remarks>
        /// 在首次调用时订阅内部的 ClipboardNotification.ClipboardUpdate 事件、将监控标志设为已启用，并在窗口句柄可用时安装窗口消息钩子；若句柄尚不可用则延迟到 SourceInitialized 事件完成后安装。此方法会异步调度 EnsureClipboardHookInstalled 以在加载优先级下最终确认钩子已安装。发生异常时记录错误但不会抛出。
        /// <summary>
        /// 启用窗口级的剪贴板监控并在窗口句柄可用时安装用于接收剪贴板更新的消息钩子。
        /// </summary>
        /// <remarks>
        /// 如果已启用监控则不作任何操作；方法会订阅剪贴板更新通知并异步确保窗口消息钩子被安装，从而允许接收 WM_CLIPBOARDUPDATE 消息。
        /// </remarks>
        private void InitializeClipboardMonitoring()
        {
            try
            {
                if (isClipboardMonitoringEnabled)
                    return;

                ClipboardNotification.ClipboardUpdate += OnClipboardUpdate;
                isClipboardMonitoringEnabled = true;

                if (new WindowInteropHelper(this).Handle != IntPtr.Zero)
                    OnSourceInitializedForClipboard(this, EventArgs.Empty);
                else
                    SourceInitialized += OnSourceInitializedForClipboard;
                Dispatcher.BeginInvoke(new Action(EnsureClipboardHookInstalled), DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"初始化剪贴板监控失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// — 在窗口句柄可用且尚未安装钩子时，为接收剪贴板更新消息安装窗口消息钩子。
        /// <summary>
        /// 在窗口句柄可用且尚未安装时，为当前窗口安装用于接收剪贴板更新的窗口消息钩子。
        /// </summary>
        private void EnsureClipboardHookInstalled()
        {
            if (_clipboardHwndSource != null) return;
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;
            OnSourceInitializedForClipboard(this, EventArgs.Empty);
        }

        /// <summary>
        /// 在窗口初始化后安装用于接收系统剪贴板更改消息的窗口钩子并注册剪贴板格式监听器。
        /// </summary>
        /// <remarks>
        /// 将当前窗口的 HwndSource 与 ClipboardWndProc 消息钩子关联，并调用 AddClipboardFormatListener 注册剪贴板更新通知；若无法获取窗口句柄则不执行任何操作。
        /// <summary>
        /// 为当前窗口安装用于接收剪贴板更新的窗口消息钩子并注册系统剪贴板格式监听器，同时取消自身的 SourceInitialized 订阅。 
        /// </summary>
        /// <remarks>
        /// 如果窗口句柄不可用或注册失败，会记录警告或错误信息但不会抛出异常。
        /// </remarks>
        private void OnSourceInitializedForClipboard(object sender, EventArgs e)
        {
            SourceInitialized -= OnSourceInitializedForClipboard;
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle == IntPtr.Zero) return;

                _clipboardHwndSource = HwndSource.FromHwnd(handle);
                _clipboardHwndSource?.AddHook(ClipboardWndProc);

                if (!AddClipboardFormatListener(handle))
                    LogHelper.WriteLogToFile($"AddClipboardFormatListener 失败: {Marshal.GetLastWin32Error()}", LogHelper.LogType.Warning);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"安装剪贴板监听失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理窗口消息，响应剪贴板更新事件
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="msg">消息类型</param>
        /// <param name="wParam">消息参数W</param>
        /// <param name="lParam">消息参数L</param>
        /// <param name="handled">消息是否已处理</param>
        /// <returns>处理结果</returns>
        /// <remarks>
        /// - 当收到剪贴板更新消息时，通知剪贴板变更
        /// - 标记消息为已处理
        /// <summary>
        /// 处理窗口消息；当接收到 WM_CLIPBOARDUPDATE 时，调度剪贴板更新通知并将消息标记为已处理。
        /// </summary>
        /// <param name="msg">窗口消息的标识符。</param>
        /// <param name="handled">当方法处理了该消息时将被设置为 <c>true</c>。</param>
        /// <returns>始终返回 <see cref="IntPtr.Zero"/>。</returns>
        private IntPtr ClipboardWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                Dispatcher.BeginInvoke(new Action(() => ClipboardNotification.NotifyFromMessage()), DispatcherPriority.Background);
                handled = true;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 在剪贴板内容变化时检查剪贴板是否包含图像并缓存该图像。
        /// </summary>
        /// <remarks>
        /// 如果剪贴板包含图像，则读取该图像并更新字段 <c>lastClipboardImage</c>；否则不做任何操作。方法内部会捕获异常并记录日志，不会向上抛出。 
        /// <summary>
        /// 检查系统剪贴板是否包含图像，并在有图像时将其缓存到 lastClipboardImage 字段。
        /// </summary>
        /// <remarks>
        /// 如果剪贴板不包含图像则不做任何操作；在读取或访问剪贴板时发生的异常会被捕获并记录，但不会向调用者抛出。
        /// </remarks>
        private void OnClipboardUpdate()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return;

                var clipboardImage = Clipboard.GetImage();
                if (clipboardImage != null)
                    lastClipboardImage = clipboardImage;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理剪贴板更新失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 在进入白板时检查系统剪贴板是否包含图片；如果存在图片且与上次提示间隔超过预设节流时间，则显示粘贴提示。
        /// <summary>
        /// 在进入白板时检查剪贴板是否包含图片；若包含且距离上次提示已超过节流间隔，则更新上次提示时间并显示“粘贴图片”通知。
        /// </summary>
        public void CheckClipboardImageAndShowPasteNotificationWhenEnteringBoard()
        {
            try
            {
                if (!Clipboard.ContainsImage())
                    return;

                bool debounceElapsed = (DateTime.Now - _lastPasteNotificationTime).TotalSeconds >= PasteNotificationDebounceSeconds;
                if (!debounceElapsed)
                    return;

                _lastPasteNotificationTime = DateTime.Now;
                ShowPasteNotification();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"进入白板时检测剪贴板失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 在界面上显示提示，告知用户剪贴板中存在图片并可在白板上右键粘贴。
        /// <summary>
        /// 在 UI 线程显示提醒用户剪贴板包含图片的通知。
        /// </summary>
        private void ShowPasteNotification()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    ShowNotification("检测到剪贴板中有图片，右键点击白板可粘贴");
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"显示粘贴提示失败: {ex.Message}", LogHelper.LogType.Error);
                }
            }), DispatcherPriority.Normal);
        }

        /// <summary>
        /// 在指定位置显示包含“粘贴图片”项的右键菜单（仅在剪贴板包含图片时显示）。
        /// </summary>
        /// <summary>
        /// 在画布上显示一个针对剪贴板图像的右键上下文菜单，提供“粘贴图片”操作。
        /// </summary>
        /// <param name="position">用于定位菜单和作为粘贴时图片中心点的画布坐标。</param>
        private void ShowPasteContextMenu(Point position)
        {
            try
            {
                if (!Clipboard.ContainsImage()) return;

                // 创建右键菜单
                var contextMenu = new ContextMenu();

                var pasteMenuItem = new MenuItem
                {
                    Header = "粘贴图片"
                };

                pasteMenuItem.Click += async (s, e) => await PasteImageFromClipboard(position);
                contextMenu.Items.Add(pasteMenuItem);

                // 显示菜单
                contextMenu.IsOpen = true;
                contextMenu.PlacementTarget = inkCanvas;
                contextMenu.Placement = PlacementMode.MousePoint;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"显示粘贴菜单失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 从剪贴板粘贴图片到画布
        /// </summary>
        /// <param name="position">粘贴位置（可选）</param>
        /// <returns>异步任务</returns>
        /// <remarks>
        /// - 检查剪贴板是否包含图片
        /// - 创建Image控件并设置属性
        /// - 生成唯一名称
        /// - 初始化变换组
        /// - 设置图片属性，避免被InkCanvas选择系统处理
        /// - 添加到画布
        /// - 等待图片加载完成后进行居中处理
        /// - 如果有指定位置，调整到指定位置
        /// - 绑定事件处理器
        /// - 提交到历史记录
        /// - 插入图片后切换到选择模式并刷新浮动栏高光显示
        /// - 显示通知
        /// - 包含异常处理
        /// <summary>
        /// 将剪贴板中的图片粘贴到画布上并为其初始化变换与交互行为。
        /// </summary>
        /// <param name="position">可选的粘贴中心位置；若提供则以该点为图片中心放置，未提供则居中显示。</param>
        /// <remarks>
        /// 如果剪贴板不包含图片或无法读取图片，会显示相应通知并中止操作。成功粘贴后会将图片添加到 inkCanvas、初始化渲染变换与鼠标/触摸事件处理器、将操作提交到历史记录，并切换为选择工具模式与显示成功通知。
        /// </remarks>
        private async Task PasteImageFromClipboard(Point? position = null)
        {
            try
            {
                if (!Clipboard.ContainsImage())
                {
                    ShowNotification("剪贴板中没有图片");
                    return;
                }

                var clipboardImage = Clipboard.GetImage();
                if (clipboardImage == null)
                {
                    ShowNotification("无法获取剪贴板图片");
                    return;
                }

                // 创建Image控件
                var image = new Image
                {
                    Source = clipboardImage,
                    Width = clipboardImage.PixelWidth,
                    Height = clipboardImage.PixelHeight,
                    Stretch = Stretch.Fill
                };

                // 生成唯一名称
                string timestamp = "img_clipboard_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                image.Name = timestamp;

                // 初始化TransformGroup
                if (image is FrameworkElement element)
                {
                    var transformGroup = new TransformGroup();
                    transformGroup.Children.Add(new ScaleTransform(1, 1));
                    transformGroup.Children.Add(new TranslateTransform(0, 0));
                    transformGroup.Children.Add(new RotateTransform(0));
                    element.RenderTransform = transformGroup;
                }

                // 设置图片属性，避免被InkCanvas选择系统处理
                image.IsHitTestVisible = true;
                image.Focusable = false;

                // 初始化InkCanvas选择设置
                if (inkCanvas != null)
                {
                    // 清除当前选择，避免显示控制点
                    inkCanvas.Select(new StrokeCollection());
                    // 设置编辑模式为非选择模式，这样可以保持图片的交互功能
                    // 同时通过图片的IsHitTestVisible和Focusable属性来避免InkCanvas选择系统的干扰
                    inkCanvas.EditingMode = InkCanvasEditingMode.None;
                }

                // 添加到画布
                inkCanvas.Children.Add(image);

                // 等待图片加载完成后再进行居中处理
                image.Loaded += (sender, e) =>
                {
                    // 确保在UI线程中执行
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        // 先进行缩放居中处理
                        CenterAndScaleElement(image);

                        // 如果有指定位置，调整到指定位置
                        if (position.HasValue)
                        {
                            // 在指定位置居中显示
                            InkCanvas.SetLeft(image, position.Value.X - image.Width / 2);
                            InkCanvas.SetTop(image, position.Value.Y - image.Height / 2);
                        }

                        // 绑定事件处理器
                        if (image is FrameworkElement elementForEvents)
                        {
                            // 鼠标事件
                            elementForEvents.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                            elementForEvents.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                            elementForEvents.MouseMove += Element_MouseMove;
                            elementForEvents.MouseWheel += Element_MouseWheel;

                            // 触摸事件
                            elementForEvents.TouchDown += Element_TouchDown;
                            elementForEvents.TouchUp += Element_TouchUp;
                            elementForEvents.IsManipulationEnabled = true;
                            elementForEvents.ManipulationDelta += Element_ManipulationDelta;
                            elementForEvents.ManipulationCompleted += Element_ManipulationCompleted;

                            // 设置光标
                            elementForEvents.Cursor = Cursors.Hand;
                        }
                    }), DispatcherPriority.Loaded);
                };

                // 提交到历史记录
                timeMachine.CommitElementInsertHistory(image);

                // 插入图片后切换到选择模式并刷新浮动栏高光显示
                SetCurrentToolMode(InkCanvasEditingMode.Select);
                UpdateCurrentToolMode("select");
                HideSubPanels("select");

                ShowNotification("图片已从剪贴板粘贴");
            }
            catch (Exception ex)
            {
                ShowNotification($"粘贴图片失败: {ex.Message}");
                LogHelper.WriteLogToFile($"粘贴图片失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }



        /// <summary>
        /// 处理白板右键事件，显示粘贴图片菜单
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 只在白板模式下处理
        /// - 检查是否有图片在剪贴板中
        /// - 显示粘贴上下文菜单
        /// - 包含异常处理
        /// <summary>
        /// 当处于白板模式且在画布上发生鼠标右键抬起时，如果剪贴板包含图片，显示“粘贴图片”上下文菜单。
        /// </summary>
        private void InkCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 只在白板模式下处理
                if (currentMode != 1) return;

                // 检查是否有图片在剪贴板中
                if (Clipboard.ContainsImage())
                {
                    var position = e.GetPosition(inkCanvas);
                    ShowPasteContextMenu(position);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理右键事件失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理全局粘贴快捷键，粘贴剪贴板中的图片
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 只在白板模式下处理
        /// - 检查剪贴板是否包含图片
        /// - 从剪贴板粘贴图片
        /// - 包含异常处理
        /// <summary>
        /// 作为全局粘贴命令的事件处理器：仅在白板模式且剪贴板包含图片时，将剪贴板中的图片粘贴到画布上。
        /// </summary>
        internal async void HandleGlobalPaste(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                // 只在白板模式下处理
                if (currentMode != 1) return;

                if (Clipboard.ContainsImage())
                {
                    await PasteImageFromClipboard();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理全局粘贴失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 清理剪贴板监控资源
        /// </summary>
        /// <remarks>
        /// - 取消订阅剪贴板更新事件
        /// - 移除剪贴板格式监听器
        /// - 移除窗口消息钩子
        /// - 重置相关变量
        /// - 包含异常处理
        /// <summary>
        /// 停止并清理与窗口相关的剪贴板监控资源。
        /// </summary>
        /// <remarks>
        /// 如果已启用，则取消订阅剪贴板更新事件、移除窗口消息钩子并注销剪贴板格式监听器；发生异常时将错误写入日志。
        /// </remarks>
        private void CleanupClipboardMonitoring()
        {
            try
            {
                if (isClipboardMonitoringEnabled)
                {
                    ClipboardNotification.ClipboardUpdate -= OnClipboardUpdate;
                    isClipboardMonitoringEnabled = false;
                }

                var handle = new WindowInteropHelper(this).Handle;
                if (handle != IntPtr.Zero)
                    RemoveClipboardFormatListener(handle);

                _clipboardHwndSource?.RemoveHook(ClipboardWndProc);
                _clipboardHwndSource = null;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"清理剪贴板监控失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }
    }

    /// <summary>
    /// 剪贴板通知类，用于监控剪贴板变化
    /// </summary>
    public static class ClipboardNotification
    {
        /// <summary>
        /// 剪贴板更新事件
        /// </summary>
        public static event Action ClipboardUpdate;

        /// <summary>
        /// 最后一次剪贴板文本
        /// </summary>
        private static string lastClipboardText = "";

        /// <summary>
        /// 最后一次是否有图片
        /// </summary>
        private static bool lastHadImage;

        /// <summary>
        /// 检查当前系统剪贴板的文本与图像状态，并在检测到相关变化或存在图像时触发 <see cref="ClipboardUpdate"/> 事件以通知订阅者。
        /// </summary>
        /// <remarks>
        /// 会比较当前剪贴板的图像存在性与文本内容与内部缓存的上一状态；若图像存在性发生变化、文本内容发生变化，或当前存在图像，则更新缓存并调用 <see cref="ClipboardUpdate"/>。方法内部捕获异常并将错误记录到日志，而不是向调用方抛出异常。
        /// <summary>
        /// 检查当前剪贴板的图像与文本状态，并在相关变化或存在图像时触发 ClipboardUpdate 事件。
        /// </summary>
        /// <remarks>
        /// 更新内部缓存的剪贴板文本和图像存在标记，并在下列任一情况触发 <see cref="ClipboardNotification.ClipboardUpdate"/>：
        /// - 图像存在状态与上次不同；
        /// - 剪贴板文本与上次不同；
        /// - 当前剪贴板包含图像。
        /// 该方法内部捕获异常并记录，不会向调用方抛出异常。
        /// </remarks>
        public static void NotifyFromMessage()
        {
            try
            {
                bool currentHasImage = Clipboard.ContainsImage();
                string currentText = Clipboard.ContainsText() ? Clipboard.GetText() : "";

                if (currentHasImage != lastHadImage || currentText != lastClipboardText || currentHasImage)
                {
                    lastHadImage = currentHasImage;
                    lastClipboardText = currentText;
                    ClipboardUpdate?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"剪贴板 NotifyFromMessage 异常: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 停止剪贴板监控
        /// </summary>
        /// <remarks>
        /// 当前实现为空方法，预留用于未来扩展
        /// <summary>
        /// 停止 ClipboardNotification 的运行并释放与剪贴板监听相关的资源（当前为无操作实现）。
        /// </summary>
        /// <remarks>
        /// 该方法预留用于将来在需要时终止或清理剪贴板通知的订阅与资源；目前调用不会产生副作用。
        /// </remarks>
        public static void Stop()
        {
        }
    }
}