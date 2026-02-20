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

