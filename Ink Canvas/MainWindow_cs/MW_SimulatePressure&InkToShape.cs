using Ink_Canvas.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace Ink_Canvas
{
    /// <summary>
    /// 主窗口类的部分类，包含压感模拟和墨水到形状识别的功能
    /// </summary>
    /// <remarks>
    /// 本文件主要包含以下功能：
    /// 1. 压感模拟：根据输入设备类型和设置模拟不同的压感效果
    /// 2. 墨水到形状识别：将手绘墨迹转换为规则形状（直线、圆形、椭圆、三角形、矩形等）
    /// 3. 直线自动拉直：将近似直线的墨迹自动拉成直线
    /// 4. 端点吸附：将直线端点吸附到其他直线的端点
    /// 5. 矩形参考线系统：通过多条直线构成矩形
    /// 6. 高级贝塞尔曲线平滑：对墨迹进行平滑处理
    /// 7. 异步墨水处理：提高性能的异步墨水处理机制
    /// </remarks>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 存储新的笔画集合，用于形状识别
        /// </summary>
        private StrokeCollection newStrokes = new StrokeCollection();

        /// <summary>
        /// 存储圆形形状的列表
        /// </summary>
        private List<Circle> circles = new List<Circle>();



        /// <summary>
        /// 矩形参考线的列表
        /// </summary>
        private List<RectangleGuideLine> rectangleGuideLines = new List<RectangleGuideLine>();

        /// <summary>
        /// 矩形端点的阈值
        /// </summary>
        private const double RECTANGLE_ENDPOINT_THRESHOLD = 30.0;

        /// <summary>
        /// 矩形角度的阈值
        /// </summary>
        private const double RECTANGLE_ANGLE_THRESHOLD = 15.0;

        /// <summary>
        /// 矩形参考线类，用于辅助矩形绘制
        /// </summary>
        private class RectangleGuideLine
        {
            /// <summary>
            /// 原始笔画
            /// </summary>
            public Stroke OriginalStroke { get; set; }

            /// <summary>
            /// 起始点
            /// </summary>
            public Point StartPoint { get; set; }

            /// <summary>
            /// 结束点
            /// </summary>
            public Point EndPoint { get; set; }

            /// <summary>
            /// 创建时间
            /// </summary>
            public DateTime CreatedTime { get; set; }

            /// <summary>
            /// 角度
            /// </summary>
            public double Angle { get; set; }

            /// <summary>
            /// 是否为水平线
            /// </summary>
            public bool IsHorizontal { get; set; }

            /// <summary>
            /// 是否为垂直线
            /// </summary>
            public bool IsVertical { get; set; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="stroke">原始笔画</param>
            /// <param name="start">起始点</param>
            /// <summary>
            /// 初始化表示矩形参考线的实例，并计算其角度与水平/垂直属性。
            /// </summary>
            /// <param name="stroke">该参考线对应的原始 Stroke 对象。</param>
            /// <param name="start">参考线的起点。</param>
            /// <param name="end">参考线的终点。</param>
            public RectangleGuideLine(Stroke stroke, Point start, Point end)
            {
                OriginalStroke = stroke;
                StartPoint = start;
                EndPoint = end;
                CreatedTime = DateTime.Now;

                // 计算角度
                double deltaX = end.X - start.X;
                double deltaY = end.Y - start.Y;
                Angle = Math.Atan2(deltaY, deltaX);

                // 判断是否为水平或垂直线
                double angleDegrees = Math.Abs(Angle * 180.0 / Math.PI);
                IsHorizontal = angleDegrees < RECTANGLE_ANGLE_THRESHOLD || angleDegrees > (180 - RECTANGLE_ANGLE_THRESHOLD);
                IsVertical = Math.Abs(angleDegrees - 90) < RECTANGLE_ANGLE_THRESHOLD;
            }
        }

        /// <summary>
        /// 处理墨水画布的笔画收集事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">笔画收集事件参数</param>
        /// <remarks>
        /// 当用户在墨水画布上完成一笔绘制后：
        /// 1. 检查是否启用墨迹渐隐功能，如果启用则添加到墨迹渐隐管理器
        /// 2. 根据设置处理压感：
        ///    - 如果禁用压感，统一压感值为0.5
        ///    - 如果启用触摸压感模式，根据速度模拟压感
        /// 3. 如果启用了形状识别：
        ///    - 检查是否启用了直线自动拉直功能，如果是则尝试拉直线条
        ///    - 处理形状识别，包括圆形、椭圆、三角形、矩形等
        /// 4. 检查是否是压感笔书写，如果是则返回
        /// 5. 根据墨水风格设置模拟压感
        /// 6. 应用高级贝塞尔曲线平滑（仅在未进行直线拉直时）
        /// <para>
        /// 注意：形状识别（圆形、椭圆、三角形、矩形等）仅在32位进程中可用。当 Environment.Is64BitProcess 为 true 时，形状识别功能会被禁用。
        /// </para>
        /// <summary>
        /// 处理 InkCanvas 的 StrokeCollected 事件：根据用户设置对新笔画执行压感调整、直线拉直与端点吸附、墨迹渐隐、形状识别（包括圆、椭圆、矩形、三角形等）以及可选的同步/异步贝塞尔平滑，并在需要时替换画布上的笔画。
        /// </summary>
        /// <param name="sender">事件源（触发 InkCanvas 的对象）。</param>
        /// <param name="e">包含已采集笔画信息的事件参数；方法会读取并可能修改或替换 e.Stroke 对应的笔画集合。</param>
        private void inkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            // 检查是否启用墨迹渐隐功能
            if (Settings.Canvas.EnableInkFade)
            {
                // 获取墨迹的起点和终点
                var startPoint = e.Stroke.StylusPoints.Count > 0 ? e.Stroke.StylusPoints[0].ToPoint() : new Point();
                var endPoint = e.Stroke.StylusPoints.Count > 0 ? e.Stroke.StylusPoints[e.Stroke.StylusPoints.Count - 1].ToPoint() : new Point();

                if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink)
                {
                    inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                }

                // 添加到墨迹渐隐管理器
                if (_inkFadeManager != null)
                {
                    _inkFadeManager.AddFadingStroke(e.Stroke, startPoint, endPoint);
                }
                else
                {
                    LogHelper.WriteLogToFile("StrokeCollected: 墨迹渐隐管理器为空，无法添加墨迹", LogHelper.LogType.Error);
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink)
                        {
                            inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                        }

                        if (inkCanvas.Strokes.Contains(e.Stroke))
                        {
                            inkCanvas.Strokes.Remove(e.Stroke);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.WriteLogToFile($"延迟移除墨迹时出错: {ex}", LogHelper.LogType.Error);
                    }
                }), DispatcherPriority.Background);

                return;
            }

            // 标记是否进行了直线拉直
            bool wasStraightened = false;

            if (Settings.Canvas.FitToCurve) drawingAttributes.FitToCurve = false;

            try
            {
                inkCanvas.Opacity = 1;

                if (Settings.Canvas.DisablePressure)
                {
                    var uniformPoints = new StylusPointCollection();
                    foreach (StylusPoint point in e.Stroke.StylusPoints)
                    {
                        StylusPoint newPoint = new StylusPoint(point.X, point.Y, 0.5f); // 统一压感值为0.5
                        uniformPoints.Add(newPoint);
                    }
                    e.Stroke.StylusPoints = uniformPoints;
                }
                else if (Settings.Canvas.EnablePressureTouchMode)
                {
                    bool isTouchInput = true;
                    foreach (StylusPoint point in e.Stroke.StylusPoints)
                    {
                        if ((point.PressureFactor > 0.501 || point.PressureFactor < 0.5) && point.PressureFactor != 0)
                        {
                            isTouchInput = false;
                            break;
                        }
                    }

                    if (isTouchInput)
                    {
                        switch (Settings.Canvas.InkStyle)
                        {
                            case 1:
                                if (penType == 0)
                                    try
                                    {
                                        var stylusPoints = new StylusPointCollection();
                                        var n = e.Stroke.StylusPoints.Count - 1;

                                        for (var i = 0; i <= n; i++)
                                        {
                                            var speed = GetPointSpeed(e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(),
                                                e.Stroke.StylusPoints[i].ToPoint(),
                                                e.Stroke.StylusPoints[Math.Min(i + 1, n)].ToPoint());
                                            var point = new StylusPoint();
                                            if (speed >= 0.25)
                                                point.PressureFactor = (float)(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2);
                                            else if (speed >= 0.05)
                                                point.PressureFactor = (float)0.5;
                                            else
                                                point.PressureFactor = (float)(0.5 + 0.4 * (0.05 - speed) / 0.05);

                                            point.X = e.Stroke.StylusPoints[i].X;
                                            point.Y = e.Stroke.StylusPoints[i].Y;
                                            stylusPoints.Add(point);
                                        }

                                        e.Stroke.StylusPoints = stylusPoints;
                                    }
                                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                                break;
                            case 0:
                                if (penType == 0)
                                    try
                                    {
                                        var stylusPoints = new StylusPointCollection();
                                        var n = e.Stroke.StylusPoints.Count - 1;
                                        var pressure = 0.1;
                                        var x = 10;
                                        if (n == 1) return;
                                        if (n >= x)
                                        {
                                            for (var i = 0; i < n - x; i++)
                                            {
                                                var point = new StylusPoint();

                                                point.PressureFactor = (float)0.5;
                                                point.X = e.Stroke.StylusPoints[i].X;
                                                point.Y = e.Stroke.StylusPoints[i].Y;
                                                stylusPoints.Add(point);
                                            }

                                            for (var i = n - x; i <= n; i++)
                                            {
                                                var point = new StylusPoint();

                                                point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                                point.X = e.Stroke.StylusPoints[i].X;
                                                point.Y = e.Stroke.StylusPoints[i].Y;
                                                stylusPoints.Add(point);
                                            }
                                        }
                                        else
                                        {
                                            for (var i = 0; i <= n; i++)
                                            {
                                                var point = new StylusPoint();

                                                point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                                point.X = e.Stroke.StylusPoints[i].X;
                                                point.Y = e.Stroke.StylusPoints[i].Y;
                                                stylusPoints.Add(point);
                                            }
                                        }

                                        e.Stroke.StylusPoints = stylusPoints;
                                    }
                                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                                break;
                        }
                    }
                }

                // Apply line straightening and endpoint snapping if ink-to-shape is enabled

                if (Settings.InkToShape.IsInkToShapeEnabled)
                {
                    // 检查是否启用了直线自动拉直功能
                    if (Settings.Canvas.AutoStraightenLine && IsPotentialStraightLine(e.Stroke))
                    {
                        Point endpoint1, endpoint2;
                        bool shouldStraighten = TryGetStraightLineEndpoints(e.Stroke, out endpoint1, out endpoint2);

                        if (shouldStraighten)
                        {
                            Point startPoint = endpoint1;
                            Point endPoint = endpoint2;

                            // 只有当确定要拉直线条时，才检查端点吸附
                            if (Settings.Canvas.LineEndpointSnapping)
                            {
                                // 只有在启用了形状识别（矩形或三角形）时才执行端点吸附
                                if (Settings.InkToShape.IsInkToShapeRectangle || Settings.InkToShape.IsInkToShapeTriangle)
                                {
                                    Point[] snappedPoints = GetSnappedEndpoints(startPoint, endPoint);
                                    if (snappedPoints != null)
                                    {
                                        startPoint = snappedPoints[0];
                                        endPoint = snappedPoints[1];
                                    }
                                }
                            }

                            // 创建直线
                            StylusPointCollection straightLinePoints = CreateStraightLine(startPoint, endPoint);
                            Stroke straightStroke = new Stroke(straightLinePoints)
                            {
                                DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                            };

                            // Replace the original stroke with the straightened one
                            SetNewBackupOfStroke();
                            _currentCommitType = CommitReason.ShapeRecognition;
                            inkCanvas.Strokes.Remove(e.Stroke);
                            inkCanvas.Strokes.Add(straightStroke);
                            _currentCommitType = CommitReason.UserInput;

                            // We can't modify e.Stroke directly, but we need to update newStrokes
                            // to ensure proper shape recognition for the straightened line
                            if (newStrokes.Contains(e.Stroke))
                            {
                                newStrokes.Remove(e.Stroke);
                                newStrokes.Add(straightStroke);
                            }

                            wasStraightened = true; // 标记已进行直线拉直
                        }
                    }
                }

                if (Settings.InkToShape.IsInkToShapeEnabled && !Environment.Is64BitProcess)
                {
                    void InkToShapeProcess()
                    {
                        try
                        {
                            newStrokes.Add(e.Stroke);
                            if (newStrokes.Count > 4) newStrokes.RemoveAt(0);
                            for (var i = 0; i < newStrokes.Count; i++)
                                if (!inkCanvas.Strokes.Contains(newStrokes[i]))
                                    newStrokes.RemoveAt(i--);

                            for (var i = 0; i < circles.Count; i++)
                                if (!inkCanvas.Strokes.Contains(circles[i].Stroke))
                                    circles.RemoveAt(i);

                            // 处理矩形参考线系统
                            ProcessRectangleGuideLines(e.Stroke);

                            var strokeReco = new StrokeCollection();
                            var result = InkRecognizeHelper.RecognizeShape(newStrokes);
                            for (var i = newStrokes.Count - 1; i >= 0; i--)
                            {
                                strokeReco.Add(newStrokes[i]);
                                var newResult = InkRecognizeHelper.RecognizeShape(strokeReco);
                                if (newResult.InkDrawingNode.GetShapeName() == "Circle" ||
                                    newResult.InkDrawingNode.GetShapeName() == "Ellipse")
                                {
                                    result = newResult;
                                    break;
                                }
                                //Label.Visibility = Visibility.Visible;
                                //Label.Content = circles.Count.ToString() + "\n" + newResult.InkDrawingNode.GetShapeName();
                            }

                            if (result.InkDrawingNode.GetShapeName() == "Circle" &&
                                Settings.InkToShape.IsInkToShapeRounded)
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                if (shape.Width > 75)
                                {
                                    foreach (var circle in circles)
                                        //判断是否画同心圆
                                        if (Math.Abs(result.Centroid.X - circle.Centroid.X) / shape.Width < 0.12 &&
                                            Math.Abs(result.Centroid.Y - circle.Centroid.Y) / shape.Width < 0.12)
                                        {
                                            result.Centroid = circle.Centroid;
                                            break;
                                        }
                                        else
                                        {
                                            var d = (result.Centroid.X - circle.Centroid.X) *
                                                    (result.Centroid.X - circle.Centroid.X) +
                                                    (result.Centroid.Y - circle.Centroid.Y) *
                                                    (result.Centroid.Y - circle.Centroid.Y);
                                            d = Math.Sqrt(d);
                                            //判断是否画外切圆
                                            var x = shape.Width / 2.0 + circle.R - d;
                                            if (Math.Abs(x) / shape.Width < 0.1)
                                            {
                                                var sinTheta = (result.Centroid.Y - circle.Centroid.Y) / d;
                                                var cosTheta = (result.Centroid.X - circle.Centroid.X) / d;
                                                var newX = result.Centroid.X + x * cosTheta;
                                                var newY = result.Centroid.Y + x * sinTheta;
                                                result.Centroid = new Point(newX, newY);
                                            }

                                            //判断是否画外切圆
                                            x = Math.Abs(circle.R - shape.Width / 2.0) - d;
                                            if (Math.Abs(x) / shape.Width < 0.1)
                                            {
                                                var sinTheta = (result.Centroid.Y - circle.Centroid.Y) / d;
                                                var cosTheta = (result.Centroid.X - circle.Centroid.X) / d;
                                                var newX = result.Centroid.X + x * cosTheta;
                                                var newY = result.Centroid.Y + x * sinTheta;
                                                result.Centroid = new Point(newX, newY);
                                            }
                                        }

                                    var iniP = new Point(result.Centroid.X - shape.Width / 2,
                                        result.Centroid.Y - shape.Height / 2);
                                    var endP = new Point(result.Centroid.X + shape.Width / 2,
                                        result.Centroid.Y + shape.Height / 2);
                                    var pointList = GenerateEllipseGeometry(iniP, endP);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(point)
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    circles.Add(new Circle(result.Centroid, shape.Width / 2.0, stroke));
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if (result.InkDrawingNode.GetShapeName().Contains("Ellipse") &&
                                     Settings.InkToShape.IsInkToShapeRounded)
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                //var shape1 = result.InkDrawingNode.GetShape();
                                //shape1.Fill = Brushes.Gray;
                                //Canvas.Children.Add(shape1);
                                var p = result.InkDrawingNode.HotPoints;
                                var a = GetDistance(p[0], p[2]) / 2; //长半轴
                                var b = GetDistance(p[1], p[3]) / 2; //短半轴
                                if (a < b)
                                {
                                    var t = a;
                                    a = b;
                                    b = t;
                                }

                                result.Centroid = new Point((p[0].X + p[2].X) / 2, (p[0].Y + p[2].Y) / 2);
                                var needRotation = true;

                                if (shape.Width > 75 || (shape.Height > 75 && p.Count == 4))
                                {
                                    var iniP = new Point(result.Centroid.X - shape.Width / 2,
                                        result.Centroid.Y - shape.Height / 2);
                                    var endP = new Point(result.Centroid.X + shape.Width / 2,
                                        result.Centroid.Y + shape.Height / 2);

                                    foreach (var circle in circles)
                                        //判断是否画同心椭圆
                                        if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2 &&
                                            Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                                        {
                                            result.Centroid = circle.Centroid;
                                            iniP = new Point(result.Centroid.X - shape.Width / 2,
                                                result.Centroid.Y - shape.Height / 2);
                                            endP = new Point(result.Centroid.X + shape.Width / 2,
                                                result.Centroid.Y + shape.Height / 2);

                                            //再判断是否与圆相切
                                            if (Math.Abs(a - circle.R) / a < 0.2)
                                            {
                                                if (shape.Width >= shape.Height)
                                                {
                                                    iniP.X = result.Centroid.X - circle.R;
                                                    endP.X = result.Centroid.X + circle.R;
                                                    iniP.Y = result.Centroid.Y - b;
                                                    endP.Y = result.Centroid.Y + b;
                                                }
                                                else
                                                {
                                                    iniP.Y = result.Centroid.Y - circle.R;
                                                    endP.Y = result.Centroid.Y + circle.R;
                                                    iniP.X = result.Centroid.X - a;
                                                    endP.X = result.Centroid.X + a;
                                                }
                                            }

                                            break;
                                        }
                                        else if (Math.Abs(result.Centroid.X - circle.Centroid.X) / a < 0.2)
                                        {
                                            var sinTheta = Math.Abs(circle.Centroid.Y - result.Centroid.Y) /
                                                           circle.R;
                                            var cosTheta = Math.Sqrt(1 - sinTheta * sinTheta);
                                            var newA = circle.R * cosTheta;
                                            if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 &&
                                                Math.Abs(newA - a) / newA < 0.3)
                                            {
                                                iniP.X = circle.Centroid.X - newA;
                                                endP.X = circle.Centroid.X + newA;
                                                iniP.Y = result.Centroid.Y - newA / 5;
                                                endP.Y = result.Centroid.Y + newA / 5;

                                                var topB = endP.Y - iniP.Y;

                                                SetNewBackupOfStroke();
                                                _currentCommitType = CommitReason.ShapeRecognition;
                                                inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                                newStrokes = new StrokeCollection();

                                                var _pointList = GenerateEllipseGeometry(iniP, endP, false);
                                                var _point = new StylusPointCollection(_pointList);
                                                var _stroke = new Stroke(_point)
                                                {
                                                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                                };
                                                var _dashedLineStroke =
                                                    GenerateDashedLineEllipseStrokeCollection(iniP, endP, true, false);
                                                var strokes = new StrokeCollection {
                                                    _stroke,
                                                    _dashedLineStroke
                                                };
                                                inkCanvas.Strokes.Add(strokes);
                                                _currentCommitType = CommitReason.UserInput;
                                                return;
                                            }
                                        }
                                        else if (Math.Abs(result.Centroid.Y - circle.Centroid.Y) / a < 0.2)
                                        {
                                            var cosTheta = Math.Abs(circle.Centroid.X - result.Centroid.X) /
                                                           circle.R;
                                            var sinTheta = Math.Sqrt(1 - cosTheta * cosTheta);
                                            var newA = circle.R * sinTheta;
                                            if (circle.R * sinTheta / circle.R < 0.9 && a / b > 2 &&
                                                Math.Abs(newA - a) / newA < 0.3)
                                            {
                                                iniP.X = result.Centroid.X - newA / 5;
                                                endP.X = result.Centroid.X + newA / 5;
                                                iniP.Y = circle.Centroid.Y - newA;
                                                endP.Y = circle.Centroid.Y + newA;
                                                needRotation = false;
                                            }
                                        }

                                    //纠正垂直与水平关系
                                    var newPoints = FixPointsDirection(p[0], p[2]);
                                    p[0] = newPoints[0];
                                    p[2] = newPoints[1];
                                    newPoints = FixPointsDirection(p[1], p[3]);
                                    p[1] = newPoints[0];
                                    p[3] = newPoints[1];

                                    var pointList = GenerateEllipseGeometry(iniP, endP);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(point)
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };

                                    if (needRotation)
                                    {
                                        var m = new Matrix();
                                        var fe = e.Source as FrameworkElement;
                                        var tanTheta = (p[2].Y - p[0].Y) / (p[2].X - p[0].X);
                                        var theta = Math.Atan(tanTheta);
                                        m.RotateAt(theta * 180.0 / Math.PI, result.Centroid.X, result.Centroid.Y);
                                        stroke.Transform(m, false);
                                    }

                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if (result.InkDrawingNode.GetShapeName().Contains("Triangle") &&
                                     Settings.InkToShape.IsInkToShapeTriangle)
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                var p = result.InkDrawingNode.HotPoints;
                                if ((Math.Max(Math.Max(p[0].X, p[1].X), p[2].X) -
                                     Math.Min(Math.Min(p[0].X, p[1].X), p[2].X) >= 100 ||
                                     Math.Max(Math.Max(p[0].Y, p[1].Y), p[2].Y) -
                                     Math.Min(Math.Min(p[0].Y, p[1].Y), p[2].Y) >= 100) &&
                                    result.InkDrawingNode.HotPoints.Count == 3)
                                {
                                    //纠正垂直与水平关系
                                    var newPoints = FixPointsDirection(p[0], p[1]);
                                    p[0] = newPoints[0];
                                    p[1] = newPoints[1];
                                    newPoints = FixPointsDirection(p[0], p[2]);
                                    p[0] = newPoints[0];
                                    p[2] = newPoints[1];
                                    newPoints = FixPointsDirection(p[1], p[2]);
                                    p[1] = newPoints[0];
                                    p[2] = newPoints[1];

                                    var pointList = p.ToList();
                                    //pointList.Add(p[0]);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(GenerateFakePressureTriangle(point))
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                            else if ((result.InkDrawingNode.GetShapeName().Contains("Rectangle") ||
                                      result.InkDrawingNode.GetShapeName().Contains("Diamond") ||
                                      result.InkDrawingNode.GetShapeName().Contains("Parallelogram") ||
                                      result.InkDrawingNode.GetShapeName().Contains("Square") ||
                                      result.InkDrawingNode.GetShapeName().Contains("Trapezoid")) &&
                                     Settings.InkToShape.IsInkToShapeRectangle)
                            {
                                var shape = result.InkDrawingNode.GetShape();
                                var p = result.InkDrawingNode.HotPoints;
                                if ((Math.Max(Math.Max(Math.Max(p[0].X, p[1].X), p[2].X), p[3].X) -
                                     Math.Min(Math.Min(Math.Min(p[0].X, p[1].X), p[2].X), p[3].X) >= 100 ||
                                     Math.Max(Math.Max(Math.Max(p[0].Y, p[1].Y), p[2].Y), p[3].Y) -
                                     Math.Min(Math.Min(Math.Min(p[0].Y, p[1].Y), p[2].Y), p[3].Y) >= 100) &&
                                    result.InkDrawingNode.HotPoints.Count == 4)
                                {
                                    //纠正垂直与水平关系
                                    var newPoints = FixPointsDirection(p[0], p[1]);
                                    p[0] = newPoints[0];
                                    p[1] = newPoints[1];
                                    newPoints = FixPointsDirection(p[1], p[2]);
                                    p[1] = newPoints[0];
                                    p[2] = newPoints[1];
                                    newPoints = FixPointsDirection(p[2], p[3]);
                                    p[2] = newPoints[0];
                                    p[3] = newPoints[1];
                                    newPoints = FixPointsDirection(p[3], p[0]);
                                    p[3] = newPoints[0];
                                    p[0] = newPoints[1];

                                    var pointList = p.ToList();
                                    pointList.Add(p[0]);
                                    var point = new StylusPointCollection(pointList);
                                    var stroke = new Stroke(GenerateFakePressureRectangle(point))
                                    {
                                        DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                                    };
                                    SetNewBackupOfStroke();
                                    _currentCommitType = CommitReason.ShapeRecognition;
                                    inkCanvas.Strokes.Remove(result.InkDrawingNode.Strokes);
                                    inkCanvas.Strokes.Add(stroke);
                                    _currentCommitType = CommitReason.UserInput;
                                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                                    newStrokes = new StrokeCollection();
                                }
                            }
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
                    }

                    InkToShapeProcess();
                }

                foreach (var stylusPoint in e.Stroke.StylusPoints)
                    //LogHelper.WriteLogToFile(stylusPoint.PressureFactor.ToString(), LogHelper.LogType.Info);
                    // 检查是否是压感笔书写
                    //if (stylusPoint.PressureFactor != 0.5 && stylusPoint.PressureFactor != 0)
                    if ((stylusPoint.PressureFactor > 0.501 || stylusPoint.PressureFactor < 0.5) &&
                        stylusPoint.PressureFactor != 0)
                        return;

                try
                {
                    if (e.Stroke.StylusPoints.Count > 3)
                    {
                        var random = new Random();
                        var _speed = GetPointSpeed(
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint(),
                            e.Stroke.StylusPoints[random.Next(0, e.Stroke.StylusPoints.Count - 1)].ToPoint());

                        RandWindow.randSeed = (int)(_speed * 100000 * 1000);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                switch (Settings.Canvas.InkStyle)
                {
                    case 1:
                        if (penType == 0)
                            try
                            {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var s = "";

                                for (var i = 0; i <= n; i++)
                                {
                                    var speed = GetPointSpeed(e.Stroke.StylusPoints[Math.Max(i - 1, 0)].ToPoint(),
                                        e.Stroke.StylusPoints[i].ToPoint(),
                                        e.Stroke.StylusPoints[Math.Min(i + 1, n)].ToPoint());
                                    s += speed + "\t";
                                    var point = new StylusPoint();
                                    if (speed >= 0.25)
                                        point.PressureFactor = (float)(0.5 - 0.3 * (Math.Min(speed, 1.5) - 0.3) / 1.2);
                                    else if (speed >= 0.05)
                                        point.PressureFactor = (float)0.5;
                                    else
                                        point.PressureFactor = (float)(0.5 + 0.4 * (0.05 - speed) / 0.05);

                                    point.X = e.Stroke.StylusPoints[i].X;
                                    point.Y = e.Stroke.StylusPoints[i].Y;
                                    stylusPoints.Add(point);
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                        break;
                    case 0:
                        if (penType == 0)
                            try
                            {
                                var stylusPoints = new StylusPointCollection();
                                var n = e.Stroke.StylusPoints.Count - 1;
                                var pressure = 0.1;
                                var x = 10;
                                if (n == 1) return;
                                if (n >= x)
                                {
                                    for (var i = 0; i < n - x; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)0.5;
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }

                                    for (var i = n - x; i <= n; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)((0.5 - pressure) * (n - i) / x + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }
                                else
                                {
                                    for (var i = 0; i <= n; i++)
                                    {
                                        var point = new StylusPoint();

                                        point.PressureFactor = (float)(0.4 * (n - i) / n + pressure);
                                        point.X = e.Stroke.StylusPoints[i].X;
                                        point.Y = e.Stroke.StylusPoints[i].Y;
                                        stylusPoints.Add(point);
                                    }
                                }

                                e.Stroke.StylusPoints = stylusPoints;
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

                        break;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

            // 应用高级贝塞尔曲线平滑（仅在未进行直线拉直时）
            Debug.WriteLine($"墨迹平滑检查: UseAdvancedBezierSmoothing={Settings.Canvas.UseAdvancedBezierSmoothing}, wasStraightened={wasStraightened}");
            Debug.WriteLine($"异步平滑设置: UseAsyncInkSmoothing={Settings.Canvas.UseAsyncInkSmoothing}, _inkSmoothingManager={_inkSmoothingManager != null}");

            if (Settings.Canvas.UseAdvancedBezierSmoothing && !wasStraightened)
            {
                try
                {
                    Debug.WriteLine($"开始墨迹平滑处理: 原始点数={e.Stroke.StylusPoints.Count}, 直线拉直={wasStraightened}");

                    // 检查原始笔画是否仍然存在于画布中
                    if (inkCanvas.Strokes.Contains(e.Stroke))
                    {
                        // 使用新的异步墨迹平滑管理器
                        if (Settings.Canvas.UseAsyncInkSmoothing && _inkSmoothingManager != null)
                        {
                            Debug.WriteLine("使用异步墨迹平滑");
                            // 异步处理
                            _ = ProcessStrokeAsync(e.Stroke);
                        }
                        else
                        {
                            // 同步处理（向后兼容）
                            var smoothedStroke = _inkSmoothingManager?.SmoothStroke(e.Stroke) ?? e.Stroke;

                            if (smoothedStroke != e.Stroke)
                            {
                                // 替换原始笔画
                                SetNewBackupOfStroke();
                                _currentCommitType = CommitReason.ShapeRecognition;
                                inkCanvas.Strokes.Remove(e.Stroke);
                                inkCanvas.Strokes.Add(smoothedStroke);
                                _currentCommitType = CommitReason.UserInput;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("原始笔画不在画布中，跳过平滑处理");
                    }
                }
                catch (Exception ex)
                {
                    // 如果高级平滑失败，回退到原始笔画
                    Debug.WriteLine($"高级贝塞尔曲线平滑失败: {ex.Message}");
                }
            }
            else if (Settings.Canvas.FitToCurve && !wasStraightened)
            {
                drawingAttributes.FitToCurve = true;
            }
        }

        /// <summary>
        /// 异步处理笔画平滑
        /// </summary>
        /// <param name="originalStroke">原始笔画</param>
        /// <returns>返回一个表示异步操作的Task</returns>
        /// <remarks>
        /// 异步处理笔画平滑的流程：
        /// 1. 调用墨迹平滑管理器的SmoothStrokeAsync方法
        /// 2. 在平滑完成后，在UI线程上执行笔画替换
        /// 3. 如果原始笔画仍然存在于画布中且平滑后的笔画不同，则替换原始笔画
        /// 4. 捕获并记录可能的异常
        /// <summary>
        /// 异步对指定的笔画进行平滑处理，并在平滑结果与原笔画不同且原笔画仍在画布上时，用平滑后的笔画替换原笔画。
        /// </summary>
        /// <param name="originalStroke">要进行平滑并可能替换的原始笔画。</param>
        private async Task ProcessStrokeAsync(Stroke originalStroke)
        {
            try
            {
                Debug.WriteLine($"异步平滑开始: 原始点数={originalStroke.StylusPoints.Count}");
                await _inkSmoothingManager.SmoothStrokeAsync(originalStroke, (original, smoothed) =>
                {
                    Debug.WriteLine($"异步平滑完成: 原始点数={original.StylusPoints.Count}, 平滑后点数={smoothed.StylusPoints.Count}");
                    Debug.WriteLine($"墨迹比较: smoothed != original = {smoothed != original}");
                    Debug.WriteLine($"画布包含原始墨迹: {inkCanvas.Strokes.Contains(original)}");

                    // 在UI线程上执行笔画替换
                    if (inkCanvas.Strokes.Contains(original) && smoothed != original)
                    {
                        Debug.WriteLine("异步替换原始笔画为平滑后的笔画");
                        SetNewBackupOfStroke();
                        _currentCommitType = CommitReason.ShapeRecognition;
                        inkCanvas.Strokes.Remove(original);
                        inkCanvas.Strokes.Add(smoothed);
                        _currentCommitType = CommitReason.UserInput;
                    }
                    else
                    {
                        Debug.WriteLine($"异步平滑后的笔画与原始笔画相同，未进行替换 (contains={inkCanvas.Strokes.Contains(original)}, different={smoothed != original})");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"异步墨迹平滑失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查一笔墨迹是否可能是直线
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>如果可能是直线则返回true，否则返回false</returns>
        /// <remarks>
        /// 检查一笔墨迹是否可能是直线的流程：
        /// 1. 确保有足够的点来进行线条分析（至少5个点）
        /// 2. 计算线条长度，确保线条足够长（使用分辨率自适应阈值）
        /// 3. 检查墨迹复杂度，避免将复杂图形拉直
        /// 4. 检查是否为明显的曲线
        /// 5. 根据用户设置的灵敏度值计算阈值
        /// 6. 快速检查：计算几个关键点与直线的距离
        /// 7. 根据偏差阈值判断是否可能是直线
        /// <summary>
        /// 判断给定笔画是否可以作为“可拉直”的直线候选。
        /// </summary>
        /// <param name="stroke">要检测的笔画（Ink Stroke）。</param>
        /// <returns>`true` 如果笔画满足被视为直线的条件（点数与长度足够、非复杂或明显曲线、经采样偏差在阈值内），`false` 否则。</returns>
        private bool IsPotentialStraightLine(Stroke stroke)
        {
            // 确保有足够的点来进行线条分析
            if (stroke.StylusPoints.Count < 5)
                return false;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();
            double lineLength = GetDistance(start, end);
            // 分辨率自适应阈值
            double adaptiveThreshold = Settings.Canvas.AutoStraightenLineThreshold * GetResolutionScale();
            // 线条必须足够长才考虑拉直，使用自适应阈值
            if (lineLength < adaptiveThreshold)
                return false;

            // 新增：检查墨迹复杂度，避免将复杂图形拉直
            if (IsComplexShape(stroke))
                return false;

            // 新增：检查是否为明显的曲线
            if (IsObviousCurve(stroke))
                return false;

            // 获取用户设置的灵敏度值，确保使用正确的设置
            double sensitivity = Settings.InkToShape.LineStraightenSensitivity;

            // 输出当前灵敏度值
            Debug.WriteLine($"IsPotentialStraightLine - sensitivity: {sensitivity}, length: {lineLength}");

            // 将灵敏度转换为阈值：灵敏度0.05-2.0映射到阈值0.01-0.4
            double quickThreshold = Math.Max(0.01, sensitivity * 0.2); // 确保最小阈值为0.01

            Debug.WriteLine($"使用快速检查阈值: {quickThreshold}");

            // 快速检查：计算几个关键点与直线的距离
            if (stroke.StylusPoints.Count >= 10)
            {
                List<Point> checkPoints;

                // 使用采样点进行更准确的判断
                if (Settings.Canvas.HighPrecisionLineStraighten)
                {
                    var allPoints = stroke.StylusPoints.Select(p => p.ToPoint()).ToList();
                    checkPoints = SamplePointsByDistance(allPoints, 10.0);
                    Debug.WriteLine($"高精度模式快速检查：原始点数={allPoints.Count}, 采样点数={checkPoints.Count}");
                }
                else
                {
                    // 取中点和1/4、3/4位置的点
                    int quarterIdx = stroke.StylusPoints.Count / 4;
                    int midIdx = stroke.StylusPoints.Count / 2;
                    int threeQuarterIdx = quarterIdx * 3;

                    checkPoints = new List<Point>
                    {
                        stroke.StylusPoints[quarterIdx].ToPoint(),
                        stroke.StylusPoints[midIdx].ToPoint(),
                        stroke.StylusPoints[threeQuarterIdx].ToPoint()
                    };
                }

                // 计算所有检查点与直线的平均偏差
                double totalDeviation = 0;
                double maxDeviation = 0;
                int validPointCount = 0;

                foreach (Point checkPoint in checkPoints)
                {
                    double deviation = DistanceFromLineToPoint(start, end, checkPoint);
                    totalDeviation += deviation;
                    maxDeviation = Math.Max(maxDeviation, deviation);
                    validPointCount++;
                }

                if (validPointCount > 0)
                {
                    double avgDeviation = totalDeviation / validPointCount;
                    // 使用相对偏差：偏差与线长的比例，并使用灵敏度进行调整
                    double quickRelativeThreshold = lineLength * quickThreshold;

                    // 使用平均偏差和最大偏差的综合判断
                    double deviationThreshold = Settings.Canvas.HighPrecisionLineStraighten
                        ? Math.Max(avgDeviation, maxDeviation * 0.7) // 高精度模式更严格
                        : maxDeviation;

                    // 记录检测到的偏差
                    Debug.WriteLine($"Deviations: avg={avgDeviation:F2}, max={maxDeviation:F2}, threshold={quickRelativeThreshold:F2}, highPrecision={Settings.Canvas.HighPrecisionLineStraighten}");

                    if (deviationThreshold > quickRelativeThreshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 检查墨迹是否为复杂形状
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>如果是复杂形状则返回true，否则返回false</returns>
        /// <remarks>
        /// 检查墨迹是否为复杂形状的流程：
        /// 1. 确保有足够的点来进行分析（至少10个点）
        /// 2. 计算直线距离和实际路径长度
        /// 3. 如果实际路径长度远大于直线距离（2.5倍以上），说明是复杂形状
        /// 4. 检查方向变化次数，如果超过动态阈值，说明是复杂形状
        /// <summary>
        /// 判断给定笔画是否属于复杂形状。
        /// </summary>
        /// <param name="stroke">要检测的笔画；当点数少于 10 时视为非复杂形状。</param>
        /// <returns>`true` 如果笔画被判定为复杂形状（实际路径长度超过端点直线距离的 2.5 倍，或方向变化次数超过动态阈值），`false` 否则。</returns>
        private bool IsComplexShape(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 10) return false;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();
            double lineLength = GetDistance(start, end);

            // 计算墨迹的实际路径长度
            double actualLength = 0;
            for (int i = 1; i < stroke.StylusPoints.Count; i++)
            {
                Point p1 = stroke.StylusPoints[i - 1].ToPoint();
                Point p2 = stroke.StylusPoints[i].ToPoint();
                actualLength += GetDistance(p1, p2);
            }

            // 如果实际路径长度远大于直线距离，说明是复杂形状
            double complexityRatio = actualLength / Math.Max(lineLength, 1);
            if (complexityRatio > 2.5) // 实际路径是直线距离的2.5倍以上
            {
                Debug.WriteLine($"检测到复杂形状：复杂度比率 = {complexityRatio:F2}");
                return true;
            }

            // 检查方向变化次数
            int directionChanges = CountDirectionChanges(stroke);
            int maxAllowedChanges = Math.Max(3, stroke.StylusPoints.Count / 20); // 动态阈值
            if (directionChanges > maxAllowedChanges)
            {
                Debug.WriteLine($"检测到复杂形状：方向变化次数 = {directionChanges}，阈值 = {maxAllowedChanges}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否为明显的曲线（如圆弧、抛物线等）
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>如果是明显的曲线则返回true，否则返回false</returns>
        /// <remarks>
        /// 检查墨迹是否为明显的曲线的流程：
        /// 1. 确保有足够的点来进行分析（至少10个点）
        /// 2. 计算线条长度
        /// 3. 检查曲率一致性，如果一致则认为是明显的曲线
        /// 4. 检查中点偏移（对圆弧特别有效）：
        ///    - 计算中点到直线的距离
        ///    - 如果中点偏移超过线长的15%，且偏移方向一致，可能是圆弧
        /// <summary>
        /// 判断给定笔画是否明显呈现曲线（例如圆弧或具有一致曲率的弧线）。
        /// </summary>
        /// <remarks>
        /// 该判定依赖笔画的几何特征；需要至少 10 个采样点才能进行判断。
        /// </remarks>
        /// <param name="stroke">要检测的笔画。</param>
        /// <returns>`true` 如果笔画表现为明显曲线（例如具备一致曲率或中点相对于两端线段存在显著且方向一致的偏移），`false` 否则。</returns>
        private bool IsObviousCurve(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 10) return false;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();
            double lineLength = GetDistance(start, end);

            // 检查曲率一致性
            if (HasConsistentCurvature(stroke))
            {
                Debug.WriteLine("检测到明显曲线：曲率一致");
                return true;
            }

            // 检查中点偏移（对圆弧特别有效）
            int midIndex = stroke.StylusPoints.Count / 2;
            Point midPoint = stroke.StylusPoints[midIndex].ToPoint();
            double midDeviation = DistanceFromLineToPoint(start, end, midPoint);

            // 如果中点偏移超过线长的15%，且偏移方向一致，可能是圆弧
            if (midDeviation > lineLength * 0.15)
            {
                // 检查偏移方向的一致性
                if (IsConsistentArcDirection(stroke))
                {
                    Debug.WriteLine($"检测到明显曲线：中点偏移 = {midDeviation:F2}，线长 = {lineLength:F2}");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 计算方向变化次数
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>返回方向变化的次数</returns>
        /// <remarks>
        /// 计算方向变化次数的流程：
        /// 1. 确保有足够的点来进行分析（至少3个点）
        /// 2. 遍历笔画中的每个点（除了第一个和最后一个）
        /// 3. 计算每个点前后线段的角度变化
        /// 4. 处理角度跨越问题（超过180度的情况）
        /// 5. 如果角度变化超过30度，且与上一次角度变化的差异超过15度，认为是方向变化
        /// 6. 返回方向变化的总次数
        /// <summary>
        /// 计算笔画中显著方向变化的次数。
        /// </summary>
        /// <param name="stroke">要分析的笔画，其 StylusPoints 列表用于计算相邻线段的夹角变化。</param>
        /// <returns>笔画中显著方向变化的数量；若点数少于 3 则返回 0。显著变化以相邻线段夹角超过 30° 且与上一次记录的变化相差超过 15° 为准。</returns>
        private int CountDirectionChanges(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 3) return 0;

            int changes = 0;
            double lastAngle = 0;
            bool hasLastAngle = false;

            for (int i = 1; i < stroke.StylusPoints.Count - 1; i++)
            {
                Point p1 = stroke.StylusPoints[i - 1].ToPoint();
                Point p2 = stroke.StylusPoints[i].ToPoint();
                Point p3 = stroke.StylusPoints[i + 1].ToPoint();

                // 计算角度变化
                double angle1 = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                double angle2 = Math.Atan2(p3.Y - p2.Y, p3.X - p2.X);
                double angleDiff = Math.Abs(angle2 - angle1);

                // 处理角度跨越问题
                if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

                // 如果角度变化超过30度，认为是方向变化
                if (angleDiff > Math.PI / 6) // 30度
                {
                    if (hasLastAngle && Math.Abs(angleDiff - lastAngle) > Math.PI / 12) // 15度
                    {
                        changes++;
                    }
                    lastAngle = angleDiff;
                    hasLastAngle = true;
                }
            }

            return changes;
        }

        /// <summary>
        /// 检查曲率是否一致（用于识别圆弧等规则曲线）
        /// </summary>
        /// <param name="stroke">要检查的笔画</param>
        /// <returns>如果曲率一致则返回true，否则返回false</returns>
        /// <remarks>
        /// 检查曲率是否一致的流程：
        /// 1. 确保有足够的点来进行分析（至少15个点）
        /// 2. 计算每个点的曲率（使用前后各两个点）
        /// 3. 过滤掉无效的曲率值（NaN或无穷大），并取绝对值
        /// 4. 确保有足够的有效曲率值（至少5个）
        /// 5. 计算曲率的平均值和标准差
        /// 6. 如果平均曲率大于0.001且标准差与平均值的比例小于0.5，认为曲率一致
        /// <summary>
        /// 判断给定笔画是否具有一致的曲率（即近似规则弧线或圆弧）。
        /// </summary>
        /// <param name="stroke">要检测的一次笔画。</param>
        /// <returns>`true` 如果笔画包含足够的样本点且曲率平均值大于 0.001 且曲率的相对标准差小于 0.5；否则返回 `false`。</returns>
        private bool HasConsistentCurvature(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 15) return false;

            List<double> curvatures = new List<double>();

            // 计算每个点的曲率
            for (int i = 2; i < stroke.StylusPoints.Count - 2; i++)
            {
                Point p1 = stroke.StylusPoints[i - 2].ToPoint();
                Point p2 = stroke.StylusPoints[i].ToPoint();
                Point p3 = stroke.StylusPoints[i + 2].ToPoint();

                double curvature = CalculateCurvature(p1, p2, p3);
                if (!double.IsNaN(curvature) && !double.IsInfinity(curvature))
                {
                    curvatures.Add(Math.Abs(curvature));
                }
            }

            if (curvatures.Count < 5) return false;

            // 计算曲率的标准差
            double avgCurvature = curvatures.Average();
            double variance = curvatures.Select(c => Math.Pow(c - avgCurvature, 2)).Average();
            double stdDev = Math.Sqrt(variance);

            // 如果曲率变化很小且平均曲率不为零，可能是规则曲线
            return avgCurvature > 0.001 && stdDev / avgCurvature < 0.5;
        }

        /// <summary>
        /// 检查圆弧方向是否一致
        /// </summary>
        private bool IsConsistentArcDirection(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 10) return false;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();

            int positiveDeviations = 0;
            int negativeDeviations = 0;

            // 检查多个点相对于直线的偏移方向
            for (int i = 1; i < stroke.StylusPoints.Count - 1; i += Math.Max(1, stroke.StylusPoints.Count / 10))
            {
                Point p = stroke.StylusPoints[i].ToPoint();
                double signedDistance = SignedDistanceFromLineToPoint(start, end, p);

                if (Math.Abs(signedDistance) > 5) // 忽略很小的偏移
                {
                    if (signedDistance > 0) positiveDeviations++;
                    else negativeDeviations++;
                }
            }

            // 如果大部分点都在直线的同一侧，说明是一致的弧形
            int totalSignificantDeviations = positiveDeviations + negativeDeviations;
            if (totalSignificantDeviations < 3) return false;

            double consistency = Math.Max(positiveDeviations, negativeDeviations) / (double)totalSignificantDeviations;
            return consistency > 0.8; // 80%的点在同一侧
        }

        /// <summary>
        /// 计算三点的曲率
        /// <summary>
        /// 计算由三个点确定的圆弧的曲率（即圆的倒数半径）。
        /// </summary>
        /// <param name="p1">圆弧上的第一个点（顺序上的一个端点或采样点）。</param>
        /// <param name="p2">圆弧上的第二个点（中间采样点）。</param>
        /// <param name="p3">圆弧上的第三个点（顺序上的另一个端点或采样点）。</param>
        /// <returns>计算得到的曲率（1 / 半径）。如果任意两点重合或无法计算曲率，则返回 0。</returns>
        private double CalculateCurvature(Point p1, Point p2, Point p3)
        {
            // 使用三点计算曲率的公式
            double a = GetDistance(p1, p2);
            double b = GetDistance(p2, p3);
            double c = GetDistance(p1, p3);

            if (a == 0 || b == 0 || c == 0) return 0;

            // 使用海伦公式计算面积
            double s = (a + b + c) / 2;
            double areaSquared = s * (s - a) * (s - b) * (s - c);
            double area = areaSquared > 0 ? Math.Sqrt(areaSquared) : 0;

            // 曲率 = 4 * 面积 / (a * b * c)
            return 4 * area / (a * b * c);
        }

        /// <summary>
        /// 计算点到直线的有符号距离
        /// <summary>
        /// 计算点到由 lineStart 和 lineEnd 定义的直线的有符号垂直距离。
        /// </summary>
        /// <param name="lineStart">直线的起点。</param>
        /// <param name="lineEnd">直线的终点。</param>
        /// <param name="point">要测量距离的点。</param>
        /// <returns>点到直线的有符号垂直距离（像素）。正值表示点位于从 lineStart 指向 lineEnd 的方向的左侧，负值表示位于右侧；当 lineStart 与 lineEnd 重合时返回 0。</returns>
        private double SignedDistanceFromLineToPoint(Point lineStart, Point lineEnd, Point point)
        {
            // 使用叉积计算有符号距离
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;
            double lineLength = Math.Sqrt(dx * dx + dy * dy);

            if (lineLength == 0) return 0;

            return ((lineEnd.Y - lineStart.Y) * point.X - (lineEnd.X - lineStart.X) * point.Y +
                    lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X) / lineLength;
        }

        /// <summary>
        /// 尝试获取直线的端点
        /// </summary>
        /// <param name="stroke">要分析的笔画</param>
        /// <param name="endpoint1">输出参数：直线的第一个端点</param>
        /// <param name="endpoint2">输出参数：直线的第二个端点</param>
        /// <returns>如果成功获取直线端点则返回true，否则返回false</returns>
        /// <remarks>
        /// 尝试获取直线端点的流程：
        /// 1. 确保笔画有足够的点（至少10个点）
        /// 2. 如果启用高精度直线拉直，则对点数进行采样
        /// 3. 使用总最小二乘法(TLS/PCA)进行直线拟合
        /// 4. 计算中心点和协方差矩阵
        /// 5. 计算特征值和特征向量，确定直线方向
        /// 6. 计算解释方差比例（拟合优度）
        /// 7. 计算所有点在直线方向上的投影，找到最小和最大投影值
        /// 8. 根据投影值计算端点坐标
        /// 9. 根据解释方差比例判断是否为直线
        /// <summary>
        /// 尝试从给定笔画中通过主方向拟合确定一条直线的端点。
        /// </summary>
        /// <param name="stroke">用于拟合的输入笔画；方法假定笔画包含足够的点（通常至少 10 个），否则会返回失败。</param>
        /// <param name="endpoint1">输出参数；拟合直线的一端点（在笔画坐标系中）。</param>
        /// <param name="endpoint2">输出参数；拟合直线的另一端点（在笔画坐标系中）。</param>
        /// <returns>`true` 如果成功计算出端点且拟合质量（解释方差比）超过阈值；`false` 表示点数不足或拟合质量未达标。</returns>
        private bool TryGetStraightLineEndpoints(Stroke stroke, out Point endpoint1, out Point endpoint2)
        {
            endpoint1 = new Point();
            endpoint2 = new Point();

            var points = stroke.StylusPoints.Select(p => p.ToPoint()).ToList();
            if (points.Count < 10)
            {
                return false;
            }

            List<Point> workingPoints = points;
            if (Settings.Canvas.HighPrecisionLineStraighten)
            {
                workingPoints = SamplePointsByDistance(points, 10.0);
                Debug.WriteLine($"高精度模式：原始点数={points.Count}, 采样后点数={workingPoints.Count}");
            }

            // 使用总最小二乘法(TLS/PCA)进行直线拟合
            int n = workingPoints.Count - 8;
            if (n < 1)
            {
                // 如果采样后点数太少，回退到原始方法
                n = points.Count - 8;
                workingPoints = points;
            }

            List<Point> filteredPoints = new List<Point>();

            // 收集过滤后的点（跳过前 4 个和后 4 个点，用于计算直线方向）
            int skipCount = Math.Min(4, n / 2); // 确保跳过数量不超过一半
            for (int i = skipCount; i < n + skipCount && i < workingPoints.Count; i++)
            {
                filteredPoints.Add(workingPoints[i]);
            }

            // 计算中心点（使用过滤后的点）
            double centerX = 0, centerY = 0;
            foreach (Point p in filteredPoints)
            {
                centerX += p.X;
                centerY += p.Y;
            }
            centerX /= filteredPoints.Count;
            centerY /= filteredPoints.Count;

            // 计算协方差矩阵（使用过滤后的点）
            double covXX = 0, covYY = 0, covXY = 0;
            foreach (Point p in filteredPoints)
            {
                double dx = p.X - centerX;
                double dy = p.Y - centerY;
                covXX += dx * dx;
                covYY += dy * dy;
                covXY += dx * dy;
            }

            // 计算特征值和特征向量
            double trace = covXX + covYY;
            double determinant = covXX * covYY - covXY * covXY;
            double discriminantSquared = trace * trace - 4 * determinant;
            double discriminant = discriminantSquared > 0 ? Math.Sqrt(discriminantSquared) : 0;

            double eigenvalue1 = (trace + discriminant) / 2;
            double eigenvalue2 = (trace - discriminant) / 2;

            // 最大特征值对应的特征向量即为直线方向
            double directionX, directionY;
            if (Math.Abs(covXY) > 1e-10)
            {
                directionX = covXY;
                directionY = eigenvalue1 - covXX;
                // 归一化
                double length = Math.Sqrt(directionX * directionX + directionY * directionY);
                if (length > 1e-10)
                {
                    directionX /= length;
                    directionY /= length;
                }
                else
                {
                    // 如果归一化失败，使用起点和终点计算方向
                    Point start = points.First();
                    Point end = points.Last();
                    double dx = end.X - start.X;
                    double dy = end.Y - start.Y;
                    double lineLength = Math.Sqrt(dx * dx + dy * dy);
                    if (lineLength > 1e-10)
                    {
                        directionX = dx / lineLength;
                        directionY = dy / lineLength;
                    }
                    else
                    {
                        directionX = (covXX >= covYY) ? 1 : 0;
                        directionY = (covXX >= covYY) ? 0 : 1;
                    }
                }
            }
            else
            {
                Point start = points.First();
                Point end = points.Last();
                double dx = end.X - start.X;
                double dy = end.Y - start.Y;
                double lineLength = Math.Sqrt(dx * dx + dy * dy);
                
                if (lineLength > 1e-10)
                {
                    directionX = dx / lineLength;
                    directionY = dy / lineLength;
                }
                else
                {
                    if (Math.Abs(eigenvalue1 - covXX) < Math.Abs(eigenvalue1 - covYY))
                    {
                        // 主要方向是 X 轴方向
                        directionX = 1;
                        directionY = 0;
                    }
                    else
                    {
                        // 主要方向是 Y 轴方向
                        directionX = 0;
                        directionY = 1;
                    }
                }
            }

            // 计算解释方差比例（拟合优度）
            double totalVariance = eigenvalue1 + eigenvalue2;
            double explainedVarianceRatio = (totalVariance > 1e-10) ?
                Math.Max(eigenvalue1, eigenvalue2) / totalVariance : 1d;

            // 使用所有点计算端点
            double minProjection = double.MaxValue;
            double maxProjection = double.MinValue;

            // 计算所有点在直线方向上的投影
            List<Point> pointsForProjection = Settings.Canvas.HighPrecisionLineStraighten ? workingPoints : points;
            foreach (Point p in pointsForProjection)
            {
                // 相对于过滤点中心的投影
                double projection = (p.X - centerX) * directionX + (p.Y - centerY) * directionY;
                minProjection = Math.Min(minProjection, projection);
                maxProjection = Math.Max(maxProjection, projection);
            }

            // 计算端点坐标
            endpoint1 = new Point(
                centerX + minProjection * directionX,
                centerY + minProjection * directionY
            );

            endpoint2 = new Point(
                centerX + maxProjection * directionX,
                centerY + maxProjection * directionY
            );

            // 使用解释方差比例作为判断条件
            double threshold = 0.998 + Settings.InkToShape.LineNormalizationThreshold / 500;
            return explainedVarianceRatio > threshold;
        }

        /// <summary>
        /// 确定笔画是否应该被拉成直线
        /// </summary>
        /// <param name="stroke">要分析的笔画</param>
        /// <returns>如果笔画应该被拉成直线则返回true，否则返回false</returns>
        /// <remarks>
        /// 确定笔画是否应该被拉成直线的流程：
        /// 1. 计算线条长度和分辨率自适应阈值
        /// 2. 如果线条太短，不进行拉直处理
        /// 3. 检查线条复杂度，如果是复杂形状，不进行拉直处理
        /// 4. 尝试获取直线端点，判断是否满足直线条件
        /// 5. 根据判断结果返回相应的布尔值
        /// <summary>
        /// 判断给定笔划是否应被自动拉直为直线。
        /// </summary>
        /// <remarks>
        /// 判断基于三项可观测条件：笔划与分辨率相关的最小长度阈值、笔划是否被判定为复杂形状，以及对笔划端点进行直线拟合后得到的拟合质量是否满足配置的阈值。
        /// </remarks>
        /// <returns>`true` 如果笔划满足自动拉直的条件并应被替换为直线，`false` 否则。</returns>
        private bool ShouldStraightenLine(Stroke stroke)
        {
            // 分辨率自适应阈值
            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();
            double lineLength = GetDistance(start, end);
            double adaptiveThreshold = Settings.Canvas.AutoStraightenLineThreshold * GetResolutionScale();

            // 如果线条太短，不进行拉直处理
            if (lineLength < adaptiveThreshold)
            {
                Debug.WriteLine($"线条太短: {lineLength} < {adaptiveThreshold}");
                return false;
            }

            // 检查复杂度
            if (IsComplexShape(stroke))
            {
                Debug.WriteLine("拒绝拉直：检测到复杂形状");
                return false;
            }

            Point endpoint1, endpoint2;
            bool shouldStraighten = TryGetStraightLineEndpoints(stroke, out endpoint1, out endpoint2);

            if (shouldStraighten)
            {
                Debug.WriteLine($"接受拉直：判断为直线，解释方差比例满足阈值");
            }
            else
            {
                Debug.WriteLine($"拒绝拉直：判断不满足直线条件");
            }

            return shouldStraighten;
        }

        /// <summary>
        /// 计算墨迹的直线度评分（0-1，1表示完美直线）
        /// </summary>
        /// <param name="stroke">要分析的笔画</param>
        /// <returns>返回直线度评分，范围为0到1，1表示完美直线</returns>
        /// <remarks>
        /// 计算墨迹直线度评分的流程：
        /// 1. 确保笔画有足够的点（至少3个点）
        /// 2. 计算线条长度
        /// 3. 计算偏差评分（基于点到直线的距离）：
        ///    - 计算所有点到直线的平均偏差和最大偏差
        ///    - 根据偏差计算评分
        /// 4. 计算方向一致性评分：
        ///    - 计算每个线段与目标方向的角度差
        ///    - 将角度差转换为评分
        /// 5. 计算路径效率评分：
        ///    - 计算实际路径长度与直线距离的比例
        /// 6. 计算端点连接度评分（默认满分）
        /// 7. 综合评分（加权平均）：
        ///    - 偏差评分：40%
        ///    - 方向一致性评分：30%
        ///    - 路径效率评分：20%
        ///    - 端点连接度评分：10%
        /// 8. 返回最终评分，确保在0到1之间
        /// <summary>
        /// 评估给定笔划与理想直线的相似度并返回归一化的直线度评分。
        /// </summary>
        /// <remarks>
        /// 评分综合考虑：点到起止直线的偏差、方向一致性、路径长度相对于直线距离的效率，以及端点连接性；最终结果归一化到 0 到 1 的范围内，1 表示完全像直线。
        /// </remarks>
        /// <returns>`0` 到 `1` 之间的值，越接近 `1` 表示笔划越像一条直线。</returns>
        private double CalculateStraightnessScore(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 3) return 0;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();
            double lineLength = GetDistance(start, end);

            if (lineLength == 0) return 0;

            // 1. 计算偏差评分（基于点到直线的距离）
            double totalDeviation = 0;
            double maxDeviation = 0;
            int pointCount = 0;

            foreach (StylusPoint sp in stroke.StylusPoints)
            {
                Point p = sp.ToPoint();
                double deviation = DistanceFromLineToPoint(start, end, p);
                totalDeviation += deviation;
                maxDeviation = Math.Max(maxDeviation, deviation);
                pointCount++;
            }

            double avgDeviation = totalDeviation / pointCount;

            // 偏差评分：基于平均偏差和最大偏差
            double deviationScore = Math.Max(0, 1 - (avgDeviation / (lineLength * 0.05)) - (maxDeviation / (lineLength * 0.1)));

            // 2. 计算方向一致性评分
            double directionScore = CalculateDirectionConsistency(stroke);

            // 3. 计算路径效率评分（实际路径长度 vs 直线距离）
            double actualLength = 0;
            for (int i = 1; i < stroke.StylusPoints.Count; i++)
            {
                Point p1 = stroke.StylusPoints[i - 1].ToPoint();
                Point p2 = stroke.StylusPoints[i].ToPoint();
                actualLength += GetDistance(p1, p2);
            }
            double efficiencyScore = Math.Max(0, Math.Min(1, lineLength / actualLength));

            // 4. 计算端点连接度评分（起点到终点的直接性）
            double endpointScore = 1.0; // 默认满分，因为我们已经有了起点和终点

            // 综合评分（加权平均）
            double finalScore = (deviationScore * 0.4 + directionScore * 0.3 + efficiencyScore * 0.2 + endpointScore * 0.1);

            Debug.WriteLine($"直线度评分详情: 偏差={deviationScore:F3}, 方向={directionScore:F3}, 效率={efficiencyScore:F3}, 综合={finalScore:F3}");

            return Math.Max(0, Math.Min(1, finalScore));
        }

        /// <summary>
        /// 计算方向一致性评分
        /// </summary>
        /// <param name="stroke">要分析的笔画</param>
        /// <returns>返回方向一致性评分，范围为0到1，1表示方向完全一致</returns>
        /// <remarks>
        /// 计算方向一致性评分的流程：
        /// 1. 确保笔画有足够的点（至少5个点）
        /// 2. 计算目标方向（从起点到终点的方向）
        /// 3. 计算每个线段与目标方向的角度差：
        ///    - 遍历笔画中的每个线段
        ///    - 忽略太短的线段（长度小于2）
        ///    - 计算线段的角度
        ///    - 计算与目标方向的角度差
        ///    - 处理角度跨越问题（超过180度的情况）
        /// 4. 计算平均角度差
        /// 5. 将角度差转换为评分（0-1）：
        ///    - 0度差 = 1分
        ///    - 90度差 = 0分
        /// 6. 返回方向一致性评分
        /// <summary>
        /// 评估给定笔划各线段相对于笔划首尾连线方向的一致性得分。
        /// </summary>
        /// <param name="stroke">要评估方向一致性的笔划。</param>
        /// <returns>方向一致性得分，范围为 0 到 1；1 表示所有采样线段方向与笔划首尾方向完全一致，0 表示平均方向偏离约 90 度。</returns>
        /// <remarks>
        /// - 在笔划点数少于 5 或未产生有效线段时返回 1.0。  
        /// - 忽略长度小于 2 像素的短线段。  
        /// - 对每个有效线段计算其与笔划首尾方向的最小角差并取平均，随后将平均角差线性映射到 0–1 的评分区间（0° -> 1，90° -> 0）。  
        /// - 角度跨越（> π）时会做归一处理以得到最小角差。
        /// </remarks>
        private double CalculateDirectionConsistency(Stroke stroke)
        {
            if (stroke.StylusPoints.Count < 5) return 1.0;

            Point start = stroke.StylusPoints.First().ToPoint();
            Point end = stroke.StylusPoints.Last().ToPoint();

            // 目标方向
            double targetAngle = Math.Atan2(end.Y - start.Y, end.X - start.X);

            double totalAngleDifference = 0;
            int segmentCount = 0;

            // 计算每个线段与目标方向的角度差
            for (int i = 1; i < stroke.StylusPoints.Count; i++)
            {
                Point p1 = stroke.StylusPoints[i - 1].ToPoint();
                Point p2 = stroke.StylusPoints[i].ToPoint();

                double segmentLength = GetDistance(p1, p2);
                if (segmentLength < 2) continue; // 忽略太短的线段

                double segmentAngle = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                double angleDiff = Math.Abs(segmentAngle - targetAngle);

                // 处理角度跨越问题
                if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

                totalAngleDifference += angleDiff;
                segmentCount++;
            }

            if (segmentCount == 0) return 1.0;

            double avgAngleDifference = totalAngleDifference / segmentCount;

            // 将角度差转换为评分（0-1）
            // 0度差 = 1分，90度差 = 0分
            double directionScore = Math.Max(0, 1 - (avgAngleDifference / (Math.PI / 2)));

            return directionScore;
        }

        /// <summary>
        /// 在两点之间创建直线笔画
        /// </summary>
        /// <param name="start">直线的起始点</param>
        /// <param name="end">直线的结束点</param>
        /// <returns>返回包含直线点集的StylusPointCollection</returns>
        /// <remarks>
        /// 在两点之间创建直线笔画的流程：
        /// 1. 根据是否启用压感触屏模式决定如何设置压感：
        ///    - 如果未启用压感触屏模式、禁用压感、启用无压感矩形或使用钢笔类型1，则使用均匀粗细（压感值0.5）
        ///    - 否则，创建带有压感变化的直线：
        ///      - 计算中点
        ///      - 从起点到中点：压感从0.4渐变到0.8
        ///      - 从中点到终点：压感从0.8渐变到0.4
        /// 2. 使用GeneratePointsBetween方法生成点集
        /// 3. 返回生成的点集
        /// <summary>
        /// 生成连接两点的用于绘制直线的 StylusPointCollection，包含适当的压力值分布。
        /// </summary>
        /// <returns>连接 start 与 end 的 StylusPointCollection；在禁用触控压感或禁用压力或特定矩形/笔类型时使用恒定压力 0.5，否则通过中点将压力从 0.4 过渡到 0.8 再回到 0.4。</returns>
        private StylusPointCollection CreateStraightLine(Point start, Point end)
        {
            StylusPointCollection points = new StylusPointCollection();

            // 根据是否启用压感触屏模式决定如何设置压感
            // 如果未启用压感触屏模式，则使用均匀粗细
            if (!Settings.Canvas.EnablePressureTouchMode || Settings.Canvas.DisablePressure ||
                Settings.InkToShape.IsInkToShapeNoFakePressureRectangle || penType == 1)
            {
                var linePoints = GeneratePointsBetween(start, end, 0.5f, 0.5f, 8.0);
                foreach (var pt in linePoints)
                {
                    points.Add(pt);
                }
            }
            else
            {
                Point midPoint = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
                
                var startToMid = GeneratePointsBetween(start, midPoint, 0.4f, 0.8f, 8.0);
                foreach (var pt in startToMid)
                {
                    points.Add(pt);
                }
                
                var midToEnd = GeneratePointsBetween(midPoint, end, 0.8f, 0.4f, 8.0);
                for (int i = 1; i < midToEnd.Count; i++)
                {
                    points.Add(midToEnd[i]);
                }
            }

            return points;
        }

        /// <summary>
        /// 根据距离对点数进行采样
        /// </summary>
        /// <param name="points">原始点列表</param>
        /// <param name="sampleInterval">采样间隔，默认为10.0</param>
        /// <returns>返回采样后的点列表</returns>
        /// <remarks>
        /// 根据距离对点数进行采样的流程：
        /// 1. 确保原始点列表不为空且至少有2个点
        /// 2. 总是包含起点
        /// 3. 遍历原始点列表，计算累积距离：
        ///    - 当累积距离达到采样间隔时，添加当前点
        ///    - 重置累积距离
        /// 4. 总是包含终点（如果还没有包含）
        /// 5. 返回采样后的点列表
        /// <summary>
        /// 按距离间隔重采样点集，生成间隔约为指定距离的一组代表点以用于后续处理。
        /// </summary>
        /// <param name="points">原始点序列（按顺序）。如果为 null 或点数少于 2，则原样返回。</param>
        /// <param name="sampleInterval">期望的采样间隔（像素），默认为 10.0。</param>
        /// <returns>按顺序返回重采样后的点列表；始终包含起点，并在必要时包含终点，其他点以大致等距采样结果为准。</returns>
        private List<Point> SamplePointsByDistance(List<Point> points, double sampleInterval = 10.0)
        {
            if (points == null || points.Count < 2)
                return points;

            List<Point> sampledPoints = new List<Point>();
            sampledPoints.Add(points[0]); // 总是包含起点

            double accumulatedDistance = 0;
            Point lastSampledPoint = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                double segmentDistance = GetDistance(lastSampledPoint, points[i]);
                accumulatedDistance += segmentDistance;

                // 当累积距离达到采样间隔时，添加当前点
                if (accumulatedDistance >= sampleInterval)
                {
                    sampledPoints.Add(points[i]);
                    lastSampledPoint = points[i];
                    accumulatedDistance = 0; // 重置累积距离
                }
            }

            // 总是包含终点（如果还没有包含）
            if (sampledPoints.Count == 0 || GetDistance(sampledPoints.Last(), points.Last()) > 1.0)
            {
                sampledPoints.Add(points.Last());
            }

            return sampledPoints;
        }

        /// <summary>
        /// 计算点到直线的距离
        /// </summary>
        /// <param name="lineStart">直线的起始点</param>
        /// <param name="lineEnd">直线的结束点</param>
        /// <param name="point">要计算距离的点</param>
        /// <returns>返回点到直线的距离</returns>
        /// <remarks>
        /// 计算点到直线距离的流程：
        /// 1. 计算直线的长度
        /// 2. 如果直线长度为0（即两个点重合），则返回点到该点的距离
        /// 3. 否则，使用叉积计算点到直线的垂直距离
        /// 4. 返回计算得到的距离
        /// <summary>
        /// 计算点到由 lineStart 和 lineEnd 定义的直线的垂直距离。
        /// </summary>
        /// <param name="lineStart">直线的起点。</param>
        /// <param name="lineEnd">直线的终点。</param>
        /// <param name="point">要测量的点。</param>
        /// <returns>点到该直线的垂直距离；如果起点与终点重合，则返回点到该重合点的欧氏距离。</returns>
        private double DistanceFromLineToPoint(Point lineStart, Point lineEnd, Point point)
        {
            // Calculate distance from point to line defined by lineStart and lineEnd
            double lineLength = GetDistance(lineStart, lineEnd);
            if (lineLength == 0) return GetDistance(point, lineStart);

            // Calculate the cross product to get the perpendicular distance
            double distance = Math.Abs((lineEnd.Y - lineStart.Y) * point.X -
                                      (lineEnd.X - lineStart.X) * point.Y +
                                      lineEnd.X * lineStart.Y - lineEnd.Y * lineStart.X) / lineLength;
            return distance;
        }

        /// <summary>
        /// 判断一个 stroke 是否是直线（排除虚线和点线）
        /// </summary>
        /// <param name="stroke">要检查的 stroke</param>
        /// <returns>如果是直线返回 true，否则返回 false</returns>
        /// <remarks>
        /// 判断一个 stroke 是否是直线的流程：
        /// 1. 检查 stroke 是否为空或没有点，如果是则返回 false
        /// 2. 检查点的数量：
        ///    - 如果只有1个点，返回 false
        ///    - 如果有2个点：
        ///      - 计算两点之间的距离
        ///      - 如果距离小于10，返回 false
        ///      - 否则返回 true
        ///    - 如果有3个点：
        ///      - 计算第一个点和第三个点之间的距离
        ///      - 如果距离小于10，返回 false
        ///      - 计算第二个点到由第一个点和第三个点组成的直线的距离
        ///      - 如果距离相对于线段长度很小（小于1%），认为是直线，返回 true
        ///      - 否则返回 false
        ///    - 如果点的数量大于3，返回 false
        /// <summary>
        /// 判断给定的 Stroke 是否表示一条直线（排除点、过短或点数超过 3 的折线情况）。
        /// </summary>
        /// <param name="stroke">要检测的画笔笔迹。</param>
        /// <returns>`true` 如果笔迹表示一条直线：当包含 2 个采样点且端点间距离至少 10 像素；或包含 3 个采样点且中间点到两端直线的相对偏差小于 1%；否则 `false`。</returns>
        private bool IsStraightLine(Stroke stroke)
        {
            if (stroke == null || stroke.StylusPoints.Count == 0)
                return false;

            int pointCount = stroke.StylusPoints.Count;

            if (pointCount == 1)
                return false;

            // 最简单的直线：只有2个点
            if (pointCount == 2)
            {
                Point p1 = stroke.StylusPoints[0].ToPoint();
                Point p2 = stroke.StylusPoints[1].ToPoint();
                double lineLength = GetDistance(p1, p2);

                if (lineLength < 10)
                    return false;

                return true;
            }

            if (pointCount > 3)
                return false;

            // 对于3个点的情况，检查它们是否基本在一条直线上
            if (pointCount == 3)
            {
                Point p1 = stroke.StylusPoints[0].ToPoint();
                Point p2 = stroke.StylusPoints[1].ToPoint();
                Point p3 = stroke.StylusPoints[2].ToPoint();

                double totalLength = GetDistance(p1, p3);
                if (totalLength < 10)
                    return false;

                // 计算点到直线的距离
                // 使用 p1 和 p3 作为直线端点，检查 p2 是否在这条直线上
                double distance = DistanceFromLineToPoint(p1, p3, p2);

                // 如果点到直线的距离相对于线段长度很小，认为是直线
                // 使用相对误差阈值（比如 1%）
                if (totalLength > 0 && distance / totalLength < 0.01)
                    return true;

                return false;
            }

            return false;
        }

        /// <summary>
        /// 尝试将直线端点吸附到现有笔画的端点
        /// </summary>
        /// <param name="start">直线的起始点</param>
        /// <param name="end">直线的结束点</param>
        /// <returns>返回吸附后的端点数组，如果没有发生吸附则返回null</returns>
        /// <remarks>
        /// 尝试将直线端点吸附到现有笔画端点的流程：
        /// 1. 检查是否启用了线段端点吸附功能，如果没有启用则返回null
        /// 2. 初始化吸附状态和吸附后的点
        /// 3. 获取设置中的吸附距离阈值
        /// 4. 遍历画布中的所有笔画：
        ///    - 跳过没有点的笔画
        ///    - 只对直线进行端点吸附，跳过虚线和点线
        ///    - 获取笔画的起点和终点
        ///    - 检查起点是否应该吸附到现有笔画的端点
        ///    - 检查终点是否应该吸附到现有笔画的端点
        ///    - 如果两个端点都已经吸附，结束遍历
        /// 5. 如果发生了吸附，返回吸附后的端点数组，否则返回null
        /// <summary>
        /// 尝试将给定线段的起点和终点吸附到画布上已有直线的端点上。
        /// </summary>
        /// <remarks>
        /// 仅在 LineEndpointSnapping 启用时才会生效；匹配仅针对画布上被识别为“直线”的笔画的端点，使用 Settings.Canvas.LineEndpointSnappingThreshold 作为吸附距离阈值。
        /// 如果只发生部分吸附，会返回已吸附的点与未吸附点的组合。
        /// </remarks>
        /// <returns>`Point[]`：包含吸附后的起点和终点（按 [start, end] 顺序）；若未发生任何吸附则返回 null。</returns>
        private Point[] GetSnappedEndpoints(Point start, Point end)
        {
            if (!Settings.Canvas.LineEndpointSnapping)
                return null;

            bool startSnapped = false;
            bool endSnapped = false;
            Point snappedStart = start;
            Point snappedEnd = end;

            // 使用设置中的吸附距离阈值
            double snapThreshold = Settings.Canvas.LineEndpointSnappingThreshold;

            // Check all strokes in canvas for potential snap points
            foreach (Stroke stroke in inkCanvas.Strokes)
            {
                if (stroke.StylusPoints.Count == 0) continue;

                // 只对直线进行端点吸附，跳过虚线和点线
                if (!IsStraightLine(stroke))
                    continue;

                // Get stroke endpoints
                Point strokeStart = stroke.StylusPoints.First().ToPoint();
                Point strokeEnd = stroke.StylusPoints.Last().ToPoint();

                // Check if start point should snap to an endpoint
                if (!startSnapped)
                {
                    if (GetDistance(start, strokeStart) < snapThreshold)
                    {
                        snappedStart = strokeStart;
                        startSnapped = true;
                    }
                    else if (GetDistance(start, strokeEnd) < snapThreshold)
                    {
                        snappedStart = strokeEnd;
                        startSnapped = true;
                    }
                }

                // Check if end point should snap to an endpoint
                if (!endSnapped)
                {
                    if (GetDistance(end, strokeStart) < snapThreshold)
                    {
                        snappedEnd = strokeStart;
                        endSnapped = true;
                    }
                    else if (GetDistance(end, strokeEnd) < snapThreshold)
                    {
                        snappedEnd = strokeEnd;
                        endSnapped = true;
                    }
                }

                // If both endpoints are snapped, we're done
                if (startSnapped && endSnapped) break;
            }

            // Return snapped points if any snapping occurred
            if (startSnapped || endSnapped)
            {
                return new[] { snappedStart, snappedEnd };
            }

            return null;
        }

        /// <summary>
        /// 设置新的笔画备份
        /// </summary>
        /// <remarks>
        /// 设置新的笔画备份的流程：
        /// 1. 克隆当前墨水画布的笔画集合
        /// 2. 获取当前白板索引
        /// 3. 如果当前模式为0，则将白板索引设置为0
        /// 4. 将克隆的笔画集合存储到strokeCollections中对应索引的位置
        /// <summary>
        /// 在画布当前白板索引位置保存一份当前笔画集合的备份副本。
        /// </summary>
        /// <remarks>
        /// 克隆 InkCanvas.Strokes 到 lastTouchDownStrokeCollection，并将该副本存入 strokeCollections 的 CurrentWhiteboardIndex 位置；当 currentMode 为 0 时使用索引 0 代替 CurrentWhiteboardIndex。
        /// </remarks>
        private void SetNewBackupOfStroke()
        {
            lastTouchDownStrokeCollection = inkCanvas.Strokes.Clone();
            var whiteboardIndex = CurrentWhiteboardIndex;
            if (currentMode == 0) whiteboardIndex = 0;

            strokeCollections[whiteboardIndex] = lastTouchDownStrokeCollection;
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="point1">第一个点</param>
        /// <param name="point2">第二个点</param>
        /// <returns>返回两点之间的距离</returns>
        /// <remarks>
        /// 使用欧几里得距离公式计算两点之间的距离：
        /// distance = √[(x2 - x1)² + (y2 - y1)²]
        /// <summary>
        /// 计算两个点之间的欧几里得距离。
        /// </summary>
        /// <param name="point1">第一个点的坐标。</param>
        /// <param name="point2">第二个点的坐标。</param>
        /// <returns>两点之间的欧几里得距离（大于或等于 0）。</returns>
        public double GetDistance(Point point1, Point point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                             (point1.Y - point2.Y) * (point1.Y - point2.Y));
        }

        /// <summary>
        /// 计算点的速度
        /// </summary>
        /// <param name="point1">第一个点</param>
        /// <param name="point2">第二个点（当前点）</param>
        /// <param name="point3">第三个点</param>
        /// <returns>返回点的速度</returns>
        /// <remarks>
        /// 计算点速度的流程：
        /// 1. 计算第一个点到第二个点的距离
        /// 2. 计算第三个点到第二个点的距离
        /// 3. 将两个距离相加
        /// 4. 除以20，得到速度值
        /// <summary>
        /// 计算由三个连续采样点近似得到的点速度度量。
        /// </summary>
        /// <param name="point1">中间点之前的点（前一采样点）。</param>
        /// <param name="point2">参考的中间点。</param>
        /// <param name="point3">中间点之后的点（后一采样点）。</param>
        /// <returns>返回从中间点到前后两点距离之和除以 20 的速度度量。</returns>
        public double GetPointSpeed(Point point1, Point point2, Point point3)
        {
            return (Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                              (point1.Y - point2.Y) * (point1.Y - point2.Y))
                    + Math.Sqrt((point3.X - point2.X) * (point3.X - point2.X) +
                                (point3.Y - point2.Y) * (point3.Y - point2.Y)))
                   / 20;
        }

        /// <summary>
        /// 微调给定的两点，使它们在接近水平或接近垂直的情况下更严格地对齐水平或垂直方向。
        /// </summary>
        /// <param name="p1">线段的一端坐标。</param>
        /// <param name="p2">线段的另一端坐标。</param>
        /// <returns>包含调整后两端点的数组，顺序为 { p1, p2 }。</returns>
        public Point[] FixPointsDirection(Point p1, Point p2)
        {
            double deltaY = Math.Abs(p1.Y - p2.Y);
            double deltaX = Math.Abs(p1.X - p2.X);
            
            if (deltaY < 1e-10 || deltaX / deltaY > 8)
            {
                //水平
                var x = deltaY / 2;
                if (p1.Y > p2.Y)
                {
                    p1.Y -= x;
                    p2.Y += x;
                }
                else
                {
                    p1.Y += x;
                    p2.Y -= x;
                }
            }
            else if (deltaX < 1e-10 || deltaY / deltaX > 8)
            {
                //垂直
                var x = deltaX / 2;
                if (p1.X > p2.X)
                {
                    p1.X -= x;
                    p2.X += x;
                }
                else
                {
                    p1.X += x;
                    p2.X -= x;
                }
            }

            return new Point[2] { p1, p2 };
        }

        public StylusPointCollection GenerateFakePressureTriangle(StylusPointCollection points)
        {
            var newPoint = new StylusPointCollection();
            
            if (Settings.InkToShape.IsInkToShapeNoFakePressureTriangle || penType == 1)
            {
                if (points.Count >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Point start = points[i].ToPoint();
                        Point end = points[(i + 1) % 3].ToPoint();
                        var edgePoints = GeneratePointsBetween(start, end, 0.5f, 0.5f, 8.0);
                        if (i == 0)
                        {
                            foreach (var pt in edgePoints)
                            {
                                newPoint.Add(pt);
                            }
                        }
                        else
                        {
                            for (int j = 1; j < edgePoints.Count; j++)
                            {
                                newPoint.Add(edgePoints[j]);
                            }
                        }
                    }
                    Point lastPoint = points[0].ToPoint();
                    Point firstPoint = newPoint[0].ToPoint();
                    if (GetDistance(lastPoint, firstPoint) > 1.0)
                    {
                        newPoint.Add(new StylusPoint(lastPoint.X, lastPoint.Y, 0.5f));
                    }
                }
                else
                {
                    return points;
                }
                return newPoint;
            }
            else
            {
                if (points.Count >= 3)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Point start = points[i].ToPoint();
                        Point end = points[(i + 1) % 3].ToPoint();
                        
                        Point midPoint = GetCenterPoint(start, end);
                        
                        var startToMid = GeneratePointsBetween(start, midPoint, 0.4f, 0.8f, 8.0);
                        if (i == 0)
                        {
                            foreach (var pt in startToMid)
                            {
                                newPoint.Add(pt);
                            }
                        }
                        else
                        {
                            for (int j = 1; j < startToMid.Count; j++)
                            {
                                newPoint.Add(startToMid[j]);
                            }
                        }
                        
                        var midToEnd = GeneratePointsBetween(midPoint, end, 0.8f, 0.4f, 8.0);
                        for (int j = 1; j < midToEnd.Count; j++)
                        {
                            newPoint.Add(midToEnd[j]);
                        }
                    }
                    Point lastPoint = points[0].ToPoint();
                    Point firstPoint = newPoint[0].ToPoint();
                    if (GetDistance(lastPoint, firstPoint) > 1.0)
                    {
                        newPoint.Add(new StylusPoint(lastPoint.X, lastPoint.Y, 0.4f));
                    }
                }
                else
                {
                    return points;
                }
                return newPoint;
            }
        }

        /// <summary>
        /// 在两点之间生成多个点，用于增加图形边缘的点密度
        /// </summary>
        private StylusPointCollection GeneratePointsBetween(Point start, Point end, float startPressure, float endPressure, double minPointInterval = 8.0)
        {
            var result = new StylusPointCollection();
            double distance = GetDistance(start, end);
            
            if (distance < minPointInterval)
            {
                result.Add(new StylusPoint(start.X, start.Y, startPressure));
                result.Add(new StylusPoint(end.X, end.Y, endPressure));
                return result;
            }
            
            int pointCount = Math.Max(2, (int)(distance / minPointInterval) + 1);
            
            result.Add(new StylusPoint(start.X, start.Y, startPressure));
            
            for (int i = 1; i < pointCount - 1; i++)
            {
                double ratio = (double)i / (pointCount - 1);
                double pressure = startPressure + (endPressure - startPressure) * ratio;
                double x = start.X + (end.X - start.X) * ratio;
                double y = start.Y + (end.Y - start.Y) * ratio;
                result.Add(new StylusPoint(x, y, (float)pressure));
            }
            
            result.Add(new StylusPoint(end.X, end.Y, endPressure));
            
            return result;
        }

        public StylusPointCollection GenerateFakePressureRectangle(StylusPointCollection points)
        {
            var newPoint = new StylusPointCollection();
            
            if (Settings.InkToShape.IsInkToShapeNoFakePressureRectangle || penType == 1)
            {
                if (points.Count >= 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Point start = points[i].ToPoint();
                        Point end = points[(i + 1) % 4].ToPoint();
                        var edgePoints = GeneratePointsBetween(start, end, 0.5f, 0.5f, 8.0);
                        if (i == 0)
                        {
                            foreach (var pt in edgePoints)
                            {
                                newPoint.Add(pt);
                            }
                        }
                        else
                        {
                            for (int j = 1; j < edgePoints.Count; j++)
                            {
                                newPoint.Add(edgePoints[j]);
                            }
                        }
                    }
                }
                else
                {
                    return points;
                }
                return newPoint;
            }

            if (points.Count >= 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    Point start = points[i].ToPoint();
                    Point end = points[(i + 1) % 4].ToPoint();
                    
                    Point midPoint = GetCenterPoint(start, end);
                    
                    var startToMid = GeneratePointsBetween(start, midPoint, 0.4f, 0.8f, 8.0);
                    if (i == 0)
                    {
                        foreach (var pt in startToMid)
                        {
                            newPoint.Add(pt);
                        }
                    }
                    else
                    {
                        for (int j = 1; j < startToMid.Count; j++)
                        {
                            newPoint.Add(startToMid[j]);
                        }
                    }
                    
                    var midToEnd = GeneratePointsBetween(midPoint, end, 0.8f, 0.4f, 8.0);
                    for (int j = 1; j < midToEnd.Count; j++)
                    {
                        newPoint.Add(midToEnd[j]);
                    }
                }
            }
            else
            {
                return points;
            }
            
            return newPoint;
        }

        public Point GetCenterPoint(Point point1, Point point2)
        {
            return new Point((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        public StylusPoint GetCenterPoint(StylusPoint point1, StylusPoint point2)
        {
            return new StylusPoint((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2);
        }

        // 分辨率自适应：以1080P为基准，返回当前分辨率下的阈值倍数
        private double GetResolutionScale()
        {
            // 以1920x1080为基准
            double baseWidth = 1920.0;
            double baseHeight = 1080.0;
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            // 取宽高平均缩放，防止极端比例
            double scaleW = screenWidth / baseWidth;
            double scaleH = screenHeight / baseHeight;
            return (scaleW + scaleH) / 2.0;
        }

        #region 矩形参考线系统

        /// <summary>
        /// 处理矩形参考线系统
        /// </summary>
        private void ProcessRectangleGuideLines(Stroke newStroke)
        {
            // 只有启用矩形识别时才处理
            if (!Settings.InkToShape.IsInkToShapeRectangle) return;

            // 检查新笔画是否为直线
            if (!IsPotentialStraightLine(newStroke)) return;

            Point startPoint = newStroke.StylusPoints[0].ToPoint();
            Point endPoint = newStroke.StylusPoints[newStroke.StylusPoints.Count - 1].ToPoint();

            // 创建新的参考线
            var newGuideLine = new RectangleGuideLine(newStroke, startPoint, endPoint);

            // 清理过期的参考线（超过30秒的）
            CleanupExpiredGuideLines();

            // 添加新参考线
            rectangleGuideLines.Add(newGuideLine);

            // 检查是否可以构成矩形
            CheckForRectangleFormation();
        }

        /// <summary>
        /// 清理过期的参考线
        /// </summary>
        private void CleanupExpiredGuideLines()
        {
            var expireTime = DateTime.Now.AddSeconds(-30); // 30秒过期
            for (int i = rectangleGuideLines.Count - 1; i >= 0; i--)
            {
                var guideLine = rectangleGuideLines[i];
                if (guideLine.CreatedTime < expireTime || !inkCanvas.Strokes.Contains(guideLine.OriginalStroke))
                {
                    rectangleGuideLines.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 检查是否可以构成矩形
        /// </summary>
        private void CheckForRectangleFormation()
        {
            if (rectangleGuideLines.Count < 4) return;

            // 尝试找到四条能构成矩形的直线
            var rectangleLines = FindRectangleLines();
            if (rectangleLines != null && rectangleLines.Count == 4)
            {
                // 创建矩形并替换原有直线
                CreateRectangleFromLines(rectangleLines);
            }
        }

        /// <summary>
        /// 寻找能构成矩形的四条直线
        /// </summary>
        private List<RectangleGuideLine> FindRectangleLines()
        {
            // 按时间排序，优先考虑最近绘制的直线
            var sortedLines = rectangleGuideLines.OrderByDescending(l => l.CreatedTime).ToList();

            // 尝试不同的四条直线组合
            for (int i = 0; i < sortedLines.Count - 3; i++)
            {
                for (int j = i + 1; j < sortedLines.Count - 2; j++)
                {
                    for (int k = j + 1; k < sortedLines.Count - 1; k++)
                    {
                        for (int l = k + 1; l < sortedLines.Count; l++)
                        {
                            var lines = new List<RectangleGuideLine> { sortedLines[i], sortedLines[j], sortedLines[k], sortedLines[l] };
                            if (CanFormRectangle(lines))
                            {
                                return lines;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 判断四条直线是否能构成矩形
        /// </summary>
        private bool CanFormRectangle(List<RectangleGuideLine> lines)
        {
            if (lines.Count != 4) return false;

            // 分类水平线和垂直线
            var horizontalLines = lines.Where(l => l.IsHorizontal).ToList();
            var verticalLines = lines.Where(l => l.IsVertical).ToList();

            // 必须有2条水平线和2条垂直线
            if (horizontalLines.Count != 2 || verticalLines.Count != 2) return false;

            // 检查端点相交关系
            return CheckEndpointConnections(horizontalLines, verticalLines);
        }

        /// <summary>
        /// 检查端点相交关系
        /// </summary>
        private bool CheckEndpointConnections(List<RectangleGuideLine> horizontalLines, List<RectangleGuideLine> verticalLines)
        {
            // 收集所有端点
            var allEndpoints = new List<Point>();
            foreach (var line in horizontalLines.Concat(verticalLines))
            {
                allEndpoints.Add(line.StartPoint);
                allEndpoints.Add(line.EndPoint);
            }

            // 检查是否有4个相交点（允许一定误差）
            var intersectionPoints = new List<Point>();

            foreach (var hLine in horizontalLines)
            {
                foreach (var vLine in verticalLines)
                {
                    var intersection = GetLineIntersection(hLine, vLine);
                    if (intersection.HasValue)
                    {
                        // 检查交点是否在两条线段的端点附近
                        if (IsPointNearLineEndpoints(intersection.Value, hLine) &&
                            IsPointNearLineEndpoints(intersection.Value, vLine))
                        {
                            intersectionPoints.Add(intersection.Value);
                        }
                    }
                }
            }

            // 需要有4个交点才能构成矩形
            return intersectionPoints.Count >= 4;
        }

        /// <summary>
        /// 计算两条直线的交点
        /// </summary>
        private Point? GetLineIntersection(RectangleGuideLine line1, RectangleGuideLine line2)
        {
            double x1 = line1.StartPoint.X, y1 = line1.StartPoint.Y;
            double x2 = line1.EndPoint.X, y2 = line1.EndPoint.Y;
            double x3 = line2.StartPoint.X, y3 = line2.StartPoint.Y;
            double x4 = line2.EndPoint.X, y4 = line2.EndPoint.Y;

            double denom = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);
            if (Math.Abs(denom) < 1e-10) return null; // 平行线

            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / denom;
            double u = -((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) / denom;

            double intersectionX = x1 + t * (x2 - x1);
            double intersectionY = y1 + t * (y2 - y1);

            return new Point(intersectionX, intersectionY);
        }

        /// <summary>
        /// 检查点是否在直线端点附近
        /// </summary>
        private bool IsPointNearLineEndpoints(Point point, RectangleGuideLine line)
        {
            double distToStart = GetDistance(point, line.StartPoint);
            double distToEnd = GetDistance(point, line.EndPoint);

            return distToStart <= RECTANGLE_ENDPOINT_THRESHOLD || distToEnd <= RECTANGLE_ENDPOINT_THRESHOLD;
        }

        /// <summary>
        /// 从四条直线创建矩形
        /// </summary>
        private void CreateRectangleFromLines(List<RectangleGuideLine> lines)
        {
            try
            {
                // 计算矩形的四个角点
                var corners = CalculateRectangleCorners(lines);
                if (corners == null || corners.Count != 4) return;

                // 创建矩形笔画
                var pointList = new List<Point>(corners) { corners[0] }; // 闭合矩形
                var point = new StylusPointCollection(pointList);
                var rectangleStroke = new Stroke(GenerateFakePressureRectangle(point))
                {
                    DrawingAttributes = inkCanvas.DefaultDrawingAttributes.Clone()
                };

                // 移除原有的四条直线
                SetNewBackupOfStroke();
                _currentCommitType = CommitReason.ShapeRecognition;

                foreach (var line in lines)
                {
                    if (inkCanvas.Strokes.Contains(line.OriginalStroke))
                    {
                        inkCanvas.Strokes.Remove(line.OriginalStroke);
                    }
                }

                // 添加新的矩形
                inkCanvas.Strokes.Add(rectangleStroke);
                _currentCommitType = CommitReason.UserInput;

                // 清理参考线
                foreach (var line in lines)
                {
                    rectangleGuideLines.Remove(line);
                }

                // 清空新笔画集合，避免重复处理
                newStrokes = new StrokeCollection();

                Debug.WriteLine("成功创建矩形参考线矩形");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"创建矩形时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算矩形的四个角点
        /// </summary>
        private List<Point> CalculateRectangleCorners(List<RectangleGuideLine> lines)
        {
            var horizontalLines = lines.Where(l => l.IsHorizontal).ToList();
            var verticalLines = lines.Where(l => l.IsVertical).ToList();

            if (horizontalLines.Count != 2 || verticalLines.Count != 2) return null;

            var corners = new List<Point>();

            // 计算四个交点
            foreach (var hLine in horizontalLines)
            {
                foreach (var vLine in verticalLines)
                {
                    var intersection = GetLineIntersection(hLine, vLine);
                    if (intersection.HasValue)
                    {
                        corners.Add(intersection.Value);
                    }
                }
            }

            if (corners.Count != 4) return null;

            // 按顺序排列角点（顺时针或逆时针）
            return SortRectangleCorners(corners);
        }

        /// <summary>
        /// 按顺序排列矩形角点
        /// </summary>
        private List<Point> SortRectangleCorners(List<Point> corners)
        {
            if (corners.Count != 4) return corners;

            // 计算中心点
            double centerX = corners.Average(p => p.X);
            double centerY = corners.Average(p => p.Y);
            var center = new Point(centerX, centerY);

            // 按角度排序
            return corners.OrderBy(p => Math.Atan2(p.Y - center.Y, p.X - center.X)).ToList();
        }

        #endregion
    }
}