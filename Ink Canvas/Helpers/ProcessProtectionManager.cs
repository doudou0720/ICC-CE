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
                        LogHelper.LogType.Warn);
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

        private static void Enable()
        {
            Enable(rescanRoot: true, rescanDirs: null);
        }

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

