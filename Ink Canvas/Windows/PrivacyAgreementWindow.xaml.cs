using Ink_Canvas.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class PrivacyAgreementWindow : Window
    {
        public bool UserAccepted { get; private set; } = false;

        public PrivacyAgreementWindow()
        {
            InitializeComponent();
            AnimationsHelper.ShowWithSlideFromBottomAndFade(this, 0.25);
            ApplyTheme();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string privacyText = null;
                string pathTxt = Path.Combine(App.RootPath, "privacy.txt");
                string pathNoExt = Path.Combine(App.RootPath, "privacy");

                if (File.Exists(pathTxt))
                {
                    privacyText = File.ReadAllText(pathTxt, System.Text.Encoding.UTF8);
                }
                else if (File.Exists(pathNoExt))
                {
                    privacyText = File.ReadAllText(pathNoExt, System.Text.Encoding.UTF8);
                }

                if (string.IsNullOrWhiteSpace(privacyText))
                {
                    privacyText = "未找到隐私说明文件（privacy.txt 或 privacy）。";
                }

                TextBoxPrivacyContent.Text = privacyText;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"读取隐私说明失败: {ex.Message}", LogHelper.LogType.Warning);
                TextBoxPrivacyContent.Text = "读取隐私说明文件时出错。";
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            UserAccepted = false;
            DialogResult = false;
            Close();
        }

        private void ButtonAccept_Click(object sender, RoutedEventArgs e)
        {
            UserAccepted = true;
            DialogResult = true;
            Close();
        }

        private void ApplyTheme()
        {
            try
            {
                if (MainWindow.Settings != null)
                {
                    ApplyTheme(MainWindow.Settings);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"应用隐私说明窗口主题出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private void ApplyTheme(Settings settings)
        {
            try
            {
                if (settings.Appearance.Theme == 0)
                {
                    iNKORE.UI.WPF.Modern.ThemeManager.SetRequestedTheme(this, iNKORE.UI.WPF.Modern.ElementTheme.Light);
                    ApplyThemeResources("Light");
                }
                else if (settings.Appearance.Theme == 1)
                {
                    iNKORE.UI.WPF.Modern.ThemeManager.SetRequestedTheme(this, iNKORE.UI.WPF.Modern.ElementTheme.Dark);
                    ApplyThemeResources("Dark");
                }
                else
                {
                    bool isSystemLight = IsSystemThemeLight();
                    if (isSystemLight)
                    {
                        iNKORE.UI.WPF.Modern.ThemeManager.SetRequestedTheme(this, iNKORE.UI.WPF.Modern.ElementTheme.Light);
                        ApplyThemeResources("Light");
                    }
                    else
                    {
                        iNKORE.UI.WPF.Modern.ThemeManager.SetRequestedTheme(this, iNKORE.UI.WPF.Modern.ElementTheme.Dark);
                        ApplyThemeResources("Dark");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"应用隐私说明窗口主题出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private void ApplyThemeResources(string theme)
        {
            try
            {
                var resources = this.Resources;

                if (theme == "Light")
                {
                    resources["PrivacyAgreementWindowBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    resources["PrivacyAgreementWindowForeground"] = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    resources["PrivacyAgreementWindowButtonBackground"] = new SolidColorBrush(Color.FromRgb(244, 244, 245));
                    resources["PrivacyAgreementWindowButtonForeground"] = new SolidColorBrush(Color.FromRgb(24, 24, 27));
                    resources["PrivacyAgreementWindowBorderBrush"] = new SolidColorBrush(Color.FromRgb(228, 228, 231));
                    resources["PrivacyAgreementWindowButtonAcceptBackground"] = new SolidColorBrush(Color.FromRgb(53, 132, 228));
                    resources["PrivacyAgreementWindowButtonAcceptForeground"] = new SolidColorBrush(Colors.White);
                }
                else
                {
                    resources["PrivacyAgreementWindowBackground"] = new SolidColorBrush(Color.FromRgb(31, 31, 31));
                    resources["PrivacyAgreementWindowForeground"] = new SolidColorBrush(Colors.White);
                    resources["PrivacyAgreementWindowButtonBackground"] = new SolidColorBrush(Color.FromRgb(42, 42, 42));
                    resources["PrivacyAgreementWindowButtonForeground"] = new SolidColorBrush(Colors.White);
                    resources["PrivacyAgreementWindowBorderBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
                    resources["PrivacyAgreementWindowButtonAcceptBackground"] = new SolidColorBrush(Color.FromRgb(53, 132, 228));
                    resources["PrivacyAgreementWindowButtonAcceptForeground"] = new SolidColorBrush(Colors.White);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"应用隐私说明窗口主题资源出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        private bool IsSystemThemeLight()
        {
            var light = false;
            try
            {
                var registryKey = Microsoft.Win32.Registry.CurrentUser;
                var themeKey = registryKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (themeKey != null)
                {
                    var value = themeKey.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        light = (int)value == 1;
                    }
                    themeKey.Close();
                }
            }
            catch
            {
                light = true;
            }
            return light;
        }
    }
}

