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

        /// <summary>
            /// 检查应用设置中是否启用了密码功能。
            /// </summary>
            /// <param name="settings">应用设置对象；如果为 null 或其 Security 为 null，则视为未启用。</param>
            /// <returns>`true` 如果 settings 的 Security 存在且 PasswordEnabled 为 true，`false` 否则。</returns>
            public static bool IsPasswordFeatureEnabled(Settings settings)
            => settings?.Security != null && settings.Security.PasswordEnabled;

        /// <summary>
               /// 检查设置中是否已配置密码（存在非空的密码盐和密码哈希）。
               /// </summary>
               /// <returns>`true` 如果设置包含非空的 PasswordSalt 和 PasswordHash，`false` 否则。</returns>
               public static bool HasPasswordConfigured(Settings settings)
            => settings?.Security != null
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordSalt)
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordHash);

        /// <summary>
            /// 确定应用退出时是否需要验证密码。
            /// </summary>
            /// <param name="settings">用于读取安全相关配置的应用设置；若为 <c>null</c> 或未配置安全，则视为未要求密码。</param>
            /// <returns><c>true</c> 如果已启用密码功能、已配置密码且设置要求在退出时输入密码；否则 <c>false</c>。</returns>
            public static bool IsPasswordRequiredForExit(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnExit;

        /// <summary>
            /// 确定在进入设置界面时是否需要输入密码。
            /// </summary>
            /// <param name="settings">包含安全配置的应用设置，用于检查密码功能及其具体要求（可能为 null）。</param>
            /// <returns>`true` 如果密码功能已启用、已配置密码且设置为在进入设置时要求密码；否则 `false`.</returns>
            public static bool IsPasswordRequiredForEnterSettings(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnEnterSettings;

        /// <summary>
            /// 检查在重置配置时是否需要输入密码。
            /// </summary>
            /// <param name="settings">应用配置对象，包含安全设置。</param>
            /// <returns><c>true</c> 如果已启用密码功能、已配置密码且在重置配置时要求密码，否则 <c>false</c>。</returns>
            public static bool IsPasswordRequiredForResetConfig(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnResetConfig;

        /// <summary>
        /// 验证给定密码是否与设置中存储的密码哈希匹配。
        /// </summary>
        /// <param name="settings">包含 PasswordSalt 和 PasswordHash 的应用设置。</param>
        /// <param name="password">要验证的明文密码。</param>
        /// <returns>`true` 如果密码与存储的哈希匹配，`false` 否则或在验证过程中发生错误。</returns>
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

        /// <summary>
        /// 显示一个密码对话框以提示用户输入并验证该密码。
        /// </summary>
        /// <param name="settings">包含存储的密码哈希与盐的应用设置；如果未配置密码，方法直接通过验证。</param>
        /// <param name="owner">作为对话框所有者的窗口。</param>
        /// <param name="title">对话框标题文本。</param>
        /// <param name="message">对话框中显示的提示消息。</param>
        /// <returns>`true` 如果未配置密码或用户输入的密码与存储的密码匹配，`false` 否则。</returns>
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

        /// <summary>
        /// 弹出对话框让用户设置新的密码并进行基本验证。
        /// </summary>
        /// <param name="owner">对话框所属的窗口，用于作为父窗口（可为 null）。</param>
        /// <returns>成功时返回新密码字符串；当用户取消或验证失败（密码为空、长度小于 4 或两次输入不一致）时返回 <c>null</c>。</returns>
        /// <remarks>
        /// 验证规则：密码长度必须至少为 4，且新密码与确认密码必须完全相同（区分大小写，使用 Ordinal 比较）。
        /// </remarks>
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

        /// <summary>
        /// 弹出界面以修改已配置的安全密码，成功时返回新的密码字符串。
        /// </summary>
        /// <param name="settings">包含当前密码配置和验证所需信息的应用设置对象。</param>
        /// <param name="owner">用于显示对话框的父窗口。</param>
        /// <returns>新的密码字符串；用户取消、验证失败或输入验证不通过时返回 <c>null</c>。</returns>
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

        /// <summary>
        /// 为应用设置生成并保存新的密码验证数据。
        /// </summary>
        /// <param name="settings">承载密码信息的设置对象；方法将在 settings.Security 中写入盐和哈希。</param>
        /// <param name="password">要设置的明文密码。</param>
        /// <remarks>
        /// 如果 <paramref name="settings"/> 或其 <c>Security</c> 字段为 <c>null</c>，方法不会执行任何操作。
        /// 生成随机盐并对给定密码派生哈希，随后以 Base64 字符串形式保存到 <c>settings.Security.PasswordSalt</c> 和 <c>settings.Security.PasswordHash</c>（会覆盖已有值）。
        /// </remarks>
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

        /// <summary>
        /// 清除指定配置中的密码凭据（将存储的盐和哈希设为空字符串）。
        /// </summary>
        /// <param name="settings">包含 Security 节点的应用设置；如果为 null 或其 Security 为 null，则不执行任何操作。</param>
        public static void ClearPassword(Settings settings)
        {
            if (settings?.Security == null) return;
            settings.Security.PasswordSalt = "";
            settings.Security.PasswordHash = "";
        }

        /// <summary>
        /// 使用 PBKDF2（基于配置的迭代次数）从给定密码和盐派生指定长度的密钥字节数组。
        /// </summary>
        /// <param name="password">用于派生密钥的明文密码。</param>
        /// <param name="salt">用于派生的随机盐字节数组。</param>
        /// <param name="keyBytes">要生成的密钥字节长度。</param>
        /// <returns>长度为 <paramref name="keyBytes"/> 的派生密钥字节数组。</returns>
        private static byte[] DeriveKey(string password, byte[] salt, int keyBytes)
        {
            // 注意：Rfc2898DeriveBytes 在 net472 默认 HMACSHA1
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations))
            {
                return kdf.GetBytes(keyBytes);
            }
        }

        /// <summary>
        /// 在固定时间内比较两个字节数组以判断它们是否完全相等，旨在减少基于比较时长的侧信道泄露。
        /// </summary>
        /// <param name="a">要比较的第一个字节数组。</param>
        /// <param name="b">要比较的第二个字节数组。</param>
        /// <returns>`true` 如果两个数组长度相同且所有字节相等，`false` 否则。</returns>
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
