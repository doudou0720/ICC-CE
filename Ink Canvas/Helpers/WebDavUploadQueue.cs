using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// WebDAV上传队列辅助类
    /// </summary>
    public class WebDavUploadQueue
    {
        private const int BATCH_SIZE = 10; // 批量上传大小
        private const int MAX_RETRY_COUNT = 3; // 最大重试次数
        private const string QUEUE_FILE_NAME = "WebDavUploadQueue.json";

        /// <summary>
        /// 上传队列项
        /// </summary>
        private class UploadQueueItemData
        {
            [JsonProperty("file_path")]
            public string FilePath { get; set; }

            [JsonProperty("retry_count")]
            public int RetryCount { get; set; }

            [JsonProperty("added_time")]
            public DateTime AddedTime { get; set; }
        }

        /// <summary>
        /// 上传队列项
        /// </summary>
        private class UploadQueueItem
        {
            public string FilePath { get; set; }
            public int RetryCount { get; set; }
        }

        /// <summary>
        /// 上传队列
        /// </summary>
        private static readonly ConcurrentQueue<UploadQueueItem> _uploadQueue = new ConcurrentQueue<UploadQueueItem>();

        /// <summary>
        /// 队列处理锁，防止并发处理
        /// </summary>
        private static readonly SemaphoreSlim _queueProcessingLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 队列保存锁，防止并发保存
        /// </summary>
        private static readonly SemaphoreSlim _queueSaveLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 是否已初始化队列
        /// </summary>
        private static bool _isQueueInitialized = false;

        /// <summary>
        /// 获取队列文件路径
        /// </summary>
        private static string GetQueueFilePath()
        {
            var configsDir = Path.Combine(App.RootPath, "Configs");
            if (!Directory.Exists(configsDir))
            {
                Directory.CreateDirectory(configsDir);
            }
            return Path.Combine(configsDir, QUEUE_FILE_NAME);
        }

        /// <summary>
        /// 初始化上传队列
        /// </summary>
        public static void InitializeQueue()
        {
            if (_isQueueInitialized)
            {
                return;
            }

            try
            {
                var queueFilePath = GetQueueFilePath();
                if (!File.Exists(queueFilePath))
                {
                    _isQueueInitialized = true;
                    return;
                }

                var jsonContent = File.ReadAllText(queueFilePath);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _isQueueInitialized = true;
                    return;
                }

                var queueData = JsonConvert.DeserializeObject<List<UploadQueueItemData>>(jsonContent);
                if (queueData == null || queueData.Count == 0)
                {
                    _isQueueInitialized = true;
                    return;
                }

                int restoredCount = 0;
                int skippedCount = 0;

                foreach (var item in queueData)
                {
                    // 验证文件是否存在
                    if (!File.Exists(item.FilePath))
                    {
                        skippedCount++;
                        continue;
                    }

                    // 验证文件格式和大小
                    var fileExtension = Path.GetExtension(item.FilePath).ToLower();
                    if (fileExtension != ".png" && fileExtension != ".icstk" && fileExtension != ".xml" && fileExtension != ".zip")
                    {
                        skippedCount++;
                        continue;
                    }

                    try
                    {
                        var fileInfo = new FileInfo(item.FilePath);
                        long maxSize = fileExtension == ".zip" ? 50 * 1024 * 1024 : 10 * 1024 * 1024;
                        if (fileInfo.Length > maxSize)
                        {
                            skippedCount++;
                            continue;
                        }
                    }
                    catch
                    {
                        skippedCount++;
                        continue;
                    }

                    // 恢复队列项
                    _uploadQueue.Enqueue(new UploadQueueItem
                    {
                        FilePath = item.FilePath,
                        RetryCount = item.RetryCount
                    });
                    restoredCount++;
                }

                _isQueueInitialized = true;

                if (restoredCount > 0)
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 已恢复上传队列：{restoredCount}个文件，跳过{skippedCount}个无效文件", LogHelper.LogType.Event);
                    // 如果恢复了队列，触发处理
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessUploadQueueAsync().ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"恢复WebDAV上传队列后处理时出错: {ex}", LogHelper.LogType.Error);
                        }
                    });
                }
                else if (skippedCount > 0)
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 队列恢复完成：跳过{skippedCount}个无效文件", LogHelper.LogType.Event);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 恢复上传队列时出错: {ex.Message}", LogHelper.LogType.Error);
                _isQueueInitialized = true; // 即使出错也标记为已初始化，避免重复尝试
            }
        }

        /// <summary>
        /// 保存队列到文件
        /// </summary>
        private static async Task SaveQueueToFileAsync(CancellationToken cancellationToken = default)
        {
            if (!await _queueSaveLock.WaitAsync(1000, cancellationToken)) // 最多等待1秒
            {
                return; // 如果无法获取锁，跳过保存（避免阻塞）
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var queueData = new List<UploadQueueItemData>();

                // 将队列转换为可序列化的格式
                foreach (var item in _uploadQueue)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    queueData.Add(new UploadQueueItemData
                    {
                        FilePath = item.FilePath,
                        RetryCount = item.RetryCount,
                        AddedTime = DateTime.Now
                    });
                }

                var queueFilePath = GetQueueFilePath();

                // 如果队列为空，清空文件
                if (queueData.Count == 0)
                {
                    ClearQueueFile();
                    return;
                }

                var jsonContent = JsonConvert.SerializeObject(queueData, Formatting.Indented);

                // 使用进程保护的写入门控，避免安全面板中"进程文件保护"占用导致无法写入
                var tempFilePath = queueFilePath + ".tmp";
                ProcessProtectionManager.WithWriteAccess(queueFilePath, () =>
                {
                    File.WriteAllText(tempFilePath, jsonContent);
                    if (File.Exists(queueFilePath))
                        File.Delete(queueFilePath);
                    File.Move(tempFilePath, queueFilePath);
                });
            }
            catch (OperationCanceledException)
            {
                // 取消操作，静默处理
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 保存上传队列时出错: {ex.Message}", LogHelper.LogType.Error);
            }
            finally
            {
                _queueSaveLock.Release();
            }
        }

        /// <summary>
        /// 清空队列文件
        /// </summary>
        private static void ClearQueueFile()
        {
            try
            {
                var queueFilePath = GetQueueFilePath();
                ProcessProtectionManager.WithWriteAccess(queueFilePath, () =>
                {
                    if (File.Exists(queueFilePath))
                        File.WriteAllText(queueFilePath, "[]");
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 清空队列文件时出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 异步上传文件到WebDAV
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功加入队列（不等待实际上传完成）</returns>
        public static async Task<bool> UploadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // 检查是否启用WebDAV上传
                if (!WebDavUploader.IsWebDavEnabled())
                {
                    return false;
                }

                // 基本验证
                if (!File.Exists(filePath))
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 上传失败：文件不存在 - {filePath}", LogHelper.LogType.Error);
                    return false;
                }

                var fileExtension = Path.GetExtension(filePath).ToLower();
                if (fileExtension != ".png" && fileExtension != ".icstk" && fileExtension != ".xml" && fileExtension != ".zip")
                {
                    return false;
                }

                var fileInfo = new FileInfo(filePath);
                long maxSize = fileExtension == ".zip" ? 50 * 1024 * 1024 : 10 * 1024 * 1024;
                if (fileInfo.Length > maxSize)
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 上传失败：文件过大（{fileInfo.Length / 1024 / 1024}MB），超过{maxSize / 1024 / 1024}MB限制", LogHelper.LogType.Error);
                    return false;
                }

                // 确保队列已初始化
                if (!_isQueueInitialized)
                {
                    InitializeQueue();
                }

                // 加入队列
                EnqueueFile(filePath, 0, cancellationToken);

                return true;
            }
            catch (OperationCanceledException)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 上传被取消: {Path.GetFileName(filePath)}", LogHelper.LogType.Event);
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"[WebDAV] 加入上传队列时出错: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 将文件加入上传队列
        /// </summary>
        private static void EnqueueFile(string filePath, int retryCount = 0, CancellationToken cancellationToken = default)
        {
            _uploadQueue.Enqueue(new UploadQueueItem
            {
                FilePath = filePath,
                RetryCount = retryCount
            });

            // 异步保存队列到文件
            _ = Task.Run(async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await SaveQueueToFileAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // 取消操作，静默处理
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"[WebDAV] 保存上传队列时出错（后台任务）: {ex}", LogHelper.LogType.Error);
                }
            }, cancellationToken);

            // 只要有文件加入队列就触发处理
            _ = ProcessUploadQueueAsync(cancellationToken);
        }

        /// <summary>
        /// 处理上传队列，批量上传文件
        /// </summary>
        private static async Task ProcessUploadQueueAsync(CancellationToken cancellationToken = default)
        {
            // 使用信号量防止并发处理
            if (!await _queueProcessingLock.WaitAsync(0, cancellationToken))
            {
                return; // 已有处理任务在运行
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var filesToUpload = new List<UploadQueueItem>();

                // 从队列中取出最多BATCH_SIZE个文件
                int count = 0;
                while (_uploadQueue.TryDequeue(out UploadQueueItem item) && count < BATCH_SIZE)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // 再次检查文件是否存在
                    if (File.Exists(item.FilePath))
                    {
                        filesToUpload.Add(item);
                        count++;
                    }
                }

                if (filesToUpload.Count == 0)
                {
                    return;
                }

                // 检查WebDAV设置
                if (!WebDavUploader.IsWebDavEnabled())
                {
                    LogHelper.WriteLogToFile("[WebDAV] 上传失败：WebDAV未启用", LogHelper.LogType.Error);
                    // 将文件重新加入队列
                    foreach (var item in filesToUpload)
                    {
                        EnqueueFile(item.FilePath, item.RetryCount, cancellationToken);
                    }
                    return;
                }

                // 并发上传所有文件，并处理失败重试
                var uploadTasks = filesToUpload.Select(async item =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var success = await WebDavUploader.UploadFileAsync(item.FilePath, cancellationToken);
                        if (!success)
                        {
                            // 检查是否是可重试的错误
                            if (IsRetryableError(item.FilePath))
                            {
                                // 检查重试次数
                                if (item.RetryCount < MAX_RETRY_COUNT)
                                {
                                    LogHelper.WriteLogToFile($"[WebDAV] 上传失败，将重试 ({item.RetryCount + 1}/{MAX_RETRY_COUNT}): {Path.GetFileName(item.FilePath)}", LogHelper.LogType.Event);
                                    EnqueueFile(item.FilePath, item.RetryCount + 1, cancellationToken);
                                }
                                else
                                {
                                    LogHelper.WriteLogToFile($"[WebDAV] 上传失败，已达到最大重试次数: {Path.GetFileName(item.FilePath)}", LogHelper.LogType.Error);
                                }
                            }
                        }
                        else
                        {
                            LogHelper.WriteLogToFile($"[WebDAV] 上传成功: {Path.GetFileName(item.FilePath)}", LogHelper.LogType.Event);
                        }
                        return success;
                    }
                    catch (OperationCanceledException)
                    {
                        // 取消操作，将文件重新加入队列
                        EnqueueFile(item.FilePath, item.RetryCount, cancellationToken);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // 检查是否是可重试的错误（超时、网络错误等）
                        var errorMessage = ex.Message.ToLower();
                        bool isRetryable = errorMessage.Contains("超时") ||
                                          errorMessage.Contains("timeout") ||
                                          errorMessage.Contains("网络错误") ||
                                          errorMessage.Contains("network") ||
                                          errorMessage.Contains("408") || // 请求超时
                                          errorMessage.Contains("423") || // 资源锁定
                                          errorMessage.Contains("429") || // 请求过多
                                          errorMessage.Contains("500") || // 服务器错误
                                          errorMessage.Contains("502") || // 网关错误
                                          errorMessage.Contains("503") || // 服务不可用
                                          errorMessage.Contains("504"); // 网关超时

                        if (isRetryable && IsRetryableError(item.FilePath))
                        {
                            // 检查重试次数
                            if (item.RetryCount < MAX_RETRY_COUNT)
                            {
                                LogHelper.WriteLogToFile($"[WebDAV] 上传失败({ex.Message})，将重试 ({item.RetryCount + 1}/{MAX_RETRY_COUNT}): {Path.GetFileName(item.FilePath)}", LogHelper.LogType.Event);
                                EnqueueFile(item.FilePath, item.RetryCount + 1, cancellationToken);
                            }
                            else
                            {
                                LogHelper.WriteLogToFile($"[WebDAV] 上传失败，已达到最大重试次数: {Path.GetFileName(item.FilePath)}", LogHelper.LogType.Error);
                            }
                        }
                        else
                        {
                            LogHelper.WriteLogToFile($"[WebDAV] 上传失败（不可重试）: {Path.GetFileName(item.FilePath)} - {ex.Message}", LogHelper.LogType.Error);
                        }
                        return false;
                    }
                });
                await Task.WhenAll(uploadTasks);

                // 上传完成后保存队列状态
                await SaveQueueToFileAsync(cancellationToken);

                // 检查队列中是否还有文件，如果有就继续处理
                if (_uploadQueue.Count > 0)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessUploadQueueAsync(cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"[WebDAV] 继续处理上传队列时出错: {ex}", LogHelper.LogType.Error);
                        }
                    }, cancellationToken);
                }
            }
            finally
            {
                _queueProcessingLock.Release();
            }
        }

        /// <summary>
        /// 判断错误是否可重试
        /// </summary>
        private static bool IsRetryableError(string filePath)
        {
            // 检查文件是否存在
            if (!File.Exists(filePath))
            {
                return false; // 文件不存在，不可重试
            }

            // 检查文件扩展名
            var fileExtension = Path.GetExtension(filePath).ToLower();
            if (fileExtension != ".png" && fileExtension != ".icstk" && fileExtension != ".xml" && fileExtension != ".zip")
            {
                return false; // 文件格式错误，不可重试
            }

            // 检查文件大小
            try
            {
                var fileInfo = new FileInfo(filePath);
                long maxSize = fileExtension == ".zip" ? 50 * 1024 * 1024 : 10 * 1024 * 1024;
                if (fileInfo.Length > maxSize)
                {
                    return false; // 文件过大，不可重试
                }
            }
            catch
            {
                return false; // 无法读取文件信息，不可重试
            }

            // 检查WebDAV设置是否仍然有效
            if (!WebDavUploader.IsWebDavEnabled())
            {
                return false; // WebDAV未启用，不可重试
            }

            // 其他错误（超时、网络错误等）可以重试
            return true;
        }
    }
}
