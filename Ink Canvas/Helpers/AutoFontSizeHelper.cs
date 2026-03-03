using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Markup;
using System.Windows.Media.TextFormatting;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 让 TextBlock 在可用宽度不足时自动缩小字号（只缩小不放大），用于避免英文等长文本被截断。
    /// </summary>
    public static class AutoFontSizeHelper
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(AutoFontSizeHelper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty MinFontSizeProperty =
            DependencyProperty.RegisterAttached(
                "MinFontSize",
                typeof(double),
                typeof(AutoFontSizeHelper),
                new PropertyMetadata(6d, OnSizingPropertyChanged));

        public static void SetMinFontSize(DependencyObject element, double value) => element.SetValue(MinFontSizeProperty, value);
        public static double GetMinFontSize(DependencyObject element) => (double)element.GetValue(MinFontSizeProperty);

        public static readonly DependencyProperty MaxFontSizeProperty =
            DependencyProperty.RegisterAttached(
                "MaxFontSize",
                typeof(double),
                typeof(AutoFontSizeHelper),
                new PropertyMetadata(double.NaN, OnSizingPropertyChanged));

        public static void SetMaxFontSize(DependencyObject element, double value) => element.SetValue(MaxFontSizeProperty, value);
        public static double GetMaxFontSize(DependencyObject element) => (double)element.GetValue(MaxFontSizeProperty);

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.RegisterAttached(
                "Step",
                typeof(double),
                typeof(AutoFontSizeHelper),
                new PropertyMetadata(0.5d, OnSizingPropertyChanged));

        public static void SetStep(DependencyObject element, double value) => element.SetValue(StepProperty, value);
        public static double GetStep(DependencyObject element) => (double)element.GetValue(StepProperty);

        private static readonly DependencyProperty IsAdjustingProperty =
            DependencyProperty.RegisterAttached(
                "IsAdjusting",
                typeof(bool),
                typeof(AutoFontSizeHelper),
                new PropertyMetadata(false));

        private static void SetIsAdjusting(DependencyObject element, bool value) => element.SetValue(IsAdjustingProperty, value);
        private static bool GetIsAdjusting(DependencyObject element) => (bool)element.GetValue(IsAdjustingProperty);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = d as TextBlock;
            if (tb == null) return;

            if ((bool)e.NewValue)
            {
                tb.SizeChanged += TextBlock_OnSizeChanged;
                tb.Loaded += TextBlock_OnLoaded;
                tb.Unloaded += TextBlock_OnUnloaded;

                try
                {
                    var dpd = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                    dpd?.AddValueChanged(tb, TextBlock_OnTextChanged);
                }
                catch
                {
                    // 忽略：极端情况下 descriptor 可能不可用
                }

                // 让第一次布局完成后再做一次调整（避免 ActualWidth=0）
                tb.Dispatcher.BeginInvoke(new Action(() => TryAdjust(tb)), DispatcherPriority.Loaded);
            }
            else
            {
                tb.SizeChanged -= TextBlock_OnSizeChanged;
                tb.Loaded -= TextBlock_OnLoaded;
                tb.Unloaded -= TextBlock_OnUnloaded;

                try
                {
                    var dpd = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                    dpd?.RemoveValueChanged(tb, TextBlock_OnTextChanged);
                }
                catch
                {
                }
            }
        }

        private static void OnSizingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock tb && GetIsEnabled(tb))
            {
                tb.Dispatcher.BeginInvoke(new Action(() => TryAdjust(tb)), DispatcherPriority.Loaded);
            }
        }

        private static void TextBlock_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb) tb.Dispatcher.BeginInvoke(new Action(() => TryAdjust(tb)), DispatcherPriority.Loaded);
        }

        private static void TextBlock_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 这里不做额外处理；事件解绑由 IsEnabled 关闭或对象销毁处理
        }

        private static void TextBlock_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is TextBlock tb) TryAdjust(tb);
        }

        private static void TextBlock_OnTextChanged(object sender, EventArgs e)
        {
            if (sender is TextBlock tb) tb.Dispatcher.BeginInvoke(new Action(() => TryAdjust(tb)), DispatcherPriority.Loaded);
        }

        private static void TryAdjust(TextBlock tb)
        {
            if (tb == null) return;
            if (!GetIsEnabled(tb)) return;
            if (GetIsAdjusting(tb)) return;

            // 没有可用宽度时跳过
            var availableWidth = tb.ActualWidth;
            if (double.IsNaN(availableWidth) || availableWidth <= 1) return;

            // 文本为空时不需要调整
            var text = tb.Text;
            if (string.IsNullOrEmpty(text)) return;

            var min = GetMinFontSize(tb);
            if (double.IsNaN(min) || min <= 0) min = 6d;

            var step = GetStep(tb);
            if (double.IsNaN(step) || step <= 0.01) step = 0.5d;

            var max = GetMaxFontSize(tb);
            if (double.IsNaN(max) || max <= 0)
            {
                max = tb.FontSize;
            }

            if (double.IsNaN(max) || max <= 0) return;

            // 只做“缩小不放大”
            var startFont = Math.Min(tb.FontSize, max);
            if (startFont < min) startFont = min;

            SetIsAdjusting(tb, true);
            try
            {
                // 如果当前已合适，直接回到 max（但不超过原本 fontSize），避免之前缩小后再变短不恢复
                // 注意：恢复也只在不超过 max 的范围内
                var desiredAtMax = MeasureTextWidth(tb, text, max);
                if (desiredAtMax > 0 && desiredAtMax <= availableWidth + 0.5)
                {
                    if (tb.FontSize != max) tb.FontSize = max;
                    return;
                }

                double font = startFont;
                double desired = MeasureTextWidth(tb, text, font);
                if (desired <= 0) return;

                // 逐步减小直到适配或触底
                while (font > min && desired > availableWidth + 0.5)
                {
                    font = Math.Max(min, font - step);
                    desired = MeasureTextWidth(tb, text, font);
                    if (desired <= 0) break;
                }

                if (!double.IsNaN(font) && font > 0 && Math.Abs(tb.FontSize - font) > 0.01)
                {
                    tb.FontSize = font;
                }
            }
            finally
            {
                SetIsAdjusting(tb, false);
            }
        }

        private static double MeasureTextWidth(TextBlock tb, string text, double fontSize)
        {
            try
            {
                var dpi = VisualTreeHelper.GetDpi(tb);
                var culture = CultureInfo.CurrentUICulture;

                // 使用 TextBlock 自身的语言/流向
                if (tb.Language != null)
                {
                    try
                    {
                        culture = tb.Language.GetEquivalentCulture();
                    }
                    catch
                    {
                    }
                }

                var typeface = new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch);
                var formatted = new FormattedText(
                    text,
                    culture,
                    tb.FlowDirection,
                    typeface,
                    fontSize,
                    tb.Foreground,
                    dpi.PixelsPerDip);

                // 这里用包含尾随空白的宽度更接近实际布局
                return formatted.WidthIncludingTrailingWhitespace;
            }
            catch
            {
                return -1;
            }
        }
    }
}

