using Ink_Canvas.Windows.SettingsViews2.Pages;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Forms;

namespace Ink_Canvas.Windows.SettingsViews2
{
    public partial class SettingsWindow2 : Window
    {
        private Dictionary<string, Type> _pageTypes;
        private Dictionary<string, object> _pages = new Dictionary<string, object>();

        public SettingsWindow2()
        {
            InitializeComponent();

            // 初始化页面类型映射
            _pageTypes = new Dictionary<string, Type>
            {
                { "Basic", typeof(Basic) },
                { "Page2", typeof(Page2) },
                { "Design", typeof(Design) },
                { "Appearance", typeof(Appearance) },
                { "Iconography", typeof(Iconography) },
                { "Typography", typeof(Typography) },
                { "Theme", typeof(Theme) },
                { "Colors", typeof(Colors) },
                { "Fonts", typeof(Fonts) },
                { "NewSettingStartup", typeof(NewSettingStartup) },
                { "About", typeof(About) },
                { "Settings", typeof(SettingsPage) }
            };

            // 默认选中第一个项目
            if (NavigationViewControl.MenuItems.Count > 0)
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            }

            // 窗口加载完成后设置最大尺寸，确保能正确获取 DPI 缩放因子
            this.Loaded += (sender, e) =>
            {
                SetMaxWindowSize();
            };
        }

        private void SetMaxWindowSize()
        {
            // 设置最大高度和宽度为工作区的高度和宽度，分别减去 40 和 10，并考虑 DPI 缩放
            var workingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            
            // 获取 DPI 缩放因子
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            var source = System.Windows.PresentationSource.FromVisual(this);
            if (source != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }
            
            // 先将物理像素转换为逻辑像素，再减去边距
            this.MaxWidth = (workingArea.Width / dpiScaleX) - 10;
            this.MaxHeight = (workingArea.Height / dpiScaleY) - 40;
            
            // 确保窗口居中显示
            this.Left = (workingArea.Width / dpiScaleX - this.Width) / 2;
            this.Top = (workingArea.Height / dpiScaleY - this.Height) / 2;
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 导航到设置页面
                NavigateToPage("Settings");
            }
            else if (args.SelectedItem is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem item)
            {
                // 检查是否有Tag，如果有则导航
                string tag = item.Tag as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    // 检查当前页面是否已经是目标页面，避免重复导航
                    if (rootFrame.SourcePageType != _pageTypes[tag])
                    {
                        NavigateToPage(tag);
                    }
                }
                // 父级导航项（有子菜单）会自动展开，不需要额外处理
            }
        }

        public void NavigateToPage(string pageTag)
        {
            if (_pageTypes.TryGetValue(pageTag, out Type pageType))
            {
                try
                {
                    // 使用Type参数导航，这样可以正确记录导航历史
                    rootFrame.Navigate(pageType);
                    // 更新标题
                    if (NavigationViewControl.SelectedItem is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem selectedItem)
                    {
                        NavigationViewControl.Header = selectedItem.Content;
                    }
                }
                catch (Exception ex)
                {
                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"导航到页面时出错: {ex.Message}", "错误");
                }
            }
        }

        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string query = args.QueryText.ToLower();
            List<object> allItems = new List<object>();
            foreach (object item in NavigationViewControl.MenuItems) allItems.Add(item);
            foreach (object item in NavigationViewControl.FooterMenuItems) allItems.Add(item);

            foreach (object item in allItems)
            {
                if (item is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem navItem)
                {
                    string content = navItem.Content?.ToString().ToLower();
                    if (content != null && content.Contains(query))
                    {
                        NavigationViewControl.SelectedItem = navItem;
                        break;
                    }
                }
            }
        }

        private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string query = sender.Text.ToLower();
                List<string> suggestions = new List<string>();

                List<object> allItems = new List<object>();
                foreach (object item in NavigationViewControl.MenuItems) allItems.Add(item);
                foreach (object item in NavigationViewControl.FooterMenuItems) allItems.Add(item);

                foreach (object item in allItems)
                {
                    if (item is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem navItem)
                    {
                        string content = navItem.Content?.ToString();
                        if (content != null && content.ToLower().Contains(query))
                        {
                            suggestions.Add(content);
                        }
                    }
                }

                sender.ItemsSource = suggestions;
            }
        }

        private void OnNavigationViewBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
        {
            // 更新NavigationView的选中状态
            Type pageType = rootFrame.SourcePageType;
            foreach (KeyValuePair<string, Type> kvp in _pageTypes)
            {
                if (kvp.Value == pageType)
                {
                    // 找到对应的NavigationViewItem
                    NavigationViewItem item = FindNavigationViewItemByTag(kvp.Key);
                    if (item != null && NavigationViewControl.SelectedItem != item)
                    {
                        NavigationViewControl.SelectedItem = item;
                        NavigationViewControl.Header = item.Content;
                    }
                    break;
                }
            }
        }

        private NavigationViewItem FindNavigationViewItemByTag(string tag)
        {
            // 遍历所有主菜单项
            foreach (object item in NavigationViewControl.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    if (navItem.Tag as string == tag)
                    {
                        return navItem;
                    }
                    // 检查子菜单项
                    foreach (object child in navItem.MenuItems)
                    {
                        if (child is NavigationViewItem childNavItem)
                        {
                            if (childNavItem.Tag as string == tag)
                            {
                                // 展开父项
                                navItem.IsExpanded = true;
                                return childNavItem;
                            }
                        }
                    }
                }
            }
            // 遍历页脚菜单项
            foreach (object item in NavigationViewControl.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    if (navItem.Tag as string == tag)
                    {
                        return navItem;
                    }
                }
            }
            return null;
        }
    }
}
