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
        public PPTInkManager()
        {
            InitializeMemoryStreams(DefaultMaxSlides + 2);
        }

        private void InitializeMemoryStreams(int capacity)
        {
            if (_memoryStreams != null)
            {
                for (int i = 0; i < _memoryStreams.Length; i++)
                {
                    try { _memoryStreams[i]?.Dispose(); } catch (Exception ex) { LogHelper.WriteLogToFile($"InitializeMemoryStreams 释放内存流 {i} 失败: {ex}", LogHelper.LogType.Warning); }
                }
            }
            _memoryStreams = new MemoryStream[Math.Max(2, capacity)];
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// 初始化新的演示文稿
        /// </summary>
        public void InitializePresentation(Presentation presentation)
        {
            ThrowIfDisposed();
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
        public void SaveCurrentSlideStrokes(int slideIndex, StrokeCollection strokes)
        {
            ThrowIfDisposed();
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
        public void ForceSaveSlideStrokes(int slideIndex, StrokeCollection strokes)
        {
            ThrowIfDisposed();
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
        public StrokeCollection LoadSlideStrokes(int slideIndex)
        {
            ThrowIfDisposed();
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
        public StrokeCollection SwitchToSlide(int slideIndex, StrokeCollection currentStrokes = null)
        {
            ThrowIfDisposed();
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
        /// <param name="currentSlideIndex">当前播放的页码，如果提供则使用此值保存位置，否则使用_lockedSlideIndex</param>
        public void SaveAllStrokesToFile(Presentation presentation, int currentSlideIndex = -1)
        {
            ThrowIfDisposed();
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
        public void LoadSavedStrokes()
        {
            ThrowIfDisposed();
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
        public void ClearAllStrokes()
        {
            ThrowIfDisposed();
            lock (_lockObject)
            {
                ClearAllStrokesInternal();
            }
        }

        public void LockInkForSlide(int slideIndex)
        {
            ThrowIfDisposed();
            _inkLockUntil = DateTime.Now.AddMilliseconds(InkLockMilliseconds);
            _lockedSlideIndex = slideIndex;
        }

        public bool CanWriteInk(int currentSlideIndex)
        {
            ThrowIfDisposed();
            if (DateTime.Now >= _inkLockUntil) return true;
            if (currentSlideIndex == _lockedSlideIndex) return true;
            if (DateTime.Now - (_inkLockUntil.AddMilliseconds(-InkLockMilliseconds)) < TimeSpan.FromMilliseconds(50)) return true;
            return false;
        }

        public void ResetLockState()
        {
            ThrowIfDisposed();
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

        private void ReplaceSlideStream(int slideIndex, StrokeCollection strokes)
        {
            try { _memoryStreams[slideIndex]?.Dispose(); } catch (Exception ex) { LogHelper.WriteLogToFile($"释放旧内存流失败: {ex}", LogHelper.LogType.Warning); }
            var ms = new MemoryStream();
            strokes.Save(ms);
            ms.Position = 0;
            _memoryStreams[slideIndex] = ms;
        }

        private void TryDeleteStrokeFile(string folderPath, int slideIndex)
        {
            try
            {
                string path = Path.Combine(folderPath, slideIndex.ToString("0000") + StrokeFileExtension);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

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
                    try { _memoryStreams[i].Dispose(); freed += len; cleaned++; } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                    finally { _memoryStreams[i] = null; }
                }
            }
            if (cleaned > 0)
                LogHelper.WriteLogToFile($"已清理 {cleaned} 页墨迹，释放 {freed / 1024}KB", LogHelper.LogType.Trace);
        }

        private string GeneratePresentationId(Presentation presentation)
        {
            try
            {
                string path = presentation.FullName;
                string hash = HashHelper.GetFileHash(path);
                return $"{presentation.Name}_{presentation.Slides.Count}_{hash}";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"生成演示文稿 ID 失败: {ex}", LogHelper.LogType.Error);
                return $"unknown_{DateTime.Now.Ticks}";
            }
        }

        private string GetPresentationFolderPath()
        {
            return Path.Combine(AutoSaveLocation, "Auto Saved - Presentations", _currentPresentationId);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_disposed) return;
            lock (_lockObject) { ClearAllStrokesInternal(); }
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PPTInkManager));
        }

        #endregion
    }
}
