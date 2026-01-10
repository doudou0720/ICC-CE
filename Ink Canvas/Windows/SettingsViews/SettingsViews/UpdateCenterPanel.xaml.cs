using iNKORE.UI.WPF.Helpers;
using Ink_Canvas.Helpers;
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Ink_Canvas.Windows.SettingsViews
{
    public partial class UpdateCenterPanel : UserControl
    {
        private bool isLoaded = false;

        public UpdateCenterPanel()
        {
            InitializeComponent();
            Loaded += UpdateCenterPanel_Loaded;
        }

        private void UpdateCenterPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isLoaded)
            {
                isLoaded = true;
                LoadSettings();
                CheckUpdateStatus();
            }
        }

        private void LoadSettings()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                CurrentVersionText.Text = $"InkCanvasForClass v{version}";

                if (MainWindow.Settings?.Startup != null)
                {
                    UpdateToggleSwitch(AutoUpdateToggle, MainWindow.Settings.Startup.IsAutoUpdate);

                    if (UpdateChannelComboBox != null)
                    {
                        foreach (ComboBoxItem item in UpdateChannelComboBox.Items)
                        {
                            if (item.Tag?.ToString() == MainWindow.Settings.Startup.UpdateChannel.ToString())
                            {
                                UpdateChannelComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCenterPanel 加载设置失败: {ex.Message}");
            }
        }

        private void UpdateToggleSwitch(Border toggle, bool isOn)
        {
            if (toggle == null) return;

            toggle.Background = isOn ? new SolidColorBrush(Color.FromRgb(53, 132, 228)) : new SolidColorBrush(Color.FromRgb(225, 225, 225));
            var innerBorder = toggle.Child as Border;
            if (innerBorder != null)
            {
                innerBorder.HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
            else
            {
                var ellipse = new Border
                {
                    Width = 19,
                    Height = 19,
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(10),
                    HorizontalAlignment = isOn ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                ellipse.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    Direction = -45,
                    Color = Colors.Black,
                    Opacity = 0.3,
                    ShadowDepth = 0
                };
                toggle.Child = ellipse;
            }
        }

        private void ToggleSwitch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var toggle = sender as Border;
            if (toggle == null) return;

            var tag = toggle.Tag?.ToString();
            if (tag == "AutoUpdate" && MainWindow.Settings?.Startup != null)
            {
                MainWindowSettingsHelper.UpdateSettingSafely(() =>
                {
                    MainWindow.Settings.Startup.IsAutoUpdate = !MainWindow.Settings.Startup.IsAutoUpdate;
                    UpdateToggleSwitch(toggle, MainWindow.Settings.Startup.IsAutoUpdate);
                }, "ToggleSwitchIsAutoUpdate_Toggled", "ToggleSwitchIsAutoUpdate");
            }
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateStatus();
        }

        private void UpdateNowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    var method = typeof(MainWindow).GetMethod("CheckForUpdates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(mainWindow, null);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"立即更新失败: {ex.Message}");
            }
        }

        private void UpdateChannelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoaded || MainWindow.Settings?.Startup == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem is ComboBoxItem selectedItem)
            {
                var channel = selectedItem.Tag?.ToString();
                if (Enum.TryParse<UpdateChannel>(channel, out var updateChannel))
                {
                    MainWindowSettingsHelper.UpdateSettingSafely(() =>
                    {
                        MainWindow.Settings.Startup.UpdateChannel = updateChannel;
                    }, "UpdateChannelSelector_Checked");
                }
            }
        }

        private void CheckUpdateStatus()
        {
            UpdateStatusText.Text = "正在检查更新...";
            UpdateStatusIcon.Visibility = Visibility.Collapsed;
            UpdateAvailablePanel.Visibility = Visibility.Collapsed;
            CheckUpdateButton.IsEnabled = false;

            Task.Run(async () =>
            {
                try
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        var field = typeof(MainWindow).GetField("AvailableLatestVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (field != null)
                        {
                            var availableVersion = field.GetValue(mainWindow) as string;
                            
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (!string.IsNullOrEmpty(availableVersion))
                                {
                                    UpdateStatusText.Text = "有可用更新";
                                    UpdateStatusIcon.Visibility = Visibility.Visible;
                                    LatestVersionText.Text = $"版本 {availableVersion} 现已可用";
                                    UpdateAvailablePanel.Visibility = Visibility.Visible;
                                }
                                else
                                {
                                    UpdateStatusText.Text = "已是最新版本";
                                    UpdateStatusIcon.Visibility = Visibility.Collapsed;
                                    UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                                }
                                CheckUpdateButton.IsEnabled = true;
                            }));
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                UpdateStatusText.Text = "检查更新失败";
                                UpdateStatusIcon.Visibility = Visibility.Collapsed;
                                UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                                CheckUpdateButton.IsEnabled = true;
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        UpdateStatusText.Text = "检查更新失败";
                        UpdateStatusIcon.Visibility = Visibility.Collapsed;
                        UpdateAvailablePanel.Visibility = Visibility.Collapsed;
                        CheckUpdateButton.IsEnabled = true;
                    }));
                    System.Diagnostics.Debug.WriteLine($"检查更新状态失败: {ex.Message}");
                }
            });
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

        private void ScrollBar_Scroll(object sender, RoutedEventArgs e)
        {
            var scrollbar = (ScrollBar)sender;
            var scrollviewer = scrollbar.FindAscendant<ScrollViewer>();
            if (scrollviewer != null) scrollviewer.ScrollToVerticalOffset(scrollbar.Track.Value);
        }

        private void ScrollBarTrack_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            if (border.Child is Track track)
            {
                track.Width = 16;
                track.Margin = new Thickness(0, 0, -2, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 16;
            }
        }

        private void ScrollBarTrack_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var border = (Border)sender;
            if (border.Child is Track track)
            {
                track.Width = 6;
                track.Margin = new Thickness(0, 0, 0, 0);
                var scrollbar = track.FindAscendant<ScrollBar>();
                if (scrollbar != null) scrollbar.Width = 6;
            }
        }

        private void ScrollbarThumb_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            if (border is Border borderElement)
            {
                borderElement.Background = new SolidColorBrush(Color.FromRgb(95, 95, 95));
            }
        }

        private void ScrollbarThumb_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var thumb = (System.Windows.Controls.Primitives.Thumb)sender;
            var border = thumb.Template.FindName("ScrollbarThumbEx", thumb);
            if (border is Border borderElement)
            {
                borderElement.Background = new SolidColorBrush(Color.FromRgb(195, 195, 195));
            }
        }

        public void ApplyTheme()
        {
            try
            {
                ThemeHelper.ApplyThemeToControl(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCenterPanel 应用主题时出错: {ex.Message}");
            }
        }
    }
}
