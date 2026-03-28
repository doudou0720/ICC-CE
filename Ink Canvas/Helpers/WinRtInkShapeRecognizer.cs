using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Ink;
using System.Windows.Media;
using OSVersionExtension;
using WinRtInkAnalyzer = global::Windows.UI.Input.Inking.Analysis.InkAnalyzer;
using SysPoint = System.Windows.Point;

namespace Ink_Canvas.Helpers
{
    /// <summary>基于 Windows.UI.Input.Inking.Analysis 的形状识别（适用于 64 位进程等场景）。</summary>
    internal static class WinRtInkShapeRecognizer
    {
        public static bool IsApiAvailable =>
            OSVersion.GetOperatingSystem() >= OSVersionExtension.OperatingSystem.Windows10;

        public static void Warmup()
        {
            if (!IsApiAvailable) return;
            try
            {
                RecognizeShape(new StrokeCollection());
            }
            catch
            {
                // ignore
            }
        }

        public static InkShapeRecognitionResult RecognizeShape(StrokeCollection strokes)
        {
            if (!IsApiAvailable || strokes == null || strokes.Count == 0)
                return InkShapeRecognitionResult.Empty;

            try
            {
                return RecognizeShapeAsync(strokes).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return InkShapeRecognitionResult.Empty;
            }
        }

        private static async Task<InkShapeRecognitionResult> RecognizeShapeAsync(StrokeCollection strokes)
        {
            var analyzer = new WinRtInkAnalyzer();
            foreach (Stroke s in strokes)
            {
                var inkStroke = CreateInkStrokeFromWpf(s);
                if (inkStroke != null)
                    analyzer.AddDataForStroke(inkStroke);
            }

            await analyzer.AnalyzeAsync().AsTask().ConfigureAwait(false);

            var drawing = FindPrimaryDrawing(analyzer);
            if (drawing == null ||
                drawing.DrawingKind == global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Drawing)
                return InkShapeRecognitionResult.Empty;

            var name = MapDrawingKindToShapeName(drawing.DrawingKind);
            if (string.IsNullOrEmpty(name) || name == "Drawing")
                return InkShapeRecognitionResult.Empty;

            var winPts = CopyWinRtPoints(drawing);
            var hot = ToWpfPointCollection(winPts);
            var c = drawing.Center;
            var centroid = new SysPoint(c.X, c.Y);
            BoundsFromPoints(winPts, out double w, out double h);

            var toRemove = new StrokeCollection();
            foreach (Stroke s in strokes)
                toRemove.Add(s);

            return new InkShapeRecognitionResult(name, centroid, hot, w, h, toRemove);
        }

        private static global::Windows.UI.Input.Inking.InkStroke CreateInkStrokeFromWpf(Stroke stroke)
        {
            if (stroke?.StylusPoints == null || stroke.StylusPoints.Count == 0)
                return null;

            var da = stroke.DrawingAttributes;
            var wda = new global::Windows.UI.Input.Inking.InkDrawingAttributes
            {
                PenTip = global::Windows.UI.Input.Inking.PenTipShape.Circle,
                Color = global::Windows.UI.Color.FromArgb(da.Color.A, da.Color.R, da.Color.G, da.Color.B),
                Size = new global::Windows.Foundation.Size((float)da.Width, (float)da.Height)
            };

            var builder = new global::Windows.UI.Input.Inking.InkStrokeBuilder();
            builder.SetDefaultDrawingAttributes(wda);

            var inkPoints = new List<global::Windows.UI.Input.Inking.InkPoint>(stroke.StylusPoints.Count);
            foreach (StylusPoint sp in stroke.StylusPoints)
            {
                var pi = sp.ToPoint();
                inkPoints.Add(new global::Windows.UI.Input.Inking.InkPoint(
                    new global::Windows.Foundation.Point((float)pi.X, (float)pi.Y), (float)sp.PressureFactor));
            }

            var transform = global::Windows.Foundation.Numerics.Matrix3x2.Identity;
            return builder.CreateStrokeFromInkPoints(inkPoints, transform);
        }

        private static global::Windows.UI.Input.Inking.Analysis.InkAnalysisInkDrawing FindPrimaryDrawing(
            WinRtInkAnalyzer analyzer)
        {
            global::Windows.UI.Input.Inking.Analysis.InkAnalysisInkDrawing best = null;
            double bestArea = -1;
            Visit(analyzer.AnalysisRoot);
            return best;

            void Visit(global::Windows.UI.Input.Inking.Analysis.IInkAnalysisNode node)
            {
                if (node == null) return;

                if (node is global::Windows.UI.Input.Inking.Analysis.InkAnalysisInkDrawing d &&
                    d.DrawingKind != global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Drawing)
                {
                    double area = EstimateDrawingArea(d);
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = d;
                    }
                }

                foreach (var child in node.Children)
                    Visit(child);
            }
        }

        private static double EstimateDrawingArea(global::Windows.UI.Input.Inking.Analysis.InkAnalysisInkDrawing drawing)
        {
            var pts = CopyWinRtPoints(drawing);
            BoundsFromPoints(pts, out double w, out double h);
            return w * h;
        }

        private static global::Windows.Foundation.Point[] CopyWinRtPoints(
            global::Windows.UI.Input.Inking.Analysis.InkAnalysisInkDrawing drawing)
        {
            var src = drawing?.Points;
            if (src == null)
                return Array.Empty<global::Windows.Foundation.Point>();

            var n = src.Count;
            if (n == 0)
                return Array.Empty<global::Windows.Foundation.Point>();

            var arr = new global::Windows.Foundation.Point[n];
            for (var i = 0; i < n; i++)
                arr[i] = src[i];
            return arr;
        }

        private static void BoundsFromPoints(
            System.Collections.Generic.IReadOnlyList<global::Windows.Foundation.Point> points,
            out double w,
            out double h)
        {
            if (points == null || points.Count == 0)
            {
                w = h = 0;
                return;
            }

            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                minX = Math.Min(minX, pt.X);
                maxX = Math.Max(maxX, pt.X);
                minY = Math.Min(minY, pt.Y);
                maxY = Math.Max(maxY, pt.Y);
            }

            w = Math.Max(0, maxX - minX);
            h = Math.Max(0, maxY - minY);
        }

        private static PointCollection ToWpfPointCollection(
            System.Collections.Generic.IReadOnlyList<global::Windows.Foundation.Point> points)
        {
            var hot = new PointCollection();
            if (points == null) return hot;
            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                hot.Add(new SysPoint(pt.X, pt.Y));
            }

            return hot;
        }

        private static string MapDrawingKindToShapeName(
            global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind kind)
        {
            switch (kind)
            {
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Circle:
                    return "Circle";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Ellipse:
                    return "Ellipse";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Triangle:
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.IsoscelesTriangle:
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.EquilateralTriangle:
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.RightTriangle:
                    return "Triangle";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Rectangle:
                    return "Rectangle";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Square:
                    return "Square";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Diamond:
                    return "Diamond";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Trapezoid:
                    return "Trapezoid";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Parallelogram:
                    return "Parallelogram";
                case global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Quadrilateral:
                    return "Quadrilateral";
                default:
                    return kind == global::Windows.UI.Input.Inking.Analysis.InkAnalysisDrawingKind.Drawing
                        ? "Drawing"
                        : kind.ToString();
            }
        }
    }
}
