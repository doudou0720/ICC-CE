using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Ink_Canvas.Helpers
{
    class LogHelper
    {
        public static string LogFile = "Log.txt";
        private static string LogsFolder = "Logs";
        private static string AppStartTime = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        private static readonly long MaxLogsFolderSizeBytes = 5 * 1024 * 1024; // 5MB

        public static void NewLog(string str)
        {
            WriteLogToFile(str);
        }

        /// <summary>
        /// 将异常的类型、消息与堆栈信息作为错误条目写入日志（包含内部异常信息）。
        /// </summary>
        /// <param name="ex">要记录的异常；若为 <c>null</c> 则不进行任何记录。</param>
        public static void NewLog(Exception ex)
        {
            if (ex == null) return;
            var stackTrace = ex.StackTrace ?? "<no stack trace>";
            var msg = $"[Exception] Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStackTrace: {stackTrace}";
            if (ex.InnerException != null)
            {
                msg += $"\nInnerException: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
            }
            WriteLogToFile(msg, LogType.Error);
        }

        /// <summary>
        /// 将一条日志消息记录到应用的日志文件中（可能是单一日志文件或按启动时间存档的文件），同时在日志条目中包含时间戳、线程 ID 和调用者信息，并遵循应用的日志设置。
        /// </summary>
        /// <param name="str">要记录的日志文本消息。</param>
        /// <summary>
        /// 将指定消息按配置写入日志文件（支持按启动时间分文件或使用单一日志文件）。  
        /// 日志条目包含时间戳、线程 ID、日志等级、调用者信息和消息内容；会遵循 MainWindow.Settings.Advanced 中的日志开关与按日期保存设置。
        /// </summary>
        /// <param name="str">要写入的日志消息文本。</param>
        /// <param name="logType">日志等级，用于在日志条目中标识（例如 Info、Error、Warning 等）。</param>
        public static void WriteLogToFile(string str, LogType logType = LogType.Info)
        {
            // 检查日志是否启用
            if (MainWindow.Settings != null && MainWindow.Settings.Advanced != null && !MainWindow.Settings.Advanced.IsLogEnabled) return;

            string strLogType = logType.ToString();
            try
            {
                string file;

                // 检查是否启用了日期保存功能
                if (MainWindow.Settings != null && MainWindow.Settings.Advanced != null && MainWindow.Settings.Advanced.IsSaveLogByDate)
                {
                    // 确保Logs文件夹存在
                    string logsPath = Path.Combine(App.RootPath, LogsFolder);
                    if (!Directory.Exists(logsPath))
                    {
                        Directory.CreateDirectory(logsPath);
                    }

                    // 检查Logs文件夹大小，如果超过5MB则清空
                    CheckAndCleanLogsFolder(logsPath);

                    // 使用软件启动时间作为日志文件名
                    file = Path.Combine(logsPath, $"Log_{AppStartTime}.txt");
                }
                else
                {
                    file = App.RootPath + LogFile;
                }

                if (!Directory.Exists(App.RootPath))
                {
                    ProcessProtectionManager.WithWriteAccess(App.RootPath, () => Directory.CreateDirectory(App.RootPath));
                }

                var threadId = Thread.CurrentThread.ManagedThreadId;
                var callingMethod = new StackTrace(2, true).GetFrame(0);
                string callerInfo = "<unknown>";
                if (callingMethod != null)
                {
                    var method = callingMethod.GetMethod();
                    if (method != null)
                    {
                        var className = method.DeclaringType != null ? method.DeclaringType.FullName : "<no class>";
                        callerInfo = $"{className}.{method.Name}";
                    }
                }
                string logLine = string.Format("{0} [T{1}] [{2}] [{3}] {4}", DateTime.Now.ToString("O"), threadId, strLogType, callerInfo, str);
                ProcessProtectionManager.WithWriteAccess(file, () =>
                {
                    using (StreamWriter sw = new StreamWriter(file, true))
                    {
                        sw.WriteLine(logLine);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogHelper] WriteLogToFile failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查指定日志文件夹的总大小，并在超过 MaxLogsFolderSizeBytes 时删除该文件夹下的所有文件并记录清理日志。
        /// </summary>
        /// <param name="logsPath">要检查和清理的日志文件夹路径。</param>
        /// <remarks>
        /// - 如果目录不存在则直接返回。 
        /// - 当总大小超过 MaxLogsFolderSizeBytes 时，会尝试删除目录下的每个文件（单个删除失败将被忽略）。 
        /// - 清理完成后会向该目录下的 Log_{AppStartTime}.txt 写入一条带有时间戳和 [Cleanup] 标签的记录。 
        /// - 方法内部捕获并忽略所有异常以避免影响调用者流程。
        /// <summary>
        /// 检查指定的日志目录大小，若超过最大允许值则删除目录内所有日志文件并记录一次清理条目。
        /// </summary>
        /// <param name="logsPath">要检查和清理的日志目录的完整路径。</param>
        /// <remarks>
        /// - 计算目录下所有文件的总字节数；当总大小大于 <see cref="MaxLogsFolderSizeBytes"/> 时，尝试删除该目录下的所有文件。 
        /// - 删除失败的单个文件将被忽略（相关信息写入调试输出），不会抛出异常给调用方。 
        /// - 清理完成后，会向目录下的 Log_{AppStartTime}.txt 追加一条带有时间戳和 "[Cleanup]" 标签的记录，说明已清理并给出大小信息。
        /// - 方法内部捕获并处理所有异常，故不会向上抛出异常。
        /// </remarks>
        private static void CheckAndCleanLogsFolder(string logsPath)
        {
            try
            {
                long totalSize = 0;
                DirectoryInfo dirInfo = new DirectoryInfo(logsPath);

                // 如果目录不存在，直接返回
                if (!dirInfo.Exists) return;

                // 计算文件夹大小
                foreach (FileInfo file in dirInfo.GetFiles())
                {
                    totalSize += file.Length;
                }

                // 如果超过5MB，清空文件夹
                if (totalSize > MaxLogsFolderSizeBytes)
                {
                    foreach (FileInfo file in dirInfo.GetFiles())
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LogHelper] Delete log file failed: {ex.Message}");
                        }
                    }

                    // 记录清理操作
                    string cleanupMessage = $"Logs folder exceeded size limit ({totalSize / 1024.0 / 1024.0:F2} MB > {MaxLogsFolderSizeBytes / 1024.0 / 1024.0:F2} MB). Folder cleaned.";
                    var logFile = Path.Combine(logsPath, $"Log_{AppStartTime}.txt");
                    ProcessProtectionManager.WithWriteAccess(logFile, () =>
                    {
                        using (StreamWriter sw = new StreamWriter(logFile, true))
                        {
                            sw.WriteLine($"{DateTime.Now:O} [Cleanup] {cleanupMessage}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogHelper] CheckAndCleanLogsFolder failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 将指定消息作为警告写入日志，消息前添加 "[Warning]" 前缀并以 Warning 级别记录。
        /// </summary>
        /// <param name="v">要写入的警告消息文本。</param>
        /// <param name="warning">可选的上下文对象；用于签名兼容，不会单独输出到日志。</param>
        internal static void WriteLogToFile(string v, object warning)
        {
            WriteLogToFile($"[Warning] {v}", LogType.Warning);
        }

        public enum LogType
        {
            Info,
            Trace,
            Error,
            Event,
            Warning
        }
    }
}