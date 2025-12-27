using iNKORE.UI.WPF.Modern;
using Microsoft.Win32;
using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Ink_Canvas.Windows
{
    /// <summary>
    /// 最小化计时器窗口
    /// </summary>
    public partial class MinimizedTimerControl : UserControl
    {
        private TimerControl parentControl;
        private System.Timers.Timer updateTimer;

        public MinimizedTimerControl()
        {
            InitializeComponent();

            updateTimer = new System.Timers.Timer(100);
            updateTimer.Elapsed += UpdateTimer_Elapsed;
            updateTimer.Start();

            ApplyTheme();

            // 监听主题变化事件
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // 监听分辨率变化事件
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            Unloaded += MinimizedTimerControl_Unloaded;

            // 监听加载事件，设置UI大小
            Loaded += MinimizedTimerControl_Loaded;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // 分辨率变化时重新计算大小
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateMinimizedTimerControlSize();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            // DPI变化时重新计算大小
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateMinimizedTimerControlSize();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void MinimizedTimerControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 根据DPI和分辨率限制UI大小为屏幕的35%
            UpdateMinimizedTimerControlSize();

            // 监听DPI变化事件（通过主窗口）
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.DpiChanged += MainWindow_DpiChanged;
            }
        }

        /// <summary>
        /// 根据DPI和分辨率更新最小化计时器控件大小，限制在屏幕大小的35%
        /// </summary>
        private void UpdateMinimizedTimerControlSize()
        {
            try
            {
                // 获取主窗口
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow == null) return;

                // 获取DPI缩放因子
                var source = PresentationSource.FromVisual(mainWindow);
                double dpiScaleX = 1.0, dpiScaleY = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    var transform = source.CompositionTarget.TransformToDevice;
                    dpiScaleX = transform.M11;
                    dpiScaleY = transform.M22;
                }

                // 获取屏幕工作区域大小（考虑DPI）
                double screenWidth = SystemParameters.WorkArea.Width / dpiScaleX;
                double screenHeight = SystemParameters.WorkArea.Height / dpiScaleY;

                // 计算最大尺寸（屏幕的35%）
                double maxWidth = screenWidth * 0.35;
                double maxHeight = screenHeight * 0.35;

                // 保持宽高比（基于原始设计尺寸 600x200，宽高比约为 3:1）
                double aspectRatio = 600.0 / 200.0;
                double calculatedWidth = maxWidth;
                double calculatedHeight = calculatedWidth / aspectRatio;

                // 如果计算出的高度超过最大高度，则按高度计算
                if (calculatedHeight > maxHeight)
                {
                    calculatedHeight = maxHeight;
                    calculatedWidth = calculatedHeight * aspectRatio;
                }

                // 计算缩放比例（基于原始尺寸600x200）
                double originalWidth = 600.0;
                double originalHeight = 200.0;
                double scaleX = calculatedWidth / originalWidth;
                double scaleY = calculatedHeight / originalHeight;
                double scale = Math.Min(scaleX, scaleY); // 使用较小的缩放比例以保持比例

                // 查找父容器（MinimizedTimerContainer）
                var parent = this.Parent as FrameworkElement;
                if (parent == null)
                {
                    // 如果直接父元素不是FrameworkElement，尝试查找Border
                    var visualParent = System.Windows.Media.VisualTreeHelper.GetParent(this);
                    while (visualParent != null)
                    {
                        if (visualParent is FrameworkElement fe && fe.Name == "MinimizedTimerContainer")
                        {
                            parent = fe;
                            break;
                        }
                        visualParent = System.Windows.Media.VisualTreeHelper.GetParent(visualParent);
                    }
                }

                if (parent != null)
                {
                    // 设置父容器大小
                    parent.Width = calculatedWidth;
                    parent.Height = calculatedHeight;

                    // 更新数字显示（Path元素）的大小 - 原始尺寸72x72
                    double digitSize = 72.0 * scale;
                    UpdateElementSize("MinHour1Display", digitSize, digitSize);
                    UpdateElementSize("MinHour2Display", digitSize, digitSize);
                    UpdateElementSize("MinMinute1Display", digitSize, digitSize);
                    UpdateElementSize("MinMinute2Display", digitSize, digitSize);
                    UpdateElementSize("MinSecond1Display", digitSize, digitSize);
                    UpdateElementSize("MinSecond2Display", digitSize, digitSize);

                    // 更新冒号字体大小 - 原始尺寸48
                    double colonFontSize = 48.0 * scale;
                    UpdateTextBlockFontSize("MinColon1Display", colonFontSize);
                    UpdateTextBlockFontSize("MinColon2Display", colonFontSize);

                    // 更新关闭按钮大小 - 原始尺寸24x24
                    double closeButtonSize = 24.0 * scale;
                    UpdateElementSize("CloseButton", closeButtonSize, closeButtonSize);

                    // 更新关闭按钮字体大小 - 原始尺寸12
                    double closeButtonFontSize = 12.0 * scale;
                    var closeButton = this.FindName("CloseButton") as System.Windows.Controls.Button;
                    if (closeButton != null)
                    {
                        var textBlock = closeButton.Content as System.Windows.Controls.TextBlock;
                        if (textBlock != null)
                        {
                            textBlock.FontSize = closeButtonFontSize;
                        }
                    }

                    // 更新Margin - 原始Margin是12，需要按比例缩放
                    double marginSize = 12.0 * scale;
                    UpdateElementMargin("MinHour1Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinHour2Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinColon1Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinMinute1Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinMinute2Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinColon2Display", new Thickness(0, 0, marginSize, 0));
                    UpdateElementMargin("MinSecond1Display", new Thickness(0, 0, marginSize, 0));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新最小化计时器控件大小失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新元素的大小
        /// </summary>
        private void UpdateElementSize(string elementName, double width, double height)
        {
            var element = this.FindName(elementName) as FrameworkElement;
            if (element != null)
            {
                element.Width = width;
                element.Height = height;
            }
        }

        /// <summary>
        /// 更新TextBlock的字体大小
        /// </summary>
        private void UpdateTextBlockFontSize(string elementName, double fontSize)
        {
            var textBlock = this.FindName(elementName) as System.Windows.Controls.TextBlock;
            if (textBlock != null)
            {
                textBlock.FontSize = fontSize;
            }
        }

        /// <summary>
        /// 更新元素的Margin
        /// </summary>
        private void UpdateElementMargin(string elementName, Thickness margin)
        {
            var element = this.FindName(elementName) as FrameworkElement;
            if (element != null)
            {
                element.Margin = margin;
            }
        }

        private void MinimizedTimerControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // 取消订阅主题变化事件
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            // 取消订阅DPI变化事件
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.DpiChanged -= MainWindow_DpiChanged;
            }

            if (parentControl != null)
            {
                parentControl.TimerCompleted -= ParentControl_TimerCompleted;
            }

            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // 当主题变化时，重新应用主题
            Application.Current.Dispatcher.Invoke(() =>
            {
                RefreshTheme();
            });
        }

        /// <summary>
        /// 刷新主题
        /// </summary>
        public void RefreshTheme()
        {
            try
            {
                // 重新应用主题
                ApplyTheme();

                // 强制刷新UI
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"刷新最小化计时器窗口主题出错: {ex.Message}");
            }
        }

        public void SetParentControl(TimerControl parent)
        {
            if (parentControl != null)
            {
                parentControl.TimerCompleted -= ParentControl_TimerCompleted;
            }

            parentControl = parent;

            if (parentControl != null)
            {
                parentControl.TimerCompleted += ParentControl_TimerCompleted;
                UpdateTimeDisplay();
            }
        }

        private void UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (parentControl != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (this.Visibility != Visibility.Visible)
                    {
                        return;
                    }

                    if (ShouldHide())
                    {
                        this.Visibility = Visibility.Collapsed;
                        var parent = this.Parent as FrameworkElement;
                        if (parent != null)
                        {
                            parent.Visibility = Visibility.Collapsed;
                        }
                        return;
                    }

                    UpdateTimeDisplay();
                });
            }
        }

        private bool ShouldHide()
        {
            if (parentControl == null) return true;

            if (parentControl.IsFullscreenWindowOpen)
            {
                return true;
            }

            if (MainWindow.Settings.RandSettings?.EnableOvertimeCountUp == true)
            {
                if (parentControl.IsTimerRunning)
                {
                    return false;
                }

                var remainingTime = parentControl.GetRemainingTime();
                if (remainingTime.HasValue && remainingTime.Value.TotalSeconds < 0)
                {
                    return false;
                }

                return true;
            }
            else
            {
                return !parentControl.IsTimerRunning;
            }
        }

        private void UpdateTimeDisplay()
        {
            if (parentControl == null) return;

            var remainingTime = parentControl.GetRemainingTime();
            if (remainingTime.HasValue)
            {
                var timeSpan = remainingTime.Value;
                bool isOvertimeMode = timeSpan.TotalSeconds < 0;
                bool shouldShowRed = isOvertimeMode && MainWindow.Settings.RandSettings?.EnableOvertimeRedText == true;

                int hours, minutes, seconds;

                if (isOvertimeMode)
                {
                    var totalTimeSpan = parentControl.GetTotalTimeSpan();
                    if (totalTimeSpan.HasValue)
                    {
                        var elapsedTime = parentControl.GetElapsedTime();
                        if (elapsedTime.HasValue)
                        {
                            var overtimeSpan = elapsedTime.Value - totalTimeSpan.Value;
                            hours = (int)overtimeSpan.TotalHours;
                            minutes = overtimeSpan.Minutes;
                            seconds = overtimeSpan.Seconds;
                        }
                        else
                        {
                            hours = 0;
                            minutes = 0;
                            seconds = 0;
                        }
                    }
                    else
                    {
                        hours = 0;
                        minutes = 0;
                        seconds = 0;
                    }
                }
                else
                {
                    hours = (int)timeSpan.TotalHours;
                    minutes = timeSpan.Minutes;
                    seconds = timeSpan.Seconds;
                }

                SetDigitDisplay("MinHour1Display", Math.Abs(hours / 10) % 10, shouldShowRed);
                SetDigitDisplay("MinHour2Display", (hours % 10 + 10) % 10, shouldShowRed);

                SetDigitDisplay("MinMinute1Display", minutes / 10, shouldShowRed);
                SetDigitDisplay("MinMinute2Display", minutes % 10, shouldShowRed);

                SetDigitDisplay("MinSecond1Display", seconds / 10, shouldShowRed);
                SetDigitDisplay("MinSecond2Display", seconds % 10, shouldShowRed);

                SetColonDisplay(shouldShowRed);
            }
        }

        private void ParentControl_TimerCompleted(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Visibility = Visibility.Collapsed;
            });
        }

        private void SetDigitDisplay(string pathName, int digit, bool isRed = false)
        {
            var path = this.FindName(pathName) as Path;
            if (path != null)
            {
                string resourceKey = $"Digit{digit}";
                var geometry = this.FindResource(resourceKey) as Geometry;
                if (geometry != null)
                {
                    path.Data = geometry;
                }

                if (isRed)
                {
                    path.Fill = Brushes.Red;
                }
                else
                {
                    var defaultBrush = this.TryFindResource("NewTimerWindowDigitForeground") as Brush;
                    if (defaultBrush != null)
                    {
                        path.Fill = defaultBrush;
                    }
                    else
                    {
                        bool isLightTheme = IsLightTheme();
                        path.Fill = isLightTheme ? Brushes.Black : Brushes.White;
                    }
                }
            }
        }

        private void SetColonDisplay(bool isRed = false)
        {
            var colon1 = this.FindName("MinColon1Display") as TextBlock;
            var colon2 = this.FindName("MinColon2Display") as TextBlock;

            if (colon1 != null)
            {
                if (isRed)
                {
                    colon1.Foreground = Brushes.Red;
                }
                else
                {
                    var defaultBrush = this.TryFindResource("NewTimerWindowDigitForeground") as Brush;
                    if (defaultBrush != null)
                    {
                        colon1.Foreground = defaultBrush;
                    }
                    else
                    {
                        bool isLightTheme = IsLightTheme();
                        colon1.Foreground = isLightTheme ? Brushes.Black : Brushes.White;
                    }
                }
            }

            if (colon2 != null)
            {
                if (isRed)
                {
                    colon2.Foreground = Brushes.Red;
                }
                else
                {
                    var defaultBrush = this.TryFindResource("NewTimerWindowDigitForeground") as Brush;
                    if (defaultBrush != null)
                    {
                        colon2.Foreground = defaultBrush;
                    }
                    else
                    {
                        bool isLightTheme = IsLightTheme();
                        colon2.Foreground = isLightTheme ? Brushes.Black : Brushes.White;
                    }
                }
            }
        }

        private void ApplyTheme()
        {
            try
            {
                if (MainWindow.Settings != null)
                {
                    ApplyTheme(MainWindow.Settings);
                }
                else
                {
                    bool isLightTheme = IsLightTheme();
                    if (!isLightTheme)
                    {
                        SetDarkThemeBorder();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用主题时出错: {ex.Message}");
            }
        }

        private void ApplyTheme(Settings settings)
        {
            try
            {
                if (settings.Appearance.Theme == 0) // 浅色主题
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                }
                else if (settings.Appearance.Theme == 1) // 深色主题
                {
                    ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                    SetDarkThemeBorder();
                }
                else // 跟随系统主题
                {
                    bool isSystemLight = IsSystemThemeLight();
                    if (isSystemLight)
                    {
                        ThemeManager.SetRequestedTheme(this, ElementTheme.Light);
                    }
                    else
                    {
                        ThemeManager.SetRequestedTheme(this, ElementTheme.Dark);
                        SetDarkThemeBorder();
                    }
                }

                // 刷新数字和冒号显示的颜色
                if (parentControl != null)
                {
                    UpdateTimeDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"应用最小化计时器窗口主题出错: {ex.Message}");
            }
        }

        private bool IsSystemThemeLight()
        {
            var light = false;
            try
            {
                var registryKey = Microsoft.Win32.Registry.CurrentUser;
                var themeKey = registryKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (themeKey != null)
                {
                    var value = themeKey.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        light = (int)value == 1;
                    }
                    themeKey.Close();
                }
            }
            catch
            {
                // 如果读取注册表失败，默认为浅色主题
                light = true;
            }
            return light;
        }

        private bool IsLightTheme()
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var currentModeField = mainWindow.GetType().GetField("currentMode",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (currentModeField != null)
                    {
                        var currentMode = currentModeField.GetValue(mainWindow);
                        return currentMode?.ToString() == "Light";
                    }
                }
            }
            catch
            {
            }
            return true;
        }

        private void SetDarkThemeBorder()
        {
            try
            {
                var border = this.FindName("MainBorder") as Border;
                if (border != null)
                {
                    border.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                }
            }
            catch
            {
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (parentControl != null)
            {
                parentControl.StopTimer();
            }
            Visibility = Visibility.Collapsed;
        }

        private bool isDragging = false;
        private bool isDragStarted = false;
        private Point dragStartPoint;
        private Point containerStartPosition;
        private const double DragThreshold = 5.0; // 拖动阈值，像素

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // 双击：恢复主窗口
                if (parentControl != null)
                {
                    parentControl.UpdateActivityTime();

                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var timerContainer = mainWindow.FindName("TimerContainer") as FrameworkElement;
                        var minimizedContainer = mainWindow.FindName("MinimizedTimerContainer") as FrameworkElement;

                        if (timerContainer != null && minimizedContainer != null)
                        {
                            timerContainer.Visibility = Visibility.Visible;
                            minimizedContainer.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                e.Handled = true;
            }
            else if (e.ClickCount == 1)
            {
                // 单击：准备拖动或点击
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var minimizedContainer = mainWindow.FindName("MinimizedTimerContainer") as FrameworkElement;
                    if (minimizedContainer != null)
                    {
                        var point = e.GetPosition(minimizedContainer);
                        var mainWindowPoint = minimizedContainer.TransformToAncestor(mainWindow).Transform(point);

                        // 初始化拖动状态，但不立即开始拖动
                        isDragging = false;
                        isDragStarted = false;
                        dragStartPoint = mainWindowPoint;

                        var margin = minimizedContainer.Margin;
                        containerStartPosition = new Point(margin.Left, margin.Top);

                        if (double.IsNaN(containerStartPosition.X) || containerStartPosition.X < 0) containerStartPosition.X = 0;
                        if (double.IsNaN(containerStartPosition.Y) || containerStartPosition.Y < 0) containerStartPosition.Y = 0;

                        // 捕获鼠标并订阅事件，等待判断是拖动还是点击
                        minimizedContainer.CaptureMouse();
                        minimizedContainer.MouseMove += MinimizedContainer_MouseMove;
                        minimizedContainer.MouseLeftButtonUp += MinimizedContainer_MouseLeftButtonUp;
                        e.Handled = true;
                    }
                }
            }
        }

        private void MinimizedContainer_MouseMove(object sender, MouseEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            var minimizedContainer = mainWindow.FindName("MinimizedTimerContainer") as FrameworkElement;
            if (minimizedContainer == null) return;

            var currentPoint = e.GetPosition(mainWindow);
            var deltaX = currentPoint.X - dragStartPoint.X;
            var deltaY = currentPoint.Y - dragStartPoint.Y;
            var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // 如果移动距离超过阈值，开始拖动
            if (!isDragStarted && distance > DragThreshold)
            {
                isDragStarted = true;
                isDragging = true;
            }

            // 如果已经开始拖动，更新位置
            if (isDragging)
            {
                var timerContainer = mainWindow.FindName("TimerContainer") as FrameworkElement;

                var newX = containerStartPosition.X + deltaX;
                var newY = containerStartPosition.Y + deltaY;

                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;

                minimizedContainer.Margin = new Thickness(newX, newY, 0, 0);
                minimizedContainer.HorizontalAlignment = HorizontalAlignment.Left;
                minimizedContainer.VerticalAlignment = VerticalAlignment.Top;

                if (timerContainer != null)
                {
                    timerContainer.Margin = new Thickness(newX, newY, 0, 0);
                    timerContainer.HorizontalAlignment = HorizontalAlignment.Left;
                    timerContainer.VerticalAlignment = VerticalAlignment.Top;
                }
            }
        }

        private void MinimizedContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            var minimizedContainer = mainWindow.FindName("MinimizedTimerContainer") as FrameworkElement;
            if (minimizedContainer != null)
            {
                minimizedContainer.ReleaseMouseCapture();
                minimizedContainer.MouseMove -= MinimizedContainer_MouseMove;
                minimizedContainer.MouseLeftButtonUp -= MinimizedContainer_MouseLeftButtonUp;
            }

            // 如果没有开始拖动（移动距离小于阈值），则视为单击，恢复主窗口
            if (!isDragStarted)
            {
                if (parentControl != null)
                {
                    parentControl.UpdateActivityTime();

                    var timerContainer = mainWindow.FindName("TimerContainer") as FrameworkElement;
                    if (timerContainer != null && minimizedContainer != null)
                    {
                        timerContainer.Visibility = Visibility.Visible;
                        minimizedContainer.Visibility = Visibility.Collapsed;
                    }
                }
            }

            isDragging = false;
            isDragStarted = false;
        }

    }
}


