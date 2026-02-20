using Microsoft.Office.Interop.PowerPoint;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Ink;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// PPT墨迹管理器 - 负责按幻灯片保存/加载墨迹、自动保存与内存管理。
    /// </summary>
    public class PPTInkManager : IDisposable
    {
        #region Properties
        public bool IsAutoSaveEnabled { get; set; } = true;
        public string AutoSaveLocation { get; set; } = "";
        public StrokeCollection CurrentStrokes { get; private set; } = new StrokeCollection();
        #endregion

        #region Private Fields
        private MemoryStream[] _memoryStreams;
        private const int DefaultMaxSlides = 100;
        private int _maxSlides = DefaultMaxSlides;
        private string _currentPresentationId = "";
        private readonly object _lockObject = new object();
        private bool _disposed;

        // 墨迹锁定机制，防止翻页时的墨迹冲突
        private DateTime _inkLockUntil = DateTime.MinValue;
        private int _lockedSlideIndex = -1;
        private const int InkLockMilliseconds = 500;

        // 添加快速切换保护机制
        private DateTime _lastSwitchTime = DateTime.MinValue;
        private int _lastSwitchSlideIndex = -1;
        private const int MinSwitchIntervalMs = 100;

        private long _totalMemoryUsage;
        private const long MaxMemoryUsageBytes = 100 * 1024 * 1024; // 100MB
        private DateTime _lastMemoryCleanup = DateTime.MinValue;
        private const int MemoryCleanupIntervalMinutes = 5;

        private const string StrokeFileExtension = ".icstk";
        #endregion

        #region Constructor
        /// <summary>
        /// 初始化一个 PPTInkManager 实例并为幻灯片墨迹数据分配默认的内存流缓存容量。
        /// </summary>
        public PPTInkManager()
        {
            InitializeMemoryStreams(DefaultMaxSlides + 2);
        }

        /// <summary>
        /// 根据指定容量初始化内部内存流数组，保证至少分配 2 个槽位。
        /// </summary>
        /// <param name="capacity">期望的数组容量；如果小于 2 则会使用 2。</param>
        private void InitializeMemoryStreams(int capacity)
        {
            _memoryStreams = new MemoryStream[Math.Max(2, capacity)];
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化新的演示文稿
        /// <summary>
        — 为指定的 PowerPoint 演示文稿初始化墨迹管理器的内部状态并准备存储结构。
        /// </summary>
        /// <param name="presentation">要初始化的 PowerPoint 演示文稿；若为 null 则不执行任何操作。</param>
        /// <remarks>
        /// 清除现有墨迹数据、重置锁与切换追踪状态、基于演示文稿生成唯一 ID，并根据幻灯片数量调整内部内存流数组的容量。
        /// 如果启用了自动保存且已配置保存位置，则会尝试从磁盘加载已保存的墨迹数据。
        /// 在读取幻灯片计数时若遇到特定 COM 错误 (HRESULT 0x80048010)，方法将提前返回而不抛出异常。
        /// </remarks>
        public void InitializePresentation(Presentation presentation)
        {
            if (presentation == null) return;

            lock (_lockObject)
            {
                try
                {
                    ClearAllStrokesInternal();
                    _inkLockUntil = DateTime.MinValue;
                    _lockedSlideIndex = -1;
                    _lastSwitchSlideIndex = -1;
                    _lastSwitchTime = DateTime.MinValue;

                    _currentPresentationId = GeneratePresentationId(presentation);

                    int slideCount = 0;
                    try
                    {
                        slideCount = presentation.Slides.Count;
                    }
                    catch (COMException comEx)
                    {
                        uint hr = (uint)comEx.HResult;
                        if (hr == 0x80048010) return;
                        throw;
                    }

                    int capacity = slideCount + 2;
                    _maxSlides = Math.Max(_maxSlides, slideCount);
                    _memoryStreams = new MemoryStream[capacity];

                    if (IsAutoSaveEnabled && !string.IsNullOrEmpty(AutoSaveLocation))
                        LoadSavedStrokes();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"初始化演示文稿墨迹管理失败: {ex}", LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 保存当前页面的墨迹
        /// <summary>
        /// 将指定幻灯片的墨迹保存到内存缓存并在必要时触发内存清理。
        /// </summary>
        /// <param name="slideIndex">要保存的幻灯片索引（基于1）。</param>
        /// <param name="strokes">要保存的墨迹集合；若为 null 则不执行保存。</param>
        public void SaveCurrentSlideStrokes(int slideIndex, StrokeCollection strokes)
        {
            if (slideIndex <= 0 || strokes == null) return;

            lock (_lockObject)
            {
                try
                {
                    if (!CanWriteInk(slideIndex)) return;
                    if (slideIndex >= _memoryStreams.Length) return;

                    ReplaceSlideStream(slideIndex, strokes);
                    CheckAndPerformMemoryCleanup();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"保存第{slideIndex}页墨迹失败: {ex}", LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 强制保存指定页墨迹到内存（不受锁定限制）。用于放映结束前保存当前画布到当前页。
        /// <summary>
        /// 强制将指定幻灯片的墨迹保存到管理器的内存存储，替换该幻灯片已有的墨迹数据并记录操作日志。
        /// </summary>
        /// <param name="slideIndex">要保存的幻灯片索引（从 1 开始）。如果小于或等于 0 或超过可用存储则不执行任何操作。</param>
        /// <param name="strokes">要保存的墨迹集合；若为 null 则不执行任何操作。</param>
        public void ForceSaveSlideStrokes(int slideIndex, StrokeCollection strokes)
        {
            if (slideIndex <= 0 || strokes == null) return;

            lock (_lockObject)
            {
                try
                {
                    if (slideIndex >= _memoryStreams.Length) return;
                    ReplaceSlideStream(slideIndex, strokes);
                    LogHelper.WriteLogToFile($"已强制保存第{slideIndex}页墨迹", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"强制保存第{slideIndex}页墨迹失败: {ex}", LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 加载指定页面的墨迹
        /// <summary>
        /// 获取指定幻灯片的墨迹集合。
        /// </summary>
        /// <param name="slideIndex">幻灯片的 1 为基准索引；小于等于 0 或无数据时视为无墨迹。</param>
        /// <returns>指定幻灯片的 `StrokeCollection`；若索引无效、没有已保存的墨迹或发生错误，则返回空的 `StrokeCollection`。</returns>
        public StrokeCollection LoadSlideStrokes(int slideIndex)
        {
            if (slideIndex <= 0) return new StrokeCollection();

            lock (_lockObject)
            {
                try
                {
                    if (slideIndex < _memoryStreams.Length && _memoryStreams[slideIndex] != null && _memoryStreams[slideIndex].Length > 0)
                    {
                        _memoryStreams[slideIndex].Position = 0;
                        return new StrokeCollection(_memoryStreams[slideIndex]);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"加载第{slideIndex}页墨迹失败: {ex}", LogHelper.LogType.Error);
                }
            }

            return new StrokeCollection();
        }

        /// <summary>
        /// 切换到指定页面并加载墨迹
        /// <summary>
        /// 切换到指定的幻灯片，加载并返回该幻灯片的墨迹集合，同时对目标幻灯片设置墨迹锁并启用快速切换保护以防止短时间内重复处理相同幻灯片的切换请求。
        /// </summary>
        /// <param name="slideIndex">要切换到的幻灯片索引，从 1 开始。</param>
        /// <param name="currentStrokes">可选：当前幻灯片的笔迹集合，允许为 null。</param>
        /// <returns>指定幻灯片的 <see cref="StrokeCollection"/>；发生错误或无数据时返回空的 <see cref="StrokeCollection"/>。</returns>
        public StrokeCollection SwitchToSlide(int slideIndex, StrokeCollection currentStrokes = null)
        {
            lock (_lockObject)
            {
                try
                {
                    var now = DateTime.Now;
                    if (now - _lastSwitchTime < TimeSpan.FromMilliseconds(MinSwitchIntervalMs) && _lastSwitchSlideIndex == slideIndex)
                    {
                        LogHelper.WriteLogToFile($"快速切换保护：忽略重复请求 页{slideIndex}", LogHelper.LogType.Trace);
                        return LoadSlideStrokes(slideIndex);
                    }

                    LockInkForSlide(slideIndex);
                    StrokeCollection newStrokes = LoadSlideStrokes(slideIndex);
                    _lastSwitchTime = now;
                    _lastSwitchSlideIndex = slideIndex;
                    return newStrokes;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"切换到第{slideIndex}页失败: {ex}", LogHelper.LogType.Error);
                    return new StrokeCollection();
                }
            }
        }

        /// <summary>
        /// 保存所有墨迹到文件
        /// </summary>
        /// <param name="presentation">演示文稿对象</param>
        /// <summary>
        /// 将当前内存中的所有幻灯片笔迹数据持久化到磁盘（按幻灯片分别存为文件），并保存当前播放位置。
        /// </summary>
        /// <param name="presentation">目标 PowerPoint 演示文稿，用于确定幻灯片总数。</param>
        /// <param name="currentSlideIndex">要保存的当前播放页码；如果小于等于 0，则使用已锁定的页码或最近切换的页码作为位置。</param>
        public void SaveAllStrokesToFile(Presentation presentation, int currentSlideIndex = -1)
        {
            if (!IsAutoSaveEnabled || string.IsNullOrEmpty(AutoSaveLocation) || presentation == null) return;

            lock (_lockObject)
            {
                try
                {
                    string folderPath = GetPresentationFolderPath();
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    int positionToSave = currentSlideIndex > 0 ? currentSlideIndex : (_lockedSlideIndex > 0 ? _lockedSlideIndex : _lastSwitchSlideIndex);
                    if (positionToSave > 0)
                    {
                        try { File.WriteAllText(Path.Combine(folderPath, "Position"), positionToSave.ToString()); }
                        catch (Exception ex) { LogHelper.WriteLogToFile($"保存 Position 失败: {ex}", LogHelper.LogType.Warning); }
                    }

                    int slideCount = 0;
                    try { slideCount = presentation.Slides.Count; }
                    catch (COMException comEx)
                    {
                        if ((uint)comEx.HResult == 0x80048010) return;
                        throw;
                    }

                    for (int i = 1; i <= slideCount && i < _memoryStreams.Length; i++)
                    {
                        if (_memoryStreams[i] == null) continue;
                        try
                        {
                            if (_memoryStreams[i].Length > 8)
                            {
                                _memoryStreams[i].Position = 0;
                                byte[] buf = new byte[_memoryStreams[i].Length];
                                int read = _memoryStreams[i].Read(buf, 0, buf.Length);
                                if (read > 0)
                                {
                                    string basePath = Path.Combine(folderPath, i.ToString("0000"));
                                    File.WriteAllBytes(basePath + StrokeFileExtension, buf);
                                }
                            }
                            else
                            {
                                TryDeleteStrokeFile(folderPath, i);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"保存第{i}页墨迹到文件失败: {ex}", LogHelper.LogType.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"保存墨迹到文件失败: {ex}", LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 从文件加载已保存的墨迹
        /// <summary>
        /// 从自动保存目录加载当前演示文稿的已保存墨迹文件到内存缓存中，按文件名推断幻灯片索引并恢复到相应内存槽位。
        /// </summary>
        /// <remarks>
        /// - 在 IsAutoSaveEnabled 为 false 或 AutoSaveLocation 未设置时不会执行任何操作。
        /// - 仅处理以 <see cref="StrokeFileExtension"/> 结尾且文件名（去除扩展名）能解析为大于 0 的整数的文件；该整数被视为幻灯片索引并映射到内部内存流数组的对应位置（超出范围的索引将被忽略）。 
        /// - 仅在文件内容长度大于 8 字节时才会将其加载为内存流；加载过程中的单个文件错误会记录但不会中断整体加载流程。 
        /// - 方法是线程安全的，会在完成后记录已成功加载的文件数量。
        /// </remarks>
        public void LoadSavedStrokes()
        {
            if (!IsAutoSaveEnabled || string.IsNullOrEmpty(AutoSaveLocation)) return;

            lock (_lockObject)
            {
                try
                {
                    string folderPath = GetPresentationFolderPath();
                    if (!Directory.Exists(folderPath)) return;

                    var dir = new DirectoryInfo(folderPath);
                    int loadedCount = 0;
                    foreach (FileInfo file in dir.GetFiles("*" + StrokeFileExtension))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(file.Name);
                        if (!int.TryParse(nameWithoutExt, out int slideIndex) || slideIndex <= 0) continue;
                        if (slideIndex >= _memoryStreams.Length) continue;

                        try
                        {
                            byte[] bytes = File.ReadAllBytes(file.FullName);
                            if (bytes.Length > 8)
                            {
                                _memoryStreams[slideIndex] = new MemoryStream(bytes);
                                _memoryStreams[slideIndex].Position = 0;
                                loadedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"加载墨迹文件 {file.Name} 失败: {ex}", LogHelper.LogType.Error);
                        }
                    }

                    if (loadedCount > 0)
                        LogHelper.WriteLogToFile($"已从磁盘加载 {loadedCount} 页墨迹", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"从文件加载墨迹失败: {ex}", LogHelper.LogType.Error);
                }
            }
        }

        /// <summary>
        /// 清除所有墨迹
        /// <summary>
        /// 清除管理器内存中保存的所有笔迹数据并重置当前的笔迹集合。
        /// </summary>
        /// <remarks>
        /// 在内部使用锁以保证线程安全；此操作只影响内存中的笔迹和 CurrentStrokes，不会直接写入或删除磁盘上的自动保存文件。 
        /// </remarks>
        public void ClearAllStrokes()
        {
            lock (_lockObject)
            {
                ClearAllStrokesInternal();
            }
        }

        /// <summary>
        /// 为指定的幻灯片设置临时的墨迹写入锁，记录被锁定的幻灯片索引并设置锁的到期时间。
        /// </summary>
        /// <param name="slideIndex">要锁定的幻灯片索引（基于演示文稿的索引值）。</param>
        public void LockInkForSlide(int slideIndex)
        {
            _inkLockUntil = DateTime.Now.AddMilliseconds(InkLockMilliseconds);
            _lockedSlideIndex = slideIndex;
        }

        /// <summary>
        /// 确定在给定幻灯片上下文中当前是否允许写入墨迹（绘制/保存）。
        /// </summary>
        /// <param name="currentSlideIndex">当前活动幻灯片的索引（与类中使用的索引约定一致）。</param>
        /// <returns>`true` 如果当前时间已超过墨迹锁过期、或当前幻灯片与锁定幻灯片相同、或刚刚解锁（短时间窗口内）允许写入；否则为 `false`。</returns>
        public bool CanWriteInk(int currentSlideIndex)
        {
            if (DateTime.Now >= _inkLockUntil) return true;
            if (currentSlideIndex == _lockedSlideIndex) return true;
            if (DateTime.Now - (_inkLockUntil.AddMilliseconds(-InkLockMilliseconds)) < TimeSpan.FromMilliseconds(50)) return true;
            return false;
        }

        /// <summary>
        /// 将墨迹锁和幻灯片切换相关的状态重置为未锁定和初始值。
        /// </summary>
        /// <remarks>
        /// 重置包括清除墨迹锁过期时间、解锁当前幻灯片索引，以及清空上次切换时间和上次切换幻灯片索引。
        /// </remarks>
        public void ResetLockState()
        {
            lock (_lockObject)
            {
                _inkLockUntil = DateTime.MinValue;
                _lockedSlideIndex = -1;
                _lastSwitchTime = DateTime.MinValue;
                _lastSwitchSlideIndex = -1;
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// 释放并清除管理器中所有内存中的笔迹数据。
        /// </summary>
        /// <remarks>
        /// 该方法逐个释放并置空内部的内存流数组，随后将内存流数组重建为容量为 _maxSlides + 2 的新数组，并清空 CurrentStrokes；操作完成后记录一条跟踪日志。
        /// </remarks>
        private void ClearAllStrokesInternal()
        {
            if (_memoryStreams != null)
            {
                for (int i = 0; i < _memoryStreams.Length; i++)
                {
                    try { _memoryStreams[i]?.Dispose(); } catch (Exception ex) { LogHelper.WriteLogToFile($"释放内存流 {i} 失败: {ex}", LogHelper.LogType.Warning); }
                    finally { _memoryStreams[i] = null; }
                }
                _memoryStreams = new MemoryStream[_maxSlides + 2];
            }
            CurrentStrokes?.Clear();
            LogHelper.WriteLogToFile("已清除所有墨迹", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 用指定的 StrokeCollection 替换内存中对应幻灯片的笔迹数据流。
        /// </summary>
        /// <remarks>
        /// 该方法会释放原有内存流（若存在），将传入的 strokes 序列化写入新的 MemoryStream，并将流位置重置到起始处以备后续读取。
        /// </remarks>
        private void ReplaceSlideStream(int slideIndex, StrokeCollection strokes)
        {
            try { _memoryStreams[slideIndex]?.Dispose(); } catch (Exception ex) { LogHelper.WriteLogToFile($"释放旧内存流失败: {ex}", LogHelper.LogType.Warning); }
            var ms = new MemoryStream();
            strokes.Save(ms);
            ms.Position = 0;
            _memoryStreams[slideIndex] = ms;
        }

        /// <summary>
        /// 尝试删除指定文件夹中对应幻灯片编号的自动保存墨迹文件（如果存在）。操作在发生任何异常时会被静默忽略，不会抛出异常。
        /// </summary>
        /// <param name="folderPath">包含自动保存墨迹文件的目录路径。</param>
        /// <param name="slideIndex">幻灯片索引（以 1 为起始），用于构造文件名（格式为四位数，例如 0001）并附加扩展名。</param>
        private void TryDeleteStrokeFile(string folderPath, int slideIndex)
        {
            try
            {
                string path = Path.Combine(folderPath, slideIndex.ToString("0000") + StrokeFileExtension);
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }

        /// <summary>
        /// 定期检查已缓存的墨迹内存占用并在超过阈值时触发释放不活跃幻灯片的墨迹数据以回收内存。
        /// </summary>
        /// <remarks>
        /// 更新内部计数器 `_totalMemoryUsage` 并刷新最后一次清理时间戳 `_lastMemoryCleanup`。当检测到内存使用量超过 `MaxMemoryUsageBytes` 时，会记录警告并调用 `CleanupInactiveSlideStrokes` 执行释放操作；若检查过程发生异常，则记录错误日志。
        /// </remarks>
        private void CheckAndPerformMemoryCleanup()
        {
            try
            {
                var now = DateTime.Now;
                if (now - _lastMemoryCleanup < TimeSpan.FromMinutes(MemoryCleanupIntervalMinutes)) return;

                long currentMemoryUsage = 0;
                if (_memoryStreams != null)
                {
                    for (int i = 0; i < _memoryStreams.Length; i++)
                        if (_memoryStreams[i] != null) currentMemoryUsage += _memoryStreams[i].Length;
                }
                _totalMemoryUsage = currentMemoryUsage;

                if (currentMemoryUsage > MaxMemoryUsageBytes)
                {
                    LogHelper.WriteLogToFile($"墨迹内存超限 ({currentMemoryUsage / (1024 * 1024)}MB)，执行清理", LogHelper.LogType.Warning);
                    CleanupInactiveSlideStrokes();
                }
                _lastMemoryCleanup = now;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"内存清理检查失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 释放未使用幻灯片的内存墨迹数据，处置并清空对应的内存流，保留当前锁定页与最近切换页。
        /// </summary>
        /// <remarks>
        /// 遍历内部内存流数组，处置非活动（既不是锁定页也不是最近切换页）的流并将数组槽位设为 <c>null</c>；若释放了任何数据，会写入一次跟踪日志，包含释放的页数和近似大小（KB）。
        /// </remarks>
        private void CleanupInactiveSlideStrokes()
        {
            if (_memoryStreams == null) return;
            int cleaned = 0;
            long freed = 0;
            for (int i = 0; i < _memoryStreams.Length; i++)
            {
                if (i == _lockedSlideIndex || i == _lastSwitchSlideIndex) continue;
                if (_memoryStreams[i] != null)
                {
                    long len = _memoryStreams[i].Length;
                    try { _memoryStreams[i].Dispose(); freed += len; cleaned++; } catch { }
                    finally { _memoryStreams[i] = null; }
                }
            }
            if (cleaned > 0)
                LogHelper.WriteLogToFile($"已清理 {cleaned} 页墨迹，释放 {freed / 1024}KB", LogHelper.LogType.Trace);
        }

        /// <summary>
        /// 生成用于标识给定演示文稿的 ID，用于区分不同文件或不同版本的同一文件。
        /// </summary>
        /// <param name="presentation">要为其生成 ID 的 PowerPoint 演示文稿对象。</param>
        /// <returns>由演示文稿名称、幻灯片数量和文件路径哈希组成的字符串；若生成失败，则返回形如 `unknown_<ticks>` 的备用标识符。</returns>
        private string GeneratePresentationId(Presentation presentation)
        {
            try
            {
                string path = presentation.FullName;
                string hash = GetFileHash(path);
                return $"{presentation.Name}_{presentation.Slides.Count}_{hash}";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"生成演示文稿 ID 失败: {ex}", LogHelper.LogType.Error);
                return $"unknown_{DateTime.Now.Ticks}";
            }
        }

        /// <summary>
        /// 基于提供的文件路径字符串生成一个短的 8 字符 MD5 哈希摘要。
        /// </summary>
        /// <param name="filePath">用于计算哈希的文件路径字符串；若为 null 或空字符串则返回 "unknown"。</param>
        /// <returns>8 字符的十六进制哈希字符串；在处理失败时返回 "error"。</returns>
        private static string GetFileHash(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return "unknown";
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(filePath));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"计算文件哈希失败: {ex}", LogHelper.LogType.Error);
                return "error";
            }
        }

        /// <summary>
        /// 构建并返回当前演示文稿的自动保存文件夹路径。
        /// </summary>
        /// <returns>基于 AutoSaveLocation 和当前演示文稿 ID 组成的用于存放自动保存数据的完整文件夹路径。</returns>
        private string GetPresentationFolderPath()
        {
            return Path.Combine(AutoSaveLocation, "Auto Saved - Presentations", _currentPresentationId);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// 释放管理器持有的所有内存和资源并清除所有笔迹数据；可重复调用且线程安全。
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            lock (_lockObject) { ClearAllStrokesInternal(); }
            _disposed = true;
        }

        #endregion
    }
}