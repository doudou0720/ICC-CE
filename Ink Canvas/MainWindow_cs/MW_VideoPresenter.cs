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

        // 按页绑定：每一页对应一个“实时画面”元素与布局/设备信息
        private readonly Dictionary<int, System.Windows.Controls.Image> _liveFrameImageByPage = new Dictionary<int, System.Windows.Controls.Image>();
        private readonly HashSet<int> _liveEnabledPages = new HashSet<int>();
        private readonly Dictionary<int, int> _cameraIndexByPage = new Dictionary<int, int>();
        private readonly Dictionary<int, (double left, double top, double width)> _liveFrameLayoutByPage =
            new Dictionary<int, (double left, double top, double width)>();

        private DateTime _lastCaptureTime = DateTime.MinValue;
        private const int VideoPresenterCaptureCooldownMs = 1000;

        private const int CorrectedPaperHeight = 600;

        private void BtnToggleVideoPresenter_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ToggleVideoPresenterSidebar();
        }

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

            if (CheckBoxEnablePhotoCorrection != null)
            {
                CheckBoxEnablePhotoCorrection.IsChecked = Settings?.Automation?.IsEnablePhotoCorrection ?? false;
            }

            // 同步“上屏”按钮状态（按页绑定）
            if (BtnToggleVideoPresenterLiveOnCanvas != null)
            {
                BtnToggleVideoPresenterLiveOnCanvas.IsChecked = _liveEnabledPages.Contains(GetCurrentPageIndex());
            }
        }

        private void BtnCloseVideoPresenter_Click(object sender, RoutedEventArgs e)
        {
            if (VideoPresenterSidebar != null)
            {
                VideoPresenterSidebar.Visibility = Visibility.Collapsed;
            }
        }

        private void EnsureCameraService()
        {
            if (_cameraService != null) return;

            _cameraService = new CameraService();
            _cameraService.FrameReceived += CameraService_FrameReceived;
            _cameraService.ErrorOccurred += CameraService_ErrorOccurred;
        }

        private void CameraService_ErrorOccurred(object sender, string e)
        {
            try
            {
                LogHelper.WriteLogToFile($"视频展台摄像头错误: {e}", LogHelper.LogType.Error);
            }
            catch { }
        }

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

        private int GetCurrentPageIndex()
        {
            return Math.Max(1, CurrentWhiteboardIndex);
        }

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

        // 翻页前调用：保存当前页实时画面的位置/大小
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

        // 翻页后调用：根据该页状态恢复实时画面，并同步设备选择
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

        private void CheckBoxEnablePhotoCorrection_Checked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = true;
            SaveSettingsToFile();
        }

        private void CheckBoxEnablePhotoCorrection_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Settings?.Automation == null) return;
            Settings.Automation.IsEnablePhotoCorrection = false;
            SaveSettingsToFile();
        }

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

