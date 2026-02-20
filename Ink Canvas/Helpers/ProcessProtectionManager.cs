using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ink_Canvas.Helpers
{
    internal static class ProcessProtectionManager
    {
        private static readonly object _lock = new object();
        private static readonly Dictionary<string, FileStream> _lockedFiles = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, SafeFileHandle> _lockedDirs = new Dictionary<string, SafeFileHandle>(StringComparer.OrdinalIgnoreCase);
        private static bool _enabled;
        private static int _writeGate;
        private static readonly string[] _excludedSubDirectories = new[]
        {
            "Configs",
            "Saves",
            "Backups",
            "Logs",
            "AutoUpdate"
        };

        public static bool Enabled
        {
            get { lock (_lock) return _enabled; }
        }

        /// <summary>
        /// 从应用设置读取进程保护的开启状态并将其应用到 ProcessProtectionManager 中。
        /// </summary>
        /// <remarks>
        /// 该方法读取 MainWindow.Settings.Security.EnableProcessProtection 并调用 SetEnabled 应用该值；在读取或应用过程中发生的异常会被吞掉且不会抛出。
        /// </remarks>
        public static void ApplyFromSettings()
        {
            try
            {
                var settings = MainWindow.Settings;
                var enabled = settings?.Security != null && settings.Security.EnableProcessProtection;
                SetEnabled(enabled);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 在进程保护管理器中设置是否启用保护，并在状态变更时触发相应的启用或禁用流程（线程安全）。
        /// </summary>
        /// <param name="enabled">为 <c>true</c> 则启用进程/文件/目录保护，为 <c>false</c> 则禁用。</param>
        public static void SetEnabled(bool enabled)
        {
            lock (_lock)
            {
                if (_enabled == enabled) return;
                _enabled = enabled;
            }

            if (enabled) Enable();
            else Disable();
        }

        /// <summary>
        /// 在受保护环境中为指定目标临时释放相关锁并执行写入操作。
        /// </summary>
        /// <param name="targetPath">目标路径（文件或目录）；将临时释放该路径及其到应用根目录的目录链上的锁以允许写入。</param>
        /// <param name="action">在临时释放锁后执行的写入操作委托。</param>
        public static void WithWriteAccess(string targetPath, Action action)
        {
            if (action == null) return;

            if (!Enabled)
            {
                action();
                return;
            }

            const int gateTimeoutMs = 10_000;
            if (!TryEnterWriteGate(gateTimeoutMs))
            {
                try
                {
                    LogHelper.WriteLogToFile($"ProcessProtectionManager.WithWriteAccess: 获取写入门闩超时({gateTimeoutMs}ms)，将降级直接执行写入动作。目标: {targetPath}",
                        LogHelper.LogType.Warning);
                }
                catch
                {
                }

                action();
                return;
            }

            var normalized = NormalizePath(targetPath);
            var dirsToToggle = GetDirChainToRoot(normalized);

            Dictionary<string, SafeFileHandle> releasedDirs = null;
            Dictionary<string, FileStream> releasedFiles = null;

            try
            {
                lock (_lock)
                {
                    releasedDirs = new Dictionary<string, SafeFileHandle>(StringComparer.OrdinalIgnoreCase);
                    releasedFiles = new Dictionary<string, FileStream>(StringComparer.OrdinalIgnoreCase);

                    foreach (var dir in dirsToToggle)
                    {
                        if (_lockedDirs.TryGetValue(dir, out var handle))
                        {
                            _lockedDirs.Remove(dir);
                            releasedDirs[dir] = handle;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(normalized) && File.Exists(normalized) && _lockedFiles.TryGetValue(normalized, out var fs))
                    {
                        _lockedFiles.Remove(normalized);
                        releasedFiles[normalized] = fs;
                    }
                }

                if (releasedFiles != null)
                {
                    foreach (var kv in releasedFiles)
                    {
                        try { kv.Value.Dispose(); } catch { }
                    }
                }
                if (releasedDirs != null)
                {
                    foreach (var kv in releasedDirs)
                    {
                        try { kv.Value.Dispose(); } catch { }
                    }
                }

                action();
            }
            finally
            {
                try
                {
                    if (Enabled)
                    {
                        Enable(rescanRoot: false, rescanDirs: dirsToToggle);
                    }
                }
                catch
                {
                }

                Interlocked.Exchange(ref _writeGate, 0);
            }
        }

        /// <summary>
        /// 在限定的超时时间内尝试获取写入门（单一占用锁）。
        /// </summary>
        /// <param name="timeoutMs">等待获取写入门的超时时间，单位为毫秒；若小于等于 0 则视为 1 毫秒。</param>
        /// <returns>`true` 表示已成功获取写入门，`false` 表示在超时前未能获取。</returns>
        private static bool TryEnterWriteGate(int timeoutMs)
        {
            if (timeoutMs <= 0) timeoutMs = 1;

            var start = Environment.TickCount;
            while (Interlocked.CompareExchange(ref _writeGate, 1, 0) != 0)
            {
                var elapsed = unchecked(Environment.TickCount - start);
                if (elapsed >= timeoutMs) return false;

                if (elapsed < 2000) Thread.Sleep(10);
                else Thread.Sleep(50);
            }
            return true;
        }

        /// <summary>
        /// 启用进程与文件保护，并对应用根目录执行完整重扫描以建立所需的目录和文件锁定。
        /// </summary>
        private static void Enable()
        {
            Enable(rescanRoot: true, rescanDirs: null);
        }

        /// <summary>
        /// 根据指定选项锁定（或重新扫描并锁定）应用根路径或指定路径下的目录与文件。
        /// </summary>
        /// <param name="rescanRoot">为 true 时对应用根路径及其子目录递归地锁定目录与文件；为 false 时仅处理 <paramref name="rescanDirs"/> 指定的路径。</param>
        /// <param name="rescanDirs">当 <paramref name="rescanRoot"/> 为 false 时，枚举要重扫描的目录或文件路径；存在且为目录的项将递归锁定目录并锁定其文件，存在且为文件的项将尝试单独锁定该文件；忽略不存在的路径。</param>
        /// <remarks>
        /// 本方法捕获并吞掉内部异常以避免影响调用者的执行流。
        /// </remarks>
        private static void Enable(bool rescanRoot, IEnumerable<string> rescanDirs)
        {
            try
            {
                var root = App.RootPath;
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return;
                root = NormalizePath(root);

                if (rescanRoot)
                {
                    LockDirectoryRecursive(root);
                }
                else if (rescanDirs != null)
                {
                    foreach (var d in rescanDirs)
                    {
                        if (Directory.Exists(d))
                        {
                            LockDirectory(d);
                        }
                    }
                }

                if (rescanRoot)
                {
                    LockFilesRecursive(root);
                }
                else if (rescanDirs != null)
                {
                    foreach (var d in rescanDirs)
                    {
                        if (Directory.Exists(d))
                        {
                            LockFilesRecursive(d);
                        }
                        else if (File.Exists(d))
                        {
                            LockFile(d);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 以线程安全的方式释放当前持有的所有文件和目录锁，并清空内部锁集合。
        /// </summary>
        private static void Disable()
        {
            lock (_lock)
            {
                foreach (var kv in _lockedFiles)
                {
                    try { kv.Value.Dispose(); } catch { }
                }
                _lockedFiles.Clear();

                foreach (var kv in _lockedDirs)
                {
                    try { kv.Value.Dispose(); } catch { }
                }
                _lockedDirs.Clear();
            }
        }

        /// <summary>
        /// 递归地为指定目录及其所有子目录获取并保持目录锁定，跳过位于排除列表中的子目录。
        /// </summary>
        /// <param name="root">要锁定的目录路径；方法会对该目录及其所有子目录执行操作。</param>
        /// <remarks>方法在遍历或锁定过程中遇到的任何异常都会被吞掉，不会向调用方抛出。</remarks>
        private static void LockDirectoryRecursive(string root)
        {
            try
            {
                if (!IsExcludedPath(root))
                {
                    LockDirectory(root);
                }
                foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.AllDirectories))
                {
                    if (!IsExcludedPath(dir))
                    {
                        LockDirectory(dir);
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 递归扫描指定根目录及其子目录，锁定扩展名为 .exe、.dll、.config、.manifest、.dat 和 .enc 的文件（会跳过排除路径）。
        /// </summary>
        /// <param name="root">要扫描的根目录路径。</param>
        private static void LockFilesRecursive(string root)
        {
            try
            {
                foreach (var file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!IsExcludedPath(file))
                    {
                        var ext = Path.GetExtension(file);
                        if (string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".dll", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".config", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".manifest", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".dat", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ext, ".enc", StringComparison.OrdinalIgnoreCase))
                        {
                            LockFile(file);
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 为指定文件创建并保存一个只读文件句柄以保持文件被锁定，便于后续保护操作使用。
        /// </summary>
        /// <param name="filePath">要锁定的文件的路径（会被归一化）。如果该文件已被记录为锁定，方法立即返回。</param>
        /// <remarks>若无法打开文件（例如权限不足或文件不存在），方法将静默失败且不抛出异常。</remarks>
        private static void LockFile(string filePath)
        {
            filePath = NormalizePath(filePath);
            lock (_lock)
            {
                if (_lockedFiles.ContainsKey(filePath)) return;
                try
                {
                    var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    _lockedFiles[filePath] = fs;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 获取并保存指定目录的只读句柄以将该目录置于锁定状态（如果尚未锁定）。
        /// </summary>
        /// <param name="dirPath">要锁定的目录路径；方法内部会规范化该路径。</param>
        /// <remarks>若目录已锁定则不做任何操作；尝试获取句柄失败时会静默忽略异常。</remarks>
        private static void LockDirectory(string dirPath)
        {
            dirPath = NormalizePath(dirPath);
            lock (_lock)
            {
                if (_lockedDirs.ContainsKey(dirPath)) return;
                try
                {
                    var handle = CreateDirectoryHandle(dirPath);
                    if (handle != null && !handle.IsInvalid)
                    {
                        _lockedDirs[dirPath] = handle;
                    }
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// 规范化给定路径：去除末尾的目录分隔符并返回其绝对规范形式。
        /// </summary>
        /// <param name="p">要规范化的路径；可以为相对路径、绝对路径或空/空白字符串。</param>
        /// <returns>处理后的绝对路径（不包含末尾的目录分隔符）。如果输入为 null 或仅包含空白，则原样返回；若规范化过程中发生错误，也返回原始输入。</returns>
        private static string NormalizePath(string p)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(p)) return p;
                return Path.GetFullPath(p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            }
            catch
            {
                return p;
            }
        }

        /// <summary>
        /// 构建从指定路径到应用根路径（含根）的目录链，按从下至上的顺序排列。
        /// </summary>
        /// <param name="path">要起始的路径；可以是文件路径或目录路径（若为文件则使用其所在目录）。</param>
        /// <returns>包含从指定路径所在目录向上到 App.RootPath 的目录路径列表（自下而上）。若 App.RootPath 无效或指定路径不在根路径下，则返回空列表。</returns>
        private static List<string> GetDirChainToRoot(string path)
        {
            var list = new List<string>();
            try
            {
                var root = NormalizePath(App.RootPath);
                if (string.IsNullOrWhiteSpace(root)) return list;

                string dir = Directory.Exists(path) ? NormalizePath(path) : NormalizePath(Path.GetDirectoryName(path));
                while (!string.IsNullOrWhiteSpace(dir))
                {
                    if (!dir.StartsWith(root, StringComparison.OrdinalIgnoreCase)) break;
                    list.Add(dir);
                    if (string.Equals(dir, root, StringComparison.OrdinalIgnoreCase)) break;
                    dir = NormalizePath(Path.GetDirectoryName(dir));
                }
            }
            catch
            {
            }
            return list;
        }

        /// <summary>
        /// 判断给定路径是否位于应用根路径下的被排除子目录之一。
        /// </summary>
        /// <param name="path">要检查的文件或目录路径。</param>
        /// <returns>`true` 如果路径位于 App.RootPath 下且属于预定义的排除子目录（例如 Configs、Saves、Backups、Logs、AutoUpdate）之一，`false` 否则。</returns>
        private static bool IsExcludedPath(string path)
        {
            try
            {
                var root = NormalizePath(App.RootPath);
                if (string.IsNullOrWhiteSpace(root)) return false;
                path = NormalizePath(path);
                foreach (var name in _excludedSubDirectories)
                {
                    var prefix = Path.Combine(root, name);
                    if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 为指定目录创建并返回一个用于保持目录打开的操作系统句柄（以便对目录加锁或防止被删除/修改）。
        /// </summary>
        /// <param name="dirPath">要打开的目录路径。</param>
        /// <returns>指向已打开目录的 <see cref="SafeFileHandle"/>；如果无法打开则返回无效句柄（<see cref="SafeFileHandle.IsInvalid"/> 为 true）。调用方应在不再需要时释放该句柄。</returns>
        private static SafeFileHandle CreateDirectoryHandle(string dirPath)
        {
            const uint GENERIC_READ = 0x80000000;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint OPEN_EXISTING = 3;
            const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

            return CreateFile(
                dirPath,
                GENERIC_READ,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);
        }

        /// <summary>
            /// 使用指定访问和共享模式打开或创建文件或目录的原生句柄。
            /// </summary>
            /// <param name="lpFileName">要打开或创建的文件或目录的路径（可以是文件或目录名）。</param>
            /// <param name="dwDesiredAccess">请求的访问权限标志（例如 GENERIC_READ、GENERIC_WRITE）。</param>
            /// <param name="dwShareMode">共享模式标志，控制其他打开句柄的读/写/删除共享权限。</param>
            /// <param name="lpSecurityAttributes">指向 SECURITY_ATTRIBUTES 结构的指针，或为 IntPtr.Zero 表示默认安全性。</param>
            /// <param name="dwCreationDisposition">创建/打开操作的行为标志（例如 OPEN_EXISTING、CREATE_NEW）。</param>
            /// <param name="dwFlagsAndAttributes">文件属性和标志（例如 FILE_FLAG_BACKUP_SEMANTICS、FILE_ATTRIBUTE_NORMAL）。</param>
            /// <param name="hTemplateFile">用于创建新文件的模板文件句柄，或为 IntPtr.Zero 表示不使用模板。</param>
            /// <returns>表示打开或创建的本机句柄的 <see cref="SafeFileHandle"/>；操作失败时返回一个无效句柄（其 <c>IsInvalid</c> 为 true）。</returns>
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
    }
}
