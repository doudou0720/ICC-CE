using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WebDav;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// WebDav上传工具类
    /// </summary>
    public static class WebDavUploader
    {
        /// <summary>
        /// 上传文件到WebDav服务器
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否上传成功</returns>
        public static async Task<bool> UploadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 检查文件是否存在
                if (!File.Exists(filePath))
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 上传失败：文件不存在 - {filePath}", LogHelper.LogType.Error);
                    return false;
                }

                // 获取WebDav设置
                var webDavUrl = MainWindow.Settings?.Dlass?.WebDavUrl;
                var username = MainWindow.Settings?.Dlass?.WebDavUsername;
                var password = MainWindow.Settings?.Dlass?.WebDavPassword;
                var rootDirectory = MainWindow.Settings?.Dlass?.WebDavRootDirectory;

                // 验证设置
                if (string.IsNullOrEmpty(webDavUrl))
                {
                    LogHelper.WriteLogToFile("[WebDAV] 上传失败：未设置WebDav地址", LogHelper.LogType.Error);
                    return false;
                }

                // 构建完整的目标路径
                var fileName = Path.GetFileName(filePath);
                var targetPath = Path.Combine(rootDirectory ?? string.Empty, fileName).Replace("\\", "/");
                if (targetPath.StartsWith("/"))
                {
                    targetPath = targetPath.Substring(1);
                }

                // 创建WebDav客户端
                var clientParams = new WebDavClientParams
                {
                    BaseAddress = new Uri(webDavUrl),
                    Credentials = new NetworkCredential(username ?? string.Empty, password ?? string.Empty)
                };

                using (var client = new WebDavClient(clientParams))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 确保目录存在
                    var directoryPath = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        await EnsureDirectoryExistsAsync(client, directoryPath, cancellationToken);
                    }

                    // 上传文件
                    using (var fileStream = File.OpenRead(filePath))
                    {
                        // 检查取消令牌
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var result = await client.PutFile(targetPath, fileStream);
                        if (result.IsSuccessful)
                        {
                            LogHelper.WriteLogToFile($"[WebDAV] 上传成功：{filePath} -> {targetPath}", LogHelper.LogType.Event);
                            return true;
                        }
                        else
                        {
                            LogHelper.WriteLogToFile($"[WebDAV] 上传失败：{filePath}, 状态码: {result.StatusCode}, 原因: {result.Description}", LogHelper.LogType.Error);
                            return false;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.WriteLogToFile("[WebDAV] 上传被取消", LogHelper.LogType.Event);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 上传异常：{ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 确保WebDav目录存在
        /// </summary>
        /// <param name="client">WebDav客户端</param>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        private static async Task EnsureDirectoryExistsAsync(IWebDavClient client, string directoryPath, CancellationToken cancellationToken)
        {
            try
            {
                // 分割路径并逐级创建目录
                var pathParts = directoryPath.Split('/');
                var currentPath = string.Empty;

                foreach (var part in pathParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (string.IsNullOrEmpty(part))
                        continue;

                    currentPath = Path.Combine(currentPath, part).Replace("\\", "/");

                    // 检查取消令牌
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 尝试创建目录
                    var result = await client.Mkcol(currentPath);
                    // 如果目录已存在，忽略错误（409 Conflict）
                    if (!result.IsSuccessful && result.StatusCode != 409)
                    {
                        LogHelper.WriteLogToFile($"[WebDAV] 创建目录失败：{currentPath}, 状态码: {result.StatusCode}", LogHelper.LogType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 确保目录存在时出错：{ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 检查WebDAV是否已启用
        /// </summary>
        /// <returns>是否启用</returns>
        public static bool IsWebDavEnabled()
        {
            // 检查WebDav设置是否有效
            var webDavUrl = MainWindow.Settings?.Dlass?.WebDavUrl;
            if (string.IsNullOrEmpty(webDavUrl))
            {
                return false;
            }

            // 尝试解析URL
            try
            {
                new Uri(webDavUrl);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
