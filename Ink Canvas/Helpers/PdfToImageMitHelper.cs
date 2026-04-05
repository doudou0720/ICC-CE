using PDFtoImage;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 使用 NuGet「PDFtoImage」(MIT，基于 PDFium/SkiaSharp) 解析/渲染 PDF，作为 WinRT 不可用时的备用实现。
    /// </summary>
    internal static class PdfToImageMitHelper
    {
        public static uint GetPageCount(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return 0;

            try
            {
                using (var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int n = Conversion.GetPageCount(fs, leaveOpen: true, password: null);
                    return n < 0 ? 0u : (uint)n;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 在工作线程加载 PDF 页为 <see cref="SKBitmap"/>，在 UI 线程编码为 WPF <see cref="BitmapSource"/>。
        /// </summary>
        public static async Task<BitmapSource> RenderPageToBitmapSourceAsync(string pdfPath, uint pageIndex)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return null;

            int page = checked((int)pageIndex);

            SKBitmap skBitmap = await Task.Run(() =>
            {
                try
                {
                    using (var fs = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        return Conversion.ToImage(fs, System.Index.FromStart(page), leaveOpen: true, password: null, options: default);
                    }
                }
                catch
                {
                    return null;
                }
            }).ConfigureAwait(false);

            if (skBitmap == null)
                return null;

            try
            {
                if (Application.Current?.Dispatcher == null)
                    return null;

                return await Application.Current.Dispatcher.InvokeAsync(() => EncodeSkBitmapToBitmapSource(skBitmap));
            }
            finally
            {
                skBitmap.Dispose();
            }
        }

        private static BitmapSource EncodeSkBitmapToBitmapSource(SKBitmap bitmap)
        {
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                var ms = new MemoryStream();
                data.SaveTo(ms);
                ms.Position = 0;

                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                ms.Dispose();
                return bi;
            }
        }
    }
}
