using Ink_Canvas.Helpers;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 当前选中的可操作元素
        /// </summary>
        private FrameworkElement currentSelectedElement;

        /// <summary>
        /// 是否正在拖动
        /// </summary>
        private bool isDragging;

        /// <summary>
        /// 拖动起始点
        /// </summary>
        private Point dragStartPoint;

        #region Image
        /// <summary>
        /// 处理图片插入按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 打开文件选择对话框，选择图片文件
        /// - 创建并压缩图片
        /// - 设置图片属性，避免被InkCanvas选择系统处理
        /// - 初始化InkCanvas选择设置
        /// - 添加图片到画布
        /// - 等待图片加载完成后进行后续处理
        /// - 初始化TransformGroup
        /// - 居中缩放图片
        /// - 绑定事件处理器
        /// - 提交到时间机器历史记录
        /// - 插入图片后切换到选择模式并刷新浮动栏高光显示
        /// <summary>
        /// 打开图片文件对话框并将选中的图片插入到 InkCanvas，完成压缩、初始化变换、居中缩放、事件绑定和历史记录提交等后续设置。
        /// </summary>
        /// <remarks>
        /// - 处理流程：弹出文件选择对话框 -> 异步创建/压缩图片 -> 将图片添加到画布 -> 在图片加载完成后初始化 TransformGroup、居中并缩放、绑定交互事件 -> 提交插入历史 -> 切换到选择模式并刷新相关浮动面板。 
        /// - 插入的图片会被设置为可命中测试且不可聚焦，以避免被 InkCanvas 的系统选择行为干扰。
        /// - 方法为事件处理器，响应按钮点击事件；不显式抛出异常，内部错误通过现有日志机制处理。
        /// </remarks>
        private async void BtnImageInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                Image image = await CreateAndCompressImageAsync(filePath);

                if (image != null)
                {
                    string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                    image.Name = timestamp;

                    // 设置图片属性，避免被InkCanvas选择系统处理
                    image.IsHitTestVisible = true;
                    image.Focusable = false;

                    // 初始化InkCanvas选择设置
                    InitializeInkCanvasSelectionSettings();

                    // 先添加到画布
                    inkCanvas.Children.Add(image);

                    // 等待图片加载完成后再进行后续处理
                    image.Loaded += (s, args) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // 初始化TransformGroup
                            InitializeElementTransform(image);

                            // 居中缩放
                            CenterAndScaleElement(image);

                            // 最后绑定事件处理器
                            BindElementEvents(image);

                            LogHelper.WriteLogToFile($"图片插入完成: {image.Name}");
                        }), DispatcherPriority.Loaded);
                    };

                    timeMachine.CommitElementInsertHistory(image);

                    // 插入图片后切换到选择模式并刷新浮动栏高光显示
                    SetCurrentToolMode(InkCanvasEditingMode.Select);
                    UpdateCurrentToolMode("select");
                    HideSubPanels("select");
                }
            }
        }

        /// <summary>
        /// 初始化元素的TransformGroup
        /// </summary>
        /// <param name="element">要初始化的元素</param>
        /// <remarks>
        /// - 创建TransformGroup
        /// - 添加ScaleTransform、TranslateTransform和RotateTransform
        /// - 设置元素的RenderTransform
        /// <summary>
        /// 为指定元素设置一个包含缩放、平移和旋转的初始 TransformGroup 作为其 RenderTransform。
        /// </summary>
        /// <param name="element">将被初始化变换的 FrameworkElement。</param>
        private void InitializeElementTransform(FrameworkElement element)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1, 1));
            transformGroup.Children.Add(new TranslateTransform(0, 0));
            transformGroup.Children.Add(new RotateTransform(0));
            element.RenderTransform = transformGroup;
        }

        /// <summary>
        /// 绑定元素事件处理器
        /// </summary>
        /// <param name="element">要绑定事件的元素</param>
        /// <remarks>
        /// - 绑定鼠标事件（MouseLeftButtonDown、MouseLeftButtonUp、MouseMove、MouseWheel）
        /// - 启用触摸操作
        /// - 绑定触摸事件（ManipulationDelta、ManipulationCompleted）
        /// - 设置光标为手形
        /// - 禁用InkCanvas对图片的选择处理
        /// <summary>
        /// 为指定的元素附加常用的鼠标与触摸交互事件并配置交互相关属性以便在画布上进行拖拽、缩放和旋转等操作。
        /// </summary>
        /// <param name="element">要绑定交互事件并配置行为的 FrameworkElement 实例。</param>
        private void BindElementEvents(FrameworkElement element)
        {
            // 鼠标事件
            element.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            element.MouseLeftButtonUp += Element_MouseLeftButtonUp;
            element.MouseMove += Element_MouseMove;
            element.MouseWheel += Element_MouseWheel;

            // 触摸事件
            element.IsManipulationEnabled = true;
            element.ManipulationDelta += Element_ManipulationDelta;
            element.ManipulationCompleted += Element_ManipulationCompleted;

            // 设置光标
            element.Cursor = Cursors.Hand;

            // 禁用InkCanvas对图片的选择处理
            element.IsHitTestVisible = true;
            element.Focusable = false;
        }

        /// <summary>
        /// 处理元素鼠标左键按下事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查编辑模式是否为选择模式
        /// - 取消之前选中的元素
        /// - 选中当前元素
        /// - 开始拖动
        /// - 捕获鼠标
        /// - 设置光标为全尺寸光标
        /// <summary>
        /// 处理元素的鼠标左键按下：在 InkCanvas 处于选择模式时选中被点击元素并开始拖动（捕获鼠标并设置拖拽光标）。
        /// </summary>
        /// <param name="sender">触发事件的元素（通常为被点击的 FrameworkElement）。</param>
        /// <param name="e">包含鼠标位置信息和事件状态的 MouseButtonEventArgs。</param>
        /// <remarks>如果当前 InkCanvas 不处于选择模式，则不改变选择或开始拖动，事件保持未处理。</remarks>
        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (inkCanvas.EditingMode != InkCanvasEditingMode.Select)
                {
                    e.Handled = false;
                    return;
                }

                // 取消之前选中的元素
                if (currentSelectedElement != null && currentSelectedElement != element)
                {
                    // 保存当前编辑模式
                    var previousEditingMode = inkCanvas.EditingMode;
                    UnselectElement(currentSelectedElement);
                    // 恢复编辑模式
                    inkCanvas.EditingMode = previousEditingMode;
                }

                // 选中当前元素
                SelectElement(element);

                // 开始拖动
                isDragging = true;
                dragStartPoint = e.GetPosition(inkCanvas);
                element.CaptureMouse();
                element.Cursor = Cursors.SizeAll;

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素鼠标左键释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 停止拖动
        /// - 释放鼠标捕获
        /// - 恢复光标为手形
        /// <summary>
        /// 结束对元素的鼠标拖拽：停止拖拽、释放鼠标捕获并将光标还原为手型，同时将事件标记为已处理。
        /// </summary>
        /// <param name="sender">触发事件的元素（应为 FrameworkElement）。</param>
        /// <param name="e">鼠标按钮事件参数；方法会将该事件标记为已处理（Handled = true）。</param>
        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                isDragging = false;
                element.ReleaseMouseCapture();
                element.Cursor = Cursors.Hand;

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素触摸释放事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 停止拖动
        /// - 释放触摸捕获
        /// - 恢复光标为手形
        /// <summary>
        /// 在触控结束时停止对元素的拖拽、释放触摸捕获并恢复手型光标。
        /// </summary>
        /// <param name="sender">接收触控事件的 FrameworkElement（被拖拽的元素）。</param>
        /// <param name="e">触摸事件参数；事件会被标记为已处理。</param>
        private void Element_TouchUp(object sender, TouchEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                isDragging = false;
                element.ReleaseTouchCapture(e.TouchDevice);
                element.Cursor = Cursors.Hand;

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素鼠标移动事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查是否正在拖动且鼠标已捕获
        /// - 获取当前鼠标位置
        /// - 应用鼠标拖动变换
        /// - 如果是图片元素，更新工具栏位置
        /// - 如果是图片元素，更新选择点位置
        /// - 更新拖动起始点
        /// <summary>
        /// 处理元素的鼠标移动事件：在拖拽过程中应用平移变换并在元素为图片时同步更新工具栏与调整手柄位置。
        /// </summary>
        /// <remarks>
        /// 当处于拖拽状态且元素已捕获鼠标时，计算当前画布坐标，应用鼠标拖拽变换，若元素为 Image 则更新图片选择工具栏位置和调整手柄位置，最后更新拖拽起点并将事件标记为已处理。
        /// </remarks>
        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is FrameworkElement element && isDragging && element.IsMouseCaptured)
            {
                var currentPoint = e.GetPosition(inkCanvas);

                // 使用鼠标拖动的完整实现机制
                ApplyMouseDragTransform(element, currentPoint, dragStartPoint);

                // 如果是图片元素，更新工具栏位置
                if (element is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                {
                    UpdateImageSelectionToolbarPosition(element);
                }

                // 如果是图片元素，更新选择点位置
                if (element is Image && ImageResizeHandlesCanvas?.Visibility == Visibility.Visible)
                {
                    UpdateImageResizeHandlesPosition(GetElementActualBounds(element));
                }

                dragStartPoint = currentPoint;
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素鼠标滚轮事件 - 缩放
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 应用滚轮缩放变换
        /// - 如果是图片元素，更新工具栏位置
        /// - 如果是图片元素，更新选择点位置
        /// <summary>
        /// 处理元素上的鼠标滚轮事件：根据滚轮输入对元素执行缩放，并在选中图片时更新图片工具栏与调整句柄位置。
        /// </summary>
        /// <param name="sender">触发事件的元素（通常为要进行缩放的 FrameworkElement）。</param>
        /// <param name="e">包含滚轮增量和事件上下文的 MouseWheelEventArgs 实例。</param>
        private void Element_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is FrameworkElement element)
            {


                // 使用滚轮缩放的核心机制
                ApplyWheelScaleTransform(element, e);

                // 如果是图片元素，更新工具栏位置
                if (element is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                {
                    UpdateImageSelectionToolbarPosition(element);
                }

                // 如果是图片元素，更新选择点位置
                if (element is Image && ImageResizeHandlesCanvas?.Visibility == Visibility.Visible)
                {
                    UpdateImageResizeHandlesPosition(GetElementActualBounds(element));
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素触摸按下事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查编辑模式是否为选择模式
        /// - 取消之前选中的元素
        /// - 选中当前元素
        /// - 开始拖动
        /// - 捕获触摸
        /// - 设置光标为全尺寸光标
        /// <summary>
        — 处理元素的触摸按下事件：在选择模式下选中元素并开始触摸拖动。
        /// </summary>
        /// <param name="sender">触发事件的元素，应为要选中并拖动的 FrameworkElement。</param>
        /// <param name="e">触摸事件参数；在未处于选择模式时不处理并保留原有处理状态，成功开始拖动时将设置为已处理。</param>
        /// <remarks>
        /// 如果 InkCanvas 当前不在 Select 编辑模式，事件不被处理。若已有其它选中元素，会先取消其选中状态；随后选中当前元素、记录拖动起点、捕获触摸并将光标设置为拖动样式。
        /// </remarks>
        private void Element_TouchDown(object sender, TouchEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                if (inkCanvas.EditingMode != InkCanvasEditingMode.Select)
                {
                    e.Handled = false;
                    return;
                }

                // 取消之前选中的元素
                if (currentSelectedElement != null && currentSelectedElement != element)
                {
                    // 保存当前编辑模式
                    var previousEditingMode = inkCanvas.EditingMode;
                    UnselectElement(currentSelectedElement);
                    // 恢复编辑模式
                    inkCanvas.EditingMode = previousEditingMode;
                }

                // 选中当前元素
                SelectElement(element);

                // 开始拖动
                isDragging = true;
                dragStartPoint = e.GetTouchPoint(inkCanvas).Position;
                element.CaptureTouch(e.TouchDevice);
                element.Cursor = Cursors.SizeAll;

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素触摸操作事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查是否是双指手势
        /// - 双指手势时，让画布级别的手势处理
        /// - 单指手势时，应用触摸拖动变换
        /// - 如果是图片元素，更新工具栏位置
        /// - 如果是图片元素，更新选择点位置
        /// <summary>
        /// 处理元素的触摸操作增量，用于单指平移并在必要时更新图片的选择界面。
        /// </summary>
        /// <param name="sender">触发事件的元素（通常为 FrameworkElement，例如 Image）。</param>
        /// <param name="e">包含当前操作增量的事件参数；当检测到两个或更多触点时，方法不会处理该事件以让画布级别处理多指手势。</param>
        /// <remarks>
        /// - 对双指及以上手势不处理（将 e.Handled 设为 false 并返回），以便画布同步处理图片与墨迹的多点操作。  
        /// - 对单指手势调用 ApplyTouchManipulationTransform 执行平移/旋转/缩放变换。  
        /// - 若元素为 Image 且对应的选择工具或缩放把手可见，则更新工具栏和缩放把手的位置。  
        /// - 单指处理完成后将 e.Handled 设为 true。
        /// </remarks>
        private void Element_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                // 检查是否是双指手势
                if (e.Manipulators.Count() >= 2)
                {
                    // 双指手势时，不处理单个元素的手势，让画布级别的手势处理
                    // 这样可以实现图片与墨迹的同步移动
                    e.Handled = false;
                    return;
                }

                // 单指手势时，使用触摸拖动的完整实现
                ApplyTouchManipulationTransform(element, e);

                // 如果是图片元素，更新工具栏位置
                if (element is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                {
                    UpdateImageSelectionToolbarPosition(element);
                }

                // 如果是图片元素，更新选择点位置
                if (element is Image && ImageResizeHandlesCanvas?.Visibility == Visibility.Visible)
                {
                    UpdateImageResizeHandlesPosition(GetElementActualBounds(element));
                }

                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理元素触摸操作完成事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 可以在这里添加操作完成后的处理逻辑
        /// <summary>
        /// 在触摸或操作手势结束时对元素执行后续处理逻辑（当前为空实现，用于扩展点）。
        /// </summary>
        /// <param name="sender">事件触发者，通常是被交互的 UI 元素。</param>
        /// <param name="e">包含本次操控完成信息的事件参数，如最终增量和惯性状态。</param>
        private void Element_ManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            // 可以在这里添加操作完成后的处理逻辑
        }

        /// <summary>
        /// 应用平移变换到元素
        /// </summary>
        /// <param name="element">要变换的元素</param>
        /// <param name="deltaX">X轴偏移量</param>
        /// <param name="deltaY">Y轴偏移量</param>
        /// <remarks>
        /// - 获取元素的TransformGroup
        /// - 查找TranslateTransform
        /// - 应用平移变换
        /// <summary>
        /// 对元素的 RenderTransform 中的 TranslateTransform 应用平移偏移量。
        /// </summary>
        /// <param name="element">要平移的 UI 元素。</param>
        /// <param name="deltaX">在 X 方向上应用的平移增量（设备无关像素）。</param>
        /// <param name="deltaY">在 Y 方向上应用的平移增量（设备无关像素）。</param>
        /// <remarks>
        /// 若元素的 RenderTransform 不包含 TransformGroup 或其中不包含 TranslateTransform，本方法不执行任何操作。
        /// </remarks>
        private void ApplyTranslateTransform(FrameworkElement element, double deltaX, double deltaY)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (translateTransform != null)
                {
                    translateTransform.X += deltaX;
                    translateTransform.Y += deltaY;
                }
            }
        }

        /// <summary>
        /// 应用缩放变换到元素
        /// </summary>
        /// <param name="element">要变换的元素</param>
        /// <param name="scaleFactor">缩放因子</param>
        /// <param name="center">缩放中心</param>
        /// <remarks>
        /// - 获取元素的TransformGroup
        /// - 查找ScaleTransform
        /// - 设置缩放中心
        /// - 应用缩放
        /// - 限制缩放范围（0.1到5.0）
        /// <summary>
        /// 根据给定中心点和倍数对元素的缩放分量进行累积缩放并限制缩放范围。
        /// </summary>
        /// <param name="element">要应用缩放的 FrameworkElement，期望其 RenderTransform 为包含 ScaleTransform 的 TransformGroup。</param>
        /// <param name="scaleFactor">用于累积应用到当前缩放的缩放因子（例如 1.1 表示放大 10%）。</param>
        /// <param name="center">缩放中心点，相对于元素的坐标空间。</param>
        /// <remarks>
        /// 如果元素的 RenderTransform 不是包含 ScaleTransform 的 TransformGroup，则此方法不执行任何操作。缩放结果在 X/Y 方向上均被约束到 0.1 到 5.0 之间以防止过小或过大缩放。
        /// </remarks>
        private void ApplyScaleTransform(FrameworkElement element, double scaleFactor, Point center)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                if (scaleTransform != null)
                {
                    // 设置缩放中心
                    scaleTransform.CenterX = center.X;
                    scaleTransform.CenterY = center.Y;

                    // 应用缩放
                    scaleTransform.ScaleX *= scaleFactor;
                    scaleTransform.ScaleY *= scaleFactor;

                    // 限制缩放范围
                    scaleTransform.ScaleX = Math.Max(0.1, Math.Min(scaleTransform.ScaleX, 5.0));
                    scaleTransform.ScaleY = Math.Max(0.1, Math.Min(scaleTransform.ScaleY, 5.0));
                }
            }
        }

        /// <summary>
        /// 应用旋转变换到元素
        /// </summary>
        /// <param name="element">要变换的元素</param>
        /// <param name="angle">旋转角度</param>
        /// <remarks>
        /// - 获取元素的TransformGroup
        /// - 查找RotateTransform
        /// - 应用旋转变换
        /// <summary>
        /// 在元素的现有变换组中对其旋转分量增加指定角度。
        /// </summary>
        /// <param name="element">要应用旋转的 UI 元素；如果其 RenderTransform 不是 TransformGroup 或不包含 RotateTransform，则不做任何修改。</param>
        /// <param name="angle">要增加的旋转角度（以度为单位），将累加到现有 RotateTransform 的 Angle 上。</param>
        private void ApplyRotateTransform(FrameworkElement element, double angle)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                var rotateTransform = transformGroup.Children.OfType<RotateTransform>().FirstOrDefault();
                if (rotateTransform != null)
                {
                    rotateTransform.Angle += angle;
                }
            }
        }

        /// <summary>
        /// 选中元素
        /// </summary>
        /// <param name="element">要选中的元素</param>
        /// <remarks>
        /// - 设置当前选中元素
        /// - 根据元素类型显示不同的选择工具栏
        /// - 如果是图片元素，显示图片选择工具栏和缩放选择点
        /// - 如果不是图片元素，隐藏图片选择工具栏和缩放选择点
        /// - 确保选择框不显示，避免蓝色边框
        /// - 禁用InkCanvas的选择功能，去除控制点
        /// - 保持选择模式，这样用户可以直接点击墨迹来选择
        /// <summary>
        /// 将指定框架元素设为当前选中项并根据元素类型更新相关的选择 UI 与交互状态。
        /// </summary>
        /// <param name="element">要选中的元素；若为 Image 则显示图片专用工具栏与缩放控制点，非 Image 则隐藏这些控件。</param>
        /// <remarks>
        /// 该方法会：设置 currentSelectedElement，显示或隐藏图片选择工具栏和缩放把手，隐藏画布的默认蓝色选择覆盖，清空 InkCanvas 的 Stroke 选择并将 InkCanvas 编辑模式保持为 Select，以便用户可以通过点击选择墨迹对象。
        /// </remarks>
        private void SelectElement(FrameworkElement element)
        {
            currentSelectedElement = element;

            // 根据元素类型显示不同的选择工具栏
            if (element is Image)
            {
                // 显示图片选择工具栏并设置位置
                if (BorderImageSelectionControl != null)
                {
                    // 计算工具栏位置
                    UpdateImageSelectionToolbarPosition(element);
                    BorderImageSelectionControl.Visibility = Visibility.Visible;
                }

                // 显示图片缩放选择点
                ShowImageResizeHandles(element);

                // 墨迹选择工具栏通过GridInkCanvasSelectionCover的可见性来控制
                // 不需要直接设置BorderStrokeSelectionControl.Visibility
            }
            else
            {
                // 隐藏图片选择工具栏
                if (BorderImageSelectionControl != null)
                {
                    BorderImageSelectionControl.Visibility = Visibility.Collapsed;
                }

                // 隐藏图片缩放选择点
                HideImageResizeHandles();

                // 墨迹选择工具栏通过GridInkCanvasSelectionCover的可见性来控制
                // 不需要直接设置BorderStrokeSelectionControl.Visibility
            }

            // 确保选择框不显示，避免蓝色边框
            if (GridInkCanvasSelectionCover != null)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }

            // 禁用InkCanvas的选择功能，去除控制点
            if (inkCanvas != null)
            {
                // 清除当前选择
                inkCanvas.Select(new StrokeCollection());
                // 保持选择模式，这样用户可以直接点击墨迹来选择
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }
        }

        /// <summary>
        /// 取消选中元素
        /// </summary>
        /// <param name="element">要取消选中的元素</param>
        /// <remarks>
        /// - 隐藏图片选择工具栏
        /// - 隐藏图片缩放选择点
        /// - 确保选择框隐藏
        /// - 确保InkCanvas处于选择模式，这样用户可以直接点击墨迹来选择
        /// <summary>
        /// 取消指定元素的选中状态并隐藏与元素相关的选择控件和操作句柄。
        /// </summary>
        /// <param name="element">要取消选中的 FrameworkElement（例如画布上的 Image 或其他元素）。</param>
        private void UnselectElement(FrameworkElement element)
        {
            // 去除选中效果

            // 隐藏图片选择工具栏
            if (BorderImageSelectionControl != null)
            {
                BorderImageSelectionControl.Visibility = Visibility.Collapsed;
            }

            // 隐藏图片缩放选择点
            HideImageResizeHandles();

            // 墨迹选择工具栏通过GridInkCanvasSelectionCover的可见性来控制
            // 不需要直接设置BorderStrokeSelectionControl.Visibility

            // 确保选择框隐藏
            if (GridInkCanvasSelectionCover != null)
            {
                GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
            }

            // 确保InkCanvas处于选择模式，这样用户可以直接点击墨迹来选择
            if (inkCanvas != null)
            {
                inkCanvas.EditingMode = InkCanvasEditingMode.Select;
            }
        }

        /// <summary>
        /// 应用矩阵变换到元素
        /// </summary>
        /// <param name="element">要变换的元素</param>
        /// <param name="matrix">变换矩阵</param>
        /// <remarks>
        /// - 获取元素的RenderTransform，如果不存在则创建新的TransformGroup
        /// - 创建MatrixTransform
        /// - 将MatrixTransform添加到TransformGroup
        /// <summary>
        /// 将指定的矩阵作为一个 MatrixTransform 附加到目标元素 RenderTransform 的 TransformGroup 中。
        /// </summary>
        /// <param name="element">要附加变换的目标 FrameworkElement；仅当其 RenderTransform 为 TransformGroup 时才会生效。</param>
        /// <param name="matrix">要附加的 2D 变换矩阵。</param>
        /// <remarks>
        /// 如果元素的 RenderTransform 不是 TransformGroup，则不会进行任何操作。
        /// </remarks>
        private void ApplyElementMatrixTransform(FrameworkElement element, Matrix matrix)
        {
            if (element.RenderTransform is TransformGroup transformGroup)
            {
                // 创建MatrixTransform
                var matrixTransform = new MatrixTransform(matrix);

                // 将MatrixTransform添加到TransformGroup
                transformGroup.Children.Add(matrixTransform);
            }
        }

        /// <summary>
        /// 处理滚轮缩放的核心机制
        /// </summary>
        /// <param name="element">要缩放的元素</param>
        /// <param name="e">鼠标滚轮事件参数</param>
        /// <remarks>
        /// - 根据滚轮方向确定缩放比例（向上1.1倍，向下0.9倍）
        /// - 计算选中元素的中心点作为缩放中心
        /// - 创建 Matrix 对象并应用 ScaleAt 变换
        /// - 对选中的图片元素应用矩阵变换
        /// - 对选中的笔画应用 Transform 方法
        /// - 包含异常处理
        /// <summary>
        /// 基于鼠标滚轮围绕元素中心对指定元素进行缩放，并将相同变换应用于当前选中的笔画。
        /// </summary>
        /// <param name="element">要缩放的目标 FrameworkElement。</param>
        /// <param name="e">鼠标滚轮事件，向上滚动时放大，向下滚动时缩小。</param>
        /// <remarks>
        /// 缩放以元素中心为锚点；放大因子为 1.1，缩小因子为 0.9。方法还会将相同的矩阵变换应用到 inkCanvas 上当前被选中的笔画集合（如果存在）。异常将在内部记录并被吞掉，不会抛出到调用方。
        /// </remarks>
        private void ApplyWheelScaleTransform(FrameworkElement element, MouseWheelEventArgs e)
        {
            try
            {
                // 根据滚轮方向确定缩放比例（向上1.1倍，向下0.9倍）
                double scaleFactor = e.Delta > 0 ? 1.1 : 0.9;

                // 计算选中元素的中心点作为缩放中心
                var elementCenter = new Point(element.ActualWidth / 2, element.ActualHeight / 2);

                // 创建 Matrix 对象并应用 ScaleAt 变换
                var matrix = new Matrix();
                matrix.ScaleAt(scaleFactor, scaleFactor, elementCenter.X, elementCenter.Y);

                // 对选中的图片元素调用 ApplyElementMatrixTransform
                ApplyElementMatrixTransform(element, matrix);

                // 对选中的笔画应用 Transform 方法（如果有选中的笔画）
                var selectedStrokes = inkCanvas.GetSelectedStrokes();
                foreach (var stroke in selectedStrokes)
                {
                    stroke.Transform(matrix, false);
                }


            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"滚轮缩放失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 矩阵变换的完整实现
        /// </summary>
        /// <param name="element">要变换的元素</param>
        /// <param name="matrix">变换矩阵</param>
        /// <param name="saveHistory">是否保存历史记录</param>
        /// <remarks>
        /// - 获取元素的 RenderTransform，如果不存在则创建新的 TransformGroup
        /// - 保存初始变换状态用于历史记录
        /// - 创建新的 TransformGroup 并添加 MatrixTransform
        /// - 将新的变换组添加到现有的变换组中
        /// - 如果启用了历史记录，提交变换历史
        /// - 包含异常处理
        /// <summary>
        /// 将指定的矩阵作为 MatrixTransform 追加到目标元素的 RenderTransform 中，从而对元素应用该矩阵变换。
        /// </summary>
        /// <param name="element">要应用变换的目标 FrameworkElement。</param>
        /// <param name="matrix">表示要应用的变换的 2D 仿射矩阵。</param>
        /// <param name="saveHistory">若为 true，则在变换前保存元素的初始变换并将本次变换提交到变换历史以便撤销/重做。</param>
        private void ApplyMatrixTransformToElement(FrameworkElement element, Matrix matrix, bool saveHistory = true)
        {
            try
            {
                // 获取元素的 RenderTransform，如果不存在则创建新的 TransformGroup
                TransformGroup transformGroup = element.RenderTransform as TransformGroup;
                if (transformGroup == null)
                {
                    transformGroup = new TransformGroup();
                    element.RenderTransform = transformGroup;
                }

                // 保存初始变换状态用于历史记录
                var initialTransform = transformGroup.Clone();

                // 创建新的 TransformGroup 并添加 MatrixTransform
                var newTransformGroup = new TransformGroup();
                newTransformGroup.Children.Add(new MatrixTransform(matrix));

                // 将新的变换组添加到现有的变换组中
                transformGroup.Children.Add(newTransformGroup);

                // 如果启用了历史记录，提交变换历史
                if (saveHistory)
                {
                    CommitTransformHistory(element, initialTransform, transformGroup);
                }


            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"矩阵变换失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 鼠标拖动的完整实现机制
        /// </summary>
        /// <param name="element">要拖动的元素</param>
        /// <param name="currentPoint">当前鼠标位置</param>
        /// <param name="startPoint">起始鼠标位置</param>
        /// <remarks>
        /// - 计算鼠标移动的位移向量
        /// - 创建 Matrix 对象并应用 Translate 变换
        /// - 对选中的图片元素应用矩阵变换
        /// - 对选中的笔画应用变换
        /// - 更新选择框的位置（如果有选择框）
        /// - 包含异常处理
        /// <summary>
        /// 将鼠标拖动产生的平移应用到指定元素和当前选中的笔画，并同步更新选择框的位置。
        /// </summary>
        /// <param name="element">要应用平移的 FrameworkElement（通常为画布上的图像或媒体元素）。</param>
        /// <param name="currentPoint">鼠标或触控的当前坐标（相对于 InkCanvas）。</param>
        /// <param name="startPoint">拖动起始坐标（相对于 InkCanvas）。</param>
        private void ApplyMouseDragTransform(FrameworkElement element, Point currentPoint, Point startPoint)
        {
            try
            {
                // 计算鼠标移动的位移向量
                var delta = currentPoint - startPoint;

                // 创建 Matrix 对象并应用 Translate 变换
                var matrix = new Matrix();
                matrix.Translate(delta.X, delta.Y);

                // 对选中的图片元素应用矩阵变换
                ApplyMatrixTransformToElement(element, matrix, false);

                // 对选中的笔画应用变换
                var selectedStrokes = inkCanvas.GetSelectedStrokes();
                foreach (var stroke in selectedStrokes)
                {
                    stroke.Transform(matrix, false);
                }

                // 更新选择框的位置（如果有选择框）
                UpdateSelectionBorderPosition(delta);


            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"鼠标拖动失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 更新选择框位置
        /// </summary>
        /// <param name="delta">位移向量</param>
        /// <remarks>
        /// - 更新选择框位置的逻辑
        /// - 更新 BorderStrokeSelectionControl 的位置
        /// - 包含异常处理
        /// <summary>
        /// 将当前选择框的可视位置按照给定位移偏移。
        /// </summary>
        /// <param name="delta">要应用的位移向量，X 表示水平方向位移，Y 表示垂直方向位移（设备无关像素）。</param>
        private void UpdateSelectionBorderPosition(Vector delta)
        {
            try
            {
                // 这里可以添加更新选择框位置的逻辑
                // 例如更新 BorderStrokeSelectionControl 的位置
                if (BorderStrokeSelectionControl != null)
                {
                    var currentMargin = BorderStrokeSelectionControl.Margin;
                    BorderStrokeSelectionControl.Margin = new Thickness(
                        currentMargin.Left + delta.X,
                        currentMargin.Top + delta.Y,
                        currentMargin.Right,
                        currentMargin.Bottom
                    );
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"更新选择框位置失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 提交变换历史
        /// </summary>
        /// <param name="element">变换的元素</param>
        /// <param name="initialTransform">初始变换</param>
        /// <param name="finalTransform">最终变换</param>
        /// <remarks>
        /// - 提交变换历史到时间机器的逻辑
        /// - 记录变换前后的状态
        /// - 包含异常处理
        /// <summary>
        /// 将指定元素在变换前后的状态记录到变换历史（用于撤销/重做）。 
        /// </summary>
        /// <param name="element">要记录变换历史的框架元素。</param>
        /// <param name="initialTransform">变换前的 TransformGroup 快照。</param>
        /// <param name="finalTransform">变换后的 TransformGroup 快照。</param>
        private void CommitTransformHistory(FrameworkElement element, TransformGroup initialTransform, TransformGroup finalTransform)
        {
            try
            {
                // 这里可以添加提交变换历史到时间机器的逻辑
                // 例如记录变换前后的状态
                LogHelper.WriteLogToFile($"变换历史已记录: 元素={element.Name}");
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"提交变换历史失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 触摸拖动的完整实现
        /// </summary>
        /// <param name="element">要操作的元素</param>
        /// <param name="e">操作事件参数</param>
        /// <remarks>
        /// - 支持单指拖动和多指手势
        /// - 可以同时进行平移、旋转和缩放
        /// - 通过 ManipulationDelta 获取手势变化信息
        /// - 应用平移
        /// - 支持两指缩放和旋转操作
        /// - 应用变换到元素
        /// - 应用变换到选中的笔画
        /// - 包含异常处理
        /// <summary>
        /// 根据触摸操作的增量对指定元素及当前选中的笔画应用平移、缩放和旋转变换。
        /// </summary>
        /// <param name="element">要应用变换的目标 FrameworkElement。</param>
        /// <param name="e">提供触摸操作增量、操作者数量与操作中心点的 ManipulationDeltaEventArgs。</param>
        /// <remarks>
        /// - 单指操作用于平移；当操作者数量达到两个及以上时，额外支持以 ManipulationOrigin 为中心的缩放与旋转。 
        /// - 变换同时会应用到 InkCanvas 上当前选中的笔画，以保持元素与笔画的一致位置关系。
        /// - 方法在内部处理异常并记录错误日志，不会抛出异常给调用者。
        /// </remarks>
        private void ApplyTouchManipulationTransform(FrameworkElement element, ManipulationDeltaEventArgs e)
        {
            try
            {
                var md = e.DeltaManipulation;
                var matrix = new Matrix();

                // 支持单指拖动和多指手势
                // 可以同时进行平移、旋转和缩放

                // 通过 ManipulationDelta 获取手势变化信息
                var translation = md.Translation;
                var rotation = md.Rotation;
                var scale = md.Scale;

                // 应用平移
                if (translation.X != 0 || translation.Y != 0)
                {
                    matrix.Translate(translation.X, translation.Y);
                }

                // 支持两指缩放和旋转操作
                if (e.Manipulators.Count() >= 2)
                {
                    var center = e.ManipulationOrigin;

                    // 应用缩放
                    if (scale.X != 1.0 || scale.Y != 1.0)
                    {
                        matrix.ScaleAt(scale.X, scale.Y, center.X, center.Y);
                    }

                    // 应用旋转
                    if (rotation != 0)
                    {
                        matrix.RotateAt(rotation, center.X, center.Y);
                    }
                }

                // 应用变换到元素
                ApplyMatrixTransformToElement(element, matrix, false);

                // 应用变换到选中的笔画
                var selectedStrokes = inkCanvas.GetSelectedStrokes();
                foreach (var stroke in selectedStrokes)
                {
                    stroke.Transform(matrix, false);
                }


            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"触摸操作失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 创建并压缩图片
        /// </summary>
        /// <param name="filePath">图片文件路径</param>
        /// <returns>创建的Image对象</returns>
        /// <remarks>
        /// - 创建文件依赖目录
        /// - 复制文件到依赖目录
        /// - 创建BitmapImage
        /// - 如果图片尺寸大于1920x1080且设置了压缩图片，则压缩图片
        /// - 否则使用原始尺寸
        /// - 返回创建的Image对象
        /// <summary>
        /// 创建并准备一个 Image 控件：将源文件复制到依赖目录并在必要时按配置对图片进行压缩或缩放以限制尺寸。
        /// </summary>
        /// <param name="filePath">源图片文件的完整路径。</param>
        /// <returns>已准备好的 Image 控件，包含已加载的图像源、宽度、高度，并将 Stretch 设置为 Fill。</returns>
        /// <remarks>复制的文件保存在 Settings.Automation.AutoSavedStrokesLocation 下的 "File Dependency" 子目录；当应用已加载且 Settings.Canvas.IsCompressPicturesUploaded 为真且图片超过 1920×1080 时，会按保持纵横比的方式缩放到不超过该尺寸。</remarks>
        private async Task<Image> CreateAndCompressImageAsync(string filePath)
        {
            string savePath = Path.Combine(Settings.Automation.AutoSavedStrokesLocation, "File Dependency");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            string fileExtension = Path.GetExtension(filePath);
            string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
            string newFilePath = Path.Combine(savePath, timestamp + fileExtension);

            await Task.Run(() => File.Copy(filePath, newFilePath, true));

            return await Dispatcher.InvokeAsync(() =>
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(newFilePath);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                int width = bitmapImage.PixelWidth;
                int height = bitmapImage.PixelHeight;

                Image image = new Image();
                // 设置拉伸模式为Fill，支持任意比例缩放
                image.Stretch = Stretch.Fill;

                if (isLoaded && Settings.Canvas.IsCompressPicturesUploaded && (width > 1920 || height > 1080))
                {
                    double scaleX = 1920.0 / width;
                    double scaleY = 1080.0 / height;
                    double scale = Math.Min(scaleX, scaleY);

                    TransformedBitmap transformedBitmap = new TransformedBitmap(bitmapImage, new ScaleTransform(scale, scale));

                    image.Source = transformedBitmap;
                    image.Width = transformedBitmap.PixelWidth;
                    image.Height = transformedBitmap.PixelHeight;
                }
                else
                {
                    image.Source = bitmapImage;
                    image.Width = width;
                    image.Height = height;
                }

                return image;
            });
        }
        #endregion

        #region Media
        /// <summary>
        /// 处理媒体插入按钮点击事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 打开文件选择对话框，选择媒体文件
        /// - 读取媒体文件字节
        /// - 创建MediaElement
        /// - 居中缩放MediaElement
        /// - 设置位置并添加到画布
        /// - 设置LoadedBehavior和UnloadedBehavior为Manual
        /// - 媒体加载完成后播放并立即暂停
        /// - 提交到时间机器历史记录
        /// <summary>
        /// 通过文件对话框选择本地媒体文件，创建并初始化一个 MediaElement，将其置于画布上并记录插入历史。
        /// </summary>
        /// <param name="sender">触发事件的源对象（通常为插入按钮）。</param>
        /// <param name="e">事件参数。</param>
        private async void BtnMediaInsert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Media files (*.mp4; *.avi; *.wmv)|*.mp4;*.avi;*.wmv";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                byte[] mediaBytes = await Task.Run(() => File.ReadAllBytes(filePath));

                MediaElement mediaElement = await CreateMediaElementAsync(filePath);

                if (mediaElement != null)
                {
                    CenterAndScaleElement(mediaElement);

                    InkCanvas.SetLeft(mediaElement, 0);
                    InkCanvas.SetTop(mediaElement, 0);
                    inkCanvas.Children.Add(mediaElement);

                    mediaElement.LoadedBehavior = MediaState.Manual;
                    mediaElement.UnloadedBehavior = MediaState.Manual;
                    mediaElement.Loaded += async (_, args) =>
                    {
                        mediaElement.Play();
                        await Task.Delay(100);
                        mediaElement.Pause();
                    };

                    timeMachine.CommitElementInsertHistory(mediaElement);
                }
            }
        }

        /// <summary>
        /// 创建MediaElement
        /// </summary>
        /// <param name="filePath">媒体文件路径</param>
        /// <returns>创建的MediaElement对象</returns>
        /// <remarks>
        /// - 创建文件依赖目录
        /// - 创建MediaElement
        /// - 设置Source、名称、LoadedBehavior和UnloadedBehavior
        /// - 设置宽度和高度
        /// - 复制文件到依赖目录
        /// - 更新Source为新文件路径
        /// - 返回创建的MediaElement对象
        /// <summary>
        /// 为指定的媒体文件创建并准备一个可用于画布的 MediaElement 实例，且将源文件复制到应用的依赖目录中以供后续使用。
        /// </summary>
        /// <param name="filePath">源媒体文件的完整路径。</param>
        /// <returns>一个已初始化的 <see cref="MediaElement"/>，其 Source 指向复制到依赖目录中的文件，默认设置为手动加载/卸载并具有初始尺寸。</returns>
        private async Task<MediaElement> CreateMediaElementAsync(string filePath)
        {
            string savePath = Path.Combine(Settings.Automation.AutoSavedStrokesLocation, "File Dependency");
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
            return await Dispatcher.InvokeAsync(() =>
            {
                MediaElement mediaElement = new MediaElement();
                mediaElement.Source = new Uri(filePath);
                string timestamp = "media_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                mediaElement.Name = timestamp;
                mediaElement.LoadedBehavior = MediaState.Manual;
                mediaElement.UnloadedBehavior = MediaState.Manual;

                mediaElement.Width = 256;
                mediaElement.Height = 256;

                string fileExtension = Path.GetExtension(filePath);
                string newFilePath = Path.Combine(savePath, mediaElement.Name + fileExtension);

                File.Copy(filePath, newFilePath, true);

                mediaElement.Source = new Uri(newFilePath);

                return mediaElement;
            });
        }
        #endregion

        #region Image Operations

        /// <summary>
        /// 旋转图片
        /// </summary>
        /// <param name="image">要旋转的图片</param>
        /// <param name="angle">旋转角度（正数为顺时针，负数为逆时针）</param>
        private void RotateImage(Image image, double angle)
        {
            if (image == null) return;

            try
            {
                // 获取当前的变换
                var transformGroup = image.RenderTransform as TransformGroup ?? new TransformGroup();

                // 查找现有的旋转变换
                RotateTransform rotateTransform = null;
                foreach (Transform transform in transformGroup.Children)
                {
                    if (transform is RotateTransform rt)
                    {
                        rotateTransform = rt;
                        break;
                    }
                }

                // 如果没有旋转变换，创建一个新的
                if (rotateTransform == null)
                {
                    rotateTransform = new RotateTransform();
                    transformGroup.Children.Add(rotateTransform);
                }

                // 设置旋转中心为图片中心
                rotateTransform.CenterX = image.ActualWidth / 2;
                rotateTransform.CenterY = image.ActualHeight / 2;

                // 累加旋转角度
                rotateTransform.Angle = (rotateTransform.Angle + angle) % 360;

                // 应用变换
                image.RenderTransform = transformGroup;

                // 提交到时间机器以支持撤销
                // 注意：旋转操作目前不支持撤销，因为需要更复杂的历史记录机制
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                Debug.WriteLine($"旋转图片时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 克隆图片
        /// </summary>
        /// <param name="image">要克隆的图片</param>
        private Image CloneImage(Image image)
        {
            if (image == null) return null;

            try
            {
                // 创建图片的副本
                var clonedImage = new Image
                {
                    Source = image.Source,
                    Width = image.Width,
                    Height = image.Height,
                    Stretch = image.Stretch,
                    RenderTransform = image.RenderTransform?.Clone()
                };

                // 设置位置，稍微偏移以避免重叠
                InkCanvas.SetLeft(clonedImage, InkCanvas.GetLeft(image) + 20);
                InkCanvas.SetTop(clonedImage, InkCanvas.GetTop(image) + 20);

                // 设置图片属性，避免被InkCanvas选择系统处理
                clonedImage.IsHitTestVisible = true;
                clonedImage.Focusable = false;

                // 初始化变换
                InitializeElementTransform(clonedImage);

                // 绑定事件
                BindElementEvents(clonedImage);

                // 添加到画布
                inkCanvas.Children.Add(clonedImage);

                // 提交到时间机器以支持撤销
                timeMachine.CommitElementInsertHistory(clonedImage);

                return clonedImage;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogHelper.WriteLogToFile($"克隆图片时发生错误: {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 克隆图片到新页面
        /// </summary>
        /// <param name="image">要克隆的图片</param>
        private void CloneImageToNewBoard(Image image)
        {
            if (image == null) return;

            try
            {
                // 创建图片的副本
                var clonedImage = new Image
                {
                    Source = image.Source,
                    Width = image.Width,
                    Height = image.Height,
                    Stretch = image.Stretch,
                    RenderTransform = image.RenderTransform?.Clone()
                };

                // 设置位置，稍微偏移以避免重叠
                InkCanvas.SetLeft(clonedImage, InkCanvas.GetLeft(image) + 20);
                InkCanvas.SetTop(clonedImage, InkCanvas.GetTop(image) + 20);

                // 创建新页面
                BtnWhiteBoardAdd_Click(null, null);

                // 添加到新页面的画布
                inkCanvas.Children.Add(clonedImage);

                // 提交到时间机器以支持撤销
                timeMachine.CommitElementInsertHistory(clonedImage);
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                Debug.WriteLine($"克隆图片到新页面时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 缩放图片
        /// </summary>
        /// <param name="image">要缩放的图片</param>
        /// <param name="scaleFactor">缩放因子（大于1为放大，小于1为缩小）</param>
        private void ScaleImage(Image image, double scaleFactor)
        {
            if (image == null) return;

            try
            {
                // 获取当前的变换
                var transformGroup = image.RenderTransform as TransformGroup ?? new TransformGroup();

                // 查找现有的缩放变换
                ScaleTransform scaleTransform = null;
                foreach (Transform transform in transformGroup.Children)
                {
                    if (transform is ScaleTransform st)
                    {
                        scaleTransform = st;
                        break;
                    }
                }

                // 如果没有缩放变换，创建一个新的
                if (scaleTransform == null)
                {
                    scaleTransform = new ScaleTransform();
                    transformGroup.Children.Add(scaleTransform);
                }

                // 设置缩放中心为图片中心
                scaleTransform.CenterX = image.ActualWidth / 2;
                scaleTransform.CenterY = image.ActualHeight / 2;

                // 应用缩放因子
                scaleTransform.ScaleX *= scaleFactor;
                scaleTransform.ScaleY *= scaleFactor;

                // 应用变换
                image.RenderTransform = transformGroup;

                // 提交到时间机器以支持撤销
                // 注意：缩放操作目前不支持撤销，因为需要更复杂的历史记录机制
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                Debug.WriteLine($"缩放图片时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="image">要删除的图片</param>
        private void DeleteImage(Image image)
        {
            if (image == null) return;

            try
            {
                // 从画布中移除图片
                if (inkCanvas.Children.Contains(image))
                {
                    inkCanvas.Children.Remove(image);

                    // 提交到时间机器以支持撤销
                    timeMachine.CommitElementRemoveHistory(image);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                Debug.WriteLine($"删除图片时发生错误: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 居中并缩放元素
        /// </summary>
        /// <param name="element">要居中缩放的元素</param>
        /// <remarks>
        /// - 确保元素已加载且有有效尺寸
        /// - 如果元素尺寸无效，等待加载完成后再处理
        /// - 获取画布的实际尺寸
        /// - 如果画布尺寸为0，使用窗口尺寸作为备选
        /// - 如果仍然为0，使用屏幕尺寸
        /// - 计算最大允许尺寸（画布的70%）
        /// - 获取元素的当前尺寸
        /// - 计算缩放比例
        /// - 如果元素本身比最大尺寸小，不进行缩放
        /// - 计算新的尺寸
        /// - 设置元素尺寸
        /// - 计算居中位置
        /// - 确保位置不为负数
        /// - 设置位置
        /// - 保持TransformGroup，不清除RenderTransform
        /// - 只有在没有TransformGroup时才创建
        /// - 包含异常处理
        /// <summary>
        /// 将指定元素缩放（仅向下缩放以适配）并居中放置到主画布上，同时保留其现有变换以支持后续交互。
        /// </summary>
        /// <param name="element">要在 InkCanvas 上居中并按需缩小以适配显示区域的元素。</param>
        /// <remarks>
        /// 如果元素尚未完成布局或尺寸不可用，方法会在元素加载完成后重试。缩放以使元素宽高不超过画布可用尺寸的 70%，但不会放大元素；元素的 RenderTransform 将保留或在必要时初始化以维持交互行为（如拖动与缩放）。任何内部错误会被记录但不会抛出异常给调用者。
        /// </remarks>
        private void CenterAndScaleElement(FrameworkElement element)
        {
            try
            {
                // 确保元素已加载且有有效尺寸
                if (element == null || element.ActualWidth <= 0 || element.ActualHeight <= 0)
                {
                    // 如果元素尺寸无效，等待加载完成后再处理
                    element.Loaded += (sender, e) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            CenterAndScaleElement(element);
                        }), DispatcherPriority.Loaded);
                    };
                    return;
                }

                // 获取画布的实际尺寸
                double canvasWidth = inkCanvas.ActualWidth;
                double canvasHeight = inkCanvas.ActualHeight;

                // 如果画布尺寸为0，使用窗口尺寸作为备选
                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    canvasWidth = ActualWidth;
                    canvasHeight = ActualHeight;
                }

                // 如果仍然为0，使用屏幕尺寸
                if (canvasWidth <= 0 || canvasHeight <= 0)
                {
                    canvasWidth = SystemParameters.PrimaryScreenWidth;
                    canvasHeight = SystemParameters.PrimaryScreenHeight;
                }

                // 计算最大允许尺寸（画布的70%）
                double maxWidth = canvasWidth * 0.7;
                double maxHeight = canvasHeight * 0.7;

                // 获取元素的当前尺寸
                double elementWidth = element.ActualWidth;
                double elementHeight = element.ActualHeight;

                // 计算缩放比例
                double scaleX = maxWidth / elementWidth;
                double scaleY = maxHeight / elementHeight;
                double scale = Math.Min(scaleX, scaleY);

                // 如果元素本身比最大尺寸小，不进行缩放
                if (scale > 1.0)
                {
                    scale = 1.0;
                }

                // 计算新的尺寸
                double newWidth = elementWidth * scale;
                double newHeight = elementHeight * scale;

                // 设置元素尺寸
                element.Width = newWidth;
                element.Height = newHeight;

                // 计算居中位置
                double centerX = (canvasWidth - newWidth) / 2;
                double centerY = (canvasHeight - newHeight) / 2;

                // 确保位置不为负数
                centerX = Math.Max(0, centerX);
                centerY = Math.Max(0, centerY);

                // 设置位置
                InkCanvas.SetLeft(element, centerX);
                InkCanvas.SetTop(element, centerY);

                // 保持TransformGroup，不清除RenderTransform
                // 这样可以保持滚轮缩放和拖动功能
                if (element.RenderTransform == null || element.RenderTransform == Transform.Identity)
                {
                    // 只有在没有TransformGroup时才创建
                    InitializeElementTransform(element);
                }

                LogHelper.WriteLogToFile($"元素居中完成: 位置({centerX}, {centerY}), 尺寸({newWidth}x{newHeight})");
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"元素居中失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 初始化InkCanvas选择设置
        /// </summary>
        /// <remarks>
        /// - 清除当前选择，避免显示控制点
        /// - 设置编辑模式为非选择模式
        /// <summary>
        /// 重置 InkCanvas 的选择状态并禁用其编辑模式。
        /// </summary>
        /// <remarks>
        /// 如果内部的 inkCanvas 非空，则清除当前的笔迹选择以避免显示选择控制点，并将 EditingMode 设为 <c>None</c>，使画布进入非选择/非绘制的空闲状态。
        /// </remarks>
        private void InitializeInkCanvasSelectionSettings()
        {
            if (inkCanvas != null)
            {
                // 清除当前选择，避免显示控制点
                inkCanvas.Select(new StrokeCollection());
                // 设置编辑模式为非选择模式
                inkCanvas.EditingMode = InkCanvasEditingMode.None;
            }
        }

        /// <summary>
        /// 更新图片选择工具栏位置
        /// </summary>
        /// <param name="element">图片元素</param>
        /// <remarks>
        /// - 获取元素的实际边界（考虑变换）
        /// - 计算工具栏位置（显示在图片下方）
        /// - 确保工具栏不超出画布边界
        /// - 设置工具栏位置
        /// - 包含异常处理
        /// <summary>
        /// 将图片选择工具栏定位到指定元素下方并将其限制在画布可见区域内。
        /// </summary>
        /// <param name="element">要对齐工具栏的目标元素（通常为当前选中的 Image 或其他 FrameworkElement）。</param>
        private void UpdateImageSelectionToolbarPosition(FrameworkElement element)
        {
            try
            {
                if (BorderImageSelectionControl == null || element == null) return;

                // 获取元素的实际边界（考虑变换）
                Rect elementBounds = GetElementActualBounds(element);

                // 计算工具栏位置（显示在图片下方）
                double toolbarLeft = elementBounds.Left + (elementBounds.Width / 2) - (BorderImageSelectionControl.ActualWidth / 2);
                double toolbarTop = elementBounds.Bottom + 10; // 图片下方10像素

                // 确保工具栏不超出画布边界
                double maxLeft = inkCanvas.ActualWidth - BorderImageSelectionControl.ActualWidth;
                double maxTop = inkCanvas.ActualHeight - BorderImageSelectionControl.ActualHeight;

                toolbarLeft = Math.Max(0, Math.Min(toolbarLeft, maxLeft));
                toolbarTop = Math.Max(0, Math.Min(toolbarTop, maxTop));

                // 设置工具栏位置
                BorderImageSelectionControl.Margin = new Thickness(toolbarLeft, toolbarTop, 0, 0);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"更新图片选择工具栏位置失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 获取元素的实际边界（考虑变换）
        /// </summary>
        /// <param name="element">要获取边界的元素</param>
        /// <returns>元素的实际边界</returns>
        /// <remarks>
        /// - 获取元素的Left和Top位置
        /// - 如果值为NaN，设为0
        /// - 获取元素的宽度和高度
        /// - 检查是否有RenderTransform
        /// - 如果有变换，使用变换后的边界
        /// - 变换失败时回退到简单计算
        /// - 没有变换时直接使用位置和大小
        /// - 包含异常处理
        /// - 回退到基本计算
        /// <summary>
        /// 计算指定元素在 inkCanvas 坐标系中的实际边界矩形，尽可能考虑元素的 RenderTransform 带来的变换影响。
        /// </summary>
        /// <param name="element">位于当前 InkCanvas 上的 FrameworkElement，需计算其边界。</param>
        /// <returns>表示元素在 InkCanvas 坐标系中位置和大小的 Rect；如果无法应用变换则退回到基于 Left/Top/Width/Height 的边界。 </returns>
        private Rect GetElementActualBounds(FrameworkElement element)
        {
            try
            {
                var left = InkCanvas.GetLeft(element);
                var top = InkCanvas.GetTop(element);

                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;

                var width = element.ActualWidth > 0 ? element.ActualWidth : element.Width;
                var height = element.ActualHeight > 0 ? element.ActualHeight : element.Height;

                // 检查是否有RenderTransform
                if (element.RenderTransform != null && element.RenderTransform != Transform.Identity)
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
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"获取元素实际边界失败: {ex.Message}", LogHelper.LogType.Error);
                // 回退到基本计算
                var left = InkCanvas.GetLeft(element);
                var top = InkCanvas.GetTop(element);
                if (double.IsNaN(left)) left = 0;
                if (double.IsNaN(top)) top = 0;
                return new Rect(left, top, element.ActualWidth, element.ActualHeight);
            }
        }

        #region Image Selection Toolbar Event Handlers

        /// <summary>
        /// 处理图片克隆功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前选中元素是否为图片
        /// - 创建克隆图片
        /// - 添加到画布
        /// - 初始化变换
        /// - 绑定事件
        /// - 记录历史
        /// - 包含异常处理
        /// <summary>
        /// 在画布上克隆当前选中的 Image，并将克隆项加入画布与历史记录中以便撤销/重做。
        /// </summary>
        /// <param name="sender">事件的发送者（通常为克隆按钮的边框控件）。</param>
        /// <param name="e">鼠标按键事件参数。</param>
        /// <remarks>
        /// 若当前选中元素不是 Image 则不进行任何操作。方法内部会初始化克隆图片的变换、绑定交互事件并将操作记录到时间机器；发生错误时仅写入日志，不会向外抛出异常。
        /// </remarks>
        private void BorderImageClone_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement is Image originalImage)
                {
                    // 创建克隆图片
                    Image clonedImage = CloneImage(originalImage);

                    // 添加到画布
                    inkCanvas.Children.Add(clonedImage);

                    // 初始化变换
                    InitializeElementTransform(clonedImage);

                    // 绑定事件
                    BindElementEvents(clonedImage);

                    // 记录历史
                    timeMachine.CommitElementInsertHistory(clonedImage);

                    LogHelper.WriteLogToFile($"图片克隆完成: {clonedImage.Name}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片克隆失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片克隆到新页面功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前选中元素是否为图片
        /// - 创建新页面
        /// - 创建克隆图片
        /// - 设置图片属性，避免被InkCanvas选择系统处理
        /// - 初始化变换
        /// - 绑定事件
        /// - 添加到新页面的画布
        /// - 记录历史
        /// - 包含异常处理
        /// <summary>
        /// 在新页面上克隆当前选中的图片并将克隆件添加到新页面的画布。
        /// </summary>
        /// <remarks>
        /// 如果存在被选中的 Image，会创建一个新白板页面、生成该图片的克隆、初始化克隆的变换与事件并添加到新页面的 InkCanvas，同时将插入操作提交到历史记录并记录日志。
        /// 发生异常时会被捕获并记录日志。
        /// </remarks>
        private void BorderImageCloneToNewBoard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement is Image originalImage)
                {
                    // 创建新页面
                    BtnWhiteBoardAdd_Click(null, null);

                    // 创建克隆图片（不添加到当前画布，因为已经创建了新页面）
                    Image clonedImage = CreateClonedImage(originalImage);

                    if (clonedImage != null)
                    {
                        // 设置图片属性，避免被InkCanvas选择系统处理
                        clonedImage.IsHitTestVisible = true;
                        clonedImage.Focusable = false;

                        // 初始化变换
                        InitializeElementTransform(clonedImage);

                        // 绑定事件
                        BindElementEvents(clonedImage);

                        // 添加到新页面的画布
                        inkCanvas.Children.Add(clonedImage);

                        // 记录历史
                        timeMachine.CommitElementInsertHistory(clonedImage);

                        LogHelper.WriteLogToFile($"图片克隆到新页面完成: {clonedImage.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片克隆到新页面失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片左旋转功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前是否有选中元素
        /// - 应用旋转变换（向左旋转45度）
        /// - 如果是图片元素，更新工具栏位置
        /// - 包含异常处理
        /// <summary>
        /// 将当前选中元素向左旋转 45 度；若选中元素为图片且图片选择工具栏可见，则同步更新工具栏位置。
        /// </summary>
        private void BorderImageRotateLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement != null)
                {
                    ApplyRotateTransform(currentSelectedElement, -45);

                    // 更新工具栏位置
                    if (currentSelectedElement is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                    {
                        UpdateImageSelectionToolbarPosition(currentSelectedElement);
                    }

                    LogHelper.WriteLogToFile("图片左旋转完成");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片左旋转失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片右旋转功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前是否有选中元素
        /// - 应用旋转变换（向右旋转45度）
        /// - 如果是图片元素，更新工具栏位置
        /// - 包含异常处理
        /// <summary>
        /// 将当前选择的元素顺时针旋转 45 度，并在元素为图像且选择工具栏可见时更新工具栏位置。
        /// </summary>
        /// <param name="sender">触发该事件的控件。</param>
        /// <param name="e">与鼠标按键事件相关的参数。</param>
        private void BorderImageRotateRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement != null)
                {
                    ApplyRotateTransform(currentSelectedElement, 45);

                    // 更新工具栏位置
                    if (currentSelectedElement is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                    {
                        UpdateImageSelectionToolbarPosition(currentSelectedElement);
                    }

                    LogHelper.WriteLogToFile("图片右旋转完成");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片右旋转失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片缩放减小功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前是否有选中元素
        /// - 计算元素中心点
        /// - 应用缩放变换（缩小到0.9倍）
        /// - 如果是图片元素，更新工具栏位置
        /// - 包含异常处理
        /// <summary>
        /// 将当前选中的元素以其中心为基准按 0.9 的比例缩小，并在需要时更新图片选择工具栏位置。
        /// </summary>
        /// <remarks>
        /// 仅在存在选中元素时执行缩放操作；操作完成后写入日志，发生异常时记录错误日志。
        /// </remarks>
        private void GridImageScaleDecrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement != null)
                {
                    var elementCenter = new Point(currentSelectedElement.ActualWidth / 2, currentSelectedElement.ActualHeight / 2);
                    ApplyScaleTransform(currentSelectedElement, 0.9, elementCenter);

                    // 更新工具栏位置
                    if (currentSelectedElement is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                    {
                        UpdateImageSelectionToolbarPosition(currentSelectedElement);
                    }

                    LogHelper.WriteLogToFile("图片缩放减小完成");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片缩放减小失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片缩放增大功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前是否有选中元素
        /// - 计算元素中心点
        /// - 应用缩放变换（放大到1.1倍）
        /// - 如果是图片元素，更新工具栏位置
        /// - 包含异常处理
        /// <summary>
        /// 将当前选中元素按其中心放大 10%（缩放因子 1.1），并在选中元素为图片且图片选择工具栏可见时更新工具栏位置。
        /// </summary>
        /// <param name="sender">触发事件的对象（通常为缩放按钮或其容器）。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        private void GridImageScaleIncrease_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement != null)
                {
                    var elementCenter = new Point(currentSelectedElement.ActualWidth / 2, currentSelectedElement.ActualHeight / 2);
                    ApplyScaleTransform(currentSelectedElement, 1.1, elementCenter);

                    // 更新工具栏位置
                    if (currentSelectedElement is Image && BorderImageSelectionControl?.Visibility == Visibility.Visible)
                    {
                        UpdateImageSelectionToolbarPosition(currentSelectedElement);
                    }

                    LogHelper.WriteLogToFile("图片缩放增大完成");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片缩放增大失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 处理图片删除功能
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        /// <remarks>
        /// - 检查当前是否有选中元素
        /// - 保存删除前的编辑模式
        /// - 记录删除历史
        /// - 从画布中移除
        /// - 清除选中状态
        /// - 包含异常处理
        /// <summary>
        /// 删除当前选中的画布元素并记录删除历史，同时恢复删除前的编辑模式并记录日志。
        /// </summary>
        /// <remarks>
        /// 如果存在选中元素，方法会将其删除：提交到历史记录、从 InkCanvas 移除、清除选择并将 EditingMode 恢复到删除前的状态；操作结果会写入日志，异常会被捕获并记录。
        /// </remarks>
        private void BorderImageDelete_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement != null)
                {
                    // 保存删除前的编辑模式
                    var previousEditingMode = inkCanvas.EditingMode;

                    // 记录删除历史
                    timeMachine.CommitElementRemoveHistory(currentSelectedElement);

                    // 从画布中移除
                    inkCanvas.Children.Remove(currentSelectedElement);

                    // 清除选中状态
                    UnselectElement(currentSelectedElement);
                    currentSelectedElement = null;

                    // 恢复到删除前的编辑模式
                    inkCanvas.EditingMode = previousEditingMode;

                    LogHelper.WriteLogToFile($"图片删除完成，已恢复到编辑模式: {previousEditingMode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片删除失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 克隆图片的辅助方法（只创建图片，不添加到画布）
        private Image CreateClonedImage(Image originalImage)
        {
            try
            {
                Image clonedImage = new Image();

                // 复制图片源
                if (originalImage.Source is BitmapSource bitmapSource)
                {
                    clonedImage.Source = bitmapSource;
                }

                // 复制属性
                clonedImage.Width = originalImage.Width;
                clonedImage.Height = originalImage.Height;
                clonedImage.Stretch = originalImage.Stretch;
                clonedImage.StretchDirection = originalImage.StretchDirection;

                // 复制位置（在新页面中居中显示）
                double left = InkCanvas.GetLeft(originalImage);
                double top = InkCanvas.GetTop(originalImage);
                InkCanvas.SetLeft(clonedImage, left + 20); // 稍微偏移位置
                InkCanvas.SetTop(clonedImage, top + 20);

                // 复制变换
                if (originalImage.RenderTransform is TransformGroup originalTransformGroup)
                {
                    clonedImage.RenderTransform = originalTransformGroup.Clone();
                }

                // 设置名称
                string timestamp = "img_" + DateTime.Now.ToString("yyyyMMdd_HH_mm_ss_fff");
                clonedImage.Name = timestamp;

                return clonedImage;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"克隆图片失败: {ex.Message}", LogHelper.LogType.Error);
                return null;
            }
        }

        /// <summary>
        /// 克隆墨迹集合
        /// </summary>
        /// <param name="strokes">要克隆的墨迹集合</param>
        /// <returns>克隆后的墨迹集合</returns>
        private StrokeCollection CloneStrokes(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return new StrokeCollection();

            try
            {
                // 创建墨迹集合的副本
                var clonedStrokes = strokes.Clone();

                // 为每个墨迹添加位置偏移以避免重叠
                foreach (var stroke in clonedStrokes)
                {
                    var offsetPoints = new StylusPointCollection();
                    foreach (var point in stroke.StylusPoints)
                    {
                        offsetPoints.Add(new StylusPoint(point.X + 20, point.Y + 20, point.PressureFactor));
                    }
                    stroke.StylusPoints = offsetPoints;
                }

                // 添加到画布
                inkCanvas.Strokes.Add(clonedStrokes);

                // 提交到时间机器以支持撤销
                timeMachine.CommitStrokeUserInputHistory(clonedStrokes);

                LogHelper.WriteLogToFile($"墨迹克隆完成: {clonedStrokes.Count} 个墨迹");
                return clonedStrokes;
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogHelper.WriteLogToFile($"克隆墨迹时发生错误: {ex.Message}", LogHelper.LogType.Error);
                return new StrokeCollection();
            }
        }

        /// <summary>
        /// 克隆墨迹集合到新页面
        /// </summary>
        /// <param name="strokes">要克隆的墨迹集合</param>
        private void CloneStrokesToNewBoard(StrokeCollection strokes)
        {
            if (strokes == null || strokes.Count == 0) return;

            try
            {
                // 创建墨迹集合的副本
                var clonedStrokes = strokes.Clone();

                // 为每个墨迹添加位置偏移以避免重叠
                foreach (var stroke in clonedStrokes)
                {
                    var offsetPoints = new StylusPointCollection();
                    foreach (var point in stroke.StylusPoints)
                    {
                        offsetPoints.Add(new StylusPoint(point.X + 20, point.Y + 20, point.PressureFactor));
                    }
                    stroke.StylusPoints = offsetPoints;
                }

                // 创建新页面
                BtnWhiteBoardAdd_Click(null, null);

                // 添加到新页面的画布
                inkCanvas.Strokes.Add(clonedStrokes);

                // 提交到时间机器以支持撤销
                timeMachine.CommitStrokeUserInputHistory(clonedStrokes);

                LogHelper.WriteLogToFile($"墨迹克隆到新页面完成: {clonedStrokes.Count} 个墨迹");
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                LogHelper.WriteLogToFile($"克隆墨迹到新页面时发生错误: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        #endregion

        #region Image Resize Handles

        // 图片缩放选择点相关变量
        private bool isResizingImage = false;
        private Point imageResizeStartPoint;
        private string activeResizeHandle = "";

        // 显示图片缩放选择点
        private void ShowImageResizeHandles(FrameworkElement element)
        {
            try
            {
                if (ImageResizeHandlesCanvas == null || element == null) return;

                // 获取元素的实际边界
                Rect elementBounds = GetElementActualBounds(element);

                // 设置选择点位置
                UpdateImageResizeHandlesPosition(elementBounds);

                // 显示选择点
                ImageResizeHandlesCanvas.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"显示图片缩放选择点失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 隐藏图片缩放选择点
        private void HideImageResizeHandles()
        {
            try
            {
                if (ImageResizeHandlesCanvas != null)
                {
                    ImageResizeHandlesCanvas.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"隐藏图片缩放选择点失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 更新图片缩放选择点位置
        private void UpdateImageResizeHandlesPosition(Rect elementBounds)
        {
            try
            {
                if (ImageResizeHandlesCanvas == null) return;

                ImageResizeHandlesCanvas.Margin = new Thickness(elementBounds.Left, elementBounds.Top, 0, 0);

                // 四个角控制点
                System.Windows.Controls.Canvas.SetLeft(ImageTopLeftHandle, -4);
                System.Windows.Controls.Canvas.SetTop(ImageTopLeftHandle, -4);

                System.Windows.Controls.Canvas.SetLeft(ImageTopRightHandle, elementBounds.Width - 4);
                System.Windows.Controls.Canvas.SetTop(ImageTopRightHandle, -4);

                System.Windows.Controls.Canvas.SetLeft(ImageBottomLeftHandle, -4);
                System.Windows.Controls.Canvas.SetTop(ImageBottomLeftHandle, elementBounds.Height - 4);

                System.Windows.Controls.Canvas.SetLeft(ImageBottomRightHandle, elementBounds.Width - 4);
                System.Windows.Controls.Canvas.SetTop(ImageBottomRightHandle, elementBounds.Height - 4);

                // 四个边控制点
                System.Windows.Controls.Canvas.SetLeft(ImageTopHandle, elementBounds.Width / 2 - 4);
                System.Windows.Controls.Canvas.SetTop(ImageTopHandle, -4);

                System.Windows.Controls.Canvas.SetLeft(ImageBottomHandle, elementBounds.Width / 2 - 4);
                System.Windows.Controls.Canvas.SetTop(ImageBottomHandle, elementBounds.Height - 4);

                System.Windows.Controls.Canvas.SetLeft(ImageLeftHandle, -4);
                System.Windows.Controls.Canvas.SetTop(ImageLeftHandle, elementBounds.Height / 2 - 4);

                System.Windows.Controls.Canvas.SetLeft(ImageRightHandle, elementBounds.Width - 4);
                System.Windows.Controls.Canvas.SetTop(ImageRightHandle, elementBounds.Height / 2 - 4);
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"更新图片缩放选择点位置失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 图片缩放选择点鼠标按下事件
        private void ImageResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (currentSelectedElement is Image image && sender is Ellipse ellipse)
                {
                    isResizingImage = true;
                    imageResizeStartPoint = e.GetPosition(inkCanvas);

                    // 确定是哪个控制点
                    activeResizeHandle = ellipse.Name;

                    // 捕获鼠标
                    ellipse.CaptureMouse();
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片缩放选择点鼠标按下事件失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 图片缩放选择点鼠标释放事件
        private void ImageResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (isResizingImage && sender is Ellipse ellipse)
                {
                    isResizingImage = false;
                    ellipse.ReleaseMouseCapture();
                    activeResizeHandle = "";
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片缩放选择点鼠标释放事件失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 图片缩放选择点鼠标移动事件
        private void ImageResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (isResizingImage && currentSelectedElement is Image image && sender is Ellipse ellipse)
                {
                    var currentPoint = e.GetPosition(inkCanvas);
                    ResizeImageByHandle(image, imageResizeStartPoint, currentPoint, activeResizeHandle);
                    imageResizeStartPoint = currentPoint;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"图片缩放选择点鼠标移动事件失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        // 根据控制点缩放图片
        private void ResizeImageByHandle(Image image, Point startPoint, Point currentPoint, string handleName)
        {
            try
            {
                if (image.RenderTransform is TransformGroup transformGroup)
                {
                    var scaleTransform = transformGroup.Children.OfType<ScaleTransform>().FirstOrDefault();
                    var translateTransform = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();

                    if (scaleTransform == null || translateTransform == null) return;

                    // 获取图片的当前边界
                    Rect currentBounds = GetElementActualBounds(image);
                    double deltaX = currentPoint.X - startPoint.X;
                    double deltaY = currentPoint.Y - startPoint.Y;

                    // 计算缩放比例
                    double scaleX = 1.0;
                    double scaleY = 1.0;
                    double translateX = 0;
                    double translateY = 0;

                    switch (handleName)
                    {
                        case "ImageTopLeftHandle":
                            scaleX = (currentBounds.Width - deltaX) / currentBounds.Width;
                            scaleY = (currentBounds.Height - deltaY) / currentBounds.Height;
                            translateX = deltaX;
                            translateY = deltaY;
                            break;
                        case "ImageTopRightHandle":
                            scaleX = (currentBounds.Width + deltaX) / currentBounds.Width;
                            scaleY = (currentBounds.Height - deltaY) / currentBounds.Height;
                            translateY = deltaY;
                            break;
                        case "ImageBottomLeftHandle":
                            scaleX = (currentBounds.Width - deltaX) / currentBounds.Width;
                            scaleY = (currentBounds.Height + deltaY) / currentBounds.Height;
                            translateX = deltaX;
                            break;
                        case "ImageBottomRightHandle":
                            scaleX = (currentBounds.Width + deltaX) / currentBounds.Width;
                            scaleY = (currentBounds.Height + deltaY) / currentBounds.Height;
                            break;
                        case "ImageTopHandle":
                            scaleY = (currentBounds.Height - deltaY) / currentBounds.Height;
                            translateY = deltaY;
                            break;
                        case "ImageBottomHandle":
                            scaleY = (currentBounds.Height + deltaY) / currentBounds.Height;
                            break;
                        case "ImageLeftHandle":
                            scaleX = (currentBounds.Width - deltaX) / currentBounds.Width;
                            translateX = deltaX;
                            break;
                        case "ImageRightHandle":
                            scaleX = (currentBounds.Width + deltaX) / currentBounds.Width;
                            break;
                    }

                    // 限制缩放范围
                    scaleX = Math.Max(0.1, Math.Min(scaleX, 5.0));
                    scaleY = Math.Max(0.1, Math.Min(scaleY, 5.0));

                    // 应用缩放
                    scaleTransform.ScaleX *= scaleX;
                    scaleTransform.ScaleY *= scaleY;

                    // 应用平移
                    translateTransform.X += translateX;
                    translateTransform.Y += translateY;

                    // 更新选择点位置
                    UpdateImageResizeHandlesPosition(GetElementActualBounds(image));
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"根据控制点缩放图片失败: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        #endregion
    }
}