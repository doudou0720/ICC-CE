using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Sentry;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 遥测上传辅助类：根据用户设置，通过 Sentry 上传 usage_stats.enc 和 Crashes 目录的摘要信息。
    /// </summary>
    internal static class TelemetryUploader
    {
        /// <summary>
        /// 根据当前设置决定是否上传遥测数据。
        /// 在主窗口加载完成后调用一次即可。
        /// </summary>
        public static Task UploadTelemetryIfNeededAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    var settings = MainWindow.Settings;
                    if (settings == null || settings.Startup == null)
                    {
                        return;
                    }

                    var level = settings.Startup.TelemetryUploadLevel;
                    if (level == TelemetryUploadLevel.None)
                    {
                        return; // 用户未开启
                    }

                    // 获取并校验设备ID
                    string deviceId = DeviceIdentifier.GetDeviceId();
                    if (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length < 5)
                    {
                        LogHelper.WriteLogToFile("TelemetryUploader | 设备ID无效，取消遥测上传", LogHelper.LogType.Warning);
                        return;
                    }

                    // 读取 usage_stats.enc 作为基础数据
                    string usageStatsPath = Path.Combine(App.RootPath, "usage_stats.enc");
                    string usageStatsContent = null;
                    if (File.Exists(usageStatsPath))
                    {
                        try
                        {
                            usageStatsContent = File.ReadAllText(usageStatsPath);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(
                                $"TelemetryUploader | 读取 usage_stats.enc 失败: {ex.Message}",
                                LogHelper.LogType.Warning);
                        }
                    }

                    // 可选：读取 Crashes 目录下的崩溃日志（仅在扩展模式时上传）
                    List<object> crashFiles = null;
                    if (level == TelemetryUploadLevel.Extended)
                    {
                        try
                        {
                            string crashDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Crashes");
                            if (Directory.Exists(crashDir))
                            {
                                crashFiles = new List<object>();
                                var files = Directory.GetFiles(crashDir);

                                int count = 0;
                                foreach (var file in files)
                                {
                                    if (count >= 10)
                                    {
                                        break; // 简单限制：最多上传最近10个文件
                                    }

                                    try
                                    {
                                        var info = new FileInfo(file);

                                        // 避免一次上传过大，单文件限制为 512KB
                                        if (info.Length > 512 * 1024)
                                        {
                                            continue;
                                        }

                                        string content = File.ReadAllText(file);
                                        crashFiles.Add(new
                                        {
                                            file_name = info.Name,
                                            content = content
                                        });

                                        count++;
                                    }
                                    catch (Exception exFile)
                                    {
                                        LogHelper.WriteLogToFile(
                                            $"TelemetryUploader | 读取崩溃日志失败: {exFile.Message}",
                                            LogHelper.LogType.Warning);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.WriteLogToFile(
                                $"TelemetryUploader | 收集崩溃日志失败: {ex.Message}",
                                LogHelper.LogType.Warning);
                        }
                    }

                    // 通过 Sentry 上报一个包含遥测信息的事件
                    var evt = new SentryEvent
                    {
                        Message = "ICC CE Telemetry",
                        Level = SentryLevel.Info
                    };

                    evt.SetTag("telemetry_level", level.ToString());
                    evt.SetTag("device_id", deviceId);
                    evt.SetTag("app_version", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                    evt.SetTag("os_version", Environment.OSVersion.VersionString);

                    evt.SetExtra("usage_stats_raw", usageStatsContent);
                    if (crashFiles != null)
                    {
                        evt.SetExtra("crash_files", crashFiles);
                    }

                    SentrySdk.CaptureEvent(evt);
                    LogHelper.WriteLogToFile("TelemetryUploader | 遥测数据已通过 Sentry 上报", LogHelper.LogType.Event);
                }
                catch (Exception ex)
                {
                    // 遥测上传失败不影响主功能
                    LogHelper.WriteLogToFile($"TelemetryUploader | 遥测上传失败: {ex.Message}", LogHelper.LogType.Warning);
                }
            });
        }
    }
}


