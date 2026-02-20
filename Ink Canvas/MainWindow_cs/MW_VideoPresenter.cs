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
        /// 处理用于切换视频演示侧栏的鼠标点击事件，切换侧栏的显示状态。
        /// </summary>
        /// <param name="sender">触发事件的控件。</param>
        /// <param name="e">包含鼠标按键事件的详细信息。</param>
        private void BtnToggleVideoPresenter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleVideoPresenterSidebar();
        }

        /// <summary>
        /// 切换视频面板的显示状态，并在打开时初始化相关摄像头与 UI 状态。
        /// </summary>
        /// <remarks>
        /// 当面板从隐藏切换为可见时：确保摄像头服务已初始化、禁用拍照按钮（待预览启动后启用）、刷新可用摄像头列表、同步照片自动校正开关的状态，并将按页绑定的“上屏（在画布显示实时帧）”开关与当前页状态对齐；当面板可见时再次调用该方法会将其隐藏。
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
        /// 将视频演示侧栏隐藏（折叠侧边栏）。
        /// </summary>
        private void BtnCloseVideoPresenter_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPresenterSidebar != null)
            {
                VideoPresenterSidebar.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 确保 CameraService 已初始化并为其注册帧与错误事件处理器。
        /// </summary>
        /// <remarks>
        /// 如果 _cameraService 已存在则不作任何操作；否则会创建新的 CameraService 实例并订阅 FrameReceived 与 ErrorOccurred 事件。
        /// </remarks>
        private void EnsureCameraService()
        {
            if (_cameraService != null) return;

            _cameraService = new CameraService();
            _cameraService.FrameReceived += CameraService_FrameReceived;
            _cameraService.ErrorOccurred += CameraService_ErrorOccurred;
        }

        /// <summary>
        /// 将摄像头错误信息记录到错误日志文件。
        /// </summary>
        /// <param name="sender">事件发送者（通常为 CameraService 实例）。</param>
        /// <param name="e">描述错误的字符串消息。</param>
        private void CameraService_ErrorOccurred(object sender, string e)
        {
            try
            {
                LogHelper.WriteLogToFile($"视频展台摄像头错误: {e}", LogHelper.LogType.Error);
            }
            catch { }
        }

        /// <summary>
        /// 处理来自相机服务的新帧：保存为最新可用帧以供捕获，更新预览控件并在当前页（如启用）刷新画布上的实时画面元素，同时启用拍照按钮。
        /// </summary>
        /// <param name="frame">相机提供的位图帧；为 null 时将被忽略。</param>
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
        /// 获取当前白板页面的索引，确保返回值至少为 1。
        /// </summary>
        /// <returns>当前白板页面索引；如果索引小于 1，则返回 1。</returns>
        private int GetCurrentPageIndex()
        {
            return Math.Max(1, CurrentWhiteboardIndex);
        }

        /// <summary>
        /// 在当前页（若已启用实时显示）将传入的预览图像设置到白板画布上的实时帧控件并确保控件已添加并可见。
        /// </summary>
        /// <param name="preview">要显示的 BitmapImage 预览；为 <c>null</c> 时会清除控件的图像来源。</param>
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
            catch { }
        }

        private const double VideoPresenterLiveFrameScreenRatio = 0.75;

        /// <summary>
        /// 为指定页面获取或创建用于显示实时视频帧的 Image 元素，并初始化其交互变换与事件绑定。
        /// </summary>
        /// <param name="page">目标页面的索引（1 开始的页面编号）。</param>
        /// <returns>与该页面关联的 Image 控件实例；如果尚不存在则创建并返回新实例。</returns>
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
            catch { }

            _liveFrameImageByPage[page] = img;
            return img;
        }

        /// <summary>
        /// 将指定页面的已保存布局应用到给定的 Image 元素；若无保存布局则使用画布中心的默认尺寸和位置。
        /// </summary>
        /// <param name="page">目标页面编号。</param>
        /// <param name="img">要应用布局的 Image 控件；为 null 时不进行任何操作。</param>
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
        /// 刷新可用摄像头列表并在侧栏中重建设备项，用于选择和启动摄像头预览。
        /// </summary>
        /// <remarks>
        /// 如果未提供相机服务或设备面板则不执行任何操作。方法将从相机服务获取当前可用摄像头，清空并填充 CameraDevicesStackPanel：
        /// - 当无可用摄像头时显示提示文本；
        /// - 否则为每个摄像头创建一个单选项供选择。若存在至少一个摄像头，则自动选中并启动第一个摄像头的预览。
        /// 此方法会修改 UI 元素并可能触发预览启动的副作用。
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

            // 自动启动第一个摄像头
            if (_cameraService.AvailableCameras.Count > 0)
            {
                if (CameraDevicesStackPanel.Children.Count > 0 && CameraDevicesStackPanel.Children[0] is RadioButton first)
                {
                    first.IsChecked = true;
                }
                else
                {
                    StartVideoPresenterPreview(0);
                }
            }
        }

        /// <summary>
        /// 为当前页面启动指定摄像头的预览并保存该页面的摄像头选择状态。
        /// </summary>
        /// <param name="cameraIndex">要启动的摄像头设备索引（用于标识并启动对应的摄像头）。</param>
        /// <remarks>
        /// 如果预览成功，会启用捕获按钮并将所选摄像头索引记录为当前页面的设置；在启动失败或发生异常时会记录错误日志。
        /// </remarks>
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
        /// 在当前页启用画布上的实时视频帧，创建并应用该页的帧元素与布局，将其添加到画布，并切换到选择工具以便交互。
        /// </summary>
        /// <remarks>
        /// 如果侧栏预览已有图像，则立即用该预览更新画布上的帧显示。
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
            catch { }

            // 立即用侧栏预览刷新一次
            if (VideoPresenterPreviewImage?.Source is BitmapImage bi)
            {
                img.Source = bi;
            }
        }

        /// <summary>
        /// 禁用当前页的画布上实时视频覆盖，并移除对应的直播帧图像元素（如存在）。
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
                catch { }
            }
        }

        /// <summary>
        /// 在切换页面前保存当前页面的实时画面位置与宽度布局信息。
        /// </summary>
        /// <remarks>
        /// 如果当前页面没有已创建的实时画面控件则不执行任何操作。保存的数据为画面左上角坐标（左、上）和宽度；当坐标为 NaN 时使用 0 代替。异常被静默吞掉以保证翻页流程不中断。
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
            catch { }
        }

        /// <summary>
        /// 在页面切换后恢复该页的视频实时显示状态并同步该页的摄像头选择与布局。
        /// </summary>
        /// <remarks>
        /// 同步“上屏”开关的选中状态；如果该页启用了实时画面，确保并恢复对应的 Image 元素及其保存的布局并将最新预览图像赋予该元素；如果为该页记录了摄像头索引，则启动对应摄像头的预览。
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
            catch { }
        }

        /// <summary>
        /// 从当前视频帧捕获一张照片并将处理后的图像添加到已捕获照片集合以显示为缩略图。
        /// </summary>
        /// <remarks>
        /// 若启用了自动照片校正且能检测到纸张四角，会对图像应用透视矫正。捕获操作受冷却时间限制，图像处理在后台线程完成，完成后在 UI 线程将缩略图插入集合并维护集合最大长度。发生异常时记录错误日志。
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
                                    var oldPhoto = _capturedPhotos[_capturedPhotos.Count - 1];
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
        /// 将当前摄像头预览的旋转角度顺时针增加 90°，超出 360° 时循环回 0°。
        /// </summary>
        /// <remarks>
        /// 在必要时会初始化摄像头服务；若操作失败会将错误写入日志且不会抛出异常。
        /// </remarks>
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
        /// 启用拍照时的自动透视校正设置并将配置保存到文件。
        /// </summary>
        /// <remarks>当 Settings 或 Settings.Automation 为 null 时不作任何操作。</remarks>
        private void ToggleBtnPhotoCorrection_Checked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = true;
            SaveSettingsToFile();
        }

        /// <summary>
        /// 禁用拍照时的自动透视校正并将更改保存到设置文件。
        /// </summary>
        /// <remarks>
        /// 如果 Settings 或其 Automation 字段为 null，则不执行任何操作。
        /// </remarks>
        private void ToggleBtnPhotoCorrection_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = false;
            SaveSettingsToFile();
        }

        /// <summary>
        /// 使用当前已捕获的照片集合刷新侧边栏的缩略图列表并为每张缩略图绑定插入画布的点击行为。
        /// </summary>
        /// <remarks>
        /// 如果 CapturedPhotosStackPanel 为 null 则不执行任何操作。最多显示 30 张缩略图，缩略图高度为 90 像素；点击缩略图会将对应的照片插入到画布中。
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
        /// 将捕获的照片作为可交互的图像元素插入到白板画布中心并准备为编辑使用。
        /// </summary>
        /// <param name="photo">包含要插入的图片的 CapturedImage 实例；若为 null 或其 Image 为 null 则不执行任何操作。</param>
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
        /// 在退出白板模式时清理并关闭视频呈现器相关的 UI 和预览资源。
        /// </summary>
        /// <remarks>
        /// 隐藏视频呈现侧栏、取消页面上的“在画布上显示实时画面”开关、从画布中移除并隐藏所有按页面保存的实时画面元素，并尝试停止相机预览；内部捕获并吞掉可能的异常以保证退出流程稳健。
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
                        catch { }
                    }
                }

                try { _cameraService?.StopPreview(); } catch { }
            }
            catch { }
        }

        /// <summary>
        /// 将 System.Drawing.Bitmap 转换为可在 WPF 中使用的 BitmapImage。
        /// </summary>
        /// <param name="bitmap">要转换的位图；如果为 null，方法将返回 null。</param>
        /// <returns>转换得到的 BitmapImage；如果输入为 null 或转换失败，则返回 null。</returns>
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
        /// 尝试在指定位图中检测纸张状的四边形并返回其四个角点。
        /// </summary>
        /// <param name="frame">要分析的输入位图（可能为彩色照片）。</param>
        /// <param name="cornersOut">输出检测到的四个角点（按顺序：左上、右上、左下、右下），坐标基于传入的原始位图；检测失败时为 null。</param>
        /// <returns>`true` 表示成功检测到四个角点并已填充 <paramref name="cornersOut"/>，`false` 表示未检测到或发生错误。</returns>
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
        /// 对给定图像中的四个角指定的四边形应用透视矫正并返回矫正后的矩形图像。
        /// </summary>
        /// <param name="frame">包含要矫正内容的源位图。</param>
        /// <param name="corners">表示目标四边形四个角的点列表，必须包含且仅包含 4 个点，且顺序为：左上、右上、左下、右下。</param>
        /// <returns>矫正后的位图，输出图像高度为 CorrectedPaperHeight、宽度按源四边形的宽高比计算；输入无效或矫正失败时返回 <c>null</c>。</returns>
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
        /// 计算由给定顶点列表定义的多边形的有向面积（使用多边形顶点的顺序确定符号）。
        /// </summary>
        /// <param name="pts">按顶点顺序排列的多边形顶点列表（顺序可以是顺时针或逆时针）。</param>
        /// <returns>`signed` 多边形面积：当顶点按逆时针顺序时为正，按顺时针顺序时为负；顶点少于 3 个或退化时返回 0。</returns>
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
