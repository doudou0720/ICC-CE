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
        /// 使用指定的位图创建一个 CapturedImage 实例，并为其生成缩略图、空白笔划集合和时间戳。
        /// </summary>
        /// <param name="image">用于初始化的位图；不能为空。传入的图像将在内部确保为冻结状态以便安全跨线程使用。</param>
        /// <summary>
        /// 初始化一个 CapturedImage 实例：保存并冻结传入的位图，生成其缩略图并初始化相关元数据与空的笔画集合。
        /// </summary>
        /// <param name="image">要保存的位图，不能为空；会以冻结的副本作为实例的 Image。</param>
        /// <exception cref="System.ArgumentNullException">当 <paramref name="image"/> 为 null 时抛出。</exception>
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
        /// 初始化 CapturedImage 实例：将指定图像冻结用于线程安全、生成缩略图并初始化空的笔迹集合，同时设置文件路径和时间戳（尝试从文件名提取时间戳，失败则使用当前时间）。
        /// </summary>
        /// <param name="image">源图像，不能为空。</param>
        /// <param name="filePath">关联文件的路径，可能为 null。</param>
        /// <summary>
        /// 使用提供的位图和文件路径创建一个 CapturedImage 实例：将主图冻结以便跨线程使用，生成并冻结缩略图，初始化空的笔迹集合，保存文件路径并根据文件名或当前时间设置时间戳。
        /// </summary>
        /// <param name="image">用于初始化主图的位图，不能为空。</param>
        /// <param name="filePath">与图像关联的文件路径；如果文件名包含可解析的时间戳则用于设置 Timestamp，否则使用当前时间。</param>
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
        /// 尝试从给定文件路径的文件名中解析并返回规范化的时间戳。
        /// </summary>
        /// <param name="filePath">要从其文件名中解析时间戳的文件路径；可以为 null 或空字符串。</param>
        /// <summary>
        /// 从文件路径或其文件名中尝试解析时间戳并返回规范化的时间字符串。
        /// </summary>
        /// <param name="filePath">包含要解析的文件名的路径；方法会使用不带扩展名的文件名进行解析，支持完整文件名或其末尾 23 个字符符合格式的情况。</param>
        /// <returns>`yyyy-MM-dd HH:mm:ss.fff` 格式的时间戳字符串，解析失败或输入无效时返回 null。</returns>
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
        /// 确保并返回一个已冻结的 BitmapImage 副本，以便在跨线程场景中安全使用。
        /// </summary>
        /// <param name="image">要确保为冻结状态的源 BitmapImage。</param>
        /// <returns>与输入图像内容一致且已调用 Freeze 的 BitmapImage 实例。</returns>
        /// <summary>
        /// 确保并返回一个可跨线程安全使用的已冻结 BitmapImage。
        /// </summary>
        /// <param name="image">要确保为冻结状态的位图；不得为 null。</param>
        /// <returns>已冻结的 <see cref="BitmapImage"/> 实例；如果输入已经被冻结则返回相同实例，否则返回新的已冻结副本。</returns>
        /// <exception cref="ArgumentNullException">在 <paramref name="image"/> 为 null 时抛出。</exception>
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
        /// 生成并返回一个在 290×180 约束内按比例缩放并已冻结的缩略图。
        /// </summary>
        /// <param name="original">用于生成缩略图的源 <see cref="BitmapImage"/>；不得为 <c>null</c>，且其像素宽度和高度必须大于 0。</param>
        /// <returns>已冻结的 <see cref="BitmapImage"/> 缩略图，尺寸不超过 290×180 且保持原图纵横比。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="original"/> 为 <c>null</c> 时抛出。</exception>
        /// <exception cref="ArgumentException">当 <paramref name="original"/> 的像素宽度或高度小于等于 0 时抛出。</exception>
        /// <summary>
        /// 创建并返回一个根据目标尺寸（最大 290x180）缩放并冻结的 JPEG 缩略图。
        /// </summary>
        /// <param name="original">源 BitmapImage；不能为空且必须具有大于 0 的像素宽度和高度。</param>
        /// <returns>已缩放且已冻结的 BitmapImage 缩略图（使用 JPEG 编码，质量等级 85）。</returns>
        /// <exception cref="ArgumentNullException">当 <paramref name="original"/> 为 null 时抛出。</exception>
        /// <exception cref="ArgumentException">当 <paramref name="original"/> 的像素宽度或高度不大于 0 时抛出。</exception>
        /// <exception cref="InvalidOperationException">当无法计算出有效的缩放比例（例如结果为 NaN、Infinity 或非正数）时抛出。</exception>
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