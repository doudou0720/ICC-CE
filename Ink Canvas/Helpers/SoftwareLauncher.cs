using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Ink_Canvas.Helpers
{
    internal static class SoftwareLauncher
    {
        /// <summary>与 ICA 一致：在「程序和功能」卸载列表中按 DisplayName 匹配后启动 sweclauncher.exe。</summary>
        public static void LaunchEasiCamera(string softwareName)
        {
            string executablePath = FindEasiCameraExecutablePath(softwareName);

            if (string.IsNullOrEmpty(executablePath))
            {
                MessageBox.Show(
                    "未找到希沃视频展台安装信息（已扫描 64 位与 32 位卸载注册表）。请确认已通过官方安装包安装「希沃视频展台」。",
                    "Ink Canvas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(executablePath);
                var psi = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    WorkingDirectory = string.IsNullOrEmpty(directory) ? Environment.SystemDirectory : directory
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "无法启动希沃视频展台：" + ex.Message,
                    "Ink Canvas",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static string FindEasiCameraExecutablePath(string softwareName)
        {
            if (string.IsNullOrWhiteSpace(softwareName))
                return null;

            // 64 位进程默认只枚举 64 位注册表视图；32 位希沃常写在 WOW6432Node 下，需一并扫描。
            string[] uninstallRoots =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
            };

            foreach (string root in uninstallRoots)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (key == null) continue;
                    string found = FindInUninstallKey(key, softwareName);
                    if (!string.IsNullOrEmpty(found))
                        return found;
                }
            }

            return null;
        }

        private static string FindInUninstallKey(RegistryKey uninstallKey, string softwareName)
        {
            foreach (string subkeyName in uninstallKey.GetSubKeyNames())
            {
                using (RegistryKey subkey = uninstallKey.OpenSubKey(subkeyName))
                {
                    if (subkey == null) continue;

                    string displayName = subkey.GetValue("DisplayName") as string;
                    if (string.IsNullOrEmpty(displayName) || !displayName.Contains(softwareName))
                        continue;

                    string installLocation = subkey.GetValue("InstallLocation") as string;
                    string uninstallString = subkey.GetValue("UninstallString") as string;

                    string resolved = TryResolveSweclauncher(installLocation, uninstallString);
                    if (!string.IsNullOrEmpty(resolved) && File.Exists(resolved))
                        return resolved;
                }
            }

            return null;
        }

        private static string TryResolveSweclauncher(string installLocation, string uninstallString)
        {
            if (!string.IsNullOrWhiteSpace(installLocation))
            {
                string fromLoc = ResolveSweclauncherUnderInstallRoot(installLocation.Trim().TrimEnd('\\'));
                if (!string.IsNullOrEmpty(fromLoc))
                    return fromLoc;
            }

            if (!string.IsNullOrWhiteSpace(uninstallString))
            {
                // 常见："...\uninstall.exe" 或带引号路径
                string trimmed = uninstallString.Trim();
                if (trimmed.Length >= 2 && trimmed[0] == '"')
                {
                    int end = trimmed.IndexOf('"', 1);
                    if (end > 1)
                        trimmed = trimmed.Substring(1, end - 1);
                }

                int lastSlash = trimmed.LastIndexOf('\\');
                if (lastSlash < 0)
                    return null;

                string folderPath = trimmed.Substring(0, lastSlash);
                string candidate = Path.Combine(folderPath, "sweclauncher", "sweclauncher.exe");
                if (File.Exists(candidate))
                    return candidate;

                candidate = Path.Combine(folderPath, "sweclauncher.exe");
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string ResolveSweclauncherUnderInstallRoot(string installRoot)
        {
            string[] candidates =
            {
                Path.Combine(installRoot, "sweclauncher.exe"),
                Path.Combine(installRoot, "sweclauncher", "sweclauncher.exe"),
            };

            foreach (string p in candidates)
            {
                if (File.Exists(p))
                    return p;
            }

            return null;
        }
    }
}
