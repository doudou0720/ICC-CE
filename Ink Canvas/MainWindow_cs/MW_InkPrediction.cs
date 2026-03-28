using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas
{
    /// <summary>
    /// 墨迹预测：书写过程中根据速度与选项外推一小段预览线，减轻感知延迟。
    /// </summary>
    public partial class MainWindow
    {
        private bool _inkPredictionStrokeActive;
        private bool _inkPredictionHasSample;
        private bool _inkPredictionHasVelocity;
        private Point _inkPredictionLastPos;
        private int _inkPredictionLastTime;
        private double _inkPredictionVx;
        private double _inkPredictionVy;

        private void SyncInkStrokePredictionLeadComboVisibility()
        {
            try
            {
                bool on = Settings?.Canvas != null && Settings.Canvas.EnableInkStrokePrediction;
                var v = on ? Visibility.Visible : Visibility.Collapsed;
                if (ComboBoxInkStrokePredictionLead != null)
                    ComboBoxInkStrokePredictionLead.Visibility = v;
                if (BoardComboBoxInkStrokePredictionLead != null)
                    BoardComboBoxInkStrokePredictionLead.Visibility = v;
            }
            catch
            {
                // ignore
            }
        }

        private void ClearInkPredictionOverlay()
        {
            try
            {
                if (InkPredictionPolyline == null) return;
                InkPredictionPolyline.Visibility = Visibility.Collapsed;
                InkPredictionPolyline.Points.Clear();
            }
            catch
            {
                // ignore
            }
        }

        private void BeginInkPredictionStrokeIfNeeded()
        {
            try
            {
                if (Settings?.Canvas == null || !Settings.Canvas.EnableInkStrokePrediction)
                {
                    _inkPredictionStrokeActive = false;
                    return;
                }

                _inkPredictionStrokeActive = inkCanvas != null
                    && inkCanvas.EditingMode == InkCanvasEditingMode.Ink
                    && penType != 1
                    && !_isBoardBrushMode;
                _inkPredictionHasSample = false;
                _inkPredictionHasVelocity = false;
                ClearInkPredictionOverlay();
            }
            catch
            {
                _inkPredictionStrokeActive = false;
            }
        }

        private void EndInkPredictionStroke()
        {
            _inkPredictionStrokeActive = false;
            _inkPredictionHasSample = false;
            _inkPredictionHasVelocity = false;
            ClearInkPredictionOverlay();
        }

        private double GetInkPredictionLeadMs()
        {
            int mode = Settings?.Canvas?.InkStrokePredictionLeadMode ?? 0;
            if (mode == 1) return 25.0;
            if (mode == 2) return 50.0;

            double speed = Math.Sqrt(_inkPredictionVx * _inkPredictionVx + _inkPredictionVy * _inkPredictionVy);
            double norm = Math.Min(1.0, speed / 2600.0);
            double lead = 16.0 + norm * 34.0;
            return Math.Max(14.0, Math.Min(52.0, lead));
        }

        private double GetInkPredictionMaxDistance(double leadMs)
        {
            double baseD = Math.Max(4.0, Settings?.Canvas?.InkStrokePredictionMaxDistance ?? 18.0);
            int mode = Settings?.Canvas?.InkStrokePredictionLeadMode ?? 0;
            if (mode != 0)
                return Math.Max(6.0, Math.Min(42.0, baseD * (leadMs / 24.0)));

            double speed = Math.Sqrt(_inkPredictionVx * _inkPredictionVx + _inkPredictionVy * _inkPredictionVy);
            double norm = Math.Min(1.0, speed / 2200.0);
            return Math.Max(6.0, Math.Min(48.0, baseD + norm * baseD * 0.9));
        }

        private void inkCanvas_PreviewStylusMove(object sender, StylusEventArgs e)
        {
            try
            {
                if (Settings?.Canvas == null || !Settings.Canvas.EnableInkStrokePrediction) return;
                if (inkCanvas == null || InkPredictionPolyline == null) return;
                if (!_inkPredictionStrokeActive || penType == 1) return;
                if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink) return;

                if (e.InAir)
                {
                    ClearInkPredictionOverlay();
                    return;
                }

                var pos = e.GetPosition(inkCanvas);
                UpdateInkPredictionCore(pos, e.Timestamp);
            }
            catch
            {
                // ignore
            }
        }

        private void inkCanvas_LostStylusCapture(object sender, StylusEventArgs e)
        {
            EndInkPredictionStroke();
        }

        private void inkCanvas_PreviewMouseMoveForPrediction(object sender, MouseEventArgs e)
        {
            try
            {
                if (Settings?.Canvas == null || !Settings.Canvas.EnableInkStrokePrediction) return;
                if (inkCanvas == null || InkPredictionPolyline == null) return;
                if (!_inkPredictionStrokeActive || penType == 1) return;
                if (inkCanvas.EditingMode != InkCanvasEditingMode.Ink) return;
                if (e.LeftButton != MouseButtonState.Pressed) return;
                if (e.StylusDevice != null) return;

                var pos = e.GetPosition(inkCanvas);
                UpdateInkPredictionCore(pos, Environment.TickCount & int.MaxValue);
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateInkPredictionCore(Point pos, int timestamp)
        {
            if (InkPredictionPolyline == null || Settings?.Canvas == null) return;

            if (!_inkPredictionHasSample)
            {
                _inkPredictionLastPos = pos;
                _inkPredictionLastTime = timestamp;
                _inkPredictionHasSample = true;
                return;
            }

            double dtMs = timestamp - _inkPredictionLastTime;
            if (dtMs <= 0 || dtMs > 120) dtMs = 16;

            double vx = (pos.X - _inkPredictionLastPos.X) / dtMs * 1000.0;
            double vy = (pos.Y - _inkPredictionLastPos.Y) / dtMs * 1000.0;

            const double velocitySmooth = 0.62;
            if (!_inkPredictionHasVelocity)
            {
                _inkPredictionVx = vx;
                _inkPredictionVy = vy;
                _inkPredictionHasVelocity = true;
            }
            else
            {
                _inkPredictionVx = velocitySmooth * _inkPredictionVx + (1.0 - velocitySmooth) * vx;
                _inkPredictionVy = velocitySmooth * _inkPredictionVy + (1.0 - velocitySmooth) * vy;
            }

            double leadMs = GetInkPredictionLeadMs();
            double predX = pos.X + _inkPredictionVx * (leadMs / 1000.0);
            double predY = pos.Y + _inkPredictionVy * (leadMs / 1000.0);

            double maxDist = GetInkPredictionMaxDistance(leadMs);
            double dx = predX - pos.X;
            double dy = predY - pos.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > maxDist && len > 1e-6)
            {
                double s = maxDist / len;
                predX = pos.X + dx * s;
                predY = pos.Y + dy * s;
            }

            _inkPredictionLastPos = pos;
            _inkPredictionLastTime = timestamp;

            var da = inkCanvas.DefaultDrawingAttributes;
            var c = da.Color;
            InkPredictionPolyline.Stroke = new SolidColorBrush(Color.FromArgb(110, c.R, c.G, c.B));
            InkPredictionPolyline.StrokeThickness = Math.Max(1.0, da.Width * 0.42);

            InkPredictionPolyline.Points.Clear();
            InkPredictionPolyline.Points.Add(pos);
            InkPredictionPolyline.Points.Add(new Point(predX, predY));
            InkPredictionPolyline.Visibility = Visibility.Visible;
        }
    }
}
