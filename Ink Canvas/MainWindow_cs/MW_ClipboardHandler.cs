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
        private const int WM_CLIPBOARDUPDATE = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private bool isClipboardMonitoringEnabled;
        private BitmapSource lastClipboardImage;
        private HwndSource _clipboardHwndSource;
        private DateTime _lastPasteNotificationTime = DateTime.MinValue;
        private const int PasteNotificationDebounceSeconds = 4;

        /// <summary>
        /// 启动并配置窗口的剪贴板监控：订阅剪贴板更新事件、设置监控标记，并确保在窗口可用时安装消息钩子以接收剪贴板更新通知。
        /// </summary>
        /// <remarks>
        /// 如果监控已启用则不做任何操作。方法内部在发生异常时会记录错误但不会抛出异常；同时会排队执行 EnsureClipboardHookInstalled 以在窗口句柄就绪后安装钩子。
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
        /// 在可用的窗口句柄上安装用于接收剪贴板更新的窗口钩子；如果钩子已安装或窗口句柄无效则不执行任何操作。
        /// </summary>
        private void EnsureClipboardHookInstalled()
        {
            if (_clipboardHwndSource != null) return;
            var handle = new WindowInteropHelper(this).Handle;
            if (handle == IntPtr.Zero) return;
            OnSourceInitializedForClipboard(this, EventArgs.Empty);
        }

        /// <summary>
        /// 在窗口源可用时为剪贴板更新安装窗口消息钩子并向系统注册剪贴板格式监听器。
        /// </summary>
        /// <remarks>
        /// 该方法会注销自身的 SourceInitialized 事件处理器、获取窗口句柄、在该句柄上添加消息钩子以拦截剪贴板更新消息，并调用 Win32 的 AddClipboardFormatListener 请求剪贴板格式更改通知。若注册失败或发生异常，会记录相应的日志信息。
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
        /// 在剪贴板发生变化时读取剪贴板中的图像并在存在时更新 lastClipboardImage 字段。
        /// </summary>
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
        /// 在进入白板时检查剪贴板是否包含图片，并在满足防抖间隔后显示粘贴提示。
        /// </summary>
        /// <remarks>
        /// 如果剪贴板包含图片且自上次提示已超过配置的防抖秒数，则更新上次提示时间并触发粘贴通知显示；否则不做任何操作。
        /// </remarks>
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
        /// 在界面上显示一条提示，告知用户剪贴板中有图片并可在白板上粘贴。
        /// </summary>
        /// <remarks>
        /// 提示会安排在 UI 线程上显示以确保与界面交互安全。发生异常时会被记录但不会抛出到调用方。
        /// </remarks>
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
        /// 在指定画布位置显示一个用于将剪贴板图像粘贴到 InkCanvas 的右键上下文菜单（仅当剪贴板包含图像时）。
        /// </summary>
        /// <param name="position">用于粘贴操作的目标位置（画布坐标），用于在菜单项触发时将图像放置到该位置。</param>
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

        // 从剪贴板粘贴图片
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



        // 处理白板右键事件
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

        // 处理全局粘贴快捷键
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

        // 清理剪贴板监控
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

    // 剪贴板通知类
    public static class ClipboardNotification
    {
        public static event Action ClipboardUpdate;

        private static string lastClipboardText = "";
        private static bool lastHadImage;

        /// <summary>
        /// 在收到系统剪贴板更改消息时更新内部剪贴板状态并在必要时触发 <c>ClipboardUpdate</c> 事件。
        /// </summary>
        /// <remarks>
        /// 比较当前剪贴板的图像存在性与文本内容与保存的上次状态：当图像存在性发生变化、文本发生变化，或当前包含图像时，更新内部跟踪字段 <c>lastHadImage</c> 和 <c>lastClipboardText</c> 并调用 <c>ClipboardUpdate</c> 事件以通知订阅者。
        /// 此方法会吞并异常并记录错误，不会向调用者抛出异常。
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

        public static void Stop()
        {
        }
    }
}