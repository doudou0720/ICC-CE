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
        /// 从应用设置读取 EnableProcessProtection 并相应地启用或禁用进程保护。
        /// <summary>
        /// 从当前应用设置读取进程保护的启用标志并将其应用到 ProcessProtectionManager 的状态。
        /// </summary>
        /// <remarks>如果读取或应用设置时发生错误，方法会静默忽略异常。</remarks>
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
        /// 切换进程保护的启用状态；在状态发生变化时触发相应的启用或禁用操作并保证线程安全。
        /// </summary>
        /// <summary>
        /// 切换进程保护的启用状态并应用相应的保护或取消保护操作。
        /// </summary>
        /// <param name="enabled">为 `true` 时启用进程保护；为 `false` 时禁用进程保护。</param>
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
        /// 在受进程保护的上下文中执行对指定目标的写入操作；在执行时会在必要情况下临时释放针对目标路径及其父目录的锁，执行完成后恢复这些锁。
        /// </summary>
        /// <param name="targetPath">目标文件或目录的路径，用于确定需要临时释放和随后恢复的锁。</param>
        /// <param name="action">执行写入的操作委托，不能为空。</param>
        /// <remarks>
        /// 如果 ProcessProtectionManager.Enabled 为 false，会直接执行 <paramref name="action"/>；
        /// 若在有限时间内无法获取写入门闩，会记录警告并降级为直接执行 <paramref name="action"/>。方法在内部处理异常，不会抛出异常给调用者。
        /// <summary>
        /// 在对指定目标执行写入操作时，临时解除对与该目标相关的文件和目录的进程保护并执行提供的操作。
        /// </summary>
        /// <remarks>
        /// 方法会尝试在有限时间内获得写入门闩以安全地释放受保护的句柄；若门闩获取超时，方法会记录警告并降级为直接执行写入操作。操作完成后（若保护仍启用）会恢复并重新扫描/上锁受影响的目录。若 <paramref name="action"/> 为 null 则立即返回且不会执行任何操作。
        /// </remarks>
        /// <param name="targetPath">写入操作的目标路径（文件或目录），用于确定需要临时解除保护的目录链；可以为 null 或空字符串，表示不基于具体文件路径调整文件锁。</param>
        /// <param name="action">要在解除保护后执行的写入动作；该动作由调用方提供并在本方法的保护上下文中执行。</param>
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
        /// 尝试在指定的毫秒数内获取写入门控（write gate）。
        /// </summary>
        /// <param name="timeoutMs">等待超时时间（毫秒）。小于或等于 0 时视为 1 毫秒。</param>
        /// <summary>
        /// 尝试在指定毫秒内获取写入门控。
        /// </summary>
        /// <param name="timeoutMs">等待门控的超时时间，单位为毫秒；小于或等于 0 时视为 1 毫秒。</param>
        /// <returns>`true` 如果在指定时间内成功获取到写入门控，`false` 否则。</returns>
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
        /// 启用进程保护并对应用根路径进行完整重扫描以锁定需要保护的目录和文件。
        /// <summary>
        /// 启用进程保护，并从应用根目录重新扫描以锁定相关目录和文件。
        /// </summary>
        private static void Enable()
        {
            Enable(rescanRoot: true, rescanDirs: null);
        }

        /// <summary>
        /// 在应用根目录或提供的路径集合上建立目录句柄和文件读取锁以启用进程保护。
        /// </summary>
        /// <param name="rescanRoot">为 true 时对 App.RootPath 进行递归扫描并锁定其下的目录与文件；为 false 时仅处理 <paramref name="rescanDirs"/> 指定的路径（若为 null 则不处理）。</param>
        /// <summary>
        /// 为应用根目录或指定路径建立必要的目录句柄和文件句柄以启用进程保护（尝试锁定受保护的目录与文件）。
        /// </summary>
        /// <param name="rescanRoot">为 true 时对应用根目录进行递归扫描并锁定目录与文件；为 false 时仅处理 <paramref name="rescanDirs"/> 中列出的路径。</param>
        /// <param name="rescanDirs">在 <paramref name="rescanRoot"/> 为 false 时要扫描的路径集合；对于存在的目录会建立目录锁并扫描其文件，对于存在的文件会建立文件锁；可为 null。</param>
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
        /// 释放并清除当前进程持有的所有文件和目录锁定句柄与流资源。
        /// </summary>
        /// <remarks>
        /// 在内部同步锁定下逐一 Dispose 已记录的 FileStream 和 SafeFileHandle，并清空对应的缓存字典；
        /// 释放过程中发生的异常会被忽略（吞掉）。
        /// <summary>
        /// 关闭进程保护：释放所有保持的文件和目录句柄并清空内部状态。
        /// </summary>
        /// <remarks>
        /// 在内部锁保护下执行。会释放并清除用于保持文件/目录锁定的所有资源；释放过程中发生的异常会被吞掉（以保证最佳努力释放）。此方法不会抛出异常以外显式指示失败。
        /// </remarks>
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
        /// 递归地尝试为指定目录及其所有子目录建立并保持目录句柄锁定，跳过配置的排除目录。
        /// </summary>
        /// <param name="root">起始目录的路径；从此路径开始遍历并对符合条件的子目录尝试建立锁定。</param>
        /// <summary>
        /// 递归地为指定根目录及其子目录创建并保持目录句柄锁定，以保护这些目录不被外部修改。
        /// </summary>
        /// <param name="root">要递归锁定的目录路径。</param>
        /// <remarks>遇到的异常会被捕获并忽略；此方法以尽力而为的方式运行，不会向调用方抛出异常。</remarks>
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
        /// 递归扫描指定根目录下的所有文件，并为具有特定扩展名的文件建立读取锁定以防止被进程修改或替换。
        /// </summary>
        /// <param name="root">要开始扫描的根目录路径。</param>
        /// <remarks>
        /// <summary>
        /// 在指定根目录及其所有子目录中查找并为特定扩展名的文件建立只读锁以防止外部修改。
        /// </summary>
        /// <param name="root">要递归扫描的根目录路径。</param>
        /// <remarks>
        /// 仅处理扩展名为 `.exe`、`.dll`、`.config`、`.manifest`、`.dat`、`.enc` 的文件；会跳过被 <see cref="IsExcludedPath(string)"/> 判定为排除的路径。遇到任何 I/O 或访问错误时静默忽略，不会抛出异常或向上返回错误信息；操作为尽力而为（best-effort）。
        /// </remarks>
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
        /// 以只读方式打开并保留指定文件的句柄，将其加入内部锁定缓存以减少该文件被外部修改或删除的可能性。
        /// </summary>
        /// <summary>
        /// 为指定文件打开并保留一个只读文件句柄，从而对该文件保持锁定状态，便于进程保护管理。
        /// </summary>
        /// <param name="filePath">要锁定的文件路径；会先规范化为完整路径并以该路径作为锁表的键。</param>
        /// <remarks>
        /// 如果该文件已被记录为已锁定则立即返回；在打开文件或写入锁表时发生的错误将被吞掉（不会抛出异常）。
        /// </remarks>
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
        /// 尝试为指定目录获取一个用于保持目录打开的句柄并将其保存为内部锁定记录；若目录已被记录则不作任何操作，发生错误时静默忽略。
        /// </summary>
        /// <summary>
        /// 尝试为指定目录获取并保存一个目录句柄以将该目录置于进程级保护（即保持打开的目录句柄以防止外部删除/替换）。</summary>
        /// <param name="dirPath">要锁定的目录路径；调用时会对路径进行规范化（转换为完整路径并移除多余分隔符）。</param>
        /// <remarks>此操作为最佳努力：如果目录已被锁定或在获取句柄时出现错误，方法不会抛出异常。</remarks>
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
        /// 将路径标准化为不含末尾路径分隔符的绝对路径。
        /// </summary>
        /// <param name="p">要规范化的路径；如果为 null、空或仅空白，则返回原值。</param>
        /// <summary>
        /// 将输入路径规范化为不含末尾路径分隔符的绝对路径。
        /// </summary>
        /// <param name="p">要规范化的路径字符串；可以为相对路径、绝对路径或空/空白字符串。</param>
        /// <returns>规范化后的路径：在解析成功时返回去除末尾分隔符的绝对路径；在无法解析或输入为空/空白时返回原始输入。</returns>
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
        /// 构建从指定路径向上直到应用根目录（App.RootPath）的目录链，并按从下到上的顺序返回已规范化的目录路径。
        /// </summary>
        /// <param name="path">起始路径，既可为文件路径也可为目录路径；若为文件则使用其所在目录作为起点。</param>
        /// <summary>
        /// 生成从指定路径向上直到应用根目录的目录链（包含起始目录和根目录）。
        /// </summary>
        /// <param name="path">起始路径；若为文件路径则使用其所在目录，若为目录路径则使用该目录。</param>
        /// <returns>按从起始目录到根目录的顺序返回目录路径列表；当应用根路径无效或指定路径不位于根路径下时返回空列表。</returns>
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
        /// 检查给定路径是否位于应用根目录下的受排除子目录之一。
        /// </summary>
        /// <param name="path">要检查的文件或目录路径。</param>
        /// <summary>
        /// 判断指定路径是否位于应用根目录下的任一被排除子目录中。
        /// </summary>
        /// <param name="path">要检查的文件或目录路径（可为相对或绝对路径）。</param>
        /// <returns>`true` 如果路径位于任何配置为排除的子目录下（比较不区分大小写），`false` 否则。</returns>
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
        /// 为指定目录打开一个文件句柄，便于对该目录进行锁定或访问其元数据。
        /// </summary>
        /// <param name="dirPath">目标目录的完整路径。</param>
        /// <summary>
        /// 返回指定目录的原始文件句柄，供低级别目录访问或锁定使用。
        /// </summary>
        /// <param name="dirPath">要打开的目录的路径。</param>
        /// <returns>表示已打开目录的 <see cref="SafeFileHandle"/>；若无法打开则返回无效的句柄（`IsInvalid` 为 true），调用方应检查句柄有效性。</returns>
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
        /// 使用原生 CreateFile API 打开或创建一个文件/目录并返回底层句柄的包装函数声明。
        /// </summary>
        /// <param name="lpFileName">要打开或创建的文件或目录的完整路径（UTF-16 编码）。</param>
        /// <param name="dwDesiredAccess">请求的访问权限位掩码（例如读取或写入访问）。</param>
        /// <param name="dwShareMode">共享模式位掩码，指定其他进程可以如何共享此文件句柄。</param>
        /// <param name="lpSecurityAttributes">指向安全属性结构的指针，或为 <see cref="IntPtr.Zero"/> 表示默认安全性。</param>
        /// <param name="dwCreationDisposition">指定如何处理已存在或不存在的文件（例如打开、创建或截断）。</param>
        /// <param name="dwFlagsAndAttributes">文件属性和标志位，用于控制文件或目录的特殊行为（例如备份语义）。</param>
        /// <param name="hTemplateFile">用于创建新文件时的模板句柄，通常为 <see cref="IntPtr.Zero"/>。</param>
        /// <summary>
            /// 使用指定的访问权限、共享模式和标志打开现有的文件或目录并返回其底层句柄。
            /// </summary>
            /// <param name="lpFileName">要打开的文件或目录的完整路径。</param>
            /// <param name="dwDesiredAccess">所需的访问权限（例如读取、写入或通用访问的位掩码）。</param>
            /// <param name="dwShareMode">共享模式，指定其他句柄对该对象可用的访问类型。</param>
            /// <param name="lpSecurityAttributes">指向安全属性结构的指针；通常为 <see cref="System.IntPtr.Zero"/>。</param>
            /// <param name="dwCreationDisposition">创建或打开文件的方式（例如仅打开、覆盖等）。</param>
            /// <param name="dwFlagsAndAttributes">文件或目录的标志和属性（例如备份语义以打开目录）。</param>
            /// <param name="hTemplateFile">用于指定模板文件句柄的保留参数；通常为 <see cref="System.IntPtr.Zero"/>。</param>
            /// <returns>表示文件或目录句柄的 <see cref="Microsoft.Win32.SafeHandles.SafeFileHandle"/>；调用失败时返回无效句柄，可通过 <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/> 获取错误码。</returns>
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