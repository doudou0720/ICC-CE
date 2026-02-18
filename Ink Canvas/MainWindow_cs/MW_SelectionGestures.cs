using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        #region Floating Control

        private object lastBorderMouseDownObject;

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // 如果发送者是 RandomDrawPanel 或 SingleDrawPanel，且它们被隐藏，则不处理事件
            if (sender is SimpleStackPanel panel)
            {
                if ((panel == RandomDrawPanel || panel == SingleDrawPanel) &&
                    panel.Visibility != Visibility.Visible)
                {
                    return;
                }
            }

            lastBorderMouseDownObject = sender;
        }


        private void BorderStrokeSelectionClone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            try
            {
                var strokes = inkCanvas.GetSelectedStrokes();
                if (strokes.Count > 0)
                {
                    // 直接执行克隆操作，与图片克隆保持一致
                    CloneStrokes(strokes);
                    LogHelper.WriteLogToFile($"墨迹克隆完成: {strokes.Count} 个墨迹");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"墨迹克隆失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private void BorderStrokeSelectionCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var strokes = inkCanvas.GetSelectedStrokes();
            inkCanvas.Select(new StrokeCollection());
            CloneStrokesToNewBoard(strokes);
        }

        private void BorderStrokeSelectionDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            SymbolIconDelete_MouseUp(sender, e);
        }

        private void GridPenWidthDecrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            ChangeStrokeThickness(0.8);
        }

        private void GridPenWidthIncrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            ChangeStrokeThickness(1.25);
        }

        private void ChangeStrokeThickness(double multipler)
        {
            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                var newWidth = stroke.DrawingAttributes.Width * multipler;
                var newHeight = stroke.DrawingAttributes.Height * multipler;
                if (!(newWidth >= DrawingAttributes.MinWidth) || !(newWidth <= DrawingAttributes.MaxWidth)
                                                              || !(newHeight >= DrawingAttributes.MinHeight) ||
                                                              !(newHeight <= DrawingAttributes.MaxHeight)) continue;
                stroke.DrawingAttributes.Width = newWidth;
                stroke.DrawingAttributes.Height = newHeight;
            }

            if (DrawingAttributesHistory.Count > 0)
            {

                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        private void GridPenWidthRestore_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = inkCanvas.DefaultDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Height;
            }
        }

        private void ImageFlipHorizontal_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(-1, 1, center.X, center.Y); // 缩放

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                //var collecion = new StrokeCollection();
                //foreach (var item in DrawingAttributesHistory)
                //{
                //    collecion.Add(item.Key);
                //}
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }

            //updateBorderStrokeSelectionControlLocation();
        }

        private void ImageFlipVertical_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.ScaleAt(1, -1, center.X, center.Y); // 缩放

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        // ... existing code ...
        private void ImageRotate45_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(45, center.X, center.Y); // 顺时针旋转45度

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        private void ImageRotate90_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var m = new Matrix();

            // Find center of element and then transform to get current location of center
            var fe = e.Source as FrameworkElement;
            var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
            center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
            center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

            // Update matrix to reflect translation/rotation
            m.RotateAt(90, center.X, center.Y); // 旋转

            var targetStrokes = inkCanvas.GetSelectedStrokes();
            foreach (var stroke in targetStrokes) stroke.Transform(m, false);

            if (DrawingAttributesHistory.Count > 0)
            {
                var collecion = new StrokeCollection();
                foreach (var item in DrawingAttributesHistory)
                {
                    collecion.Add(item.Key);
                }

                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        #endregion

        private bool isGridInkCanvasSelectionCoverMouseDown;
        private bool isStrokeDragging = false;
        private Point strokeDragStartPoint;
        private StrokeCollection StrokesSelectionClone = new StrokeCollection();

        // 选择框和选择点相关变量
        private bool isResizing = false;
        private string currentResizeHandle = "";
        private Point resizeStartPoint;
        private Rect originalSelectionBounds;

        private void GridInkCanvasSelectionCover_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isGridInkCanvasSelectionCoverMouseDown = true;

            // 检查是否有选中的墨迹
            if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                // 获取鼠标点击位置
                var clickPoint = e.GetPosition(inkCanvas);
                var selectionBounds = inkCanvas.GetSelectionBounds();

                // 检查点击位置是否在选择框边界内
                if (clickPoint.X >= selectionBounds.Left &&
                    clickPoint.X <= selectionBounds.Right &&
                    clickPoint.Y >= selectionBounds.Top &&
                    clickPoint.Y <= selectionBounds.Bottom)
                {
                    // 只有在选择框边界内才允许拖动
                    isStrokeDragging = true;
                    strokeDragStartPoint = clickPoint;
                    GridInkCanvasSelectionCover.CaptureMouse();
                    GridInkCanvasSelectionCover.Cursor = Cursors.SizeAll;
                }
                else
                {
                    // 点击在选择框外，取消选择
                    inkCanvas.Select(new StrokeCollection());
                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void GridInkCanvasSelectionCover_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isGridInkCanvasSelectionCoverMouseDown) return;

            // 如果正在拖动墨迹，执行拖动操作
            if (isStrokeDragging && GridInkCanvasSelectionCover.IsMouseCaptured)
            {
                var currentPoint = e.GetPosition(inkCanvas);
                var delta = currentPoint - strokeDragStartPoint;

                // 创建变换矩阵
                var matrix = new Matrix();
                matrix.Translate(delta.X, delta.Y);

                // 对选中的墨迹应用变换
                var selectedStrokes = inkCanvas.GetSelectedStrokes();
                foreach (var stroke in selectedStrokes)
                {
                    stroke.Transform(matrix, false);
                }

                // 更新选中栏位置
                updateBorderStrokeSelectionControlLocation();

                // 更新起始点
                strokeDragStartPoint = currentPoint;
            }
            else if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                // 当鼠标在选中区域移动时，更新墨迹选中栏位置
                updateBorderStrokeSelectionControlLocation();
            }
        }

        private void GridInkCanvasSelectionCover_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isGridInkCanvasSelectionCoverMouseDown) return;

            // 结束墨迹拖动
            if (isStrokeDragging)
            {
                isStrokeDragging = false;
                GridInkCanvasSelectionCover.ReleaseMouseCapture();
                GridInkCanvasSelectionCover.Cursor = Cursors.Arrow;
            }

            isGridInkCanvasSelectionCoverMouseDown = false;

            // 只有在没有选中墨迹时才隐藏选中栏
            if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = true;
            drawingShapeMode = 0;
            inkCanvas.IsManipulationEnabled = false;
            if (inkCanvas.EditingMode == InkCanvasEditingMode.Select)
            {
                if (inkCanvas.GetSelectedStrokes().Count == inkCanvas.Strokes.Count)
                {
                    // 使用集中化的工具模式切换方法
                    SetCurrentToolMode(InkCanvasEditingMode.Ink,
                        () => { SetCurrentToolMode(InkCanvasEditingMode.Select); });
                }
                else
                {
                    var selectedStrokes = new StrokeCollection();
                    foreach (var stroke in inkCanvas.Strokes)
                        if (stroke.GetBounds().Width > 0 && stroke.GetBounds().Height > 0)
                            selectedStrokes.Add(stroke);
                    inkCanvas.Select(selectedStrokes);
                }
            }
            else
            {
                // 使用集中化的工具模式切换方法
                SetCurrentToolMode(InkCanvasEditingMode.Select);
            }
        }

        private double BorderStrokeSelectionControlWidth = 490.0;
        private double BorderStrokeSelectionControlHeight = 80.0;
        private bool isProgramChangeStrokeSelection;

        private void inkCanvas_SelectionChanged(object sender, EventArgs e)
        {
            if (isProgramChangeStrokeSelection) return;

            // 优先检查墨迹选择状态
            if (inkCanvas.GetSelectedStrokes().Count > 0)
            {
                // 有墨迹被选中，清除图片选择状态
                if (currentSelectedElement != null)
                {
                    currentSelectedElement = null;
                    // 隐藏图片选择工具栏
                    if (BorderImageSelectionControl != null)
                    {
                        BorderImageSelectionControl.Visibility = Visibility.Collapsed;
                    }
                }

                // 显示墨迹选择栏和选择框
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                BorderStrokeSelectionClone.Background = Brushes.Transparent;
                updateBorderStrokeSelectionControlLocation();
                UpdateSelectionDisplay();
                return;
            }

            // 检查是否有图片元素被选中（通过InkCanvas的选中元素）
            var selectedElements = inkCanvas.GetSelectedElements();
            bool hasImageElement = selectedElements.Any(element => element is Image);

            // 如果有图片元素被选中，不显示选择框
            if (hasImageElement)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                HideSelectionDisplay();
                return;
            }

            // 检查是否有图片元素被选中（通过currentSelectedElement）
            if (currentSelectedElement != null && currentSelectedElement is Image)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                HideSelectionDisplay();
                return;
            }

            // 没有选中任何内容，隐藏选择框
            GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            HideSelectionDisplay();
        }



        private void updateBorderStrokeSelectionControlLocation()
        {
            var borderLeft = (inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Right -
                              BorderStrokeSelectionControlWidth) / 2;
            var borderTop = inkCanvas.GetSelectionBounds().Bottom + 10; // 在墨迹下方10像素处显示
            if (borderLeft < 0) borderLeft = 0;
            if (borderTop < 0) borderTop = 0;
            if (Width - borderLeft < BorderStrokeSelectionControlWidth || double.IsNaN(borderLeft))
                borderLeft = Width - BorderStrokeSelectionControlWidth;
            if (Height - borderTop < BorderStrokeSelectionControlHeight || double.IsNaN(borderTop))
                borderTop = Height - BorderStrokeSelectionControlHeight;

            // 确保墨迹选中栏始终显示在墨迹下方
            // 如果选中栏会超出屏幕底部，则显示在墨迹上方
            if (borderTop + BorderStrokeSelectionControlHeight > Height)
            {
                borderTop = inkCanvas.GetSelectionBounds().Top - BorderStrokeSelectionControlHeight - 10;
                if (borderTop < 0) borderTop = 10; // 如果上方也没有空间，则显示在顶部
            }

            BorderStrokeSelectionControl.Margin = new Thickness(borderLeft, borderTop, 0, 0);
        }

        private void GridInkCanvasSelectionCover_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        private void GridInkCanvasSelectionCover_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (StrokeManipulationHistory?.Count > 0)
            {
                timeMachine.CommitStrokeManipulationHistory(StrokeManipulationHistory);
                foreach (var item in StrokeManipulationHistory)
                {
                    StrokeInitialHistory[item.Key] = item.Value.Item2;
                }

                StrokeManipulationHistory = null;
            }

            if (DrawingAttributesHistory.Count > 0)
            {
                timeMachine.CommitStrokeDrawingAttributesHistory(DrawingAttributesHistory);
                DrawingAttributesHistory = new Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>();
                foreach (var item in DrawingAttributesHistoryFlag)
                {
                    item.Value.Clear();
                }
            }
        }

        /// <summary>
        /// 处理选区覆盖层上的操控增量事件：根据触点数与事件增量对当前选中的墨迹应用平移、缩放与旋转变换，并刷新选区控件位置。
        /// </summary>
        /// <param name="sender">触发事件的元素（选区覆盖层）。</param>
        /// <param name="e">包含本次操控的位移、旋转与缩放增量，用于构建并应用到选中墨迹的变换。若仅有单指触点且已有选中墨迹，则在此方法中不处理（由单指触摸逻辑处理）。</param>
        private void GridInkCanvasSelectionCover_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            try
            {
                if (dec.Count >= 1)
                {
                    // 单指时，让TouchMove处理拖动
                    if (dec.Count == 1 && inkCanvas.GetSelectedStrokes().Count > 0)
                    {
                        return;
                    }

                    bool disableScale = dec.Count >= 3;
                    var md = e.DeltaManipulation;
                    var trans = md.Translation; // 获得位移矢量
                    var rotate = md.Rotation; // 获得旋转角度
                    var scale = md.Scale; // 获得缩放倍数

                    var m = new Matrix();

                    // Find center of element and then transform to get current location of center
                    var fe = e.Source as FrameworkElement;
                    var center = new Point(fe.ActualWidth / 2, fe.ActualHeight / 2);
                    center = new Point(inkCanvas.GetSelectionBounds().Left + inkCanvas.GetSelectionBounds().Width / 2,
                        inkCanvas.GetSelectionBounds().Top + inkCanvas.GetSelectionBounds().Height / 2);
                    center = m.Transform(center); // 转换为矩阵缩放和旋转的中心点

                    // Update matrix to reflect translation/rotation
                    m.Translate(trans.X, trans.Y); // 移动
                    if (!disableScale)
                        m.ScaleAt(scale.X, scale.Y, center.X, center.Y); // 缩放

                    var strokes = inkCanvas.GetSelectedStrokes();
                    if (StrokesSelectionClone.Count != 0)
                        strokes = StrokesSelectionClone;
                    else if (Settings.Gesture.IsEnableTwoFingerRotationOnSelection)
                        m.RotateAt(rotate, center.X, center.Y); // 旋转

                    // 应用变换到选中的墨迹
                    foreach (var stroke in strokes)
                    {
                        stroke.Transform(m, false);
                    }

                    updateBorderStrokeSelectionControlLocation();
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"墨迹ManipulationDelta错误: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理画布选择覆盖层的触摸按下事件：记录触点 ID，并在这是第一个触点时保存触摸中心点与最后触摸位置。
        /// </summary>
        /// <param name="sender">事件来源对象（未使用）。</param>
        /// <param name="e">触摸事件参数，用于获取触点位置和 ID。</param>
        private void GridInkCanvasSelectionCover_TouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                var touchPoint = e.GetTouchPoint(null);
                centerPoint = touchPoint.Position;
                lastTouchPointOnGridInkCanvasCover = touchPoint.Position;
            }
        }

        /// <summary>
        /// 处理选择覆盖层的触摸抬起事件，维护多点触控计数并根据触点位置与当前选区更新选择状态和选择覆盖可见性。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">触摸事件参数，包含触点位置与触点标识。</param>
        private void GridInkCanvasSelectionCover_TouchUp(object sender, TouchEventArgs e)
        {
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count >= 1) return;
            isProgramChangeStrokeSelection = false;
            
            var touchUpPoint = e.GetTouchPoint(null).Position;
            if (lastTouchPointOnGridInkCanvasCover == touchUpPoint)
            {
                var touchPointInCanvas = e.GetTouchPoint(inkCanvas).Position;
                var selectionBounds = inkCanvas.GetSelectionBounds();
                
                if (!(touchPointInCanvas.X < selectionBounds.Left) &&
                    !(touchPointInCanvas.Y < selectionBounds.Top) &&
                    !(touchPointInCanvas.X > selectionBounds.Right) &&
                    !(touchPointInCanvas.Y > selectionBounds.Bottom))
                {
                    return;
                }
                isProgramChangeStrokeSelection = true;
                inkCanvas.Select(new StrokeCollection());
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                isProgramChangeStrokeSelection = false;
                StrokesSelectionClone = new StrokeCollection();
            }
            else if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                StrokesSelectionClone = new StrokeCollection();
            }
            else
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Visible;
                StrokesSelectionClone = new StrokeCollection();
            }
        }

        /// <summary>
        /// 处理选区覆盖层的触摸移动，用于以单指拖动平移当前选中的墨迹并更新选区控件位置。
        /// </summary>
        /// <remarks>
        /// 仅在当前有选中墨迹且仅有一个有效触点时生效；当触点相对于上一次记录的位置在任一轴上移动超过 1 像素时，按位移量对所有选中笔划应用平移变换并更新最后触摸点和选区控件位置。
        /// </remarks>
        private void GridInkCanvasSelectionCover_TouchMove(object sender, TouchEventArgs e)
        {
            // 处理触摸移动事件 - 用于拖动选中的墨迹
            if (inkCanvas.GetSelectedStrokes().Count > 0 && dec.Count == 1)
            {
                var currentTouchPoint = e.GetTouchPoint(inkCanvas).Position;

                // 检查是否有有效的起始触摸点
                if (lastTouchPointOnGridInkCanvasCover != new Point(0, 0))
                {
                    var delta = currentTouchPoint - lastTouchPointOnGridInkCanvasCover;

                    // 只有当移动距离足够大时才进行拖动（避免微小移动造成的抖动）
                    if (Math.Abs(delta.X) > 1 || Math.Abs(delta.Y) > 1)
                    {
                        // 创建变换矩阵
                        var matrix = new Matrix();
                        matrix.Translate(delta.X, delta.Y);

                        // 对选中的墨迹应用变换
                        var selectedStrokes = inkCanvas.GetSelectedStrokes();
                        foreach (var stroke in selectedStrokes)
                        {
                            stroke.Transform(matrix, false);
                        }

                        // 更新选中栏位置
                        updateBorderStrokeSelectionControlLocation();

                        // 更新最后触摸点
                        lastTouchPointOnGridInkCanvasCover = currentTouchPoint;
                    }
                }
            }
        }

        private Point lastTouchPointOnGridInkCanvasCover = new Point(0, 0);

        /// <summary>
        /// 切换到套索/选择工具并重置与擦除器和绘图形状相关的临时状态。
        /// </summary>
        /// <remarks>
        /// 将 forceEraser、forcePointEraser 置为 false，重置 drawingShapeMode 为 0，然后将当前工具设为选择并更新光标显示。
        /// </remarks>
        private void LassoSelect_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            forcePointEraser = false;
            drawingShapeMode = 0;
            // 使用集中化的工具模式切换方法
            SetCurrentToolMode(InkCanvasEditingMode.Select);
            SetCursorBasedOnEditingMode(inkCanvas);
        }

        private void BtnLassoSelect_Click(object sender, RoutedEventArgs e)
        {
            forceEraser = false;
            forcePointEraser = false;
            drawingShapeMode = 0;
            // 使用集中化的工具模式切换方法
            SetCurrentToolMode(InkCanvasEditingMode.Select);
            inkCanvas.IsManipulationEnabled = true;
            SetCursorBasedOnEditingMode(inkCanvas);
        }

        #region UIElement Selection and Resize

        private Rect GetUIElementBounds(UIElement element)
        {
            if (element is FrameworkElement fe)
            {
                var left = InkCanvas.GetLeft(element);
                var top = InkCanvas.GetTop(element);

                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                var width = fe.ActualWidth > 0 ? fe.ActualWidth : fe.Width;
                var height = fe.ActualHeight > 0 ? fe.ActualHeight : fe.Height;

                // 检查是否有RenderTransform
                if (fe.RenderTransform != null && fe.RenderTransform != Transform.Identity)
                {
                    try
                    {
                        // 如果有变换，使用变换后的边界
                        var transform = element.TransformToAncestor(inkCanvas);
                        var elementBounds = new Rect(0, 0, width, height);
                        var transformedBounds = transform.TransformBounds(elementBounds);
                        return transformedBounds;
                    }
                    catch
                    {
                        // 变换失败时回退到简单计算
                        return new Rect(left, top, width, height);
                    }
                }

                // 没有变换时直接使用位置和大小
                return new Rect(left, top, width, height);
            }

            return new Rect(0, 0, 0, 0);
        }

        #endregion

        #region Selection Display and Resize Handles

        /// <summary>
        /// 显示并定位当前选中笔画的选择矩形与控制点（缩放/旋转手柄）。
        /// </summary>
        /// <remarks>
        /// 如果没有选中笔画，则隐藏选择显示。对选区边界向外扩展 8 像素以置放选择框和手柄，设置选择矩形的位置与尺寸并调用 UpdateSelectionHandles 更新手柄位置，然后使手柄画布可见。
        /// </remarks>
        private void UpdateSelectionDisplay()
        {
            if (inkCanvas.GetSelectedStrokes().Count == 0)
            {
                HideSelectionDisplay();
                return;
            }

            var selectionBounds = inkCanvas.GetSelectionBounds();

            // 向外扩展8像素
            double expandOffset = 8;

            // 更新选择框，向外扩展8像素
            SelectionRectangle.Visibility = Visibility.Visible;
            SelectionRectangle.Margin = new Thickness(selectionBounds.Left - expandOffset, selectionBounds.Top - expandOffset, 0, 0);
            SelectionRectangle.Width = selectionBounds.Width + expandOffset * 2;
            SelectionRectangle.Height = selectionBounds.Height + expandOffset * 2;

            // 更新选择点位置
            UpdateSelectionHandles(selectionBounds);
            SelectionHandlesCanvas.Visibility = Visibility.Visible;
        }

        private void HideSelectionDisplay()
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            SelectionHandlesCanvas.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 更新选择框的八个控制手柄的位置，使之围绕指定边界外侧排列：四个边中点手柄在边外向外扩展 8 像素，四个角手柄完全位于选择框外角位置。
        /// </summary>
        /// <param name="bounds">表示当前选区在画布坐标系中的边界矩形，用于计算各手柄的目标位置。</param>
        private void UpdateSelectionHandles(Rect bounds)
        {
            // 四个边选择点，向外扩展8像素
            TopHandle.Margin = new Thickness(bounds.Left + bounds.Width / 2 - 4, bounds.Top - 12, 0, 0);
            BottomHandle.Margin = new Thickness(bounds.Left + bounds.Width / 2 - 4, bounds.Bottom + 4 , 0, 0);
            LeftHandle.Margin = new Thickness(bounds.Left - 12 , bounds.Top + bounds.Height / 2 - 4, 0, 0);
            RightHandle.Margin = new Thickness(bounds.Right + 4, bounds.Top + bounds.Height / 2 - 4, 0, 0);

            // 四个角选择点，完全位于选择框外部
            TopLeftHandle.Margin = new Thickness(bounds.Left - 12, bounds.Top - 12, 0, 0);
            TopRightHandle.Margin = new Thickness(bounds.Right + 4, bounds.Top - 12, 0, 0);
            BottomLeftHandle.Margin = new Thickness(bounds.Left - 12, bounds.Bottom + 4, 0, 0);
            BottomRightHandle.Margin = new Thickness(bounds.Right + 4, bounds.Bottom + 4, 0, 0);
        }

        /// <summary>
        /// 处理选择框调整大小句柄的鼠标按下事件：记录调整状态与初始参数并捕获鼠标以开始尺寸调整操作。
        /// </summary>
        /// <param name="sender">触发事件的句柄矩形（调整大小的句柄）。</param>
        /// <param name="e">包含用于获取在 inkCanvas 上起始鼠标位置的事件数据；事件在成功处理后被标记为已处理。</param>
        private void SelectionHandle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                isResizing = true;
                currentResizeHandle = handle.Name;
                resizeStartPoint = e.GetPosition(inkCanvas);
                originalSelectionBounds = inkCanvas.GetSelectionBounds();
                handle.CaptureMouse();
                e.Handled = true;
            }
        }

        private void SelectionHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isResizing || !(sender is Rectangle handle)) return;

            var currentPoint = e.GetPosition(inkCanvas);
            var delta = new Point(currentPoint.X - resizeStartPoint.X, currentPoint.Y - resizeStartPoint.Y);

            var newBounds = CalculateNewBounds(originalSelectionBounds, delta, currentResizeHandle);

            // 应用新的边界到选中的墨迹
            ApplyBoundsToStrokes(newBounds);

            // 更新选择框显示
            UpdateSelectionDisplay();
        }

        /// <summary>
        /// 在选区缩放句柄上释放鼠标时结束缩放交互：重置缩放状态、清除当前句柄并释放句柄的鼠标捕获，同时将事件标记为已处理。
        /// </summary>
        private void SelectionHandle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                isResizing = false;
                currentResizeHandle = "";
                handle.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 在触摸按下选择调整把手时初始化调整操作的状态与基准数据，进入缩放/调整模式并标记事件为已处理。
        /// </summary>
        /// <param name="sender">触发事件的选择把手（Rectangle）。</param>
        /// <param name="e">触摸事件参数，包含触摸位置等信息。</param>
        private void SelectionHandle_TouchDown(object sender, TouchEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                isResizing = true;
                currentResizeHandle = handle.Name;
                var touchPoint = e.GetTouchPoint(inkCanvas);
                resizeStartPoint = touchPoint.Position;
                originalSelectionBounds = inkCanvas.GetSelectionBounds();
                e.Handled = true;
            }
        }

        /// <summary>
        /// 在触摸移动时根据当前拖动手柄计算并应用新的选区边界，将相应变换应用到所选墨迹并更新选择框显示。
        /// </summary>
        /// <param name="sender">触发事件的缩放句柄（Rectangle）。</param>
        /// <param name="e">触摸事件参数，提供触点位置与事件状态。</param>
        private void SelectionHandle_TouchMove(object sender, TouchEventArgs e)
        {
            if (!isResizing || !(sender is Rectangle handle)) return;

            var touchPoint = e.GetTouchPoint(inkCanvas);
            var currentPoint = touchPoint.Position;
            var delta = new Point(currentPoint.X - resizeStartPoint.X, currentPoint.Y - resizeStartPoint.Y);

            var newBounds = CalculateNewBounds(originalSelectionBounds, delta, currentResizeHandle);

            // 应用新的边界到选中的墨迹
            ApplyBoundsToStrokes(newBounds);

            // 更新选择框显示
            UpdateSelectionDisplay();

            e.Handled = true;
        }

        /// <summary>
        /// 在触摸结束时停止对选区调整大小的交互并清除当前活动的调整柄状态。
        /// </summary>
        /// <param name="sender">触发事件的调整柄（Rectangle）。</param>
        /// <param name="e">触摸事件参数，事件将在处理后标记为已处理。</param>
        private void SelectionHandle_TouchUp(object sender, TouchEventArgs e)
        {
            if (sender is Rectangle handle)
            {
                isResizing = false;
                currentResizeHandle = "";
                e.Handled = true;
            }
        }

        /// <summary>
        /// 根据被拖动的句柄名称和位移，计算并返回调整后的选择矩形。
        /// </summary>
        /// <param name="originalBounds">原始选择矩形。</param>
        /// <param name="delta">拖动产生的位移（用于调整对应边或角的偏移）。</param>
        /// <param name="handleName">被拖动的句柄名称；支持的值：TopLeftHandle、TopRightHandle、BottomLeftHandle、BottomRightHandle、TopHandle、BottomHandle、LeftHandle、RightHandle。</param>
        /// <returns>应用位移并强制最小尺寸（宽、高至少为 10）后得到的新矩形。</returns>
        private Rect CalculateNewBounds(Rect originalBounds, Point delta, string handleName)
        {
            var newBounds = originalBounds;
            double newWidth = originalBounds.Width;
            double newHeight = originalBounds.Height;
            double newX = originalBounds.X;
            double newY = originalBounds.Y;

            switch (handleName)
            {
                case "TopLeftHandle":
                    newX = originalBounds.X + delta.X;
                    newY = originalBounds.Y + delta.Y;
                    newWidth = originalBounds.Width - delta.X;
                    newHeight = originalBounds.Height - delta.Y;
                    break;
                case "TopRightHandle":
                    newY = originalBounds.Y + delta.Y;
                    newWidth = originalBounds.Width + delta.X;
                    newHeight = originalBounds.Height - delta.Y;
                    break;
                case "BottomLeftHandle":
                    newX = originalBounds.X + delta.X;
                    newWidth = originalBounds.Width - delta.X;
                    newHeight = originalBounds.Height + delta.Y;
                    break;
                case "BottomRightHandle":
                    newWidth = originalBounds.Width + delta.X;
                    newHeight = originalBounds.Height + delta.Y;
                    break;
                case "TopHandle":
                    newY = originalBounds.Y + delta.Y;
                    newHeight = originalBounds.Height - delta.Y;
                    break;
                case "BottomHandle":
                    newHeight = originalBounds.Height + delta.Y;
                    break;
                case "LeftHandle":
                    newX = originalBounds.X + delta.X;
                    newWidth = originalBounds.Width - delta.X;
                    break;
                case "RightHandle":
                    newWidth = originalBounds.Width + delta.X;
                    break;
            }

            // 确保最小尺寸和正值
            if (newWidth < 10) newWidth = 10;
            if (newHeight < 10) newHeight = 10;

            // 创建新的Rect，确保所有值都是有效的
            newBounds = new Rect(newX, newY, newWidth, newHeight);

            return newBounds;
        }

        private void ApplyBoundsToStrokes(Rect newBounds)
        {
            var selectedStrokes = inkCanvas.GetSelectedStrokes();
            if (selectedStrokes.Count == 0) return;

            var originalBounds = inkCanvas.GetSelectionBounds();

            // 计算缩放比例
            var scaleX = newBounds.Width / originalBounds.Width;
            var scaleY = newBounds.Height / originalBounds.Height;

            // 计算平移量
            var translateX = newBounds.X - originalBounds.X;
            var translateY = newBounds.Y - originalBounds.Y;

            // 创建变换矩阵
            var matrix = new Matrix();
            matrix.Translate(translateX, translateY);
            matrix.ScaleAt(scaleX, scaleY, originalBounds.X + originalBounds.Width / 2, originalBounds.Y + originalBounds.Height / 2);

            // 应用变换到选中的墨迹
            foreach (var stroke in selectedStrokes)
            {
                stroke.Transform(matrix, false);
            }
        }

        #endregion
    }
}
