using System;
using System.Windows.Ink;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Models
{
    public class CapturedImage
    {
        public BitmapImage Image { get; }
        public BitmapImage Thumbnail { get; }
        public StrokeCollection Strokes { get; }
        public string Timestamp { get; }
        public string FilePath { get; }

        /// <summary>
        /// 使用提供的 BitmapImage 创建一个 CapturedImage 实例，生成可跨线程使用的冻结图像副本、缩略图，并初始化笔迹集合、时间戳和文件路径字段。
        /// </summary>
        /// <param name="image">源 BitmapImage；不能为空。构造后会被复制并冻结以便安全重用。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="image"/> 为 null 时抛出。</exception>
        public CapturedImage(BitmapImage image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image), "图像不能为空");

            // 确保 Image 被冻结，避免跨线程访问风险
            Image = EnsureFrozen(image);
            Thumbnail = CreateThumbnail(Image);
            Strokes = new StrokeCollection();
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            FilePath = null;
        }

        /// <summary>
        /// 使用指定的图像和可选的文件路径初始化一个 CapturedImage 实例。
        /// </summary>
        /// <param name="image">用于初始化的位图图像；不能为空。</param>
        /// <param name="filePath">图像对应的文件路径（可为 null）；用于尝试从文件名提取时间戳。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="image"/> 为 null 时抛出。</exception>
        public CapturedImage(BitmapImage image, string filePath)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image), "图像不能为空");

            // 确保 Image 被冻结，避免跨线程访问风险
            Image = EnsureFrozen(image);
            Thumbnail = CreateThumbnail(Image);
            Strokes = new StrokeCollection();
            FilePath = filePath;
            Timestamp = TryExtractTimestampFromFilePath(filePath) ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        /// <summary>
        /// 尝试从给定文件路径的文件名中提取时间戳并以 "yyyy-MM-dd HH:mm:ss.fff" 形式返回规范化字符串。
        /// </summary>
        /// <param name="filePath">包含可能带时间戳的文件名的文件路径。</param>
        /// <returns>`yyyy-MM-dd HH:mm:ss.fff` 格式的时间字符串（若解析成功），解析失败或输入无效时返回 <c>null</c>。</returns>
        private static string TryExtractTimestampFromFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return null;
                var name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                if (DateTime.TryParseExact(
                        name,
                        "yyyy-MM-dd HH-mm-ss-fff",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var dt))
                {
                    return dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                }
                if (name.Length >= 23)
                {
                    var tail = name.Substring(name.Length - 23);
                    if (DateTime.TryParseExact(
                            tail,
                            "yyyy-MM-dd HH-mm-ss-fff",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out var dt2))
                    {
                        return dt2.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 确保指定的 <see cref="BitmapImage"/> 已被冻结，以便可安全跨线程使用；如果输入未被冻结则返回其冻结副本，否则返回原始实例。
        /// </summary>
        /// <param name="image">要确保被冻结的 BitmapImage。</param>
        /// <returns>已被冻结的 <see cref="BitmapImage"/>（可能是原始对象或其冻结副本）。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="image"/> 为 null 时抛出。</exception>
        private static BitmapImage EnsureFrozen(BitmapImage image)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if (image.IsFrozen)
                return image;

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));

            var stream = new System.IO.MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            var frozenCopy = new BitmapImage();
            frozenCopy.BeginInit();
            frozenCopy.CacheOption = BitmapCacheOption.OnLoad;
            frozenCopy.StreamSource = stream;
            frozenCopy.EndInit();
            frozenCopy.Freeze();

            return frozenCopy;
        }

        /// <summary>
        /// 生成并返回一个固定（冻结）的缩略图，按 290×180 的目标框架等比缩放原图以适配最大尺寸。
        /// </summary>
        /// <param name="original">用于生成缩略图的源位图。</param>
        /// <returns>按等比缩放并冻结后的缩略图（BitmapImage）。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="original"/> 为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当 <paramref name="original"/> 的像素宽度或高度小于或等于 0 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当无法计算出有效的缩放比例（例如为 NaN、无穷或非正值）时抛出。</exception>
        private static BitmapImage CreateThumbnail(BitmapImage original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            if (original.PixelWidth <= 0 || original.PixelHeight <= 0)
            {
                throw new ArgumentException(
                    $"图像尺寸无效：宽度={original.PixelWidth}, 高度={original.PixelHeight}。图像必须具有有效的像素尺寸。",
                    nameof(original));
            }

            double targetWidth = 290.0;
            double targetHeight = 180.0;
            double scale = Math.Min(targetWidth / original.PixelWidth, targetHeight / original.PixelHeight);
            
            if (double.IsInfinity(scale) || double.IsNaN(scale) || scale <= 0)
            {
                throw new InvalidOperationException(
                    $"无法计算有效的缩放比例：scale={scale}, 图像尺寸={original.PixelWidth}x{original.PixelHeight}");
            }

            var thumbnail = new TransformedBitmap(original, new System.Windows.Media.ScaleTransform(scale, scale));

            var bmp = new JpegBitmapEncoder { QualityLevel = 85 };
            bmp.Frames.Add(BitmapFrame.Create(thumbnail));

            using (var stream = new System.IO.MemoryStream())
            {
                bmp.Save(stream);
                stream.Seek(0, System.IO.SeekOrigin.Begin);

                var result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();

                return result;
            }
        }
    }
}
