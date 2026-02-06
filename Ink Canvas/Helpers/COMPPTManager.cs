using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 基于 COM 的 PPT 联动管理器
    /// </summary>
    public class ComPPTManager : IDisposable
    {
        #region Events
        public event Action<SlideShowWindow> SlideShowBegin;
        public event Action<SlideShowWindow> SlideShowNextSlide;
        public event Action<Presentation> SlideShowEnd;
        public event Action<Presentation> PresentationOpen;
        public event Action<Presentation> PresentationClose;
        public event Action<bool> PPTConnectionChanged;
        public event Action<bool> SlideShowStateChanged;
        #endregion

        #region Properties
        public Microsoft.Office.Interop.PowerPoint.Application PPTApplication { get; private set; }
        public Presentation CurrentPresentation { get; private set; }
        public Slides CurrentSlides { get; private set; }
        public Slide CurrentSlide { get; private set; }
        public int SlidesCount { get; private set; }

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (PPTApplication == null) return false;
                    if (!Marshal.IsComObject(PPTApplication)) return false;

                    // 尝试访问一个简单的属性来验证连接是否有效
                    var _ = PPTApplication.Name;
                    return true;
                }
                catch (COMException comEx)
                {
                    var hr = (uint)comEx.HResult;
                    // 如果COM对象已失效，返回false
                    if (hr == 0x8001010E || hr == 0x80004005 || hr == 0x800706B5)
                    {
                        return false;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool IsInSlideShow
        {
            get
            {
                object slideShowWindows = null;
                object slideShowWindow = null;
                object view = null;
                try
                {
                    if (PPTApplication == null || !Marshal.IsComObject(PPTApplication)) return false;

                    slideShowWindows = PPTApplication.SlideShowWindows;
                    if (slideShowWindows == null) return false;

                    dynamic ssw = slideShowWindows;
                    if (ssw.Count == 0) return false;

                    try
                    {
                        slideShowWindow = ssw[1];
                        if (slideShowWindow == null) return false;

                        dynamic sswObj = slideShowWindow;
                        view = sswObj.View;
                        if (view == null) return false;

                        return true;
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x8001010E || hr == 0x80004005)
                        {
                            DisconnectFromPPT();
                        }
                        return false;
                    }
                }
                catch (COMException comEx)
                {
                    var hr = (uint)comEx.HResult;
                    if (hr == 0x8001010E || hr == 0x80004005)
                    {
                        DisconnectFromPPT();
                    }
                    LogHelper.WriteLogToFile($"检查PPT放映状态失败: {comEx.Message} (HR: 0x{hr:X8})", LogHelper.LogType.Warning);
                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"检查PPT放映状态时发生意外错误: {ex}", LogHelper.LogType.Warning);
                    return false;
                }
                finally
                {
                    SafeReleaseComObject(view);
                    SafeReleaseComObject(slideShowWindow);
                    SafeReleaseComObject(slideShowWindows);
                }
            }
        }

        /// <summary>
        /// 是否尝试支持 WPS（通过 COM 接口 kwpp.Application）
        /// </summary>
        public bool IsSupportWPS { get; set; } = false;
        #endregion

        #region Private Fields
        private Timer _connectionCheckTimer;
        private Timer _slideShowStateCheckTimer;
        private bool _isModuleUnloading = false;
        private bool _lastSlideShowState;
        private readonly object _lockObject = new object();
        private bool _disposed;
        #endregion

        #region Constructor & Initialization
        public ComPPTManager()
        {
            InitializeConnectionTimer();
        }

        private void InitializeConnectionTimer()
        {
            _connectionCheckTimer = new Timer(500);
            _connectionCheckTimer.Elapsed += OnConnectionCheckTimerElapsed;
            _connectionCheckTimer.AutoReset = true;

            _slideShowStateCheckTimer = new Timer(1000);
            _slideShowStateCheckTimer.Elapsed += OnSlideShowStateCheckTimerElapsed;
            _slideShowStateCheckTimer.AutoReset = true;
        }

        public void StartMonitoring()
        {
            if (!_disposed)
            {
                _connectionCheckTimer?.Start();
                _slideShowStateCheckTimer?.Start();
                LogHelper.WriteLogToFile("ComPPTManager: PPT监控已启动", LogHelper.LogType.Trace);
            }
        }

        public void StopMonitoring()
        {
            _connectionCheckTimer?.Stop();
            _slideShowStateCheckTimer?.Stop();
            DisconnectFromPPT();
            LogHelper.WriteLogToFile("ComPPTManager: PPT监控已停止", LogHelper.LogType.Trace);
        }
        #endregion

        #region Connection Management
        private void OnConnectionCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_isModuleUnloading)
                {
                    CheckAndConnectToPPT();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: PPT连接检查失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowStateCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_isModuleUnloading && IsConnected)
                {
                    CheckSlideShowState();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: PPT放映状态检查失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void CheckAndConnectToPPT()
        {
            if (_isModuleUnloading) return;

            lock (_lockObject)
            {
                try
                {
                    if (_isModuleUnloading) return;

                    // 尝试连接到PowerPoint
                    var pptApp = TryConnectToPowerPoint();
                    if (pptApp == null && IsSupportWPS)
                    {
                        // 如果PowerPoint连接失败且支持WPS，尝试连接WPS
                        pptApp = TryConnectToWPS();
                    }

                    if (pptApp != null && !IsConnected)
                    {
                        // 有可用的PPT/WPS应用程序且当前未连接，建立连接
                        ConnectToPPT(pptApp);
                    }
                    else if (pptApp == null && IsConnected)
                    {
                        // 没有可用的PPT/WPS应用程序但当前显示已连接，断开连接
                        DisconnectFromPPT();
                    }
                    else if (pptApp == null && PPTApplication != null)
                    {
                        // PPT/WPS应用程序不可用但PPTApplication对象仍存在，清理无效连接
                        DisconnectFromPPT();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"ComPPTManager: PPT连接检查异常: {ex}", LogHelper.LogType.Error);
                    if (PPTApplication != null)
                    {
                        DisconnectFromPPT();
                    }
                }
            }
        }

        private void CheckSlideShowState()
        {
            try
            {
                if (!IsConnected) return;

                var currentSlideShowState = IsInSlideShow;
                if (currentSlideShowState != _lastSlideShowState)
                {
                    _lastSlideShowState = currentSlideShowState;
                    SlideShowStateChanged?.Invoke(currentSlideShowState);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 检查PPT放映状态异常: {ex}", LogHelper.LogType.Error);
            }
        }

        private Microsoft.Office.Interop.PowerPoint.Application TryConnectToPowerPoint()
        {
            try
            {
                var pptApp = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("PowerPoint.Application");

                if (pptApp != null && Marshal.IsComObject(pptApp))
                {
                    var _ = pptApp.Name;
                    return pptApp;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private Microsoft.Office.Interop.PowerPoint.Application TryConnectToWPS()
        {
            try
            {
                var wpsApp = (Microsoft.Office.Interop.PowerPoint.Application)Marshal.GetActiveObject("kwpp.Application");

                if (wpsApp != null && Marshal.IsComObject(wpsApp))
                {
                    var _ = wpsApp.Name;
                    return wpsApp;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void ConnectToPPT(Microsoft.Office.Interop.PowerPoint.Application pptApp)
        {
            try
            {
                PPTApplication = pptApp;

                // 在主线程中注册事件，确保COM对象在正确的线程中
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        PPTApplication.PresentationOpen += OnPresentationOpen;
                        PPTApplication.PresentationClose += OnPresentationClose;
                        PPTApplication.SlideShowBegin += OnSlideShowBegin;
                        PPTApplication.SlideShowNextSlide += OnSlideShowNextSlide;
                        PPTApplication.SlideShowEnd += OnSlideShowEnd;

                        LogHelper.WriteLogToFile("ComPPTManager: PPT事件注册成功", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ComPPTManager: PPT事件注册失败: {ex}", LogHelper.LogType.Error);
                        throw; // 重新抛出异常，让外层处理
                    }
                }, DispatcherPriority.Normal, default, TimeSpan.FromSeconds(2));

                // 获取当前演示文稿信息
                UpdateCurrentPresentationInfo();

                // 停止连接检查定时器，避免重复连接
                _connectionCheckTimer?.Stop();

                // 触发连接成功事件
                PPTConnectionChanged?.Invoke(true);

                LogHelper.WriteLogToFile("ComPPTManager: 成功连接到PPT应用程序", LogHelper.LogType.Event);

                if (IsInSlideShow && PPTApplication.SlideShowWindows.Count > 0)
                {
                    OnSlideShowBegin(PPTApplication.SlideShowWindows[1]);
                }
                else if (CurrentPresentation != null)
                {
                    OnPresentationOpen(CurrentPresentation);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 连接PPT应用程序失败: {ex}", LogHelper.LogType.Error);
                PPTApplication = null;
            }
        }

        private void DisconnectFromPPT()
        {
            try
            {
                if (PPTApplication != null)
                {
                    // 取消事件注册
                    try
                    {
                        if (Marshal.IsComObject(PPTApplication))
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                try
                                {
                                    PPTApplication.PresentationOpen -= OnPresentationOpen;
                                    PPTApplication.PresentationClose -= OnPresentationClose;
                                    PPTApplication.SlideShowBegin -= OnSlideShowBegin;
                                    PPTApplication.SlideShowNextSlide -= OnSlideShowNextSlide;
                                    PPTApplication.SlideShowEnd -= OnSlideShowEnd;
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile($"ComPPTManager: 取消PPT事件注册时发生异常: {ex}", LogHelper.LogType.Warning);
                                }
                            }, DispatcherPriority.Normal, default, TimeSpan.FromSeconds(1));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ComPPTManager: 取消PPT事件注册失败: {ex}", LogHelper.LogType.Warning);
                    }

                    SafeReleaseComObject(CurrentSlide);
                    SafeReleaseComObject(CurrentSlides);
                    SafeReleaseComObject(CurrentPresentation);

                    if (PPTApplication != null && Marshal.IsComObject(PPTApplication))
                    {
                        try
                        {
                            Marshal.FinalReleaseComObject(PPTApplication);
                        }
                        catch
                        {
                            try
                            {
                                int refCount = Marshal.ReleaseComObject(PPTApplication);
                                while (refCount > 0)
                                {
                                    refCount = Marshal.ReleaseComObject(PPTApplication);
                                }
                            }
                            catch { }
                        }
                    }
                }

                PPTApplication = null;
                CurrentPresentation = null;
                CurrentSlides = null;
                CurrentSlide = null;
                SlidesCount = 0;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _isModuleUnloading = true;
                _connectionCheckTimer?.Stop();
                _slideShowStateCheckTimer?.Stop();

                // 触发连接断开事件
                PPTConnectionChanged?.Invoke(false);

                LogHelper.WriteLogToFile("ComPPTManager: 已断开PPT连接", LogHelper.LogType.Event);

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        Thread.Sleep(2000);

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        Thread.Sleep(1000);

                        _isModuleUnloading = false;
                        _connectionCheckTimer?.Start();
                        _slideShowStateCheckTimer?.Start();

                        LogHelper.WriteLogToFile("ComPPTManager: PPT联动模块已重新加载", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ComPPTManager: 重新加载PPT联动模块失败: {ex}", LogHelper.LogType.Error);
                        _isModuleUnloading = false;
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 断开PPT连接失败: {ex}", LogHelper.LogType.Error);
                _isModuleUnloading = false;
            }
        }

        private void SafeReleaseComObject(object comObject)
        {
            try
            {
                if (comObject != null && Marshal.IsComObject(comObject))
                {
                    Marshal.ReleaseComObject(comObject);
                }
            }
            catch { }
        }

        private void UpdateCurrentPresentationInfo()
        {
            object activePresentation = null;
            object slideShowWindows = null;
            object slideShowWindow = null;
            object activeWindow = null;
            object view = null;
            object selection = null;
            object slideRange = null;

            try
            {
                if (PPTApplication != null && Marshal.IsComObject(PPTApplication))
                {
                    try
                    {
                        activePresentation = PPTApplication.ActivePresentation;
                        if (activePresentation != null)
                        {
                            SafeReleaseComObject(CurrentPresentation);
                            CurrentPresentation = activePresentation as Presentation;
                            CurrentSlides = CurrentPresentation.Slides;

                            try
                            {
                                var slideCount = CurrentSlides.Count;
                                SlidesCount = slideCount > 0 ? slideCount : 0;
                            }
                            catch
                            {
                                SlidesCount = 0;
                            }

                            try
                            {
                                slideShowWindows = PPTApplication.SlideShowWindows;
                                if (IsInSlideShow && slideShowWindows != null)
                                {
                                    dynamic ssw = slideShowWindows;
                                    if (ssw.Count > 0)
                                    {
                                        slideShowWindow = ssw[1];
                                        if (slideShowWindow != null)
                                        {
                                            dynamic sswObj = slideShowWindow;
                                            view = sswObj.View;
                                            if (view != null)
                                            {
                                                dynamic viewObj = view;
                                                CurrentSlide = viewObj.Slide as Slide;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    activeWindow = PPTApplication.ActiveWindow;
                                    if (activeWindow != null)
                                    {
                                        dynamic aw = activeWindow;
                                        selection = aw.Selection;
                                        if (selection != null)
                                        {
                                            dynamic sel = selection;
                                            slideRange = sel.SlideRange;
                                            if (slideRange != null)
                                            {
                                                dynamic sr = slideRange;
                                                int slideNumber = sr.SlideNumber;
                                                if (slideNumber > 0 && slideNumber <= SlidesCount)
                                                {
                                                    CurrentSlide = CurrentSlides[slideNumber];
                                                }
                                            }
                                        }
                                    }

                                    if (CurrentSlide == null && SlidesCount > 0)
                                    {
                                        CurrentSlide = CurrentSlides[1];
                                    }
                                }
                            }
                            catch
                            {
                                if (SlidesCount > 0)
                                {
                                    CurrentSlide = CurrentSlides[1];
                                }
                            }
                        }
                        else
                        {
                            CurrentPresentation = null;
                            CurrentSlides = null;
                            CurrentSlide = null;
                            SlidesCount = 0;
                        }
                    }
                    catch
                    {
                        CurrentPresentation = null;
                        CurrentSlides = null;
                        CurrentSlide = null;
                        SlidesCount = 0;
                    }
                }
                else
                {
                    CurrentPresentation = null;
                    CurrentSlides = null;
                    CurrentSlide = null;
                    SlidesCount = 0;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 更新演示文稿信息失败: {ex}", LogHelper.LogType.Error);
                CurrentPresentation = null;
                CurrentSlides = null;
                CurrentSlide = null;
                SlidesCount = 0;
            }
            finally
            {
                SafeReleaseComObject(slideRange);
                SafeReleaseComObject(selection);
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(activeWindow);
                SafeReleaseComObject(slideShowWindows);
                if (activePresentation != null && !ReferenceEquals(activePresentation, CurrentPresentation))
                {
                    SafeReleaseComObject(activePresentation);
                }
            }
        }
        #endregion

        #region Event Handlers
        private void OnPresentationOpen(Presentation pres)
        {
            try
            {
                UpdateCurrentPresentationInfo();
                PresentationOpen?.Invoke(pres);
                LogHelper.WriteLogToFile($"ComPPTManager: 演示文稿已打开: {pres?.Name}", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 处理演示文稿打开事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnPresentationClose(Presentation pres)
        {
            try
            {
                PresentationClose?.Invoke(pres);
                DisconnectFromPPT();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 处理演示文稿关闭事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowBegin(SlideShowWindow wn)
        {
            try
            {
                UpdateCurrentPresentationInfo();
                SlideShowBegin?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 处理幻灯片放映开始事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowNextSlide(SlideShowWindow wn)
        {
            try
            {
                UpdateCurrentPresentationInfo();
                SlideShowNextSlide?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 处理幻灯片切换事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowEnd(Presentation pres)
        {
            try
            {
                SlideShowEnd?.Invoke(pres);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片放映结束事件失败: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion

        #region Public Navigation APIs
        public bool TryNavigateToSlide(int slideNumber)
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            object windows = null;
            object window = null;
            object windowView = null;
            try
            {
                if (!IsConnected || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                if (IsInSlideShow)
                {
                    slideShowWindows = PPTApplication.SlideShowWindows;
                    if (slideShowWindows != null)
                    {
                        dynamic ssw = slideShowWindows;
                        if (ssw.Count >= 1)
                        {
                            slideShowWindow = ssw[1];
                            if (slideShowWindow != null)
                            {
                                dynamic sswObj = slideShowWindow;
                                view = sswObj.View;
                                if (view != null)
                                {
                                    dynamic viewObj = view;
                                    viewObj.GotoSlide(slideNumber);
                                    return true;
                                }
                            }
                        }
                    }
                }
                else if (CurrentPresentation != null)
                {
                    windows = CurrentPresentation.Windows;
                    if (windows != null)
                    {
                        dynamic win = windows;
                        if (win.Count >= 1)
                        {
                            window = win[1];
                            if (window != null)
                            {
                                dynamic winObj = window;
                                windowView = winObj.View;
                                if (windowView != null)
                                {
                                    dynamic viewObj = windowView;
                                    viewObj.GotoSlide(slideNumber);
                                    return true;
                                }
                            }
                        }
                    }
                }
                return false;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 跳转到幻灯片{slideNumber}失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 跳转到幻灯片{slideNumber}失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(windowView);
                SafeReleaseComObject(window);
                SafeReleaseComObject(windows);
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public bool TryNavigateNext()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            try
            {
                if (!IsConnected || !IsInSlideShow || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                slideShowWindows = PPTApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow != null)
                    {
                        dynamic sswObj = slideShowWindow;
                        sswObj.Activate();
                        view = sswObj.View;
                        if (view != null)
                        {
                            dynamic viewObj = view;
                            viewObj.Next();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 切换到下一页失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 切换到下一页失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public bool TryNavigatePrevious()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            try
            {
                if (!IsConnected || !IsInSlideShow || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                slideShowWindows = PPTApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow != null)
                    {
                        dynamic sswObj = slideShowWindow;
                        sswObj.Activate();
                        view = sswObj.View;
                        if (view != null)
                        {
                            dynamic viewObj = view;
                            viewObj.Previous();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 切换到上一页失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 切换到上一页失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public bool TryEndSlideShow()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            try
            {
                if (!IsConnected || !IsInSlideShow || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                slideShowWindows = PPTApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow != null)
                    {
                        dynamic sswObj = slideShowWindow;
                        view = sswObj.View;
                        if (view != null)
                        {
                            dynamic viewObj = view;
                            viewObj.Exit();
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 结束幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 结束幻灯片放映失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public bool TryStartSlideShow()
        {
            try
            {
                if (!IsConnected || CurrentPresentation == null || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication) || !Marshal.IsComObject(CurrentPresentation)) return false;

                CurrentPresentation.SlideShowSettings.Run();
                return true;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 开始幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 开始幻灯片放映失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        public Presentation GetCurrentActivePresentation()
        {
            try
            {
                if (!IsConnected || PPTApplication == null) return null;
                if (!Marshal.IsComObject(PPTApplication)) return null;

                if (IsInSlideShow && PPTApplication.SlideShowWindows.Count > 0)
                {
                    try
                    {
                        var slideShowWindow = PPTApplication.SlideShowWindows[1];
                        if (slideShowWindow?.View != null)
                        {
                            return (Presentation)slideShowWindow.View.Slide.Parent;
                        }
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x80048240)
                        {
                            return null;
                        }
                        throw;
                    }
                }

                if (PPTApplication.ActiveWindow?.Presentation != null)
                {
                    return PPTApplication.ActiveWindow.Presentation;
                }

                return CurrentPresentation;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 获取当前活跃演示文稿失败: {comEx.Message}", LogHelper.LogType.Warning);
                return CurrentPresentation;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 获取当前活跃演示文稿失败: {ex}", LogHelper.LogType.Error);
                return CurrentPresentation;
            }
        }

        public int GetCurrentSlideNumber()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            object activeWindow = null;
            object selection = null;
            object slideRange = null;
            try
            {
                if (!IsConnected || PPTApplication == null) return 0;
                if (!Marshal.IsComObject(PPTApplication)) return 0;

                if (IsInSlideShow)
                {
                    slideShowWindows = PPTApplication.SlideShowWindows;
                    if (slideShowWindows != null)
                    {
                        dynamic ssw = slideShowWindows;
                        if (ssw.Count > 0)
                        {
                            slideShowWindow = ssw[1];
                            if (slideShowWindow != null)
                            {
                                dynamic sswObj = slideShowWindow;
                                view = sswObj.View;
                                if (view != null)
                                {
                                    dynamic viewObj = view;
                                    return viewObj.CurrentShowPosition;
                                }
                            }
                        }
                    }
                }

                activeWindow = PPTApplication.ActiveWindow;
                if (activeWindow != null)
                {
                    dynamic aw = activeWindow;
                    selection = aw.Selection;
                    if (selection != null)
                    {
                        dynamic sel = selection;
                        slideRange = sel.SlideRange;
                        if (slideRange != null)
                        {
                            dynamic sr = slideRange;
                            int slideNumber = sr.SlideNumber;
                            if (slideNumber > 0)
                            {
                                return slideNumber;
                            }
                        }
                    }
                }

                if (CurrentSlide != null && Marshal.IsComObject(CurrentSlide))
                {
                    return CurrentSlide.SlideNumber;
                }

                return 0;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
            finally
            {
                SafeReleaseComObject(slideRange);
                SafeReleaseComObject(selection);
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
                SafeReleaseComObject(activeWindow);
            }
        }

        public string GetPresentationName()
        {
            try
            {
                if (CurrentPresentation == null || !Marshal.IsComObject(CurrentPresentation)) return "";

                return CurrentPresentation.Name ?? "";
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 获取演示文稿名称失败: {comEx.Message}", LogHelper.LogType.Warning);
                return "";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 获取演示文稿名称失败: {ex}", LogHelper.LogType.Error);
                return "";
            }
        }

        public string GetPresentationPath()
        {
            try
            {
                if (CurrentPresentation == null || !Marshal.IsComObject(CurrentPresentation)) return "";

                return CurrentPresentation.FullName ?? "";
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 获取演示文稿路径失败: {comEx.Message}", LogHelper.LogType.Warning);
                return "";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 获取演示文稿路径失败: {ex}", LogHelper.LogType.Error);
                return "";
            }
        }

        public bool TryShowSlideNavigation()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object slideNavigation = null;
            try
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 尝试显示幻灯片导航 - 连接状态: {IsConnected}, 放映状态: {IsInSlideShow}", LogHelper.LogType.Trace);

                if (!IsConnected || !IsInSlideShow || PPTApplication == null)
                {
                    LogHelper.WriteLogToFile("ComPPTManager: PPT未连接或未在放映状态", LogHelper.LogType.Warning);
                    return false;
                }

                if (!Marshal.IsComObject(PPTApplication))
                {
                    LogHelper.WriteLogToFile("ComPPTManager: PPT应用程序COM对象无效", LogHelper.LogType.Warning);
                    return false;
                }

                slideShowWindows = PPTApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow == null)
                    {
                        LogHelper.WriteLogToFile("ComPPTManager: 幻灯片放映窗口为空", LogHelper.LogType.Warning);
                        return false;
                    }

                    try
                    {
                        dynamic sswObj = slideShowWindow;
                        slideNavigation = sswObj.SlideNavigation;
                        if (slideNavigation != null)
                        {
                            dynamic sn = slideNavigation;
                            sn.Visible = true;
                            LogHelper.WriteLogToFile("ComPPTManager: 成功显示幻灯片导航（PowerPoint模式）", LogHelper.LogType.Event);
                            return true;
                        }

                        LogHelper.WriteLogToFile("ComPPTManager: SlideNavigation对象为空，可能是WPS不支持此功能", LogHelper.LogType.Warning);
                        return false;
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x80020006)
                        {
                            LogHelper.WriteLogToFile("ComPPTManager: WPS不支持SlideNavigation功能", LogHelper.LogType.Warning);
                            return false;
                        }
                        throw;
                    }
                }
                return false;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ComPPTManager: 显示幻灯片导航COM异常: {comEx.Message} (HRESULT: 0x{hr:X8})", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ComPPTManager: 显示幻灯片导航失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(slideNavigation);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();

                _connectionCheckTimer?.Dispose();
                _slideShowStateCheckTimer?.Dispose();

                _disposed = true;
            }
        }
        #endregion
    }
}


