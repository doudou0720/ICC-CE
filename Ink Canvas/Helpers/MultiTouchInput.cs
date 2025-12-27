using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Helpers
{
    public class VisualCanvas : FrameworkElement
    {
        private readonly List<DrawingVisual> _visuals = new List<DrawingVisual>();

        /// <summary>
        /// Retrieve the visual child at the specified zero-based index.
        /// </summary>
        /// <param name="index">Zero-based index of the visual to retrieve.</param>
        /// <returns>The visual child at the given index.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="index"/> is less than 0 or greater than or equal to the visual count.</exception>
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visuals.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _visuals[index];
        }

        protected override int VisualChildrenCount => _visuals.Count;

        /// <summary>
        /// Initializes a new VisualCanvas configured for bitmap caching and low-quality, aliased rendering with caching enabled.
        /// </summary>
        public VisualCanvas()
        {
            CacheMode = new BitmapCache();
            
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.LowQuality); 
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased); 
            RenderOptions.SetCachingHint(this, CachingHint.Cache); 
        }

        /// <summary>
        /// Adds the specified DrawingVisual to the internal visual collection and registers it as a visual child.
        /// </summary>
        /// <param name="visual">The DrawingVisual to add; if null, no action is taken.</param>
        public void AddVisual(DrawingVisual visual)
        {
            if (visual == null) return;
            _visuals.Add(visual);
            AddVisualChild(visual);
        }
        /// <summary>
        /// Removes every DrawingVisual from the visual tree and clears the internal visuals collection.
        /// </summary>
        public void Clear()
        {
            foreach (var visual in _visuals)
            {
                RemoveVisualChild(visual);
            }
            _visuals.Clear();
        }

        public IReadOnlyList<DrawingVisual> Visuals => _visuals;
    }

    /// <summary>
    /// 用于显示笔迹的类 
    /// </summary>
    public class StrokeVisual
    {
        private int _lastDrawnPointCount = 0; 
        private const int INCREMENTAL_DRAW_THRESHOLD = 2; 
        private VisualCanvas _visualCanvas; 

        /// <summary>
        ///     创建显示笔迹的类
        /// <summary>
        /// Initializes a new StrokeVisual configured with default drawing attributes: red color and a pen size of 3 by 3.
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes
        {
            Color = Colors.Red,
            //FitToCurve = true,
            Width = 3,
            Height = 3
        })
        {
        }

        /// <summary>
        /// 创建显示笔迹的类
        /// </summary>
        /// <summary>
        /// Initializes a new StrokeVisual configured with the specified drawing attributes.
        /// </summary>
        /// <param name="drawingAttributes">The drawing attributes (color, width, height, etc.) used to render the stroke.</param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        /// <summary>
        /// 设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        /// 设置关联的VisualCanvas
        /// <summary>
        /// Associates this StrokeVisual with the provided VisualCanvas so subsequent drawing operations are added to that canvas.
        /// </summary>
        /// <param name="visualCanvas">The VisualCanvas to use for rendering stroke segments, or null to disassociate.</param>
        public void SetVisualCanvas(VisualCanvas visualCanvas)
        {
            _visualCanvas = visualCanvas;
        }

        /// <summary>
        /// 在笔迹中添加点
        /// </summary>
        /// <summary>
        /// Adds a StylusPoint to the current stroke, creating a new Stroke with the visual's drawing attributes if no stroke exists.
        /// </summary>
        /// <param name="point">The stylus point to append to the stroke.</param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection { point };
                Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        /// 绘制点段到新的DrawingVisual
        /// <summary>
        /// Renders the stroke points in the range [startIndex, endIndex) into a new DrawingVisual and adds it to the associated VisualCanvas.
        /// </summary>
        /// <param name="startIndex">Inclusive start index of the StylusPoints range to draw.</param>
        /// <param name="endIndex">Exclusive end index of the StylusPoints range to draw.</param>
        /// <remarks>
        /// No action is performed if the Stroke is null, has no points, the VisualCanvas has not been set, or if the index range is invalid (startIndex &gt;= endIndex, startIndex &lt; 0, or endIndex &gt; StylusPoints.Count).
        /// </remarks>
        private void DrawSegmentToNewVisual(int startIndex, int endIndex)
        {
            if (Stroke == null || Stroke.StylusPoints.Count == 0 || _visualCanvas == null) return;
            if (startIndex >= endIndex || startIndex < 0 || endIndex > Stroke.StylusPoints.Count) return;

            var points = Stroke.StylusPoints;
            var drawingAttributes = Stroke.DrawingAttributes;
            
            // 创建新的DrawingVisual用于绘制这个点段
            var segmentVisual = new DrawingVisual();
            
            RenderOptions.SetBitmapScalingMode(segmentVisual, BitmapScalingMode.LowQuality);
            RenderOptions.SetEdgeMode(segmentVisual, EdgeMode.Aliased);
            RenderOptions.SetCachingHint(segmentVisual, CachingHint.Cache);

            using (var dc = segmentVisual.RenderOpen())
            {
                var pen = new Pen(new SolidColorBrush(drawingAttributes.Color), drawingAttributes.Width);
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
                pen.LineJoin = PenLineJoin.Round;

                // 绘制指定范围内的点段
                if (endIndex - startIndex >= 2)
                {
                    // 多个点，绘制线段
                    for (int i = startIndex; i < endIndex - 1 && i < points.Count - 1; i++)
                    {
                        var startPoint = new Point(points[i].X, points[i].Y);
                        var endPoint = new Point(points[i + 1].X, points[i + 1].Y);
                        dc.DrawLine(pen, startPoint, endPoint);
                    }
                }
                else if (endIndex - startIndex == 1 && startIndex < points.Count)
                {
                    // 只有一个点，绘制圆点
                    var brush = new SolidColorBrush(drawingAttributes.Color);
                    var point = points[startIndex];
                    dc.DrawEllipse(brush, null, new Point(point.X, point.Y), 
                        drawingAttributes.Width / 2, drawingAttributes.Height / 2);
                }
            }

            // 将新的DrawingVisual添加到VisualCanvas中
            _visualCanvas.AddVisual(segmentVisual);
        }

        /// <summary>
        /// 重新画出笔迹
        /// <summary>
        /// Renders any new stylus points of the associated Stroke into the linked VisualCanvas, performing incremental updates when possible.
        /// </summary>
        /// <remarks>
        /// Does nothing if there is no Stroke, no StylusPoints, or no associated VisualCanvas. When enough new points have been added (or on the first draw), this method renders only the new segment(s) and updates internal draw state; otherwise it skips drawing. Exceptions thrown during rendering are caught and ignored to avoid interrupting caller code.
        /// </remarks>
        public void Redraw()
        {
            if (Stroke == null || _visualCanvas == null) return;

            var currentPointCount = Stroke.StylusPoints.Count;
            if (currentPointCount == 0) return;

            // 计算新增的点数
            int newPointCount = currentPointCount - _lastDrawnPointCount;

            // 如果新增点数达到阈值，才进行增量绘制
            if (newPointCount >= INCREMENTAL_DRAW_THRESHOLD || _lastDrawnPointCount == 0)
            {
                try
                {
                    if (_lastDrawnPointCount == 0)
                    {
                        // 首次绘制：绘制所有点
                        DrawSegmentToNewVisual(0, currentPointCount);
                        _lastDrawnPointCount = currentPointCount;
                    }
                    else
                    {
                        // 从上次绘制的最后一个点开始
                        int startIndex = Math.Max(0, _lastDrawnPointCount - 1);
                        DrawSegmentToNewVisual(startIndex, currentPointCount);
                        _lastDrawnPointCount = currentPointCount;
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// 强制重绘
        /// <summary>
        /// Clears any rendered visuals, resets the internal drawn-point counter, and forces the stroke to be redrawn.
        /// If no VisualCanvas is associated, the counter is still reset and a redraw is attempted.
        /// </summary>
        public void ForceRedraw()
        {
            if (_visualCanvas != null)
            {
                _visualCanvas.Clear();
            }
            _lastDrawnPointCount = 0;
            Redraw();
        }

        private readonly DrawingAttributes _drawingAttributes;

        public static implicit operator Stroke(StrokeVisual v)
        {
            throw new NotImplementedException();
        }
    }
}