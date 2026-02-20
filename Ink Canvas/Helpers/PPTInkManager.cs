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
        /// 初始化 PPTInkManager 实例并为内部内存流分配初始容量以跟踪默认最大幻灯片数加上备用槽位。
        /// <summary>
        /// 创建一个 PPTInkManager 实例，并为按幻灯片存储的墨迹在内存中分配初始缓冲区。
        /// </summary>
        /// <remarks>
        /// 初始缓冲区容量为 DefaultMaxSlides + 2，用于存放各幻灯片的内存流占位。
        /// </remarks>
        public PPTInkManager()
        {
            InitializeMemoryStreams(DefaultMaxSlides + 2);
        }

        /// <summary>
        /// 根据指定容量初始化用于存储每页墨迹的内存流数组。
        /// </summary>
        /// <summary>
        /// 为幻灯片墨迹缓存分配或重建用于存放 MemoryStream 的数组。
        /// </summary>
        /// <param name="capacity">期望的数组容量；如果小于 2，则使用 2 作为最小容量。</param>
        private void InitializeMemoryStreams(int capacity)
        {
            _memoryStreams = new MemoryStream[Math.Max(2, capacity)];
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化新的演示文稿
        /// </summary>
        /// <remarks>
        /// 为新的或当前的演示文稿初始化墨迹管理器的内部状态。
        /// 方法会清除所有内存中的笔迹数据，重置墨迹写入锁与快速切换追踪，并根据演示文稿的幻灯片数量分配内部内存缓冲区。
        /// 如果已启用自动保存且设置了 <see cref="AutoSaveLocation"/>，则会尝试加载磁盘上的已保存墨迹文件。
        /// </remarks>
        /// <summary>
        /// 为指定的 PowerPoint 演示文稿初始化墨迹管理器的状态与内存缓冲区。
        /// </summary>
        /// <remarks>
        /// 方法会在内部清除现有内存中的所有笔迹并重置锁与切换跟踪状态，基于演示文稿的幻灯片数量分配内部流数组，并在启用自动保存且已设置保存目录时尝试从磁盘加载已保存的墨迹。若遇到特定 COM 错误（HRESULT = 0x80048010）则方法会提前返回而不抛出异常；其它异常将被捕获并记录日志但不会向上抛出。
        /// </remarks>
        /// <param name="presentation">要初始化的 PowerPoint Presentation 实例；为 null 时方法不执行任何操作并直接返回。</param>
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
        /// </summary>
        /// <remarks>
        /// 将指定幻灯片的墨迹保存到内部内存缓存，并在必要时触发内存清理。
        /// </remarks>
        /// <param name="slideIndex">要保存的幻灯片索引（从 1 开始）。方法在索引小于或等于 0 时不执行任何操作。</param>
        /// <summary>
        /// 将指定幻灯片的墨迹保存到内存缓存（按幻灯片索引存储），并在必要时触发内存清理。
        /// </summary>
        /// <param name="slideIndex">目标幻灯片的索引，必须大于 0（1 起算）；无效索引将导致方法不执行任何操作。</param>
        /// <param name="strokes">要保存的墨迹集合；为 null 时方法不执行任何操作。</param>
        /// <remarks>
        /// 如果当前无法写入（受写入锁保护）或索引超出内部缓冲范围，方法为无操作；遇到异常时会记录日志并吞掉异常，不会抛出给调用方。
        /// </remarks>
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
        /// </summary>
        /// <remarks>
        /// 强制将指定幻灯片的墨迹保存到内部内存缓存，覆盖该幻灯片已有的墨迹数据。
        /// </remarks>
        /// <param name="slideIndex">要保存的幻灯片索引（从 1 开始）。</param>
        /// <summary>
        /// 强制将指定幻灯片的墨迹保存到内存缓存，不受常规定义的写入锁限制。
        /// </summary>
        /// <param name="slideIndex">目标幻灯片索引，必须大于 0 并在当前内存缓冲区范围内。</param>
        /// <param name="strokes">要保存的墨迹集合，不能为空。</param>
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
        /// </summary>
        /// <remarks>
        /// 加载并返回指定幻灯片的墨迹集合。
        /// </remarks>
        /// <param name="slideIndex">要加载的幻灯片索引（从1开始）。</param>
        /// <summary>
        /// 加载并返回指定幻灯片的墨迹集合。
        /// </summary>
        /// <param name="slideIndex">要加载的幻灯片索引（必须大于 0）。</param>
        /// <returns>包含指定幻灯片的墨迹的 <see cref="StrokeCollection"/>；如果该幻灯片没有已保存的墨迹或加载失败，则返回空的 <see cref="StrokeCollection"/>。</returns>
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
        /// </summary>
        /// <remarks>
        /// 切换到指定幻灯片并返回该幻灯片的已加载笔迹集合。
        /// </remarks>
        /// <param name="slideIndex">要切换到的幻灯片索引（从 1 开始）。</param>
        /// <param name="currentStrokes">可选的当前笔迹集合，用于在切换时提供当前画面状态。</param>
        /// <summary>
        /// 切换到指定幻灯片并加载该幻灯片的墨迹集合。
        /// </summary>
        /// <param name="slideIndex">目标幻灯片的索引（从 0 开始）。</param>
        /// <param name="currentStrokes">可选的当前笔迹集合；此方法不会修改该对象，仅用于调用上下文的传递。</param>
        /// <returns>`StrokeCollection`：指定幻灯片已加载的笔迹集合；若加载失败或不存在笔迹则返回一个空的 `StrokeCollection`。</returns>
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
        /// <remarks>
        /// 将内存中当前演示文稿的每页墨迹保存到磁盘，并根据情况写入当前播放位置文件。
        /// 仅在 IsAutoSaveEnabled 为真且 AutoSaveLocation 已设置时执行。会在演示文稿专属文件夹中写入按页编号的墨迹文件（带 `.icstk` 扩展名）和可选的 Position 文件。遇到特定 COM 错误（HRESULT 0x80048010）时会中止保存当前幻灯片计数读取而不抛出异常；单页保存失败会记录错误并继续处理其他页。
        /// </remarks>
        /// <param name="presentation">要保存墨迹的 PowerPoint 演示文稿对象。</param>
        /// <summary>
        /// 将当前演示文稿的内存墨迹按幻灯片写入自动保存目录（AutoSaveLocation）中的单独文件，并可保存当前播放位置。
        /// </summary>
        /// <param name="presentation">要保存其墨迹的 PowerPoint 演示文稿，用于确定幻灯片数量和生成保存目录。</param>
        /// <param name="currentSlideIndex">当前播放的页码；若大于 0 则作为 Position 文件写入保存位置，否则使用已锁定的页码或最后切换的页码作为保存位置。</param>
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
        /// </summary>
        /// <remarks>
        /// 从自动保存目录加载已保存的幻灯片墨迹数据到内存流中，供后续显示和编辑使用。
        /// 仅在启用自动保存且已设置 AutoSaveLocation 时执行。函数获取当前演示文稿的自动保存文件夹，遍历以 <c>.icstk</c> 为扩展名的文件，
        /// 将文件名（去除扩展名）解析为幻灯片索引并在合法且文件大小大于 8 字节时加载到对应的内存流槽位。对单个文件的读取失败会记录错误并继续处理其他文件；
        /// 若成功加载则会记录已加载页数。方法在内部使用锁以保证线程安全。
        /// <summary>
        /// 将自动保存目录中按幻灯片编号存放的墨迹文件加载到内存缓存中。
        /// </summary>
        /// <remarks>
        /// 仅当自动保存启用且已设置 AutoSaveLocation 时执行。方法会从当前演示文稿的自动保存文件夹读取扩展名为 <c>.icstk</c> 的文件，
        /// 将文件名（不含扩展名）解析为幻灯片索引并将有效且大小大于 8 字节的文件内容填充到对应的内存流槽位（_memoryStreams）中。
        /// 无效文件、索引越界或读取失败的单个文件会被跳过并记录错误；完成后会记录已加载的页数。
        /// 该方法在内部使用锁以保证线程安全。
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
        /// </summary>
        /// <remarks>
        /// 清除并释放当前演示文稿所有幻灯片的墨迹数据和相关内存资源。
        /// 该方法在内部加锁以保证线程安全；会处置并清空所有内部存储的墨迹流、重建内部流数组并清空 CurrentStrokes。
        /// <summary>
        /// 清除并释放所有幻灯片的墨迹缓存与相关内存资源，并清空当前笔迹集合。
        /// </summary>
        /// <remarks>
        /// 此操作在内部加锁以保证线程安全。调用后内存中保存的每页笔迹会被释放，CurrentStrokes 将被清空。
        /// </remarks>
        public void ClearAllStrokes()
        {
            lock (_lockObject)
            {
                ClearAllStrokesInternal();
            }
        }

        /// <summary>
        /// 为指定幻灯片设置短时墨迹写入锁，防止在该时间窗口内对其他幻灯片进行写入操作。
        /// </summary>
        /// <summary>
        /// 为指定幻灯片设置短时墨迹写入锁，防止在锁定期间写入该幻灯片的墨迹。
        /// </summary>
        /// <param name="slideIndex">要上锁的幻灯片索引（大于 0）。锁从调用时刻开始，持续 InkLockMilliseconds 毫秒。</param>
        public void LockInkForSlide(int slideIndex)
        {
            _inkLockUntil = DateTime.Now.AddMilliseconds(InkLockMilliseconds);
            _lockedSlideIndex = slideIndex;
        }

        /// <summary>
        /// 确定在当前滑页上下文中是否允许写入墨迹（基于短期的墨迹写入锁与容差窗口）。
        /// </summary>
        /// <param name="currentSlideIndex">当前尝试写入墨迹的幻灯片索引（从 1 开始）。</param>
        /// <summary>
        /// 确定当前是否允许向指定幻灯片写入墨迹。
        /// </summary>
        /// <param name="currentSlideIndex">当前幻灯片的索引。</param>
        /// <returns>`true` 如果锁已过期、目标为已锁定的幻灯片，或处于短暂的容差窗口内，`false` 否则。</returns>
        public bool CanWriteInk(int currentSlideIndex)
        {
            if (DateTime.Now >= _inkLockUntil) return true;
            if (currentSlideIndex == _lockedSlideIndex) return true;
            if (DateTime.Now - (_inkLockUntil.AddMilliseconds(-InkLockMilliseconds)) < TimeSpan.FromMilliseconds(50)) return true;
            return false;
        }

        /// <summary>
        /// 重置与墨迹书写和幻灯片切换相关的锁与跟踪状态为初始（未锁定）值。
        /// </summary>
        /// <remarks>
        /// 将内部的墨迹写入到期时间、当前被锁定的幻灯片索引、上次切换时间和上次切换的幻灯片索引均恢复为默认未设置状态。
        /// <summary>
        /// 将墨迹写入锁和快速切换的跟踪状态重置为初始未设置值。
        /// </summary>
        /// <remarks>
        /// 在内部同步锁保护下，将锁过期时间设为最小值、被锁定幻灯片索引设为 -1，以及最近切换时间和索引恢复为未设置状态，确保后续写入和切换判定使用默认解锁行为。
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
        /// 释放并清除类内用于存储各页墨迹的所有内存流，清空当前画笔集合，并重置内部内存流数组容量为 _maxSlides + 2。
        /// </summary>
        /// <remarks>
        /// - 会逐个释放已存在的 MemoryStream（忽略释放过程中的异常），并将对应槽位设为 null。
        /// - 会清空 CurrentStrokes 集合。 
        /// - 会记录一条跟踪日志，指示已完成清除操作。
        /// <summary>
        /// 释放并清除管理的所有幻灯片墨迹内存与相关状态。
        /// </summary>
        /// <remarks>
        /// 释放并置空每个已分配的内存流，重建内部内存流数组（大小为 _maxSlides + 2），清空 CurrentStrokes 并记录操作日志。
        /// 个别内存流释放失败时仅记录警告并继续处理，不会抛出异常。
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
        /// 用指定的笔迹集合替换内部存储中对应幻灯片索引的内存流：释放（并忽略释放错误）旧流，将 <paramref name="strokes"/> 序列化到新的 <see cref="MemoryStream"/> 并保存回内部数组。
        /// </summary>
        /// <param name="slideIndex">要替换的幻灯片索引（内部内存流数组的索引）。</param>
        /// <summary>
        /// 将指定幻灯片的笔迹集合序列化为新的内存流并替换该索引处的现有内存流；在替换前尝试释放旧的内存流。
        /// </summary>
        /// <param name="slideIndex">目标幻灯片的索引，用于在内部内存流数组中定位要替换的槽位。</param>
        /// <param name="strokes">要序列化并保存到内存流的笔迹集合。</param>
        /// <remarks>新创建的内存流位置将被重置为 0，以便后续读取从流起始位置开始。</remarks>
        private void ReplaceSlideStream(int slideIndex, StrokeCollection strokes)
        {
            try { _memoryStreams[slideIndex]?.Dispose(); } catch (Exception ex) { LogHelper.WriteLogToFile($"释放旧内存流失败: {ex}", LogHelper.LogType.Warning); }
            var ms = new MemoryStream();
            strokes.Save(ms);
            ms.Position = 0;
            _memoryStreams[slideIndex] = ms;
        }

        /// <summary>
        /// 从指定文件夹删除对应幻灯片的笔迹文件（按四位索引命名）；如果文件不存在或删除失败则静默忽略错误。
        /// </summary>
        /// <param name="folderPath">存放笔迹文件的文件夹路径。</param>
        /// <summary>
        /// 从指定文件夹删除对应幻灯片的墨迹文件（按四位编号格式化为 0000.icstk）；若删除失败或文件不存在则静默忽略错误。
        /// </summary>
        /// <param name="folderPath">目标文件夹的路径（演示文稿的自动保存目录）。</param>
        /// <param name="slideIndex">用于生成文件名的幻灯片索引，格式化为四位数字（例如 1 -> "0001"）。</param>
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
        /// 检查当前墨迹内存使用状况并在超过阈值时触发清理操作。
        /// </summary>
        /// <remarks>
        /// 会更新内部的内存使用统计并刷新上次清理时间；当总占用超过 MaxMemoryUsageBytes 时，会记录警告并调用 CleanupInactiveSlideStrokes 清理不活跃幻灯页的墨迹流。若检查或清理过程中发生异常，会记录错误日志。
        /// <summary>
        /// 检查是否达到内存清理间隔，统计当前内存中笔迹流占用并在超出阈值时触发清理操作。
        /// </summary>
        /// <remarks>
        /// 该方法会计算所有已分配内存流的总字节数并更新内部的内存使用记账；当总占用超过 <c>MaxMemoryUsageBytes</c> 时调用 <c>CleanupInactiveSlideStrokes</c> 执行回收，并更新上次清理时间。方法内部的异常会被捕获并记录日志。
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
        /// 清理不活跃幻灯片的内存化墨迹数据以回收内存空间。
        /// </summary>
        /// <remarks>
        /// 将释放除当前锁定幻灯片与最近切换幻灯片之外的每页内存流（若存在），并将对应数组项设为 null；完成后若有释放，会记录已清理页数与释放的总大小（KB）。
        /// <summary>
        /// 释放不活跃幻灯片的内存笔迹数据。
        /// </summary>
        /// <remarks>
        /// 遍历内部内存流数组，释放并清除不等于当前锁定页与最近切换页的幻灯片对应的 MemoryStream；
        /// 对每个被释放的流累积释放的字节数并统计已清理页数；若有清理则以 Trace 级别记录已释放的 KB 数量。
        /// 在释放过程中若发生异常则被静默忽略，不会抛出或中断其它项的清理流程。
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
        /// 生成基于演示文稿名称、幻灯片数量和路径哈希的标识符字符串。
        /// </summary>
        /// <summary>
        /// 为指定演示文稿生成一个基于名称、幻灯片数量与路径哈希的标识符。
        /// </summary>
        /// <param name="presentation">目标演示文稿对象，用于读取名称、幻灯片数量与完整路径。</param>
        /// <returns>形如 `名称_幻灯片数_路径哈希` 的标识符；若生成失败则返回回退格式 `unknown_{ticks}` 表示不可用的标识符。</returns>
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
        /// 基于提供的文件路径生成一个短的哈希标识（用于标识或区分同一路径的资源）。
        /// </summary>
        /// <param name="filePath">用于生成哈希的文件路径字符串；如果为空或 null 则视为无效。</param>
        /// <summary>
        /// 为给定的文件路径生成一个简短的 8 字符十六进制哈希标识符。
        /// </summary>
        /// <param name="filePath">用于计算哈希的文件路径字符串；若为空或 null 将被视为无效输入。</param>
        /// <returns>8 字符十六进制哈希字符串；如果输入无效返回 "unknown"；若计算过程中发生错误返回 "error"。</returns>
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
        /// 获取由 AutoSaveLocation、"Auto Saved - Presentations" 子目录和当前演示文稿标识组成的完整文件夹路径。
        /// </summary>
        /// <summary>
        /// 获取当前演示文稿用于存放自动保存数据的完整文件夹路径。
        /// </summary>
        /// <returns>基于当前 AutoSaveLocation 与演示文稿标识生成的自动保存文件夹的完整路径。</returns>
        private string GetPresentationFolderPath()
        {
            return Path.Combine(AutoSaveLocation, "Auto Saved - Presentations", _currentPresentationId);
        }

        #endregion

        #region Dispose

        /// <summary>
        /// 释放 PPTInkManager 持有的资源并清除所有内存中的笔迹数据。
        /// </summary>
        /// <remarks>
        /// 调用后该实例将进入已释放状态，不应再被使用。方法为幂等且线程安全：如果已释放则立即返回，否则在同步区内清理资源并标记为已释放。
        /// <summary>
        /// 释放管理器占用的内存笔迹资源并将实例标记为已释放。
        /// </summary>
        /// <remarks>
        /// 该方法线程安全且可重入；第一次调用会清除并释放所有内存中的笔迹数据，随后调用不再产生效果。
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;
            lock (_lockObject) { ClearAllStrokesInternal(); }
            _disposed = true;
        }

        #endregion
    }
}