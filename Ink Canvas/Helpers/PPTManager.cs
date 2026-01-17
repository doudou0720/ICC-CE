using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// PPT联动管理器 - 统一管理PPT和WPS的连接、事件处理和进程管理
    /// </summary>
    public class PPTManager : IDisposable
    {
        #region Events
        public event Action<SlideShowWindow> SlideShowBegin;
        public event Action<SlideShowWindow> SlideShowNextSlide;
        public event Action<Presentation> SlideShowEnd;
        public event Action<Presentation> PresentationOpen;
        public event Action<bool> PPTConnectionChanged;
        public event Action<bool> SlideShowStateChanged;
        #endregion

        #region Properties
        public object PPTApplication { get; private set; }
        public dynamic CurrentPresentation { get; private set; }
        public dynamic CurrentSlides { get; private set; }
        public dynamic CurrentSlide { get; private set; }
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
                    dynamic app = PPTApplication;
                    var _ = app.Name;
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

                    dynamic app = PPTApplication;
                    slideShowWindows = app.SlideShowWindows;
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
        public bool IsSupportWPS { get; set; } = false;
        #endregion

        #region Private Fields
        private Timer _connectionCheckTimer;
        private Timer _slideShowStateCheckTimer;
        private Timer _wpsProcessCheckTimer;
        private Process _wpsProcess;
        private bool _isModuleUnloading = false;
        private bool _hasWpsProcessId;
        private DateTime _wpsProcessRecordTime = DateTime.MinValue;
        private int _wpsProcessCheckCount;
        private WpsWindowInfo _lastForegroundWpsWindow;
        private DateTime _lastWindowCheckTime = DateTime.MinValue;
        private bool _lastSlideShowState;
        private readonly object _lockObject = new object();
        private bool _disposed;
        
        private dynamic _pptActivePresentation;
        private dynamic _pptSlideShowWindow;
        private int _polling = 0;
        private bool _forcePolling = false;
        private bool _bindingEvents = false;
        private DateTime _updateTime;
        #endregion

        #region Constructor & Initialization
        public PPTManager()
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
                LogHelper.WriteLogToFile("PPT监控已启动", LogHelper.LogType.Trace);
            }
        }

        public void StopMonitoring()
        {
            _connectionCheckTimer?.Stop();
            _slideShowStateCheckTimer?.Stop();
            DisconnectFromPPT();
            LogHelper.WriteLogToFile("PPT监控已停止", LogHelper.LogType.Trace);
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
                LogHelper.WriteLogToFile($"PPT连接检查失败: {ex}", LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile($"PPT放映状态检查失败: {ex}", LogHelper.LogType.Error);
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
                    
                    object bestApp = PPTROTConnectionHelper.GetAnyActivePowerPoint(PPTApplication, out int bestPriority, out int targetPriority);
                    bool needRebind = false;

                    LogHelper.WriteLogToFile($"ROT扫描结果: now={targetPriority}, best={bestPriority}, bestApp={(bestApp != null ? "found" : "null")}", LogHelper.LogType.Trace);

                    if (PPTApplication == null && bestApp != null)
                    {
                        needRebind = true;
                    }
                    else if (PPTApplication != null && bestApp != null && bestPriority > targetPriority)
                    {
                        if (!PPTROTConnectionHelper.AreComObjectsEqual(PPTApplication, bestApp))
                        {
                            needRebind = true;
                        }
                    }

                    if (needRebind)
                    {
                        LogHelper.WriteLogToFile($"需要重新绑定: bestPriority={bestPriority}, targetPriority={targetPriority}", LogHelper.LogType.Trace);
                        
                        bool wait = (PPTApplication != null);
                        DisconnectFromPPT();

                        if (bestApp != null)
                        {
                            if (wait) Thread.Sleep(1000);

                            try
                            {
                                LogHelper.WriteLogToFile("使用dynamic类型连接", LogHelper.LogType.Trace);
                                PPTApplication = bestApp;
                                ConnectToPPT(null);
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"连接失败: {ex.Message}", LogHelper.LogType.Warning);
                                PPTROTConnectionHelper.SafeReleaseComObject(bestApp);
                            }
                        }
                    }
                    else
                    {
                        if (bestApp != null && (PPTApplication == null || !PPTROTConnectionHelper.AreComObjectsEqual(PPTApplication, bestApp)))
                        {
                            PPTROTConnectionHelper.SafeReleaseComObject(bestApp);
                            bestApp = null;
                        }
                    }

                    if (PPTApplication != null && _pptActivePresentation != null)
                    {
                        CheckPresentationAndSlideShowState();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"PPT连接检查异常: {ex}", LogHelper.LogType.Error);
                    if (PPTApplication != null)
                    {
                        DisconnectFromPPT();
                    }
                }
            }
        }

        private void CheckPresentationAndSlideShowState()
        {
            try
            {
                dynamic activePresentation = null;
                dynamic slideShowWindow = null;

                try
                {
                    dynamic app = PPTApplication;
                    activePresentation = app.ActivePresentation;

                    if (activePresentation != null && _pptActivePresentation != null && !PPTROTConnectionHelper.AreComObjectsEqual(_pptActivePresentation, activePresentation))
                    {
                        DisconnectFromPPT();
                        return;
                    }
                }
                catch (COMException ex) when ((uint)ex.HResult == 0x8001010A)
                {
                    LogHelper.WriteLogToFile("PowerPoint 忙，稍后重试", LogHelper.LogType.Trace);
                    return;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"检查演示文稿状态失败: {ex.Message}，继续使用轮询模式", LogHelper.LogType.Warning);
                    activePresentation = null;
                }
                finally
                {
                    if (activePresentation != null && (_pptActivePresentation == null || !PPTROTConnectionHelper.AreComObjectsEqual(_pptActivePresentation, activePresentation)))
                    {
                        SafeReleaseComObject(activePresentation);
                    }
                }

                bool isSlideShowActive = false;
                try
                {
                    dynamic app = PPTApplication;
                    if (activePresentation == null)
                    {
                        try
                        {
                            activePresentation = app.ActivePresentation;
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"访问ActivePresentation失败: {ex.Message}，继续使用轮询模式", LogHelper.LogType.Warning);
                        }
                    }

                    if (activePresentation != null)
                    {
                        try
                        {
                            dynamic slideShowWindows = app.SlideShowWindows;
                            if (slideShowWindows != null && slideShowWindows.Count > 0)
                            {
                                isSlideShowActive = true;
                            }
                        }
                        catch { }
                    }

                    if (isSlideShowActive && activePresentation != null)
                    {
                        dynamic pres = activePresentation;
                        slideShowWindow = pres.SlideShowWindow;
                        if (_pptSlideShowWindow == null || !PPTROTConnectionHelper.IsValidSlideShowWindow(_pptSlideShowWindow))
                        {
                            if (!PPTROTConnectionHelper.AreComObjectsEqual(_pptSlideShowWindow, slideShowWindow))
                            {
                                SafeReleaseComObject(_pptSlideShowWindow);
                                _pptSlideShowWindow = slideShowWindow;
                                LogHelper.WriteLogToFile("发现窗口，成功设置 slideshowwindow", LogHelper.LogType.Trace);
                            }
                        }
                    }
                }
                catch (COMException ex) when ((uint)ex.HResult == 0x8001010A)
                {
                    LogHelper.WriteLogToFile("PowerPoint 忙，稍后重试", LogHelper.LogType.Trace);
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"发现窗口失败: {ex}", LogHelper.LogType.Warning);
                }
                finally
                {
                    if (activePresentation != null && (_pptActivePresentation == null || !PPTROTConnectionHelper.AreComObjectsEqual(_pptActivePresentation, activePresentation)))
                    {
                        SafeReleaseComObject(activePresentation);
                    }
                    if (slideShowWindow != null && !PPTROTConnectionHelper.AreComObjectsEqual(_pptSlideShowWindow, slideShowWindow))
                    {
                        SafeReleaseComObject(slideShowWindow);
                    }
                }

                if (isSlideShowActive)
                {
                    if ((DateTime.Now - _updateTime).TotalMilliseconds > 3000 || _forcePolling)
                    {
                        try
                        {
                            dynamic pres = _pptActivePresentation;
                            slideShowWindow = pres.SlideShowWindow;

                            int tempTotalPage = -1;
                            if (slideShowWindow != null)
                            {
                                tempTotalPage = GetTotalSlideIndex(_pptActivePresentation);
                            }

                            if (tempTotalPage == -1)
                            {
                                SlidesCount = 0;
                                _polling = 0;
                            }
                            else
                            {
                                try
                                {
                                    int currentPage = GetCurrentSlideIndex(_pptSlideShowWindow);
                                    SlidesCount = tempTotalPage;
                                    if (currentPage >= tempTotalPage) _polling = 1;
                                    else _polling = 0;
                                }
                                catch (Exception ex)
                                {
                                    SlidesCount = 0;
                                    _polling = 1;
                                    LogHelper.WriteLogToFile($"获取当前页数失败: {ex}", LogHelper.LogType.Warning);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SlidesCount = 0;
                            LogHelper.WriteLogToFile($"获取总页数失败: {ex.Message}", LogHelper.LogType.Warning);
                        }
                        finally
                        {
                            SafeReleaseComObject(slideShowWindow);
                        }

                        _updateTime = DateTime.Now;
                    }

                    if (_polling != 0)
                    {
                        try
                        {
                            int currentPage = GetCurrentSlideIndex(_pptSlideShowWindow);
                            UpdateCurrentPresentationInfo();
                            _polling = 2;
                        }
                        catch
                        {
                        }
                    }
                }
                else
                {
                    SlidesCount = 0;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查演示文稿和放映状态失败: {ex}", LogHelper.LogType.Warning);
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

                    if (!currentSlideShowState)
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查PPT放映状态异常: {ex}", LogHelper.LogType.Error);
            }
        }

        private Microsoft.Office.Interop.PowerPoint.Application TryConnectToPowerPoint()
        {
            try
            {
                var pptApp = PPTROTConnectionHelper.TryConnectViaROT(IsSupportWPS);
                return pptApp;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROT连接PowerPoint异常: {ex}", LogHelper.LogType.Error);
                return null;
            }
        }

        private Microsoft.Office.Interop.PowerPoint.Application TryConnectToWPS()
        {
            try
            {
                var wpsApp = PPTROTConnectionHelper.TryConnectViaROT(true);
                return wpsApp;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROT连接WPS异常: {ex}", LogHelper.LogType.Error);
                return null;
            }
        }

        private void ConnectToPPT(object pptApp)
        {
            try
            {
                if (pptApp != null)
                {
                    PPTApplication = pptApp;
                }

                try
                {
                    dynamic pptAppDynamic = PPTApplication;
                    try
                    {
                        _pptActivePresentation = pptAppDynamic.ActivePresentation;
                        _updateTime = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"访问ActivePresentation失败: {ex.Message}，继续使用轮询模式", LogHelper.LogType.Warning);
                        _pptActivePresentation = null;
                        _updateTime = DateTime.Now;
                    }

                    int tempTotalPage = -1;
                    if (_pptActivePresentation != null)
                    {
                        try
                        {
                            _pptSlideShowWindow = _pptActivePresentation.SlideShowWindow;
                            tempTotalPage = GetTotalSlideIndex(_pptActivePresentation);
                        }
                        catch
                        {
                            tempTotalPage = -1;
                        }
                    }
                    else
                    {
                        tempTotalPage = -1;
                    }

                    if (tempTotalPage == -1)
                    {
                        SlidesCount = 0;
                        _polling = 0;
                    }
                    else
                    {
                        try
                        {
                            if (_pptSlideShowWindow != null)
                            {
                                int currentPage = GetCurrentSlideIndex(_pptSlideShowWindow);
                                SlidesCount = tempTotalPage;
                                if (currentPage >= tempTotalPage) _polling = 1;
                                else _polling = 0;
                            }
                            else
                            {
                                SlidesCount = tempTotalPage;
                                _polling = 0;
                            }
                        }
                        catch
                        {
                            SlidesCount = 0;
                            _polling = 1;
                        }
                    }

                    try
                    {
                        if (PPTApplication != null)
                        {
                            try
                            {
                                Type appType = typeof(Microsoft.Office.Interop.PowerPoint.Application);
                                if (appType.IsInstanceOfType(PPTApplication))
                                {
                                    Microsoft.Office.Interop.PowerPoint.Application pptAppForEvents = (Microsoft.Office.Interop.PowerPoint.Application)PPTApplication;
                                    pptAppForEvents.SlideShowNextSlide += new EApplication_SlideShowNextSlideEventHandler(OnSlideShowNextSlide);
                                    pptAppForEvents.SlideShowBegin += new EApplication_SlideShowBeginEventHandler(OnSlideShowBegin);
                                    pptAppForEvents.SlideShowEnd += new EApplication_SlideShowEndEventHandler(OnSlideShowEnd);

                                    try
                                    {
                                        pptAppForEvents.PresentationBeforeClose += new EApplication_PresentationBeforeCloseEventHandler(OnPresentationBeforeClose);
                                    }
                                    catch
                                    {
                                        LogHelper.WriteLogToFile("无法注册PresentationBeforeClose事件", LogHelper.LogType.Warning);
                                    }

                                    _bindingEvents = true;
                                    _forcePolling = false;

                                    LogHelper.WriteLogToFile("PPT事件注册成功", LogHelper.LogType.Trace);
                                }
                                else
                                {
                                    _bindingEvents = false;
                                    _forcePolling = true;
                                    LogHelper.WriteLogToFile("无法转换为强类型Application，使用轮询模式", LogHelper.LogType.Trace);
                                }
                            }
                            catch (Exception ex)
                            {
                                _bindingEvents = false;
                                _forcePolling = true;
                                LogHelper.WriteLogToFile($"事件注册失败: {ex.Message}，使用轮询模式", LogHelper.LogType.Trace);
                            }
                        }
                        else
                        {
                            _bindingEvents = false;
                            _forcePolling = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _bindingEvents = false;
                        _forcePolling = true;
                        LogHelper.WriteLogToFile($"无法注册事件: {ex.Message}", LogHelper.LogType.Warning);
                    }

                    if (_pptActivePresentation != null)
                    {
                        UpdateCurrentPresentationInfo();
                    }

                    PPTConnectionChanged?.Invoke(true);

                    try
                    {
                        dynamic pptAppDynamic2 = PPTApplication;
                        LogHelper.WriteLogToFile($"成功绑定! {pptAppDynamic2.Name}", LogHelper.LogType.Event);
                    }
                    catch
                    {
                        LogHelper.WriteLogToFile("成功绑定!", LogHelper.LogType.Event);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"ConnectToPPT内部异常: {ex.Message}", LogHelper.LogType.Warning);
                    if (_pptActivePresentation == null && PPTApplication == null)
                    {
                        DisconnectFromPPT();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"连接PPT应用程序失败: {ex}", LogHelper.LogType.Error);
                PPTApplication = null;
            }
        }

        private void UnbindEvents()
        {
            try
            {
                if (_bindingEvents && PPTApplication != null)
                {
                    try
                    {
                        Type appType = typeof(Microsoft.Office.Interop.PowerPoint.Application);
                        if (appType.IsInstanceOfType(PPTApplication))
                        {
                            Microsoft.Office.Interop.PowerPoint.Application app = (Microsoft.Office.Interop.PowerPoint.Application)PPTApplication;
                            app.SlideShowNextSlide -= new EApplication_SlideShowNextSlideEventHandler(OnSlideShowNextSlide);
                            app.SlideShowBegin -= new EApplication_SlideShowBeginEventHandler(OnSlideShowBegin);
                            app.SlideShowEnd -= new EApplication_SlideShowEndEventHandler(OnSlideShowEnd);
                            app.PresentationBeforeClose -= new EApplication_PresentationBeforeCloseEventHandler(OnPresentationBeforeClose);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"取消PPT事件注册失败: {ex.Message}", LogHelper.LogType.Trace);
                    }

                    _bindingEvents = false;
                    _forcePolling = false;
                }
            }
            catch { }
        }

        private void DisconnectFromPPT()
        {
            if (PPTApplication == null && _pptActivePresentation == null)
            {
                return;
            }

            try
            {
                UnbindEvents();

                SafeReleaseComObject(_pptSlideShowWindow, "_pptSlideShowWindow");
                SafeReleaseComObject(_pptActivePresentation, "_pptActivePresentation");
                SafeReleaseComObject(CurrentSlide, "CurrentSlide");
                SafeReleaseComObject(CurrentSlides, "CurrentSlides");
                SafeReleaseComObject(CurrentPresentation, "CurrentPresentation");
                
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

                PPTApplication = null;
                _pptActivePresentation = null;
                _pptSlideShowWindow = null;
                CurrentPresentation = null;
                CurrentSlides = null;
                CurrentSlide = null;
                SlidesCount = 0;
                _polling = 0;
                _forcePolling = false;
                _bindingEvents = false;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                _connectionCheckTimer?.Stop();
                _slideShowStateCheckTimer?.Stop();

                // 触发连接断开事件
                PPTConnectionChanged?.Invoke(false);

                LogHelper.WriteLogToFile("已断开PPT连接，暂时卸载模块以确保COM完全释放", LogHelper.LogType.Event);

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
                        
                        LogHelper.WriteLogToFile("PPT联动模块已重新加载", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"重新加载PPT联动模块失败: {ex}", LogHelper.LogType.Error);
                        _isModuleUnloading = false;
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"断开PPT连接失败: {ex}", LogHelper.LogType.Error);
                _isModuleUnloading = false;
            }
        }

        /// <summary>
        /// 安全释放COM对象
        /// </summary>
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

        private void SafeReleaseComObject(object comObject, string objectName)
        {
            try
            {
                if (comObject != null && Marshal.IsComObject(comObject))
                {
                    int refCount = Marshal.ReleaseComObject(comObject);
                    LogHelper.WriteLogToFile($"已释放COM对象 {objectName}，引用计数: {refCount}", LogHelper.LogType.Trace);
                }
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                LogHelper.WriteLogToFile($"释放COM对象 {objectName} 时COM异常: {comEx.Message} (HR: 0x{hr:X8})", LogHelper.LogType.Warning);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"释放COM对象 {objectName} 时发生异常: {ex}", LogHelper.LogType.Warning);
            }
        }

        private void UpdateCurrentPresentationInfo()
        {
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
                        if (_pptActivePresentation != null)
                        {
                            try
                            {
                                dynamic pres = _pptActivePresentation;
                                CurrentPresentation = pres;
                                CurrentSlides = pres.Slides;
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"访问演示文稿属性失败: {ex.Message}", LogHelper.LogType.Warning);
                                CurrentPresentation = null;
                                CurrentSlides = null;
                            }

                            if (CurrentSlides != null)
                            {
                                try
                                {
                                    var slideCount = CurrentSlides.Count;
                                    if (slideCount > 0)
                                    {
                                        SlidesCount = slideCount;
                                    }
                                    else
                                    {
                                        SlidesCount = 0;
                                        LogHelper.WriteLogToFile("PPT演示文稿页数为0，可能为空演示文稿", LogHelper.LogType.Warning);
                                    }
                                }
                                catch (COMException comEx)
                                {
                                    var hr = (uint)comEx.HResult;
                                    SlidesCount = 0;
                                    LogHelper.WriteLogToFile($"读取PPT页数失败: {comEx.Message} (HR: 0x{hr:X8})", LogHelper.LogType.Warning);
                                }

                                try
                                {
                                    if (IsInSlideShow && _pptSlideShowWindow != null)
                                    {
                                        try
                                        {
                                            dynamic ssw = _pptSlideShowWindow;
                                            view = ssw.View;
                                            if (view != null)
                                            {
                                                dynamic viewObj = view;
                                                CurrentSlide = viewObj.Slide;
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogHelper.WriteLogToFile($"获取SlideShowWindow的Slide失败: {ex.Message}", LogHelper.LogType.Trace);
                                        }
                                    }
                                    else
                                    {
                                        dynamic app = PPTApplication;
                                        activeWindow = app.ActiveWindow;
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
                                catch (COMException comEx)
                                {
                                    var hr = (uint)comEx.HResult;
                                    if (hr != 0x8001010E && hr != 0x80004005)
                                    {
                                        LogHelper.WriteLogToFile($"获取当前幻灯片失败: {comEx.Message}", LogHelper.LogType.Warning);
                                    }

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
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x8001010E || hr == 0x80004005)
                        {
                            CurrentPresentation = null;
                            CurrentSlides = null;
                            CurrentSlide = null;
                            SlidesCount = 0;
                        }
                        else
                        {
                            LogHelper.WriteLogToFile($"访问活动演示文稿失败: {comEx}", LogHelper.LogType.Warning);
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
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"更新演示文稿信息失败: {ex}", LogHelper.LogType.Error);
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
                SafeReleaseComObject(activeWindow);
            }
        }
        #endregion

        #region Event Handlers
        private void OnPresentationOpen(Presentation pres)
        {
            try
            {
                if (PPTApplication != null && pres != null)
                {
                    try
                    {
                        dynamic app = PPTApplication;
                        _pptActivePresentation = app.ActivePresentation;
                        _updateTime = DateTime.Now;
                    }
                    catch { }
                }

                UpdateCurrentPresentationInfo();
                PresentationOpen?.Invoke(pres);
                LogHelper.WriteLogToFile($"演示文稿已打开: {pres?.Name}", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理演示文稿打开事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnPresentationBeforeClose(Presentation pres, ref bool cancel)
        {
            try
            {
                LogHelper.WriteLogToFile("PresentationBeforeClose事件触发", LogHelper.LogType.Trace);

                if (_bindingEvents && PPTApplication != null)
                {
                    try
                    {
                        Type appType = typeof(Microsoft.Office.Interop.PowerPoint.Application);
                        if (appType.IsInstanceOfType(PPTApplication))
                        {
                            Microsoft.Office.Interop.PowerPoint.Application app = (Microsoft.Office.Interop.PowerPoint.Application)PPTApplication;
                            app.SlideShowNextSlide -= new EApplication_SlideShowNextSlideEventHandler(OnSlideShowNextSlide);
                            app.SlideShowBegin -= new EApplication_SlideShowBeginEventHandler(OnSlideShowBegin);
                            app.SlideShowEnd -= new EApplication_SlideShowEndEventHandler(OnSlideShowEnd);
                            app.PresentationBeforeClose -= new EApplication_PresentationBeforeCloseEventHandler(OnPresentationBeforeClose);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"取消PPT事件注册失败: {ex.Message}", LogHelper.LogType.Trace);
                    }

                    _bindingEvents = false;
                    _forcePolling = false;
                }

                DisconnectFromPPT();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理演示文稿关闭前事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowBegin(SlideShowWindow wn)
        {
            try
            {
                _updateTime = DateTime.Now;
                _pptSlideShowWindow = wn;

                try
                {
                    if (_pptActivePresentation != null)
                    {
                        int currentPage = GetCurrentSlideIndex(_pptSlideShowWindow);
                        int totalPage = GetTotalSlideIndex(_pptActivePresentation);
                        
                        if (currentPage >= totalPage) _polling = 1;
                        else _polling = 0;
                        
                        SlidesCount = totalPage;
                    }
                }
                catch
                {
                    _polling = 1;
                }

                UpdateCurrentPresentationInfo();
                SlideShowBegin?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片放映开始事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowNextSlide(SlideShowWindow wn)
        {
            try
            {
                _updateTime = DateTime.Now;

                try
                {
                    if (_pptActivePresentation != null && _pptSlideShowWindow != null)
                    {
                        int currentPage = GetCurrentSlideIndex(_pptSlideShowWindow);
                        int totalPage = GetTotalSlideIndex(_pptActivePresentation);
                        
                        if (currentPage >= totalPage) _polling = 1;
                        else _polling = 0;
                    }
                }
                catch
                {
                    _polling = 1;
                }

                UpdateCurrentPresentationInfo();
                SlideShowNextSlide?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片切换事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowEnd(Presentation pres)
        {
            try
            {
                _updateTime = DateTime.Now;
                SlidesCount = 0;

                // 记录WPS进程用于后续管理
                if (IsSupportWPS && PPTApplication != null)
                {
                    RecordWpsProcessForManagement();
                }

                UpdateCurrentPresentationInfo();
                SlideShowEnd?.Invoke(pres);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片放映结束事件失败: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion

        #region Public Methods
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
                    dynamic app = PPTApplication;
                    slideShowWindows = app.SlideShowWindows;
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
                LogHelper.WriteLogToFile($"跳转到幻灯片{slideNumber}失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"跳转到幻灯片{slideNumber}失败: {ex}", LogHelper.LogType.Error);
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
            try
            {
                if (!IsConnected || !IsInSlideShow || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                // 在新线程中执行翻页操作，避免等待动画完成
                new Thread(() =>
                {
                    try
                    {
                        dynamic app = PPTApplication;
                        object slideShowWindows = app.SlideShowWindows;
                        if (slideShowWindows != null)
                        {
                            dynamic ssw = slideShowWindows;
                            object slideShowWindow = ssw[1];
                            if (slideShowWindow != null)
                            {
                                dynamic sswObj = slideShowWindow;
                                try
                                {
                                    sswObj.Activate();
                                }
                                catch { }
                                try
                                {
                                    object view = sswObj.View;
                                    if (view != null)
                                    {
                                        dynamic viewObj = view;
                                        viewObj.Next();
                                    }
                                }
                                catch { }
                                SafeReleaseComObject(slideShowWindow);
                            }
                            SafeReleaseComObject(slideShowWindows);
                        }
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x8001010E || hr == 0x80004005)
                        {
                            DisconnectFromPPT();
                        }
                        LogHelper.WriteLogToFile($"切换到下一页失败: {comEx.Message}", LogHelper.LogType.Error);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"切换到下一页失败: {ex}", LogHelper.LogType.Error);
                    }
                }).Start();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"启动下一页线程失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        public bool TryNavigatePrevious()
        {
            try
            {
                if (!IsConnected || !IsInSlideShow || PPTApplication == null) return false;
                if (!Marshal.IsComObject(PPTApplication)) return false;

                // 在新线程中执行翻页操作，避免等待动画完成
                new Thread(() =>
                {
                    try
                    {
                        dynamic app = PPTApplication;
                        object slideShowWindows = app.SlideShowWindows;
                        if (slideShowWindows != null)
                        {
                            dynamic ssw = slideShowWindows;
                            object slideShowWindow = ssw[1];
                            if (slideShowWindow != null)
                            {
                                dynamic sswObj = slideShowWindow;
                                try
                                {
                                    sswObj.Activate();
                                }
                                catch { }
                                try
                                {
                                    object view = sswObj.View;
                                    if (view != null)
                                    {
                                        dynamic viewObj = view;
                                        viewObj.Previous();
                                    }
                                }
                                catch { }
                                SafeReleaseComObject(slideShowWindow);
                            }
                            SafeReleaseComObject(slideShowWindows);
                        }
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x8001010E || hr == 0x80004005)
                        {
                            DisconnectFromPPT();
                        }
                        LogHelper.WriteLogToFile($"切换到上一页失败: {comEx.Message}", LogHelper.LogType.Error);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"切换到上一页失败: {ex}", LogHelper.LogType.Error);
                    }
                }).Start();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"启动上一页线程失败: {ex}", LogHelper.LogType.Error);
                return false;
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

                dynamic app = PPTApplication;
                slideShowWindows = app.SlideShowWindows;
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
                LogHelper.WriteLogToFile($"结束幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"结束幻灯片放映失败: {ex}", LogHelper.LogType.Error);
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
                    // COM对象已失效，触发断开连接
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"开始幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"开始幻灯片放映失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 获取当前活跃的演示文稿
        /// </summary>
        public object GetCurrentActivePresentation()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            object slide = null;
            object activeWindow = null;
            object presentation = null;
            try
            {
                if (!IsConnected || PPTApplication == null) return null;
                if (!Marshal.IsComObject(PPTApplication)) return null;

                if (IsInSlideShow)
                {
                    dynamic pptAppForSSW = PPTApplication;
                    slideShowWindows = pptAppForSSW.SlideShowWindows;
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
                                    slide = viewObj.Slide;
                                    if (slide != null)
                                    {
                                        dynamic slideObj = slide;
                                        presentation = slideObj.Parent;
                                        return presentation;
                                    }
                                }
                            }
                        }
                    }
                }

                dynamic app = PPTApplication;
                activeWindow = app.ActiveWindow;
                if (activeWindow != null)
                {
                    dynamic aw = activeWindow;
                    presentation = aw.Presentation;
                    if (presentation != null)
                    {
                        return presentation;
                    }
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
                if (hr == 0x80048240)
                {
                    return null;
                }
                LogHelper.WriteLogToFile($"获取当前活跃演示文稿失败: {comEx.Message}", LogHelper.LogType.Warning);
                return CurrentPresentation;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取当前活跃演示文稿失败: {ex}", LogHelper.LogType.Error);
                return CurrentPresentation;
            }
            finally
            {
                if (presentation != null && !ReferenceEquals(presentation, CurrentPresentation))
                {
                    SafeReleaseComObject(presentation);
                }
                SafeReleaseComObject(slide);
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
                SafeReleaseComObject(activeWindow);
            }
        }

        /// <summary>
        /// 获取当前幻灯片编号
        /// </summary>
        private int GetCurrentSlideIndex(dynamic slideShowWindow)
        {
            object view = null;
            object slide = null;

            try
            {
                if (slideShowWindow == null) return 0;
                dynamic ssw = slideShowWindow;
                view = ssw.View;
                if (view != null)
                {
                    dynamic viewObj = view;
                    slide = viewObj.Slide;
                    if (slide != null)
                    {
                        dynamic slideObj = slide;
                        return slideObj.SlideIndex;
                    }
                }
                return 0;
            }
            finally
            {
                SafeReleaseComObject(slide);
                SafeReleaseComObject(view);
            }
        }

        private int GetTotalSlideIndex(dynamic presentation)
        {
            try
            {
                if (presentation == null) return 0;
                dynamic pres = presentation;
                return pres.Slides.Count;
            }
            catch
            {
                return 0;
            }
        }

        public int GetCurrentSlideNumber()
        {
            object activeWindow = null;
            object selection = null;
            object slideRange = null;
            try
            {
                if (!IsConnected || PPTApplication == null) return 0;
                if (!Marshal.IsComObject(PPTApplication)) return 0;

                if (IsInSlideShow && _pptSlideShowWindow != null)
                {
                    try
                    {
                        return GetCurrentSlideIndex(_pptSlideShowWindow);
                    }
                    catch
                    {
                        return 0;
                    }
                }

                dynamic app = PPTApplication;
                activeWindow = app.ActiveWindow;
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
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                SafeReleaseComObject(slideRange);
                SafeReleaseComObject(selection);
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
                    // COM对象已失效，触发断开连接
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"获取演示文稿名称失败: {comEx.Message}", LogHelper.LogType.Warning);
                return "";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取演示文稿名称失败: {ex}", LogHelper.LogType.Error);
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
                    // COM对象已失效，触发断开连接
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"获取演示文稿路径失败: {comEx.Message}", LogHelper.LogType.Warning);
                return "";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取演示文稿路径失败: {ex}", LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile($"尝试显示幻灯片导航 - 连接状态: {IsConnected}, 放映状态: {IsInSlideShow}", LogHelper.LogType.Trace);

                if (!IsConnected || !IsInSlideShow || PPTApplication == null)
                {
                    LogHelper.WriteLogToFile("PPT未连接或未在放映状态", LogHelper.LogType.Warning);
                    return false;
                }

                if (!Marshal.IsComObject(PPTApplication))
                {
                    LogHelper.WriteLogToFile("PPT应用程序COM对象无效", LogHelper.LogType.Warning);
                    return false;
                }

                dynamic app = PPTApplication;
                slideShowWindows = app.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow == null)
                    {
                        LogHelper.WriteLogToFile("幻灯片放映窗口为空", LogHelper.LogType.Warning);
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
                            LogHelper.WriteLogToFile("成功显示幻灯片导航（PowerPoint模式）", LogHelper.LogType.Event);
                            return true;
                        }

                        LogHelper.WriteLogToFile("SlideNavigation对象为空，可能是WPS不支持此功能", LogHelper.LogType.Warning);
                        return false;
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x80020006)
                        {
                            LogHelper.WriteLogToFile("WPS不支持SlideNavigation功能", LogHelper.LogType.Warning);
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
                LogHelper.WriteLogToFile($"显示幻灯片导航COM异常: {comEx.Message} (HRESULT: 0x{hr:X8})", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"显示幻灯片导航失败: {ex}", LogHelper.LogType.Error);
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

        #region WPS Process Management
        private void RecordWpsProcessForManagement()
        {
            if (!IsSupportWPS || PPTApplication == null) return;

            try
            {
                Process wpsProcess = null;

                // 方法1：通过应用程序路径检测
                dynamic app = PPTApplication;
                if (app.Path.Contains("Kingsoft\\WPS Office\\") ||
                    app.Path.Contains("WPS Office\\"))
                {
                    uint processId;
                    GetWindowThreadProcessId((IntPtr)app.HWND, out processId);
                    wpsProcess = Process.GetProcessById((int)processId);
                }

                // 方法2：通过前台窗口检测
                if (wpsProcess == null)
                {
                    var foregroundWpsWindow = GetForegroundWpsWindow();
                    if (foregroundWpsWindow != null)
                    {
                        wpsProcess = Process.GetProcessById((int)foregroundWpsWindow.ProcessId);
                    }
                }

                // 方法3：通过进程名检测
                if (wpsProcess == null)
                {
                    var wpsProcesses = GetWpsProcesses();
                    if (wpsProcesses.Count > 0)
                    {
                        wpsProcess = wpsProcesses.First();
                    }
                }

                if (wpsProcess != null)
                {
                    _wpsProcess = wpsProcess;
                    _hasWpsProcessId = true;
                    _wpsProcessRecordTime = DateTime.Now;
                    _wpsProcessCheckCount = 0;
                    LogHelper.WriteLogToFile($"成功记录 WPS 进程 ID: {wpsProcess.Id}", LogHelper.LogType.Trace);

                    StartWpsProcessCheckTimer();
                }
                else
                {
                    LogHelper.WriteLogToFile("未能检测到WPS进程", LogHelper.LogType.Warning);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"记录WPS进程失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void StartWpsProcessCheckTimer()
        {
            if (!IsSupportWPS) return;

            if (_wpsProcessCheckTimer != null)
            {
                _wpsProcessCheckTimer.Stop();
                _wpsProcessCheckTimer.Dispose();
            }

            // 增加检查间隔到2秒，减少性能开销
            _wpsProcessCheckTimer = new Timer(2000);
            _wpsProcessCheckTimer.Elapsed += OnWpsProcessCheckTimerElapsed;
            _wpsProcessCheckTimer.Start();
            LogHelper.WriteLogToFile("启动 WPS 进程检测定时器", LogHelper.LogType.Trace);
        }

        private void OnWpsProcessCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsSupportWPS)
            {
                StopWpsProcessCheckTimer();
                return;
            }

            try
            {
                if (_wpsProcess == null || !_hasWpsProcessId)
                {
                    StopWpsProcessCheckTimer();
                    return;
                }

                _wpsProcess.Refresh();
                _wpsProcessCheckCount++;

                if (_wpsProcess.HasExited)
                {
                    LogHelper.WriteLogToFile("WPS 进程已正常关闭", LogHelper.LogType.Trace);
                    StopWpsProcessCheckTimer();
                    return;
                }


                // 检查前台WPS窗口是否存在
                bool isForegroundWpsWindowActive = IsForegroundWpsWindowStillActiveOptimized();

                if (isForegroundWpsWindowActive)
                {
                    if (_wpsProcessCheckCount % 5 == 0) // 每10秒记录一次日志
                    {
                        LogHelper.WriteLogToFile($"WPS窗口仍然活跃，继续监控（已检查{_wpsProcessCheckCount}次）", LogHelper.LogType.Trace);
                    }
                    return;
                }

                // 多重验证确保准确性
                if (!PerformMultipleWpsWindowChecks())
                {
                    LogHelper.WriteLogToFile("多重验证显示WPS窗口仍然存在，跳过查杀", LogHelper.LogType.Trace);
                    return;
                }

                // 前台窗口已消失，准备结束WPS进程
                LogHelper.WriteLogToFile("多重验证确认WPS窗口已消失，准备结束WPS进程", LogHelper.LogType.Event);

                // 安全结束WPS进程
                SafeTerminateWpsProcess();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"WPS 进程检测失败: {ex}", LogHelper.LogType.Error);
                StopWpsProcessCheckTimer();
            }
        }

        /// <summary>
        /// 前台WPS窗口检测
        /// </summary>
        private bool IsForegroundWpsWindowStillActiveOptimized()
        {
            try
            {
                // 快速检查：直接检查前台窗口
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero) return false;

                // 获取前台窗口的进程ID
                uint processId;
                GetWindowThreadProcessId(foregroundWindow, out processId);

                // 如果前台窗口就是我们监控的WPS进程，则认为仍然活跃
                if (processId == _wpsProcess?.Id)
                {
                    return true;
                }

                // 检查是否为WPS相关窗口
                var windowInfo = GetWindowInfo(foregroundWindow);
                return IsWpsWindow(windowInfo);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"WPS窗口检测失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 多重验证WPS窗口状态，确保查杀准确性
        /// </summary>
        private bool PerformMultipleWpsWindowChecks()
        {
            try
            {
                // 第一重验证：等待1秒后再次检查
                Thread.Sleep(1000);
                if (IsForegroundWpsWindowStillActiveOptimized())
                {
                    LogHelper.WriteLogToFile("第一重验证：WPS窗口仍然存在", LogHelper.LogType.Trace);
                    return false;
                }

                // 第二重验证：检查所有WPS进程的窗口
                var wpsProcesses = GetWpsProcesses();
                foreach (var process in wpsProcesses)
                {
                    if (process.Id == _wpsProcess?.Id) continue; // 跳过当前监控的进程

                    var windows = GetWpsWindowsByProcess(process.Id);
                    if (windows.Any(w => w.IsVisible && !w.IsMinimized))
                    {
                        LogHelper.WriteLogToFile($"第二重验证：发现其他WPS进程{process.Id}有活跃窗口", LogHelper.LogType.Trace);
                        return false;
                    }
                }

                // 第三重验证：检查任务栏中的WPS窗口
                if (HasWpsWindowInTaskbar())
                {
                    LogHelper.WriteLogToFile("第三重验证：任务栏中仍有WPS窗口", LogHelper.LogType.Trace);
                    return false;
                }

                LogHelper.WriteLogToFile("多重验证完成：确认WPS窗口已全部消失", LogHelper.LogType.Event);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"多重验证失败: {ex}", LogHelper.LogType.Error);
                return false; // 出错时保守处理，不进行查杀
            }
        }

        /// <summary>
        /// 检查任务栏中是否有WPS窗口
        /// </summary>
        private bool HasWpsWindowInTaskbar()
        {
            try
            {
                var allWindows = new List<WpsWindowInfo>();

                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        if (IsWindow(hWnd) && IsWindowVisible(hWnd))
                        {
                            var windowInfo = GetWindowInfo(hWnd);
                            if (IsWpsWindow(windowInfo) && !string.IsNullOrEmpty(windowInfo.Title))
                            {
                                allWindows.Add(windowInfo);
                            }
                        }
                    }
                    catch { }
                    return true;
                }, IntPtr.Zero);

                return allWindows.Count > 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查任务栏WPS窗口失败: {ex}", LogHelper.LogType.Error);
                return true; // 出错时保守处理，认为仍有窗口
            }
        }

        /// <summary>
        /// 安全地结束WPS进程 - 通过释放PPTCOM对象
        /// </summary>
        private void SafeTerminateWpsProcess()
        {
            try
            {
                if (_wpsProcess == null || _wpsProcess.HasExited)
                {
                    LogHelper.WriteLogToFile("WPS进程已经结束，无需查杀", LogHelper.LogType.Trace);
                    StopWpsProcessCheckTimer();
                    return;
                }

                LogHelper.WriteLogToFile($"开始通过释放PPTCOM对象安全结束WPS进程 (PID: {_wpsProcess.Id})", LogHelper.LogType.Event);

                // 第一步：释放 pptActWindow 对象（SlideShowWindow）
                SlideShowWindow pptActWindow = null;
                try
                {
                    if (PPTApplication != null && Marshal.IsComObject(PPTApplication))
                    {
                        dynamic app = PPTApplication;
                        if (app.SlideShowWindows?.Count > 0)
                        {
                            pptActWindow = app.SlideShowWindows[1];
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"获取SlideShowWindow对象时发生异常: {ex}", LogHelper.LogType.Warning);
                }

                if (pptActWindow != null)
                {
                    Marshal.ReleaseComObject(pptActWindow);
                    pptActWindow = null;
                    LogHelper.WriteLogToFile("已释放pptActWindow对象", LogHelper.LogType.Trace);
                }

                // 第二步：释放 pptActDoc 对象（CurrentPresentation）
                Presentation pptActDoc = CurrentPresentation;
                if (pptActDoc != null)
                {
                    Marshal.ReleaseComObject(pptActDoc);
                    pptActDoc = null;
                    CurrentPresentation = null;
                    LogHelper.WriteLogToFile("已释放pptActDoc对象", LogHelper.LogType.Trace);
                }

                // 第三步：释放 pptApp 对象（PPTApplication）
                if (PPTApplication != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(PPTApplication);
                        PPTApplication = null;
                        LogHelper.WriteLogToFile("已释放pptApp对象", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"释放pptApp对象失败: {ex.Message}", LogHelper.LogType.Trace);
                        PPTApplication = null;
                    }
                }

                // 第四步：强制垃圾回收及等待终结器执行
                LogHelper.WriteLogToFile("执行强制垃圾回收", LogHelper.LogType.Trace);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // 等待一段时间让COM对象完全释放
                Thread.Sleep(1000);

                // 检查进程是否已经结束
                try
                {
                    _wpsProcess.Refresh();
                    if (_wpsProcess.HasExited)
                    {
                        LogHelper.WriteLogToFile("WPS进程已通过COM对象释放成功结束", LogHelper.LogType.Event);
                        StopWpsProcessCheckTimer();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"检查WPS进程状态失败: {ex}", LogHelper.LogType.Warning);
                }

                // 备用方案：如果COM对象释放后进程仍未结束，尝试关闭
                try
                {
                    LogHelper.WriteLogToFile("COM对象释放后进程仍在运行，尝试关闭", LogHelper.LogType.Warning);
                    _wpsProcess.CloseMainWindow();
                    if (_wpsProcess.WaitForExit(3000)) // 等待3秒
                    {
                        LogHelper.WriteLogToFile("WPS进程已关闭", LogHelper.LogType.Event);
                        StopWpsProcessCheckTimer();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"关闭WPS进程失败: {ex}", LogHelper.LogType.Warning);
                }

                // 最后备用方案：强制结束进程
                try
                {
                    if (!_wpsProcess.HasExited)
                    {
                        LogHelper.WriteLogToFile("所有方法都失败，强制结束WPS进程", LogHelper.LogType.Warning);
                        _wpsProcess.Kill();
                        LogHelper.WriteLogToFile("WPS进程已强制结束", LogHelper.LogType.Event);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"强制结束WPS进程失败: {ex}", LogHelper.LogType.Error);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"安全结束WPS进程时发生异常: {ex}", LogHelper.LogType.Error);
            }
            finally
            {
                // 确保清理状态
                if (CurrentSlide != null && Marshal.IsComObject(CurrentSlide))
                {
                    try { Marshal.ReleaseComObject(CurrentSlide); } catch { }
                }
                if (CurrentSlides != null && Marshal.IsComObject(CurrentSlides))
                {
                    try { Marshal.ReleaseComObject(CurrentSlides); } catch { }
                }
                if (CurrentPresentation != null && Marshal.IsComObject(CurrentPresentation))
                {
                    try { Marshal.ReleaseComObject(CurrentPresentation); } catch { }
                }
                if (PPTApplication != null && Marshal.IsComObject(PPTApplication))
                {
                    try { Marshal.ReleaseComObject(PPTApplication); } catch { }
                }

                CurrentSlide = null;
                CurrentSlides = null;
                CurrentPresentation = null;
                PPTApplication = null;
                SlidesCount = 0;
                StopWpsProcessCheckTimer();

                // 重新启动连接检查定时器，以便能够检测新的WPS实例
                _connectionCheckTimer?.Start();

                // 触发连接断开事件
                PPTConnectionChanged?.Invoke(false);

                LogHelper.WriteLogToFile("WPS进程结束后已清理所有COM对象并重启连接检查", LogHelper.LogType.Event);
            }
        }



        private void StopWpsProcessCheckTimer()
        {
            if (_wpsProcessCheckTimer != null)
            {
                _wpsProcessCheckTimer.Stop();
                _wpsProcessCheckTimer.Dispose();
                _wpsProcessCheckTimer = null;
            }

            _wpsProcess = null;
            _hasWpsProcessId = false;
            _wpsProcessRecordTime = DateTime.MinValue;
            _wpsProcessCheckCount = 0;
            _lastForegroundWpsWindow = null;
            _lastWindowCheckTime = DateTime.MinValue;
            LogHelper.WriteLogToFile("停止 WPS 进程检测定时器", LogHelper.LogType.Trace);
        }
        #endregion

        #region WPS Window Detection
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private class WpsWindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
            public string ClassName { get; set; }
            public bool IsVisible { get; set; }
            public bool IsMinimized { get; set; }
            public bool IsMaximized { get; set; }
            public RECT Rect { get; set; }
            public uint ProcessId { get; set; }
            public string ProcessName { get; set; }
        }

        private WpsWindowInfo GetForegroundWpsWindow()
        {
            try
            {
                var foregroundHwnd = GetForegroundWindow();
                if (foregroundHwnd != IntPtr.Zero && IsWindow(foregroundHwnd))
                {
                    var windowInfo = GetWindowInfo(foregroundHwnd);
                    if (IsWpsWindow(windowInfo))
                    {
                        return windowInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取前台WPS窗口失败: {ex}", LogHelper.LogType.Error);
            }
            return null;
        }

        private WpsWindowInfo GetWindowInfo(IntPtr hWnd)
        {
            var windowInfo = new WpsWindowInfo
            {
                Handle = hWnd,
                IsVisible = IsWindowVisible(hWnd),
                IsMinimized = IsIconic(hWnd),
                IsMaximized = IsZoomed(hWnd)
            };

            // 获取窗口标题
            var windowTitle = new StringBuilder(256);
            GetWindowText(hWnd, windowTitle, 256);
            windowInfo.Title = windowTitle.ToString().Trim();

            // 获取窗口类名
            var className = new StringBuilder(256);
            GetClassName(hWnd, className, 256);
            windowInfo.ClassName = className.ToString().Trim();

            // 获取窗口位置
            GetWindowRect(hWnd, out RECT rect);
            windowInfo.Rect = rect;

            // 获取进程ID
            uint processId;
            GetWindowThreadProcessId(hWnd, out processId);
            windowInfo.ProcessId = processId;

            // 获取进程名
            windowInfo.ProcessName = "";
            try
            {
                var proc = Process.GetProcessById((int)processId);
                windowInfo.ProcessName = proc.ProcessName.ToLower();
            }
            catch { }

            return windowInfo;
        }

        private bool IsWpsWindow(WpsWindowInfo windowInfo)
        {
            if (string.IsNullOrEmpty(windowInfo.Title) && string.IsNullOrEmpty(windowInfo.ClassName))
                return false;

            var title = windowInfo.Title.ToLower();
            var className = windowInfo.ClassName.ToLower();
            var processName = windowInfo.ProcessName ?? "";

            // WPS相关关键词
            var wpsKeywords = new[] { "wps", "wpp", "kingsoft", "金山", "wps演示", "wps presentation", "wps office", "kingsoft office" };
            // 微软Office相关进程名
            var msOfficeProcess = new[] { "powerpnt", "excel", "word", "onenote", "outlook", "microsoftoffice", "office" };

            // 只要进程名是微软Office，直接排除
            if (msOfficeProcess.Any(keyword => processName.Contains(keyword)))
                return false;

            // 只要进程名是WPS/WPP/Kingsoft，直接通过
            if (wpsKeywords.Any(keyword => processName.Contains(keyword)))
                return true;

            // 标题或类名包含WPS相关关键词
            bool hasWpsTitle = wpsKeywords.Any(keyword => title.Contains(keyword));
            bool hasWpsClass = wpsKeywords.Any(keyword => className.Contains(keyword));
            bool isWpsClass = className.Contains("wps") || className.Contains("kingsoft") || className.Contains("wpp");
            bool hasValidSize = (windowInfo.Rect.Right - windowInfo.Rect.Left) > 0 && (windowInfo.Rect.Bottom - windowInfo.Rect.Top) > 0;

            return (hasWpsTitle || hasWpsClass || isWpsClass) && hasValidSize;
        }

        private List<Process> GetWpsProcesses()
        {
            var wpsProcesses = new List<Process>();
            try
            {
                var allProcesses = Process.GetProcesses();
                foreach (var process in allProcesses)
                {
                    try
                    {
                        var pname = process.ProcessName.ToLower();

                        // 精确的WPS进程名匹配，避免误杀
                        var exactWpsNames = new[] { "wps", "wpp", "et", "wpspdf", "wpsoffice" };
                        var microsoftOfficeNames = new[] { "powerpnt", "excel", "word", "onenote", "outlook", "winword", "msaccess" };

                        // 排除微软Office进程
                        if (microsoftOfficeNames.Any(name => pname.Contains(name)))
                        {
                            continue;
                        }

                        // 精确匹配WPS进程名
                        bool isWpsProcess = exactWpsNames.Any(name => pname.Equals(name) || pname.StartsWith(name + "."));

                        // 额外验证：检查进程路径
                        if (isWpsProcess)
                        {
                            try
                            {
                                var processPath = process.MainModule?.FileName?.ToLower() ?? "";
                                if (processPath.Contains("kingsoft") || processPath.Contains("wps office"))
                                {
                                    wpsProcesses.Add(process);
                                    LogHelper.WriteLogToFile($"检测到WPS进程: {process.ProcessName} (PID: {process.Id})", LogHelper.LogType.Trace);
                                }
                            }
                            catch
                            {
                                // 无法访问进程路径时，基于进程名判断
                                if (exactWpsNames.Contains(pname))
                                {
                                    wpsProcesses.Add(process);
                                    LogHelper.WriteLogToFile($"基于进程名检测到WPS进程: {process.ProcessName} (PID: {process.Id})", LogHelper.LogType.Trace);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"检查进程{process.ProcessName}失败: {ex}", LogHelper.LogType.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取WPS进程失败: {ex}", LogHelper.LogType.Error);
            }

            LogHelper.WriteLogToFile($"共检测到{wpsProcesses.Count}个WPS进程", LogHelper.LogType.Trace);
            return wpsProcesses;
        }

        private bool IsForegroundWpsWindowStillActive()
        {
            try
            {
                var currentTime = DateTime.Now;
                var currentForegroundWindow = GetForegroundWpsWindow();

                // 检查窗口状态是否发生变化
                if (_lastForegroundWpsWindow != null && currentForegroundWindow != null)
                {
                    if (_lastForegroundWpsWindow.Handle != currentForegroundWindow.Handle ||
                        _lastForegroundWpsWindow.Title != currentForegroundWindow.Title)
                    {
                        LogHelper.WriteLogToFile($"前台WPS窗口发生变化: {_lastForegroundWpsWindow.Title} -> {currentForegroundWindow.Title}", LogHelper.LogType.Trace);
                    }
                }
                else if (_lastForegroundWpsWindow == null && currentForegroundWindow != null)
                {
                    LogHelper.WriteLogToFile($"检测到新的前台WPS窗口: {currentForegroundWindow.Title}", LogHelper.LogType.Trace);
                }
                else if (_lastForegroundWpsWindow != null && currentForegroundWindow == null)
                {
                    LogHelper.WriteLogToFile($"前台WPS窗口已消失: {_lastForegroundWpsWindow.Title}", LogHelper.LogType.Trace);
                }

                // 更新记录
                _lastForegroundWpsWindow = currentForegroundWindow;
                _lastWindowCheckTime = currentTime;

                if (currentForegroundWindow != null)
                {
                    if (IsWindow(currentForegroundWindow.Handle) && IsWindowVisible(currentForegroundWindow.Handle))
                    {
                        return true;
                    }
                }

                // 检查所有WPS进程的活跃窗口
                var wpsProcesses = GetWpsProcesses();
                foreach (var process in wpsProcesses)
                {
                    var windows = GetWpsWindowsByProcess(process.Id);
                    if (windows.Any(w => w.IsVisible && !w.IsMinimized && w.Handle == GetForegroundWindow()))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查前台WPS窗口状态失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
        }

        private List<WpsWindowInfo> GetWpsWindowsByProcess(int processId)
        {
            var wpsWindows = new List<WpsWindowInfo>();

            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    try
                    {
                        if (!IsWindow(hWnd)) return true;

                        uint windowProcessId;
                        GetWindowThreadProcessId(hWnd, out windowProcessId);

                        if ((int)windowProcessId == processId)
                        {
                            var windowInfo = GetWindowInfo(hWnd);
                            if (IsWpsWindow(windowInfo))
                            {
                                wpsWindows.Add(windowInfo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"枚举窗口时出错: {ex}", LogHelper.LogType.Error);
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取WPS窗口失败: {ex}", LogHelper.LogType.Error);
            }

            return wpsWindows;
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            if (!_disposed)
            {
                StopMonitoring();
                StopWpsProcessCheckTimer();

                _connectionCheckTimer?.Dispose();
                _slideShowStateCheckTimer?.Dispose();
                _wpsProcessCheckTimer?.Dispose();

                _disposed = true;
            }
        }
        #endregion
    }
}


