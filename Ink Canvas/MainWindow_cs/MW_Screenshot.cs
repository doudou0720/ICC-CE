using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 保存截图
        /// </summary>
        /// <param name="isHideNotification">是否隐藏通知</param>
        /// <param name="fileName">文件名</param>
        /// <remarks>
        /// 该方法会：
        /// 1. 根据设置确定保存路径
        /// 2. 调用CaptureAndSaveScreenshot方法捕获并保存截图
        /// 3. 如果设置了自动保存墨迹，调用SaveInkCanvasStrokes方法保存墨迹
        /// <summary>
        /// 将当前屏幕截图保存到由设置决定的路径，并在保存后根据设置决定是否同时保存墨迹笔划。
        /// </summary>
        /// <param name="isHideNotification">为 true 时在保存完成后不显示成功通知，为 false 时显示通知。</param>
        /// <param name="fileName">可选的文件名（不含扩展名）；为 null 或空时使用默认或基于时间的命名规则。</param>
        private void SaveScreenShot(bool isHideNotification, string fileName = null)
        {
            var savePath = Settings.Automation.IsSaveScreenshotsInDateFolders
                ? GetDateFolderPath(fileName)
                : GetDefaultFolderPath();

            CaptureAndSaveScreenshot(savePath, isHideNotification);

            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false);
        }

        /// <summary>
        /// 保存截图到桌面
        /// </summary>
        /// <remarks>
        /// 该方法会：
        /// 1. 生成桌面路径和文件名
        /// 2. 调用CaptureAndSaveScreenshot方法捕获并保存截图到桌面
        /// 3. 如果设置了自动保存墨迹，调用SaveInkCanvasStrokes方法保存墨迹
        /// <summary>
        /// 将当前屏幕截图保存到桌面，并在配置允许时同时保存笔划数据。
        /// </summary>
        /// <remarks>
        /// 生成的文件名为当前时间戳，格式为 yyyy-MM-dd_HH-mm-ss.png。若 Settings.Automation.IsAutoSaveStrokesAtScreenshot 为 true，则调用保存笔划的逻辑。
        /// </remarks>
        internal void SaveScreenShotToDesktop()
        {
            var desktopPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");

            CaptureAndSaveScreenshot(desktopPath, false);

            if (Settings.Automation.IsAutoSaveStrokesAtScreenshot)
                SaveInkCanvasStrokes(false);
        }

        /// <summary>
        /// 提取公共的截图和保存逻辑
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="isHideNotification">是否隐藏通知</param>
        /// <remarks>
        /// 该方法会：
        /// 1. 获取虚拟屏幕边界
        /// 2. 创建位图并设置高质量渲染
        /// 3. 从屏幕复制内容到位图
        /// 4. 确保保存目录存在
        /// 5. 保存为PNG格式
        /// 6. 如果不隐藏通知，显示保存成功通知
        /// 7. 异步上传截图到Dlass
        /// <summary>
        /// 捕获整个虚拟屏幕并将截图以 PNG 格式保存到指定路径。
        /// </summary>
        /// <param name="savePath">截图文件的完整目标路径（包含文件名和扩展名）。</param>
        /// <param name="isHideNotification">为 true 时不显示保存成功的通知；为 false 时显示通知。</param>
        /// <remarks>
        /// 如果目标目录不存在，会尝试创建该目录。保存完成后（若 <paramref name="isHideNotification"/> 为 false）会显示一条包含保存路径的通知；随后在后台任务中根据设置的延迟（Settings.Dlass.AutoUploadDelayMinutes）可异步上传该文件，上传过程中的异常会被捕获并吞掉，不会影响主流程。
        /// </remarks>
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

        /// <summary>
        /// 获取日期文件夹路径
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>日期文件夹路径</returns>
        /// <remarks>
        /// 该方法会：
        /// 1. 如果文件名为空，使用当前时间作为文件名
        /// 2. 获取基础路径和日期文件夹名
        /// 3. 组合路径并返回
        /// <summary>
        /// 生成基于当前日期的截图文件完整路径，使用指定的文件名或当前时间作为文件名。
        /// </summary>
        /// <param name="fileName">目标文件名（不含扩展名）。若为 null 或空白，则使用当前时间（格式 "HH-mm-ss"）作为文件名。</param>
        /// <returns>组合后的完整文件路径，位于 Settings.Automation.AutoSavedStrokesLocation 下的 "Auto Saved - Screenshots/{yyyyMMdd}/{fileName}.png"。</returns>
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

        /// <summary>
        /// 获取默认文件夹路径
        /// </summary>
        /// <returns>默认文件夹路径</returns>
        /// <remarks>
        /// 该方法会：
        /// 1. 获取基础路径
        /// 2. 组合截图文件夹路径
        /// 3. 确保截图文件夹存在
        /// 4. 生成文件名并组合完整路径返回
        /// <summary>
        /// 为截图生成默认的保存路径并确保目标目录存在。
        /// </summary>
        /// <remarks>
        /// 目标目录为 Settings.Automation.AutoSavedStrokesLocation 下的 "Auto Saved - Screenshots" 子目录；若该目录不存在则会创建它。
        /// 生成的文件名以当前本地时间戳命名，格式为 yyyy-MM-dd_HH-mm-ss，并以 .png 作为扩展名。
        /// </remarks>
        /// <returns>包含完整目录和基于当前时间戳的文件名的 PNG 文件路径。</returns>
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