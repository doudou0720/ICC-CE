using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// Automatically shrinks text to fit available width.
    /// Supports TextBlock and Label.
    /// Only shrinks, never enlarges above MaxFontSize.
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
            if (!(d is FrameworkElement fe)) return;
            if (!(fe is TextBlock) && !(fe is Label)) return;

            if ((bool)e.NewValue)
            {
                fe.SizeChanged += Element_OnSizeChanged;
                fe.Loaded += Element_OnLoaded;
                fe.Unloaded += Element_OnUnloaded;
                TryHookContentChanged(fe, true);

                fe.Dispatcher.BeginInvoke(new Action(() => TryAdjust(fe)), DispatcherPriority.Loaded);
            }
            else
            {
                fe.SizeChanged -= Element_OnSizeChanged;
                fe.Loaded -= Element_OnLoaded;
                fe.Unloaded -= Element_OnUnloaded;
                TryHookContentChanged(fe, false);
            }
        }

        private static void OnSizingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe && GetIsEnabled(fe))
            {
                fe.Dispatcher.BeginInvoke(new Action(() => TryAdjust(fe)), DispatcherPriority.Loaded);
            }
        }

        private static void Element_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                fe.Dispatcher.BeginInvoke(new Action(() => TryAdjust(fe)), DispatcherPriority.Loaded);
            }
        }

        private static void Element_OnUnloaded(object sender, RoutedEventArgs e)
        {
            // No extra cleanup required here.
        }

        private static void Element_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is FrameworkElement fe) TryAdjust(fe);
        }

        private static void Element_OnTextChanged(object sender, EventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                fe.Dispatcher.BeginInvoke(new Action(() => TryAdjust(fe)), DispatcherPriority.Loaded);
            }
        }

        private static void TryHookContentChanged(FrameworkElement fe, bool add)
        {
            try
            {
                DependencyPropertyDescriptor dpd = null;
                if (fe is TextBlock)
                {
                    dpd = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                }
                else if (fe is Label)
                {
                    dpd = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
                }

                if (dpd == null) return;
                if (add) dpd.AddValueChanged(fe, Element_OnTextChanged);
                else dpd.RemoveValueChanged(fe, Element_OnTextChanged);
            }
            catch
            {
                // Ignore descriptor issues in rare runtime cases.
            }
        }

        private static void TryAdjust(FrameworkElement fe)
        {
            if (fe == null) return;
            if (!GetIsEnabled(fe)) return;
            if (GetIsAdjusting(fe)) return;

            var text = GetElementText(fe);
            if (string.IsNullOrEmpty(text)) return;

            var availableWidth = GetAvailableWidth(fe);
            if (double.IsNaN(availableWidth) || availableWidth <= 1) return;

            var min = GetMinFontSize(fe);
            if (double.IsNaN(min) || min <= 0) min = 6d;

            var step = GetStep(fe);
            if (double.IsNaN(step) || step < 0.1) step = 0.5d;

            var current = GetElementFontSize(fe);
            if (double.IsNaN(current) || current <= 0) return;

            var max = GetMaxFontSize(fe);
            if (double.IsNaN(max) || max <= 0) max = current;

            var startFont = Math.Min(current, max);
            if (startFont < min) startFont = min;

            SetIsAdjusting(fe, true);
            try
            {
                var desiredAtMax = MeasureTextWidth(fe, text, max);
                if (desiredAtMax > 0 && desiredAtMax <= availableWidth + 0.5)
                {
                    if (Math.Abs(current - max) > 0.01) SetElementFontSize(fe, max);
                    return;
                }

                var font = startFont;
                var desired = MeasureTextWidth(fe, text, font);
                if (desired <= 0) return;

                while (font > min && desired > availableWidth + 0.5)
                {
                    font = Math.Max(min, font - step);
                    desired = MeasureTextWidth(fe, text, font);
                    if (desired <= 0) break;
                }

                if (!double.IsNaN(font) && font > 0 && Math.Abs(current - font) > 0.01)
                {
                    SetElementFontSize(fe, font);
                }
            }
            finally
            {
                SetIsAdjusting(fe, false);
            }
        }

        private static string GetElementText(FrameworkElement fe)
        {
            if (fe is TextBlock tb) return tb.Text;
            if (fe is Label label) return label.Content as string ?? label.Content?.ToString();
            return null;
        }

        private static double GetElementFontSize(FrameworkElement fe)
        {
            if (fe is TextBlock tb) return tb.FontSize;
            if (fe is Label label) return label.FontSize;
            return double.NaN;
        }

        private static void SetElementFontSize(FrameworkElement fe, double value)
        {
            if (fe is TextBlock tb) tb.FontSize = value;
            else if (fe is Label label) label.FontSize = value;
        }

        private static double GetAvailableWidth(FrameworkElement fe)
        {
            double width = double.PositiveInfinity;

            if (fe.ActualWidth > 1) width = Math.Min(width, fe.ActualWidth);

            if (fe.Parent is FrameworkElement parent && parent.ActualWidth > 1)
            {
                var parentWidth = parent.ActualWidth - fe.Margin.Left - fe.Margin.Right;
                if (parentWidth > 1) width = Math.Min(width, parentWidth);
            }

            if (double.IsInfinity(width) || double.IsNaN(width) || width <= 1) return -1;

            // Keep width as inner text area.
            if (fe is Control control)
            {
                width -= control.Padding.Left + control.Padding.Right;
                width -= control.BorderThickness.Left + control.BorderThickness.Right;
            }
            else if (fe is Border border)
            {
                width -= border.Padding.Left + border.Padding.Right;
                width -= border.BorderThickness.Left + border.BorderThickness.Right;
            }

            return width;
        }

        private static double MeasureTextWidth(FrameworkElement fe, string text, double fontSize)
        {
            try
            {
                var dpi = VisualTreeHelper.GetDpi(fe);
                var culture = CultureInfo.CurrentUICulture;

                if (fe.Language != null)
                {
                    try
                    {
                        culture = fe.Language.GetEquivalentCulture();
                    }
                    catch
                    {
                    }
                }

                var fontFamily = SystemFonts.MessageFontFamily;
                var fontStyle = FontStyles.Normal;
                var fontWeight = FontWeights.Normal;
                var fontStretch = FontStretches.Normal;
                Brush foreground = Brushes.Black;
                var flowDirection = FlowDirection.LeftToRight;

                if (fe is TextBlock tb)
                {
                    fontFamily = tb.FontFamily;
                    fontStyle = tb.FontStyle;
                    fontWeight = tb.FontWeight;
                    fontStretch = tb.FontStretch;
                    foreground = tb.Foreground;
                    flowDirection = tb.FlowDirection;
                }
                else if (fe is Label label)
                {
                    fontFamily = label.FontFamily;
                    fontStyle = label.FontStyle;
                    fontWeight = label.FontWeight;
                    fontStretch = label.FontStretch;
                    foreground = label.Foreground;
                    flowDirection = label.FlowDirection;
                }

                var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
                var formatted = new FormattedText(
                    text,
                    culture,
                    flowDirection,
                    typeface,
                    fontSize,
                    foreground,
                    dpi.PixelsPerDip);

                return formatted.WidthIncludingTrailingWhitespace;
            }
            catch
            {
                return -1;
            }
        }
    }
}
