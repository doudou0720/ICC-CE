using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 自动备份管理器
    /// 负责管理配置文件的自动备份功能
    /// </summary>
    public static class AutoBackupManager
    {
        private static readonly string BackupDir = Path.Combine(App.RootPath, "Backups");
        private static readonly string SettingsFile = Path.Combine(App.RootPath, "Configs", "Settings.json");
        private static readonly string BackupPrefix = "Settings_AutoBackup_";

        /// <summary>
        /// 检查是否需要执行自动备份
        /// </summary>
        /// <param name="settings">设置对象</param>
        /// <returns>如果需要备份返回true，否则返回false</returns>
        public static bool ShouldPerformAutoBackup(Settings settings)
        {
            try
            {
                // 如果自动备份功能未启用，不执行备份
                if (!settings.Advanced.IsAutoBackupEnabled)
                {
                    return false;
                }

                // 如果从未备份过，需要创建首次备份
                if (settings.Advanced.LastAutoBackupTime == DateTime.MinValue)
                {
                    return true;
                }

                // 检查是否已超过备份间隔
                var daysSinceLastBackup = (DateTime.Now - settings.Advanced.LastAutoBackupTime).TotalDays;
                return daysSinceLastBackup >= settings.Advanced.AutoBackupIntervalDays;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"检查自动备份条件时出错: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 执行自动备份
        /// </summary>
        /// <param name="settings">设置对象</param>
        /// <summary>
        /// 将主设置文件复制到备份目录并更新设置中的最后自动备份时间。
        /// </summary>
        /// <param name="settings">要更新并持久化的设置对象；在成功备份后其 <c>Advanced.LastAutoBackupTime</c> 会被设置为当前时间。</param>
        /// <returns>`true` 如果备份成功，`false` 否则。</returns>
        public static bool PerformAutoBackup(Settings settings)
        {
            try
            {
                // 确保备份目录存在
                if (!Directory.Exists(BackupDir))
                {
                    ProcessProtectionManager.WithWriteAccess(BackupDir, () => Directory.CreateDirectory(BackupDir));
                }

                // 检查主配置文件是否存在
                if (!File.Exists(SettingsFile))
                {
                    LogHelper.WriteLogToFile("主配置文件不存在，跳过自动备份", LogHelper.LogType.Warning);
                    return false;
                }

                // 创建备份文件名（使用当前日期时间）
                string backupFileName = $"{BackupPrefix}{DateTime.Now:yyyyMMdd_HHmmss}.json";
                string backupPath = Path.Combine(BackupDir, backupFileName);

                // 复制主配置文件到备份位置
                ProcessProtectionManager.WithWriteAccess(backupPath, () => File.Copy(SettingsFile, backupPath, true));

                // 更新最后备份时间
                settings.Advanced.LastAutoBackupTime = DateTime.Now;
                MainWindow.SaveSettingsToFile();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"执行自动备份时出错: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 尝试从备份恢复配置文件
        /// </summary>
        /// <summary>
        /// 从最新可用的自动备份恢复主设置文件；若当前设置文件存在且将被覆盖，则先在备份目录中保存一份带时间戳的“损坏”副本以作备份。
        /// </summary>
        /// <returns>`true` 如果恢复成功，`false` 否则（例如备份目录或备份文件不存在、备份内容验证失败或发生错误）。</returns>
        public static bool TryRestoreFromBackup()
        {
            try
            {
                // 确保备份目录存在
                if (!Directory.Exists(BackupDir))
                {
                    LogHelper.WriteLogToFile("备份目录不存在，无法从备份恢复", LogHelper.LogType.Warning);
                    return false;
                }

                // 查找最新的备份文件
                var backupFiles = Directory.GetFiles(BackupDir, $"{BackupPrefix}*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                if (backupFiles.Length == 0)
                {
                    LogHelper.WriteLogToFile("没有找到可用的备份文件", LogHelper.LogType.Warning);
                    return false;
                }

                // 尝试使用最新的备份文件
                string latestBackup = backupFiles[0];

                // 验证备份文件是否有效
                try
                {
                    string backupJson = File.ReadAllText(latestBackup);
                    var testSettings = JsonConvert.DeserializeObject<Settings>(backupJson);
                    if (testSettings == null)
                    {
                        LogHelper.WriteLogToFile("备份文件内容无效，无法恢复", LogHelper.LogType.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"备份文件验证失败: {ex.Message}", LogHelper.LogType.Error);
                    return false;
                }

                // 备份当前损坏的配置文件（如果存在）
                if (File.Exists(SettingsFile))
                {
                    string corruptedBackup = Path.Combine(BackupDir, $"Settings_Corrupted_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    ProcessProtectionManager.WithWriteAccess(corruptedBackup, () => File.Copy(SettingsFile, corruptedBackup, true));
                }

                // 从备份恢复配置文件
                ProcessProtectionManager.WithWriteAccess(SettingsFile, () => File.Copy(latestBackup, SettingsFile, true));
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"从备份恢复配置文件时出错: {ex.Message}", LogHelper.LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// 清理过期的备份文件
        /// 保留最近30天的备份文件
        /// <summary>
        /// 删除 Backups 目录中前缀为 Settings_AutoBackup_ 且创建时间早于 30 天的备份文件。
        /// </summary>
        /// <remarks>
        /// 如果目标备份目录不存在则什么也不做；删除操作在受保护的写入上下文中执行，遇到异常时将记录错误日志并中止当前清理操作。
        /// </remarks>
        public static void CleanupOldBackups()
        {
            try
            {
                if (!Directory.Exists(BackupDir))
                {
                    return;
                }

                var cutoffDate = DateTime.Now.AddDays(-30);
                var backupFiles = Directory.GetFiles(BackupDir, $"{BackupPrefix}*.json");

                int deletedCount = 0;
                foreach (var file in backupFiles)
                {
                    if (File.GetCreationTime(file) < cutoffDate)
                    {
                        ProcessProtectionManager.WithWriteAccess(file, () => File.Delete(file));
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"清理过期备份文件时出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }

        /// <summary>
        /// 初始化自动备份功能
        /// 在应用程序启动时调用
        /// </summary>
        /// <param name="settings">设置对象</param>
        public static void Initialize(Settings settings)
        {
            try
            {
                // 检查是否需要执行自动备份
                if (ShouldPerformAutoBackup(settings))
                {
                    PerformAutoBackup(settings);
                }

                // 清理过期备份
                CleanupOldBackups();
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"初始化自动备份功能时出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }
    }
}