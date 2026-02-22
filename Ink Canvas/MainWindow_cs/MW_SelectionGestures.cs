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

        /// <summary>
        /// 存储最后一次鼠标按下的边界对象
        /// </summary>
        private object lastBorderMouseDownObject;

        /// <summary>
        /// 处理边界鼠标按下事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 如果发送者是 RandomDrawPanel 或 SingleDrawPanel，且它们被隐藏，则不处理事件
        /// 否则存储当前鼠标按下的对象
        /// <summary>
        /// 记录最近一次边框相关的鼠标按下事件的源对象，用于后续鼠标弹起事件匹配和操作调度。
        /// </summary>
        /// <remarks>
        /// 如果事件源是 RandomDrawPanel 或 SingleDrawPanel 且当前不可见，则会忽略该按下事件并不记录对象。
        /// </remarks>
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

        /// <summary>
        /// 处理墨迹选择克隆鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 执行墨迹克隆操作并记录日志
        /// <summary>
        /// 在与上一次按下相同的边框对象上松开鼠标时，将当前选中的墨迹克隆到画布上。
        /// </summary>
        /// <remarks>
        /// 若存在选中的墨迹则执行克隆并记录成功日志；若没有选中墨迹则不执行任何操作；如发生异常则记录错误日志但不抛出异常。
        /// </remarks>
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

        /// <summary>
        /// 处理墨迹选择克隆到新画板鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 克隆选中的墨迹到新画板并清除当前选择
        /// <summary>
        /// 将当前选中的笔画克隆到一个新的画板并清除当前画布上的选择。
        /// </summary>
        /// <remarks>
        /// 仅在鼠标按下和抬起事件来自同一对象时执行；函数会先清空当前选择，然后把选中的笔画复制到新画板。
        /// </remarks>
        private void BorderStrokeSelectionCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            var strokes = inkCanvas.GetSelectedStrokes();
            inkCanvas.Select(new StrokeCollection());
            CloneStrokesToNewBoard(strokes);
        }

        /// <summary>
        /// 处理墨迹选择删除鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 调用 SymbolIconDelete_MouseUp 方法执行删除操作
        /// <summary>
        /// 在边界控件的鼠标释放时，如果释放来源与最后一次按下的对象相同，则执行对当前选中项的删除操作。
        /// </summary>
        /// <param name="sender">触发事件的控件对象。</param>
        /// <param name="e">鼠标事件参数，包含按键和位置等信息。</param>
        /// <remarks>
        /// 仅在鼠标按下与释放来自同一边界对象时才会执行删除；否则不作任何操作。
        /// </remarks>
        private void BorderStrokeSelectionDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            SymbolIconDelete_MouseUp(sender, e);
        }

        /// <summary>
        /// 处理笔宽减小鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 调用 ChangeStrokeThickness 方法减小笔宽
        /// <summary>
        /// 将当前选中笔划的笔宽按 0.8 的倍数缩小（仅在鼠标按下与弹起发生在同一边框控件时生效）。
        /// </summary>
        private void GridPenWidthDecrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            ChangeStrokeThickness(0.8);
        }

        /// <summary>
        /// 处理笔宽增大鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 调用 ChangeStrokeThickness 方法增大笔宽
        /// <summary>
        /// 在鼠标释放时（仅当按下和释放来自同一边框控件）将当前选中笔划的笔宽放大到原来的 1.25 倍。
        /// </summary>
        private void GridPenWidthIncrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;
            ChangeStrokeThickness(1.25);
        }

        /// <summary>
        /// 更改选中墨迹的粗细
        /// </summary>
        /// <param name="multipler">缩放倍数</param>
        /// <remarks>
        /// 对选中的每个墨迹应用缩放倍数
        /// 确保新的粗细在允许的范围内
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 将当前选中描迹的笔触宽高按给定倍数缩放，并在可接受范围内应用改动。
        /// </summary>
        /// <param name="multipler">用于缩放笔触宽度和高度的乘数（例如 0.8 表示缩小到 80%，1.25 表示放大到 125%）。</param>
        /// <remarks>
        /// 仅当计算后宽度和高度落在 DrawingAttributes 的最小/最大允许范围内时才会应用变更。  
        /// 若存在未提交的 DrawingAttributesHistory，会将其提交到 timeMachine 并清空相关历史与标记。
        /// </remarks>
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

        /// <summary>
        /// 处理笔宽恢复默认鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 将选中墨迹的粗细恢复为默认值
        /// <summary>
        /// 将当前选中的所有笔画的宽度和高度恢复为画布的默认绘图属性。
        /// </summary>
        private void GridPenWidthRestore_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lastBorderMouseDownObject != sender) return;

            foreach (var stroke in inkCanvas.GetSelectedStrokes())
            {
                stroke.DrawingAttributes.Width = inkCanvas.DefaultDrawingAttributes.Width;
                stroke.DrawingAttributes.Height = inkCanvas.DefaultDrawingAttributes.Height;
            }
        }

        /// <summary>
        /// 处理水平翻转鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 对选中的墨迹应用水平翻转变换
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 将当前选中的墨迹围绕选区中心做水平翻转（镜像）。
        /// </summary>
        /// <remarks>
        /// 仅在鼠标按下与本次释放来源相同的边栏控件时生效；对每个被选中的 Stroke 应用水平缩放变换以实现镜像效果。若存在绘制属性历史记录，会将其通过 timeMachine 提交并清空相关历史结构。
        /// </remarks>
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

        /// <summary>
        /// 处理垂直翻转鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 对选中的墨迹应用垂直翻转变换
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 对当前选中的笔画以选区中心为轴进行垂直翻转（上下镜像）并在必要时提交笔画绘制属性的历史记录。
        /// </summary>
        /// <remarks>
        /// 仅在触发者与上一次记录的边框按下对象相同时执行；翻转会应用到画布上当前选中的所有 Stroke。若存在 DrawingAttributesHistory，则通过 timeMachine 提交该历史并重置相关历史数据与标志。
        /// </remarks>
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
        /// <summary>
        /// 处理顺时针旋转45度鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 对选中的墨迹应用45度旋转变换
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 将当前选中的墨迹围绕选区中心顺时针旋转 45 度，并在需要时提交绘制属性变更历史。
        /// </summary>
        /// <remarks>
        /// 仅当触发来源与最近记录的边框按下对象相同时执行。对 inkCanvas 上的所有选中 Stroke 应用 45° 旋转变换（以选区中心为旋转中心）。
        /// 如果存在 DrawingAttributesHistory，会将其提交到 timeMachine 并重置相关历史与标记集合。
        /// </remarks>
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

        /// <summary>
        /// 处理顺时针旋转90度鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 只有当鼠标按下和释放的是同一个对象时才处理
        /// 对选中的墨迹应用90度旋转变换
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 将当前选中的笔划绕其边界中心顺时针旋转 90 度（仅在鼠标按下与释放来自同一边界控件时生效）。
        /// </summary>
        /// <remarks>
        /// 如果存在绘图属性历史（DrawingAttributesHistory），会将其提交到时间轴（timeMachine）并清空相关历史与标记。
        /// </remarks>
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

        /// <summary>
        /// 墨迹选择覆盖层鼠标按下状态
        /// </summary>
        private bool isGridInkCanvasSelectionCoverMouseDown;
        /// <summary>
        /// 墨迹拖动状态
        /// </summary>
        private bool isStrokeDragging = false;
        /// <summary>
        /// 墨迹拖动起始点
        /// </summary>
        private Point strokeDragStartPoint;
        /// <summary>
        /// 墨迹选择克隆集合
        /// </summary>
        private StrokeCollection StrokesSelectionClone = new StrokeCollection();

        // 选择框和选择点相关变量
        /// <summary>
        /// 调整大小状态
        /// </summary>
        private bool isResizing = false;
        /// <summary>
        /// 当前调整把手
        /// </summary>
        private string currentResizeHandle = "";
        /// <summary>
        /// 调整起始点
        /// </summary>
        private Point resizeStartPoint;
        /// <summary>
        /// 原始选择边界
        /// </summary>
        private Rect originalSelectionBounds;

        /// <summary>
        /// 处理墨迹选择覆盖层鼠标按下事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 如果有选中的墨迹，检查点击位置是否在选择框边界内
        /// 如果在边界内，开始拖动墨迹
        /// 如果在边界外，取消选择
        /// <summary>
        /// 处理选择覆盖层的鼠标按下；在点击位于当前选中墨迹边界内时开始拖动选中墨迹，否则取消选择并隐藏覆盖层。
        /// </summary>
        /// <param name="sender">事件源（触发此处理器的元素）。</param>
        /// <param name="e">鼠标事件数据，用于获取点击位置并决定是否进入拖动状态。</param>
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

        /// <summary>
        /// 处理墨迹选择覆盖层鼠标移动事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标事件参数</param>
        /// <remarks>
        /// 如果正在拖动墨迹，执行拖动操作
        /// 如果鼠标在选中区域移动，更新墨迹选中栏位置
        /// <summary>
        /// 在选区覆盖层上处理鼠标移动：在拖动时将选中的墨迹按鼠标位移平移并更新选区控制位置；在未拖动但有选中墨迹时只更新选区控制位置。
        /// </summary>
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

        /// <summary>
        /// 处理墨迹选择覆盖层鼠标释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 结束墨迹拖动
        /// 只有在没有选中墨迹时才隐藏选中栏
        /// <summary>
        /// 处理选区覆盖层的鼠标弹起事件，结束可能的拖拽并在无选中墨迹时隐藏覆盖层。
        /// </summary>
        /// <remarks>
        /// 如果当前正在拖拽选中墨迹，则结束拖拽、释放鼠标捕获并恢复光标。随后清除选区覆盖层的按下状态标记；若画布上没有选中笔画，则将覆盖层隐藏。
        /// </remarks>
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

        /// <summary>
        /// 处理选择按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        /// <remarks>
        /// 如果当前是选择模式，检查是否全选
        /// 如果全选，则切换到墨迹模式再切换回选择模式
        /// 如果不是全选，则选择所有有效墨迹
        /// 如果当前不是选择模式，则切换到选择模式
        /// <summary>
        /// 切换并处理“选择”工具：在非选择模式下切换到选择模式；在选择模式下若未全选则选择所有可见笔画，否则临时切换到绘制模式再恢复选择模式。
        /// </summary>
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

        /// <summary>
        /// 墨迹选择控件宽度
        /// </summary>
        private double BorderStrokeSelectionControlWidth = 490.0;
        /// <summary>
        /// 墨迹选择控件高度
        /// </summary>
        private double BorderStrokeSelectionControlHeight = 80.0;
        /// <summary>
        /// 程序更改墨迹选择状态
        /// </summary>
        private bool isProgramChangeStrokeSelection;

        /// <summary>
        /// 处理墨迹画布选择更改事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// 优先检查墨迹选择状态
        /// 如果有墨迹被选中，显示墨迹选择栏和选择框
        /// 如果有图片元素被选中，不显示选择框
        /// 如果没有选中任何内容，隐藏选择框
        /// <summary>
        /// 响应画布选择变化，基于当前选择显示或隐藏对应的选择框与工具栏，并同步选择显示状态。
        /// </summary>
        /// <remarks>
        /// 如果是程序性变更（isProgramChangeStrokeSelection 为 true）则忽略本次事件。
        /// 当有墨迹被选中时：清除当前图片选择（如果有）、显示墨迹选择覆盖层、清空克隆背景、更新选择工具位置并刷新选择显示。
        /// 当选中的是图片元素（通过 InkCanvas 的选中元素或 currentSelectedElement）时：隐藏选择覆盖层并隐藏选择显示。
        /// 当没有选中任何内容时：隐藏选择覆盖层并隐藏选择显示。
        /// 此方法会修改 GridInkCanvasSelectionCover、BorderImageSelectionControl、BorderStrokeSelectionClone 的可见性/背景，并调用 updateBorderStrokeSelectionControlLocation、UpdateSelectionDisplay 或 HideSelectionDisplay 以更新 UI。
        /// </remarks>
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



        /// <summary>
        /// 更新墨迹选中栏位置
        /// </summary>
        /// <remarks>
        /// 计算墨迹选中栏的位置，确保在墨迹下方显示
        /// 如果选中栏会超出屏幕底部，则显示在墨迹上方
        /// 如果上方也没有空间，则显示在顶部
        /// <summary>
        /// 将墨迹选中控制项定位在所选墨迹的下方（或在下方无空间时位于上方），并在窗口范围内进行边界约束。
        /// </summary>
        /// <remarks>
        /// 控制项水平居中于当前选中墨迹的边界，并默认在选区下方偏移 10 像素显示；如果下方空间不足则改为显示在选区上方并保留 10 像素间距；同时将位置限制在窗口可见区域内，避免超出左/上/右/下边界。最终位置通过设置 BorderStrokeSelectionControl.Margin 应用。
        /// </remarks>
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

        /// <summary>
        /// 处理墨迹选择覆盖层操作开始事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">操作开始事件参数</param>
        /// <remarks>
        /// 设置操作模式为所有模式
        /// <summary>
        /// 在操控（触摸/手势）开始时启用全部操控模式（平移、缩放、旋转）。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">操控起始事件数据；处理程序将其 <see cref="ManipulationStartingEventArgs.Mode"/> 设置为 <see cref="ManipulationModes.All"/>。</param>
        private void GridInkCanvasSelectionCover_ManipulationStarting(object sender, ManipulationStartingEventArgs e)
        {
            e.Mode = ManipulationModes.All;
        }

        /// <summary>
        /// 处理墨迹选择覆盖层操作完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">操作完成事件参数</param>
        /// <remarks>
        /// 如果有 StrokeManipulationHistory，则提交历史记录
        /// 如果有 DrawingAttributesHistory，则提交历史记录
        /// <summary>
        /// 在操控（平移/缩放/旋转）完成时提交并清理与选中笔画相关的操作历史与绘图属性历史记录。
        /// </summary>
        /// <param name="sender">触发事件的对象（通常为 selection cover 或其容器）。</param>
        /// <param name="e">包含操控完成信息的事件参数。</param>
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
        /// 处理选择覆盖层的操作增量事件，将来自触摸或操作的平移、缩放和旋转应用到当前选中的墨迹上并更新选择控件位置。
        /// </summary>
        /// <param name="sender">触发事件的元素（通常为选择覆盖层）。</param>
        /// <param name="e">包含平移、缩放和旋转增量的 ManipulationDeltaEventArgs。</param>
        /// <remarks>
        /// - 当只有单指触摸且已有选中墨迹时，不在此处处理拖动（由 TouchMove 处理）。
        /// - 三指及以上触摸时禁用缩放。 
        /// - 若 StrokesSelectionClone 非空，则对其内的墨迹应用变换；否则在允许两指旋转时也会应用旋转变换。 
        /// - 处理完成后会刷新并更新边框/选择控件的位置。
        /// <summary>
        /// 处理选区覆盖层的操作增量事件，对当前选中的墨迹应用平移、缩放和旋转变换并更新选区控件位置。
        /// </summary>
        /// <remarks>
        /// - 当触控点数为 1 且有选中墨迹时，交由触摸移动处理（不在此处应用变换）。  
        /// - 当触控点数 >= 3 时禁用缩放，仅应用平移与（可选的）旋转。  
        /// - 如果存在 StrokesSelectionClone，则对该克隆集合应用变换；否则对画布上的选中墨迹应用变换。  
        /// - 变换以选区中心为基点进行缩放和旋转（当配置允许两指旋转时才会进行旋转）。  
        /// </remarks>
        /// <param name="sender">事件源，通常为选区覆盖的 FrameworkElement。</param>
        /// <param name="e">包含平移、缩放和旋转增量的 ManipulationDeltaEventArgs。</param>
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
        /// 处理选区覆盖层的触摸按下事件：记录触摸设备 ID，并在第一个触点时保存选择中心与用于拖拽的初始触点位置。
        /// </summary>
        /// <param name="sender">事件源（触摸事件的发送者）。</param>
        /// <summary>
        /// 记录触摸设备，并在首次触摸时保存触摸中心点和在画布内的起始拖拽位置以供后续拖动与操控使用。
        /// </summary>
        /// <param name="e">触摸事件参数，包含触点位置和触摸设备 ID。</param>
        private void GridInkCanvasSelectionCover_TouchDown(object sender, TouchEventArgs e)
        {
            dec.Add(e.TouchDevice.Id);
            //设备1个的时候，记录中心点
            if (dec.Count == 1)
            {
                var touchPoint = e.GetTouchPoint(null);
                centerPoint = touchPoint.Position;
                lastTouchPointOnGridInkCanvasCover = touchPoint.Position;
                
                var touchPointInCanvas = e.GetTouchPoint(inkCanvas);
                lastDragPointInCanvas = touchPointInCanvas.Position;
            }
        }

        /// <summary>
        /// 处理选区覆盖层的触摸结束事件：更新触摸跟踪状态并根据触摸位置和当前选区状态显示或隐藏选区覆盖层与克隆集合。
        /// </summary>
        /// <param name="sender">触发事件的对象（通常为选区覆盖层）。</param>
        /// <summary>
        /// 处理选择覆盖层的触摸抬起事件，结束触摸拖动并根据触点位置或当前选中状态更新选择覆盖层的可见性与选区状态。
        /// </summary>
        /// <param name="e">触摸事件参数，包含触点信息。</param>
        private void GridInkCanvasSelectionCover_TouchUp(object sender, TouchEventArgs e)
        {
            dec.Remove(e.TouchDevice.Id);
            if (dec.Count >= 1) return;
            isProgramChangeStrokeSelection = false;
            
            lastDragPointInCanvas = new Point(0, 0);
            
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
        /// 处理触摸移动事件，按单指拖动并平移当前选中的墨迹，同时更新选择控件的位置。
        /// </summary>
        /// <summary>
        /// 在单指触控时根据触摸移动平移当前选中的墨迹。
        /// </summary>
        /// <remarks>仅在存在选中墨迹且当前仅有一个触点时生效；若 lastDragPointInCanvas 为 (0,0) 则不执行移动。只有当触摸位移在任一方向超过 1 像素时，才对每条选中墨迹应用平移变换，并更新选择控件位置与 lastDragPointInCanvas。</remarks>
        private void GridInkCanvasSelectionCover_TouchMove(object sender, TouchEventArgs e)
        {
            // 处理触摸移动事件 - 用于拖动选中的墨迹
            if (inkCanvas.GetSelectedStrokes().Count > 0 && dec.Count == 1)
            {
                var currentTouchPoint = e.GetTouchPoint(inkCanvas).Position;

                // 检查是否有有效的起始触摸点
                if (lastDragPointInCanvas != new Point(0, 0))
                {
                    var delta = currentTouchPoint - lastDragPointInCanvas;

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
                        lastDragPointInCanvas = currentTouchPoint;
                    }
                }
            }
        }

        private Point lastTouchPointOnGridInkCanvasCover = new Point(0, 0);
        private Point lastDragPointInCanvas = new Point(0, 0);

        /// <summary>
        /// 切换到选择（套索）工具模式并同步光标显示。
        /// </summary>
        /// <remarks>
        /// 同时取消强制橡皮擦和点式橡皮擦状态，并将绘制形状模式重置为默认（0）。
        /// <summary>
        /// 切换到套索选择工具并同步相关状态与光标。
        /// </summary>
        /// <remarks>
        /// 清除对橡皮擦的强制状态、重置绘图形状模式为默认，并将当前工具模式设置为选择模式，随后根据新模式更新画布光标。
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

        /// <summary>
        /// 处理套索选择按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">路由事件参数</param>
        /// <remarks>
        /// 设置工具模式为选择模式
        /// 启用墨迹画布的操作支持
        /// 设置光标为选择模式光标
        /// <summary>
        /// 切换到套索（选择）工具，重置擦除与绘图形状相关状态，并启用画布的操作与光标更新。
        /// </summary>
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

        /// <summary>
        /// 获取UI元素的边界
        /// </summary>
        /// <param name="element">UI元素</param>
        /// <returns>UI元素的边界矩形</returns>
        /// <remarks>
        /// 如果元素是FrameworkElement，获取其位置和大小
        /// 如果元素有RenderTransform，尝试使用变换后的边界
        /// 如果变换失败，回退到简单计算
        /// 如果元素不是FrameworkElement，返回空矩形
        /// <summary>
        /// 获取指定元素在 inkCanvas 坐标系下的边界矩形。
        /// </summary>
        /// <param name="element">要测量的 UIElement；通常为 FrameworkElement（如 Image 等）。</param>
        /// <returns>元素在 inkCanvas 坐标系中的边界矩形。若元素不是 FrameworkElement 或无法计算，则返回宽高为 0 的矩形。</returns>
        /// <remarks>
        /// - 当元素具有 RenderTransform 时，返回变换后的边界（相对于 inkCanvas）。
        /// - 否则使用 Canvas.Left/Top（NaN 视作 0）和元素的 ActualWidth/ActualHeight（不可用时使用 Width）计算边界。
        /// </remarks>
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
        /// 在画布上显示当前选中墨迹的可视选择框与调整控件（如有选中墨迹则显示，否则隐藏）。
        /// </summary>
        /// <remarks>
        /// 如果没有选中任何墨迹，隐藏选择显示；否则计算选区边界，将选择框在所有方向各扩展 8 像素，设置选择框的位置与尺寸，并更新调整句柄的位置后显示句柄画布。
        /// <summary>
        /// 显示或隐藏并更新当前所选笔划的可视选择矩形和调整句柄。
        /// </summary>
        /// <remarks>
        /// 如果没有选中的笔划，会隐藏选择显示。若有选中笔划，则从 inkCanvas 获取选择边界，将选择矩形在四周各扩展 8 像素，更新选择矩形的位置和尺寸，并刷新调整句柄的位置和可见性。
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

        /// <summary>
        /// 隐藏选择显示
        /// </summary>
        /// <remarks>
        /// 隐藏选择矩形和选择把手画布
        /// <summary>
        /// 隐藏当前笔划选择的显示框与调整句柄。
        /// </summary>
        private void HideSelectionDisplay()
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            SelectionHandlesCanvas.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 根据给定的选择边界定位并设置八个缩放/移动把手的位置，四个把手在各边外扩展 8 像素，四个角把手完全位于外部。
        /// </summary>
        /// <summary>
        /// 根据给定的选区边界定位并布置八个缩放/调整把手（四边和四角）的显示位置，使把手位于选区外侧以便交互操作。
        /// </summary>
        /// <param name="bounds">当前选区在画布坐标系中的边界矩形（未包含用于显示的 8 像素外扩展）。</param>
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
        /// 在用户按下选择框的缩放把手时开始缩放操作。
        /// </summary>
        /// <remarks>
        /// 记录缩放开始位置和当前选区的初始边界，设置当前活动把手并捕获鼠标以便后续移动/释放事件处理。
        /// </remarks>
        /// <param name="sender">触发事件的缩放把手，预期为一个 Rectangle。</param>
        /// <summary>
        /// 在按下选区调整句柄时初始化并开始一次调整（缩放/移动）操作。
        /// </summary>
        /// <param name="sender">触发事件的调整句柄（Rectangle），其 Name 表示当前使用的把手位置。</param>
        /// <param name="e">鼠标按下事件信息，位置相对于 InkCanvas，用于记录调整起点。</param>
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

        /// <summary>
        /// 处理选择把手鼠标移动事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">鼠标事件参数</param>
        /// <remarks>
        /// 如果正在调整大小，计算新的边界
        /// 应用新的边界到选中的墨迹
        /// 更新选择框显示
        /// <summary>
        /// 在拖动当前选中缩放控制点时，实时调整所选墨迹的边界并刷新选择框显示。
        /// </summary>
        /// <remarks>
        /// 如果当前不处于缩放状态或触发者不是缩放句柄，则不执行任何操作。
        /// </remarks>
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
        /// 在选择框的大小调整句柄上释放鼠标时结束调整操作、释放句柄的鼠标捕获并将事件标记为已处理。
        /// </summary>
        /// <param name="sender">触发事件的对象，应为表示调整句柄的 <see cref="System.Windows.Shapes.Rectangle"/>。</param>
        /// <summary>
        /// 结束由选区缩放把手发起的缩放操作：释放鼠标捕获并重置缩放状态与当前把手标识。
        /// </summary>
        /// <param name="sender">触发事件的对象，期望为表示缩放把手的 <see cref="System.Windows.Shapes.Rectangle"/>。</param>
        /// <param name="e">鼠标事件参数；方法执行后该事件会被标记为已处理（Handled = true）。</param>
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
        /// 在触摸按下选择调整句柄时开始调整操作并记录初始状态（活动句柄、起始点和当前选区边界）。
        /// </summary>
        /// <param name="sender">触发事件的调整句柄（应为 Rectangle）。</param>
        /// <summary>
        /// 在触摸选择把手时，进入调整大小模式并记录起始触点与当前选区边界作为后续缩放/移动的基准。
        /// </summary>
        /// <param name="sender">触发事件的矩形把手（handle），其 Name 用于确定当前活动的缩放方向。</param>
        /// <param name="e">触摸事件数据；方法会将事件标记为已处理，并记录触点相对于 inkCanvas 的位置。</param>
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
        /// 在触摸移动时根据拖动更新选区的边界并将其应用到当前所选的墨迹。
        /// </summary>
        /// <param name="sender">触发事件的调整句柄（应为一个 Rectangle）。</param>
        /// <param name="e">包含触摸位置和状态的事件参数。</param>
        /// <summary>
        — 在触摸移动时根据拖动计算并应用新的选择边界，然后刷新选择显示。
        /// </summary>
        /// <param name="sender">触发事件的句柄矩形。</param>
        /// <param name="e">触摸事件参数；方法会将该事件标记为已处理（Handled = true）。</param>
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
        /// 结束触摸对选择框调整大小的交互，重置调整状态并标记事件已处理。
        /// </summary>
        /// <param name="sender">触发事件的调整句柄（Rectangle）。</param>
        /// <summary>
        /// 在触控释放时结束当前的调整大小操作，重置相关状态并将触摸事件标记为已处理。
        /// </summary>
        /// <param name="sender">触发事件的 UI 元素（期望为调整句柄的 Rectangle）。</param>
        /// <param name="e">触摸事件数据；方法会将其标记为已处理（Handled = true）。</param>
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
        /// 根据指定的拖动增量和所操作的调整柄，计算并返回调整后的边界矩形。
        /// </summary>
        /// <param name="originalBounds">调整前的原始边界矩形。</param>
        /// <param name="delta">从初始位置到当前拖动位置的偏移量，用于更新对应边或角的位置和尺寸。</param>
        /// <param name="handleName">被拖动的调整柄的名称，支持的值：`TopLeftHandle`、`TopRightHandle`、`BottomLeftHandle`、`BottomRightHandle`、`TopHandle`、`BottomHandle`、`LeftHandle`、`RightHandle`。</param>
        /// <summary>
        /// 根据指定的拖拽手柄和偏移量计算并返回调整后的边界矩形。
        /// </summary>
        /// <param name="originalBounds">要调整的原始边界矩形。</param>
        /// <param name="delta">拖拽产生的偏移量（X 表示水平偏移，Y 表示垂直偏移）。</param>
        /// <param name="handleName">被拖拽的把手名称（例如 "TopLeftHandle"、"RightHandle" 等），决定如何变更边界的边或角。</param>
        /// <returns>应用偏移并强制最小宽高限制（10×10）后的新的边界矩形。</returns>
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

        /// <summary>
        /// 应用新的边界到选中的墨迹
        /// </summary>
        /// <param name="newBounds">新的边界矩形</param>
        /// <remarks>
        /// 计算缩放比例和平移量
        /// 创建变换矩阵
        /// 应用变换到选中的墨迹
        /// <summary>
        /// 将当前选中的墨迹缩放并平移到指定的目标边界，使其在位置和大小上匹配该矩形。
        /// </summary>
        /// <param name="newBounds">目标边界（包含位置和尺寸）。选中的墨迹将围绕原始选择边界的中心进行缩放，并平移到该目标矩形的位置和大小。</param>
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