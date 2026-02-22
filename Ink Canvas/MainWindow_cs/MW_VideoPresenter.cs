using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using Ink_Canvas.Helpers;
using Ink_Canvas.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        // 标记：用于在保存/恢复白板内容时排除“展台实时上屏”画面
        private const string VideoPresenterLiveFrameTag = "__VideoPresenterLiveFrame";

        private CameraService _cameraService;
        private readonly object _videoPresenterFrameLock = new object();
        private Bitmap _lastFrame;

        private readonly List<CapturedImage> _capturedPhotos = new List<CapturedImage>();
        private const int MaxCapturedPhotos = 50; // 容量上限：比 UI 显示的 30 项多一些，避免频繁清理

        // 按页绑定：每一页对应一个“实时画面”元素与布局/设备信息
        private readonly Dictionary<int, System.Windows.Controls.Image> _liveFrameImageByPage = new Dictionary<int, System.Windows.Controls.Image>();
        private readonly HashSet<int> _liveEnabledPages = new HashSet<int>();
        private readonly Dictionary<int, int> _cameraIndexByPage = new Dictionary<int, int>();
        private readonly Dictionary<int, (double left, double top, double width)> _liveFrameLayoutByPage =
            new Dictionary<int, (double left, double top, double width)>();

        private DateTime _lastCaptureTime = DateTime.MinValue;
        private const int VideoPresenterCaptureCooldownMs = 1000;

        private const int CorrectedPaperHeight = 600;

        /// <summary>
        /// 切换视频呈现侧边栏的显示状态（显示或隐藏）。
        /// </summary>
        /// <param name="sender">触发事件的源对象。</param>
        /// <summary>
        /// 切换（显示或隐藏）视频演示侧边栏的可见性。
        /// </summary>
        private void BtnToggleVideoPresenter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleVideoPresenterSidebar();
        }

        /// <summary>
        /// 切换视频演示侧栏的显示状态并在显示时初始化相关控件与状态。
        /// </summary>
        /// <remarks>
        /// 当侧栏被显示时：确保摄像头服务已初始化、暂时禁用拍照按钮、刷新可用摄像头列表，并将“照片校正”和当前页面的“上屏（live on canvas）”开关同步为保存的设置或页面状态；
        /// 当侧栏被隐藏时：将其折叠并停止进一步初始化操作。
        /// <summary>
        /// 切换视频展示侧边栏的可见性；在展开侧边栏时初始化相机服务并同步相关界面状态。
        /// </summary>
        /// <remarks>
        /// 当侧边栏被隐藏时将其折叠；当侧边栏被显示时，会确保 CameraService 已初始化、临时禁用拍照按钮、刷新可用摄像头设备列表，并将照片矫正开关与当前页面的“上屏”开关状态同步到界面上。
        /// </remarks>
        private void ToggleVideoPresenterSidebar()
        {
            if (VideoPresenterSidebar == null) return;

            if (VideoPresenterSidebar.Visibility == Visibility.Visible)
            {
                VideoPresenterSidebar.Visibility = Visibility.Collapsed;
                return;
            }

            VideoPresenterSidebar.Visibility = Visibility.Visible;
            EnsureCameraService();
            if (BtnCapturePhoto != null) BtnCapturePhoto.IsEnabled = false;
            RefreshVideoPresenterDeviceList();

            if (ToggleBtnPhotoCorrection != null)
            {
                ToggleBtnPhotoCorrection.IsChecked = Settings?.Automation?.IsEnablePhotoCorrection ?? false;
            }

            // 同步“上屏”按钮状态（按页绑定）
            if (BtnToggleVideoPresenterLiveOnCanvas != null)
            {
                BtnToggleVideoPresenterLiveOnCanvas.IsChecked = _liveEnabledPages.Contains(GetCurrentPageIndex());
            }
        }

        /// <summary>
        /// 关闭视频呈现侧边栏（将其可见性设为 Collapsed）。
        /// <summary>
        /// 隐藏视频展示侧边栏，将其可见性设为 Collapsed。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">路由事件参数。</param>
        private void BtnCloseVideoPresenter_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPresenterSidebar != null)
            {
                VideoPresenterSidebar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 延迟初始化摄像头服务并订阅其帧和错误事件；如果服务已存在则不做任何操作。
        /// <summary>
        /// 确保 _cameraService 已初始化；如果尚未创建则实例化 CameraService 并订阅 FrameReceived 与 ErrorOccurred 事件。
        /// </summary>
        private void EnsureCameraService()
        {
            if (_cameraService != null) return;

            _cameraService = new CameraService();
            _cameraService.FrameReceived += CameraService_FrameReceived;
            _cameraService.ErrorOccurred += CameraService_ErrorOccurred;
        }

        /// <summary>
        /// 在相机服务发生错误时将错误信息写入错误日志文件。
        /// </summary>
        /// <summary>
        /// 处理 CameraService 报告的错误并将错误信息写入日志文件。
        /// </summary>
        /// <param name="e">来自相机服务的错误描述，会被写入错误日志。</param>
        private void CameraService_ErrorOccurred(object sender, string e)
        {
            try
            {
                LogHelper.WriteLogToFile($"视频展台摄像头错误: {e}", LogHelper.LogType.Error);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 处理来自摄像头的单帧图像，用于更新预览、缓存最新帧并刷新当前页的实时画面显示。
        /// </summary>
        /// <param name="frame">来自摄像头的位图帧；为 null 时忽略。</param>
        /// <remarks>
        /// 缓存该帧为最新帧、更新预览控件的图像来源、启用拍照按钮并尝试在当前白板页上刷新实时画面。
        /// <summary>
        /// 处理来自 CameraService 的新帧：缓存最新位图并在 UI 线程上刷新侧栏预览、启用拍照按钮，并在需要时更新画布上的实时画面元素。
        /// </summary>
        /// <param name="sender">事件发送者（CameraService 实例）。</param>
        /// <param name="frame">收到的相机帧位图；为 null 时将被忽略。</param>
        /// <remarks>
        /// 方法会为内部使用而克隆并缓存一份最新帧的副本，以线程安全的方式更新内部缓冲（_lastFrame）。随后将帧转换为 WPF 可用的 BitmapImage，并在 UI 线程上：
        /// - 更新 VideoPresenterPreviewImage 的 Source（若存在），
        /// - 启用截图按钮 BtnCapturePhoto（若存在），
        /// - 调用 TryUpdateLiveFrameOnCanvas 刷新当前页的画面元素（若启用）。
        /// 如果帧不可克隆或转换失败，方法会静默忽略该帧而不抛出异常。
        /// </remarks>
        private void CameraService_FrameReceived(object sender, Bitmap frame)
        {
            if (frame == null) return;

            try
            {
                Bitmap serviceCopy;
                try
                {
                    serviceCopy = (Bitmap)frame.Clone();
                }
                catch
                {
                    // 可能在下一帧到来时被 CameraService 释放，直接忽略这一帧
                    return;
                }

                lock (_videoPresenterFrameLock)
                {
                    _lastFrame?.Dispose();
                    _lastFrame = (Bitmap)serviceCopy.Clone();
                }

                var preview = ConvertBitmapToBitmapImage(serviceCopy);
                serviceCopy.Dispose();
                if (preview == null) return;

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (VideoPresenterPreviewImage != null)
                    {
                        VideoPresenterPreviewImage.Source = preview;
                    }

                    if (BtnCapturePhoto != null)
                    {
                        BtnCapturePhoto.IsEnabled = true;
                    }

                    // 实时上屏：刷新当前页的画面元素
                    TryUpdateLiveFrameOnCanvas(preview);
                }));
            }
            catch
            {
                // 忽略预览刷新异常
            }
        }

        /// <summary>
        /// 获取当前白板页索引（确保返回值至少为 1）。
        /// </summary>
        /// <summary>
        /// 获取当前白板页的索引（保证最小为 1）。
        /// </summary>
        /// <returns>当前白板页索引，若内部索引小于 1 则返回 1。</returns>
        private int GetCurrentPageIndex()
        {
            return Math.Max(1, CurrentWhiteboardIndex);
        }

        /// <summary>
        /// 在当前白板页面（若已启用）将给定预览图像应用到页面上的实时摄像框元素，并确保该元素已添加到画布且可见。
        /// </summary>
        /// <remarks>
        /// 如果当前页面未启用实时显示，或画布/对应图像元素不可用，则函数不执行任何操作。
        /// <summary>
        /// 将侧边预览图像应用到当前页面的在画布上的实时画面元素（若已启用且存在）。 
        /// </summary>
        /// <param name="preview">要显示的预览图像；传入 null 可清空当前实时画面的 Source。</param>
        private void TryUpdateLiveFrameOnCanvas(BitmapImage preview)
        {
            try
            {
                int page = GetCurrentPageIndex();
                if (!_liveEnabledPages.Contains(page)) return;
                if (inkCanvas == null) return;
                if (!_liveFrameImageByPage.TryGetValue(page, out var img) || img == null) return;

                if (!inkCanvas.Children.Contains(img))
                {
                    inkCanvas.Children.Add(img);
                }

                img.Source = preview;
                img.Visibility = Visibility.Visible;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        private const double VideoPresenterLiveFrameScreenRatio = 0.75;

        /// <summary>
        /// 获取或创建并缓存用于指定白板页的实时视频帧 Image 元素。
        /// </summary>
        /// <param name="page">白板页索引（页面编号，用于在每页间区分并缓存元素）。</param>
        /// <summary>
        /// 确保并返回指定页对应的直播帧 Image 元素。
        /// </summary>
        /// <param name="page">要获取或创建 Image 的页面索引。</param>
        /// <returns>`Image` 实例：若已存在则返回缓存的实例，否则创建、缓存并返回新的 Image。</returns>
        private System.Windows.Controls.Image EnsureLiveFrameElementForPage(int page)
        {
            if (_liveFrameImageByPage.TryGetValue(page, out var existing) && existing != null) return existing;

            double canvasW = inkCanvas?.ActualWidth ?? 0;
            double canvasH = inkCanvas?.ActualHeight ?? 0;
            double w = canvasW > 10 && canvasH > 10
                ? canvasW * VideoPresenterLiveFrameScreenRatio
                : 520;
            double h = canvasW > 10 && canvasH > 10
                ? canvasH * VideoPresenterLiveFrameScreenRatio
                : 390;

            var img = new System.Windows.Controls.Image
            {
                Tag = VideoPresenterLiveFrameTag,
                Stretch = System.Windows.Media.Stretch.Uniform,
                Width = w,
                Height = h,
                Visibility = Visibility.Visible,
                Opacity = 1.0
            };
            try
            {
                InitializeElementTransform(img);
                BindElementEvents(img);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

            _liveFrameImageByPage[page] = img;
            return img;
        }

        /// <summary>
        /// 将已保存的布局（或默认布局）应用到指定白板页面上的直播帧 Image 元素，设置其位置和尺寸并确保坐标有效。
        /// </summary>
        /// <param name="page">目标白板页面的索引。</param>
        /// <param name="img">要应用布局的 Image 元素；为 null 时不执行任何操作。</param>
        /// <remarks>
        /// 如果存在为该页面保存的布局则使用其宽度和左/上坐标；否则将 Image 调整为画布尺寸的 75% 并居中。最终位置会限制为不小于 0 的坐标，且对无效计算结果使用合理的默认偏移。
        /// <summary>
        /// 为指定页的直播帧图片应用保存的布局；若无保存布局则使用默认居中大小和位置并进行合法性修正。
        /// </summary>
        /// <param name="page">目标页的页码（从 1 开始）。</param>
        /// <param name="img">要应用布局的 InkCanvas 上的 Image 元素（直播帧）。</param>
        /// <remarks>
        /// - 若存在保存的布局，则恢复其宽度、左/上坐标（坐标会限制为不小于 0）。  
        /// - 若不存在保存布局，则将图片尺寸设为画布宽高的 75%（如可用），并在画布中心定位。  
        /// - 会处理 NaN 或 Infinity 值并替换为合理默认位置，且对 null 的 img 立即返回。  
        /// </remarks>
        private void ApplyLiveFrameLayoutForPage(int page, System.Windows.Controls.Image img)
        {
            if (img == null) return;

            if (_liveFrameLayoutByPage.TryGetValue(page, out var layout))
            {
                if (!double.IsNaN(layout.width) && layout.width > 10) img.Width = layout.width;
                InkCanvas.SetLeft(img, Math.Max(0, layout.left));
                InkCanvas.SetTop(img, Math.Max(0, layout.top));
                return;
            }

            // 默认尺寸：画布宽高的 75%；位置居中
            double cw = inkCanvas?.ActualWidth ?? 0;
            double ch = inkCanvas?.ActualHeight ?? 0;
            if (cw > 10 && ch > 10)
            {
                img.Width = cw * VideoPresenterLiveFrameScreenRatio;
                img.Height = ch * VideoPresenterLiveFrameScreenRatio;
            }
            double x = (inkCanvas?.ActualWidth ?? 0) / 2 - img.Width / 2;
            double y = (inkCanvas?.ActualHeight ?? 0) / 2 - img.Height / 2;
            if (double.IsNaN(x) || double.IsInfinity(x)) x = 100;
            if (double.IsNaN(y) || double.IsInfinity(y)) y = 100;
            InkCanvas.SetLeft(img, Math.Max(0, x));
            InkCanvas.SetTop(img, Math.Max(0, y));
        }

        /// <summary>
        /// 刷新视频呈现器侧栏中的摄像头设备列表并在界面上显示可选项。
        /// </summary>
        /// <remarks>
        /// 若未检测到摄像头，会在面板中显示提示文本；若存在设备，则为每个设备创建一个用于选择的单选按钮，选择某项会启动对应的摄像头预览。函数在列表生成后会尝试恢复并启动当前页面在 _cameraIndexByPage 中存储的摄像头索引，仅当没有保存的索引时才会选择并启动第一个可用设备。保存的每页选择优先于默认选择第一个设备。
        /// <summary>
        /// 刷新侧边栏中的摄像头设备列表并为当前页面恢复或启动相应的预览。
        /// </summary>
        /// <remarks>
        /// 更新 CameraService 的可用摄像头列表，重建 CameraDevicesStackPanel 的子项：在无设备时显示提示文本，否则为每个设备创建 RadioButton 并绑定选择时启动该设备的预览。若当前页面保存有已选摄像头则预选该项，否则选择第一个设备并尝试启动其预览。
        /// </remarks>
        private void RefreshVideoPresenterDeviceList()
        {
            if (_cameraService == null) return;
            if (CameraDevicesStackPanel == null) return;

            _cameraService.RefreshCameraList();
            CameraDevicesStackPanel.Children.Clear();

            if (_cameraService.AvailableCameras == null || _cameraService.AvailableCameras.Count == 0)
            {
                var tb = new TextBlock
                {
                    Text = "未检测到摄像头设备",
                    FontSize = 12,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                tb.SetResourceReference(TextBlock.ForegroundProperty, "FloatBarForeground");
                CameraDevicesStackPanel.Children.Add(tb);
                return;
            }

            for (int i = 0; i < _cameraService.AvailableCameras.Count; i++)
            {
                int idx = i;
                var dev = _cameraService.AvailableCameras[i];
                var rb = new RadioButton
                {
                    Content = dev.Name,
                    Margin = new Thickness(0, 2, 0, 2),
                    FontSize = 12,
                    Tag = idx,
                };
                rb.SetResourceReference(Control.ForegroundProperty, "FloatBarForeground");
                rb.Checked += (s, e) => StartVideoPresenterPreview(idx);
                CameraDevicesStackPanel.Children.Add(rb);
            }

            // 预选该页已保存的摄像头，否则使用第一个
            if (_cameraService.AvailableCameras.Count > 0)
            {
                int currentPage = GetCurrentPageIndex();
                int cameraToSelect = 0;
                if (_cameraIndexByPage.TryGetValue(currentPage, out int savedIdx) && savedIdx >= 0 && savedIdx < _cameraService.AvailableCameras.Count)
                {
                    cameraToSelect = savedIdx;
                }

                if (cameraToSelect < CameraDevicesStackPanel.Children.Count && CameraDevicesStackPanel.Children[cameraToSelect] is RadioButton rb)
                {
                    rb.IsChecked = true;
                }
                else
                {
                    StartVideoPresenterPreview(cameraToSelect);
                }
            }
        }

        /// <summary>
        /// 为当前白板页开始指定摄像头的预览并保存该页的摄像头选择。
        /// </summary>
        /// <param name="cameraIndex">要启动的摄像头在设备列表中的索引。</param>
        /// <summary>
        /// 为当前页面启动指定摄像头的实时预览并保存该页面的摄像头选择。
        /// </summary>
        /// <param name="cameraIndex">要用于预览的摄像头在设备列表中的索引。</param>
        /// <remarks>预览成功时会使拍照按钮可用；启动失败会被捕获并记录。</remarks>
        private void StartVideoPresenterPreview(int cameraIndex)
        {
            try
            {
                EnsureCameraService();
                _cameraIndexByPage[GetCurrentPageIndex()] = cameraIndex;
                if (_cameraService.StartPreview(cameraIndex))
                {
                    if (BtnCapturePhoto != null) BtnCapturePhoto.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"启动视频展台预览失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 在当前白板页面启用“在画布上显示实时视频”功能并将对应的实时画面元素添加到画布上。
        /// </summary>
        /// <remarks>
        /// 确保为当前页面创建并应用已保存的布局，将实时画面 Image 加入 inkCanvas（若尚未存在），并尝试切换编辑工具为选择模式。若侧栏预览已有帧，则立即用该预览刷新画布上的实时画面图像源。
        /// <summary>
        /// 在当前页启用“在画布上显示实时视频”并将对应的直播帧图像准备并显示到画布上。
        /// </summary>
        /// <remarks>
        /// 将当前页标记为启用实时画面，确保并布局该页的 Image 元素，若尚未添加则插入到 inkCanvas；切换编辑工具到选择模式并隐藏相关子面板；若侧栏预览存在则用其图像作为画布实时帧的源。
        /// </remarks>
        private void BtnToggleVideoPresenterLiveOnCanvas_Checked(object sender, RoutedEventArgs e)
        {
            int page = GetCurrentPageIndex();
            _liveEnabledPages.Add(page);

            var img = EnsureLiveFrameElementForPage(page);
            ApplyLiveFrameLayoutForPage(page, img);

            if (inkCanvas != null && !inkCanvas.Children.Contains(img))
            {
                inkCanvas.Children.Add(img);
            }

            try
            {
                SetCurrentToolMode(InkCanvasEditingMode.Select);
                UpdateCurrentToolMode("select");
                HideSubPanels("select");
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

            // 立即用侧栏预览刷新一次
            if (VideoPresenterPreviewImage?.Source is BitmapImage bi)
            {
                img.Source = bi;
            }
        }

        /// <summary>
        /// 在当前页面禁用画布上的实时视频覆盖并移除其视觉元素。
        /// </summary>
        /// <remarks>
        /// 从记录已启用实时显示的集合中删除当前页面索引，并在存在对应的 Image 元素且已添加到 inkCanvas 时尝试将其移除。
        /// <summary>
        /// 在当前页禁用画布上的实时视频预览，并从 inkCanvas 中移除该页对应的 Image 元素（如存在）。
        /// </summary>
        private void BtnToggleVideoPresenterLiveOnCanvas_Unchecked(object sender, RoutedEventArgs e)
        {
            int page = GetCurrentPageIndex();
            _liveEnabledPages.Remove(page);

            if (_liveFrameImageByPage.TryGetValue(page, out var img) && img != null)
            {
                try
                {
                    if (inkCanvas != null && inkCanvas.Children.Contains(img))
                    {
                        inkCanvas.Children.Remove(img);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
        }

        /// <summary>
        /// 在离开当前白板页之前保存该页实时视频画面在画布上的位置和宽度（按页索引进行存储）。
        /// </summary>
        /// <remarks>
        /// 若画面元素的 Left 或 Top 为 NaN，则按 0 处理；保存的数据格式为 (left, top, width) 到页面布局映射中供后续恢复使用。
        /// <summary>
        /// 保存当前页的画布上视频预览元素的位置和宽度，以便离开页面后恢复该布局。
        /// </summary>
        /// <remarks>
        /// 如果找不到当前页的预览元素则不做任何操作；若元素的 Left/Top 为 NaN，则将其视为 0 后保存。
        /// 保存的值写入字段 _liveFrameLayoutByPage，格式为 (left, top, width)。
        /// </remarks>
        private void VideoPresenter_BeforePageLeave()
        {
            try
            {
                int page = GetCurrentPageIndex();
                if (!_liveFrameImageByPage.TryGetValue(page, out var img) || img == null) return;

                double left = InkCanvas.GetLeft(img);
                double top = InkCanvas.GetTop(img);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                _liveFrameLayoutByPage[page] = (left, top, img.Width);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 在页面切换后恢复该页的实时画面状态并同步相关设备与 UI 控件状态。
        /// </summary>
        /// <remarks>
        /// 同步“上屏”切换按钮状态；若当前页启用了在画布上显示实时画面，则确保并布局对应的 Image 元素并用当前预览图像填充其 Source；同时恢复该页保存的摄像头索引并启动对应摄像头预览。
        /// <summary>
        /// 在页码改变时同步并恢复当前页的视频呈现状态和画面预览。
        /// </summary>
        /// <remarks>
        /// - 将“上屏”切换按钮状态与当前页的上屏设置同步。 
        /// - 如果当前页已启用上屏：确保并添加该页的实时帧 Image 元素到 inkCanvas，应用已保存的布局，并将侧栏预览图像复制到画面元素上。 
        /// - 如存在为该页保存的摄像头索引，则确保摄像头服务已初始化并启动对应的摄像头预览。 
        /// 此方法不会抛出异常到调用方；内部异常仅写入调试输出。
        /// </remarks>
        private void VideoPresenter_OnPageChanged()
        {
            try
            {
                int page = GetCurrentPageIndex();

                // 同步“上屏”按钮状态
                if (BtnToggleVideoPresenterLiveOnCanvas != null)
                {
                    BtnToggleVideoPresenterLiveOnCanvas.IsChecked = _liveEnabledPages.Contains(page);
                }

                // 若该页上屏，恢复画面元素（RestoreStrokes 会清空 inkCanvas.Children）
                if (_liveEnabledPages.Contains(page))
                {
                    var img = EnsureLiveFrameElementForPage(page);
                    ApplyLiveFrameLayoutForPage(page, img);
                    if (inkCanvas != null && !inkCanvas.Children.Contains(img))
                    {
                        inkCanvas.Children.Add(img);
                    }

                    if (VideoPresenterPreviewImage?.Source is BitmapImage bi)
                    {
                        img.Source = bi;
                    }
                }

                // 按页摄像头索引：切页后自动切回该页的摄像头
                if (_cameraIndexByPage.TryGetValue(page, out int idx))
                {
                    EnsureCameraService();
                    _cameraService?.StartPreview(idx);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 处理“拍照”按钮的点击：捕获当前视频帧并将照片加入捕获列表，随后刷新捕获照片的显示。
        /// </summary>
        /// <remarks>
        /// - 在拍照前会检查并强制执行最小冷却时间，防止短时间内重复拍照。
        /// - 如果用户已启用照片纠正，会尝试检测纸张轮廓并对照片做透视校正再保存。  
        /// - 照片处理在后台线程完成，最终的列表更新和 UI 刷新在 UI 线程上执行。  
        /// - 发生异常时会记录错误日志，不会向上抛出异常。
        /// <summary>
        /// 捕获当前摄像头帧并将结果加入捕获照片库以供浏览与插入。
        /// </summary>
        /// <remarks>
        /// 在调用时会检查捕获冷却时间，若未超过阈值则忽略请求。方法在后台线程中处理帧：从线程安全的缓冲复制当前位图、（在设置允许时）尝试检测纸张四角并进行透视矫正、将位图转换为用于 UI 的 BitmapImage，然后在 UI 线程将新的 CapturedImage 插入到照片列表前端并修剪到最大数量。过程中会正确释放临时位图资源，任何处理错误将记录到日志，但不会向调用者抛出异常。
        /// </remarks>
        private void BtnCapturePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((DateTime.Now - _lastCaptureTime).TotalMilliseconds < VideoPresenterCaptureCooldownMs) return;
                _lastCaptureTime = DateTime.Now;

                Bitmap frame;
                lock (_videoPresenterFrameLock)
                {
                    if (_lastFrame == null) return;
                    frame = (Bitmap)_lastFrame.Clone();
                }

                Task.Run(() =>
                {
                    try
                    {
                        using (frame)
                        {
                            Bitmap toSave = frame;

                            if (Settings?.Automation?.IsEnablePhotoCorrection == true
                                && TryDetectPaperCorners(toSave, out List<AForge.IntPoint> corners))
                            {
                                var corrected = ApplyPerspectiveCorrection(toSave, corners);
                                if (corrected != null) toSave = corrected;
                            }

                            var bmpImage = ConvertBitmapToBitmapImage(toSave);
                            if (!ReferenceEquals(toSave, frame))
                            {
                                toSave.Dispose();
                            }

                            if (bmpImage == null) return;

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                var ci = new CapturedImage(bmpImage);
                                _capturedPhotos.Insert(0, ci);
                                
                                while (_capturedPhotos.Count > MaxCapturedPhotos)
                                {
                                    _capturedPhotos.RemoveAt(_capturedPhotos.Count - 1);
                                }
                                
                                UpdateCapturedPhotosDisplay();
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"视频展台拍照失败: {ex.Message}", LogHelper.LogType.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"视频展台拍照失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 将当前相机预览的显示角度顺时针旋转 90°（在四个方向间切换）。
        /// </summary>
        /// <remarks>
        /// 更新内部 CameraService 的旋转状态以切换到下一个方向；在错误发生时会记录日志但不会抛出异常到调用者。
        /// </remarks>
        /// <param name="sender">触发该事件的控件（通常为旋转按钮）。</param>
        /// <summary>
        /// 将摄像头预览以90度为步进顺时针旋转一次（在 0、90、180、270 之间循环）。
        /// </summary>
        private void BtnRotateImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnsureCameraService();
                _cameraService.RotationAngle = (_cameraService.RotationAngle + 1) % 4;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"视频展台旋转失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 在启用照片校正的切换按钮被选中时，将该偏好设置为开启并保存到设置文件。
        /// <summary>
        /// 启用照片的透视/校正功能并将该设置写入配置文件。
        /// </summary>
        /// <remarks>
        /// 如果 Settings 或其 Automation 字段为 null，则方法不执行任何操作。
        /// </remarks>
        private void ToggleBtnPhotoCorrection_Checked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = true;
            SaveSettingsToFile();
        }

        /// <summary>
        /// 关闭“相片校正”设置并将变更持久化到设置文件。
        /// <summary>
        /// 在设置中禁用拍照透视校正并将更改保存到设置文件。
        /// </summary>
        private void ToggleBtnPhotoCorrection_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = false;
            SaveSettingsToFile();
        }

        /// <summary>
        /// 刷新并在 CapturedPhotosStackPanel 中显示最近捕获的照片缩略图，最多显示 30 张。
        /// </summary>
        /// <remarks>
        /// 如果 CapturedPhotosStackPanel 为 null 则不执行任何操作。该方法会清空面板现有内容，并为每张照片创建一个包含缩略图的按钮；点击按钮会将对应照片插入画布。
        /// <summary>
        /// 刷新捕获照片面板的显示，最多展示最近 30 张缩略图并为每张创建可点击的按钮以插入到画布。
        /// </summary>
        /// <remarks>
        /// 若 CapturedPhotosStackPanel 为 null 则不做任何操作。方法会清空面板的子项，然后遍历内部 _capturedPhotos 列表的前 30 项，为每项创建一个透明无边框的 Button（内容为高度为 90 的缩略图 Image），并将按钮的点击事件绑定到将该照片插入画布的操作上。
        /// </remarks>
        private void UpdateCapturedPhotosDisplay()
        {
            if (CapturedPhotosStackPanel == null) return;

            CapturedPhotosStackPanel.Children.Clear();

            foreach (var photo in _capturedPhotos.Take(30))
            {
                var btn = new Button
                {
                    Margin = new Thickness(0, 0, 0, 6),
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = System.Windows.Media.Brushes.Transparent,
                    Tag = photo
                };
                btn.Click += (s, e) =>
                {
                    if (btn.Tag is CapturedImage p) InsertPhotoToCanvas(p);
                };

                var img = new System.Windows.Controls.Image
                {
                    Source = photo.Thumbnail,
                    Stretch = System.Windows.Media.Stretch.UniformToFill,
                    Height = 90
                };
                btn.Content = img;
                CapturedPhotosStackPanel.Children.Add(btn);
            }
        }

        /// <summary>
        /// 将选定的捕获图片作为图像元素插入到画布中央并切换到选择工具模式。
        /// </summary>
        /// <param name="photo">要插入的捕获图片；若为 null 或其 Image 为 null，则不进行任何操作。</param>
        /// <remarks>
        /// 在画布上创建并配置一个 Image 元素（设置 Source、Stretch、默认宽度及位置），初始化其变换与事件绑定，提交插入历史记录，添加到 inkCanvas，并将当前工具切换为“选择”同时隐藏相关子面板。方法内部捕获并记录异常，不会向外抛出。
        /// <summary>
        /// 将一张已捕获的照片作为可编辑图像插入到画布中央并切换到选择工具。
        /// </summary>
        /// <param name="photo">包含要插入的图像（photo.Image 不能为空）的 CapturedImage 对象；若为 null 或 photo.Image 为 null 则不执行任何操作。</param>
        private void InsertPhotoToCanvas(CapturedImage photo)
        {
            if (photo?.Image == null) return;

            try
            {
                var img = new System.Windows.Controls.Image
                {
                    Source = photo.Image,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    Width = 500
                };

                double x = (inkCanvas?.ActualWidth ?? 0) / 2 - img.Width / 2;
                double y = (inkCanvas?.ActualHeight ?? 0) / 2 - 200;
                if (double.IsNaN(x) || double.IsInfinity(x)) x = 100;
                if (double.IsNaN(y) || double.IsInfinity(y)) y = 100;

                InkCanvas.SetLeft(img, Math.Max(0, x));
                InkCanvas.SetTop(img, Math.Max(0, y));
                InitializeElementTransform(img);
                BindElementEvents(img);
                timeMachine.CommitElementInsertHistory(img);

                inkCanvas?.Children.Add(img);

                SetCurrentToolMode(InkCanvasEditingMode.Select);
                UpdateCurrentToolMode("select");
                HideSubPanels("select");
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"插入展台照片失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 在离开白板模式时关闭并清理视频呈现器相关的 UI 与运行状态。
        /// </summary>
        /// <remarks>
        /// 隐藏视频呈现侧栏、将“在画布上显示实时帧”开关取消选中、从画布中移除并隐藏所有每页的实时帧图像实例，并尝试停止相机预览。该方法在执行过程中会吞并内部异常以避免抛出至调用方。
        /// <summary>
        /// 清理并关闭视频展示器相关的 UI 与运行时状态以退出白板模式。
        /// </summary>
        /// <remarks>
        /// 执行以下操作：收起视频展示侧栏，取消当前页面的“在画布上显示实时画面”开关，移除并隐藏所有页面上挂载的实时画面元素，停止相机预览并释放相关运行时预览状态。
        /// </remarks>
        private void VideoPresenter_OnExitWhiteboardMode()
        {
            try
            {
                // 收起侧栏
                if (VideoPresenterSidebar != null)
                {
                    VideoPresenterSidebar.Visibility = Visibility.Collapsed;
                }

                if (BtnToggleVideoPresenterLiveOnCanvas != null)
                {
                    BtnToggleVideoPresenterLiveOnCanvas.IsChecked = false;
                }

                if (inkCanvas != null)
                {
                    foreach (var kv in _liveFrameImageByPage.ToList())
                    {
                        var img = kv.Value;
                        if (img == null) continue;
                        try
                        {
                            if (inkCanvas.Children.Contains(img))
                            {
                                inkCanvas.Children.Remove(img);
                            }
                            img.Visibility = Visibility.Collapsed;
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                    }
                }

                try { _cameraService?.StopPreview(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 将一个 System.Drawing.Bitmap 转换为可跨线程使用的 WPF BitmapImage。
        /// </summary>
        /// <param name="bitmap">要转换的源位图；若为 <see langword="null"/> 则直接返回 <see langword="null"/>。</param>
        /// <summary>
        /// 将 <see cref="Bitmap"/> 转换为可用于 WPF 的 <see cref="BitmapImage"/>，并将结果冻结以便在 UI 线程之外安全读取。
        /// </summary>
        /// <param name="bitmap">要转换的位图；若为 <see langword="null"/> 则返回 <see langword="null"/>。</param>
        /// <returns>转换得到的 <see cref="BitmapImage"/>，已被冻结以便跨线程使用；若输入为 <see langword="null"/> 或转换失败则返回 <see langword="null"/>。</returns>
        private static BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            try
            {
                if (bitmap == null) return null;

                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.StreamSource = ms;
                    bi.EndInit();
                    bi.Freeze();
                    return bi;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 在给定帧中尝试检测纸张（四边形）角点，并返回按原始帧坐标排列的四个点。
        /// </summary>
        /// <param name="frame">要检测的输入位图帧。</param>
        /// <param name="cornersOut">检测到的四个角点（按顺序：左上、右上、左下、右下），坐标以输入帧的像素空间为准；检测失败时为 null。</param>
        /// <summary>
        /// 在给定位图中检测纸张的四个角点并按顺序返回它们。
        /// </summary>
        /// <param name="frame">要检测的输入位图。</param>
        /// <param name="cornersOut">检测到的四个角点（顺序为：左上、右上、左下、右下），在检测失败时为 <c>null</c>。</param>
        /// <returns><see langword="true"/> 如果成功检测到四个角点并填充 <paramref name="cornersOut"/>，<see langword="false"/> 否则（包括输入为 <c>null</c> 或检测过程中发生错误）。</returns>
        private static bool TryDetectPaperCorners(Bitmap frame, out List<AForge.IntPoint> cornersOut)
        {
            cornersOut = null;
            try
            {
                if (frame == null) return false;

                int targetWidth = 640;
                int ow = frame.Width;
                int oh = frame.Height;
                double scale = 1.0;
                Bitmap work = frame;
                if (ow > targetWidth)
                {
                    int nh = (int)Math.Round(oh * (targetWidth / (double)ow));
                    var resize = new ResizeBilinear(targetWidth, nh);
                    work = resize.Apply(frame);
                    scale = (double)ow / targetWidth;
                }

                var gray = Grayscale.CommonAlgorithms.BT709.Apply(work);
                var blur = new GaussianBlur(3, 3);
                blur.ApplyInPlace(gray);
                var canny = new CannyEdgeDetector();
                canny.ApplyInPlace(gray);
                var dilate = new Dilatation3x3();
                dilate.ApplyInPlace(gray);

                var bc = new BlobCounter
                {
                    FilterBlobs = true,
                    MinHeight = 50,
                    MinWidth = 50,
                    ObjectsOrder = ObjectsOrder.Size
                };
                bc.ProcessImage(gray);
                var blobs = bc.GetObjectsInformation();
                var sc = new SimpleShapeChecker();
                List<AForge.IntPoint> best = null;
                double bestArea = 0;

                foreach (var blob in blobs)
                {
                    var edgePoints = bc.GetBlobsEdgePoints(blob);
                    if (edgePoints == null || edgePoints.Count < 4) continue;
                    if (sc.IsQuadrilateral(edgePoints, out List<AForge.IntPoint> crn))
                    {
                        double area = Math.Abs(PolygonArea(crn));
                        if (area > bestArea)
                        {
                            bestArea = area;
                            best = crn;
                        }
                    }
                }

                if (best != null)
                {
                    var pts = best
                        .Select(p => new AForge.IntPoint((int)Math.Round(p.X * scale), (int)Math.Round(p.Y * scale)))
                        .ToList();
                    pts.Sort((a, b) => a.Y.CompareTo(b.Y));
                    if (pts[0].X > pts[1].X) (pts[0], pts[1]) = (pts[1], pts[0]);
                    if (pts[2].X > pts[3].X) (pts[2], pts[3]) = (pts[3], pts[2]);
                    cornersOut = pts;
                    if (!ReferenceEquals(work, frame)) work.Dispose();
                    gray.Dispose();
                    return true;
                }

                if (!ReferenceEquals(work, frame)) work.Dispose();
                gray.Dispose();
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 将源图像中由四个角点定义的纸张区域进行透视矫正并裁切为目标尺寸的位图，目标高度为 CorrectedPaperHeight，宽度按纸张比例计算。
        /// </summary>
        /// <param name="frame">包含待矫正纸张的源位图。</param>
        /// <param name="corners">纸张在源图像中的四个角点，按顺序提供：左上 (top-left)、右上 (top-right)、左下 (bottom-left)、右下 (bottom-right)。坐标为图像像素坐标系。</param>
        /// <summary>
        /// 将给定位图中由四个角点定义的四边形区域按透视变换校正并裁切为目标尺寸的矩形位图。
        /// </summary>
        /// <param name="frame">源位图。</param>
        /// <param name="corners">表示四边形四个顶点的点列表，必须包含 4 个点，顺序为：左上、右上、右下、左下。</param>
        /// <returns>透视矫正并裁切后的位图；在输入无效或矫正失败时返回 <see langword="null"/>。</returns>
        private static Bitmap ApplyPerspectiveCorrection(Bitmap frame, List<AForge.IntPoint> corners)
        {
            try
            {
                if (frame == null || corners == null || corners.Count != 4) return null;
                var tl = corners[0];
                var tr = corners[1];
                var bl = corners[2];
                var br = corners[3];

                double topW = Math.Sqrt((tr.X - tl.X) * (tr.X - tl.X) + (tr.Y - tl.Y) * (tr.Y - tl.Y));
                double bottomW = Math.Sqrt((br.X - bl.X) * (br.X - bl.X) + (br.Y - bl.Y) * (br.Y - bl.Y));
                double leftH = Math.Sqrt((bl.X - tl.X) * (bl.X - tl.X) + (bl.Y - tl.Y) * (bl.Y - tl.Y));
                double rightH = Math.Sqrt((br.X - tr.X) * (br.X - tr.X) + (br.Y - tr.Y) * (br.Y - tr.Y));

                double avgW = (topW + bottomW) / 2.0;
                double avgH = (leftH + rightH) / 2.0;
                if (avgH <= 0) avgH = 1;
                double ratio = avgW / avgH;

                int targetH = CorrectedPaperHeight;
                int targetW = Math.Max(1, (int)Math.Round(targetH * ratio));

                var orderedCorners = new List<AForge.IntPoint> { tl, tr, br, bl };
                var qtf = new QuadrilateralTransformation(orderedCorners, targetW, targetH);
                return qtf.Apply(frame);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 计算由给定顶点按顺序构成的多边形的有向面积（使用高斯面积/鞋带公式）。
        /// </summary>
        /// <param name="pts">按顶点顺序排列的多边形顶点列表（至少应包含三个点以形成多边形）。</param>
        /// <summary>
        /// 计算按给定顶点顺序的多边形的有向面积。
        /// </summary>
        /// <param name="pts">按顶点顺序排列的多边形顶点列表（可能为顺时针或逆时针）；若顶点数少于三则视为无面积。</param>
        /// <returns>当顶点按逆时针排列时为正值，按顺时针排列时为负值；顶点少于三时返回 0。</returns>
        private static double PolygonArea(List<AForge.IntPoint> pts)
        {
            int n = pts.Count;
            if (n < 3) return 0;
            long sum = 0;
            for (int i = 0; i < n; i++)
            {
                var p = pts[i];
                var q = pts[(i + 1) % n];
                sum += (long)p.X * q.Y - (long)p.Y * q.X;
            }
            return 0.5 * sum;
        }
    }
}