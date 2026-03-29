using Ink_Canvas.Windows.SettingsViews2.Pages;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;

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
                { "Page1", typeof(Page1) },
                { "Page2", typeof(Page2) },
                { "Iconography", typeof(Iconography) },
                { "Typography", typeof(Typography) },
                { "Theme", typeof(Theme) },
                { "Colors", typeof(Colors) },
                { "Fonts", typeof(Fonts) }
            };

            // 默认选中第一个项目
            if (NavigationViewControl.MenuItems.Count > 0)
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            }
        }

        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 暂时导航到 Page1 作为设置页示例
                NavigateToPage("Page1");
            }
            else if (args.SelectedItem is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem item)
            {
                // 检查是否是子导航项（没有子菜单）
                if (item.MenuItems.Count == 0)
                {
                    // 如果是子导航项，直接导航
                    var tag = item.Tag as string;
                    if (!string.IsNullOrEmpty(tag))
                    {
                        NavigateToPage(tag);
                    }
                }
                // 父级导航项（有子菜单）会自动展开，不需要额外处理
            }
        }

        private void NavigateToPage(string pageTag)
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
            var query = args.QueryText.ToLower();
            var allItems = new List<object>();
            foreach (var item in NavigationViewControl.MenuItems) allItems.Add(item);
            foreach (var item in NavigationViewControl.FooterMenuItems) allItems.Add(item);

            foreach (var item in allItems)
            {
                if (item is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem navItem)
                {
                    var content = navItem.Content?.ToString().ToLower();
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
                var query = sender.Text.ToLower();
                var suggestions = new List<string>();

                var allItems = new List<object>();
                foreach (var item in NavigationViewControl.MenuItems) allItems.Add(item);
                foreach (var item in NavigationViewControl.FooterMenuItems) allItems.Add(item);

                foreach (var item in allItems)
                {
                    if (item is iNKORE.UI.WPF.Modern.Controls.NavigationViewItem navItem)
                    {
                        var content = navItem.Content?.ToString();
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
            var pageType = e.SourcePageType;
            foreach (var kvp in _pageTypes)
            {
                if (kvp.Value == pageType)
                {
                    // 找到对应的NavigationViewItem
                    var item = FindNavigationViewItemByTag(kvp.Key);
                    if (item != null)
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
            // 遍历所有菜单项
            foreach (var item in NavigationViewControl.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    if (navItem.Tag as string == tag)
                    {
                        return navItem;
                    }
                    // 检查子菜单项
                    foreach (var child in navItem.MenuItems)
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
            return null;
        }
    }
}
