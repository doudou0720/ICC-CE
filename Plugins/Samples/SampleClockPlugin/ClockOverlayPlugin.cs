using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using InkCanvasForClass.PluginSdk;

namespace InkCanvasForClass.SamplePlugins
{
    /// <summary>
    /// 示例插件：在当前 <see cref="InkCanvas"/> 右上角叠加显示实时时钟（不拦截笔触命中）。
    /// </summary>
    public sealed class ClockOverlayPlugin : InkCanvasPluginBase
    {
        public override string Id => "inkcanvas.sample.clock-overlay";

        public override string Name => "画布时钟示例";

        public override string Description => "在画布右上角显示当前时间（HH:mm:ss）";

        public override Version Version => new Version(1, 0, 0);

        public override string Author => "ICC CE Sample";

        private Border _host;
        private TextBlock _timeText;
        private DispatcherTimer _timer;
        private System.Windows.Controls.InkCanvas _canvas;
        private SizeChangedEventHandler _sizeHandler;

        public override void Start()
        {
            base.Start();

            var mw = Context?.MainWindow;
            var canvas = Context?.CurrentCanvas as System.Windows.Controls.InkCanvas;
            if (mw == null || canvas == null)
            {
                return;
            }

            mw.Dispatcher.Invoke(() =>
            {
                _canvas = canvas;
                _host = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(170, 32, 32, 36)),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 8, 12, 8),
                    IsHitTestVisible = false
                };
                _timeText = new TextBlock
                {
                    FontSize = 24,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI")
                };
                _host.Child = _timeText;
                Panel.SetZIndex(_host, 10050);

                _canvas.Children.Add(_host);
                _sizeHandler = (s, e) => Reposition();
                _canvas.SizeChanged += _sizeHandler;
                Reposition();

                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _timer.Tick += (_, __) => Tick();
                _timer.Start();
                Tick();
            });
        }

        private void Tick()
        {
            if (_timeText != null)
            {
                _timeText.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }

        private void Reposition()
        {
            if (_canvas == null || _host == null)
            {
                return;
            }

            _host.UpdateLayout();
            var left = Math.Max(8, _canvas.ActualWidth - _host.ActualWidth - 20);
            InkCanvas.SetLeft(_host, left);
            InkCanvas.SetTop(_host, 20);
        }

        public override void Stop()
        {
            var mw = Context?.MainWindow;
            if (mw != null && (_canvas != null || _host != null))
            {
                mw.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_timer != null)
                        {
                            _timer.Stop();
                            _timer = null;
                        }

                        if (_canvas != null && _sizeHandler != null)
                        {
                            _canvas.SizeChanged -= _sizeHandler;
                            _sizeHandler = null;
                        }

                        if (_canvas != null && _host != null && _canvas.Children.Contains(_host))
                        {
                            _canvas.Children.Remove(_host);
                        }
                    }
                    catch
                    {
                        // 避免卸载时异常影响宿主
                    }
                    finally
                    {
                        _host = null;
                        _timeText = null;
                        _canvas = null;
                    }
                });
            }

            base.Stop();
        }
    }
}
