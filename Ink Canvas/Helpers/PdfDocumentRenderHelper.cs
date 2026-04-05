using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// PDF 页数与位图渲染：优先 <see cref="PdfWinRtHelper"/>（Windows.Data.Pdf），失败或无效结果时使用 PDFtoImage(MIT) 备用。
    /// </summary>
    internal static class PdfDocumentRenderHelper
    {
        public static async Task<uint> GetPageCountAsync(string pdfPath)
        {
            uint winRt = 0;
            try
            {
                winRt = await PdfWinRtHelper.GetPageCountAsync(pdfPath).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PDF WinRT 获取页数失败，将尝试 PDFtoImage(MIT): {ex.Message}", LogHelper.LogType.Warning);
            }

            if (winRt > 0)
                return winRt;

            try
            {
                return await Task.Run(() => PdfToImageMitHelper.GetPageCount(pdfPath)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PDFtoImage(MIT) 获取页数失败: {ex.Message}", LogHelper.LogType.Warning);
                return 0;
            }
        }

        public static async Task<BitmapSource> RenderPageToBitmapSourceAsync(string pdfPath, uint pageIndex)
        {
            try
            {
                BitmapSource win = await PdfWinRtHelper.RenderPageToBitmapSourceAsync(pdfPath, pageIndex).ConfigureAwait(false);
                if (win != null)
                    return win;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PDF WinRT 渲染失败，将尝试 PDFtoImage(MIT): {ex.Message}", LogHelper.LogType.Warning);
            }

            try
            {
                return await PdfToImageMitHelper.RenderPageToBitmapSourceAsync(pdfPath, pageIndex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PDFtoImage(MIT) 渲染失败: {ex.Message}", LogHelper.LogType.Warning);
                return null;
            }
        }
    }
}
