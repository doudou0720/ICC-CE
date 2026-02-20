using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private void SaveScreenShot(bool isHideNotification, string fileName = null)
        {
            var savePath = Settings.Automation.IsSaveScreenshotsInDateFolders
                ? GetDateFolderPath(fileName)
                : GetDefaultFolderPath();

            CaptureAndSaveScreenshot(savePath, isHideNotification);

            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false);
        }

        internal void SaveScreenShotToDesktop()
        {
            var desktopPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");

            CaptureAndSaveScreenshot(desktopPath, false);

            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false);
        }

        internal async Task SaveAreaScreenShotToDesktop()
        {
            try
            {
                var originalVisibility = Visibility;
                Visibility = Visibility.Hidden;
                await Task.Delay(200);

                var screenshotResult = await ShowScreenshotSelector();
                Visibility = originalVisibility;

                if (!screenshotResult.HasValue)
                {
                    ShowNotification("截图已取消");
                    return;
                }

                if (screenshotResult.Value.AddToWhiteboard)
                {
                    await AddScreenshotToNewWhiteboardPage(screenshotResult.Value);
                    return;
                }

                if (screenshotResult.Value.Area.Width <= 0 || screenshotResult.Value.Area.Height <= 0)
                {
                    ShowNotification("未选择有效截图区域");
                    return;
                }

                var desktopPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");

                using (var originalBitmap = CaptureScreenArea(screenshotResult.Value.Area))
                {
                    if (originalBitmap == null)
                    {
                        ShowNotification("截图失败");
                        return;
                    }

                    Bitmap finalBitmap = originalBitmap;
                    bool needDisposeFinalBitmap = false;

                    try
                    {
                        if (screenshotResult.Value.Path != null && screenshotResult.Value.Path.Count > 0)
                        {
                            finalBitmap = ApplyShapeMask(originalBitmap, screenshotResult.Value.Path, screenshotResult.Value.Area);
                            needDisposeFinalBitmap = true;
                        }

                        var directory = Path.GetDirectoryName(desktopPath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        finalBitmap.Save(desktopPath, ImageFormat.Png);
                        ShowNotification($"截图成功保存至 {desktopPath}");
                    }
                    finally
                    {
                        if (needDisposeFinalBitmap && finalBitmap != originalBitmap)
                        {
                            finalBitmap.Dispose();
                        }
                    }
                }

                if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                    SaveInkCanvasStrokes(false);
            }
            catch (Exception ex)
            {
                Visibility = Visibility.Visible;
                ShowNotification($"截图失败: {ex.Message}");
            }
        }

        private async Task AddScreenshotToNewWhiteboardPage(ScreenshotResult screenshotResult)
        {
            // 先在当前场景准备截图数据，再进白板，避免误截到白板页面
            BitmapSource bitmapSourceForClipboard = null;

            // 摄像头截图（BitmapSource）
            if (screenshotResult.CameraBitmapSource != null)
            {
                bitmapSourceForClipboard = screenshotResult.CameraBitmapSource;
            }
            // 摄像头截图（Bitmap）
            else if (screenshotResult.CameraImage != null)
            {
                bitmapSourceForClipboard = ConvertBitmapToBitmapSource(screenshotResult.CameraImage);
            }
            else
            {
                if (screenshotResult.Area.Width <= 0 || screenshotResult.Area.Height <= 0)
                {
                    ShowNotification("未选择有效截图区域");
                    return;
                }

                using (var originalBitmap = CaptureScreenArea(screenshotResult.Area))
                {
                    if (originalBitmap == null)
                    {
                        ShowNotification("截图失败");
                        return;
                    }

                    Bitmap finalBitmap = originalBitmap;
                    bool needDisposeFinalBitmap = false;

                    try
                    {
                        if (screenshotResult.Path != null && screenshotResult.Path.Count > 0)
                        {
                            finalBitmap = ApplyShapeMask(originalBitmap, screenshotResult.Path, screenshotResult.Area);
                            needDisposeFinalBitmap = true;
                        }

                        bitmapSourceForClipboard = ConvertBitmapToBitmapSource(finalBitmap);
                    }
                    finally
                    {
                        if (needDisposeFinalBitmap && finalBitmap != originalBitmap)
                        {
                            finalBitmap.Dispose();
                        }
                    }
                }
            }

            if (bitmapSourceForClipboard == null)
            {
                ShowNotification("截图转换失败");
                return;
            }

            // 图像已拷贝到内存后再进入白板
            bitmapSourceForClipboard.Freeze();

            if (currentMode != 1)
            {
                SwitchToBoardMode();
                await Task.Delay(150);
            }

            BtnWhiteBoardAdd_Click(null, EventArgs.Empty);

            System.Windows.Clipboard.SetImage(bitmapSourceForClipboard);
            await Task.Delay(60);
            await PasteImageFromClipboard();
        }

        // 提取公共的截图和保存逻辑
        private void CaptureAndSaveScreenshot(string savePath, bool isHideNotification)
        {
            var rc = SystemInformation.VirtualScreen;

            using (var bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb))
            using (var memoryGraphics = Graphics.FromImage(bitmap))
            {
                // 设置高质量渲染
                memoryGraphics.CompositingQuality = CompositingQuality.HighQuality;
                memoryGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                memoryGraphics.SmoothingMode = SmoothingMode.HighQuality;
                memoryGraphics.CompositingMode = CompositingMode.SourceOver;

                memoryGraphics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);

                // 确保目录存在
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 使用PNG格式保存，确保透明度信息不丢失
                bitmap.Save(savePath, ImageFormat.Png);
            }

            if (!isHideNotification)
            {
                ShowNotification($"截图成功保存至 {savePath}");
            }
            _ = Task.Run(async () =>
            {
                try
                {
                    var delayMinutes = Settings?.Dlass?.AutoUploadDelayMinutes ?? 0;
                    if (delayMinutes > 0)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(delayMinutes));
                    }

                    await Helpers.DlassNoteUploader.UploadNoteFileAsync(savePath);
                }
                catch (Exception)
                {
                }
            });
        }

        // 获取日期文件夹路径
        private string GetDateFolderPath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = DateTime.Now.ToString("HH-mm-ss");
            }

            var basePath = Settings.Automation.AutoSavedStrokesLocation;
            var dateFolder = DateTime.Now.ToString("yyyyMMdd");

            return Path.Combine(
                basePath,
                "Auto Saved - Screenshots",
                dateFolder,
                $"{fileName}.png");
        }

        // 获取默认文件夹路径
        private string GetDefaultFolderPath()
        {
            var basePath = Settings.Automation.AutoSavedStrokesLocation;
            var screenshotsFolder = Path.Combine(basePath, "Auto Saved - Screenshots");

            if (!Directory.Exists(screenshotsFolder))
            {
                Directory.CreateDirectory(screenshotsFolder);
            }

            return Path.Combine(
                screenshotsFolder,
                $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
        }
    }
}
