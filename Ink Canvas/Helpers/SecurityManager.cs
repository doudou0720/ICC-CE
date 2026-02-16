using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Ink_Canvas.Windows;
using iNKORE.UI.WPF.Modern.Controls;
using MessageBox=iNKORE.UI.WPF.Modern.Controls.MessageBox;
using System.Threading.Tasks;

namespace Ink_Canvas.Helpers
{
    internal static class SecurityManager
    {
        private const int Pbkdf2Iterations = 120_000;
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;

        public static bool IsPasswordFeatureEnabled(Settings settings)
            => settings?.Security != null && settings.Security.PasswordEnabled;

        public static bool HasPasswordConfigured(Settings settings)
            => settings?.Security != null
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordSalt)
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordHash);

        public static bool IsPasswordRequiredForExit(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnExit;

        public static bool IsPasswordRequiredForEnterSettings(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnEnterSettings;

        public static bool IsPasswordRequiredForResetConfig(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnResetConfig;

        public static bool VerifyPassword(Settings settings, string password)
        {
            if (!HasPasswordConfigured(settings)) return false;
            if (password == null) return false;

            try
            {
                var salt = Convert.FromBase64String(settings.Security.PasswordSalt);
                var expected = Convert.FromBase64String(settings.Security.PasswordHash);

                var actual = DeriveKey(password, salt, expected.Length);
                return FixedTimeEquals(actual, expected);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> PromptAndVerifyAsync(Settings settings, Window owner, string title, string message)
        {
            if (!HasPasswordConfigured(settings)) return true;

            var dialog = new ContentDialog
            {
                Title = title,
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消"
            };

            var panel = new SimpleStackPanel
            {
                Spacing = 12,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap
            };

            var passwordBox = new PasswordBox
            {
                Height = 32
            };

            panel.Children.Add(textBlock);
            panel.Children.Add(passwordBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return false;

            return VerifyPassword(settings, passwordBox.Password);
        }

        public static async Task<string> PromptSetNewPasswordAsync(Window owner)
        {
            var dialog = new ContentDialog
            {
                Title = "设置安全密码",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消"
            };

            var panel = new SimpleStackPanel
            {
                Spacing = 12,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var tipText = new TextBlock
            {
                Text = "请输入新密码",
                TextWrapping = TextWrapping.Wrap
            };

            var newPwdBox = new PasswordBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            var confirmPwdBox = new PasswordBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };

            panel.Children.Add(tipText);
            panel.Children.Add(new TextBlock { Text = "新密码", Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(newPwdBox);
            panel.Children.Add(new TextBlock { Text = "确认新密码", Margin = new Thickness(0, 8, 0, 0) });
            panel.Children.Add(confirmPwdBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return null;

            var pwd = newPwdBox.Password ?? "";
            var confirm = confirmPwdBox.Password ?? "";

            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 4)
            {
                MessageBox.Show("密码长度过短。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            if (!string.Equals(pwd, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("两次输入的密码不一致。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return pwd;
        }

        public static async Task<string> PromptChangePasswordAsync(Settings settings, Window owner)
        {
            if (!HasPasswordConfigured(settings))
            {
                return await PromptSetNewPasswordAsync(owner);
            }

            var dialog = new ContentDialog
            {
                Title = "修改安全密码",
                PrimaryButtonText = "确定",
                SecondaryButtonText = "取消"
            };

            var panel = new SimpleStackPanel
            {
                Spacing = 12,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var tipText = new TextBlock
            {
                Text = "请输入当前密码，并设置新密码。",
                TextWrapping = TextWrapping.Wrap
            };

            var currentBox = new PasswordBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            var newPwdBox = new PasswordBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };
            var confirmPwdBox = new PasswordBox { Height = 32, Margin = new Thickness(0, 4, 0, 0) };

            panel.Children.Add(tipText);
            panel.Children.Add(new TextBlock { Text = "当前密码", Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(currentBox);
            panel.Children.Add(new TextBlock { Text = "新密码", Margin = new Thickness(0, 8, 0, 0) });
            panel.Children.Add(newPwdBox);
            panel.Children.Add(new TextBlock { Text = "确认新密码", Margin = new Thickness(0, 8, 0, 0) });
            panel.Children.Add(confirmPwdBox);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return null;

            var current = currentBox.Password ?? "";
            var newPwd = newPwdBox.Password ?? "";
            var confirm = confirmPwdBox.Password ?? "";

            if (!VerifyPassword(settings, current))
            {
                MessageBox.Show("当前密码错误。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            if (string.IsNullOrWhiteSpace(newPwd) || newPwd.Length < 4)
            {
                MessageBox.Show("新密码长度过短。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
            if (!string.Equals(newPwd, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("两次输入的新密码不一致。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            return newPwd;
        }

        public static void SetPassword(Settings settings, string password)
        {
            if (settings?.Security == null) return;

            var salt = new byte[SaltSizeBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            var hash = DeriveKey(password, salt, HashSizeBytes);

            settings.Security.PasswordSalt = Convert.ToBase64String(salt);
            settings.Security.PasswordHash = Convert.ToBase64String(hash);
        }

        public static void ClearPassword(Settings settings)
        {
            if (settings?.Security == null) return;
            settings.Security.PasswordSalt = "";
            settings.Security.PasswordHash = "";
        }

        private static byte[] DeriveKey(string password, byte[] salt, int keyBytes)
        {
            // 注意：Rfc2898DeriveBytes 在 net472 默认 HMACSHA1
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations))
            {
                return kdf.GetBytes(keyBytes);
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}

