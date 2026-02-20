using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Win32 API Declarations
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
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out ForegroundWindowInfo.RECT lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_MINIMIZE = 0x20000000;
        private const uint GW_HWNDNEXT = 2;
        private const uint GW_HWNDPREV = 3;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        #endregion

        #region PPT Application Variables
        public static Microsoft.Office.Interop.PowerPoint.Application pptApplication;
        public static Presentation presentation;
        public static Slides slides;
        public static Slide slide;
        public static int slidescount;
        #endregion

        #region PPT State Management
        private bool isEnteredSlideShowEndEvent; 
        private bool isPresentationHaveBlackSpace;

        // 长按翻页相关字段
        private DispatcherTimer _longPressTimer;
        private bool _isLongPressNext = true; // true为下一页，false为上一页
        private const int LongPressDelay = 500; // 长按延迟时间（毫秒）
        private const int LongPressInterval = 50; // 长按翻页间隔（毫秒）

        // PowerPoint应用程序守护相关字段
        private DispatcherTimer _powerPointProcessMonitorTimer;
        private const int ProcessMonitorInterval = 1000; // 应用程序监控间隔（毫秒）

        // 上次播放位置相关字段
        private int _lastPlaybackPage = 0;
        private bool _shouldNavigateToLastPage = false;
        
        // 当前播放页码跟踪
        private int _currentSlideShowPosition = 0;
        private readonly object _slideSwitchLock = new object();
        private bool _isProcessingSlideSwitch = false;

        private DispatcherTimer _exitPPTModeAfterDisconnectTimer;
        private const int ExitPPTModeAfterDisconnectDelayMs = 1200; 
        #endregion

        #region PPT Managers
        private IPPTLinkManager _pptManager;
        private PPTInkManager _singlePPTInkManager;
        private PPTUIManager _pptUIManager;

        /// <summary>
        /// 获取PPT管理器实例
        /// </summary>
        public IPPTLinkManager PPTManager => _pptManager;
        #endregion

        #region PPT Manager Initialization
        /// <summary>
        /// 初始化并配置用于与 PowerPoint 交互的管理器与相关资源（选择 ROT 或 COM 链接、注册事件、创建单一演示文稿的墨迹管理器与 PPT UI 管理器）。 
        /// </summary>
        /// <remarks>
        /// 在初始化前会尝试清理并关闭任何现存的 PPT 管理器、监控定时器和 COM 互操作状态；初始化期间会根据设置启用/配置长按翻页定时器、墨迹自动保存和 UI 按钮选项。方法内部捕获并记录所有异常，不会向调用方抛出异常。
        /// </remarks>
        private void InitializePPTManagers()
        {
            try
            {
                // 初始化长按定时器
                InitializeLongPressTimer();

                // 完全清理旧模式
                try
                {
                    _pptManager?.StopMonitoring();
                    _pptManager?.Dispose();
                    _pptManager = null;
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"清理旧 PPT 管理器异常: {ex}", LogHelper.LogType.Warning);
                }

                try
                {
                    StopPowerPointProcessMonitoring();
                    _powerPointProcessMonitorTimer = null;
                    ClosePowerPointApplication();
                    ClearStaticInteropState();
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"清理 Interop 状态异常: {ex}", LogHelper.LogType.Warning);
                }

                // 根据设置选择 COM / ROT 架构
                if (Settings.PowerPointSettings.UseRotPptLink)
                {
                    _pptManager = new ROTPPTManager();
                }
                else
                {
                    _pptManager = new ComPPTLinkManager();
                }

                _pptManager.IsSupportWPS = Settings.PowerPointSettings.IsSupportWPS;

                // 注册事件
                _pptManager.PPTConnectionChanged += OnPPTConnectionChanged;
                _pptManager.SlideShowBegin += o => OnPPTSlideShowBegin(o as SlideShowWindow);
                _pptManager.SlideShowNextSlide += o => OnPPTSlideShowNextSlide(o as SlideShowWindow);
                _pptManager.SlideShowEnd += o => OnPPTSlideShowEnd(o as Presentation);
                _pptManager.PresentationOpen += o => OnPPTPresentationOpen(o as Presentation);
                _pptManager.PresentationClose += o => OnPPTPresentationClose(o as Presentation);
                _pptManager.SlideShowStateChanged += OnPPTSlideShowStateChanged;

                _singlePPTInkManager = new PPTInkManager();
                _singlePPTInkManager.IsAutoSaveEnabled = Settings.PowerPointSettings.IsAutoSaveStrokesInPowerPoint;
                _singlePPTInkManager.AutoSaveLocation = Settings.Automation.AutoSavedStrokesLocation;

                // 初始化UI管理器
                _pptUIManager = new PPTUIManager(this);
                _pptUIManager.ShowPPTButton = Settings.PowerPointSettings.ShowPPTButton;
                _pptUIManager.PPTButtonsDisplayOption = Settings.PowerPointSettings.PPTButtonsDisplayOption;
                _pptUIManager.PPTSButtonsOption = Settings.PowerPointSettings.PPTSButtonsOption;
                _pptUIManager.PPTBButtonsOption = Settings.PowerPointSettings.PPTBButtonsOption;
                _pptUIManager.PPTLSButtonPosition = Settings.PowerPointSettings.PPTLSButtonPosition;
                _pptUIManager.PPTRSButtonPosition = Settings.PowerPointSettings.PPTRSButtonPosition;
                _pptUIManager.PPTLBButtonPosition = Settings.PowerPointSettings.PPTLBButtonPosition;
                _pptUIManager.PPTRBButtonPosition = Settings.PowerPointSettings.PPTRBButtonPosition;
                _pptUIManager.EnablePPTButtonPageClickable = Settings.PowerPointSettings.EnablePPTButtonPageClickable;
                _pptUIManager.EnablePPTButtonLongPressPageTurn = Settings.PowerPointSettings.EnablePPTButtonLongPressPageTurn;

                LogHelper.WriteLogToFile("PPT管理器初始化完成", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PPT管理器初始化失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void StartPPTMonitoring()
        {
            if (Settings.PowerPointSettings.PowerPointSupport)
            {
                _pptManager?.StartMonitoring();
                LogHelper.WriteLogToFile("PPT监控已启动", LogHelper.LogType.Event);
            }
        }

        /// <summary>
        /// 停止与 PowerPoint 相关的监控并清理用于延迟退出 PPT 模式的定时器。
        /// </summary>
        /// <remarks>
        /// 对可能为 null 的监控器或定时器安全处理后调用停止操作，并将操作写入事件日志。
        /// </remarks>
        private void StopPPTMonitoring()
        {
            try
            {
                _exitPPTModeAfterDisconnectTimer?.Stop();
                _exitPPTModeAfterDisconnectTimer = null;
            }
            catch
            {
            }

            _pptManager?.StopMonitoring();
            LogHelper.WriteLogToFile("PPT监控已停止", LogHelper.LogType.Event);
        }

        #region PowerPoint Application Management
        /// <summary>
        /// 启动PowerPoint应用程序守护
        /// <summary>
        /// 在启用 PowerPoint 增强且未使用 ROT PPT 链接时，创建或确保存在 PowerPoint 应用实例并启动用于监控该应用的定时器；成功时记录启动事件，失败时记录错误。
        /// </summary>
        private void StartPowerPointProcessMonitoring()
        {
            try
            {
                if (!Settings.PowerPointSettings.EnablePowerPointEnhancement) return;
                if (Settings.PowerPointSettings.UseRotPptLink) return;

                // 创建PowerPoint应用程序实例
                CreatePowerPointApplication();

                // 启动应用程序监控定时器
                if (_powerPointProcessMonitorTimer == null)
                {
                    _powerPointProcessMonitorTimer = new DispatcherTimer();
                    _powerPointProcessMonitorTimer.Interval = TimeSpan.FromMilliseconds(ProcessMonitorInterval);
                    _powerPointProcessMonitorTimer.Tick += OnPowerPointApplicationMonitorTick;
                }
                _powerPointProcessMonitorTimer.Start();

                LogHelper.WriteLogToFile("PowerPoint应用程序守护已启动", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"启动PowerPoint应用程序守护失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 停止PowerPoint应用程序守护
        /// </summary>
        private void StopPowerPointProcessMonitoring()
        {
            try
            {
                // 停止应用程序监控定时器
                _powerPointProcessMonitorTimer?.Stop();

                // 关闭PowerPoint应用程序
                ClosePowerPointApplication();

                LogHelper.WriteLogToFile("PowerPoint应用程序守护已停止", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"停止PowerPoint应用程序守护失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 创建PowerPoint应用程序实例
        /// <summary>
        /// 创建并初始化一个不可见的 PowerPoint 应用程序实例，并在实例可用时将其注入到当前的 PPT 管理器以供后续使用。
        /// </summary>
        /// <remarks>
        /// 如果设置为使用 ROT 链接或当前已有有效的 PowerPoint 实例，则不会创建新实例。创建后将把应用设置为最小化且不可见，并在短暂延迟后尝试将该实例赋予 PPT 管理器（如果存在）。异常会被内部记录但不会向调用方抛出。
        /// </remarks>
        private void CreatePowerPointApplication()
        {
            try
            {
                if (Settings.PowerPointSettings.UseRotPptLink) return;
                // 如果应用程序已存在且有效，则不重复创建
                if (pptApplication != null && IsPowerPointApplicationValid())
                {
                    return;
                }

                // 创建新的PowerPoint应用程序实例
                pptApplication = new Microsoft.Office.Interop.PowerPoint.Application();

                // 设置为不可见，作为后台进程
                pptApplication.Visible = MsoTriState.msoFalse;

                // 设置应用程序属性
                pptApplication.WindowState = PpWindowState.ppWindowMinimized;

                // 直接设置PPTManager的PPTApplication属性，绕过COM注册问题
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // 直接设置PPTManager的PowerPoint应用程序实例
                            if (_pptManager != null)
                            {
                                // 使用反射或直接访问来设置PPTManager的PPTApplication
                                SetPPTManagerApplication(pptApplication);
                                LogHelper.WriteLogToFile("已直接设置PPTManager的PowerPoint应用程序实例", LogHelper.LogType.Event);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"设置PPTManager的PowerPoint应用程序实例失败: {ex}", LogHelper.LogType.Error);
                        }
                    });
                });

                LogHelper.WriteLogToFile("PowerPoint应用程序实例已创建", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"创建PowerPoint应用程序实例失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 设置PPTManager的PowerPoint应用程序实例
        /// <summary>
        /// 将指定的 PowerPoint 应用实例注入到当前 PPT 管理器以供其与 PowerPoint 交互。
        /// </summary>
        /// <param name="app">要注入的 PowerPoint 应用实例；也可以为 null，以便清除或重置管理器中的应用引用。</param>
        private void SetPPTManagerApplication(Microsoft.Office.Interop.PowerPoint.Application app)
        {
            try
            {
                if (_pptManager == null) return;
                if (Settings.PowerPointSettings.UseRotPptLink) return;

                // 使用反射调用PPTManager的ConnectToPPT方法
                var pptManagerType = _pptManager.GetType();
                var connectMethod = pptManagerType.GetMethod("ConnectToPPT",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (connectMethod != null)
                {
                    connectMethod.Invoke(_pptManager, new object[] { app });
                    LogHelper.WriteLogToFile("通过ConnectToPPT方法设置PowerPoint应用程序实例", LogHelper.LogType.Event);
                }
                else
                {
                    // 如果无法通过反射调用，尝试直接设置属性
                    var pptApplicationProperty = pptManagerType.GetProperty("PPTApplication",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (pptApplicationProperty != null && pptApplicationProperty.CanWrite)
                    {
                        pptApplicationProperty.SetValue(_pptManager, app);
                        LogHelper.WriteLogToFile("通过属性设置PPTManager的PowerPoint应用程序实例", LogHelper.LogType.Event);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("无法设置PPTManager的PowerPoint应用程序实例", LogHelper.LogType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"设置PPTManager的PowerPoint应用程序实例失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 检查PowerPoint应用程序是否有效
        /// </summary>
        private bool IsPowerPointApplicationValid()
        {
            try
            {
                if (pptApplication == null) return false;
                if (!Marshal.IsComObject(pptApplication)) return false;

                // 尝试访问一个简单的属性来验证连接是否有效
                var _ = pptApplication.Name;
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

        /// <summary>
        /// 关闭PowerPoint应用程序
        /// <summary>
        /// 关闭当前的 PowerPoint 应用程序实例，释放相关的 COM 资源并清理静态互操作状态。
        /// </summary>
        /// <remarks>
        /// 如果存在打开的演示文稿，会尝试逐个关闭，然后退出 PowerPoint 进程；无论成功与否都会清除静态互操作对象并记录操作结果或错误日志。
        /// </remarks>
        private void ClosePowerPointApplication()
        {
            try
            {
                if (pptApplication != null)
                {
                    // 关闭所有打开的演示文稿
                    if (pptApplication.Presentations.Count > 0)
                    {
                        for (int i = pptApplication.Presentations.Count; i >= 1; i--)
                        {
                            try
                            {
                                pptApplication.Presentations[i].Close();
                            }
                            catch { }
                        }
                    }

                    // 退出PowerPoint应用程序
                    pptApplication.Quit();

                    // 释放COM对象
                    Marshal.ReleaseComObject(pptApplication);
                    pptApplication = null;
                }

                ClearStaticInteropState();
                LogHelper.WriteLogToFile("PowerPoint应用程序已关闭", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"关闭PowerPoint应用程序失败: {ex}", LogHelper.LogType.Error);
                ClearStaticInteropState();
            }
        }

        /// <summary>
        /// 释放并清除与 PowerPoint COM 互操作相关的静态对象（presentation、slides、slide），并将 slidescount 重置为 0。
        /// </summary>
        /// <remarks>
        /// 若释放过程中发生异常，会记录一条警告日志并继续执行以确保程序状态被重置。
        /// </remarks>
        private void ClearStaticInteropState()
        {
            try
            {
                if (presentation != null)
                {
                    try { if (Marshal.IsComObject(presentation)) Marshal.ReleaseComObject(presentation); } catch { }
                    presentation = null;
                }
                if (slides != null)
                {
                    try { if (Marshal.IsComObject(slides)) Marshal.ReleaseComObject(slides); } catch { }
                    slides = null;
                }
                if (slide != null)
                {
                    try { if (Marshal.IsComObject(slide)) Marshal.ReleaseComObject(slide); } catch { }
                    slide = null;
                }
                slidescount = 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"ClearStaticInteropState 异常: {ex}", LogHelper.LogType.Warning);
            }
        }

        /// <summary>
        /// PowerPoint应用程序监控定时器事件
        /// <summary>
        /// 周期性检查并确保内部的 PowerPoint COM 应用实例处于有效状态；当增强功能被禁用时停止监控，若检测到应用无效则尝试重建实例，并在异常时记录错误日志。
        /// </summary>
        private void OnPowerPointApplicationMonitorTick(object sender, EventArgs e)
        {
            try
            {
                if (!Settings.PowerPointSettings.EnablePowerPointEnhancement)
                {
                    StopPowerPointProcessMonitoring();
                    return;
                }
                if (Settings.PowerPointSettings.UseRotPptLink) return;

                // 检查应用程序是否还在运行
                if (!IsPowerPointApplicationValid())
                {
                    LogHelper.WriteLogToFile("检测到PowerPoint应用程序已失效，重新创建", LogHelper.LogType.Event);
                    CreatePowerPointApplication();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PowerPoint应用程序监控异常: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion

        /// <summary>
        /// 释放并停止所有与 PowerPoint 交互的管理器、计时器和进程监控，清理 COM 互操作状态以恢复应用的非PPT资源使用状态。
        /// </summary>
        /// <remarks>
        /// 此方法停止并释放 PPT 管理器与单个演示文稿的墨迹管理器，停止长按与进程监控计时器，关闭 PowerPoint 应用并清除静态 COM 互操作对象；在内部捕获并记录所有异常，不会向外抛出异常。
        /// </remarks>
        private void DisposePPTManagers()
        {
            try
            {
                _pptManager?.StopMonitoring();
                _pptManager?.Dispose();
                _singlePPTInkManager?.Dispose();
                _longPressTimer?.Stop();
                _longPressTimer = null;
                _pptManager = null;
                _singlePPTInkManager = null;
                _pptUIManager = null;

                // 清理PowerPoint进程守护
                StopPowerPointProcessMonitoring();
                _powerPointProcessMonitorTimer = null;
                ClosePowerPointApplication();
                ClearStaticInteropState();

                LogHelper.WriteLogToFile("PPT管理器已释放", LogHelper.LogType.Event);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"释放PPT管理器失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 初始化长按定时器
        /// </summary>
        private void InitializeLongPressTimer()
        {
            _longPressTimer = new DispatcherTimer();
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressDelay);
            _longPressTimer.Tick += OnLongPressTimerTick;
        }

        /// <summary>
        /// 启动长按检测
        /// </summary>
        /// <param name="sender">触发事件的控件</param>
        /// <param name="isNext">是否为下一页按钮</param>
        private void StartLongPressDetection(object sender, bool isNext)
        {
            if (!Settings.PowerPointSettings.EnablePPTButtonLongPressPageTurn) return;

            _isLongPressNext = isNext;
            // 重置定时器间隔为初始延迟时间，确保每次长按检测都从正确的延迟开始
            _longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressDelay);
            _longPressTimer?.Start();
        }

        /// <summary>
        /// 停止长按检测
        /// </summary>
        private void StopLongPressDetection()
        {
            _longPressTimer?.Stop();
        }

        /// <summary>
        /// 长按定时器事件处理
        /// </summary>
        private void OnLongPressTimerTick(object sender, EventArgs e)
        {
            if (!Settings.PowerPointSettings.EnablePPTButtonLongPressPageTurn) return;

            _longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressInterval);

            // 执行翻页
            if (_isLongPressNext)
            {
                BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
            }
            else
            {
                BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
            }
        }
        #endregion

        #region New PPT Event Handlers
        /// <summary>
        /// 处理 PowerPoint 连接状态变化并据此更新 UI 与内部 PPT 状态。
        /// </summary>
        /// <param name="isConnected">`true` 表示已与 PowerPoint 建立连接，`false` 表示连接已断开。</param>
        /// <remarks>
        /// 当连接建立时，停止并清除任何待处理的“退出 PPT 模式”延迟计时器并记录事件；
        /// 当连接断开时，清除当前临时笔迹并启动一个短延迟计时器；若延迟后仍未连接，则更新幻灯片放映状态与侧边栏按钮、重置 PPT 相关状态变量，并触发手动的幻灯片放映结束处理以完成收尾工作。
        /// </remarks>
        private void OnPPTConnectionChanged(bool isConnected)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _pptUIManager?.UpdateConnectionStatus(isConnected);

                    if (isConnected)
                    {
                        _exitPPTModeAfterDisconnectTimer?.Stop();
                        _exitPPTModeAfterDisconnectTimer = null;
                        LogHelper.WriteLogToFile("PPT连接已建立", LogHelper.LogType.Event);
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("PPT连接已断开", LogHelper.LogType.Event);
                        _singlePPTInkManager?.ClearAllStrokes();
                        _exitPPTModeAfterDisconnectTimer?.Stop();
                        _exitPPTModeAfterDisconnectTimer = new DispatcherTimer
                        {
                            Interval = TimeSpan.FromMilliseconds(ExitPPTModeAfterDisconnectDelayMs)
                        };
                        _exitPPTModeAfterDisconnectTimer.Tick += (s, e) =>
                        {
                            _exitPPTModeAfterDisconnectTimer?.Stop();
                            _exitPPTModeAfterDisconnectTimer = null;
                            if (_pptManager?.IsConnected != true)
                            {
                                _pptUIManager?.UpdateSlideShowStatus(false);
                                _pptUIManager?.UpdateSidebarExitButtons(false);
                                ResetPPTStateVariables();
                                _ = HandleManualSlideShowEnd();
                            }
                        };
                        _exitPPTModeAfterDisconnectTimer.Start();
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理PPT连接状态变化失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnPPTPresentationOpen(Presentation pres)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 在初始化墨迹管理器之前，先清理画布上的所有墨迹
                    ClearStrokes(true);

                    // 清理备份历史记录，防止旧演示文稿的墨迹影响新演示文稿
                    if (TimeMachineHistories != null && TimeMachineHistories.Length > 0)
                    {
                        TimeMachineHistories[0] = null;
                    }

                    _singlePPTInkManager?.InitializePresentation(pres);

                    // 处理跳转到首页或上次播放页的逻辑
                    HandlePresentationOpenNavigation(pres);

                    // 检查隐藏幻灯片
                    if (Settings.PowerPointSettings.IsNotifyHiddenPage)
                    {
                        CheckAndNotifyHiddenSlides(pres);
                    }

                    // 检查自动播放设置
                    if (Settings.PowerPointSettings.IsNotifyAutoPlayPresentation)
                    {
                        CheckAndNotifyAutoPlaySettings(pres);
                    }

                    _pptUIManager?.UpdateConnectionStatus(true);

                    LogHelper.WriteLogToFile($"已打开新演示文稿: {pres.Name}，墨迹状态已清理", LogHelper.LogType.Event);
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理演示文稿打开事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void OnPPTPresentationClose(Presentation pres)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _singlePPTInkManager?.SaveAllStrokesToFile(pres);

                    _pptUIManager?.UpdateConnectionStatus(false);
                });
            }
            catch (COMException comEx)
            {
                // COM对象已失效，这是正常情况，完全静默处理
                var hr = (uint)comEx.HResult;
                if (hr == 0x8001010E || hr == 0x80004005 || hr == 0x800706BA || hr == 0x800706BE || hr == 0x80048010)
                {
                }
            }
            catch (Exception)
            {
            }
        }

        private void OnPPTSlideShowStateChanged(bool isInSlideShow)
        {
            try
            {
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 通知UI管理器放映状态变化
                    _pptUIManager?.OnSlideShowStateChanged(isInSlideShow);

                    if (!isInSlideShow)
                    {
                    }

                    // 检查主窗口可见性（用于仅PPT模式）
                    CheckMainWindowVisibility();
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理PPT放映状态变化失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private async void OnPPTSlideShowBegin(SlideShowWindow wn)
        {
            try
            {
                if (Settings.Automation.IsAutoFoldInPPTSlideShow)
                {
                    if (!isFloatingBarFolded)
                        FoldFloatingBar_MouseUp(new object(), null);
                }
                else
                {
                    if (isFloatingBarFolded)
                    {
                        await UnFoldFloatingBar(new object());
                    }
                }

                isStopInkReplay = true;

                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    Presentation activePresentation = null;
                    int currentSlide = 0;
                    int totalSlides = 0;

                    if (wn?.View != null && wn.Presentation != null)
                    {
                        activePresentation = wn.Presentation;
                        currentSlide = wn.View.CurrentShowPosition;
                        totalSlides = activePresentation.Slides.Count;
                        // 初始化当前播放页码跟踪
                        _currentSlideShowPosition = currentSlide;
                    }
                    else
                    {
                        activePresentation = _pptManager?.GetCurrentActivePresentation() as Presentation;
                        currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                        totalSlides = _pptManager?.SlidesCount ?? 0;
                        // 初始化当前播放页码跟踪
                        _currentSlideShowPosition = currentSlide;
                    }

                    if (activePresentation != null)
                    {
                        if (_singlePPTInkManager != null)
                        {
                            try
                            {
                                _singlePPTInkManager.InitializePresentation(activePresentation);
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }

                    // 处理跳转到首页或上次播放位置
                    if (Settings.PowerPointSettings.IsAlwaysGoToFirstPageOnReenter)
                    {
                        _pptManager?.TryNavigateToSlide(1);
                    }
                    else if (_shouldNavigateToLastPage && _lastPlaybackPage > 0)
                    {
                        _pptManager?.TryNavigateToSlide(_lastPlaybackPage);
                        _shouldNavigateToLastPage = false; // 重置标志位
                    }

                    // 更新UI状态
                    _pptUIManager?.UpdateSlideShowStatus(true, currentSlide, totalSlides);

                    // 设置浮动栏透明度和边距
                    _pptUIManager?.SetFloatingBarOpacity(Settings.Appearance.ViewboxFloatingBarOpacityInPPTValue);
                    _pptUIManager?.SetMainPanelMargin(new Thickness(10, 10, 10, 10));

                    // 显示侧边栏退出按钮
                    _pptUIManager?.UpdateSidebarExitButtons(true);

                    // 处理画板显示
                    if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow &&
                        !Settings.Automation.IsAutoFoldInPPTSlideShow &&
                        GridTransparencyFakeBackground.Background == Brushes.Transparent && !isFloatingBarFolded)
                    {
                        BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                    }

                    if (currentMode != 0)
                    {
                        ImageBlackboard_MouseUp(null, null);
                        BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                    }

                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;

                    // 在PPT模式下根据设置决定是否隐藏手势面板和手势按钮
                    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);

                    // 根据设置决定是否在PPT放映模式下显示手势按钮
                    if (Settings.PowerPointSettings.ShowGestureButtonInSlideShow)
                    {
                        // 如果启用了PPT放映模式显示手势按钮，则显示手势按钮
                        if (Settings.Gesture.IsEnableTwoFingerGesture)
                        {
                            CheckEnableTwoFingerGestureBtnVisibility(true);
                        }
                    }
                    else
                    {
                        // 如果禁用了PPT放映模式显示手势按钮，则隐藏手势按钮
                        EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                    }

                    if (Settings.PowerPointSettings.IsShowCanvasAtNewSlideShow &&
                        !Settings.Automation.IsAutoFoldInPPTSlideShow)
                    {
                        await Task.Delay(300);
                        // 先进入批注模式，这会显示调色盘
                        PenIcon_Click(null, null);
                        // 然后设置颜色
                        BtnColorRed_Click(null, null);
                        try
                        {
                            if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                            {
                                UpdateCurrentToolMode("pen");
                                SetFloatingBarHighlightPosition("pen");
                                if (Settings.Appearance.IsShowQuickColorPalette && QuickColorPalettePanel != null && QuickColorPaletteSingleRowPanel != null)
                                {
                                    // 根据显示模式选择显示哪个面板
                                    if (Settings.Appearance.QuickColorPaletteDisplayMode == 0)
                                    {
                                        // 单行显示模式
                                        QuickColorPalettePanel.Visibility = Visibility.Collapsed;
                                        QuickColorPaletteSingleRowPanel.Visibility = Visibility.Visible;
                                    }
                                    else
                                    {
                                        // 双行显示模式
                                        QuickColorPalettePanel.Visibility = Visibility.Visible;
                                        QuickColorPaletteSingleRowPanel.Visibility = Visibility.Collapsed;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"PPT进入批注模式后同步浮动栏高光状态失败: {ex.Message}", LogHelper.LogType.Error);
                        }
                    }

                    isEnteredSlideShowEndEvent = false;

                    // 加载当前页墨迹
                    LoadCurrentSlideInk(currentSlide);
                });

                if (!isFloatingBarFolded)
                {
                    new Thread(() =>
                    {
                        Thread.Sleep(100);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ViewboxFloatingBarMarginAnimation(60);
                        });
                    }).Start();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片放映开始事件失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理放映模式下的幻灯片切换：在切换时保存离开的幻灯片墨迹、清除画布并加载目标幻灯片的墨迹，同时更新幻灯片编号和内部位置状态。
        /// </summary>
        /// <param name="wn">触发事件的 PowerPoint 放映窗口（SlideShowWindow）；若为 null 或其 View/Presentation 不可用则不执行任何操作。</param>
        private void OnPPTSlideShowNextSlide(SlideShowWindow wn)
        {
            try
            {
                if (wn?.View == null || wn.Presentation == null) return;

                int currentSlide = wn.View.CurrentShowPosition;
                int totalSlides = wn.Presentation.Slides.Count;

                lock (_slideSwitchLock)
                {
                    if (currentSlide == _currentSlideShowPosition) return;
                    if (_isProcessingSlideSwitch) return;

                    _isProcessingSlideSwitch = true;
                    int prev = _currentSlideShowPosition;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (inkCanvas.Strokes.Count > 0 && prev > 0 && prev != currentSlide)
                                _singlePPTInkManager?.SaveCurrentSlideStrokes(prev, inkCanvas.Strokes);

                            ClearStrokes(true);
                            timeMachine.ClearStrokeHistory();

                            StrokeCollection newStrokes = _singlePPTInkManager?.LoadSlideStrokes(currentSlide);
                            if (newStrokes != null && newStrokes.Count > 0)
                                inkCanvas.Strokes.Add(newStrokes);

                            _singlePPTInkManager?.LockInkForSlide(currentSlide);
                            _pptUIManager?.UpdateCurrentSlideNumber(currentSlide, totalSlides);
                        }
                        finally
                        {
                            _currentSlideShowPosition = currentSlide;
                            _isProcessingSlideSwitch = false;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片切换事件失败: {ex}", LogHelper.LogType.Error);
                lock (_slideSwitchLock)
                {
                    _isProcessingSlideSwitch = false;
                }
            }
        }

        /// <summary>
        /// 处理 PowerPoint 幻灯片放映结束事件：保存当前幻灯片墨迹、恢复界面与主题、重置内部 PPT/墨迹状态并根据设置折叠或显示悬浮工具栏等 UI 元素。
        /// </summary>
        /// <param name="pres">触发放映结束的 PowerPoint 演示文稿对象，用于保存当前幻灯片墨迹并读取放映信息。</param>
        private async void OnPPTSlideShowEnd(Presentation pres)
        {
            try
            {
                if (Settings.Automation.IsAutoFoldAfterPPTSlideShow && !isFloatingBarFolded)
                {
                    FoldFloatingBar_MouseUp(new object(), null);
                }

                if (isEnteredSlideShowEndEvent) return;
                isEnteredSlideShowEndEvent = true;

                // 获取当前播放页码，优先使用跟踪的页码，否则尝试从PPT管理器获取
                int currentPage = _currentSlideShowPosition;
                if (currentPage <= 0)
                {
                    try
                    {
                        currentPage = _pptManager?.GetCurrentSlideNumber() ?? 0;
                    }
                    catch
                    {
                        // 如果无法获取，尝试从演示文稿的SlideShowWindow获取
                        try
                        {
                            if (pres.SlideShowWindow != null && pres.SlideShowWindow.View != null)
                            {
                                currentPage = pres.SlideShowWindow.View.CurrentShowPosition;
                            }
                        }
                        catch { }
                    }
                }

                // 先保存当前画布墨迹到当前页
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (currentPage > 0 && _singlePPTInkManager != null && inkCanvas?.Strokes != null)
                        _singlePPTInkManager.ForceSaveSlideStrokes(currentPage, inkCanvas.Strokes);
                });
                _singlePPTInkManager?.SaveAllStrokesToFile(pres, currentPage);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        isPresentationHaveBlackSpace = false;

                        // 恢复主题
                        if (BtnSwitchTheme.Content.ToString() == "深色")
                        {
                            BtnExit.Foreground = Brushes.Black;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        }

                        // 更新UI状态
                        _pptUIManager?.UpdateSlideShowStatus(false);
                        _pptUIManager?.UpdateSidebarExitButtons(false);
                        _pptUIManager?.SetMainPanelMargin(new Thickness(10, 10, 10, 55));
                        _pptUIManager?.SetFloatingBarOpacity(Settings.Appearance.ViewboxFloatingBarOpacityValue);

                        if (currentMode != 0)
                        {
                            CloseWhiteboardImmediately();
                            currentMode = 0;
                        }

                        ClearStrokes(true);
                        // 清空备份历史记录，防止退出白板时恢复已结束PPT的墨迹
                        // 注意：这里只清空索引0的备份，不影响白板页面的墨迹（索引1及以上）
                        TimeMachineHistories[0] = null;

                        // 重置墨迹管理器的锁定状态，防止下次放映时墨迹显示错误
                        ResetInkManagerLockState();

                        // 退出PPT模式时恢复手势面板和手势按钮的显示状态
                        if (Settings.Gesture.IsEnableTwoFingerGesture && ToggleSwitchEnableMultiTouchMode.IsOn)
                        {
                            // 根据手势设置决定是否显示手势面板和手势按钮
                            CheckEnableTwoFingerGestureBtnVisibility(true);
                        }
                        else
                        {
                            // 如果手势功能未启用，确保手势按钮保持隐藏
                            EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                        }

                        // 退出PPT模式时隐藏快捷调色盘
                        if (QuickColorPalettePanel != null)
                        {
                            QuickColorPalettePanel.Visibility = Visibility.Collapsed;
                        }
                        if (QuickColorPaletteSingleRowPanel != null)
                        {
                            QuickColorPaletteSingleRowPanel.Visibility = Visibility.Collapsed;
                        }

                        if (GridTransparencyFakeBackground.Background != Brushes.Transparent)
                            BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                        SetCurrentToolMode(InkCanvasEditingMode.None);
                        
                        UpdateCurrentToolMode("cursor");
                        SetFloatingBarHighlightPosition("cursor");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"处理幻灯片放映结束UI更新失败: {ex}", LogHelper.LogType.Error);
                    }
                });

                await Task.Delay(100);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (!isFloatingBarFolded)
                    {
                        PureViewboxFloatingBarMarginAnimationInDesktopMode();
                        if (Settings.Automation.IsAutoEnterAnnotationModeWhenExitFoldMode)
                        {
                            Task.Delay(350).ContinueWith(_ =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (!isFloatingBarFolded)
                                    {
                                        ViewboxFloatingBarMarginAnimation(-60);
                                    }
                                });
                            });
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理幻灯片放映结束事件失败: {ex}", LogHelper.LogType.Error);
            }
        }
        #endregion

        #region Helper Methods
        private void HandlePresentationOpenNavigation(Presentation pres)
        {
            try
            {
                if (Settings.PowerPointSettings.IsAlwaysGoToFirstPageOnReenter)
                {
                    _pptManager?.TryNavigateToSlide(1);
                }
                else if (Settings.PowerPointSettings.IsNotifyPreviousPage)
                {
                    ShowPreviousPageNotification(pres);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理演示文稿导航失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void ShowPreviousPageNotification(Presentation pres)
        {
            try
            {
                if (pres == null) return;

                var presentationPath = pres.FullName;
                var fileHash = GetFileHash(presentationPath);
                var folderName = pres.Name + "_" + pres.Slides.Count + "_" + fileHash;
                var folderPath = Path.Combine(Settings.Automation.AutoSavedStrokesLocation, "Auto Saved - Presentations", folderName);
                var positionFile = Path.Combine(folderPath, "Position");

                if (!File.Exists(positionFile)) return;

                if (int.TryParse(File.ReadAllText(positionFile), out var page) && page > 0)
                {
                    _lastPlaybackPage = page;
                    new YesOrNoNotificationWindow($"上次播放到了第 {page} 页, 是否立即跳转", () =>
                    {
                        try
                        {
                            var pptApp = _pptManager?.PPTApplication as Microsoft.Office.Interop.PowerPoint.Application;
                            if (pptApp != null)
                            {
                                if (pptApp.SlideShowWindows.Count >= 1)
                                {
                                    pres.SlideShowWindow.View.GotoSlide(page);
                                }
                                else
                                {
                                    pres.Windows[1].View.GotoSlide(page);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile($"跳转到第{page}页失败: {ex}", LogHelper.LogType.Error);
                        }
                    }).ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"显示上次播放页通知失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void CheckAndNotifyHiddenSlides(Presentation pres)
        {
            try
            {
                bool hasHiddenSlides = false;
                if (pres?.Slides != null)
                {
                    foreach (Slide slide in pres.Slides)
                    {
                        if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
                        {
                            hasHiddenSlides = true;
                            break;
                        }
                    }
                }

                if (hasHiddenSlides && !IsShowingRestoreHiddenSlidesWindow)
                {
                    IsShowingRestoreHiddenSlidesWindow = true;
                    new YesOrNoNotificationWindow("检测到此演示文档中包含隐藏的幻灯片，是否取消隐藏？",
                        () =>
                        {
                            try
                            {
                                if (pres?.Slides != null)
                                {
                                    foreach (Slide slide in pres.Slides)
                                    {
                                        if (slide.SlideShowTransition.Hidden == MsoTriState.msoTrue)
                                            slide.SlideShowTransition.Hidden = MsoTriState.msoFalse;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"取消隐藏幻灯片失败: {ex}", LogHelper.LogType.Error);
                            }
                            finally
                            {
                                IsShowingRestoreHiddenSlidesWindow = false;
                            }
                        },
                        () => { IsShowingRestoreHiddenSlidesWindow = false; },
                        () => { IsShowingRestoreHiddenSlidesWindow = false; }).ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查隐藏幻灯片失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void CheckAndNotifyAutoPlaySettings(Presentation pres)
        {
            try
            {
                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible) return;

                bool hasSlideTimings = false;
                if (pres?.Slides != null)
                {
                    foreach (Slide slide in pres.Slides)
                    {
                        if (slide.SlideShowTransition.AdvanceOnTime == MsoTriState.msoTrue &&
                            slide.SlideShowTransition.AdvanceTime > 0)
                        {
                            hasSlideTimings = true;
                            break;
                        }
                    }
                }

                if (hasSlideTimings && !IsShowingAutoplaySlidesWindow)
                {
                    IsShowingAutoplaySlidesWindow = true;
                    new YesOrNoNotificationWindow("检测到此演示文档中自动播放或排练计时已经启用，可能导致幻灯片自动翻页，是否取消？",
                        () =>
                        {
                            try
                            {
                                if (pres != null)
                                {
                                    pres.SlideShowSettings.AdvanceMode = PpSlideShowAdvanceMode.ppSlideShowManualAdvance;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogHelper.WriteLogToFile($"设置手动播放模式失败: {ex}", LogHelper.LogType.Error);
                            }
                            finally
                            {
                                IsShowingAutoplaySlidesWindow = false;
                            }
                        },
                        () => { IsShowingAutoplaySlidesWindow = false; },
                        () => { IsShowingAutoplaySlidesWindow = false; }).ShowDialog();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查自动播放设置失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void LoadCurrentSlideInk(int slideIndex)
        {
            try
            {
                ClearStrokes(true);
                timeMachine.ClearStrokeHistory();

                StrokeCollection strokes = _singlePPTInkManager?.LoadSlideStrokes(slideIndex);

                if (strokes != null && strokes.Count > 0)
                {
                    inkCanvas.Strokes.Add(strokes);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"加载当前页墨迹失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 重置墨迹管理器的锁定状态，防止墨迹显示错误
        /// </summary>
        private void ResetInkManagerLockState()
        {
            try
            {
                _singlePPTInkManager?.ResetLockState();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"重置墨迹管理器锁定状态失败: {ex}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 重置PPT相关的状态变量，当PPT自动收纳设置变更时调用
        /// <summary>
        /// 将与 PowerPoint 会话和放映相关的内部状态变量重置为默认值，恢复到未连接/未放映的初始状态。
        /// </summary>
        /// <remarks>
        /// 重置的字段包括：isEnteredSlideShowEndEvent、isPresentationHaveBlackSpace、_lastPlaybackPage、_shouldNavigateToLastPage、_currentSlideShowPosition 及用于幻灯片切换互斥的 _isProcessingSlideSwitch（在 _slideSwitchLock 保护下）。
        /// </remarks>
        public void ResetPPTStateVariables()
        {
            try
            {
                // 重置PPT放映结束事件标志
                isEnteredSlideShowEndEvent = false;

                // 重置演示文稿黑边状态
                isPresentationHaveBlackSpace = false;

                // 重置上次播放位置相关字段
                _lastPlaybackPage = 0;
                _shouldNavigateToLastPage = false;
                
                // 重置当前播放页码跟踪
                _currentSlideShowPosition = 0;
                lock (_slideSwitchLock)
                {
                    _isProcessingSlideSwitch = false;
                }


                LogHelper.WriteLogToFile("PPT状态变量已重置", LogHelper.LogType.Trace);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"重置PPT状态变量失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private string GetFileHash(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return "unknown";

                using (var md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(filePath));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 8);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"计算文件哈希值失败: {ex}", LogHelper.LogType.Error);
                return "error";
            }
        }
        #endregion

        /// <summary>
        /// 手动触发对 PowerPoint 的连接检查：确保 PPT 管理器已初始化，触发重载与监控，并在短延迟后将检查结果通过日志或提示呈现给用户。
        /// </summary>
        /// <param name="sender">事件的发送者（触发该点击事件的控件）。</param>
        /// <param name="e">与路由事件关联的事件数据。</param>
        private void BtnCheckPPT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 使用新的PPT管理器进行连接检查
                if (_pptManager == null)
                {
                    InitializePPTManagers();
                }

                _pptManager?.ReloadConnection();
                _pptManager?.StartMonitoring();

                Task.Delay(800).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_pptManager?.IsConnected == true)
                        {
                            LogHelper.WriteLogToFile("手动PPT连接检查成功", LogHelper.LogType.Event);
                        }
                        else
                        {
                            MessageBox.Show("未找到幻灯片");
                            LogHelper.WriteLogToFile("手动PPT连接检查失败", LogHelper.LogType.Warning);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"手动检查PPT应用程序失败: {ex}", LogHelper.LogType.Error);
                _pptUIManager?.UpdateConnectionStatus(false);
                MessageBox.Show("未找到幻灯片");
            }
        }

        private void ToggleSwitchPowerPointEnhancement_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.PowerPointSettings.EnablePowerPointEnhancement = ToggleSwitchPowerPointEnhancement.IsOn;

            if (Settings.PowerPointSettings.EnablePowerPointEnhancement)
            {
                Settings.PowerPointSettings.IsSupportWPS = false;
                ToggleSwitchSupportWPS.IsOn = false;

                // 更新PPT管理器的WPS支持设置
                if (_pptManager != null)
                {
                    _pptManager.IsSupportWPS = false;
                }
            }

            SaveSettingsToFile();

            // 启动或停止PowerPoint进程守护
            if (Settings.PowerPointSettings.EnablePowerPointEnhancement)
            {
                StartPowerPointProcessMonitoring();
            }
            else
            {
                StopPowerPointProcessMonitoring();
            }
        }

        private void ToggleSwitchSupportWPS_Toggled(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            Settings.PowerPointSettings.IsSupportWPS = ToggleSwitchSupportWPS.IsOn;

            if (Settings.PowerPointSettings.IsSupportWPS)
            {
                if (!Settings.PowerPointSettings.PowerPointSupport)
                {
                    Settings.PowerPointSettings.PowerPointSupport = true;
                    ToggleSwitchSupportPowerPoint.IsOn = true;
                    
                    // 启动PPT监控
                    if (_pptManager == null)
                    {
                        InitializePPTManagers();
                    }
                    StartPPTMonitoring();
                }

                if (Settings.PowerPointSettings.EnablePowerPointEnhancement)
                {
                    Settings.PowerPointSettings.EnablePowerPointEnhancement = false;
                    ToggleSwitchPowerPointEnhancement.IsOn = false;
                    StopPowerPointProcessMonitoring();
                }
            }

            // 更新PPT管理器的WPS支持设置
            if (_pptManager != null)
            {
                _pptManager.IsSupportWPS = Settings.PowerPointSettings.IsSupportWPS;
            }

            SaveSettingsToFile();
        }

        private static bool isWPSSupportOn => Settings.PowerPointSettings.IsSupportWPS;

        public static bool IsShowingRestoreHiddenSlidesWindow;
        private static bool IsShowingAutoplaySlidesWindow;

        private void BtnPPTSlidesUp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // 保存当前页墨迹
                    var currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                    if (currentSlide > 0)
                    {
                        _singlePPTInkManager?.SaveCurrentSlideStrokes(currentSlide, inkCanvas.Strokes);
                    }

                    // 保存截图（如果启用）
                    if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                        Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    {
                        var presentationName = _pptManager?.GetPresentationName() ?? "";
                        SaveScreenShot(true, $"{presentationName}/{currentSlide}");
                    }

                    // 执行翻页
                    if (_pptManager?.TryNavigatePrevious() == true)
                    {
                        // 翻页成功，等待事件处理墨迹切换
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("切换到上一页失败", LogHelper.LogType.Warning);
                        _pptUIManager?.UpdateConnectionStatus(false);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"PPT上一页操作异常: {ex}", LogHelper.LogType.Error);
                    _pptUIManager?.UpdateConnectionStatus(false);
                }
            });
        }

        private void BtnPPTSlidesDown_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // 保存当前页墨迹
                    var currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                    if (currentSlide > 0)
                    {
                        _singlePPTInkManager?.SaveCurrentSlideStrokes(currentSlide, inkCanvas.Strokes);
                    }

                    // 保存截图（如果启用）
                    if (inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber &&
                        Settings.PowerPointSettings.IsAutoSaveScreenShotInPowerPoint)
                    {
                        var presentationName = _pptManager?.GetPresentationName() ?? "";
                        SaveScreenShot(true, $"{presentationName}/{currentSlide}");
                    }

                    // 执行翻页
                    if (_pptManager?.TryNavigateNext() == true)
                    {
                        // 翻页成功，等待事件处理墨迹切换
                    }
                    else
                    {
                        LogHelper.WriteLogToFile("切换到下一页失败", LogHelper.LogType.Warning);
                        _pptUIManager?.UpdateConnectionStatus(false);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"PPT下一页操作异常: {ex}", LogHelper.LogType.Error);
                    _pptUIManager?.UpdateConnectionStatus(false);
                }
            });
        }

        private void PPTNavigationBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
            if (!Settings.PowerPointSettings.EnablePPTButtonPageClickable) return;
            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0.15;
            }
        }

        private void PPTNavigationBtn_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0;
            }
        }

        private async void PPTNavigationBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            if (sender == PPTLSPageButton)
            {
                PPTLSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPageButton)
            {
                PPTRSPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPageButton)
            {
                PPTLBPageButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPageButton)
            {
                PPTRBPageButtonFeedbackBorder.Opacity = 0;
            }

            if (!Settings.PowerPointSettings.EnablePPTButtonPageClickable) return;

            // 使用新的PPT管理器检查连接状态
            if (_pptManager?.IsConnected != true || _pptManager?.IsInSlideShow != true)
            {
                LogHelper.WriteLogToFile("PPT未连接或未在放映状态，无法执行页码点击操作", LogHelper.LogType.Warning);
                return;
            }

            try
            {
                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                CursorIcon_Click(null, null);

                // 使用新的PPT管理器显示导航
                if (_pptManager.TryShowSlideNavigation())
                {
                    LogHelper.WriteLogToFile("成功显示PPT幻灯片导航", LogHelper.LogType.Trace);
                }
                else
                {
                    LogHelper.WriteLogToFile("显示PPT幻灯片导航失败", LogHelper.LogType.Warning);
                }

                // 控制居中
                if (!isFloatingBarFolded)
                {
                    await Task.Delay(100);
                    ViewboxFloatingBarMarginAnimation(60);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"PPT翻页控件操作失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void BtnPPTSlideShow_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                try
                {
                    if (_pptManager?.TryStartSlideShow() != true)
                    {
                        LogHelper.WriteLogToFile("启动幻灯片放映失败", LogHelper.LogType.Warning);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"启动幻灯片放映异常: {ex}", LogHelper.LogType.Error);
                }
            }).Start();
        }

        private async void BtnPPTSlideShowEnd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 保存当前页墨迹
                var currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                if (currentSlide > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _singlePPTInkManager?.SaveCurrentSlideStrokes(currentSlide, inkCanvas.Strokes);
                        timeMachine.ClearStrokeHistory();
                    });
                }

                // 结束放映
                if (_pptManager?.TryEndSlideShow() == true)
                {
                    // 如果成功结束放映，等待OnPPTSlideShowEnd事件处理收纳状态恢复
                }
                else
                {
                    LogHelper.WriteLogToFile("结束幻灯片放映失败", LogHelper.LogType.Warning);

                    // 手动更新UI状态，防止事件未触发
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _pptUIManager?.UpdateSlideShowStatus(false);
                        _pptUIManager?.UpdateSidebarExitButtons(false);
                        LogHelper.WriteLogToFile("手动更新放映结束UI状态", LogHelper.LogType.Trace);
                    });

                    // 手动处理自动收纳，因为OnPPTSlideShowEnd事件可能未触发
                    await HandleManualSlideShowEnd();
                }

                HideSubPanels("cursor");
                SetCurrentToolMode(InkCanvasEditingMode.None);

                await Task.Delay(150);
                if (!isFloatingBarFolded)
                {
                    PureViewboxFloatingBarMarginAnimationInDesktopMode();
                    if (Settings.Automation.IsAutoEnterAnnotationModeWhenExitFoldMode)
                    {   
                        await Task.Delay(350);
                        if (!isFloatingBarFolded)
                        {
                            ViewboxFloatingBarMarginAnimation(-60);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"结束PPT放映操作异常: {ex}", LogHelper.LogType.Error);

                // 确保UI状态正确
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _pptUIManager?.UpdateSlideShowStatus(false);
                    _pptUIManager?.UpdateSidebarExitButtons(false);
                });

                // 异常情况下也手动处理自动收纳
                await HandleManualSlideShowEnd();

                await Task.Delay(150);
                if (!isFloatingBarFolded)
                {
                    PureViewboxFloatingBarMarginAnimationInDesktopMode();
                    if (Settings.Automation.IsAutoEnterAnnotationModeWhenExitFoldMode)
                    {
                        await Task.Delay(350);
                        if (!isFloatingBarFolded)
                        {
                            ViewboxFloatingBarMarginAnimation(-60);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 手动处理PPT放映结束时的自动收纳
        /// </summary>
        private async Task HandleManualSlideShowEnd()
        {
            try
            {
                if (Settings.Automation.IsAutoFoldAfterPPTSlideShow && !isFloatingBarFolded)
                {
                    FoldFloatingBar_MouseUp(new object(), null);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"手动处理PPT放映结束自动收纳失败: {ex}", LogHelper.LogType.Error);
            }
        }

        private void GridPPTControlPrevious_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
            if (sender == PPTLSPreviousButtonBorder)
            {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRSPreviousButtonBorder)
            {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0.15;
            }

            // 启动长按检测
            if (Settings.PowerPointSettings.EnablePPTButtonLongPressPageTurn)
            {
                StartLongPressDetection(sender, false);
            }
        }
        private void GridPPTControlPrevious_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSPreviousButtonBorder)
            {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPreviousButtonBorder)
            {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0;
            }

            // 停止长按检测
            StopLongPressDetection();
        }
        private void GridPPTControlPrevious_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            if (sender == PPTLSPreviousButtonBorder)
            {
                PPTLSPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSPreviousButtonBorder)
            {
                PPTRSPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBPreviousButtonBorder)
            {
                PPTLBPreviousButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBPreviousButtonBorder)
            {
                PPTRBPreviousButtonFeedbackBorder.Opacity = 0;
            }

            // 停止长按检测
            StopLongPressDetection();

            BtnPPTSlidesUp_Click(BtnPPTSlidesUp, null);
        }


        private void GridPPTControlNext_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lastBorderMouseDownObject = sender;
            if (sender == PPTLSNextButtonBorder)
            {
                PPTLSNextButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRSNextButtonBorder)
            {
                PPTRSNextButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0.15;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0.15;
            }

            // 启动长按检测
            if (Settings.PowerPointSettings.EnablePPTButtonLongPressPageTurn)
            {
                StartLongPressDetection(sender, true);
            }
        }
        private void GridPPTControlNext_MouseLeave(object sender, MouseEventArgs e)
        {
            lastBorderMouseDownObject = null;
            if (sender == PPTLSNextButtonBorder)
            {
                PPTLSNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSNextButtonBorder)
            {
                PPTRSNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0;
            }

            // 停止长按检测
            StopLongPressDetection();
        }
        private void GridPPTControlNext_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            if (sender == PPTLSNextButtonBorder)
            {
                PPTLSNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRSNextButtonBorder)
            {
                PPTRSNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTLBNextButtonBorder)
            {
                PPTLBNextButtonFeedbackBorder.Opacity = 0;
            }
            else if (sender == PPTRBNextButtonBorder)
            {
                PPTRBNextButtonFeedbackBorder.Opacity = 0;
            }

            // 停止长按检测
            StopLongPressDetection();

            BtnPPTSlidesDown_Click(BtnPPTSlidesDown, null);
        }

        private void ImagePPTControlEnd_MouseUp(object sender, MouseButtonEventArgs e)
        {
            BtnPPTSlideShowEnd_Click(BtnPPTSlideShowEnd, null);
        }
    }
}