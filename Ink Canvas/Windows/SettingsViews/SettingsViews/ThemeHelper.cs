using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ink_Canvas;

namespace Ink_Canvas.Windows.SettingsViews
{
    public static class ThemeHelper
    {
        public static bool IsDarkTheme
        {
            get
            {
                if (MainWindow.Settings?.Appearance == null) return false;
                return MainWindow.Settings.Appearance.Theme == 1 ||
                       (MainWindow.Settings.Appearance.Theme == 2 && !IsSystemThemeLight());
            }
        }
        private static bool IsSystemThemeLight()
        {
            try
            {
                var registryKey = Microsoft.Win32.Registry.CurrentUser;
                var themeKey = registryKey.OpenSubKey("software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
                var keyValue = 0;
                if (themeKey != null) keyValue = (int)themeKey.GetValue("SystemUsesLightTheme");
                return keyValue == 1;
            }
            catch
            {
                return true; 
            }
        }
        public static Color TextPrimary => IsDarkTheme ? Color.FromRgb(243, 243, 243) : Color.FromRgb(0, 0, 0); 
        public static Color TextSecondary => IsDarkTheme ? Color.FromRgb(161, 161, 161) : Color.FromRgb(100, 100, 100); 
        public static Color TextTertiary => IsDarkTheme ? Color.FromRgb(161, 161, 161) : Color.FromRgb(100, 100, 100); 
        public static Color BackgroundPrimary => IsDarkTheme ? Color.FromRgb(32, 32, 32) : Color.FromRgb(255, 255, 255); 
        public static Color BackgroundSecondary => IsDarkTheme ? Color.FromRgb(25, 25, 25) : Color.FromRgb(248, 248, 248); 
        public static Color BackgroundTertiary => IsDarkTheme ? Color.FromRgb(43, 43, 43) : Color.FromRgb(240, 240, 240); 
        public static Color BorderPrimary => IsDarkTheme ? Color.FromRgb(62, 62, 62) : Color.FromRgb(200, 200, 200); 
        public static Color BorderSecondary => IsDarkTheme ? Color.FromRgb(70, 70, 70) : Color.FromRgb(180, 180, 180); 
        public static Color BorderTertiary => IsDarkTheme ? Color.FromRgb(70, 70, 70) : Color.FromRgb(180, 180, 180); 
        public static Color Separator => IsDarkTheme ? Color.FromRgb(62, 62, 62) : Color.FromRgb(220, 220, 220); 
        public static Color SelectedBackground => IsDarkTheme ? Color.FromRgb(62, 62, 62) : Color.FromRgb(230, 230, 230); 
        public static Color HoverBackground => IsDarkTheme ? Color.FromRgb(43, 43, 43) : Color.FromRgb(245, 245, 245); 
        public static Color ButtonBackground => IsDarkTheme ? Color.FromRgb(50, 50, 50) : Color.FromRgb(225, 225, 225); 
        public static Color ButtonHoverBackground => IsDarkTheme ? Color.FromRgb(55, 55, 55) : Color.FromRgb(210, 210, 210); 
        public static Color ToggleSwitchOnBackground => IsDarkTheme ? Color.FromRgb(0, 122, 204) : Color.FromRgb(53, 132, 228); 
        public static Color ToggleSwitchOffBackground => IsDarkTheme ? ButtonBackground : Color.FromRgb(225, 225, 225); 
        public static Color OptionButtonSelectedBackground => SelectedBackground; 
        public static Color OptionButtonUnselectedBackground => Colors.Transparent; 
        public static Color OptionButtonSelectedBorder => IsDarkTheme ? Color.FromRgb(100, 100, 100) : Color.FromRgb(160, 160, 160); 
        public static Color OptionButtonUnselectedBorder => IsDarkTheme ? Color.FromRgb(50, 50, 50) : Color.FromRgb(220, 220, 220); 
        public static Color TextBoxBackground => IsDarkTheme ? Color.FromRgb(43, 43, 43) : Color.FromRgb(255, 255, 255); 
        public static Color TextBoxBorder => IsDarkTheme ? Color.FromRgb(62, 62, 62) : Color.FromRgb(200, 200, 200); 
        public static Color ScrollBarTrack => IsDarkTheme ? Color.FromRgb(25, 25, 25) : Color.FromRgb(243, 243, 243); 
        public static Color ScrollBarThumb => IsDarkTheme ? Color.FromRgb(138, 138, 138) : Color.FromRgb(195, 195, 195); 
        public static Color ScrollBarThumbHover => IsDarkTheme ? Color.FromRgb(95, 95, 95) : Color.FromRgb(138, 138, 138); 
        public static SolidColorBrush GetTextPrimaryBrush() => new SolidColorBrush(TextPrimary);
        public static SolidColorBrush GetTextSecondaryBrush() => new SolidColorBrush(TextSecondary);
        public static SolidColorBrush GetTextTertiaryBrush() => new SolidColorBrush(TextTertiary);
        public static SolidColorBrush GetBackgroundPrimaryBrush() => new SolidColorBrush(BackgroundPrimary);
        public static SolidColorBrush GetBackgroundSecondaryBrush() => new SolidColorBrush(BackgroundSecondary);
        public static SolidColorBrush GetBackgroundTertiaryBrush() => new SolidColorBrush(BackgroundTertiary);
        public static SolidColorBrush GetBorderPrimaryBrush() => new SolidColorBrush(BorderPrimary);
        public static SolidColorBrush GetBorderSecondaryBrush() => new SolidColorBrush(BorderSecondary);
        public static SolidColorBrush GetBorderTertiaryBrush() => new SolidColorBrush(BorderTertiary);
        public static SolidColorBrush GetSeparatorBrush() => new SolidColorBrush(Separator);
        public static SolidColorBrush GetSelectedBackgroundBrush() => new SolidColorBrush(SelectedBackground);
        public static SolidColorBrush GetHoverBackgroundBrush() => new SolidColorBrush(HoverBackground);
        public static SolidColorBrush GetButtonBackgroundBrush() => new SolidColorBrush(ButtonBackground);
        public static SolidColorBrush GetButtonHoverBackgroundBrush() => new SolidColorBrush(ButtonHoverBackground);
        public static SolidColorBrush GetToggleSwitchOnBackgroundBrush() => new SolidColorBrush(ToggleSwitchOnBackground);
        public static SolidColorBrush GetToggleSwitchOffBackgroundBrush() => new SolidColorBrush(ToggleSwitchOffBackground);
        public static SolidColorBrush GetOptionButtonSelectedBackgroundBrush() => new SolidColorBrush(OptionButtonSelectedBackground);
        public static SolidColorBrush GetOptionButtonUnselectedBackgroundBrush() => new SolidColorBrush(OptionButtonUnselectedBackground);
        public static SolidColorBrush GetOptionButtonSelectedBorderBrush() => new SolidColorBrush(OptionButtonSelectedBorder);
        public static SolidColorBrush GetOptionButtonUnselectedBorderBrush() => new SolidColorBrush(OptionButtonUnselectedBorder);
        public static void SetOptionButtonSelectedState(Border button, bool isSelected)
        {
            if (button == null) return;
            
            if (isSelected)
            {
                button.Background = GetOptionButtonSelectedBackgroundBrush();
                button.BorderBrush = GetOptionButtonSelectedBorderBrush();
                button.BorderThickness = IsDarkTheme ? new Thickness(1) : new Thickness(1.5);
                var textBlock = button.Child as TextBlock;
                if (textBlock != null)
                {
                    textBlock.FontWeight = FontWeights.Bold;
                    textBlock.Foreground = GetTextPrimaryBrush();
                }
            }
            else
            {
                button.Background = GetOptionButtonUnselectedBackgroundBrush();
                button.BorderBrush = GetOptionButtonUnselectedBorderBrush();
                button.BorderThickness = IsDarkTheme ? new Thickness(1) : new Thickness(1);
                var textBlock = button.Child as TextBlock;
                if (textBlock != null)
                {
                    textBlock.FontWeight = FontWeights.Normal;
                    textBlock.Foreground = GetTextPrimaryBrush();
                }
            }
        }
        public static bool IsToggleSwitchOn(System.Windows.Media.Brush background)
        {
            if (background is SolidColorBrush brush)
            {
                var currentColor = brush.Color;
                var onColor = ToggleSwitchOnBackground;
                // 比较颜色是否匹配（允许小的误差）
                return Math.Abs(currentColor.R - onColor.R) < 5 &&
                       Math.Abs(currentColor.G - onColor.G) < 5 &&
                       Math.Abs(currentColor.B - onColor.B) < 5;
            }
            return false;
        }
        public static SolidColorBrush GetTextBoxBackgroundBrush() => new SolidColorBrush(TextBoxBackground);
        public static SolidColorBrush GetTextBoxBorderBrush() => new SolidColorBrush(TextBoxBorder);
        public static SolidColorBrush GetScrollBarTrackBrush() => new SolidColorBrush(ScrollBarTrack);
        public static SolidColorBrush GetScrollBarThumbBrush() => new SolidColorBrush(ScrollBarThumb);
        public static SolidColorBrush GetScrollBarThumbHoverBrush() => new SolidColorBrush(ScrollBarThumbHover);
        public static void UpdateTextBlockColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.TextBlock textBlock)
                {
                    var foreground = textBlock.Foreground as SolidColorBrush;
                    if (foreground != null)
                    {
                        var color = foreground.Color;
                        if (color.R == 46 && color.G == 52 && color.B == 54)
                        {
                            textBlock.Foreground = GetTextPrimaryBrush();
                        }
                        else if (color.R == 154 && color.G == 153 && color.B == 150) 
                        {
                            textBlock.Foreground = GetTextSecondaryBrush();
                        }
                        else if (color.R == 34 && color.G == 34 && color.B == 34) 
                        {
                            textBlock.Foreground = GetTextPrimaryBrush();
                        }
                    }
                }
                UpdateTextBlockColors(child);
            }
        }
        public static void UpdateBorderColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.Border border)
                {
                    var background = border.Background as SolidColorBrush;
                    if (background != null)
                    {
                        var color = background.Color;
                        if (color.R == 235 && color.G == 235 && color.B == 235)
                        {
                            border.Background = GetSeparatorBrush();
                        }
                        else if (color.R == 217 && color.G == 217 && color.B == 217) 
                        {
                            border.Background = GetButtonBackgroundBrush();
                        }
                        else if (color.R == 225 && color.G == 225 && color.B == 225 && 
                                 border.Width > 0 && border.Height > 0 && border.Width < 200 && border.Height < 100)
                        {
                            border.Background = GetButtonBackgroundBrush();
                        }
                        else if (color.R == 225 && color.G == 225 && color.B == 225)
                        {
                            border.Background = GetSeparatorBrush();
                        }
                        else if (color.R == 255 && color.G == 255 && color.B == 255) 
                        {
                            if (border.CornerRadius.TopLeft == 6 && border.CornerRadius.TopRight == 6 && 
                                border.CornerRadius.BottomLeft == 6 && border.CornerRadius.BottomRight == 6 &&
                                border.Padding.Left > 0 && border.Padding.Top > 0)
                            {
                                border.Background = new SolidColorBrush(Color.FromRgb(43, 43, 43));
                            }
                            else
                            {
                                border.Background = GetTextBoxBackgroundBrush();
                            }
                        }
                        else if (color.R == 250 && color.G == 250 && color.B == 250)
                        {
                            border.Background = GetBackgroundPrimaryBrush();
                        }
                    }
                    
                    var borderBrush = border.BorderBrush as SolidColorBrush;
                    if (borderBrush != null)
                    {
                        var color = borderBrush.Color;
                        if (color.R == 230 && color.G == 230 && color.B == 230) 
                        {
                            border.BorderBrush = GetBorderPrimaryBrush();
                        }
                        else if (color.R == 225 && color.G == 225 && color.B == 225) 
                        {
                            border.BorderBrush = GetBorderPrimaryBrush();
                        }
                        else if (color.R == 211 && color.G == 211 && color.B == 211) 
                        {
                            border.BorderBrush = GetBorderTertiaryBrush();
                        }
                    }
                }
                UpdateBorderColors(child);
            }
        }
        public static void UpdateLineColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Shapes.Line line)
                {
                    var stroke = line.Stroke as SolidColorBrush;
                    if (stroke != null)
                    {
                        var color = stroke.Color;
                        if (color.R == 211 && color.G == 211 && color.B == 211)
                        {
                            line.Stroke = GetSeparatorBrush();
                        }
                    }
                }
                UpdateLineColors(child);
            }
        }
        public static void UpdateInputControlsColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.TextBox textBox)
                {
                    var foreground = textBox.Foreground as SolidColorBrush;
                    if (foreground != null)
                    {
                        var color = foreground.Color;
                        if (color.R == 46 && color.G == 52 && color.B == 54) 
                        {
                            textBox.Foreground = GetTextPrimaryBrush();
                        }
                    }
                    var background = textBox.Background as SolidColorBrush;
                    if (background != null)
                    {
                        var color = background.Color;
                        if (color.R == 255 && color.G == 255 && color.B == 255) 
                        {
                            textBox.Background = IsDarkTheme ? GetBackgroundSecondaryBrush() : new SolidColorBrush(System.Windows.Media.Colors.White);
                        }
                    }
                }
                else if (child is System.Windows.Controls.ComboBox comboBox)
                {
                    comboBox.Foreground = GetTextPrimaryBrush();
                    comboBox.Background = GetTextBoxBackgroundBrush();
                    comboBox.BorderBrush = GetBorderPrimaryBrush();
                    
                    if (comboBox.Template != null)
                    {
                        var border = comboBox.Template.FindName("Border", comboBox) as System.Windows.Controls.Border;
                        if (border != null)
                        {
                            border.Background = GetTextBoxBackgroundBrush();
                            border.BorderBrush = GetBorderPrimaryBrush();
                        }
                        
                        var arrow = comboBox.Template.FindName("Arrow", comboBox) as System.Windows.Shapes.Path;
                        if (arrow != null)
                        {
                            arrow.Fill = GetTextSecondaryBrush();
                        }
                    }
                    
                    UpdateComboBoxItemColors(comboBox);
                }
                UpdateInputControlsColors(child);
            }
        }
        public static void UpdateButtonColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.Button button)
                {
                    var background = button.Background as SolidColorBrush;
                    if (background != null)
                    {
                        var color = background.Color;
                        if (button.Foreground is SolidColorBrush fgBrush && 
                            fgBrush.Color.R == 255 && fgBrush.Color.G == 255 && fgBrush.Color.B == 255)
                        {
                            button.Foreground = new SolidColorBrush(Colors.White); 
                        }
                        if (color.R == 255 && color.G == 255 && color.B == 255) 
                        {
                            button.Background = GetButtonBackgroundBrush();
                            button.Foreground = GetTextPrimaryBrush();
                        }
                        else if (color.A == 0 || color == System.Windows.Media.Colors.Transparent) 
                        {
                            button.Foreground = GetTextPrimaryBrush();
                        }
                    }
                    else
                    {
                    }
                }
                UpdateButtonColors(child);
            }
        }
        public static void UpdateImageIconColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.Image image && image.Source is DrawingImage drawingImage)
                {
                    if (drawingImage.Drawing is DrawingGroup drawingGroup)
                    {
                        Color iconColor = IsDarkTheme 
                            ? Color.FromRgb(243, 243, 243) 
                            : Color.FromRgb(34, 34, 34);   
                        
                        var clonedDrawing = CloneDrawingGroupForTheme(drawingGroup, iconColor);
                        image.Source = new DrawingImage { Drawing = clonedDrawing };
                    }
                }
                UpdateImageIconColors(child);
            }
        }
        private static DrawingGroup CloneDrawingGroupForTheme(DrawingGroup source, Color newColor)
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
                    var clonedBrush = CloneBrushForTheme(geometryDrawing.Brush, newColor);
                    var clonedPen = geometryDrawing.Pen != null 
                        ? ClonePenForTheme(geometryDrawing.Pen, newColor) 
                        : null;

                    cloned.Children.Add(new GeometryDrawing(clonedBrush, clonedPen, clonedGeometry));
                }
                else if (drawing is DrawingGroup subGroup)
                {
                    cloned.Children.Add(CloneDrawingGroupForTheme(subGroup, newColor));
                }
                else
                {
                    cloned.Children.Add(drawing);
                }
            }

            return cloned;
        }
        private static Brush CloneBrushForTheme(Brush source, Color newColor)
        {
            if (source is SolidColorBrush solidBrush)
            {
                var originalColor = solidBrush.Color;
                if (originalColor.A == 0 || originalColor == Colors.Transparent)
                {
                    return new SolidColorBrush(originalColor) { Opacity = solidBrush.Opacity };
                }
                if (originalColor.R < 100 && originalColor.G < 100 && originalColor.B < 100)
                {
                    return new SolidColorBrush(newColor) { Opacity = solidBrush.Opacity };
                }
                if (originalColor.R > 200 && originalColor.G > 200 && originalColor.B > 200)
                {
                    return new SolidColorBrush(newColor) { Opacity = solidBrush.Opacity };
                }
                return new SolidColorBrush(originalColor) { Opacity = solidBrush.Opacity };
            }
            return source?.Clone();
        }
        private static Pen ClonePenForTheme(Pen source, Color newColor)
        {
            var clonedBrush = CloneBrushForTheme(source.Brush, newColor);
            return new Pen(clonedBrush, source.Thickness)
            {
                StartLineCap = source.StartLineCap,
                EndLineCap = source.EndLineCap,
                LineJoin = source.LineJoin,
                MiterLimit = source.MiterLimit
            };
        }
        private static void UpdateComboBoxItemColors(System.Windows.Controls.ComboBox comboBox)
        {
            try
            {
                if (comboBox.Items.Count > 0)
                {
                    foreach (var item in comboBox.Items)
                    {
                        if (item is System.Windows.Controls.ComboBoxItem comboBoxItem)
                        {
                            comboBoxItem.Foreground = GetTextPrimaryBrush();
                            
                            comboBoxItem.Background = GetTextBoxBackgroundBrush();
                            
                            UpdateTextBlockColorsInComboBoxItem(comboBoxItem);
                            UpdateComboBoxItemTemplateBorder(comboBoxItem);
                            UpdateComboBoxItemStyleTriggers(comboBoxItem);
                            AttachComboBoxItemStateHandlers(comboBoxItem);
                        }
                    }
                }
            }
            catch
            {
            }
        }
        public static void UpdateComboBoxDropdownColors(System.Windows.Controls.ComboBox comboBox)
        {
            try
            {
                if (comboBox == null) return;
                System.Windows.Controls.Primitives.Popup popup = null;
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(comboBox); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(comboBox, i);
                    if (child is System.Windows.Controls.Primitives.Popup foundPopup)
                    {
                        popup = foundPopup;
                        break;
                    }
                    popup = FindPopupInVisualTree(child);
                    if (popup != null) break;
                }

                if (popup == null)
                {
                    if (comboBox.Template != null)
                    {
                        popup = comboBox.Template.FindName("Popup", comboBox) as System.Windows.Controls.Primitives.Popup;
                    }
                }

                if (popup != null && popup.Child is System.Windows.Controls.Border popupBorder)
                {
                    popupBorder.Background = GetTextBoxBackgroundBrush();
                    popupBorder.BorderBrush = GetBorderPrimaryBrush();
                    
                    if (comboBox.IsDropDownOpen)
                    {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                            new Action(() => UpdateComboBoxItemColorsInPopup(popupBorder)),
                            System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新 ComboBox 下拉菜单颜色时出�? {ex.Message}");
            }
        }
        private static System.Windows.Controls.Primitives.Popup FindPopupInVisualTree(System.Windows.DependencyObject parent)
        {
            if (parent == null) return null;

            if (parent is System.Windows.Controls.Primitives.Popup popup)
            {
                return popup;
            }

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                var found = FindPopupInVisualTree(child);
                if (found != null) return found;
            }

            return null;
        }
        public static void UpdateComboBoxPopupColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.Primitives.Popup popup)
                {
                    if (popup.Child is System.Windows.Controls.Border popupBorder)
                    {
                        var background = popupBorder.Background as SolidColorBrush;
                        if (background != null)
                        {
                            var color = background.Color;
                            if (color.R == 255 && color.G == 255 && color.B == 255) 
                            {
                                popupBorder.Background = GetTextBoxBackgroundBrush();
                            }
                        }
                        var borderBrush = popupBorder.BorderBrush as SolidColorBrush;
                        if (borderBrush != null)
                        {
                            var color = borderBrush.Color;
                            if (color.R == 230 && color.G == 230 && color.B == 230) 
                            {
                                popupBorder.BorderBrush = GetBorderPrimaryBrush();
                            }
                        }
                        UpdateComboBoxItemColorsInPopup(popupBorder);
                    }
                }
                UpdateComboBoxPopupColors(child);
            }
        }
        private static void UpdateComboBoxItemColorsInPopup(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.ComboBoxItem comboBoxItem)
                {
                    comboBoxItem.Foreground = GetTextPrimaryBrush();
                    comboBoxItem.Background = GetTextBoxBackgroundBrush();
                    
                    UpdateTextBlockColorsInComboBoxItem(comboBoxItem);
                    UpdateComboBoxItemTemplateBorder(comboBoxItem);
                    UpdateComboBoxItemStyleTriggers(comboBoxItem);
                    AttachComboBoxItemStateHandlers(comboBoxItem);
                }
                UpdateComboBoxItemColorsInPopup(child);
            }
        }
        
        private static void UpdateTextBlockColorsInComboBoxItem(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.TextBlock textBlock)
                {
                    var foreground = textBlock.Foreground as SolidColorBrush;
                    if (foreground != null)
                    {
                        var color = foreground.Color;
                        if (color.R == 46 && color.G == 52 && color.B == 54) 
                        {
                            textBlock.Foreground = GetTextPrimaryBrush();
                        }
                        else if (color.R == 135 && color.G == 135 && color.B == 135)
                        {
                            textBlock.Foreground = GetTextPrimaryBrush();
                        }
                    }
                    else
                    {
                        textBlock.Foreground = GetTextPrimaryBrush();
                    }
                }
                UpdateTextBlockColorsInComboBoxItem(child);
            }
        }
        private static void UpdateComboBoxItemTemplateBorder(System.Windows.Controls.ComboBoxItem comboBoxItem)
        {
            try
            {
                if (comboBoxItem.Template == null) return;
                var border = comboBoxItem.Template.FindName("Border", comboBoxItem) as System.Windows.Controls.Border;
                if (border != null)
                {
                    if (comboBoxItem.IsSelected)
                    {
                        border.Background = GetSelectedBackgroundBrush();
                        border.BorderBrush = GetBorderPrimaryBrush();
                        border.BorderThickness = new Thickness(1);
                    }
                    else if (comboBoxItem.IsMouseOver)
                    {
                        border.Background = GetHoverBackgroundBrush();
                        border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        border.BorderThickness = new Thickness(0);
                    }
                    else
                    {
                        var background = border.Background as SolidColorBrush;
                        if (background != null)
                        {
                            var color = background.Color;
                            if (color.R == 255 && color.G == 255 && color.B == 255) 
                            {
                                border.Background = GetTextBoxBackgroundBrush();
                            }
                            else if (color.R == 245 && color.G == 245 && color.B == 245) 
                            {
                                border.Background = GetHoverBackgroundBrush();
                            }
                            else if (color.R == 225 && color.G == 225 && color.B == 225) 
                            {
                                border.Background = GetSelectedBackgroundBrush();
                            }
                            else
                            {
                                border.Background = GetTextBoxBackgroundBrush();
                            }
                        }
                        else
                        {
                            border.Background = GetTextBoxBackgroundBrush();
                        }
                        border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        border.BorderThickness = new Thickness(0);
                    }
                }
            }
            catch
            {
            }
        }
        private static void AttachComboBoxItemStateHandlers(System.Windows.Controls.ComboBoxItem comboBoxItem)
        {
            try
            {
                comboBoxItem.MouseEnter -= ComboBoxItem_MouseEnter;
                comboBoxItem.MouseLeave -= ComboBoxItem_MouseLeave;
                comboBoxItem.Selected -= ComboBoxItem_Selected;
                comboBoxItem.Unselected -= ComboBoxItem_Unselected;
                comboBoxItem.MouseEnter += ComboBoxItem_MouseEnter;
                comboBoxItem.MouseLeave += ComboBoxItem_MouseLeave;
                comboBoxItem.Selected += ComboBoxItem_Selected;
                comboBoxItem.Unselected += ComboBoxItem_Unselected;
            }
            catch
            {
            }
        }
        private static void ComboBoxItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBoxItem comboBoxItem)
            {
                var border = comboBoxItem.Template?.FindName("Border", comboBoxItem) as System.Windows.Controls.Border;
                if (border != null)
                {
                    if (!comboBoxItem.IsSelected)
                    {
                        border.Background = GetHoverBackgroundBrush();
                        border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        border.BorderThickness = new Thickness(0);
                    }
                }
            }
        }
        private static void ComboBoxItem_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBoxItem comboBoxItem)
            {
                var border = comboBoxItem.Template?.FindName("Border", comboBoxItem) as System.Windows.Controls.Border;
                if (border != null)
                {
                    if (comboBoxItem.IsSelected)
                    {
                        border.Background = GetSelectedBackgroundBrush();
                        border.BorderBrush = GetBorderPrimaryBrush();
                        border.BorderThickness = new Thickness(1);
                    }
                    else
                    {
                        border.Background = GetTextBoxBackgroundBrush();
                        border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                        border.BorderThickness = new Thickness(0);
                    }
                }
            }
        }
        private static void ComboBoxItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBoxItem comboBoxItem)
            {
                var border = comboBoxItem.Template?.FindName("Border", comboBoxItem) as System.Windows.Controls.Border;
                if (border != null)
                {
                    border.Background = GetSelectedBackgroundBrush();
                    border.BorderBrush = GetBorderPrimaryBrush();
                    border.BorderThickness = new Thickness(1);
                }
            }
        }
        private static void ComboBoxItem_Unselected(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBoxItem comboBoxItem)
            {
                var border = comboBoxItem.Template?.FindName("Border", comboBoxItem) as System.Windows.Controls.Border;
                if (border != null)
                {
                    if (comboBoxItem.IsMouseOver)
                    {
                        border.Background = GetHoverBackgroundBrush();
                    }
                    else
                    {
                        border.Background = GetTextBoxBackgroundBrush();
                    }
                    border.BorderBrush = new SolidColorBrush(Colors.Transparent);
                    border.BorderThickness = new Thickness(0);
                }
            }
        }
        private static void UpdateComboBoxItemStyleTriggers(System.Windows.Controls.ComboBoxItem comboBoxItem)
        {
            try
            {
                if (comboBoxItem.Style == null) return;

                foreach (var trigger in comboBoxItem.Style.Triggers)
                {
                    if (trigger is System.Windows.Trigger baseTrigger)
                    {
                        foreach (var setter in baseTrigger.Setters)
                        {
                            if (setter is System.Windows.Setter setterBase)
                            {
                                if (setterBase.Property == System.Windows.Controls.Control.BackgroundProperty &&
                                    setterBase.Value is SolidColorBrush brush)
                                {
                                    var color = brush.Color;
                                    if (color.R == 245 && color.G == 245 && color.B == 245) 
                                    {
                                        setterBase.Value = GetHoverBackgroundBrush();
                                    }
                                    else if (color.R == 225 && color.G == 225 && color.B == 225) 
                                    {
                                        setterBase.Value = GetSelectedBackgroundBrush();
                                    }
                                }
                                else if (setterBase.TargetName == "Border")
                                {
                                    if (setterBase.Property == System.Windows.Controls.Border.BackgroundProperty &&
                                        setterBase.Value is SolidColorBrush borderBrush)
                                    {
                                        var color = borderBrush.Color;
                                        if (color.R == 245 && color.G == 245 && color.B == 245) 
                                        {
                                            setterBase.Value = GetHoverBackgroundBrush();
                                        }
                                        else if (color.R == 225 && color.G == 225 && color.B == 225) 
                                        {
                                            setterBase.Value = GetSelectedBackgroundBrush();
                                        }
                                    }
                                    else if (setterBase.Property == System.Windows.Controls.Border.BorderBrushProperty &&
                                             baseTrigger.Property == System.Windows.Controls.ComboBoxItem.IsSelectedProperty &&
                                             (bool)baseTrigger.Value == true)
                                    {
                                        setterBase.Value = GetBorderPrimaryBrush();
                                    }
                                    else if (setterBase.Property == System.Windows.Controls.Border.BorderThicknessProperty &&
                                             baseTrigger.Property == System.Windows.Controls.ComboBoxItem.IsSelectedProperty &&
                                             (bool)baseTrigger.Value == true)
                                    {
                                        setterBase.Value = new Thickness(1);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
        public static void UpdateCheckBoxColors(System.Windows.DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.CheckBox checkBox)
                {
                    checkBox.Foreground = GetTextPrimaryBrush();
                }
                UpdateCheckBoxColors(child);
            }
        }
        public static void ApplyThemeToControl(System.Windows.DependencyObject control)
        {
            UpdateTextBlockColors(control);
            UpdateBorderColors(control);
            UpdateLineColors(control);
            UpdateInputControlsColors(control);
            UpdateButtonColors(control);
            UpdateImageIconColors(control);
            UpdateComboBoxPopupColors(control);
            UpdateCheckBoxColors(control);
        }
    }
}

