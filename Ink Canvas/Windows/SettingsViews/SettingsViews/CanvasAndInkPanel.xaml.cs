using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using iNKORE.UI.WPF.Helpers;

namespace Ink_Canvas.Windows.SettingsViews
{
    /// <summary>
    /// CanvasAndInkPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CanvasAndInkPanel : UserControl
    {
        public CanvasAndInkPanel()
        {
            InitializeComponent();
        }

        public event EventHandler<RoutedEventArgs> IsTopBarNeedShadowEffect;
        public event EventHandler<RoutedEventArgs> IsTopBarNeedNoShadowEffect;

        private void ScrollViewerEx_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.VerticalOffset >= 10)
            {
                IsTopBarNeedShadowEffect?.Invoke(this, new RoutedEventArgs());
            }
            else
            {
                IsTopBarNeedNoShadowEffect?.Invoke(this, new RoutedEventArgs());
            }
        }
    }
}

