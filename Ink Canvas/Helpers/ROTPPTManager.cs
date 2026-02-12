using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Timer = System.Timers.Timer;

namespace Ink_Canvas.Helpers
{
    public class ROTPPTManager : IPPTLinkManager
    {
        #region Events
        public event Action<object> SlideShowBegin;
        public event Action<object> SlideShowNextSlide;
        public event Action<object> SlideShowEnd;
        public event Action<object> PresentationOpen;
        public event Action<object> PresentationClose;
        public event Action<bool> PPTConnectionChanged;
        public event Action<bool> SlideShowStateChanged;
        #endregion

        #region Properties
        /// <summary>
        /// 当前 PowerPoint 应用程序实例（通过 ROT 获取）。
        /// </summary>
        public object PPTApplication => _pptApplication;

        public bool IsConnected
        {
            get
            {
                try
                {
                    if (_pptApplication == null) return false;
                    if (!Marshal.IsComObject(_pptApplication)) return false;

                    // 访问简单属性验证 COM 是否仍然有效
                    var _ = _pptApplication.Name;
                    return true;
                }
                catch (COMException comEx)
                {
                    var hr = (uint)comEx.HResult;
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
                    if (_pptApplication == null || !Marshal.IsComObject(_pptApplication)) return false;

                    slideShowWindows = _pptApplication.SlideShowWindows;
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
                    LogHelper.WriteLogToFile($"检查 ROT PPT 放映状态失败: {comEx.Message} (HR: 0x{hr:X8})", LogHelper.LogType.Warning);
                    return false;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"检查 ROT PPT 放映状态时发生意外错误: {ex}", LogHelper.LogType.Warning);
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

        public int SlidesCount { get; private set; }
        #endregion

        #region Private Fields
        private Microsoft.Office.Interop.PowerPoint.Application _pptApplication;
        private Presentation _currentPresentation;
        private Slides _currentSlides;
        private Slide _currentSlide;

        private Timer _connectionCheckTimer;
        private Timer _slideShowStateCheckTimer;

        private bool _isModuleUnloading;
        private bool _lastSlideShowState;
        private readonly object _lockObject = new object();
        private bool _disposed;
        #endregion

        #region Lifecycle
        public ROTPPTManager()
        {
            InitializeTimers();
        }

        private void InitializeTimers()
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
            if (_disposed) return;

            _connectionCheckTimer?.Start();
            _slideShowStateCheckTimer?.Start();
            LogHelper.WriteLogToFile("ROTPPTManager 监控已启动", LogHelper.LogType.Trace);
        }

        public void StopMonitoring()
        {
            _connectionCheckTimer?.Stop();
            _slideShowStateCheckTimer?.Stop();
            DisconnectFromPPT();
            LogHelper.WriteLogToFile("ROTPPTManager 监控已停止", LogHelper.LogType.Trace);
        }
        #endregion

        #region Connection Management
        private void OnConnectionCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_isModuleUnloading)
                {
                    CheckAndConnectToPPTViaRot();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 连接检查失败: {ex}", LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile($"ROTPPTManager 放映状态检查失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 使用 ROT 尝试连接到 PowerPoint。
        /// </summary>
        private void CheckAndConnectToPPTViaRot()
        {
            if (_isModuleUnloading) return;

            lock (_lockObject)
            {
                try
                {
                    if (_isModuleUnloading) return;

                    var pptApp = PPTROTConnectionHelper.TryConnectViaROT(IsSupportWPS);

                    if (pptApp != null && _pptApplication == null)
                    {
                        // 从未连接 -> 连接
                        ConnectToPPT(pptApp);
                    }
                    else if (pptApp == null && _pptApplication != null)
                    {
                        // 原来有，现在没有 -> 断开
                        DisconnectFromPPT();
                    }
                    else if (pptApp != null && _pptApplication != null)
                    {
                        // 已连接，检查是否切换到了另一份 PPT
                        if (!PPTROTConnectionHelper.AreComObjectsEqual(_pptApplication, pptApp))
                        {
                            DisconnectFromPPT();
                            ConnectToPPT(pptApp);
                        }
                        else
                        {
                            // 相同实例，释放多余引用
                            PPTROTConnectionHelper.SafeReleaseComObject(pptApp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"ROTPPTManager 连接检查异常: {ex}", LogHelper.LogType.Error);
                    if (_pptApplication != null)
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
                LogHelper.WriteLogToFile($"ROTPPTManager 检查 PPT 放映状态异常: {ex}", LogHelper.LogType.Error);
            }
        }

        private void ConnectToPPT(Microsoft.Office.Interop.PowerPoint.Application pptApp)
        {
            try
            {
                _pptApplication = pptApp;

                // 在 UI 线程上注册事件
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    try
                    {
                        _pptApplication.PresentationOpen += OnPresentationOpenInternal;
                        _pptApplication.PresentationClose += OnPresentationCloseInternal;
                        _pptApplication.SlideShowBegin += OnSlideShowBeginInternal;
                        _pptApplication.SlideShowNextSlide += OnSlideShowNextSlideInternal;
                        _pptApplication.SlideShowEnd += OnSlideShowEndInternal;

                        LogHelper.WriteLogToFile("ROTPPTManager 已注册 PPT 事件", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ROTPPTManager 注册 PPT 事件失败: {ex}", LogHelper.LogType.Error);
                        throw;
                    }
                }, DispatcherPriority.Normal);

                UpdateCurrentPresentationInfo();

                // 连接成功后暂停频繁的连接检查，由状态定时器维持
                _connectionCheckTimer?.Stop();

                PPTConnectionChanged?.Invoke(true);
                LogHelper.WriteLogToFile("ROTPPTManager 成功连接到 PPT 应用程序", LogHelper.LogType.Event);

                if (IsInSlideShow && _pptApplication.SlideShowWindows.Count > 0)
                {
                    OnSlideShowBeginInternal(_pptApplication.SlideShowWindows[1]);
                }
                else if (_currentPresentation != null)
                {
                    OnPresentationOpenInternal(_currentPresentation);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 连接 PPT 应用程序失败: {ex}", LogHelper.LogType.Error);
                _pptApplication = null;
            }
        }

        private void DisconnectFromPPT()
        {
            try
            {
                if (_pptApplication != null)
                {
                    try
                    {
                        if (Marshal.IsComObject(_pptApplication))
                        {
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                try
                                {
                                    _pptApplication.PresentationOpen -= OnPresentationOpenInternal;
                                    _pptApplication.PresentationClose -= OnPresentationCloseInternal;
                                    _pptApplication.SlideShowBegin -= OnSlideShowBeginInternal;
                                    _pptApplication.SlideShowNextSlide -= OnSlideShowNextSlideInternal;
                                    _pptApplication.SlideShowEnd -= OnSlideShowEndInternal;
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.WriteLogToFile($"ROTPPTManager 取消 PPT 事件注册异常: {ex}", LogHelper.LogType.Warning);
                                    LogHelper.WriteLogToFile(ex.ToString(), LogHelper.LogType.Trace);
                                }
                            }, DispatcherPriority.Normal);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ROTPPTManager 取消 PPT 事件注册失败: {ex}", LogHelper.LogType.Warning);
                    }

                    SafeReleaseComObject(_currentSlide);
                    SafeReleaseComObject(_currentSlides);
                    SafeReleaseComObject(_currentPresentation);

                    if (Marshal.IsComObject(_pptApplication))
                    {
                        try
                        {
                            Marshal.FinalReleaseComObject(_pptApplication);
                        }
                        catch
                        {
                            try
                            {
                                int refCount = Marshal.ReleaseComObject(_pptApplication);
                                while (refCount > 0)
                                {
                                    refCount = Marshal.ReleaseComObject(_pptApplication);
                                }
                            }
                            catch { }
                        }
                    }
                }

                _pptApplication = null;
                _currentPresentation = null;
                _currentSlides = null;
                _currentSlide = null;
                SlidesCount = 0;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _isModuleUnloading = true;
                _connectionCheckTimer?.Stop();
                _slideShowStateCheckTimer?.Stop();

                PPTConnectionChanged?.Invoke(false);

                LogHelper.WriteLogToFile("ROTPPTManager 已断开 PPT 连接", LogHelper.LogType.Event);

                // 一段时间后重新尝试连接
                System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        System.Threading.Thread.Sleep(2000);

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        System.Threading.Thread.Sleep(1000);

                        _isModuleUnloading = false;
                        _connectionCheckTimer?.Start();
                        _slideShowStateCheckTimer?.Start();

                        LogHelper.WriteLogToFile("ROTPPTManager 联动模块已重新进入监控状态", LogHelper.LogType.Trace);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"ROTPPTManager 重新进入监控状态失败: {ex}", LogHelper.LogType.Error);
                        _isModuleUnloading = false;
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 断开 PPT 连接失败: {ex}", LogHelper.LogType.Error);
                _isModuleUnloading = false;
            }
        }
        #endregion

        #region PPT Event Handlers
        private void OnPresentationOpenInternal(Presentation pres)
        {
            try
            {
                _currentPresentation = pres;
                _currentSlides = pres.Slides;
                _currentSlide = null;
                SlidesCount = _currentSlides?.Count ?? 0;

                PresentationOpen?.Invoke(pres);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 处理演示文稿打开事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnPresentationCloseInternal(Presentation pres)
        {
            try
            {
                PresentationClose?.Invoke(pres);

                if (_currentPresentation != null && pres != null && ReferenceEquals(_currentPresentation, pres))
                {
                    SafeReleaseComObject(_currentSlide);
                    SafeReleaseComObject(_currentSlides);
                    SafeReleaseComObject(_currentPresentation);

                    _currentPresentation = null;
                    _currentSlides = null;
                    _currentSlide = null;
                    SlidesCount = 0;
                }
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005 || hr == 0x800706BA || hr == 0x800706BE || hr == 0x80048010)
                {
                    // COM 对象已失效，静默忽略
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnSlideShowBeginInternal(SlideShowWindow wn)
        {
            try
            {
                if (wn?.Presentation != null)
                {
                    _currentPresentation = wn.Presentation;
                    _currentSlides = _currentPresentation.Slides;
                    SlidesCount = _currentSlides?.Count ?? 0;
                }

                SlideShowBegin?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 处理放映开始事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowNextSlideInternal(SlideShowWindow wn)
        {
            try
            {
                if (wn?.View != null)
                {
                    try
                    {
                        _currentSlide = wn.View.Slide;
                    }
                    catch
                    {
                        _currentSlide = null;
                    }
                }

                SlideShowNextSlide?.Invoke(wn);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 处理放映翻页事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnSlideShowEndInternal(Presentation pres)
        {
            try
            {
                SlideShowEnd?.Invoke(pres);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 处理放映结束事件失败: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion

        #region IPPTLinkManager Methods
        public bool TryStartSlideShow()
        {
            try
            {
                if (!IsConnected || _currentPresentation == null || _pptApplication == null) return false;
                if (!Marshal.IsComObject(_pptApplication) || !Marshal.IsComObject(_currentPresentation)) return false;

                _currentPresentation.SlideShowSettings.Run();
                return true;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ROTPPTManager 开始幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 开始幻灯片放映失败: {ex}", LogHelper.LogType.Error);
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
                if (!IsConnected || _pptApplication == null) return false;
                if (!Marshal.IsComObject(_pptApplication)) return false;

                slideShowWindows = _pptApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    int count = 0;
                    try
                    {
                        count = ssw.Count;
                    }
                    catch
                    {
                        count = 0;
                    }

                    for (int i = 1; i <= count; i++)
                    {
                        try
                        {
                            slideShowWindow = ssw[i];
                            if (slideShowWindow != null)
                            {
                                dynamic sswObj = slideShowWindow;
                                view = sswObj.View;
                                if (view != null)
                                {
                                    dynamic viewObj = view;
                                    viewObj.Exit();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"ROTPPTManager 结束第 {i} 个放映窗口失败: {ex}", LogHelper.LogType.Warning);
                        }
                        finally
                        {
                            SafeReleaseComObject(view);
                            SafeReleaseComObject(slideShowWindow);
                            view = null;
                            slideShowWindow = null;
                        }
                    }
                }

                return true;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ROTPPTManager 结束幻灯片放映失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 结束幻灯片放映失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public bool TryNavigateToSlide(int slideNumber)
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            try
            {
                if (!IsConnected || !IsInSlideShow || _pptApplication == null) return false;
                if (!Marshal.IsComObject(_pptApplication)) return false;

                slideShowWindows = _pptApplication.SlideShowWindows;
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
                            viewObj.GotoSlide(slideNumber);
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
                LogHelper.WriteLogToFile($"ROTPPTManager 跳转到第 {slideNumber} 页失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 跳转到第 {slideNumber} 页失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
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
                if (!IsConnected || !IsInSlideShow || _pptApplication == null) return false;
                if (!Marshal.IsComObject(_pptApplication)) return false;

                slideShowWindows = _pptApplication.SlideShowWindows;
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
                LogHelper.WriteLogToFile($"ROTPPTManager 切换到下一页失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 切换到下一页失败: {ex}", LogHelper.LogType.Error);
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
                if (!IsConnected || !IsInSlideShow || _pptApplication == null) return false;
                if (!Marshal.IsComObject(_pptApplication)) return false;

                slideShowWindows = _pptApplication.SlideShowWindows;
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
                LogHelper.WriteLogToFile($"ROTPPTManager 切换到上一页失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 切换到上一页失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public int GetCurrentSlideNumber()
        {
            object slideShowWindows = null;
            object slideShowWindow = null;
            object view = null;
            try
            {
                if (!IsConnected || _pptApplication == null) return 0;
                if (!Marshal.IsComObject(_pptApplication)) return 0;

                slideShowWindows = _pptApplication.SlideShowWindows;
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
                            return viewObj.CurrentShowPosition;
                        }
                    }
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
                LogHelper.WriteLogToFile($"ROTPPTManager 获取当前页码失败: {comEx.Message}", LogHelper.LogType.Error);
                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 获取当前页码失败: {ex}", LogHelper.LogType.Error);
                return 0;
            }
            finally
            {
                SafeReleaseComObject(view);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public string GetPresentationName()
        {
            try
            {
                var pres = GetCurrentActivePresentation() as Presentation;
                return pres?.Name ?? "";
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 获取演示文稿名称失败: {ex}", LogHelper.LogType.Error);
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
                LogHelper.WriteLogToFile($"ROTPPTManager 尝试显示幻灯片导航 - 连接状态: {IsConnected}, 放映状态: {IsInSlideShow}", LogHelper.LogType.Trace);

                if (!IsConnected || !IsInSlideShow || _pptApplication == null)
                {
                    LogHelper.WriteLogToFile("ROTPPTManager: PPT 未连接或未在放映状态", LogHelper.LogType.Warning);
                    return false;
                }

                if (!Marshal.IsComObject(_pptApplication))
                {
                    LogHelper.WriteLogToFile("ROTPPTManager: PPT 应用程序 COM 对象无效", LogHelper.LogType.Warning);
                    return false;
                }

                slideShowWindows = _pptApplication.SlideShowWindows;
                if (slideShowWindows != null)
                {
                    dynamic ssw = slideShowWindows;
                    slideShowWindow = ssw[1];
                    if (slideShowWindow == null)
                    {
                        LogHelper.WriteLogToFile("ROTPPTManager: 幻灯片放映窗口为空", LogHelper.LogType.Warning);
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
                            LogHelper.WriteLogToFile("ROTPPTManager: 成功显示幻灯片导航", LogHelper.LogType.Event);
                            return true;
                        }

                        LogHelper.WriteLogToFile("ROTPPTManager: SlideNavigation 对象为空，可能当前环境不支持", LogHelper.LogType.Warning);
                        return false;
                    }
                    catch (COMException comEx)
                    {
                        var hr = (uint)comEx.HResult;
                        if (hr == 0x80020006)
                        {
                            LogHelper.WriteLogToFile("ROTPPTManager: 当前 PPT 实例不支持 SlideNavigation 功能", LogHelper.LogType.Warning);
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
                LogHelper.WriteLogToFile($"ROTPPTManager 显示幻灯片导航失败: {comEx.Message}", LogHelper.LogType.Error);
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 显示幻灯片导航失败: {ex}", LogHelper.LogType.Error);
                return false;
            }
            finally
            {
                SafeReleaseComObject(slideNavigation);
                SafeReleaseComObject(slideShowWindow);
                SafeReleaseComObject(slideShowWindows);
            }
        }

        public object GetCurrentActivePresentation()
        {
            try
            {
                if (!IsConnected || _pptApplication == null) return null;
                if (!Marshal.IsComObject(_pptApplication)) return null;

                if (IsInSlideShow && _pptApplication.SlideShowWindows.Count > 0)
                {
                    try
                    {
                        var slideShowWindow = _pptApplication.SlideShowWindows[1];
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

                if (_pptApplication.ActiveWindow?.Presentation != null)
                {
                    return _pptApplication.ActiveWindow.Presentation;
                }

                return _currentPresentation;
            }
            catch (COMException comEx)
            {
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005)
                {
                    DisconnectFromPPT();
                }
                LogHelper.WriteLogToFile($"ROTPPTManager 获取当前演示文稿失败: {comEx.Message}", LogHelper.LogType.Error);
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 获取当前演示文稿失败: {ex}", LogHelper.LogType.Error);
                return null;
            }
        }
        #endregion

        #region Helpers
        private void UpdateCurrentPresentationInfo()
        {
            try
            {
                if (!IsConnected || _pptApplication == null) return;
                if (!Marshal.IsComObject(_pptApplication)) return;

                try
                {
                    _currentPresentation = _pptApplication.ActivePresentation;
                }
                catch
                {
                    _currentPresentation = null;
                }

                if (_currentPresentation != null)
                {
                    try
                    {
                        _currentSlides = _currentPresentation.Slides;
                        SlidesCount = _currentSlides?.Count ?? 0;
                    }
                    catch
                    {
                        _currentSlides = null;
                        SlidesCount = 0;
                    }
                }
                else
                {
                    _currentSlides = null;
                    SlidesCount = 0;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager 更新当前演示文稿信息失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 安全释放 COM 对象。
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
            catch
            {
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                StopMonitoring();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ROTPPTManager Dispose 异常: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion
    }
}

