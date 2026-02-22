using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Panel = System.Windows.Controls.Panel;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 当前工具模式
        /// </summary>
        private string _currentToolMode = "cursor";

        #region "手勢"按鈕

        /// <summary>
        /// 用於浮動工具欄的"手勢"按鈕和白板工具欄的"手勢"按鈕的點擊事件
        /// </summary>
        private void TwoFingerGestureBorder_MouseUp(object sender, RoutedEventArgs e)
        {
            if (TwoFingerGestureBorder.Visibility == Visibility.Visible)
            {
                AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            }
            else
            {
                HideSubPanels();
                AnimationsHelper.ShowWithSlideFromBottomAndFade(TwoFingerGestureBorder);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardTwoFingerGestureBorder);
            }
        }

        /// <summary>
        /// 用於更新浮動工具欄的"手勢"按鈕和白板工具欄的"手勢"按鈕的樣式（開啟和關閉狀態）
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnColorPrompt()
        {
            // 根据主题选择手势图标和颜色
            bool isDarkTheme = Settings.Appearance.Theme == 1 ||
                               (Settings.Appearance.Theme == 2 && !IsSystemThemeLight());
            bool isLightTheme = !isDarkTheme;
            string gestureIconPath = isLightTheme ? "/Resources/new-icons/gesture.png" : "/Resources/new-icons/gesture_white.png";

            // 根据主题设置白板模式下的颜色
            Color boardBgColor, boardIconColor, boardTextColor, boardBorderColor;
            if (isLightTheme)
            {
                // 浅色主题
                boardBgColor = Color.FromRgb(244, 244, 245);
                boardIconColor = Color.FromRgb(24, 24, 27);
                boardTextColor = Color.FromRgb(24, 24, 27);
                boardBorderColor = Color.FromRgb(161, 161, 170);
            }
            else
            {
                // 深色主题
                boardBgColor = Color.FromRgb(39, 39, 42);
                boardIconColor = Color.FromRgb(244, 244, 245);
                boardTextColor = Color.FromRgb(244, 244, 245);
                boardBorderColor = Color.FromRgb(113, 113, 122);
            }

            if (ToggleSwitchEnableMultiTouchMode.IsOn)
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 0.5;
                TwoFingerGestureSimpleStackPanel.IsHitTestVisible = false;
                EnableTwoFingerGestureBtn.Source =
                    new BitmapImage(new Uri(gestureIconPath, UriKind.Relative));

                BoardGesture.Background = new SolidColorBrush(boardBgColor);
                BoardGestureGeometry.Brush = new SolidColorBrush(boardIconColor);
                BoardGestureGeometry2.Brush = new SolidColorBrush(boardIconColor);
                BoardGestureLabel.Foreground = new SolidColorBrush(boardTextColor);
                BoardGesture.BorderBrush = new SolidColorBrush(boardBorderColor);
                BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.DisabledGestureIcon);
                BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z");
            }
            else
            {
                TwoFingerGestureSimpleStackPanel.Opacity = 1;
                TwoFingerGestureSimpleStackPanel.IsHitTestVisible = true;
                if (Settings.Gesture.IsEnableTwoFingerGesture)
                {
                    EnableTwoFingerGestureBtn.Source =
                        new BitmapImage(new Uri("/Resources/new-icons/gesture-enabled.png", UriKind.Relative));

                    BoardGesture.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    BoardGestureGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                    BoardGestureGeometry2.Brush = new SolidColorBrush(Colors.GhostWhite);
                    BoardGestureLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);
                    BoardGesture.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.EnabledGestureIcon);
                    BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z " + XamlGraphicsIconGeometries.EnabledGestureIconBadgeCheck);
                }
                else
                {
                    EnableTwoFingerGestureBtn.Source =
                        new BitmapImage(new Uri(gestureIconPath, UriKind.Relative));

                    BoardGesture.Background = new SolidColorBrush(boardBgColor);
                    BoardGestureGeometry.Brush = new SolidColorBrush(boardIconColor);
                    BoardGestureGeometry2.Brush = new SolidColorBrush(boardIconColor);
                    BoardGestureLabel.Foreground = new SolidColorBrush(boardTextColor);
                    BoardGesture.BorderBrush = new SolidColorBrush(boardBorderColor);
                    BoardGestureGeometry.Geometry = Geometry.Parse(XamlGraphicsIconGeometries.DisabledGestureIcon);
                    BoardGestureGeometry2.Geometry = Geometry.Parse("F0 M24,24z M0,0z");
                }
            }
        }

        /// <summary>
        /// 控制是否顯示浮動工具欄的"手勢"按鈕
        /// </summary>
        private void CheckEnableTwoFingerGestureBtnVisibility(bool isVisible)
        {
            // 在PPT放映模式下根据设置决定是否显示手势按钮
            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
            {
                // 如果启用了PPT放映模式显示手势按钮，且当前处于批注模式，则显示手势按钮
                if (Settings.PowerPointSettings.ShowGestureButtonInSlideShow && isVisible && inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    EnableTwoFingerGestureBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                }
                return;
            }

            if (currentMode == 0)
            {
                if (GridTransparencyFakeBackground.Background != Brushes.Transparent && isVisible)
                {
                }
                else
                {
                    EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
                    return;
                }
            }

            if (StackPanelCanvasControls.Visibility != Visibility.Visible
                || BorderFloatingBarMainControls.Visibility != Visibility.Visible)
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
            else if (isVisible)
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Visible;
            }
            else
            {
                EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            }
        }

        #endregion "手勢"按鈕

        #region 浮動工具欄的拖動實現

        /// <summary>
        /// 是否正在拖动浮动工具栏
        /// </summary>
        private bool isDragDropInEffect;
        /// <summary>
        /// 当前位置
        /// </summary>
        private Point pos;
        /// <summary>
        /// 按下鼠标时的位置
        /// </summary>
        private Point downPos;
        /// <summary>
        /// 用于记录上次在桌面时的坐标
        /// </summary>
        private Point pointDesktop = new Point(-1, -1);
        /// <summary>
        /// 用于记录上次在PPT中的坐标
        /// </summary>
        private Point pointPPT = new Point(-1, -1);

        /// <summary>
        /// 浮动工具栏移动事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在拖拽浮动工具栏时，根据当前鼠标位置移动该工具栏并更新最后记录的位置（桌面或 PPT 模式）。
        /// </summary>
        /// <param name="sender">事件源（通常为浮动工具栏相关元素）。</param>
        /// <param name="e">包含当前鼠标位置的鼠标事件参数，用于计算并应用新的工具栏位置。</param>
        private void SymbolIconEmoji_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragDropInEffect)
            {
                var xPos = e.GetPosition(null).X - pos.X + ViewboxFloatingBar.Margin.Left;
                var yPos = e.GetPosition(null).Y - pos.Y + ViewboxFloatingBar.Margin.Top;
                ViewboxFloatingBar.Margin = new Thickness(xPos, yPos, -2000, -200);

                pos = e.GetPosition(null);
                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    pointPPT = new Point(xPos, yPos);
                else
                    pointDesktop = new Point(xPos, yPos);
            }
        }

        /// <summary>
        /// 浮动工具栏鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 为浮动工具栏的拖拽操作做初始化：取消正在进行的边距动画、记录起始鼠标位置并显示拖拽指示器。
        /// </summary>
        /// <param name="sender">触发事件的对象，通常是浮动工具栏上的表情/图标控件。</param>
        /// <param name="e">鼠标按钮事件参数，用于获取当前鼠标位置。</param>
        private void SymbolIconEmoji_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isViewboxFloatingBarMarginAnimationRunning)
            {
                ViewboxFloatingBar.BeginAnimation(MarginProperty, null);
                isViewboxFloatingBarMarginAnimationRunning = false;
            }

            isDragDropInEffect = true;
            pos = e.GetPosition(null);
            downPos = e.GetPosition(null);
            GridForFloatingBarDraging.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 浮动工具栏鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理表情图标的鼠标抬起事件：结束拖拽状态、隐藏拖拽提示并在点击（非拖动）时切换主浮动工具栏的可见性，同时根据可见性更新双指手势按钮显示规则。
        /// </summary>
        /// <param name="e">鼠标按钮事件参数，用于判断是否为点击（与按下位置距离≤10像素）以决定是否切换浮动工具栏；可为 null。</param>
        internal void SymbolIconEmoji_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragDropInEffect = false;

            if (e is null || (Math.Abs(downPos.X - e.GetPosition(null).X) <= 10 &&
                              Math.Abs(downPos.Y - e.GetPosition(null).Y) <= 10))
            {
                if (BorderFloatingBarMainControls.Visibility == Visibility.Visible)
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Collapsed;
                    CheckEnableTwoFingerGestureBtnVisibility(false);
                }
                else
                {
                    BorderFloatingBarMainControls.Visibility = Visibility.Visible;
                    CheckEnableTwoFingerGestureBtnVisibility(true);
                }
            }

            GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
        }

        #endregion 浮動工具欄的拖動實現

        #region 隱藏子面板和按鈕背景高亮

        /// <summary>
        /// 隐藏形状绘制面板
        /// <summary>
        /// 隐藏并以滑动与淡出动画折叠绘制形状相关的边框面板。
        /// </summary>
        private void CollapseBorderDrawShape()
        {
            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
        }

        /// <summary>
        /// HideSubPanels的简化版，立即隐藏所有子面板，无动画效果
        /// <summary>
        /// 立即隐藏并折叠所有次级面板，使工具面板、调色板、设置面板、页面列表、图像选项、双指手势面板及绘图形状子面板等全部不可见。
        /// </summary>
        private void HideSubPanelsImmediately()
        {
            BorderTools.Visibility = Visibility.Collapsed;
            BoardBorderTools.Visibility = Visibility.Collapsed;
            PenPalette.Visibility = Visibility.Collapsed;
            BoardPenPalette.Visibility = Visibility.Collapsed;
            BoardEraserSizePanel.Visibility = Visibility.Collapsed;
            EraserSizePanel.Visibility = Visibility.Collapsed;
            BorderSettings.Visibility = Visibility.Collapsed;
            BoardBorderLeftPageListView.Visibility = Visibility.Collapsed;
            BoardBorderRightPageListView.Visibility = Visibility.Collapsed;
            BoardImageOptionsPanel.Visibility = Visibility.Collapsed;
            TwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            BoardTwoFingerGestureBorder.Visibility = Visibility.Collapsed;
            // 添加隐藏图形工具的二级菜单面板
            BorderDrawShape.Visibility = Visibility.Collapsed;
            BoardBorderDrawShape.Visibility = Visibility.Collapsed;

            if (LogicalTreeHelper.FindLogicalNode(this, "BackgroundPalette") is Border bgPalette)
            {
                bgPalette.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        ///     <para>
        ///         易嚴定真，這個多功能函數包括了以下的內容：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             隱藏浮動工具欄和白板模式下的"更多功能"面板
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的畫筆調色盤
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下的"清屏"按鈕（已作廢）
        ///         </item>
        ///         <item>
        ///             負責給Settings設置面板做隱藏動畫
        ///         </item>
        ///         <item>
        ///             隱藏白板模式下和浮動工具欄的"手勢"面板
        ///         </item>
        ///         <item>
        ///             當<c>ToggleSwitchDrawShapeBorderAutoHide</c>開啟時，會自動隱藏白板模式下和浮動工具欄的"形狀"面板
        ///         </item>
        ///         <item>
        ///             按需高亮指定的浮動工具欄和白板工具欄中的按鈕，通過param：<paramref name="mode"/> 來指定
        ///         </item>
        ///         <item>
        ///             將浮動工具欄自動居中，通過param：<paramref name="autoAlignCenter"/>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="mode">
        ///     <para>
        ///         按需高亮指定的浮動工具欄和白板工具欄中的按鈕，有下面幾種情況：
        ///     </para>
        ///     <list type="number">
        ///         <item>
        ///             當<c><paramref name="mode"/>==null</c>時，不會執行任何有關操作
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>!="clear"</c>時，會先取消高亮所有工具欄按鈕，然後根據下面的情況進行高亮處理
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="color" || <paramref name="mode"/>=="pen"</c>時，會高亮浮動工具欄和白板工具欄中的"批註"，"筆"按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraser"</c>時，會高亮白板工具欄中的"橡皮"和浮動工具欄中的"面積擦"按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="eraserByStrokes"</c>時，會高亮白板工具欄中的"橡皮"和浮動工具欄中的"墨跡擦"按鈕
        ///         </item>
        ///         <item>
        ///             當<c><paramref name="mode"/>=="select"</c>時，會高亮浮動工具欄和白板工具欄中的"選擇"，"套索選"按鈕
        ///         </item>
        ///     </list>
        /// </param>
        /// <param name="autoAlignCenter">
        ///     是否自動居中浮動工具欄
        /// </param>
        private async void HideSubPanels(string mode = null, bool autoAlignCenter = false)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(PenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
            AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);
            AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
            AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);

            // 隐藏背景设置面板
            if (LogicalTreeHelper.FindLogicalNode(this, "BackgroundPalette") is Border bgPalette)
            {
                AnimationsHelper.HideWithSlideAndFade(bgPalette);
            }

            if (BorderSettings.Visibility == Visibility.Visible)
            {
                // 设置蒙版为不可点击，并移除背景
                BorderSettingsMask.IsHitTestVisible = false;
                BorderSettingsMask.Background = null;
                var sb = new Storyboard();

                // 滑动动画
                var slideAnimation = new DoubleAnimation
                {
                    From = 0, // 滑动距离
                    To = BorderSettings.RenderTransform.Value.OffsetX - 490,
                    Duration = TimeSpan.FromSeconds(0.6),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTargetProperty(slideAnimation,
                    new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                sb.Children.Add(slideAnimation);

                sb.Completed += (s, _) =>
                {
                    BorderSettings.Visibility = Visibility.Collapsed;
                    isOpeningOrHidingSettingsPane = false;
                    if (isTemporarilyDisablingNoFocusMode)
                    {
                        isTemporarilyDisablingNoFocusMode = false;
                        ApplyNoFocusMode();
                    }
                };

                BorderSettings.RenderTransform = new TranslateTransform();

                isOpeningOrHidingSettingsPane = true;
                sb.Begin(BorderSettings);
            }

            AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
            AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
            AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn)
            {
                AnimationsHelper.HideWithSlideAndFade(BorderDrawShape);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderDrawShape);
            }

            if (mode != null)
            {
                if (mode != "clear")
                {
                    CursorIconGeometry.Brush = new SolidColorBrush(FloatBarForegroundColor);
                    CursorIconGeometry.Geometry = Geometry.Parse(GetCorrectIcon("cursor", false));
                    PenIconGeometry.Brush = new SolidColorBrush(FloatBarForegroundColor);
                    PenIconGeometry.Geometry = Geometry.Parse(GetCorrectIcon("pen", false));
                    StrokeEraserIconGeometry.Brush = new SolidColorBrush(FloatBarForegroundColor);
                    StrokeEraserIconGeometry.Geometry =
                        Geometry.Parse(GetCorrectIcon("eraserStroke", false));
                    CircleEraserIconGeometry.Brush = new SolidColorBrush(FloatBarForegroundColor);
                    CircleEraserIconGeometry.Geometry =
                        Geometry.Parse(GetCorrectIcon("eraserCircle", false));
                    LassoSelectIconGeometry.Brush = new SolidColorBrush(FloatBarForegroundColor);
                    LassoSelectIconGeometry.Geometry = Geometry.Parse(GetCorrectIcon("lassoSelect", false));

                    bool isDarkThemeForButtons = Settings.Appearance.Theme == 1 ||
                                                 (Settings.Appearance.Theme == 2 && !IsSystemThemeLight());
                    if (isDarkThemeForButtons)
                    {
                        BoardPen.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        BoardSelect.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        BoardEraser.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                        BoardSelectGeometry.Brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardPenGeometry.Brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardEraserGeometry.Brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardPenLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardSelectLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardEraserLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        BoardSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                        BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                        BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                    }
                    else
                    {
                        BoardPen.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                        BoardSelect.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                        BoardEraser.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                        BoardSelectGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardPenGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardEraserGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardPenLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardSelectLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardEraserLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                        BoardSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                        BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                        BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                    }

                    HideFloatingBarHighlight();
                }

                // 根据主题选择高光颜色
                Color highlightColor;
                bool isDarkTheme = Settings.Appearance.Theme == 1 ||
                                   (Settings.Appearance.Theme == 2 && !IsSystemThemeLight());

                if (isDarkTheme)
                {
                    highlightColor = Color.FromRgb(102, 204, 255); // #66ccff for dark theme
                }
                else
                {
                    highlightColor = Color.FromRgb(30, 58, 138); // Keep current color for light theme
                }

                switch (mode)
                {
                    case "pen":
                    case "color":
                        {
                            PenIconGeometry.Brush = new SolidColorBrush(highlightColor);
                            PenIconGeometry.Geometry = Geometry.Parse(GetCorrectIcon("pen", true));
                            BoardPen.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardPenGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardPenLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            SetFloatingBarHighlightPosition("pen");
                            break;
                        }
                    case "eraser":
                        {
                            CircleEraserIconGeometry.Brush = new SolidColorBrush(highlightColor);
                            CircleEraserIconGeometry.Geometry =
                                Geometry.Parse(GetCorrectIcon("eraserCircle", true));
                            BoardEraser.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraserGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardEraserLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            SetFloatingBarHighlightPosition("eraser");
                            break;
                        }
                    case "eraserByStrokes":
                        {
                            StrokeEraserIconGeometry.Brush = new SolidColorBrush(highlightColor);
                            StrokeEraserIconGeometry.Geometry =
                                Geometry.Parse(GetCorrectIcon("eraserStroke", true));
                            BoardEraser.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraser.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardEraserGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardEraserLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            SetFloatingBarHighlightPosition("eraserByStrokes");
                            break;
                        }
                    case "select":
                        {
                            LassoSelectIconGeometry.Brush = new SolidColorBrush(highlightColor);
                            LassoSelectIconGeometry.Geometry =
                                Geometry.Parse(GetCorrectIcon("lassoSelect", true));
                            BoardSelect.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardSelect.BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            BoardSelectGeometry.Brush = new SolidColorBrush(Colors.GhostWhite);
                            BoardSelectLabel.Foreground = new SolidColorBrush(Colors.GhostWhite);

                            SetFloatingBarHighlightPosition("select");
                            break;
                        }
                    case "cursor":
                        {
                            CursorIconGeometry.Brush = new SolidColorBrush(highlightColor);
                            CursorIconGeometry.Geometry =
                                Geometry.Parse(GetCorrectIcon("cursor", true));
                            bool isDarkThemeForCursor = Settings.Appearance.Theme == 1 ||
                                                        (Settings.Appearance.Theme == 2 && !IsSystemThemeLight());
                            if (isDarkThemeForCursor)
                            {
                                BoardPen.Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                                BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                                BoardPenGeometry.Brush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                                BoardPenLabel.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                            }
                            else
                            {
                                BoardPen.Background = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                                BoardPen.BorderBrush = new SolidColorBrush(Color.FromRgb(161, 161, 170));
                                BoardPenGeometry.Brush = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                                BoardPenLabel.Foreground = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                            }

                            SetFloatingBarHighlightPosition("cursor");
                            break;
                        }
                    case "shape":
                        {
                            // 对图形模式进行特殊处理，不修改按钮UI状态
                            // 只隐藏相关面板，但保持图形绘制模式
                            break;
                        }
                }


                if (autoAlignCenter) // 控制居中
                {
                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(60);
                    }
                    else if (Topmost) //非黑板
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(100, true);
                    }
                    else //黑板
                    {
                        await Task.Delay(50);
                        ViewboxFloatingBarMarginAnimation(60);
                    }
                }
            }

            await Task.Delay(150);
            isHidingSubPanelsWhenInking = false;
        }

        #endregion

        #region 撤銷重做按鈕

        /// <summary>
        /// 撤销按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在浮动工具栏的撤销图标上释放鼠标时触发撤销操作并收起所有子面板。
        /// </summary>
        /// <remarks>
        /// 仅当撤销按钮处于可用状态且事件源为撤销图标时才会执行撤销操作；该方法还会清除图标的背景高亮并隐藏子面板。
        /// </remarks>
        /// <param name="sender">触发事件的源对象（通常为撤销图标元素）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        internal void SymbolIconUndo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconUndo && lastBorderMouseDownObject != SymbolIconUndo) return;

            if (!BtnUndo.IsEnabled) return;
            BtnUndo_Click(BtnUndo, null);
            HideSubPanels();
        }

        /// <summary>
        /// 重做按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在用户松开重做图标时触发重做操作并收起相关子面板；在需要时还会重置图标的背景状态。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为重做图标或者其容器）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        internal void SymbolIconRedo_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (lastBorderMouseDownObject != sender) return;

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconRedo && lastBorderMouseDownObject != SymbolIconRedo) return;

            if (!BtnRedo.IsEnabled) return;
            BtnRedo_Click(BtnRedo, null);
            HideSubPanels();
        }

        #endregion

        #region 白板按鈕和退出白板模式按鈕

        /// <summary>
        /// 是否正在显示或隐藏黑板
        /// </summary>
        private bool isDisplayingOrHidingBlackboard;

        /// <summary>
        /// 白板按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在鼠标释放时在白板与屏幕(PPT/桌面)模式之间切换并同步相关 UI 状态与设置。
        /// </summary>
        /// <remarks>
        /// 当触发白板切换时，会隐藏或显示与工具面板、PPT 导航、时间/水印、手势设置和浮动工具栏相关的面板与指示，可能触发截图保存（按配置）并更新名言/水印文本。完成切换后会根据当前 InkCanvas 编辑模式更新工具模式与浮动工具栏高亮，并将应用主题设为深色。方法通过内部标志避免重复触发并在异步任务完成后重置该标志。
        /// </remarks>
        internal void ImageBlackboard_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == WhiteboardFloatingBarBtn && lastBorderMouseDownObject != WhiteboardFloatingBarBtn) return;

            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            if (isDisplayingOrHidingBlackboard) return;
            isDisplayingOrHidingBlackboard = true;

            UnFoldFloatingBar_MouseUp(null, null);

            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select) PenIcon_Click(null, null);

            if (currentMode == 0)
            {
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                //進入黑板

                /*
                if (Not_Enter_Blackboard_fir_Mouse_Click) {// BUG-Fixed_tmp：程序启动后直接进入白板会导致后续撤销功能、退出白板无法恢复墨迹
                    BtnColorRed_Click(BorderPenColorRed, null);
                    await Task.Delay(200);
                    SimulateMouseClick.SimulateMouseClickAtTopLeft();
                    await Task.Delay(10);
                    Not_Enter_Blackboard_fir_Mouse_Click = false;
                }
                */
                new Thread(() =>
                {
                    Thread.Sleep(100);
                    Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(60); });
                }).Start();

                HideSubPanels();

                if (GridTransparencyFakeBackground.Background == Brushes.Transparent)
                {
                    if (currentMode == 1)
                    {
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Collapsed;
                        AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);
                    }

                    BtnHideInkCanvas_Click(BtnHideInkCanvas, null);
                }

                if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动关闭多指书写、开启双指移动
                {
                    ToggleSwitchEnableTwoFingerTranslate.IsOn = true;
                    if (isInMultiTouchMode) ToggleSwitchEnableMultiTouchMode.IsOn = false;
                }

                if (Settings.Appearance.EnableTimeDisplayInWhiteboardMode)
                {
                    WaterMarkTime.Visibility = Visibility.Visible;
                    WaterMarkDate.Visibility = Visibility.Visible;
                }
                else
                {
                    WaterMarkTime.Visibility = Visibility.Collapsed;
                    WaterMarkDate.Visibility = Visibility.Collapsed;
                }

                if (Settings.Appearance.EnableChickenSoupInWhiteboardMode)
                {
                    BlackBoardWaterMark.Visibility = Visibility.Visible;
                }
                else
                {
                    BlackBoardWaterMark.Visibility = Visibility.Collapsed;
                }

                _ = UpdateChickenSoupTextAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        try
                        {
                            LogHelper.WriteLogToFile($"进入白板模式时更新名言失败: {t.Exception?.GetBaseException().Message}", LogHelper.LogType.Warning);
                        }
                        catch
                        {
                        }
                        if (Settings.Appearance.EnableChickenSoupInWhiteboardMode && Settings.Appearance.ChickenSoupSource != 3)
                        {
                            try
                            {
                                if (Settings.Appearance.ChickenSoupSource == 0)
                                {
                                    int randChickenSoupIndex = new Random().Next(ChickenSoup.OSUPlayerYuLu.Length);
                                    BlackBoardWaterMark.Text = ChickenSoup.OSUPlayerYuLu[randChickenSoupIndex];
                                }
                                else if (Settings.Appearance.ChickenSoupSource == 1)
                                {
                                    int randChickenSoupIndex = new Random().Next(ChickenSoup.MingYanJingJu.Length);
                                    BlackBoardWaterMark.Text = ChickenSoup.MingYanJingJu[randChickenSoupIndex];
                                }
                                else if (Settings.Appearance.ChickenSoupSource == 2)
                                {
                                    int randChickenSoupIndex = new Random().Next(ChickenSoup.GaoKaoPhrases.Length);
                                    BlackBoardWaterMark.Text = ChickenSoup.GaoKaoPhrases[randChickenSoupIndex];
                                }
                            }
                            catch
                            {
                                BlackBoardWaterMark.Visibility = Visibility.Collapsed;
                            }
                        }
                        else if (Settings.Appearance.EnableChickenSoupInWhiteboardMode && Settings.Appearance.ChickenSoupSource == 3)
                        {
                            BlackBoardWaterMark.Text = "一言功能不可用";
                        }
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());

                if (Settings.Canvas.UsingWhiteboard)
                {
                    ICCWaterMarkDark.Visibility = Visibility.Visible;
                    ICCWaterMarkWhite.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ICCWaterMarkWhite.Visibility = Visibility.Visible;
                    ICCWaterMarkDark.Visibility = Visibility.Collapsed;
                }

                ViewboxFloatingBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                //关闭黑板
                HideSubPanelsImmediately();

                // 只有在PPT放映模式下且页数有效时才显示翻页按钮
                if (StackPanelPPTControls.Visibility == Visibility.Visible &&
                    BtnPPTSlideShowEnd.Visibility == Visibility.Visible &&
                    PPTManager?.IsInSlideShow == true &&
                    PPTManager?.SlidesCount > 0)
                {
                    var dops = Settings.PowerPointSettings.PPTButtonsDisplayOption.ToString();
                    var dopsc = dops.ToCharArray();
                    if (dopsc[0] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(LeftBottomPanelForPPTNavigation);
                    if (dopsc[1] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(RightBottomPanelForPPTNavigation);
                    if (dopsc[2] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(LeftSidePanelForPPTNavigation);
                    if (dopsc[3] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(RightSidePanelForPPTNavigation);
                }
                else
                {
                    // 如果不在放映模式或页数无效，隐藏所有翻页按钮
                    LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                }

                // 使用PPT UI管理器来正确更新翻页按钮显示状态，确保遵循用户设置
                _pptUIManager?.UpdateNavigationPanelsVisibility();

                if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                    inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber) SaveScreenShot(true);

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Collapsed)
                    new Thread(() =>
                    {
                        Thread.Sleep(300);
                        Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(100, true); });
                    }).Start();
                else
                    new Thread(() =>
                    {
                        Thread.Sleep(300);
                        Application.Current.Dispatcher.Invoke(() => { ViewboxFloatingBarMarginAnimation(60); });
                    }).Start();

                if (System.Windows.Controls.Canvas.GetLeft(FloatingbarSelectionBG) != 28) PenIcon_Click(null, null);

                if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动启用多指书写
                    ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
                // 2024.5.2 need to be tested
                // if (!isInMultiTouchMode) ToggleSwitchEnableMultiTouchMode.IsOn = true;
                WaterMarkTime.Visibility = Visibility.Collapsed;
                WaterMarkDate.Visibility = Visibility.Collapsed;
                BlackBoardWaterMark.Visibility = Visibility.Collapsed;
                ICCWaterMarkDark.Visibility = Visibility.Collapsed;
                ICCWaterMarkWhite.Visibility = Visibility.Collapsed;

                // 新增：退出白板模式时恢复基础浮动栏的显示
                ViewboxFloatingBar.Visibility = Visibility.Visible;
            }

            BtnSwitch_Click(BtnSwitch, null);

            if (currentMode == 0)
            {
                // 根据当前编辑模式正确设置工具模式和高光位置
                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    UpdateCurrentToolMode("cursor");
                    SetFloatingBarHighlightPosition("cursor");
                }
                else if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    UpdateCurrentToolMode("pen");
                    SetFloatingBarHighlightPosition("pen");
                }
                else if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    UpdateCurrentToolMode("eraser");
                    SetFloatingBarHighlightPosition("eraser");
                }
                else if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByStroke)
                {
                    UpdateCurrentToolMode("eraserByStrokes");
                    SetFloatingBarHighlightPosition("eraserByStrokes");
                }
                else if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
                {
                    UpdateCurrentToolMode("select");
                    SetFloatingBarHighlightPosition("select");
                }
            }

            if (currentMode == 0 && inkCanvas.Strokes.Count == 0 && BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                CursorIcon_Click(null, null);

            BtnExit.Foreground = Brushes.White;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;

            new Thread(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => { isDisplayingOrHidingBlackboard = false; });
            }).Start();

            SwitchToDefaultPen(null, null);
            CheckColorTheme(true);
        }

        #endregion
        /// <summary>
        /// 光标图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到光标工具：如果当前不在屏幕模式则退出白板模式，否则隐藏注释画布并在幻灯片放映结束时稍后调整浮动工具栏位置。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为光标图标按钮）。</param>
        /// <param name="e">路由事件参数。</param>
        private async void SymbolIconCursor_Click(object sender, RoutedEventArgs e)
        {
            if (currentMode != 0)
            {
                ImageBlackboard_MouseUp(null, null);
            }
            else
            {
                BtnHideInkCanvas_Click(BtnHideInkCanvas, null);

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                {
                    await Task.Delay(100);
                    ViewboxFloatingBarMarginAnimation(60);
                }
            }
        }

        #region 清空畫布按鈕

        /// <summary>
        /// 清空画布按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理删除图标的鼠标抬起事件：删除已选笔迹，或在无选中笔迹时根据设置保存截图后清空所有笔迹。
        /// </summary>
        /// <remarks>
        /// 如果存在选中的笔迹，则移除这些笔迹并隐藏选区覆盖层；如果没有选中笔迹但画布上有笔迹且启用了自动保存且笔迹数量达到阈值，则先保存截图（在 PPT 演示模式下会包含幻灯片索引与演示名），随后清空画布的所有笔迹。
        /// </remarks>
        internal void SymbolIconDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconDelete && lastBorderMouseDownObject != SymbolIconDelete) return;

            if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                inkCanvas.Strokes.Remove(inkCanvas.GetSelectedStrokes());
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
            else if (inkCanvas.Strokes.Count > 0)
            {
                if (Settings.Automation.IsAutoSaveStrokesAtClear &&
                    inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
                {
                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    {
                        var currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                        var presentationName = _pptManager?.GetPresentationName() ?? "";
                        SaveScreenShot(true, $"{presentationName}/{currentSlide}_{DateTime.Now:HH-mm-ss}");
                    }
                    else
                        SaveScreenShot(true);
                }

                BtnClear_Click(null, null);
            }
        }

        #endregion

        /// <summary>
        /// 面积擦子面板的清空墨迹按钮事件处理
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">RoutedEventArgs</param>
        private void EraserPanelSymbolIconDelete_MouseUp(object sender, RoutedEventArgs e)
        {
            PenIcon_Click(null, null);
            SymbolIconDelete_MouseUp(null, null);
        }

        #region 主要的工具按鈕事件

        /// <summary>
        /// 浮动工具栏的"套索选"按钮事件，重定向到旧UI的BtnSelect_Click方法
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理选择图标的鼠标释放事件：切换到选择工具并更新相关状态与界面（隐藏子面板、更新工具模式缓存）。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为选择图标）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        internal void SymbolIconSelect_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == SymbolIconSelect && lastBorderMouseDownObject != SymbolIconSelect) return;

            BtnSelect_Click(null, null);

            // 更新模式缓存
            UpdateCurrentToolMode("select");

            HideSubPanels("select");

        }

        #endregion

        /// <summary>
        /// 浮动工具栏按钮鼠标按下反馈效果处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 为浮动工具栏上的按钮或调色板项提供按下时的视觉反馈并记录按下的对象。
        /// </summary>
        /// <param name="sender">触发事件的元素，通常为 Panel 或 Border。方法会根据元素类型和名称修改其 Background 并将其保存到 lastBorderMouseDownObject。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void FloatingBarToolBtnMouseDownFeedback_Panel(object sender, MouseButtonEventArgs e)
        {
            if (sender is Panel panel)
            {
                lastBorderMouseDownObject = sender;
                if (panel == SymbolIconDelete) panel.Background = new SolidColorBrush(Color.FromArgb(28, 127, 29, 29));
                else panel.Background = new SolidColorBrush(Color.FromArgb(28, 24, 24, 27));
            }
            else if (sender is Border border)
            {
                lastBorderMouseDownObject = sender;
                // 对于快捷调色板的颜色球，不改变背景颜色，只添加透明度效果
                if (border.Name?.StartsWith("QuickColor") == true)
                {
                    // 保存原始颜色并添加透明度
                    if (border.Background is SolidColorBrush originalColor)
                    {
                        border.Background = new SolidColorBrush(Color.FromArgb(180, originalColor.Color.R, originalColor.Color.G, originalColor.Color.B));
                    }
                }
                else
                {
                    border.Background = new SolidColorBrush(Color.FromArgb(28, 24, 24, 27));
                }
            }
        }

        /// <summary>
        /// 浮动工具栏按钮鼠标离开反馈效果处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在鼠标离开浮动工具栏按钮时重置对应面板或边框的悬停视觉状态。
        /// </summary>
        /// <param name="sender">触发事件的元素，通常为 Panel 或 Border；对快捷调色板颜色球（Name 以 "QuickColor" 开头）会恢复其原始背景色。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void FloatingBarToolBtnMouseLeaveFeedback_Panel(object sender, MouseEventArgs e)
        {
            if (sender is Panel panel)
            {
                lastBorderMouseDownObject = null;
                panel.Background = new SolidColorBrush(Colors.Transparent);
            }
            else if (sender is Border border)
            {
                lastBorderMouseDownObject = null;
                // 对于快捷调色板的颜色球，恢复原始颜色
                if (border.Name?.StartsWith("QuickColor") == true)
                {
                    // 根据颜色球名称恢复对应的颜色
                    switch (border.Name)
                    {
                        case "QuickColorWhite":
                        case "QuickColorWhiteSingle":
                            border.Background = new SolidColorBrush(Colors.White);
                            break;
                        case "QuickColorOrange":
                        case "QuickColorOrangeSingle":
                            border.Background = new SolidColorBrush(Color.FromRgb(251, 150, 80));
                            break;
                        case "QuickColorYellow":
                        case "QuickColorYellowSingle":
                            border.Background = new SolidColorBrush(Colors.Yellow);
                            break;
                        case "QuickColorBlack":
                        case "QuickColorBlackSingle":
                            border.Background = new SolidColorBrush(Colors.Black);
                            break;
                        case "QuickColorBlue":
                            border.Background = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                            break;
                        case "QuickColorRed":
                        case "QuickColorRedSingle":
                            border.Background = new SolidColorBrush(Colors.Red);
                            break;
                        case "QuickColorGreen":
                        case "QuickColorGreenSingle":
                            border.Background = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                            break;
                        case "QuickColorPurple":
                            border.Background = new SolidColorBrush(Color.FromRgb(147, 51, 234));
                            break;
                    }
                }
                else
                {
                    border.Background = new SolidColorBrush(Colors.Transparent);
                }
            }
        }

        /// <summary>
        /// 设置图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 隐藏所有子面板并触发设置面板的打开或切换；在设置面板正在显示或隐藏过程中不执行任何操作。
        /// </summary>
        private void SymbolIconSettings_Click(object sender, RoutedEventArgs e)
        {
            if (isOpeningOrHidingSettingsPane) return;
            HideSubPanels();
            BtnSettings_Click(null, null);

        }
        /// <summary>
        /// 截图图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 隐藏子面板后延迟并将当前屏幕截图保存到桌面。
        /// </summary>
        /// <remarks>
        /// 立即折叠或隐藏所有子面板，等待短暂延迟（用于让 UI 状态稳定），然后执行截图保存操作到用户桌面目录。
        /// </remarks>
        private async void SymbolIconScreenshot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideSubPanelsImmediately();
            await Task.Delay(50);
            SaveScreenShotToDesktop();
        }

        /// <summary>
        /// 倒计时计时器图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 显示或打开计时器窗口并根据当前 UI 风格与模式初始化计时器状态。
        /// </summary>
        /// <remarks>
        /// 隐藏相关工具面板；当使用新样式 UI 时重置并显示内嵌计时器容器并调整其尺寸，订阅其关闭事件以隐藏容器；否则创建并显示独立的倒计时窗口，并在特定模式下设置窗口置顶状态。
        /// </remarks>
        /// <param name="e">鼠标按钮事件参数</param>
        private void ImageCountdownTimer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            if (Settings.RandSettings?.UseNewStyleUI == true)
            {
                if (TimerContainer != null && TimerControl != null)
                {
                    // 每次打开计时器窗口时重置计时器
                    TimerControl.ResetTimerState();
                    
                    // 根据DPI缩放因子调整TimerContainer的尺寸
                    AdjustTimerContainerSize();
                    
                    TimerContainer.Visibility = Visibility.Visible;
                    if (MinimizedTimerContainer != null)
                    {
                        MinimizedTimerContainer.Visibility = Visibility.Collapsed;
                    }
                    TimerControl.CloseRequested += (s, args) =>
                    {
                        TimerContainer.Visibility = Visibility.Collapsed;
                        if (MinimizedTimerContainer != null)
                        {
                            MinimizedTimerContainer.Visibility = Visibility.Collapsed;
                        }
                    };
                }
            }
            else
            {
                if (currentMode == 1)
                {
                    Topmost = false;
                }

                var timerWindow = CountdownTimerWindow.CreateTimerWindow();
                timerWindow.Show();
                if (currentMode == 1)
                {
                    timerWindow.Topmost = true;
                }
            }
        }

        /// <summary>
        /// 操作指南窗口图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 打开操作指南窗口并隐藏工具栏、面板和图片选项面板。
        /// </summary>
        /// <param name="sender">触发该事件的源对象。</param>
        /// <param name="e">鼠标按钮事件参数，表示触发点击的鼠标按键信息。</param>
        private void OperatingGuideWindowIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            new OperatingGuideWindow().Show();
        }

        /// <summary>
        /// 随机点名图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在浮动工具栏可见时触发随机点名界面，先收起子面板再打开对应的点名窗口并尝试将其置顶。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void SymbolIconRand_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 如果控件被隐藏，不处理事件
            if (RandomDrawPanel.Visibility != Visibility.Visible) return;

            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            // 根据设置决定使用哪个点名窗口
            if (Settings.RandSettings.UseNewRollCallUI)
            {
                // 使用新点名UI - 随机抽模式
                new NewStyleRollCallWindow(Settings, false).ShowDialog();
            }
            else
            {
                // 使用默认的随机点名窗口
                var randWindow = new RandWindow(Settings);
                randWindow.Show();

                // 使用延迟确保窗口完全显示后再强制置顶
                randWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    // 强制激活窗口
                    randWindow.Activate();
                    randWindow.Focus();

                    // 设置置顶
                    randWindow.Topmost = true;

                    // 使用Win32 API强制置顶
                    var hwnd = new WindowInteropHelper(randWindow).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        const int WS_EX_TOPMOST = 0x00000008;
                        const int GWL_EXSTYLE = -20;
                        const int SWP_NOMOVE = 0x0002;
                        const int SWP_NOSIZE = 0x0001;
                        const int SWP_SHOWWINDOW = 0x0040;
                        const int SWP_NOOWNERZORDER = 0x0200;
                        var HWND_TOPMOST = new IntPtr(-1);

                        // 设置窗口样式为置顶
                        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOPMOST);

                        // 强制置顶
                        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                            SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_NOOWNERZORDER);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"强制置顶RandWindow失败: {ex.Message}", LogHelper.LogType.Error);
                }
            }), DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// 检查并更新橡皮擦类型标签的状态
        /// <summary>
        /// 根据当前设置的橡皮形状（Circle / Rectangle）更新橡皮选项卡在桌面与白板两处的视觉状态与样式。
        /// </summary>
        public void CheckEraserTypeTab()
        {
            if (Settings.Canvas.EraserShapeType == 0)
            {
                CircleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                CircleEraserTabButton.Opacity = 1;
                CircleEraserTabButtonText.FontWeight = FontWeights.Bold;
                CircleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                CircleEraserTabButtonText.FontSize = 9.5;
                CircleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                RectangleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                RectangleEraserTabButton.Opacity = 0.75;
                RectangleEraserTabButtonText.FontWeight = FontWeights.Normal;
                RectangleEraserTabButtonText.FontSize = 9;
                RectangleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                RectangleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;

                BoardCircleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                BoardCircleEraserTabButton.Opacity = 1;
                BoardCircleEraserTabButtonText.FontWeight = FontWeights.Bold;
                BoardCircleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardCircleEraserTabButtonText.FontSize = 9.5;
                BoardCircleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                BoardRectangleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardRectangleEraserTabButton.Opacity = 0.75;
                BoardRectangleEraserTabButtonText.FontWeight = FontWeights.Normal;
                BoardRectangleEraserTabButtonText.FontSize = 9;
                BoardRectangleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardRectangleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;
            }
            else
            {
                RectangleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                RectangleEraserTabButton.Opacity = 1;
                RectangleEraserTabButtonText.FontWeight = FontWeights.Bold;
                RectangleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                RectangleEraserTabButtonText.FontSize = 9.5;
                RectangleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                CircleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                CircleEraserTabButton.Opacity = 0.75;
                CircleEraserTabButtonText.FontWeight = FontWeights.Normal;
                CircleEraserTabButtonText.FontSize = 9;
                CircleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                CircleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;

                BoardRectangleEraserTabButton.Background = new SolidColorBrush(Color.FromArgb(85, 59, 130, 246));
                BoardRectangleEraserTabButton.Opacity = 1;
                BoardRectangleEraserTabButtonText.FontWeight = FontWeights.Bold;
                BoardRectangleEraserTabButtonText.Margin = new Thickness(2, 0.5, 0, 0);
                BoardRectangleEraserTabButtonText.FontSize = 9.5;
                BoardRectangleEraserTabButtonIndicator.Visibility = Visibility.Visible;
                BoardCircleEraserTabButton.Background = new SolidColorBrush(Colors.Transparent);
                BoardCircleEraserTabButton.Opacity = 0.75;
                BoardCircleEraserTabButtonText.FontWeight = FontWeights.Normal;
                BoardCircleEraserTabButtonText.FontSize = 9;
                BoardCircleEraserTabButtonText.Margin = new Thickness(2, 1, 0, 0);
                BoardCircleEraserTabButtonIndicator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 单次点名图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理“随机点名”按钮的鼠标抬起事件：收起相关面板并根据设置调用外部点名协议或弹出内部点名窗口（单次抽模式）。
        /// </summary>
        /// <param name="sender">事件源对象。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        /// <remarks>
        /// - 优先根据 Settings.RandSettings.DirectCallCiRand 通过指定的协议发起外部点名；调用失败时回退到内部点名窗口。 
        /// - 当不使用外部调用时，依据 Settings.RandSettings.UseNewRollCallUI 选择新样式或默认的内部点名窗口，并以模态方式显示（单次抽模式）。 
        /// - 调用前会收起与点名无关的工具面板（快捷面板、工具边栏、图片选项等）。
        /// </remarks>
        private void SymbolIconRandOne_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // 如果控件被隐藏，不处理事件
            if (SingleDrawPanel.Visibility != Visibility.Visible) return;

            LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            // 检查是否启用了外部点名功能
            if (Settings.RandSettings.DirectCallCiRand)
            {
                try
                {
                    string protocol = "";
                    switch (Settings.RandSettings.ExternalCallerType)
                    {
                        case 0: // ClassIsland点名
                            protocol = "classisland://plugins/IslandCaller/Simple/1";
                            break;
                        case 1: // SecRandom点名
                            protocol = "secrandom://direct_extraction";
                            break;
                        case 2: // NamePicker点名
                            protocol = "namepicker://";
                            break;
                        default:
                            protocol = "classisland://plugins/IslandCaller/Simple/1";
                            break;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = protocol,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("无法调用外部点名：" + ex.Message);

                    // 调用失败时回退到相应的点名窗口
                    if (Settings.RandSettings.UseNewRollCallUI)
                    {
                        new NewStyleRollCallWindow(Settings, true).ShowDialog(); // 单次抽模式
                    }
                    else
                    {
                        new RandWindow(Settings, true).ShowDialog();
                    }
                }
            }
            else
            {
                // 根据设置决定使用哪个点名窗口
                if (Settings.RandSettings.UseNewRollCallUI)
                {
                    // 使用新点名UI - 单次抽模式
                    new NewStyleRollCallWindow(Settings, true).ShowDialog();
                }
                else
                {
                    // 使用默认的随机点名窗口
                    new RandWindow(Settings, true).ShowDialog();
                }
            }
        }

        /// <summary>
        /// 墨迹重播按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 开始对当前画布（或已选笔画）进行墨迹重播，并在重播期间显示重播工具箱与控制状态。
        /// </summary>
        /// <remarks>
        /// 方法会准备专用的重播画布、隐藏/禁用部分交互元素，按笔画中的采样点逐步重建并显示笔迹，从而呈现书写过程的回放；支持暂停、停止与重启控制，重播结束后会恢复原有 UI 与交互状态。不会抛出异常到调用者（内部异常仅记录或忽略）。
        /// </remarks>
        /// <param name="e">触发重播的鼠标按钮事件参数。</param>
        private void GridInkReplayButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //if (lastBorderMouseDownObject != sender) return;

            AnimationsHelper.HideWithSlideAndFade(BorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            CollapseBorderDrawShape();

            InkCanvasForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.Visibility = Visibility.Hidden;
            InkCanvasGridForInkReplay.IsHitTestVisible = false;
            FloatingbarUIForInkReplay.Visibility = Visibility.Hidden;
            FloatingbarUIForInkReplay.IsHitTestVisible = false;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Hidden;
            BlackboardUIGridForInkReplay.IsHitTestVisible = false;

            AnimationsHelper.ShowWithFadeIn(BorderInkReplayToolBox);
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Colors.Transparent);
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;

            isStopInkReplay = false;
            isPauseInkReplay = false;
            isRestartInkReplay = false;
            inkReplaySpeed = 1;
            InkCanvasForInkReplay.Strokes.Clear();
            var strokes = inkCanvas.Strokes.Clone();
            if (inkCanvas.GetSelectedStrokes().Count != 0) strokes = inkCanvas.GetSelectedStrokes().Clone();
            int k = 1, i = 0;
            new Thread(() =>
            {
                isRestartInkReplay = true;
                while (isRestartInkReplay)
                {
                    isRestartInkReplay = false;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        InkCanvasForInkReplay.Strokes.Clear();
                    });
                    foreach (var stroke in strokes)
                    {

                        if (isRestartInkReplay) break;

                        var stylusPoints = new StylusPointCollection();
                        if (stroke.StylusPoints.Count == 629) //圆或椭圆
                        {
                            Stroke s = null;
                            foreach (var stylusPoint in stroke.StylusPoints)
                            {

                                if (isRestartInkReplay) break;

                                while (isPauseInkReplay)
                                {
                                    Thread.Sleep(10);
                                }

                                if (i++ >= 50)
                                {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone())
                                    {
                                        DrawingAttributes = stroke.DrawingAttributes
                                    };
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        }
                        else
                        {
                            Stroke s = null;
                            foreach (var stylusPoint in stroke.StylusPoints)
                            {

                                if (isRestartInkReplay) break;

                                while (isPauseInkReplay)
                                {
                                    Thread.Sleep(10);
                                }

                                if (i++ >= k)
                                {
                                    i = 0;
                                    Thread.Sleep((int)(10 / inkReplaySpeed));
                                    if (isStopInkReplay) return;
                                }

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    try
                                    {
                                        InkCanvasForInkReplay.Strokes.Remove(s);
                                    }
                                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                                    stylusPoints.Add(stylusPoint);
                                    s = new Stroke(stylusPoints.Clone())
                                    {
                                        DrawingAttributes = stroke.DrawingAttributes
                                    };
                                    InkCanvasForInkReplay.Strokes.Add(s);
                                });
                            }
                        }
                    }
                }

                Thread.Sleep(100);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                    InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                    InkCanvasGridForInkReplay.IsHitTestVisible = true;
                    AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                    FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                    FloatingbarUIForInkReplay.IsHitTestVisible = true;
                    BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                    BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.IsManipulationEnabled = true;

                    if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                    {
                        inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    }

                    ResetTouchStates();
                });
            }).Start();
        }

        /// <summary>
        /// 是否停止墨迹重播
        /// </summary>
        private bool isStopInkReplay;
        /// <summary>
        /// 是否暂停墨迹重播
        /// </summary>
        private bool isPauseInkReplay;
        /// <summary>
        /// 是否重新开始墨迹重播
        /// </summary>
        private bool isRestartInkReplay;
        /// <summary>
        /// 墨迹重播速度
        /// </summary>
        private double inkReplaySpeed = 1;

        /// <summary>
        /// 墨迹重播画布鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在回放画笔轨迹时响应画布的鼠标按下事件：双击时退出回放并恢复编辑交互与相关 UI 可见性。
        /// </summary>
        /// <param name="e">鼠标按钮事件参数，方法在双击（ClickCount == 2）时触发主要行为。</param>
        /// <remarks>
        /// 触发行为包括：隐藏回放用 InkCanvas、显示回放界面网格与回放工具栏、停止回放（isStopInkReplay = true），
        /// 并将主 inkCanvas 恢复为可交互状态（可命中测试、启用 Manipulation、必要时切换到 Ink 编辑模式），同时重置触摸相关状态。
        /// </remarks>
        private void InkCanvasForInkReplay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.IsManipulationEnabled = true;

                if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }

                ResetTouchStates();
            }
        }

        /// <summary>
        /// 墨迹重播播放/暂停按钮鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理回放播放/暂停控件的鼠标按下事件并设置按下时的背景样式。
        /// </summary>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void InkReplayPlayPauseBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        /// <summary>
        /// 墨迹重播播放/暂停按钮鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换墨迹重播的暂停/播放状态并更新面板上的状态文本与播放/暂停图标可见性。
        /// </summary>
        /// <param name="sender">事件发送者，通常为触发该事件的控件（如 InkReplayPlayPauseBorder）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void InkReplayPlayPauseBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayPlayPauseBorder.Background = new SolidColorBrush(Colors.Transparent);
            isPauseInkReplay = !isPauseInkReplay;
            InkReplayPanelStatusText.Text = isPauseInkReplay ? "已暂停！" : "正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = isPauseInkReplay ? Visibility.Visible : Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = !isPauseInkReplay ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 墨迹重播停止按钮鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理回放停止按钮的鼠标按下事件，并将按钮背景设为按下状态的颜色。
        /// </summary>
        /// <param name="sender">事件源，触发按下事件的元素。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void InkReplayStopButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        /// <summary>
        /// 墨迹重播停止按钮鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 停止墨迹回放并恢复回放相关的 UI 到可交互状态。
        /// </summary>
        /// <remarks>
        /// 隐藏回放用的 InkCanvas，显示并启用回放用的网格、浮动工具栏和黑板 UI，触发回放工具箱的淡出隐藏动画，并将内部标志标记为已停止回放（isStopInkReplay = true）。
        /// </remarks>
        /// <param name="e">鼠标按钮事件参数（作为事件触发来源，方法体内未使用）。</param>
        private void InkReplayStopButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayStopButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
            InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
            InkCanvasGridForInkReplay.IsHitTestVisible = true;
            FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
            FloatingbarUIForInkReplay.IsHitTestVisible = true;
            BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
            BlackboardUIGridForInkReplay.IsHitTestVisible = true;
            AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
            isStopInkReplay = true;
        }

        /// <summary>
        /// 墨迹重播重新开始按钮鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在按下回放按钮时将其背景置为按下态颜色。
        /// </summary>
        /// <param name="sender">触发事件的回放按钮边框元素。</param>
        /// <param name="e">鼠标按下事件参数。</param>
        private void InkReplayReplayButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        /// <summary>
        /// 墨迹重播重新开始按钮鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 开始或重启墨迹重放，并将界面切换为“正在重播”状态。
        /// </summary>
        /// <remarks>
        /// 将重放状态设置为重启并解除暂停，同时更新状态文本和播放/暂停图标以反映正在重播的 UI 状态。
        /// </remarks>
        private void InkReplayReplayButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplayReplayButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            isRestartInkReplay = true;
            isPauseInkReplay = false;
            InkReplayPanelStatusText.Text = "正在重播墨迹...";
            InkReplayPlayButtonImage.Visibility = Visibility.Collapsed;
            InkReplayPauseButtonImage.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 墨迹重播速度按钮鼠标按下事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在用户按下回放速度按钮时应用按下状态的背景色以提供视觉反馈。
        /// </summary>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void InkReplaySpeedButtonBorder_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = new SolidColorBrush(Color.FromArgb(34, 9, 9, 11));
        }

        /// <summary>
        /// 墨迹重播速度按钮鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在点击速度按钮时循环切换回放速度（0.5、1、2、4、8 倍）并更新显示文本为当前速度（例如 "2x"）。
        /// </summary>
        /// <param name="sender">触发事件的控件（速度按钮的边框）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void InkReplaySpeedButtonBorder_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            InkReplaySpeedButtonBorder.Background = new SolidColorBrush(Colors.Transparent);
            inkReplaySpeed = inkReplaySpeed == 0.5 ? 1 :
                inkReplaySpeed == 1 ? 2 :
                inkReplaySpeed == 2 ? 4 :
                inkReplaySpeed == 4 ? 8 : 0.5;
            InkReplaySpeedTextBlock.Text = inkReplaySpeed + "x";
        }

        /// <summary>
        /// 工具图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换工具面板（BorderTools 与 BoardBorderTools）的可见性；在显示时隐藏其他子面板并以滑动淡入动画展示，在隐藏时以滑动淡出动画隐藏；同时处理触发按钮的背景与内部状态重置。
        /// </summary>
        /// <param name="sender">触发事件的元素（可能为浮动工具栏按钮或面板内按钮）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void SymbolIconTools_MouseUp(object sender, MouseButtonEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == ToolsFloatingBarBtn && lastBorderMouseDownObject != ToolsFloatingBarBtn) return;

            if (BorderTools.Visibility == Visibility.Visible)
            {
                AnimationsHelper.HideWithSlideAndFade(BorderTools);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderTools);
            }
            else
            {
                HideSubPanels();
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BorderTools);
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardBorderTools);
            }

            if (sender == ToolsFloatingBarBtn)
            {
                lastBorderMouseDownObject = null;
            }
        }

        /// <summary>
        /// 浮动工具栏边距动画是否正在运行
        /// </summary>
        private bool isViewboxFloatingBarMarginAnimationRunning;

        /// <summary>
        /// 浮动工具栏边距动画处理
        /// </summary>
        /// <param name="MarginFromEdge">边缘边距</param>
        /// <summary>
        /// 计算并以动画方式更新浮动工具栏(ViewboxFloatingBar)的屏幕边距以重新定位工具栏。
        /// </summary>
        /// <param name="MarginFromEdge">与屏幕底部边缘的目标间距（像素）。特殊值 -60 用于隐藏/脱离显示处理。</param>
        /// <param name="PosXCaculatedWithTaskbarHeight">是否在垂直位置计算中考虑任务栏高度；为 true 则将任务栏高度计入 Y 坐标计算。</param>
        /// <remarks>
        /// 在白板模式（currentMode == 1）下不会执行任何定位或动画。方法会根据显示缩放、浮动栏实际/渲染宽度、快捷调色盘显示状态及上次记录的位置计算最终坐标，并通过动画更新浮动栏的 Margin 与可见性。该方法在 UI 线程上执行相关更新并包含短时延迟以完成动画流程。
        /// </remarks>
        public async void ViewboxFloatingBarMarginAnimation(int MarginFromEdge,
            bool PosXCaculatedWithTaskbarHeight = false)
        {
            // 新增：在白板模式下不执行浮动栏动画
            if (currentMode == 1)
            {
                return;
            }

            if (MarginFromEdge == 60) MarginFromEdge = 55;
            await Dispatcher.InvokeAsync(() =>
            {
                if (!Topmost)
                    MarginFromEdge = -60;
                else
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                // 仅计算Windows任务栏高度，不考虑其他程序对工作区的影响
                var toolbarHeight = ForegroundWindowInfo.GetTaskbarHeight(screen, dpiScaleY);

                // 使用更可靠的方法获取浮动栏宽度
                double baseWidth = ViewboxFloatingBar.ActualWidth;

                // 如果ActualWidth为0，尝试使用DesiredSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.DesiredSize.Width;
                }

                // 如果仍然为0，使用RenderSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.RenderSize.Width;
                }

                // 如果所有方法都失败，使用一个基于内容的估算值
                if (baseWidth <= 0)
                {
                    // 根据浮动栏内容估算宽度
                    baseWidth = 200; // 最小宽度
                    LogHelper.WriteLogToFile($"浮动栏宽度无法获取，使用估算值: {baseWidth}");
                }

                double floatingBarWidth = baseWidth * ViewboxFloatingBarScaleTransform.ScaleX;


                // 如果快捷调色盘显示，确保有足够空间
                if ((QuickColorPalettePanel != null && QuickColorPalettePanel.Visibility == Visibility.Visible) ||
                    (QuickColorPaletteSingleRowPanel != null && QuickColorPaletteSingleRowPanel.Visibility == Visibility.Visible))
                {
                    // 根据显示模式调整宽度
                    if (Settings.Appearance.QuickColorPaletteDisplayMode == 0)
                    {
                        // 单行显示模式，自适应宽度，但需要足够空间显示6个颜色
                        floatingBarWidth = Math.Max(floatingBarWidth, 120 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                    else
                    {
                        // 双行显示模式，宽度较大
                        floatingBarWidth = Math.Max(floatingBarWidth, 68 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                }

                pos.X = (screenWidth - floatingBarWidth) / 2;

                if (!PosXCaculatedWithTaskbarHeight)
                {
                    // 如果任务栏高度为0(隐藏状态),则使用固定边距
                    if (toolbarHeight == 0)
                    {
                        pos.Y = screenHeight - MarginFromEdge * ViewboxFloatingBarScaleTransform.ScaleY;
                    }
                    else
                    {
                        pos.Y = screenHeight - MarginFromEdge * ViewboxFloatingBarScaleTransform.ScaleY;
                    }
                }
                else if (PosXCaculatedWithTaskbarHeight)
                {
                    // 如果任务栏高度为0(隐藏状态),则使用固定高度
                    if (toolbarHeight == 0)
                    {
                        pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                               3 * ViewboxFloatingBarScaleTransform.ScaleY;
                    }
                    else
                    {
                        pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                               toolbarHeight - ViewboxFloatingBarScaleTransform.ScaleY * 3;
                    }
                }

                if (MarginFromEdge != -60)
                {
                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    {
                        if (pointPPT.X != -1 || pointPPT.Y != -1)
                        {
                            if (Math.Abs(pointPPT.Y - pos.Y) > 50)
                                pos = pointPPT;
                            else
                                pointPPT = pos;
                        }
                    }
                    else
                    {
                        if (pointDesktop.X != -1 || pointDesktop.Y != -1)
                        {
                            if (Math.Abs(pointDesktop.Y - pos.Y) > 50)
                                pos = pointDesktop;
                            else
                                pointDesktop = pos;
                        }
                    }
                }

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20),
                    EasingFunction = new CircleEase()
                };
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(200);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
                if (!Topmost) ViewboxFloatingBar.Visibility = Visibility.Hidden;
            });
        }

        /// <summary>
        /// 桌面模式下的浮动工具栏边距动画处理
        /// <summary>
        /// 在桌面模式下以动画将浮动工具栏居中并靠近任务栏显示，同时根据屏幕 DPI、任务栏高度、快捷调色盘显示与缩放调整目标位置。
        /// </summary>
        /// <remarks>如果当前处于白板模式则不执行任何操作；该方法在 UI 线程上使浮动工具栏可见、启动边距动画并更新内部位置状态（例如 pointDesktop 和 isViewboxFloatingBarMarginAnimationRunning）。</remarks>
        public async void PureViewboxFloatingBarMarginAnimationInDesktopMode()
        {
            // 在白板模式下不执行浮动栏动画
            if (currentMode == 1)
            {
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                // 仅计算Windows任务栏高度，不考虑其他程序对工作区的影响
                var toolbarHeight = ForegroundWindowInfo.GetTaskbarHeight(screen, dpiScaleY);

                double baseWidth = ViewboxFloatingBar.ActualWidth;

                // 如果ActualWidth为0，尝试使用DesiredSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.DesiredSize.Width;
                }

                // 如果仍然为0，使用RenderSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.RenderSize.Width;
                }

                // 如果所有方法都失败，使用一个基于内容的估算值
                if (baseWidth <= 0)
                {
                    // 根据浮动栏内容估算宽度
                    baseWidth = 200; // 最小宽度
                    LogHelper.WriteLogToFile($"浮动栏宽度无法获取，使用估算值: {baseWidth}");
                }

                double floatingBarWidth = baseWidth * ViewboxFloatingBarScaleTransform.ScaleX;


                // 如果快捷调色盘显示，确保有足够空间
                if ((QuickColorPalettePanel != null && QuickColorPalettePanel.Visibility == Visibility.Visible) ||
                    (QuickColorPaletteSingleRowPanel != null && QuickColorPaletteSingleRowPanel.Visibility == Visibility.Visible))
                {
                    // 根据显示模式调整宽度
                    if (Settings.Appearance.QuickColorPaletteDisplayMode == 0)
                    {
                        // 单行显示模式，自适应宽度，但需要足够空间显示6个颜色
                        floatingBarWidth = Math.Max(floatingBarWidth, 120 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                    else
                    {
                        // 双行显示模式，宽度较大
                        floatingBarWidth = Math.Max(floatingBarWidth, 68 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                }

                pos.X = (screenWidth - floatingBarWidth) / 2;

                // 如果任务栏高度为0,则使用固定边距
                if (toolbarHeight == 0)
                {
                    pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                           3 * ViewboxFloatingBarScaleTransform.ScaleY;
                    LogHelper.WriteLogToFile($"任务栏隐藏,使用固定高度: {ViewboxFloatingBar.ActualHeight}");
                }
                else
                {
                    pos.Y = screenHeight - ViewboxFloatingBar.ActualHeight * ViewboxFloatingBarScaleTransform.ScaleY -
                           toolbarHeight - ViewboxFloatingBarScaleTransform.ScaleY * 3;
                }

                if (pointDesktop.X != -1 || pointDesktop.Y != -1) pointDesktop = pos;

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20),
                    EasingFunction = new CircleEase()
                };
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });
        }

        /// <summary>
        /// PPT模式下的浮动工具栏边距动画处理
        /// </summary>
        /// <summary>
        /// 在非白板模式下将浮动工具栏在 PPT 环境中居中并执行边距位移动画以显示工具栏。
        /// </summary>
        /// <remarks>
        /// 方法会根据屏幕 DPI、任务栏高度和快捷调色盘的可见性计算浮动栏位置与宽度，触发位移动画并在必要时根据配置进行重试以确保在仅 PPT 模式下可见性。该方法会设置 ViewboxFloatingBar 的可见性和 Margin，并可能递归重试一次以修正未显示的情况。
        /// </remarks>
        /// <param name="isRetry">指示此次调用是否为重试（用于防止在仅 PPT 模式下反复触发多次重试）。</param>
        public async void PureViewboxFloatingBarMarginAnimationInPPTMode(bool isRetry = false)
        {
            // 新增：在白板模式下不执行浮动栏动画
            if (currentMode == 1)
            {
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Visibility = Visibility.Visible;
                isViewboxFloatingBarMarginAnimationRunning = true;

                double dpiScaleX = 1, dpiScaleY = 1;
                var source = PresentationSource.FromVisual(this);
                if (source != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }

                var windowHandle = new WindowInteropHelper(this).Handle;
                var screen = Screen.FromHandle(windowHandle);
                double screenWidth = screen.Bounds.Width / dpiScaleX, screenHeight = screen.Bounds.Height / dpiScaleY;
                // 仅计算Windows任务栏高度，不考虑其他程序对工作区的影响
                var toolbarHeight = ForegroundWindowInfo.GetTaskbarHeight(screen, dpiScaleY);

                // 计算浮动栏位置，考虑快捷调色盘的显示状态
                // 使用更可靠的方法获取浮动栏宽度
                double baseWidth = ViewboxFloatingBar.ActualWidth;

                // 如果ActualWidth为0，尝试使用DesiredSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.DesiredSize.Width;
                }

                // 如果仍然为0，使用RenderSize
                if (baseWidth <= 0)
                {
                    baseWidth = ViewboxFloatingBar.RenderSize.Width;
                }

                // 如果所有方法都失败，使用一个基于内容的估算值
                if (baseWidth <= 0)
                {
                    // 根据浮动栏内容估算宽度
                    baseWidth = 200; // 最小宽度
                    LogHelper.WriteLogToFile($"浮动栏宽度无法获取，使用估算值: {baseWidth}");
                }

                double floatingBarWidth = baseWidth * ViewboxFloatingBarScaleTransform.ScaleX;


                // 如果快捷调色盘显示，确保有足够空间
                if ((QuickColorPalettePanel != null && QuickColorPalettePanel.Visibility == Visibility.Visible) ||
                    (QuickColorPaletteSingleRowPanel != null && QuickColorPaletteSingleRowPanel.Visibility == Visibility.Visible))
                {
                    // 根据显示模式调整宽度
                    if (Settings.Appearance.QuickColorPaletteDisplayMode == 0)
                    {
                        // 单行显示模式，自适应宽度，但需要足够空间显示6个颜色
                        floatingBarWidth = Math.Max(floatingBarWidth, 120 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                    else
                    {
                        // 双行显示模式，宽度较大
                        floatingBarWidth = Math.Max(floatingBarWidth, 68 * ViewboxFloatingBarScaleTransform.ScaleX);
                    }
                }

                pos.X = (screenWidth - floatingBarWidth) / 2;

                pos.Y = screenHeight - 55 * ViewboxFloatingBarScaleTransform.ScaleY;

                if (pointPPT.X != -1 || pointPPT.Y != -1)
                {
                    pointPPT = pos;
                }

                var marginAnimation = new ThicknessAnimation
                {
                    Duration = TimeSpan.FromSeconds(0.35),
                    From = ViewboxFloatingBar.Margin,
                    To = new Thickness(pos.X, pos.Y, 0, -20),
                    EasingFunction = new CircleEase()
                };
                ViewboxFloatingBar.BeginAnimation(MarginProperty, marginAnimation);
            });

            await Task.Delay(349);

            await Dispatcher.InvokeAsync(() =>
            {
                ViewboxFloatingBar.Margin = new Thickness(pos.X, pos.Y, -2000, -200);
            });

            if (Settings.ModeSettings.IsPPTOnlyMode && !isRetry)
            {
                await Task.Delay(2000); // 等待动画完成后再检查

                bool isFloatingBarVisible = false;
                await Dispatcher.InvokeAsync(() =>
                {
                    // 检查浮动栏是否真的显示了
                    isFloatingBarVisible = ViewboxFloatingBar.Visibility == Visibility.Visible &&
                                          ViewboxFloatingBar.Margin.Left >= 0 &&
                                          ViewboxFloatingBar.Margin.Top >= 0;
                });

                if (!isFloatingBarVisible)
                {
                    PureViewboxFloatingBarMarginAnimationInPPTMode(true);
                }
            }
        }

        /// <summary>
        /// 光标图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到光标（鼠标）工具：禁用橡皮擦叠加、更新当前工具模式并调整画布、浮动工具栏及相关面板的可见性与状态；在切换前根据条件保存截图与笔迹并恢复全屏等退出批注模式的必要状态。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为光标图标控件）。</param>
        /// <param name="e">路由事件参数。</param>
        internal async void CursorIcon_Click(object sender, RoutedEventArgs e)
        {
            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == Cursor_Icon && lastBorderMouseDownObject != Cursor_Icon) return;

            // 禁用高级橡皮擦系统
            DisableEraserOverlay();
            SetCurrentToolMode(InkCanvasEditingMode.None);

            UpdateCurrentToolMode("cursor");

            SetFloatingBarHighlightPosition("cursor");

            // 切换前自动截图保存墨迹
            if (inkCanvas.Strokes.Count > 0 &&
                inkCanvas.Strokes.Count > Settings.Automation.MinimumAutomationStrokeNumber)
            {
                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                {
                    var currentSlide = _pptManager?.GetCurrentSlideNumber() ?? 0;
                    var presentationName = _pptManager?.GetPresentationName() ?? "";
                    SaveScreenShot(true, $"{presentationName}/{currentSlide}_{DateTime.Now:HH-mm-ss}");
                }
                else SaveScreenShot(true);
            }

            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
            {
                if (Settings.Canvas.HideStrokeWhenSelecting)
                {
                    inkCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    inkCanvas.IsHitTestVisible = false;
                    inkCanvas.Visibility = Visibility.Visible;
                }
            }
            else
            {
                if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                {
                    inkCanvas.Visibility = Visibility.Visible;
                    inkCanvas.IsHitTestVisible = true;
                }
                else
                {
                    if (Settings.Canvas.HideStrokeWhenSelecting)
                    {
                        inkCanvas.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        inkCanvas.IsHitTestVisible = false;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }
            }

            GridTransparencyFakeBackground.Opacity = 0;
            GridTransparencyFakeBackground.Background = Brushes.Transparent;

            GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

            // 点击鼠标按钮退出批注模式时的全屏还原
            RestoreFullScreenOnExitAnnotationMode();

            inkCanvas.Select(new StrokeCollection());
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (currentMode != 0)
            {
                SaveStrokes();
                RestoreStrokes(true);
            }

            if (BtnSwitchTheme.Content.ToString() == "浅色")
                BtnSwitch.Content = "黑板";
            else
                BtnSwitch.Content = "白板";

            StackPanelPPTButtons.Visibility = Visibility.Visible;
            BtnHideInkCanvas.Content = "显示\n画板";
            CheckEnableTwoFingerGestureBtnVisibility(false);


            StackPanelCanvasControls.Visibility = Visibility.Collapsed;

            // 在鼠标模式下隐藏快捷调色盘
            if (QuickColorPalettePanel != null)
            {
                QuickColorPalettePanel.Visibility = Visibility.Collapsed;
            }
            if (QuickColorPaletteSingleRowPanel != null)
            {
                QuickColorPaletteSingleRowPanel.Visibility = Visibility.Collapsed;
            }

            if (!isFloatingBarFolded)
            {
                HideSubPanels("cursor", true);
                await Task.Delay(50);

                if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                    ViewboxFloatingBarMarginAnimation(60);
                else
                    ViewboxFloatingBarMarginAnimation(100, true);
            }
        }

        /// <summary>
        /// 画笔图标点击事件处理，用于切换到批注模式或显示画笔调色盘
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到批注（画笔）模式或在已处于批注模式时显示/隐藏画笔面板，并同步更新工具状态、绘图属性与相关 UI 元素（如快捷调色板、全屏辅助、手势按钮、画布可见性等）。
        /// </summary>
        /// <param name="sender">触发事件的源对象。</param>
        /// <param name="e">路由事件参数。</param>
        internal void PenIcon_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == Pen_Icon && lastBorderMouseDownObject != Pen_Icon) return;

            // 如果当前有选中的图片元素，先取消选中
            if (currentSelectedElement != null)
            {
                UnselectElement(currentSelectedElement);
                currentSelectedElement = null;
            }

            // 禁用高级橡皮擦系统
            DisableEraserOverlay();

            SetFloatingBarHighlightPosition("pen");

            // 记录当前是否已经是批注模式且是否为高光显示模式
            bool wasInInkMode = inkCanvas.EditingMode == InkCanvasEditingMode.Ink;
            bool wasHighlighter = drawingAttributes.IsHighlighter;

            if (drawingShapeMode != 0 && !isLongPressSelected)
            {
                return;
            }

            if (Pen_Icon.Background == null || StackPanelCanvasControls.Visibility == Visibility.Collapsed)
            {
                if (isLongPressSelected)
                {
                    drawingShapeMode = 0;
                    isLongPressSelected = false;
                }

                // 使用集中化的工具模式切换方法
                SetCurrentToolMode(InkCanvasEditingMode.Ink);

                // 更新模式缓存
                UpdateCurrentToolMode("pen");

                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));

                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                /*if (forceEraser && currentMode == 0)
                    BtnColorRed_Click(sender, null);*/

                if (GridBackgroundCover.Visibility == Visibility.Collapsed)
                {
                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                        BtnSwitch.Content = "黑板";
                    else
                        BtnSwitch.Content = "白板";
                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnSwitch.Content = "屏幕";
                    StackPanelPPTButtons.Visibility = Visibility.Collapsed;
                }

                BtnHideInkCanvas.Content = "隐藏\n画板";

                // 进入批注模式时的全屏处理（仅当未应用过全屏处理时）
                if (Settings.Advanced.IsEnableAvoidFullScreenHelper && !isFullScreenApplied)
                {
                    // 设置为画板模式，允许全屏操作
                    AvoidFullScreenHelper.SetBoardMode(true);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainWindow.MoveWindow(new WindowInteropHelper(this).Handle, 0, 0,
                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
                    }), DispatcherPriority.ApplicationIdle);

                    isFullScreenApplied = true; // 标记已应用全屏处理
                }

                StackPanelCanvasControls.Visibility = Visibility.Visible;
                //AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);
                // 使用集中化的工具模式切换方法
                SetCurrentToolMode(InkCanvasEditingMode.Ink);

                // 更新模式缓存
                UpdateCurrentToolMode("pen");

                // 在批注模式下显示快捷调色盘（如果设置中启用了）
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

                forceEraser = false;
                forcePointEraser = false;
                drawingShapeMode = 0;

                // 保持之前的笔类型状态，而不是强制重置
                if (!wasHighlighter)
                {
                    penType = 0;
                    drawingAttributes.IsHighlighter = false;
                    drawingAttributes.StylusTip = StylusTip.Ellipse;
                }
                // 如果之前是荧光笔模式，则保持荧光笔属性
                else if (penType == 1)
                {
                    drawingAttributes.IsHighlighter = true;
                    drawingAttributes.StylusTip = StylusTip.Rectangle;
                    drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                    drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                }

                ColorSwitchCheck();
                HideSubPanels("pen", true);
            }
            else
            {
                if (wasInInkMode)
                {
                    if (forceEraser)
                    {
                        // 从橡皮擦模式切换过来，保持之前的笔类型状态
                        forceEraser = false;
                        forcePointEraser = false;
                        drawingShapeMode = 0;

                        // 保持之前的笔类型状态，而不是强制重置
                        if (!wasHighlighter)
                        {
                            penType = 0;
                            drawingAttributes.IsHighlighter = false;
                            drawingAttributes.StylusTip = StylusTip.Ellipse;
                        }
                        // 如果之前是荧光笔模式，则保持荧光笔属性
                        else if (penType == 1)
                        {
                            drawingAttributes.IsHighlighter = true;
                            drawingAttributes.StylusTip = StylusTip.Rectangle;
                            drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                            drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                        }

                        // 在非白板模式下，从线擦切换到批注时不直接弹出子面板
                        if (currentMode != 1)
                        {
                            HideSubPanels("pen", true);
                            return;
                        }
                    }

                    if (PenPalette.Visibility == Visibility.Visible)
                    {
                        AnimationsHelper.HideWithSlideAndFade(PenPalette);
                        AnimationsHelper.HideWithSlideAndFade(BoardPenPalette);
                    }
                    else
                    {
                        HideSubPanels();
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(PenPalette);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardPenPalette);
                    }
                }
                else
                {
                    // 切换到批注模式时，确保保存当前图片信息
                    if (currentMode != 0)
                    {
                        SaveStrokes();
                    }
                    // 使用集中化的工具模式切换方法
                    SetCurrentToolMode(InkCanvasEditingMode.Ink);

                    // 更新模式缓存
                    UpdateCurrentToolMode("pen");

                    forceEraser = false;
                    forcePointEraser = false;
                    drawingShapeMode = 0;

                    // 保持之前的笔类型状态，而不是强制重置
                    if (!wasHighlighter)
                    {
                        penType = 0;
                        drawingAttributes.IsHighlighter = false;
                        drawingAttributes.StylusTip = StylusTip.Ellipse;
                    }
                    // 如果之前是荧光笔模式，则保持荧光笔属性
                    else if (penType == 1)
                    {
                        drawingAttributes.IsHighlighter = true;
                        drawingAttributes.StylusTip = StylusTip.Rectangle;
                        drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                        drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                    }

                    ColorSwitchCheck();
                    HideSubPanels("pen", true);
                }
            }


            forceEraser = false;
            forcePointEraser = false;
            drawingShapeMode = 0;
        }

        /// <summary>
        /// 颜色主题切换鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换浅色/深色主题设置（在桌面模式下同步保存桌面主题），并应用新的配色方案。
        /// </summary>
        /// <param name="e">路由事件参数</param>
        private void ColorThemeSwitch_MouseUp(object sender, RoutedEventArgs e)
        {
            isUselightThemeColor = !isUselightThemeColor;
            if (currentMode == 0) isDesktopUselightThemeColor = isUselightThemeColor;
            CheckColorTheme();
        }

        /// <summary>
        /// 橡皮擦图标点击事件处理，用于切换到橡皮擦模式或显示橡皮擦尺寸面板
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到点状橡皮擦工具；在必要时保存当前笔迹、启用高级橡皮擦覆盖层、更新工具模式，并根据当前状态显示或隐藏橡皮擦尺寸面板。
        /// </summary>
        /// <param name="sender">触发该事件的源对象。</param>
        /// <param name="e">路由事件参数。</param>
        internal void EraserIcon_Click(object sender, RoutedEventArgs e)
        {
            bool isAlreadyEraser = inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;
            forceEraser = false;
            forcePointEraser = true;
            drawingShapeMode = 0;

            // 切换到橡皮擦模式时，确保保存当前图片信息
            if (!isAlreadyEraser && currentMode != 0)
            {
                SaveStrokes();
            }

            if (!isAlreadyEraser)
            {
                ResetTouchStates();
            }

            // 启用新的高级橡皮擦系统
            EnableEraserOverlay();

            // 使用新的高级橡皮擦系统
            // 使用集中化的工具模式切换方法
            SetCurrentToolMode(InkCanvasEditingMode.EraseByPoint);

            // 更新模式缓存
            UpdateCurrentToolMode("eraser");

            ApplyAdvancedEraserShape(); // 使用新的橡皮擦形状应用方法
            SetCursorBasedOnEditingMode(inkCanvas);
            HideSubPanels("eraser"); // 高亮橡皮按钮
            Trace.WriteLine($"Eraser: Eraser button clicked, current size: {eraserWidth}, circle: {isEraserCircleShape}");

            if (isAlreadyEraser)
            {
                // 已是橡皮状态，再次点击才弹出/收起面板
                if (EraserSizePanel.Visibility == Visibility.Collapsed)
                {
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(EraserSizePanel);
                    if (BoardEraserSizePanel != null)
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardEraserSizePanel);
                }
                else
                {
                    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                    if (BoardEraserSizePanel != null)
                        AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                }
            }
        }

        /// <summary>
        /// 白板模式下的橡皮擦图标点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到点擦除工具（板端），启用高级橡皮擦覆盖层并更新当前工具状态；在已为橡皮擦状态时切换板上橡皮尺寸面板的显示/隐藏。
        /// </summary>
        /// <remarks>
        /// - 启用并应用高级橡皮擦形状、设置光标并隐藏其他子面板的高亮。  
        /// - 若当前已是点擦除模式，再次点击会在 BoardEraserSizePanel 与 EraserSizePanel 之间执行显示或隐藏切换。
        /// </remarks>
        /// <param name="sender">触发事件的源对象（通常为板端橡皮擦图标按钮）。</param>
        /// <param name="e">路由事件参数。</param>
        private void BoardEraserIcon_Click(object sender, RoutedEventArgs e)
        {
            bool isAlreadyEraser = inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint;
            forceEraser = false;
            forcePointEraser = true;
            drawingShapeMode = 0;

            // 启用新的高级橡皮擦系统
            EnableEraserOverlay();

            // 使用新的高级橡皮擦系统
            // 使用集中化的工具模式切换方法
            SetCurrentToolMode(InkCanvasEditingMode.EraseByPoint);

            // 更新模式缓存
            UpdateCurrentToolMode("eraser");

            ApplyAdvancedEraserShape(); // 使用新的橡皮擦形状应用方法
            SetCursorBasedOnEditingMode(inkCanvas);
            HideSubPanels("eraser"); // 高亮橡皮按钮

            if (isAlreadyEraser)
            {
                // 已是橡皮状态，再次点击才弹出/收起面板
                if (BoardEraserSizePanel != null && BoardEraserSizePanel.Visibility == Visibility.Collapsed)
                {
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardEraserSizePanel);
                    AnimationsHelper.ShowWithSlideFromBottomAndFade(EraserSizePanel);
                }
                else
                {
                    if (BoardEraserSizePanel != null)
                        AnimationsHelper.HideWithSlideAndFade(BoardEraserSizePanel);
                    AnimationsHelper.HideWithSlideAndFade(EraserSizePanel);
                }
            }
        }

        /// <summary>
        /// 墨迹擦除图标点击事件处理，用于切换到按笔画擦除模式
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到按笔划擦除工具并更新相关 UI 与内部状态。
        /// </summary>
        /// <remarks>
        /// 禁用高级橡皮擦覆盖，设置点擦/笔划擦的强制标志和橡皮擦形状，更新当前工具模式缓存、编辑模式及绘图形状状态；同时取消单指拖拽模式并隐藏相关子面板以反映模式切换的 UI 变化。
        /// </remarks>
        /// <param name="e">路由事件参数。</param>
        private void EraserIconByStrokes_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == EraserByStrokes_Icon && lastBorderMouseDownObject != EraserByStrokes_Icon) return;

            // 禁用高级橡皮擦系统
            DisableEraserOverlay();

            forceEraser = true;
            forcePointEraser = false;

            inkCanvas.EraserShape = new EllipseStylusShape(5, 5);
            // 使用集中化的工具模式切换方法
            SetCurrentToolMode(InkCanvasEditingMode.EraseByStroke);

            // 更新模式缓存
            UpdateCurrentToolMode("eraserByStrokes");

            drawingShapeMode = 0;

            // 这样从线擦切换回批注时，可以恢复之前的荧光笔状态
            // penType 和 drawingAttributes 的状态将在 PenIcon_Click 中根据 wasHighlighter 来恢复

            inkCanvas_EditingModeChanged(inkCanvas, null);
            CancelSingleFingerDragMode();

            HideSubPanels("eraserByStrokes");

        }

        /// <summary>
        /// 光标删除图标点击事件处理，用于删除选中内容并切换到光标模式
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在触发删除操作后切换到光标工具。
        /// </summary>
        /// <param name="sender">触发事件的源对象，通常为浮动工具栏或其按钮。</param>
        /// <param name="e">路由事件参数。</param>
        private void CursorWithDelIcon_Click(object sender, RoutedEventArgs e)
        {

            if (lastBorderMouseDownObject is Panel panel)
                panel.Background = new SolidColorBrush(Colors.Transparent);
            if (sender == CursorWithDelFloatingBarBtn && lastBorderMouseDownObject != CursorWithDelFloatingBarBtn) return;

            SymbolIconDelete_MouseUp(sender, null);
            CursorIcon_Click(null, null);
        }

        /// <summary>
        /// 将当前绘笔颜色设置为白色并安排在短时间后自动恢复到之前的笔刷。
        /// <summary>
        /// 将快速颜色设置为白色，并安排自动恢复到先前的画笔设置。
        /// </summary>
        private void QuickColorWhite_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Colors.White);
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将快速颜色设置为橙色，并安排稍后自动恢复到先前的画笔颜色。
        /// <summary>
        /// 将快速颜色设置为橙色，并安排画笔在短时间后自动恢复原先设置。
        /// </summary>
        private void QuickColorOrange_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Color.FromRgb(251, 150, 80)); // 橙色
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将画笔颜色切换为黄色并安排自动恢复为先前的画笔设置。
        /// <summary>
        /// 将快速颜色切换为黄色，并安排在短时间后自动恢复先前的画笔设置。
        /// </summary>
        private void QuickColorYellow_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Colors.Yellow);
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将快速颜色设置为黑色并安排在稍后自动恢复为先前的画笔颜色。
        /// <summary>
        /// 将当前快速颜色切换为黑色并安排在短时间后自动恢复到先前的笔刷设置。
        /// </summary>
        private void QuickColorBlack_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Colors.Black);
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将当前画笔颜色设置为蓝色并安排在一段时间后自动恢复到之前的画笔颜色。
        /// <summary>
        /// 将预设的蓝色应用为当前笔刷并安排在一段时间后自动恢复先前的笔刷设置。
        /// </summary>
        private void QuickColorBlue_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Color.FromRgb(37, 99, 235)); // 蓝色
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将快速颜色切换为红色，并安排稍后自动恢复为先前的画笔颜色。
        /// <summary>
        /// 将快速颜色切换为红色，并安排在短时间后自动恢复到之前的画笔设置。
        /// </summary>
        private void QuickColorRed_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Colors.Red);
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将快速颜色切换为绿色并安排在一段时间后自动恢复先前画笔颜色。
        /// <summary>
        /// 将快速颜色切换为绿色（RGB 22,163,74）并安排在短时间后自动恢复先前的画笔设置。
        /// </summary>
        private void QuickColorGreen_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Color.FromRgb(22, 163, 74));
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 将当前画笔颜色切换为紫色快捷色并安排自动恢复先前画笔设置。
        /// <summary>
        /// 将快速颜色设置为紫色（RGB 147,51,234）并安排在短时间后自动恢复为先前画笔设置。
        /// </summary>
        private void QuickColorPurple_Click(object sender, RoutedEventArgs e)
        {
            SetQuickColor(Color.FromRgb(147, 51, 234));
            ScheduleBrushAutoRestore();
        }

        /// <summary>
        /// 设置并应用快速颜色到当前画笔与相关状态，包括必要时切换到批注模式、更新荧光笔属性与颜色索引、记录桌面/白板的最近颜色，以及刷新调色盘指示器和颜色显示。
        /// </summary>
        /// <param name="color">要应用的颜色。</param>
        /// <remarks>
        /// 此方法会：
        /// - 在非批注模式时切换到绘制（Ink）模式；
        /// - 将指定颜色应用到绘图属性和 InkCanvas 的默认绘图属性；
        /// - 在荧光笔模式下更新荧光笔的内部颜色索引与绘图属性（宽度、笔尖形状、IsHighlighter 等）；
        /// - 根据当前模式（桌面或白板）记录最近使用的颜色索引；
        /// - 更新快速调色盘的选中指示器并刷新颜色显示状态。
        /// <summary>
        /// 将指定颜色应用为当前画笔/荧光笔颜色并更新相关快捷色盘与状态。
        /// </summary>
        /// <param name="color">要设置的画笔颜色。</param>
        /// <remarks>
        /// 如果当前不在批注（Ink）模式，会切换到画笔模式；当处于荧光笔类型时同时更新荧光笔属性与内部荧光笔颜色索引。方法还会更新桌面/白板模式下的最近使用颜色索引、刷新快捷调色盘的选中指示器，并触发颜色切换检查以同步界面与绘制属性。</remarks>
        private void SetQuickColor(Color color)
        {
            // 确保当前处于批注模式
            if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink)
            {
                PenIcon_Click(null, null);
            }

            // 设置画笔颜色
            drawingAttributes.Color = color;
            inkCanvas.DefaultDrawingAttributes.Color = color;

            // 如果当前是荧光笔模式，同时更新荧光笔颜色和属性
            if (penType == 1)
            {
                // 根据颜色设置对应的荧光笔颜色索引
                if (color == Colors.White || IsColorSimilar(color, Color.FromRgb(250, 250, 250), 10))
                {
                    highlighterColor = 101; // 白色荧光笔
                }
                else if (color == Colors.Black)
                {
                    highlighterColor = 100; // 黑色荧光笔
                }
                else if (color == Colors.Yellow || IsColorSimilar(color, Color.FromRgb(234, 179, 8)) ||
                         IsColorSimilar(color, Color.FromRgb(250, 204, 21)) ||
                         IsColorSimilar(color, Color.FromRgb(253, 224, 71)))
                {
                    highlighterColor = 103; // 黄色荧光笔
                }
                else if (color == Color.FromRgb(255, 165, 0) || color == Color.FromRgb(251, 150, 80) || IsColorSimilar(color, Color.FromRgb(249, 115, 22), 20) ||
                         IsColorSimilar(color, Color.FromRgb(234, 88, 12), 20) ||
                         IsColorSimilar(color, Color.FromRgb(251, 146, 60), 20) ||
                         IsColorSimilar(color, Color.FromRgb(253, 126, 20), 20))
                {
                    highlighterColor = 109; // 橙色荧光笔
                }
                else if (color == Color.FromRgb(37, 99, 235))
                {
                    highlighterColor = 106; // 蓝色荧光笔
                }
                else if (color == Colors.Red || IsColorSimilar(color, Color.FromRgb(220, 38, 38)) ||
                         IsColorSimilar(color, Color.FromRgb(239, 68, 68)))
                {
                    highlighterColor = 102; // 红色荧光笔
                }
                else if (color == Colors.Green || IsColorSimilar(color, Color.FromRgb(22, 163, 74)))
                {
                    highlighterColor = 104; // 绿色荧光笔
                }
                else if (color == Color.FromRgb(147, 51, 234))
                {
                    highlighterColor = 107; // 紫色荧光笔
                }

                // 确保荧光笔属性正确设置
                drawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                drawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                drawingAttributes.StylusTip = StylusTip.Rectangle;
                drawingAttributes.IsHighlighter = true;

                inkCanvas.DefaultDrawingAttributes.Width = Settings.Canvas.HighlighterWidth / 2;
                inkCanvas.DefaultDrawingAttributes.Height = Settings.Canvas.HighlighterWidth;
                inkCanvas.DefaultDrawingAttributes.StylusTip = StylusTip.Rectangle;
                inkCanvas.DefaultDrawingAttributes.IsHighlighter = true;

                // 确保荧光笔颜色索引正确更新
                inkCanvas.DefaultDrawingAttributes.Color = drawingAttributes.Color;
            }

            // 更新颜色状态
            if (currentMode == 0)
            {
                // 桌面模式
                if (color == Colors.White) lastDesktopInkColor = 5;
                else if (color == Color.FromRgb(251, 150, 80)) lastDesktopInkColor = 8; // 橙色
                else if (color == Colors.Yellow) lastDesktopInkColor = 4;
                else if (color == Colors.Black) lastDesktopInkColor = 0;
                else if (color == Color.FromRgb(37, 99, 235)) lastDesktopInkColor = 3; // 蓝色
                else if (color == Colors.Red) lastDesktopInkColor = 1;
                else if (color == Colors.Green || color == Color.FromRgb(22, 163, 74)) lastDesktopInkColor = 2;
                else if (color == Color.FromRgb(147, 51, 234)) lastDesktopInkColor = 6; // 紫色
            }
            else
            {
                // 白板模式
                if (color == Colors.White) lastBoardInkColor = 5;
                else if (color == Color.FromRgb(251, 150, 80)) lastBoardInkColor = 8; // 橙色
                else if (color == Colors.Yellow) lastBoardInkColor = 4;
                else if (color == Colors.Black) lastBoardInkColor = 0;
                else if (color == Color.FromRgb(37, 99, 235)) lastBoardInkColor = 3; // 蓝色
                else if (color == Colors.Red) lastBoardInkColor = 1;
                else if (color == Colors.Green || color == Color.FromRgb(22, 163, 74)) lastBoardInkColor = 2;
                else if (color == Color.FromRgb(147, 51, 234)) lastBoardInkColor = 6; // 紫色
            }

            // 更新快捷调色盘选择指示器
            UpdateQuickColorPaletteIndicator(color);

            // 更新颜色显示
            ColorSwitchCheck();

            // 如果当前是荧光笔模式，调用ColorSwitchCheck确保颜色索引正确更新
            if (penType == 1)
            {
                ColorSwitchCheck();
            }
        }

        /// <summary>
        /// 更新快速调色盘的选中指示器，根据当前选中的颜色显示对应的勾选图标
        /// </summary>
        /// <summary>
        /// 根据所选颜色在快速颜色面板（单行/双行两种展示）中显示相应的勾选指示器。
        /// </summary>
        /// <param name="selectedColor">用于匹配面板中预设颜色并显示对应勾选标记的颜色；在荧光笔模式下匹配容差更大。</param>
        private void UpdateQuickColorPaletteIndicator(Color selectedColor)
        {
            // 隐藏所有check图标（双行显示）
            QuickColorWhiteCheck.Visibility = Visibility.Collapsed;
            QuickColorOrangeCheck.Visibility = Visibility.Collapsed;
            QuickColorYellowCheck.Visibility = Visibility.Collapsed;
            QuickColorBlackCheck.Visibility = Visibility.Collapsed;
            QuickColorBlueCheck.Visibility = Visibility.Collapsed;
            QuickColorRedCheck.Visibility = Visibility.Collapsed;
            QuickColorGreenCheck.Visibility = Visibility.Collapsed;
            QuickColorPurpleCheck.Visibility = Visibility.Collapsed;

            // 隐藏所有check图标（单行显示）
            QuickColorWhiteCheckSingle.Visibility = Visibility.Collapsed;
            QuickColorOrangeCheckSingle.Visibility = Visibility.Collapsed;
            QuickColorYellowCheckSingle.Visibility = Visibility.Collapsed;
            QuickColorBlackCheckSingle.Visibility = Visibility.Collapsed;
            QuickColorRedCheckSingle.Visibility = Visibility.Collapsed;
            QuickColorGreenCheckSingle.Visibility = Visibility.Collapsed;

            // 显示当前选中颜色的check图标
            // 在荧光笔模式下，使用更宽松的颜色匹配
            int tolerance = (penType == 1) ? 25 : 15; // 荧光笔模式使用更大的容差

            if (IsColorSimilar(selectedColor, Colors.White, tolerance) || IsColorSimilar(selectedColor, Color.FromRgb(250, 250, 250), tolerance))
            {
                QuickColorWhiteCheck.Visibility = Visibility.Visible;
                QuickColorWhiteCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Colors.Black, tolerance))
            {
                QuickColorBlackCheck.Visibility = Visibility.Visible;
                QuickColorBlackCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Colors.Yellow, tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(234, 179, 8), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(250, 204, 21), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(253, 224, 71), tolerance))
            {
                QuickColorYellowCheck.Visibility = Visibility.Visible;
                QuickColorYellowCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Color.FromRgb(255, 165, 0), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(251, 150, 80), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(249, 115, 22), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(234, 88, 12), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(251, 146, 60), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(253, 126, 20), tolerance))
            {
                QuickColorOrangeCheck.Visibility = Visibility.Visible;
                QuickColorOrangeCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Color.FromRgb(37, 99, 235), tolerance))
            {
                QuickColorBlueCheck.Visibility = Visibility.Visible;
                // 单行显示模式没有蓝色，所以不设置单行的check
            }
            else if (IsColorSimilar(selectedColor, Colors.Red, tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(220, 38, 38), tolerance) ||
                     IsColorSimilar(selectedColor, Color.FromRgb(239, 68, 68), tolerance))
            {
                QuickColorRedCheck.Visibility = Visibility.Visible;
                QuickColorRedCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Color.FromRgb(22, 163, 74), tolerance))
            {
                QuickColorGreenCheck.Visibility = Visibility.Visible;
                QuickColorGreenCheckSingle.Visibility = Visibility.Visible;
            }
            else if (IsColorSimilar(selectedColor, Color.FromRgb(147, 51, 234), tolerance))
            {
                QuickColorPurpleCheck.Visibility = Visibility.Visible;
                // 单行显示模式没有紫色，所以不设置单行的check
            }
        }

        /// <summary>
        /// 检查两个颜色是否相似（允许一定的误差范围）
        /// <summary>
        /// 判断两个颜色在指定的通道差容差内是否被视为相似。
        /// </summary>
        /// <param name="color1">第一个颜色。</param>
        /// <param name="color2">第二个颜色。</param>
        /// <param name="tolerance">每个 RGB 通道允许的最大差值（含），默认 15。</param>
        /// <returns>`true` 如果 R、G、B 三个通道的绝对差均小于或等于 <paramref name="tolerance"/>，否则 `false`。</returns>
        private bool IsColorSimilar(Color color1, Color color2, int tolerance = 15)
        {
            int rDiff = Math.Abs(color1.R - color2.R);
            int gDiff = Math.Abs(color1.G - color2.G);
            int bDiff = Math.Abs(color1.B - color2.B);

            return rDiff <= tolerance && gDiff <= tolerance && bDiff <= tolerance;
        }

        /// <summary>
        /// 选择工具图标鼠标释放事件处理，用于切换到选择模式或选择所有墨迹
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换或激活“选择”工具；如果已处于选择模式，则选中画布上所有非空笔划，否则切换到选择模式。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">路由事件参数。</param>
        private void SelectIcon_MouseUp(object sender, RoutedEventArgs e)
        {
            // 禁用高级橡皮擦系统
            DisableEraserOverlay();

            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                var selectedStrokes = new StrokeCollection();
                foreach (var stroke in inkCanvas.Strokes)
                    if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                        selectedStrokes.Add(stroke);
                inkCanvas.Select(selectedStrokes);
            }
            else
            {
                // 使用集中化的工具模式切换方法
                SetCurrentToolMode(InkCanvasEditingMode.Select);
            }
        }

        /// <summary>
        /// 从图形绘制模式切换到画笔模式的提示处理
        /// <summary>
        /// 根据当前形状绘制与长按状态隐藏相关子面板并在必要时切换到笔或光标模式。
        /// </summary>
        /// <remarks>
        /// 若处于长按选中状态，仅隐藏形状相关子面板；否则当画布控制面板可见时隐藏子面板并切换到笔模式，若画布控制面板不可见则切换到光标模式。
        /// </remarks>
        private void DrawShapePromptToPen()
        {
            if (isLongPressSelected)
            {
                // 如果是长按选中的状态，只隐藏面板，不切换到笔模式
                HideSubPanels("shape");
            }
            else
            {
                if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    HideSubPanels("pen");
                else
                    HideSubPanels("cursor");
            }
        }

        /// <summary>
        /// 关闭工具面板鼠标释放事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在鼠标松开时关闭工具栏并隐藏所有子面板。
        /// </summary>
        private void CloseBordertools_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideSubPanels();
        }

        #region Left Side Panel

        /// <summary>
        /// 手指拖动模式切换按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换单指与多指拖动模式并同步更新按钮上的文本显示。
        /// </summary>
        /// <param name="sender">触发事件的控件（通常为按钮）。</param>
        /// <param name="e">路由事件参数。</param>
        private void BtnFingerDragMode_Click(object sender, RoutedEventArgs e)
        {
            if (isSingleFingerDragMode)
            {
                isSingleFingerDragMode = false;
                BtnFingerDragMode.Content = "单指\n拖动";
            }
            else
            {
                isSingleFingerDragMode = true;
                BtnFingerDragMode.Content = "多指\n拖动";
            }
        }

        /// <summary>
        /// 撤销按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 撤销上一次历史操作并将其应用到画布上；如果存在选中的笔迹，先清除选区。
        /// </summary>
        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            var item = timeMachine.Undo();
            ApplyHistoryToCanvas(item);
        }

        /// <summary>
        /// 重做按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 执行重做操作：若存在选中笔画则先取消选择，然后从时间机获取重做项并将该历史项应用到画布。
        /// </summary>
        /// <param name="e">路由事件参数。</param>
        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            if (inkCanvas.GetSelectedStrokes().Count != 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                inkCanvas.Select(new StrokeCollection());
            }

            var item = timeMachine.Redo();
            ApplyHistoryToCanvas(item);
        }

        /// <summary>
        /// 按钮启用状态变更事件处理，用于更新按钮内容的透明度
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 根据按钮的 IsEnabled 状态调整其 Content 的不透明度以提供启用/禁用的视觉反馈。
        /// </summary>
        /// <param name="sender">触发事件的 Button。</param>
        /// <param name="e">依赖属性变更事件参数，表示 IsEnabled 的变化。</param>
        private void Btn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!isLoaded) return;
            try
            {
                if (((Button)sender).IsEnabled)
                    ((UIElement)((Button)sender).Content).Opacity = 1;
                else
                    ((UIElement)((Button)sender).Content).Opacity = 0.25;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        #endregion Left Side Panel

        #region Right Side Panel

        public static bool CloseIsFromButton;

        /// <summary>
        /// 退出按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理“退出”按钮的点击：在必要时保存设置并恢复临时的无焦点模式，然后发起应用程序退出流程。
        /// </summary>
        /// <param name="sender">事件的发送者（通常为退出按钮）。</param>
        /// <param name="e">路由事件参数。</param>
        public void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            // 如果当前在设置面板中，需要先恢复无焦点模式状态
            if (BorderSettings.Visibility == Visibility.Visible)
            {
                if (isTemporarilyDisablingNoFocusMode)
                {
                    isTemporarilyDisablingNoFocusMode = false;
                    ApplyNoFocusMode();
                }
                SaveSettingsToFile();
            }

            App.IsAppExitByUser = true;
            // 不设置 CloseIsFromButton = true，让它也经过确认流程
            Close();
        }

        /// <summary>
        /// 重启按钮点击事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 启动当前可执行文件并传入参数 "-m" 以触发重启流程，然后标记为用户退出并关闭窗口；
        /// 如果设置面板处于可见状态，会保存设置并在临时恢复的无焦点模式下恢复该模式。
        /// </summary>
        /// <param name="sender">事件触发源。</param>
        /// <param name="e">路由事件参数。</param>
        public void BtnRestart_Click(object sender, RoutedEventArgs e)
        {
            if (BorderSettings.Visibility == Visibility.Visible)
            {
                if (isTemporarilyDisablingNoFocusMode)
                {
                    isTemporarilyDisablingNoFocusMode = false;
                    ApplyNoFocusMode();
                }
                SaveSettingsToFile();
            }

            Process.Start(System.Windows.Forms.Application.ExecutablePath, "-m");
            App.IsAppExitByUser = true;
            // 不设置 CloseIsFromButton = true，让它也经过确认流程
            Close();
        }

        /// <summary>
        /// 设置覆盖层点击事件处理，用于点击设置面板外部时关闭设置面板
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在设置面板外部点击时关闭设置面板（仅在面板不处于打开或隐藏过渡中生效）。
        /// </summary>
        /// <param name="sender">事件源（可为空，方法未使用该参数）。</param>
        /// <param name="e">鼠标按钮事件参数，包含点击位置用于判断是否在设置面板内部。</param>
        private void SettingsOverlayClick(object sender, MouseButtonEventArgs e)
        {
            if (isOpeningOrHidingSettingsPane) return;

            // 获取点击的位置
            Point clickPoint = e.GetPosition(BorderSettingsMask);

            // 获取BorderSettings的位置和大小
            Point settingsPosition = BorderSettings.TranslatePoint(new Point(0, 0), BorderSettingsMask);
            Rect settingsRect = new Rect(
                settingsPosition.X,
                settingsPosition.Y,
                BorderSettings.ActualWidth,
                BorderSettings.ActualHeight
            );

            // 如果点击位置不在设置界面内部，才关闭设置界面
            if (!settingsRect.Contains(clickPoint))
            {
                BtnSettings_Click(null, null);
            }
        }

        private bool isOpeningOrHidingSettingsPane;
        private bool wasNoFocusModeBeforeSettings;

        /// <summary>
        /// 切换并打开设置面板；在需要时先进行安全密码校验，然后显示设置面板并启动打开动画，同时根据设置暂时调整无焦点模式与遮罩交互状态。
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换设置面板的可见性；在打开面板前可进行密码验证，并以滑入动画展示面板，同时设置蒙版与无焦点临时状态。
        /// </summary>
        /// <param name="sender">事件源，通常为触发此操作的按钮。</param>
        /// <param name="e">路由事件参数。</param>
        private async void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (BorderSettings.Visibility == Visibility.Visible)
            {
                HideSubPanels();
            }
            else
            {
                try
                {
                    if (Ink_Canvas.Helpers.SecurityManager.IsPasswordRequiredForEnterSettings(Settings))
                    {
                        bool ok = await Ink_Canvas.Helpers.SecurityManager.PromptAndVerifyAsync(Settings, this, "进入设置", "请输入安全密码以进入设置。");
                        if (!ok) return;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"安全密码校验失败: {ex}", LogHelper.LogType.Error);
                    return;
                }

                BorderSettings.Visibility = Visibility.Visible;
                wasNoFocusModeBeforeSettings = Settings.Advanced.IsNoFocusMode;
                if (wasNoFocusModeBeforeSettings)
                {
                    isTemporarilyDisablingNoFocusMode = true;
                    ApplyNoFocusMode();
                }

                // 设置蒙版为可点击，并添加半透明背景
                BorderSettingsMask.IsHitTestVisible = true;
                BorderSettingsMask.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
                SettingsPanelScrollViewer.ScrollToTop();
                var sb = new Storyboard();

                // 滑动动画
                var slideAnimation = new DoubleAnimation
                {
                    From = BorderSettings.RenderTransform.Value.OffsetX - 490, // 滑动距离
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.6),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTargetProperty(slideAnimation,
                    new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                sb.Children.Add(slideAnimation);

                sb.Completed += (s, _) => { isOpeningOrHidingSettingsPane = false; };

                BorderSettings.RenderTransform = new TranslateTransform();

                isOpeningOrHidingSettingsPane = true;
                sb.Begin(BorderSettings);
            }
        }

        private void BtnThickness_Click(object sender, RoutedEventArgs e) { }

        private bool forceEraser;


        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            //BorderClearInDelete.Visibility = Visibility.Collapsed;

            if (currentMode == 0)
            {
                // 先回到画笔再清屏，避免 TimeMachine 的相关 bug 影响
                if (Pen_Icon.Background == null && StackPanelCanvasControls.Visibility == Visibility.Visible)
                    PenIcon_Click(null, null);
            }
            else
            {
                if (Pen_Icon.Background == null) PenIcon_Click(null, null);
            }

            if (inkCanvas.Strokes.Count != 0)
            {
                var whiteboardIndex = CurrentWhiteboardIndex;
                if (currentMode == 0) whiteboardIndex = 0;
                strokeCollections[whiteboardIndex] = inkCanvas.Strokes.Clone();
            }

            ClearStrokes(false);
            // 保存非笔画元素（如图片）
            var preservedElements = PreserveNonStrokeElements();
            inkCanvas.Children.Clear();
            // 恢复非笔画元素
            RestoreNonStrokeElements(preservedElements);

            if (Settings.Canvas.ClearCanvasAndClearTimeMachine) timeMachine.ClearStrokeHistory();

            CancelSingleFingerDragMode();

        }

        private bool lastIsInMultiTouchMode;

        private void CancelSingleFingerDragMode()
        {
            if (ToggleSwitchDrawShapeBorderAutoHide.IsOn) CollapseBorderDrawShape();

            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

            if (isSingleFingerDragMode) BtnFingerDragMode_Click(BtnFingerDragMode, null);
            isLongPressSelected = false;
        }

        /// <summary>
        /// 重置所有触摸相关状态，
        /// </summary>
        private void ResetTouchStates()
        {
            try
            {
                // 清空触摸点计数器
                dec.Clear();

                // 确保触摸事件能正常响应
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.IsManipulationEnabled = true;

                // 释放所有触摸捕获
                inkCanvas.ReleaseAllTouchCaptures();

                // 恢复UI元素的触摸响应
                ViewboxFloatingBar.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;


            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"重置触摸状态失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private void BtnHideControl_Click(object sender, RoutedEventArgs e)
        {
            if (StackPanelControl.Visibility == Visibility.Visible)
                StackPanelControl.Visibility = Visibility.Hidden;
            else
                StackPanelControl.Visibility = Visibility.Visible;
        }

        private int currentMode;

        /// <summary>
        /// 在退出批注模式后根据配置和当前状态恢复到正常的全屏/工作区行为。
        /// </summary>
        /// <remarks>
        /// 当启用了“避免全屏辅助”且此前应用了全屏限制，并且当前不处于白板模式且不在 PPT 放映状态时，
        /// 本方法会取消画板模式并将主窗口恢复到屏幕的工作区大小，同时重置对应的全屏应用标记。
        /// </remarks>
        private void RestoreFullScreenOnExitAnnotationMode()
        {
            if (Settings.Advanced.IsEnableAvoidFullScreenHelper &&
                isFullScreenApplied &&
                currentMode == 0 && // 不在白板模式
                BtnPPTSlideShowEnd.Visibility != Visibility.Visible) // 不在PPT放映模式
            {
                // 恢复为非画板模式，重新启用全屏限制
                AvoidFullScreenHelper.SetBoardMode(false);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 退出批注模式，恢复到工作区域大小
                    var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                    MainWindow.MoveWindow(new WindowInteropHelper(this).Handle,
                        workingArea.Left, workingArea.Top,
                        workingArea.Width, workingArea.Height, true);
                }), DispatcherPriority.ApplicationIdle);

                isFullScreenApplied = false; // 标记全屏处理已还原
            }
        }

        /// <summary>
        /// 在屏幕模式、白板与黑板模式之间切换并同步相关的 UI 状态与资源处理。
        /// </summary>
        /// <remarks>
        /// 切换过程中会保存/清理/恢复画笔轨迹，显示或隐藏白板/黑板面板、手势面板与 PPT 控件，调整主题与悬浮工具栏可见性，处理全屏/工作区尺寸恢复或进入全屏，以及在进入白板时检查剪贴板并显示粘贴提示。该方法还会触发隐藏/显示墨迹画布的逻辑（通过调用 BtnHideInkCanvas_Click）。
        /// <summary>
        /// 在屏幕、白板（或黑板）与PPT 模式之间切换并同步相关的 UI 状态与画布数据。
        /// </summary>
        /// <remarks>
        /// 切换时会保存/清理/恢复笔迹、显示或隐藏画板侧边栏与手势面板、调整浮动工具栏可见性、根据主题更新退出按钮颜色并在必要时切换全屏行为；还会在进入白板/黑板模式或从白板退出时根据设置处理自动收纳浮动栏与 PPT 按钮的可见性，并在进入画板时检查剪贴板以提示粘贴图像。该方法是窗口的事件处理器，用于响应界面上用于模式切换的按钮点击事件。
        /// </remarks>
        private void BtnSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (GridTransparencyFakeBackground.Background == Brushes.Transparent)
            {
                if (currentMode == 0)
                {
                    currentMode++;
                    GridBackgroundCover.Visibility = Visibility.Collapsed;
                    AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                    AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                    // 在PPT模式下隐藏手势面板和手势按钮
                    AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                    AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                    EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;

                    SaveStrokes(true);
                    ClearStrokes(true);
                    RestoreStrokes(true);


                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                    {
                        BtnSwitch.Content = "黑板";
                        BtnExit.Foreground = Brushes.White;
                    }
                    else
                    {
                        BtnSwitch.Content = "白板";
                        if (isPresentationHaveBlackSpace)
                        {
                            BtnExit.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnExit.Foreground = Brushes.Black;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        }
                    }

                    StackPanelPPTButtons.Visibility = Visibility.Visible;

                    CheckClipboardImageAndShowPasteNotificationWhenEnteringBoard();
                }

                Topmost = true;
                BtnHideInkCanvas_Click(BtnHideInkCanvas, e);
            }
            else
            {
                switch (++currentMode % 2)
                {
                    case 0: //屏幕模式
                        VideoPresenter_OnExitWhiteboardMode();
                        currentMode = 0;
                        GridBackgroundCover.Visibility = Visibility.Collapsed;
                        AnimationsHelper.HideWithSlideAndFade(BlackboardLeftSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardCenterSide);
                        AnimationsHelper.HideWithSlideAndFade(BlackboardRightSide);

                        // 在PPT模式下隐藏手势面板和手势按钮
                        AnimationsHelper.HideWithSlideAndFade(TwoFingerGestureBorder);
                        AnimationsHelper.HideWithSlideAndFade(BoardTwoFingerGestureBorder);
                        EnableTwoFingerGestureBorder.Visibility = Visibility.Collapsed;

                        SaveStrokes();
                        ClearStrokes(true);
                        RestoreStrokes(true);

                        // 退出白板模式时取消全屏（仅在非PPT模式下）
                        if (Settings.Advanced.IsEnableAvoidFullScreenHelper &&
                            BtnPPTSlideShowEnd.Visibility != Visibility.Visible) // 不在PPT放映模式
                        {
                            // 恢复为非画板模式，重新启用全屏限制
                            AvoidFullScreenHelper.SetBoardMode(false);

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                // 退出白板模式，恢复到工作区域大小
                                var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                                MainWindow.MoveWindow(new WindowInteropHelper(this).Handle,
                                    workingArea.Left, workingArea.Top,
                                    workingArea.Width, workingArea.Height, true);
                            }), DispatcherPriority.ApplicationIdle);

                            isFullScreenApplied = false; // 标记全屏处理已还原
                        }

                        // 在屏幕模式下恢复基础浮动栏的显示
                        ViewboxFloatingBar.Visibility = Visibility.Visible;

                        // 退出白板时自动收纳功能 - 等待浮动栏完全展开后再收纳
                        // 当处于PPT放映模式时，不自动收纳
                        if (Settings.Automation.IsAutoFoldWhenExitWhiteboard && !isFloatingBarFolded &&
                            BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                        {
                            // 使用异步延迟，等待浮动栏展开动画完成后再收纳
                            Task.Run(async () =>
                            {
                                await Task.Delay(700);
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    FoldFloatingBar_MouseUp(new object(), null);
                                });
                            });
                        }

                        if (BtnSwitchTheme.Content.ToString() == "浅色")
                        {
                            BtnSwitch.Content = "黑板";
                            BtnExit.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnSwitch.Content = "白板";
                            if (isPresentationHaveBlackSpace)
                            {
                                BtnExit.Foreground = Brushes.White;
                                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                            }
                            else
                            {
                                BtnExit.Foreground = Brushes.Black;
                                ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                            }
                        }

                        StackPanelPPTButtons.Visibility = Visibility.Visible;
                        Topmost = true;
                        break;
                    case 1: //黑板或白板模式
                        currentMode = 1;
                        GridBackgroundCover.Visibility = Visibility.Visible;
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardLeftSide);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardCenterSide);
                        AnimationsHelper.ShowWithSlideFromBottomAndFade(BlackboardRightSide);

                        SaveStrokes(true);
                        ClearStrokes(true);

                        RestoreStrokes();

                        // 进入白板模式时全屏（仅在非PPT模式下）
                        if (Settings.Advanced.IsEnableAvoidFullScreenHelper &&
                            BtnPPTSlideShowEnd.Visibility != Visibility.Visible) // 不在PPT放映模式
                        {
                            // 设置为画板模式，允许全屏操作
                            AvoidFullScreenHelper.SetBoardMode(true);
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                MainWindow.MoveWindow(new WindowInteropHelper(this).Handle, 0, 0,
                                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                                    System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
                            }), DispatcherPriority.ApplicationIdle);

                            isFullScreenApplied = true; // 标记已应用全屏处理
                        }

                        ViewboxFloatingBar.Visibility = Visibility.Collapsed;

                        BtnSwitch.Content = "屏幕";
                        if (BtnSwitchTheme.Content.ToString() == "浅色")
                        {
                            BtnExit.Foreground = Brushes.White;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
                        }
                        else
                        {
                            BtnExit.Foreground = Brushes.Black;
                            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Light;
                        }

                        if (Settings.Canvas.UsingWhiteboard)
                        {
                            // 如果有自定义背景色并且是白板模式，应用自定义背景色
                            if (CustomBackgroundColor.HasValue)
                            {
                                GridBackgroundCover.Background = new SolidColorBrush(CustomBackgroundColor.Value);
                            }
                            // 白板模式下设置墨迹颜色为黑色
                            CheckLastColor(0);
                            forceEraser = false;
                            ColorSwitchCheck();
                        }
                        else
                        {
                            // 黑板模式下设置墨迹颜色为白色
                            CheckLastColor(5);
                            forceEraser = false;
                            ColorSwitchCheck();
                        }

                        StackPanelPPTButtons.Visibility = Visibility.Collapsed;

                        if (Settings.Advanced.EnableUIAccessTopMost)
                        {
                            Topmost = true;
                        }
                        else
                        {
                            Topmost = false;
                        }

                        CheckClipboardImageAndShowPasteNotificationWhenEnteringBoard();
                        break;
                }
            }
        }

        private int BoundsWidth = 5;

        private void BtnHideInkCanvas_Click(object sender, RoutedEventArgs e)
        {
            if (GridTransparencyFakeBackground.Background == Brushes.Transparent)
            {
                // 进入批注模式
                GridTransparencyFakeBackground.Opacity = 1;
                GridTransparencyFakeBackground.Background = new SolidColorBrush(StringToColor("#01FFFFFF"));
                inkCanvas.IsHitTestVisible = true;
                inkCanvas.Visibility = Visibility.Visible;

                GridBackgroundCoverHolder.Visibility = Visibility.Visible;

                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;

                if (GridBackgroundCover.Visibility == Visibility.Collapsed)
                {
                    if (BtnSwitchTheme.Content.ToString() == "浅色")
                        BtnSwitch.Content = "黑板";
                    else
                        BtnSwitch.Content = "白板";
                    StackPanelPPTButtons.Visibility = Visibility.Visible;
                }
                else
                {
                    BtnSwitch.Content = "屏幕";
                    StackPanelPPTButtons.Visibility = Visibility.Collapsed;
                }

                BtnHideInkCanvas.Content = "隐藏\n画板";

                // 进入批注模式时的全屏处理（仅当未应用过全屏处理时）
                if (Settings.Advanced.IsEnableAvoidFullScreenHelper && !isFullScreenApplied)
                {
                    // 设置为画板模式，允许全屏操作
                    AvoidFullScreenHelper.SetBoardMode(true);
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        MainWindow.MoveWindow(new WindowInteropHelper(this).Handle, 0, 0,
                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height, true);
                    }), DispatcherPriority.ApplicationIdle);

                    isFullScreenApplied = true; // 标记已应用全屏处理
                }
            }
            else
            {
                // Auto-clear Strokes 要等待截图完成再清理笔记
                if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible)
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode)
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                                SaveScreenShot(true);

                            //BtnClear_Click(null, null);
                        }

                    inkCanvas.IsHitTestVisible = true;
                    inkCanvas.Visibility = Visibility.Visible;
                }
                else
                {
                    if (isLoaded && Settings.Automation.IsAutoClearWhenExitingWritingMode &&
                        !Settings.PowerPointSettings.IsNoClearStrokeOnSelectWhenInPowerPoint)
                        if (inkCanvas.Strokes.Count > 0)
                        {
                            if (Settings.Automation.IsAutoSaveStrokesAtClear && inkCanvas.Strokes.Count >
                                Settings.Automation.MinimumAutomationStrokeNumber)
                                SaveScreenShot(true);

                            //BtnClear_Click(null, null);
                        }


                    if (Settings.PowerPointSettings.IsShowStrokeOnSelectInPowerPoint)
                    {
                        inkCanvas.Visibility = Visibility.Visible;
                        inkCanvas.IsHitTestVisible = true;
                    }
                    else
                    {
                        inkCanvas.IsHitTestVisible = true;
                        inkCanvas.Visibility = Visibility.Visible;
                    }
                }

                GridTransparencyFakeBackground.Opacity = 0;
                GridTransparencyFakeBackground.Background = Brushes.Transparent;

                GridBackgroundCoverHolder.Visibility = Visibility.Collapsed;

                // 退出批注模式时的全屏还原
                RestoreFullScreenOnExitAnnotationMode();

                if (currentMode != 0)
                {
                    SaveStrokes();
                    RestoreStrokes(true);
                }

                if (BtnSwitchTheme.Content.ToString() == "浅色")
                    BtnSwitch.Content = "黑板";
                else
                    BtnSwitch.Content = "白板";

                StackPanelPPTButtons.Visibility = Visibility.Visible;
                BtnHideInkCanvas.Content = "显示\n画板";
            }

            if (GridTransparencyFakeBackground.Background == Brushes.Transparent)
            {
                StackPanelCanvasControls.Visibility = Visibility.Collapsed;
                CheckEnableTwoFingerGestureBtnVisibility(false);
                HideSubPanels("cursor");

                if (currentMode == 0)
                {
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                }
            }
            else
            {
                AnimationsHelper.ShowWithSlideFromLeftAndFade(StackPanelCanvasControls);
                CheckEnableTwoFingerGestureBtnVisibility(true);

                if (currentMode == 0)
                {
                    ViewboxFloatingBar.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnSwitchSide_Click(object sender, RoutedEventArgs e)
        {
            if (ViewBoxStackPanelMain.HorizontalAlignment == HorizontalAlignment.Right)
            {
                ViewBoxStackPanelMain.HorizontalAlignment = HorizontalAlignment.Left;
                ViewBoxStackPanelShapes.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                ViewBoxStackPanelMain.HorizontalAlignment = HorizontalAlignment.Right;
                ViewBoxStackPanelShapes.HorizontalAlignment = HorizontalAlignment.Left;
            }
        }

        private void StackPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (((StackPanel)sender).Visibility == Visibility.Visible)
                GridForLeftSideReservedSpace.Visibility = Visibility.Collapsed;
            else
                GridForLeftSideReservedSpace.Visibility = Visibility.Visible;
        }

        #endregion

        private void InsertImageOptions_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Check if the image options panel is currently visible
            bool isImagePanelVisible = BoardImageOptionsPanel.Visibility == Visibility.Visible;

            // Toggle the image options panel
            if (isImagePanelVisible)
            {
                // Panel was visible, so hide it with animation
                AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);
            }
            else
            {
                // Panel was hidden, so hide other panels and show this one
                HideSubPanels();
                AnimationsHelper.ShowWithSlideFromBottomAndFade(BoardImageOptionsPanel);
            }
        }

        private void CloseImageOptionsPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);
        }

        private async void ImageOptionScreenshot_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Hide the options panel
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            // Wait a bit for the panel to hide
            await Task.Delay(100);

            // Capture screenshot and insert to canvas
            await CaptureScreenshotAndInsert();
        }

        private async void ImageOptionSelectFile_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Hide the options panel
            AnimationsHelper.HideWithSlideAndFade(BoardImageOptionsPanel);

            // Open file dialog to select image
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                Image image = await CreateAndCompressImageAsync(filePath);
                if (image != null)
                {
                    string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                    image.Name = timestamp;

                    // 初始化TransformGroup
                    if (image is FrameworkElement element)
                    {
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new ScaleTransform(1, 1));
                        transformGroup.Children.Add(new TranslateTransform(0, 0));
                        transformGroup.Children.Add(new RotateTransform(0));
                        element.RenderTransform = transformGroup;
                    }

                    CenterAndScaleElement(image);

                    // 设置图片属性，避免被InkCanvas选择系统处理
                    image.IsHitTestVisible = true;
                    image.Focusable = false;

                    // 初始化InkCanvas选择设置
                    if (inkCanvas != null)
                    {
                        // 清除当前选择，避免显示控制点
                        inkCanvas.Select(new StrokeCollection());
                        // 同时通过图片的IsHitTestVisible和Focusable属性来避免InkCanvas选择系统的干扰
                        inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    }

                    inkCanvas.Children.Add(image);

                    // 绑定事件处理器
                    if (image is FrameworkElement elementForEvents)
                    {
                        // 鼠标事件
                        elementForEvents.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                        elementForEvents.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                        elementForEvents.MouseMove += Element_MouseMove;
                        elementForEvents.MouseWheel += Element_MouseWheel;

                        // 触摸事件
                        elementForEvents.TouchDown += Element_TouchDown;
                        elementForEvents.TouchUp += Element_TouchUp;
                        elementForEvents.IsManipulationEnabled = true;
                        elementForEvents.ManipulationDelta += Element_ManipulationDelta;
                        elementForEvents.ManipulationCompleted += Element_ManipulationCompleted;

                        // 设置光标
                        elementForEvents.Cursor = Cursors.Hand;
                    }

                    timeMachine.CommitElementInsertHistory(image);

                    // 插入图片后切换到选择模式并刷新浮动栏高光显示
                    SetCurrentToolMode(InkCanvasEditingMode.Select);
                    UpdateCurrentToolMode("select");
                    HideSubPanels("select");
                }
            }
        }

        // 插入图片方法
        private async void InsertImage_MouseUp_New(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                Image image = await CreateAndCompressImageAsync(filePath);
                if (image != null)
                {
                    string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                    image.Name = timestamp;

                    // 初始化TransformGroup
                    if (image is FrameworkElement element)
                    {
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new ScaleTransform(1, 1));
                        transformGroup.Children.Add(new TranslateTransform(0, 0));
                        transformGroup.Children.Add(new RotateTransform(0));
                        element.RenderTransform = transformGroup;
                    }

                    CenterAndScaleElement(image);

                    // 设置图片属性，避免被InkCanvas选择系统处理
                    image.IsHitTestVisible = true;
                    image.Focusable = false;

                    // 初始化InkCanvas选择设置
                    if (inkCanvas != null)
                    {
                        // 清除当前选择，避免显示控制点
                        inkCanvas.Select(new StrokeCollection());
                        // 设置编辑模式为非选择模式
                        inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    }

                    inkCanvas.Children.Add(image);

                    // 绑定事件处理器
                    if (image is FrameworkElement elementForEvents)
                    {
                        // 鼠标事件
                        elementForEvents.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                        elementForEvents.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                        elementForEvents.MouseMove += Element_MouseMove;
                        elementForEvents.MouseWheel += Element_MouseWheel;

                        // 触摸事件
                        elementForEvents.TouchDown += Element_TouchDown;
                        elementForEvents.TouchUp += Element_TouchUp;
                        elementForEvents.IsManipulationEnabled = true;
                        elementForEvents.ManipulationDelta += Element_ManipulationDelta;
                        elementForEvents.ManipulationCompleted += Element_ManipulationCompleted;

                        // 设置光标
                        elementForEvents.Cursor = Cursors.Hand;
                    }

                    timeMachine.CommitElementInsertHistory(image);

                    // 插入图片后切换到选择模式并刷新浮动栏高光显示
                    SetCurrentToolMode(InkCanvasEditingMode.Select);
                    UpdateCurrentToolMode("select");
                    HideSubPanels("select");
                }
            }
        }

        // Keep the old method for backward compatibility
        private async void InsertImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                Image image = await CreateAndCompressImageAsync(filePath); // 补充image定义
                if (image != null)
                {
                    string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                    image.Name = timestamp;

                    // 初始化TransformGroup
                    if (image is FrameworkElement element)
                    {
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(new ScaleTransform(1, 1));
                        transformGroup.Children.Add(new TranslateTransform(0, 0));
                        transformGroup.Children.Add(new RotateTransform(0));
                        element.RenderTransform = transformGroup;
                    }

                    CenterAndScaleElement(image);

                    // 设置图片属性，避免被InkCanvas选择系统处理
                    image.IsHitTestVisible = true;
                    image.Focusable = false;

                    // 初始化InkCanvas选择设置
                    if (inkCanvas != null)
                    {
                        // 清除当前选择，避免显示控制点
                        inkCanvas.Select(new StrokeCollection());
                        // 设置编辑模式为非选择模式
                        inkCanvas.EditingMode = InkCanvasEditingMode.None;
                    }

                    inkCanvas.Children.Add(image);

                    // 绑定事件处理器
                    if (image is FrameworkElement elementForEvents)
                    {
                        // 鼠标事件
                        elementForEvents.MouseLeftButtonDown += Element_MouseLeftButtonDown;
                        elementForEvents.MouseLeftButtonUp += Element_MouseLeftButtonUp;
                        elementForEvents.MouseMove += Element_MouseMove;
                        elementForEvents.MouseWheel += Element_MouseWheel;

                        // 触摸事件
                        elementForEvents.TouchDown += Element_TouchDown;
                        elementForEvents.TouchUp += Element_TouchUp;
                        elementForEvents.IsManipulationEnabled = true;
                        elementForEvents.ManipulationDelta += Element_ManipulationDelta;
                        elementForEvents.ManipulationCompleted += Element_ManipulationCompleted;

                        // 设置光标
                        elementForEvents.Cursor = Cursors.Hand;
                    }

                    timeMachine.CommitElementInsertHistory(image);

                    // 插入图片后切换到选择模式并刷新浮动栏高光显示
                    SetCurrentToolMode(InkCanvasEditingMode.Select);
                    UpdateCurrentToolMode("select");
                    HideSubPanels("select");
                }
            }
        }

        #region 动态按钮位置计算和高光显示

        /// <summary>
        /// 获取浮动栏中指定按钮的位置
        /// </summary>
        /// <param name="buttonName">按钮的名称</param>
        /// <returns>按钮在浮动栏中的相对位置</returns>
        private double GetFloatingBarButtonPosition(string buttonName)
        {
            try
            {
                // 获取浮动栏容器
                var floatingBarPanel = StackPanelFloatingBar;
                if (floatingBarPanel == null) return 0;

                double currentPosition = 0;

                // 遍历浮动栏中的所有子元素
                foreach (var child in floatingBarPanel.Children)
                {
                    if (child is UIElement element)
                    {
                        // 检查是否是我们要找的按钮
                        if (IsTargetButton(element, buttonName))
                        {
                            return currentPosition;
                        }

                        // 累加当前元素的位置
                        currentPosition += GetElementWidth(element);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取按钮位置失败: {ex.Message}", LogHelper.LogType.Error);
                return 0;
            }
        }

        /// <summary>
        /// 检查元素是否是目标按钮
        /// </summary>
        private bool IsTargetButton(UIElement element, string buttonName)
        {
            if (element is FrameworkElement fe)
            {
                return fe.Name == buttonName;
            }
            return false;
        }

        /// <summary>
        /// 获取元素的宽度
        /// </summary>
        private double GetElementWidth(UIElement element)
        {
            if (element is FrameworkElement fe)
            {
                // 对于SimpleStackPanel，使用其实际宽度
                if (fe.GetType().Name == "SimpleStackPanel")
                {
                    return fe.ActualWidth > 0 ? fe.ActualWidth : 28; // 默认宽度28
                }

                // 对于其他元素，使用其宽度或默认宽度
                return fe.ActualWidth > 0 ? fe.ActualWidth : 28;
            }
            return 28; // 默认宽度
        }

        /// <summary>
        /// 设置浮动栏高光显示位置
        /// </summary>
        /// <param name="mode">模式名称</param>
        private void SetFloatingBarHighlightPosition(string mode)
        {
            try
            {
                if (FloatingbarSelectionBG == null) return;

                // 检查浮动栏是否处于收起状态
                if (isFloatingBarFolded || (BorderFloatingBarMainControls != null && BorderFloatingBarMainControls.Visibility == Visibility.Collapsed))
                {
                    FloatingbarSelectionBG.Visibility = Visibility.Hidden;
                    return;
                }

                double position = 0;
                double buttonWidth = 28; // 每个按钮的默认宽度
                double highlightWidth = 28; // 高光的默认宽度

                // 检查快捷调色盘是否显示及其实际宽度
                bool isQuickColorPaletteVisible = false;
                double quickColorPaletteWidth = 0;

                if (QuickColorPalettePanel != null && QuickColorPalettePanel.Visibility == Visibility.Visible)
                {
                    isQuickColorPaletteVisible = true;
                    quickColorPaletteWidth = QuickColorPalettePanel.ActualWidth > 0 ? QuickColorPalettePanel.ActualWidth : 60;
                }
                else if (QuickColorPaletteSingleRowPanel != null && QuickColorPaletteSingleRowPanel.Visibility == Visibility.Visible)
                {
                    isQuickColorPaletteVisible = true;
                    quickColorPaletteWidth = QuickColorPaletteSingleRowPanel.ActualWidth > 0 ? QuickColorPaletteSingleRowPanel.ActualWidth : 120;
                }

                // 获取实际按钮宽度，如果获取不到则使用默认值，同时考虑按钮的可见性
                double cursorWidth = (Cursor_Icon?.Visibility == Visibility.Visible && Cursor_Icon?.ActualWidth > 0) ? Cursor_Icon.ActualWidth : 0;
                double penWidth = (Pen_Icon?.Visibility == Visibility.Visible && Pen_Icon?.ActualWidth > 0) ? Pen_Icon.ActualWidth : 0;
                double deleteWidth = (SymbolIconDelete?.Visibility == Visibility.Visible && SymbolIconDelete?.ActualWidth > 0) ? SymbolIconDelete.ActualWidth : 0;
                double eraserWidth = (Eraser_Icon?.Visibility == Visibility.Visible && Eraser_Icon?.ActualWidth > 0) ? Eraser_Icon.ActualWidth : 0;
                double eraserByStrokesWidth = (EraserByStrokes_Icon?.Visibility == Visibility.Visible && EraserByStrokes_Icon?.ActualWidth > 0) ? EraserByStrokes_Icon.ActualWidth : 0;
                double selectWidth = (SymbolIconSelect?.Visibility == Visibility.Visible && SymbolIconSelect?.ActualWidth > 0) ? SymbolIconSelect.ActualWidth : 0;

                // 获取高光的实际宽度
                double actualHighlightWidth = FloatingbarSelectionBG.ActualWidth > 0 ? FloatingbarSelectionBG.ActualWidth : highlightWidth;

                double marginOffset = 0;

                // 快捷调色盘的Margin：Margin="4,0,4,0"，所以总宽度需要加上8像素
                double quickColorPaletteTotalWidth = isQuickColorPaletteVisible ? quickColorPaletteWidth + 8 : 0;

                // 根据模式计算位置，确保高光居中对齐按钮
                switch (mode)
                {
                    case "cursor":
                        // 鼠标按钮位置：marginOffset + (cursorWidth - actualHighlightWidth) / 2
                        position = marginOffset + (cursorWidth - actualHighlightWidth) / 2;
                        break;
                    case "pen":
                    case "color":
                        // 批注按钮位置：marginOffset + cursorWidth + (penWidth - actualHighlightWidth) / 2
                        position = marginOffset + cursorWidth + (penWidth - actualHighlightWidth) / 2;
                        break;
                    case "eraser":
                        if (isQuickColorPaletteVisible)
                        {
                            // 有快捷调色盘时：鼠标 + 批注 + 快捷调色盘(包含Margin) + 清空 + (面积擦 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + quickColorPaletteTotalWidth + deleteWidth + (eraserWidth - actualHighlightWidth) / 2;
                        }
                        else
                        {
                            // 没有快捷调色盘时：鼠标 + 批注 + 清空 + (面积擦 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + deleteWidth + (eraserWidth - actualHighlightWidth) / 2;
                        }
                        break;
                    case "eraserByStrokes":
                        if (isQuickColorPaletteVisible)
                        {
                            // 有快捷调色盘时：鼠标 + 批注 + 快捷调色盘(包含Margin) + 清空 + 面积擦 + (线擦 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + quickColorPaletteTotalWidth + deleteWidth + eraserWidth + (eraserByStrokesWidth - actualHighlightWidth) / 2;
                        }
                        else
                        {
                            // 没有快捷调色盘时：鼠标 + 批注 + 清空 + 面积擦 + (线擦 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + deleteWidth + eraserWidth + (eraserByStrokesWidth - actualHighlightWidth) / 2;
                        }
                        break;
                    case "select":
                        if (isQuickColorPaletteVisible)
                        {
                            // 有快捷调色盘时：鼠标 + 批注 + 快捷调色盘(包含Margin) + 清空 + 面积擦 + 线擦 + (套索选 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + quickColorPaletteTotalWidth + deleteWidth + eraserWidth + eraserByStrokesWidth + (selectWidth - actualHighlightWidth) / 2;
                        }
                        else
                        {
                            // 没有快捷调色盘时：鼠标 + 批注 + 清空 + 面积擦 + 线擦 + (套索选 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + deleteWidth + eraserWidth + eraserByStrokesWidth + (selectWidth - actualHighlightWidth) / 2;
                        }
                        break;
                    case "shape":
                        if (isQuickColorPaletteVisible)
                        {
                            // 有快捷调色盘时：鼠标 + 批注 + 快捷调色盘(包含Margin) + 清空 + 面积擦 + 线擦 + 套索选 + (几何 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + quickColorPaletteTotalWidth + deleteWidth + eraserWidth + eraserByStrokesWidth + selectWidth + (buttonWidth - actualHighlightWidth) / 2;
                        }
                        else
                        {
                            // 没有快捷调色盘时：鼠标 + 批注 + 清空 + 面积擦 + 线擦 + 套索选 + (几何 - 高光) / 2
                            position = marginOffset + cursorWidth + penWidth + deleteWidth + eraserWidth + eraserByStrokesWidth + selectWidth + (buttonWidth - actualHighlightWidth) / 2;
                        }
                        break;
                    default:
                        position = marginOffset;
                        break;
                }

                // 根据主题设置高光颜色
                Color highlightBackgroundColor;
                Color highlightBarColor;
                bool isDarkTheme = Settings.Appearance.Theme == 1 ||
                                   (Settings.Appearance.Theme == 2 && !IsSystemThemeLight());

                if (isDarkTheme)
                {
                    highlightBackgroundColor = Color.FromArgb(21, 102, 204, 255);
                    highlightBarColor = Color.FromRgb(102, 204, 255);
                }
                else
                {
                    highlightBackgroundColor = Color.FromArgb(21, 59, 130, 246);
                    highlightBarColor = Color.FromRgb(37, 99, 235);
                }

                // 设置高光背景颜色
                FloatingbarSelectionBG.Background = new SolidColorBrush(highlightBackgroundColor);
                if (FloatingbarSelectionBG.Child is System.Windows.Controls.Canvas canvas && canvas.Children.Count > 0)
                {
                    var firstChild = canvas.Children[0];
                    if (firstChild is Border innerBorder)
                    {
                        innerBorder.Background = new SolidColorBrush(highlightBarColor);
                    }
                }

                // 设置高光位置
                FloatingbarSelectionBG.Visibility = Visibility.Visible;
                System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, position);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"设置高光位置失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 隐藏浮动栏高光显示
        /// </summary>
        private void HideFloatingBarHighlight()
        {
            if (FloatingbarSelectionBG != null)
            {
                FloatingbarSelectionBG.Visibility = Visibility.Hidden;
                System.Windows.Controls.Canvas.SetLeft(FloatingbarSelectionBG, 0);
            }
        }

        /// <summary>
        /// 获取当前选中的模式
        /// </summary>
        /// <returns>当前选中的模式名称</returns>
        public string GetCurrentSelectedMode()
        {
            try
            {
                // 优先使用缓存的模式，避免在浮动栏刷新时返回过时的模式信息
                if (!string.IsNullOrEmpty(_currentToolMode))
                {
                    return _currentToolMode;
                }

                // 如果缓存为空，则从inkCanvas状态推断模式
                if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
                {
                    return "select";
                }

                if (inkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                {
                    // 检查是否是荧光笔模式
                    if (drawingAttributes != null && drawingAttributes.IsHighlighter)
                    {
                        return "color";
                    }

                    return "pen";
                }

                if (inkCanvas.EditingMode == InkCanvasEditingMode.EraseByPoint)
                {
                    // 检查是面积擦还是线擦
                    if (Eraser_Icon != null && Eraser_Icon.Visibility == Visibility.Visible)
                    {
                        return "eraser";
                    }

                    if (EraserByStrokes_Icon != null && EraserByStrokes_Icon.Visibility == Visibility.Visible)
                    {
                        return "eraserByStrokes";
                    }
                }
                else if (inkCanvas.EditingMode == InkCanvasEditingMode.None)
                {
                    return "cursor";
                }
                else if (drawingShapeMode != 0)
                {
                    return "shape";
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取当前选中模式失败: {ex.Message}", LogHelper.LogType.Error);
            }

            return "cursor"; // 默认返回鼠标模式
        }

        /// <summary>
        /// 更新当前工具模式缓存
        /// </summary>
        /// <param name="mode">模式名称</param>
        private void UpdateCurrentToolMode(string mode)
        {
            _currentToolMode = mode;
        }

        #endregion

        /// <summary>
        /// 强制禁用所有双指手势功能（当多指书写模式启用时）
        /// </summary>
        private void ForceDisableTwoFingerGestures()
        {
            // 强制关闭所有双指手势设置
            Settings.Gesture.IsEnableTwoFingerTranslate = false;
            Settings.Gesture.IsEnableTwoFingerZoom = false;
            Settings.Gesture.IsEnableTwoFingerRotation = false;

            // 更新UI开关状态
            if (ToggleSwitchEnableTwoFingerTranslate != null)
                ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            if (ToggleSwitchEnableTwoFingerZoom != null)
                ToggleSwitchEnableTwoFingerZoom.IsOn = false;
            if (ToggleSwitchEnableTwoFingerRotation != null)
                ToggleSwitchEnableTwoFingerRotation.IsOn = false;

            // 更新设置窗口中的开关状态
            if (BoardToggleSwitchEnableTwoFingerTranslate != null)
                BoardToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            if (BoardToggleSwitchEnableTwoFingerZoom != null)
                BoardToggleSwitchEnableTwoFingerZoom.IsOn = false;
            if (BoardToggleSwitchEnableTwoFingerRotation != null)
                BoardToggleSwitchEnableTwoFingerRotation.IsOn = false;
        }

    }
}