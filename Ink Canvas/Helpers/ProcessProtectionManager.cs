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
        /// 根据主窗口的配置启用或禁用进程保护（对应用根路径下的文件和目录应用或移除锁定）。
        /// </summary>
        /// <remarks>
        /// 从 MainWindow.Settings 读取 Security.EnableProcessProtection 并调用 SetEnabled 设置当前保护状态；在读取或应用设置时遇到错误会被忽略。
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
        /// 启用或禁用进程级的文件与目录保护。
        /// </summary>
        /// <param name="enabled">为 <c>true</c> 时开启保护，为 <c>false</c> 时关闭保护。</param>
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
        /// 在对指定路径执行写操作时，临时释放该路径及其到应用根目录的目录链上的文件和目录锁以允许写入，写入完成后尝试恢复原有的锁定状态，并序列化并发写入请求以避免冲突。
        /// </summary>
        /// <param name="targetPath">目标文件或目录的路径；会被规范化并用于确定需要暂时释放的目录链。</param>
        /// <param name="action">在释放锁后执行的写操作；如果为 null 则不执行任何操作。</param>
        public static void WithWriteAccess(string targetPath, Action action)
        {
            if (action == null) return;

            if (!Enabled)
            {
                action();
                return;
            }

            if (Interlocked.Exchange(ref _writeGate, 1) == 1)
            {
                var start = Environment.TickCount;
                while (Interlocked.CompareExchange(ref _writeGate, 1, 1) == 1 && Environment.TickCount - start < 2000)
                {
                    Thread.Sleep(10);
                }
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
        /// 启用进程保护并对应用根目录执行完整重扫描以锁定受保护的文件和目录。
        /// </summary>
        private static void Enable()
        {
            Enable(rescanRoot: true, rescanDirs: null);
        }

        /// <summary>
        /// 根据给定范围为应用根路径下的目录和文件建立或恢复保护性锁定。
        /// </summary>
        /// <param name="rescanRoot">为 true 时递归扫描并锁定应用根路径下的所有目录和文件；为 false 时仅处理 <paramref name="rescanDirs"/> 指定的路径。</param>
        /// <param name="rescanDirs">当 <paramref name="rescanRoot"/> 为 false 时，包含需（重新）锁定的目录或文件路径的集合；目录会递归锁定，文件会单独锁定。可为 null。</param>
        /// <remarks>
        /// 如果 App.RootPath 不存在或无效则直接返回。方法内部对所有异常静默吞掉以避免抛出到调用方。
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
        /// 释放并移除当前管理器持有的所有文件与目录锁，恢复到未加锁状态。
        /// </summary>
        /// <remarks>
        /// 在内部互斥锁下执行；关闭过程中对单个释放操作抛出的异常会被吞掉以保证清理继续进行。
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
        /// 递归对指定根目录及其所有子目录（跳过排除的子路径）应用目录级锁以保护其不被写入或修改。
        /// </summary>
        /// <param name="root">要开始加锁的根目录路径。</param>
        /// <remarks>遇到任何文件系统错误时函数会静默忽略，不会向调用者抛出异常。</remarks>
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
        /// 递归扫描指定根目录下的文件，并对具有特定扩展名的文件加锁，跳过已排除的子路径。
        /// </summary>
        /// <param name="root">用于开始扫描的根目录路径。</param>
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
        /// 通过以只读方式打开并缓存文件的 FileStream 来对指定文件施加进程级写入防护；若文件已被管理则不重复操作。
        /// </summary>
        /// <param name="filePath">要施加锁定的文件路径（方法内部会规范化该路径）。</param>
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
        /// 将指定目录加入进程保护锁定，防止对该目录的写入访问。
        /// </summary>
        /// <param name="dirPath">要锁定的目录路径（可以为相对或绝对路径；会先进行归一化）。</param>
        /// <remarks>
        /// 如果目录已被锁定则不做任何操作；在内部使用同步锁以保证线程安全；任何在尝试创建目录句柄时发生的错误会被吞掉且不会抛出异常。
        /// 成功时会在内部字典中保存目录的句柄以维持锁定状态。
        /// </remarks>
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
        /// 将路径规范化为绝对路径并移除末尾的目录分隔符。
        /// </summary>
        /// <param name="p">要规范化的路径；如果为 null、空或仅包含空白字符，则原样返回该值。</param>
        /// <returns>规范化后的绝对路径（不包含末尾的目录分隔符）；若规范化过程中发生错误或输入为 null/空白，则返回原始输入。</returns>
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
        /// 构建从指定路径向上到应用根目录的目录链，包含起点目录和根目录（若在根之下）。
        /// </summary>
        /// <param name="path">起点，可为文件路径或目录路径；如果为文件则使用其所在目录。</param>
        /// <returns>按从起点向上到根的顺序返回已规范化的目录路径列表；当根路径无效、路径不在根之下或发生错误时返回空列表。</returns>
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
        /// 判断给定路径是否位于应用根目录下的预定义被排除子目录之一中。
        /// </summary>
        /// <param name="path">要检测的文件或目录路径（可为相对或绝对路径）。</param>
        /// <returns>`true` 如果路径位于 App.RootPath 下且属于预定义的被排除子目录（例如 Configs、Saves、Backups、Logs、AutoUpdate）之一，`false` 否则（包括路径无效或发生错误时）。</returns>
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
        /// 为指定目录创建一个可用于目录级别访问/锁定的本机句柄。
        /// </summary>
        /// <param name="dirPath">要打开的目录的绝对或相对路径。</param>
        /// <returns>表示该目录的 <see cref="SafeFileHandle"/>；如果创建失败则返回无效的句柄，调用方应检查句柄的有效性。</returns>
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
            /// 为指定的文件或目录打开或创建一个 Win32 句柄并返回对应的 SafeFileHandle，用于对文件或目录执行低级别访问（也可用于目录，配合 BACKUP_SEMANTICS 标志）。
            /// </summary>
            /// <param name="lpFileName">要打开或创建的文件或目录的完整路径。</param>
            /// <param name="dwDesiredAccess">所请求的访问权限掩码（例如读取、写入或通用访问标志）。</param>
            /// <param name="dwShareMode">指定其他打开者可共享的访问类型（例如共享读取/写入/删除）。</param>
            /// <param name="lpSecurityAttributes">指向安全属性结构的指针；通常传入 IntPtr.Zero。</param>
            /// <param name="dwCreationDisposition">定义文件存在与否时的行为（如打开现有、创建新文件等）。</param>
            /// <param name="dwFlagsAndAttributes">文件属性和标志位（例如 BACKUP_SEMANTICS 用于打开目录、FILE_FLAG_BACKUP_SEMANTICS 等）。</param>
            /// <param name="hTemplateFile">用于创建新文件的模板句柄；通常传入 IntPtr.Zero。</param>
            /// <returns>表示所获得句柄的 SafeFileHandle；调用方应检查返回值的有效性（例如 SafeFileHandle.IsInvalid）并在失败时使用 Marshal.GetLastWin32Error 获取错误码。</returns>
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
