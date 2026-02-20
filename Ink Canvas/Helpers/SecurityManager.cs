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
            /// <param name="settings">应用配置；如果为 null 或不包含 Security 节，则视为未启用密码功能。</param>
            /// <returns>`true` 如果 settings 包含 Security 且其 PasswordEnabled 为真，`false` 否则。</returns>
            public static bool IsPasswordFeatureEnabled(Settings settings)
            => settings?.Security != null && settings.Security.PasswordEnabled;

        /// <summary>
               /// 检查设置对象中是否已配置密码（即 Security 部分存在且同时包含非空的盐和哈希）。
               /// </summary>
               /// <param name="settings">包含 Security 配置的应用设置；若为 null 或未包含 Security 部分，则视为未配置。</param>
               /// <returns>`true` 如果 PasswordSalt 与 PasswordHash 都存在且不为全空白，`false` 否则。</returns>
               public static bool HasPasswordConfigured(Settings settings)
            => settings?.Security != null
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordSalt)
               && !string.IsNullOrWhiteSpace(settings.Security.PasswordHash);

        /// <summary>
            /// 确定在退出应用时是否需要输入密码。
            /// </summary>
            /// <param name="settings">应用配置对象；可为 null，方法会在缺少配置时返回 false。</param>
            /// <returns>`true` 表示已启用密码功能、已配置密码且退出时要求密码；`false` 表示不需要密码。</returns>
            public static bool IsPasswordRequiredForExit(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnExit;

        /// <summary>
            /// 确定在进入设置界面时是否必须输入密码。
            /// </summary>
            /// <param name="settings">应用的设置对象；若为 null 或未配置安全节，则视为不需要密码。</param>
            /// <returns>`true` 当且仅当已启用密码功能、已配置密码且 RequirePasswordOnEnterSettings 标志为真；否则返回 `false`。</returns>
            public static bool IsPasswordRequiredForEnterSettings(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnEnterSettings;

        /// <summary>
            /// 确定在重置应用配置时是否需要验证密码。
            /// </summary>
            /// <param name="settings">应用的配置对象，用于读取安全/密码相关设置。</param>
            /// <returns>`true` 如果已启用密码功能、已配置密码且设置要求在重置配置时验证密码；否则返回 `false`。</returns>
            public static bool IsPasswordRequiredForResetConfig(Settings settings)
            => IsPasswordFeatureEnabled(settings) && HasPasswordConfigured(settings) && settings.Security.RequirePasswordOnResetConfig;

        /// <summary>
        /// 验证给定密码是否与设置中已配置的密码匹配。
        /// </summary>
        /// <param name="settings">包含安全配置（例如存储的盐和哈希）的应用设置对象。</param>
        /// <param name="password">要验证的明文密码。</param>
        /// <returns>`true` 如果密码与存储的密码哈希匹配，`false` 否则（包括未配置密码、传入密码为 null 或验证过程中出现错误）。</returns>
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
        /// 在需要验证当前密码时显示一个密码输入对话框并验证输入的密码。
        /// </summary>
        /// <param name="settings">包含已配置密码信息的应用设置。</param>
        /// <param name="owner">对话框的拥有窗口（用于定位/模态）。</param>
        /// <param name="title">对话框标题文本。</param>
        /// <param name="message">对话框中显示的说明或提示文本。</param>
        /// <returns>`true` 当未配置密码或用户确认并且输入的密码验证通过；`false` 当用户取消或密码验证失败。</returns>
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
        /// 显示一个对话框以设置并确认新的安全密码，验证长度和一致性后返回该密码。
        /// </summary>
        /// <returns>设置成功时返回新密码字符串；用户取消或验证失败（如长度不足或两次输入不一致）时返回 <c>null</c>。</returns>
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
        /// 显示用于修改安全密码的对话框；在用户通过当前密码验证并输入有效的新密码时返回该新密码。
        /// </summary>
        /// <param name="settings">应用配置，用于检查是否已配置密码并验证当前密码。</param>
        /// <param name="owner">对话框的所属窗口，用于显示模态对话框。</param>
        /// <returns>用户确认且验证通过时返回新的密码字符串；用户取消、当前密码错误或新密码验证失败时返回 <c>null</c>。</returns>
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
        /// 为指定设置生成并保存新的密码凭据（盐与派生哈希），存储为 Base64 编码字符串到 settings.Security 中。
        /// </summary>
        /// <param name="settings">包含 Security 节点的应用设置；如果为 null 或其 Security 为 null 则不执行任何操作。</param>
        /// <param name="password">用于生成凭据的明文密码。</param>
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
        /// 清除配置中保存的密码凭据（盐和值）的信息。
        /// </summary>
        /// <param name="settings">包含 Security 部分的应用设置；如果为 null 或其 Security 为 null，则方法不做任何操作。</param>
        public static void ClearPassword(Settings settings)
        {
            if (settings?.Security == null) return;
            settings.Security.PasswordSalt = "";
            settings.Security.PasswordHash = "";
        }

        /// <summary>
        /// 使用 PBKDF2（基于类文件中配置的迭代次数）从给定密码和盐派生指定长度的密钥字节序列。
        /// </summary>
        /// <param name="password">用于派生的明文密码。</param>
        /// <param name="salt">用于派生的盐字节数组。</param>
        /// <param name="keyBytes">要返回的派生密钥的字节长度。</param>
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
        /// 以固定耗时比较两个字节数组是否相等，防止基于比较时间的侧信道攻击。
        /// </summary>
        /// <param name="a">要比较的第一个字节数组（若为 null 则视为不相等）。</param>
        /// <param name="b">要比较的第二个字节数组（若为 null 则视为不相等）。</param>
        /// <returns>`true` 如果两个数组长度相同且每个字节相等，`false` 否则。</returns>
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
