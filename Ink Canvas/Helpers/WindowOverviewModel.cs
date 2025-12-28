using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 矩形结构体（用于窗口位置和大小）
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    /// <summary>
    /// 窗口信息结构
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
        public string ProcessName { get; set; }
        public string ProcessPath { get; set; }
        public WindowRect Rect { get; set; }
        public bool IsVisible { get; set; }
        public bool IsMinimized { get; set; }
        public bool IsMaximized { get; set; }
        public int ZOrder { get; set; }
        public uint ProcessId { get; set; }
        public bool IsFullScreen { get; set; }

        /// <summary>
        /// 计算窗口是否覆盖指定区域
        /// </summary>
        public bool CoversArea(WindowRect area)
        {
            if (!IsVisible || IsMinimized) return false;

            // 计算交集
            int left = Math.Max(Rect.Left, area.Left);
            int top = Math.Max(Rect.Top, area.Top);
            int right = Math.Min(Rect.Right, area.Right);
            int bottom = Math.Min(Rect.Bottom, area.Bottom);

            // 如果有交集，说明窗口覆盖了该区域
            return left < right && top < bottom;
        }

        /// <summary>
        /// 计算窗口覆盖指定区域的比例
        /// </summary>
        public double GetCoverageRatio(WindowRect area)
        {
            if (!IsVisible || IsMinimized) return 0.0;

            // 计算交集
            int left = Math.Max(Rect.Left, area.Left);
            int top = Math.Max(Rect.Top, area.Top);
            int right = Math.Min(Rect.Right, area.Right);
            int bottom = Math.Min(Rect.Bottom, area.Bottom);

            if (left >= right || top >= bottom) return 0.0;

            // 计算交集面积
            double intersectionArea = (right - left) * (bottom - top);
            // 计算目标区域面积
            double targetArea = area.Width * area.Height;

            if (targetArea == 0) return 0.0;

            return intersectionArea / targetArea;
        }
    }

    /// <summary>
    /// 窗口概览模型 - 实时监控桌面所有可见窗口并计算遮挡情况
    /// </summary>
    public class WindowOverviewModel : IDisposable
    {
        #region Win32 API Declarations
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out WindowRect lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        private const uint GW_HWNDNEXT = 2;
        private const uint GW_HWNDPREV = 3;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        #endregion

        private readonly object _lockObject = new object();
        private List<WindowInfo> _windows = new List<WindowInfo>();
        private Timer _updateTimer;
        private bool _isDisposed = false;
        private readonly int _updateInterval = 200; // 更新间隔（毫秒）

        /// <summary>
        /// 窗口列表更新事件
        /// </summary>
        public event EventHandler<List<WindowInfo>> WindowsUpdated;

        /// <summary>
        /// 当前窗口列表（按Z顺序排序，最上层在前）
        /// </summary>
        public List<WindowInfo> Windows
        {
            get
            {
                lock (_lockObject)
                {
                    return new List<WindowInfo>(_windows);
                }
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public WindowOverviewModel()
        {
            // 立即执行一次更新
            UpdateWindows();

            // 启动定时器，定期更新窗口列表
            _updateTimer = new Timer(OnUpdateTimer, null, _updateInterval, _updateInterval);
        }

        /// <summary>
        /// 定时器回调
        /// </summary>
        private void OnUpdateTimer(object state)
        {
            if (_isDisposed) return;

            try
            {
                UpdateWindows();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"窗口概览模型更新失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 更新窗口列表
        /// </summary>
        public void UpdateWindows()
        {
            var windows = new List<WindowInfo>();
            var zOrder = 0;

            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    // 检查窗口是否可见
                    if (!IsWindowVisible(hWnd)) return true;

                    // 检查窗口是否最小化
                    bool isMinimized = IsIconic(hWnd);
                    if (isMinimized) return true;

                    // 获取窗口矩形
                    if (!GetWindowRect(hWnd, out WindowRect rect)) return true;

                    // 过滤掉无效的窗口（太小或位置异常的窗口）
                    if (rect.Width <= 0 || rect.Height <= 0) return true;
                    if (rect.Right < rect.Left || rect.Bottom < rect.Top) return true;

                    // 获取窗口标题
                    const int nChars = 256;
                    StringBuilder windowTitle = new StringBuilder(nChars);
                    GetWindowText(hWnd, windowTitle, nChars);
                    string title = windowTitle.ToString();

                    // 获取窗口类名
                    StringBuilder className = new StringBuilder(nChars);
                    GetClassName(hWnd, className, nChars);
                    string classNameStr = className.ToString();

                    // 获取进程信息
                    GetWindowThreadProcessId(hWnd, out uint processId);
                    string processName = "Unknown";
                    string processPath = "Unknown";

                    try
                    {
                        Process process = Process.GetProcessById((int)processId);
                        processName = process.ProcessName;
                        try
                        {
                            processPath = process.MainModule?.FileName ?? "Unknown";
                        }
                        catch
                        {
                            processPath = "Unknown";
                        }
                    }
                    catch
                    {
                        // 进程可能已退出
                    }

                    // 检查是否最大化
                    bool isMaximized = IsZoomed(hWnd);

                    // 检查是否全屏（窗口大小接近屏幕大小）
                    bool isFullScreen = false;
                    try
                    {
                        var screen = System.Windows.Forms.Screen.FromHandle(hWnd);
                        var screenBounds = screen.Bounds;
                        // 如果窗口大小接近屏幕大小（允许10像素误差），认为是全屏
                        isFullScreen = rect.Width >= screenBounds.Width - 10 &&
                                      rect.Height >= screenBounds.Height - 10 &&
                                      Math.Abs(rect.Left - screenBounds.Left) <= 10 &&
                                      Math.Abs(rect.Top - screenBounds.Top) <= 10;
                    }
                    catch
                    {
                        // 无法获取屏幕信息，使用默认值
                    }

                    // 跳过当前应用程序的窗口（避免检测到自己）
                    if (processName == "InkCanvasForClass" || processName == "Ink Canvas")
                    {
                        return true;
                    }

                    var windowInfo = new WindowInfo
                    {
                        Handle = hWnd,
                        Title = title,
                        ClassName = classNameStr,
                        ProcessName = processName,
                        ProcessPath = processPath,
                        Rect = rect,
                        IsVisible = true,
                        IsMinimized = false,
                        IsMaximized = isMaximized,
                        ZOrder = zOrder++,
                        ProcessId = processId,
                        IsFullScreen = isFullScreen
                    };

                    windows.Add(windowInfo);
                }
                catch
                {
                    // 忽略单个窗口的错误，继续枚举其他窗口
                }

                return true; // 继续枚举
            }, IntPtr.Zero);

            // 按Z顺序排序（从最上层到最下层）
            // 注意：EnumWindows返回的顺序可能不是严格的Z顺序，但我们可以通过GetWindow来获取更准确的顺序
            windows = windows.OrderByDescending(w => w.ZOrder).ToList();

            lock (_lockObject)
            {
                _windows = windows;
            }

            // 触发更新事件
            WindowsUpdated?.Invoke(this, windows);
        }

        /// <summary>
        /// 检查指定区域是否被其他窗口覆盖
        /// </summary>
        /// <param name="area">要检查的区域</param>
        /// <param name="excludeProcessNames">要排除的进程名列表（例如当前应用程序）</param>
        /// <param name="coverageThreshold">覆盖阈值（0.0-1.0），超过此阈值认为被覆盖</param>
        /// <returns>如果被覆盖返回true</returns>
        public bool IsAreaCovered(WindowRect area, List<string> excludeProcessNames = null, double coverageThreshold = 0.5)
        {
            lock (_lockObject)
            {
                excludeProcessNames = excludeProcessNames ?? new List<string>();

                // 从最上层窗口开始检查
                foreach (var window in _windows)
                {
                    // 跳过排除的进程
                    if (excludeProcessNames.Contains(window.ProcessName)) continue;

                    // 计算覆盖比例
                    double coverage = window.GetCoverageRatio(area);
                    if (coverage >= coverageThreshold)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 检查指定区域是否被全屏窗口覆盖
        /// </summary>
        /// <param name="area">要检查的区域</param>
        /// <param name="excludeProcessNames">要排除的进程名列表</param>
        /// <returns>如果被全屏窗口覆盖返回true</returns>
        public bool IsAreaCoveredByFullScreenWindow(WindowRect area, List<string> excludeProcessNames = null)
        {
            lock (_lockObject)
            {
                excludeProcessNames = excludeProcessNames ?? new List<string>();

                // 查找全屏窗口
                foreach (var window in _windows)
                {
                    // 跳过排除的进程
                    if (excludeProcessNames.Contains(window.ProcessName)) continue;

                    // 只检查全屏窗口
                    if (window.IsFullScreen && window.CoversArea(area))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 获取覆盖指定区域的所有窗口
        /// </summary>
        /// <param name="area">要检查的区域</param>
        /// <param name="excludeProcessNames">要排除的进程名列表</param>
        /// <param name="coverageThreshold">覆盖阈值</param>
        /// <returns>覆盖该区域的窗口列表（按Z顺序，最上层在前）</returns>
        public List<WindowInfo> GetCoveringWindows(WindowRect area, List<string> excludeProcessNames = null, double coverageThreshold = 0.1)
        {
            lock (_lockObject)
            {
                excludeProcessNames = excludeProcessNames ?? new List<string>();
                var coveringWindows = new List<WindowInfo>();

                foreach (var window in _windows)
                {
                    // 跳过排除的进程
                    if (excludeProcessNames.Contains(window.ProcessName)) continue;

                    // 计算覆盖比例
                    double coverage = window.GetCoverageRatio(area);
                    if (coverage >= coverageThreshold)
                    {
                        coveringWindows.Add(window);
                    }
                }

                return coveringWindows;
            }
        }

        /// <summary>
        /// 检查是否有全屏窗口
        /// </summary>
        /// <param name="excludeProcessNames">要排除的进程名列表</param>
        /// <returns>如果有全屏窗口返回true</returns>
        public bool HasFullScreenWindow(List<string> excludeProcessNames = null)
        {
            lock (_lockObject)
            {
                excludeProcessNames = excludeProcessNames ?? new List<string>();

                return _windows.Any(w => !excludeProcessNames.Contains(w.ProcessName) && w.IsFullScreen);
            }
        }

        /// <summary>
        /// 获取所有全屏窗口
        /// </summary>
        /// <param name="excludeProcessNames">要排除的进程名列表</param>
        /// <returns>全屏窗口列表</returns>
        public List<WindowInfo> GetFullScreenWindows(List<string> excludeProcessNames = null)
        {
            lock (_lockObject)
            {
                excludeProcessNames = excludeProcessNames ?? new List<string>();

                return _windows.Where(w => !excludeProcessNames.Contains(w.ProcessName) && w.IsFullScreen).ToList();
            }
        }

        /// <summary>
        /// 根据进程名查找窗口
        /// </summary>
        /// <param name="processName">进程名</param>
        /// <returns>匹配的窗口列表</returns>
        public List<WindowInfo> FindWindowsByProcessName(string processName)
        {
            lock (_lockObject)
            {
                return _windows.Where(w => w.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }

        /// <summary>
        /// 根据窗口标题查找窗口
        /// </summary>
        /// <param name="title">窗口标题（支持部分匹配）</param>
        /// <returns>匹配的窗口列表</returns>
        public List<WindowInfo> FindWindowsByTitle(string title)
        {
            lock (_lockObject)
            {
                return _windows.Where(w => w.Title.IndexOf(title, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _updateTimer?.Dispose();
            _updateTimer = null;

            lock (_lockObject)
            {
                _windows.Clear();
            }
        }
    }
}

