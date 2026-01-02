using Ink_Canvas;
using iNKORE.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public abstract class SettingsPanelBase : UserControl
    {
        protected bool _isLoaded = false;

        public SettingsPanelBase()
        {
            Loaded += SettingsPanelBase_Loaded;
        }

        private void SettingsPanelBase_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            EnableTouchSupport();
            try
            {
                var applyThemeMethod = this.GetType().GetMethod("ApplyTheme",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (applyThemeMethod != null)
                {
                    applyThemeMethod.Invoke(this, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SettingsPanelBase 应用主题时出�? {ex.Message}");
            }
            _isLoaded = true;
        }

        protected virtual void EnableTouchSupport()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    EnableTouchSupportForControls(this);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启用触摸支持时出�? {ex.Message}");
            }
        }

        private void EnableTouchSupportForControls(DependencyObject parent)
        {
            if (parent == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Border border)
                {
                    if (border.Tag != null || border.Cursor == Cursors.Hand)
                    {
                        border.IsManipulationEnabled = true;
                        
                        border.TouchDown += Border_TouchDown;
                        border.PreviewTouchDown += Border_PreviewTouchDown;
                    }
                }
                else if (child is Button button)
                {
                    button.IsManipulationEnabled = true;
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.IsManipulationEnabled = true;
                }
                else if (child is Slider slider)
                {
                    slider.IsManipulationEnabled = true;
                }
                else if (child is TextBox textBox)
                {
                    textBox.IsManipulationEnabled = true;
                }
                
                EnableTouchSupportForControls(child);
            }
        }

        private void Border_TouchDown(object sender, TouchEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var touchPoint = e.GetTouchPoint(border);
            
            var mouseButtonEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.MouseLeftButtonDownEvent,
                Source = border
            };
            
            border.RaiseEvent(mouseButtonEventArgs);
            
            border.CaptureTouch(e.TouchDevice);
            e.Handled = true;
        }

        private void Border_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            var border = sender as Border;
            if (border == null) return;

            var touchPoint = e.GetTouchPoint(border);
            
            var mouseButtonEventArgs = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                Source = border
            };
            
            border.RaiseEvent(mouseButtonEventArgs);
            
            e.Handled = true;
        }

        public abstract void LoadSettings();

        protected Border FindToggleSwitch(string name)
        {
            return this.FindDescendantByName(name) as Border;
        }

        protected void SetToggleSwitchState(Border toggleSwitch, bool isOn)
        {
            if (toggleSwitch == null) return;
            toggleSwitch.Background = isOn 
                ? ThemeHelper.GetToggleSwitchOnBackgroundBrush() 
                : ThemeHelper.GetToggleSwitchOffBackgroundBrush();
            var innerBorder = toggleSwitch.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        protected virtual void ToggleSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (!_isLoaded) return;

            var border = sender as Border;
            if (border == null) return;

            bool isOn = ThemeHelper.IsToggleSwitchOn(border.Background);
            bool newState = !isOn;
            SetToggleSwitchState(border, newState);

            string tag = border.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            HandleToggleSwitchChange(tag, newState);
        }

        protected abstract void HandleToggleSwitchChange(string tag, bool newState);

        protected virtual void HandleOptionButtonChange(object sender, string tag)
        {
            var border = sender as Border;
            if (border == null) return;

            string[] parts = tag.Split('_');
            if (parts.Length >= 2)
            {
                string group = parts[0];
                string value = parts[1];

                var parent = border.Parent as Panel;
                if (parent != null)
                {
                    foreach (var child in parent.Children)
                    {
                        if (child is Border childBorder && childBorder != border)
                        {
                            string childTag = childBorder.Tag?.ToString();
                            if (!string.IsNullOrEmpty(childTag) && childTag.StartsWith(group + "_"))
                            {
                                childBorder.Background = new SolidColorBrush(Colors.Transparent);
                                var textBlock = childBorder.Child as TextBlock;
                                if (textBlock != null)
                                {
                                    textBlock.FontWeight = FontWeights.Normal;
                                }
                            }
                        }
                    }
                }

                border.Background = ThemeHelper.GetOptionButtonSelectedBackgroundBrush();
                var currentTextBlock = border.Child as TextBlock;
                if (currentTextBlock != null)
                {
                    currentTextBlock.FontWeight = FontWeights.Bold;
                }

                HandleOptionChange(group, value);
            }
        }

        protected abstract void HandleOptionChange(string group, string value);
    }
}

