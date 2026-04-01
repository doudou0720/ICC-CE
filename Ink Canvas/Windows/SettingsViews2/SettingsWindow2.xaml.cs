using Ink_Canvas.Windows.SettingsViews2.Pages;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Interop;
using System.Windows.Input;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using MessageBox = System.Windows.MessageBox;
using Screen = System.Windows.Forms.Screen;

namespace Ink_Canvas.Windows.SettingsViews2
{
    // 插件设置页面契约接口，所有插件必须实现此接口即可自动接入
    public interface IPluginSettingsPage
    {
        string PageTag { get; }          // 页面唯一标识，不可与内置页面重复
        string PageTitle { get; }        // 导航菜单显示的标题
        string PageIconCode { get; }     // Segoe MDL2 Assets 图标字符，例："\xE713"（设置图标）
        Type PageType { get; }           // 插件设置页面的类型（继承自Page）
        bool IsFooterItem { get; }       // 是否放在导航底部菜单
    }

    public partial class SettingsWindow2 : Window
    {
        private readonly Dictionary<string, Type> _pageTypes;
        private readonly Dictionary<string, object> _pages = new Dictionary<string, object>();
        
        [ImportMany(typeof(IPluginSettingsPage))]
        private IEnumerable<IPluginSettingsPage> _pluginPages; // 自动导入所有插件页面

        public SettingsWindow2()
        {
            InitializeComponent();

            // 初始化内置页面映射
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

            // 加载插件页面
            LoadPluginSettingsPages();
            // 初始化导航菜单（内置+插件）
            InitializeNavigationMenu();

            // 默认选中第一个菜单项
            if (NavigationViewControl.MenuItems.Count > 0)
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems[0];
            }

            // 窗口生命周期事件注册
            this.Loaded += (sender, e) =>
            {
                SetMaxSizeAndCenter();
                RegisterDpiChangedListener();
            };

            // 窗口关闭时释放资源
            this.Closed += (sender, e) =>
            {
                UnregisterDpiChangedListener();
                _pages.Clear();
                _pageTypes.Clear();
            };

            // 修复触摸屏操作后鼠标指针消失的问题
            FixTouchScreenCursorIssue();
        }

        #region 修复触摸屏鼠标指针消失问题
        private void FixTouchScreenCursorIssue()
        {
            // 触摸结束时强制显示鼠标指针
            this.TouchUp += (s, e) =>
            {
                ShowCursor(true);
            };

            // 鼠标进入窗口时确保指针可见
            this.MouseEnter += (s, e) =>
            {
                ShowCursor(true);
            };

            // 窗口激活时确保指针可见
            this.Activated += (s, e) =>
            {
                ShowCursor(true);
            };
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);
        #endregion

        #region 插件化动态设置页面核心逻辑
        private void LoadPluginSettingsPages()
        {
            try
            {
                // 扫描程序目录下Plugins文件夹中的插件dll
                var pluginCatalog = new DirectoryCatalog("./Plugins", "*.dll");
                var container = new CompositionContainer(pluginCatalog);
                container.ComposeParts(this);

                // 将插件页面注册到页面映射字典
                foreach (var pluginPage in _pluginPages)
                {
                    if (!_pageTypes.ContainsKey(pluginPage.PageTag))
                    {
                        _pageTypes.Add(pluginPage.PageTag, pluginPage.PageType);
                    }
                }
            }
            catch (Exception ex)
            {
                // 插件加载失败不影响主程序运行，仅输出调试日志
                System.Diagnostics.Debug.WriteLine($"插件加载失败: {ex.Message}");
            }
        }

        private void InitializeNavigationMenu()
        {
            // 自动将插件页面添加到导航菜单
            foreach (var pluginPage in _pluginPages)
            {
                var navItem = new NavigationViewItem
                {
                    Tag = pluginPage.PageTag,
                    Content = pluginPage.PageTitle,
                    Icon = new FontIcon { Glyph = pluginPage.PageIconCode }
                };

                if (pluginPage.IsFooterItem)
                {
                    NavigationViewControl.FooterMenuItems.Add(navItem);
                }
                else
                {
                    NavigationViewControl.MenuItems.Add(navItem);
                }
            }
        }
        #endregion

        #region 高DPI/多屏自适应窗口控制
        private void SetMaxSizeAndCenter()
        {
            if (!this.IsLoaded) return;

            // 1. 获取窗口当前所在屏幕（而非固定主屏，彻底解决多屏问题）
            var windowHandle = new WindowInteropHelper(this).Handle;
            var currentScreen = Screen.FromHandle(windowHandle);
            var workingArea = currentScreen.WorkingArea;
            var screenBounds = currentScreen.Bounds;

            // 2. 获取当前窗口的DPI缩放因子
            var source = PresentationSource.FromVisual(this);
            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;

            if (source?.CompositionTarget != null)
            {
                dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
            }

            // 3. 物理像素 → WPF设备无关像素(DIP)转换
            double workAreaWidthDip = workingArea.Width / dpiScaleX;
            double workAreaHeightDip = workingArea.Height / dpiScaleY;
            double screenLeftDip = screenBounds.Left / dpiScaleX;
            double screenTopDip = screenBounds.Top / dpiScaleY;

            // 4. 设置窗口最大尺寸（保留你原有的边距）
            this.MaxWidth = workAreaWidthDip;
            this.MaxHeight = workAreaHeightDip;

            // 5. 窗口在当前屏幕居中（解决副屏居中跑偏问题）
            this.Left = screenLeftDip + (workAreaWidthDip - this.ActualWidth) / 2;
            this.Top = screenTopDip + (workAreaHeightDip - this.ActualHeight) / 2;
        }

