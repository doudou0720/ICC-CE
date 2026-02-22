using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas.Helpers
{
    internal class AnimationsHelper
    {
        public static void ShowWithFadeIn(UIElement element, double duration = 0.15)
        {
            if (element.Visibility == Visibility.Visible) return;

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var sb = new Storyboard();

            // 渐变动画
            var fadeInAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

            sb.Children.Add(fadeInAnimation);

            element.Visibility = Visibility.Visible;

            sb.Begin((FrameworkElement)element);
        }

        /// <summary>
        /// 使指定元素从底部向上滑入并同时淡入，从而将其显示出来。
        /// </summary>
        /// <param name="element">要执行动画的 UIElement；如果为 null 将抛出异常。若元素已为 Visible 则不执行任何操作。</param>
        /// <param name="duration">动画持续时间（秒）。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="element"/> 为 null 时抛出。</exception>
        public static void ShowWithSlideFromBottomAndFade(UIElement element, double duration = 0.15)
        {
            try
            {
                if (element.Visibility == Visibility.Visible) return;

                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                var sb = new Storyboard();

                // 渐变动画
                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                fadeInAnimation.EasingFunction = new CubicEase();

                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

                // 滑动动画
                var slideAnimation = new DoubleAnimation
                {
                    From = element.RenderTransform.Value.OffsetY + 10, // 滑动距离
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

                slideAnimation.EasingFunction = new CubicEase();

                sb.Children.Add(fadeInAnimation);
                sb.Children.Add(slideAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransform = new TranslateTransform();

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 使指定的 UIElement 从左侧滑入并同时淡入显示。
        /// </summary>
        /// <param name="element">要显示并执行动画的 UIElement；不能为空。</param>
        /// <param name="duration">动画持续时间（秒）。</param>
        /// <exception cref="System.ArgumentNullException">当 <paramref name="element"/> 为 null 时抛出。</exception>
        public static void ShowWithSlideFromLeftAndFade(UIElement element, double duration = 0.25)
        {
            try
            {
                if (element.Visibility == Visibility.Visible) return;

                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                var sb = new Storyboard();

                // 渐变动画
                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));

                // 滑动动画
                var slideAnimation = new DoubleAnimation
                {
                    From = element.RenderTransform.Value.OffsetX - 20, // 滑动距离
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

                sb.Children.Add(fadeInAnimation);
                sb.Children.Add(slideAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransform = new TranslateTransform();

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 将指定元素以左侧为基准从无到有按比例放大并显示（水平与垂直缩放从 0 过渡到 1）。
        /// </summary>
        /// <param name="element">要执行缩放并显示的 UIElement；不能为空。</param>
        /// <param name="duration">动画持续时长，单位为秒（默认 0.2）。</param>
        /// <remarks>
        /// 如果元素当前已可见则不会执行动画。方法内部会捕获异常并将其写入调试输出（不会向上传播）。
        /// </remarks>
        public static void ShowWithScaleFromLeft(UIElement element, double duration = 0.2)
        {
            try
            {
                if (element.Visibility == Visibility.Visible) return;

                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                var sb = new Storyboard();

                // 水平方向的缩放动画
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

                // 垂直方向的缩放动画
                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                scaleYAnimation.EasingFunction = new CubicEase();
                scaleXAnimation.EasingFunction = new CubicEase();
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

                sb.Children.Add(scaleXAnimation);
                sb.Children.Add(scaleYAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransformOrigin = new Point(0, 0.5); // 左侧中心点为基准
                element.RenderTransform = new ScaleTransform(0, 0);

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 从右侧以水平和垂直缩放动画显示指定的 UI 元素。
        /// </summary>
        /// <param name="element">要显示的 UIElement（不会返回值；方法会将其 Visibility 设置为 Visible 并播放缩放动画）。</param>
        /// <param name="duration">动画持续时间，单位为秒。</param>
        public static void ShowWithScaleFromRight(UIElement element, double duration = 0.2)
        {
            try
            {
                if (element.Visibility == Visibility.Visible) return;

                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                var sb = new Storyboard();

                // 水平方向的缩放动画
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

                // 垂直方向的缩放动画
                var scaleYAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

                scaleYAnimation.EasingFunction = new CubicEase();
                scaleXAnimation.EasingFunction = new CubicEase();

                sb.Children.Add(scaleXAnimation);
                sb.Children.Add(scaleYAnimation);

                element.Visibility = Visibility.Visible;
                element.RenderTransformOrigin = new Point(1, 0.5); // 右侧中心点为基准
                element.RenderTransform = new ScaleTransform(0, 0);

                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 对指定元素执行向下滑动并淡出动画，动画完成后将元素的 Visibility 设置为 Collapsed。
        /// </summary>
        /// <param name="element">要执行动画的目标元素；若为 null 则方法内部会抛出并被捕获。</param>
        /// <param name="duration">动画持续时长（秒），默认为 0.15 秒。</param>
        public static void HideWithSlideAndFade(UIElement element, double duration = 0.15)
        {
            try
            {
                if (element.Visibility == Visibility.Collapsed) return;

                if (element == null)
                    throw new ArgumentNullException(nameof(element));

                var sb = new Storyboard();

                // 渐变动画
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(duration)
                };
                fadeOutAnimation.EasingFunction = new CubicEase();
                Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

                // 滑动动画
                var slideAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = element.RenderTransform.Value.OffsetY + 10, // 滑动距离
                    Duration = TimeSpan.FromSeconds(duration)
                };
                slideAnimation.EasingFunction = new CubicEase();

                Storyboard.SetTargetProperty(slideAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.Y)"));

                sb.Children.Add(fadeOutAnimation);
                sb.Children.Add(slideAnimation);

                sb.Completed += (s, e) =>
                {
                    element.Visibility = Visibility.Collapsed;
                };

                element.RenderTransform = new TranslateTransform();
                sb.Begin((FrameworkElement)element);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 以淡出动画隐藏指定元素，并在动画完成后将其 Visibility 设置为 Collapsed。
        /// </summary>
        /// <param name="element">要隐藏的 UIElement。</param>
        /// <param name="duration">动画持续时间（秒）。</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="element"/> 为 null 时抛出。</exception>
        public static void HideWithFadeOut(UIElement element, double duration = 0.15)
        {
            if (element.Visibility == Visibility.Collapsed) return;

            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var sb = new Storyboard();

            // 渐变动画
            var fadeOutAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(duration)
            };
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));

            sb.Children.Add(fadeOutAnimation);

            sb.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
            };

            sb.Begin((FrameworkElement)element);
        }

    }
}