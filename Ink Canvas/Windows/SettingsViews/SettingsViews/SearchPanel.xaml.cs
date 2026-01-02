using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.International.Converters.PinYinConverter;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class SearchPanel : UserControl
    {
        public event EventHandler<string> NavigateToItem;
        public event EventHandler CloseSearch;

        private ObservableCollection<SearchResultItem> _exactMatches = new ObservableCollection<SearchResultItem>();
        private ObservableCollection<SearchResultItem> _fuzzyMatches = new ObservableCollection<SearchResultItem>();
        private ObservableCollection<SearchResultItem> _relatedItems = new ObservableCollection<SearchResultItem>();

        private List<SettingItem> _allSettings = new List<SettingItem>();

        public SearchPanel()
        {
            InitializeComponent();
            InitializeSettings();
            ExactMatchItemsControl.ItemsSource = _exactMatches;
            FuzzyMatchItemsControl.ItemsSource = _fuzzyMatches;
            RelatedItemsControl.ItemsSource = _relatedItems;
        }

        private void InitializeSettings()
        {
            _allSettings = new List<SettingItem>
            {
                new SettingItem { Title = "启动时行为", Category = "启动时行为", ItemName = "StartupItem", Type = SettingItemType.Category },
                new SettingItem { Title = "画板和墨迹", Category = "画板和墨迹", ItemName = "CanvasAndInkItem", Type = SettingItemType.Category },
                new SettingItem { Title = "手势操作", Category = "手势操作", ItemName = "GesturesItem", Type = SettingItemType.Category },
                new SettingItem { Title = "墨迹纠正", Category = "墨迹纠正", ItemName = "InkRecognitionItem", Type = SettingItemType.Category },
                new SettingItem { Title = "个性化设置", Category = "个性化设置", ItemName = "ThemeItem", Type = SettingItemType.Category },
                new SettingItem { Title = "快捷键设置", Category = "快捷键设置", ItemName = "ShortcutsItem", Type = SettingItemType.Category },
                new SettingItem { Title = "崩溃处理", Category = "崩溃处理", ItemName = "CrashActionItem", Type = SettingItemType.Category },
                new SettingItem { Title = "PowerPoint 支持", Category = "PowerPoint 支持", ItemName = "PowerPointItem", Type = SettingItemType.Category },
                new SettingItem { Title = "自动化行为", Category = "自动化行为", ItemName = "AutomationItem", Type = SettingItemType.Category },
                new SettingItem { Title = "随机点名", Category = "随机点名", ItemName = "LuckyRandomItem", Type = SettingItemType.Category },
                new SettingItem { Title = "存储空间", Category = "存储空间", ItemName = "StorageItem", Type = SettingItemType.Category },
                new SettingItem { Title = "截图和屏幕捕获", Category = "截图和屏幕捕获", ItemName = "SnapshotItem", Type = SettingItemType.Category },
                new SettingItem { Title = "高级选项", Category = "高级选项", ItemName = "AdvancedItem", Type = SettingItemType.Category },
                new SettingItem { Title = "关于 InkCanvasForClass", Category = "关于", ItemName = "AboutItem", Type = SettingItemType.Category },
            };
        }

        public void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ClearResults();
                return;
            }

            _exactMatches.Clear();
            _fuzzyMatches.Clear();
            _relatedItems.Clear();

            var searchLower = searchText.ToLower();
            var exactMatchSet = new HashSet<string>();
            var fuzzyMatchSet = new HashSet<string>();
            foreach (var setting in _allSettings)
            {
                var titleLower = setting.Title.ToLower();
                var categoryLower = setting.Category.ToLower();
                if (titleLower.Contains(searchLower) || categoryLower.Contains(searchLower))
                {
                    if (!exactMatchSet.Contains(setting.ItemName))
                    {
                        _exactMatches.Add(new SearchResultItem
                        {
                            Title = setting.Title,
                            Category = setting.Category,
                            ItemName = setting.ItemName,
                            Type = setting.Type,
                            MatchType = MatchType.Exact
                        });
                        exactMatchSet.Add(setting.ItemName);
                    }
                }
                else if (ContainsPinyinMatch(setting.Title, searchText) || ContainsPinyinMatch(setting.Category, searchText))
                {
                    if (!exactMatchSet.Contains(setting.ItemName))
                    {
                        _exactMatches.Add(new SearchResultItem
                        {
                            Title = setting.Title,
                            Category = setting.Category,
                            ItemName = setting.ItemName,
                            Type = setting.Type,
                            MatchType = MatchType.Pinyin
                        });
                        exactMatchSet.Add(setting.ItemName);
                    }
                }
            }
            foreach (var setting in _allSettings)
            {
                if (exactMatchSet.Contains(setting.ItemName))
                    continue;

                var searchableText = $"{setting.Title} {setting.Category} {setting.Description}".ToLower();
                if (FuzzyMatch(searchableText, searchLower))
                {
                    if (!fuzzyMatchSet.Contains(setting.ItemName))
                    {
                        _fuzzyMatches.Add(new SearchResultItem
                        {
                            Title = setting.Title,
                            Category = setting.Category,
                            ItemName = setting.ItemName,
                            Type = setting.Type,
                            MatchType = MatchType.Fuzzy
                        });
                        fuzzyMatchSet.Add(setting.ItemName);
                    }
                }
            }
            var allMatched = new HashSet<string>(exactMatchSet.Concat(fuzzyMatchSet));
            foreach (var setting in _allSettings)
            {
                if (!allMatched.Contains(setting.ItemName))
                {
                    if (IsRelated(setting, searchText))
                    {
                        _relatedItems.Add(new SearchResultItem
                        {
                            Title = setting.Title,
                            Category = setting.Category,
                            ItemName = setting.ItemName,
                            Type = setting.Type,
                            MatchType = MatchType.Related
                        });
                    }
                }
            }

            UpdateResultsVisibility();
        }

        private bool ContainsPinyinMatch(string text, string search)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(search))
                return false;

            try
            {
                var searchLower = search.ToLower();
                var pinyinInitials = GetPinyinInitials(text);
                var pinyinFull = GetPinyinFull(text);
                if (pinyinInitials.ToLower().Contains(searchLower) || 
                    pinyinFull.ToLower().Contains(searchLower))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
            
            return false;
        }

        private string GetPinyinInitials(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (IsChinese(c))
                {
                    try
                    {
                        var chineseChar = new ChineseChar(c);
                        if (chineseChar.PinyinCount > 0)
                        {
                            var pinyin = chineseChar.Pinyins[0];
                            if (!string.IsNullOrEmpty(pinyin) && pinyin.Length > 0)
                            {
                                var firstChar = Regex.Replace(pinyin, @"\d", "")[0];
                                sb.Append(firstChar);
                            }
                        }
                    }
                    catch
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private string GetPinyinFull(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (IsChinese(c))
                {
                    try
                    {
                        var chineseChar = new ChineseChar(c);
                        if (chineseChar.PinyinCount > 0)
                        {
                            var pinyin = chineseChar.Pinyins[0];
                            pinyin = Regex.Replace(pinyin, @"\d", "");
                            sb.Append(pinyin);
                        }
                    }
                    catch
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private bool IsChinese(char c)
        {
            return c >= 0x4e00 && c <= 0x9fbb;
        }

        private bool FuzzyMatch(string text, string search)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(search))
                return false;
            int searchIndex = 0;
            foreach (char c in text)
            {
                if (searchIndex < search.Length && c == search[searchIndex])
                {
                    searchIndex++;
                    if (searchIndex == search.Length)
                        return true;
                }
            }
            return false;
        }

        private bool IsRelated(SettingItem setting, string search)
        {
            return false; 
        }

        private void UpdateResultsVisibility()
        {
            ExactMatchPanel.Visibility = _exactMatches.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            FuzzyMatchPanel.Visibility = _fuzzyMatches.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            RelatedItemsPanel.Visibility = _relatedItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            bool hasResults = _exactMatches.Count > 0 || _fuzzyMatches.Count > 0 || _relatedItems.Count > 0;
            NoResultsText.Visibility = hasResults ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ClearResults()
        {
            _exactMatches.Clear();
            _fuzzyMatches.Clear();
            _relatedItems.Clear();
            UpdateResultsVisibility();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseSearch?.Invoke(this, EventArgs.Empty);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                PerformSearch(textBox.Text);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            textBox?.SelectAll();
        }

        private void CloseSearchButton_Click(object sender, MouseButtonEventArgs e)
        {
            CloseSearch?.Invoke(this, EventArgs.Empty);
        }

        private void SearchResultItem_Click(object sender, MouseButtonEventArgs e)
        {
            var border = sender as Border;
            if (border?.Tag is SearchResultItem item)
            {
                NavigateToItem?.Invoke(this, item.ItemName);
            }
        }

        public void FocusSearchBox()
        {
            SearchTextBox.Focus();
        }

        public void SetSearchText(string text)
        {
            SearchTextBox.Text = text;
            PerformSearch(text);
        }
        public void ApplyTheme()
        {
            try
            {
                bool isDarkTheme = ThemeHelper.IsDarkTheme;
                if (SearchPanelMainGrid != null)
                {
                    SearchPanelMainGrid.Background = ThemeHelper.GetBackgroundPrimaryBrush();
                }
                if (SearchPanelTopBarBorder != null)
                {
                    SearchPanelTopBarBorder.Background = ThemeHelper.GetBackgroundPrimaryBrush();
                }
                if (SearchInputBorder != null)
                {
                    SearchInputBorder.Background = ThemeHelper.GetTextBoxBackgroundBrush();
                    SearchInputBorder.BorderBrush = ThemeHelper.GetTextBoxBorderBrush();
                }
                if (SearchTextBox != null)
                {
                    SearchTextBox.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (ExactMatchTitle != null)
                {
                    ExactMatchTitle.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (FuzzyMatchTitle != null)
                {
                    FuzzyMatchTitle.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (RelatedItemsTitle != null)
                {
                    RelatedItemsTitle.Foreground = ThemeHelper.GetTextPrimaryBrush();
                }
                if (NoResultsText != null)
                {
                    NoResultsText.Foreground = ThemeHelper.GetTextSecondaryBrush();
                }
                UpdateSearchIconColor(isDarkTheme);
                ThemeHelper.ApplyThemeToControl(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchPanel 应用主题时出�? {ex.Message}");
            }
        }
        private void UpdateSearchIconColor(bool isDarkTheme)
        {
            try
            {
                Color iconColor = isDarkTheme 
                    ? Color.FromRgb(243, 243, 243) 
                    : Color.FromRgb(34, 34, 34);   
                if (SearchInputBorder != null)
                {
                    var image = FindVisualChild<Image>(SearchInputBorder);
                    if (image != null && image.Source is DrawingImage drawingImage)
                    {
                        if (drawingImage.Drawing is DrawingGroup drawingGroup)
                        {
                            var clonedDrawing = CloneDrawingGroup(drawingGroup, iconColor);
                            image.Source = new DrawingImage { Drawing = clonedDrawing };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新搜索图标颜色时出�? {ex.Message}");
            }
        }
        private DrawingGroup CloneDrawingGroup(DrawingGroup source, Color newColor)
        {
            var cloned = new DrawingGroup();
            cloned.ClipGeometry = source.ClipGeometry?.Clone();
            cloned.Opacity = source.Opacity;
            cloned.Transform = source.Transform?.Clone();

            foreach (var drawing in source.Children)
            {
                if (drawing is GeometryDrawing geometryDrawing)
                {
                    var clonedGeometry = geometryDrawing.Geometry?.Clone();
                    var clonedBrush = CloneBrush(geometryDrawing.Brush, newColor);
                    var clonedPen = geometryDrawing.Pen != null 
                        ? ClonePen(geometryDrawing.Pen, newColor) 
                        : null;

                    cloned.Children.Add(new GeometryDrawing(clonedBrush, clonedPen, clonedGeometry));
                }
                else if (drawing is DrawingGroup subGroup)
                {
                    cloned.Children.Add(CloneDrawingGroup(subGroup, newColor));
                }
                else
                {
                    cloned.Children.Add(drawing);
                }
            }

            return cloned;
        }
        private Brush CloneBrush(Brush source, Color newColor)
        {
            if (source is SolidColorBrush solidBrush)
            {
                var originalColor = solidBrush.Color;
                if (originalColor.R == 34 && originalColor.G == 34 && originalColor.B == 34) 
                {
                    return new SolidColorBrush(newColor) { Opacity = solidBrush.Opacity };
                }
                else if (originalColor.A > 0 && originalColor != Colors.Transparent && 
                         originalColor.R < 50 && originalColor.G < 50 && originalColor.B < 50) 
                {
                    return new SolidColorBrush(newColor) { Opacity = solidBrush.Opacity };
                }
                return new SolidColorBrush(originalColor) { Opacity = solidBrush.Opacity };
            }
            return source?.Clone();
        }
        private Pen ClonePen(Pen source, Color newColor)
        {
            var clonedBrush = CloneBrush(source.Brush, newColor);
            return new Pen(clonedBrush, source.Thickness)
            {
                StartLineCap = source.StartLineCap,
                EndLineCap = source.EndLineCap,
                LineJoin = source.LineJoin,
                MiterLimit = source.MiterLimit
            };
        }
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }

    public class SettingItem
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; } = "";
        public SettingItemType Type { get; set; }
    }

    public class SearchResultItem
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string ItemName { get; set; }
        public SettingItemType Type { get; set; }
        public MatchType MatchType { get; set; }
        public string Description { get; set; } = "";
    }

    public enum SettingItemType
    {
        Category,
        Setting
    }

    public enum MatchType
    {
        Exact,
        Pinyin,
        Fuzzy,
        Related
    }
}