        #region DPI/系统缩放变化监听
        private HwndSource _hwndSource;
        private void RegisterDpiChangedListener()
        {
            _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            _hwndSource?.AddHook(DpiChangedWndProc);
        }

        private void UnregisterDpiChangedListener()
        {
            _hwndSource?.RemoveHook(DpiChangedWndProc);
            _hwndSource = null;
        }

        private IntPtr DpiChangedWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DPICHANGED = 0x02E0;
            // 系统DPI/缩放变化时自动重新计算窗口参数
            if (msg == WM_DPICHANGED)
            {
                SetMaxSizeAndCenter();
                handled = true;
            }
            return IntPtr.Zero;
        }
        #endregion
        #endregion

        #region 导航逻辑优化（含页面缓存）
        private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            // 处理自带的设置项导航
            if (args.IsSettingsSelected)
            {
                NavigateToPage("Settings");
                NavigationViewControl.Header = "设置";
                return;
            }

            // 处理普通导航项
            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string tag = selectedItem.Tag as string;
                if (!string.IsNullOrEmpty(tag) && _pageTypes.ContainsKey(tag))
                {
                    // 避免重复导航到当前页面
                    if (rootFrame.SourcePageType != _pageTypes[tag])
                    {
                        NavigateToPage(tag);
                    }
                    NavigationViewControl.Header = selectedItem.Content;
                }
            }
        }

        public void NavigateToPage(string pageTag)
        {
            if (!_pageTypes.TryGetValue(pageTag, out Type pageType)) return;

            try
            {
                // 页面缓存：已创建的页面直接复用，保留状态，避免重复初始化
                if (!_pages.TryGetValue(pageTag, out var cachedPage))
                {
                    cachedPage = Activator.CreateInstance(pageType);
                    _pages.Add(pageTag, cachedPage);
                }

                // 导航到缓存页面
                rootFrame.Navigate(cachedPage.GetType(), cachedPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导航到页面时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNavigationViewBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (rootFrame.CanGoBack) rootFrame.GoBack();
        }

        private void OnRootFrameNavigated(object sender, NavigationEventArgs e)
        {
            // 导航后同步导航项选中状态
            Type currentPageType = rootFrame.SourcePageType;

            // 处理设置项的选中状态
            if (currentPageType == typeof(SettingsPage))
            {
                NavigationViewControl.SelectedItem = NavigationViewControl.SettingsItem;
                NavigationViewControl.Header = "设置";
                return;
            }

            // 同步其他页面的选中状态
            foreach (var kvp in _pageTypes)
            {
                if (kvp.Value == currentPageType)
                {
                    var targetItem = FindNavigationViewItemByTag(kvp.Key);
                    if (targetItem != null && NavigationViewControl.SelectedItem != targetItem)
                    {
                        NavigationViewControl.SelectedItem = targetItem;
                        NavigationViewControl.Header = targetItem.Content;
                    }
                    break;
                }
            }
        }

        private NavigationViewItem FindNavigationViewItemByTag(string tag)
        {
            // 遍历主菜单
            foreach (var item in NavigationViewControl.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    if (navItem.Tag as string == tag)
                        return navItem;

                    // 遍历子菜单，自动展开父项
                    foreach (var childItem in navItem.MenuItems)
                    {
                        if (childItem is NavigationViewItem childNavItem && childNavItem.Tag as string == tag)
                        {
                            navItem.IsExpanded = true;
                            return childNavItem;
                        }
                    }
                }
            }

            // 遍历底部菜单
            foreach (var item in NavigationViewControl.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag as string == tag)
                {
                    return navItem;
                }
            }

            return null;
        }
        #endregion

        #region 搜索框逻辑优化
        private void OnControlsSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.QueryText)) return;

            string query = args.QueryText.Trim().ToLower();
            var allNavItems = GetAllNavigationItems();

            var targetItem = allNavItems.FirstOrDefault(item =>
                item.Content?.ToString().ToLower().Contains(query) == true);

            if (targetItem != null)
            {
                NavigationViewControl.SelectedItem = targetItem;
            }
        }

        private void OnControlsSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

            string query = sender.Text.Trim().ToLower();
            var suggestions = new List<string>();

            if (!string.IsNullOrEmpty(query))
            {
                var allNavItems = GetAllNavigationItems();
                suggestions = allNavItems
                    .Where(item => item.Content?.ToString().ToLower().Contains(query) == true)
                    .Select(item => item.Content.ToString())
                    .ToList();
            }

            sender.ItemsSource = suggestions;
        }

        // 统一获取所有导航项（主菜单+子菜单+底部菜单+插件页面）
        private List<NavigationViewItem> GetAllNavigationItems()
        {
            var items = new List<NavigationViewItem>();

            // 主菜单+子菜单
            foreach (var item in NavigationViewControl.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    items.Add(navItem);
                    foreach (var child in navItem.MenuItems)
                    {
                        if (child is NavigationViewItem childNavItem)
                            items.Add(childNavItem);
                    }
                }
            }

            // 底部菜单
            foreach (var item in NavigationViewControl.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem)
                    items.Add(navItem);
            }

            return items;
        }
        #endregion
    }
}
