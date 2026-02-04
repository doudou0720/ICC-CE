using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security;

namespace Ink_Canvas.Helpers
{
    public static class UriSchemeHelper
    {
        private const string SchemeName = "icc";
        private const string FriendlyName = "URL:Ink Canvas Protocol";

        public static bool RegisterUriScheme()
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // 使用 CurrentUser\Software\Classes 代替 ClassesRoot，无需管理员权限
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + SchemeName))
                {
                    key.SetValue("", FriendlyName);
                    key.SetValue("URL Protocol", "");

                    using (RegistryKey defaultIconKey = key.CreateSubKey("DefaultIcon"))
                    {
                        // 修正引号转义
                        defaultIconKey.SetValue("", "\"" + exePath + "\",1");
                    }

                    using (RegistryKey shellKey = key.CreateSubKey("shell"))
                    using (RegistryKey openKey = shellKey.CreateSubKey("open"))
                    using (RegistryKey commandKey = openKey.CreateSubKey("command"))
                    {
                        // 修正引号转义
                        commandKey.SetValue("", "\"" + exePath + "\" \"%1\"");
                    }
                }
                LogHelper.WriteLogToFile($"成功注册URI Scheme: {SchemeName}://", LogHelper.LogType.Event);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"注册URI Scheme失败: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        public static bool UnregisterUriScheme()
        {
            try
            {
                // 使用 CurrentUser\Software\Classes
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + SchemeName, false);
                LogHelper.WriteLogToFile($"成功注销URI Scheme: {SchemeName}://", LogHelper.LogType.Event);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"注销URI Scheme失败: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        public static bool IsUriSchemeRegistered()
        {
            try
            {
                // 使用 CurrentUser\Software\Classes
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + SchemeName))
                {
                    if (key == null) return false;
                    // 修正反斜杠路径
                    using (RegistryKey shellKey = key.OpenSubKey(@"shell\open\command"))
                    {
                        if (shellKey == null) return false;
                        string command = shellKey.GetValue("") as string;
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        return !string.IsNullOrEmpty(command) && command.Contains(exePath);
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}