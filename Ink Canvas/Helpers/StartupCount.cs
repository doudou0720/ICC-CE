using System;
using System.IO;

namespace Ink_Canvas.Helpers
{
    public static class StartupCount
    {
        private static readonly string CountFilePath = Path.Combine(App.RootPath, "startup-count");
        private static readonly object fileLock = new object();

        /// <summary>
        /// 从持久化的计数文件中读取并返回启动计数。
        /// </summary>
        /// <returns>如果计数文件存在且内容可解析为整数，则返回该整数；如果文件不存在、内容无法解析或发生错误，则返回 0。</returns>
        public static int GetCount()
        {
            try
            {
                if (File.Exists(CountFilePath))
                {
                    var text = File.ReadAllText(CountFilePath).Trim();
                    if (int.TryParse(text, out int count))
                        return count;
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            return 0;
        }

        /// <summary>
        /// 将持久化的启动计数加一并写回存储文件。
        /// </summary>
        /// <remarks>
        /// 此操作在内部采用锁以保证并发安全。写入过程中发生的异常会捕获并记录到 System.Diagnostics.Debug，不会向上抛出；在异常情况下文件可能不会更新。
        /// </remarks>
        public static void Increment()
        {
            lock (fileLock)
            {
                int count = GetCount() + 1;
                try
                {
                    File.WriteAllText(CountFilePath, count.ToString());
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
        }

        /// <summary>
        /// 删除持久化的启动计数文件（如果存在），并在内部加锁以保证线程安全。
        /// </summary>
        /// <remarks>
        /// 任何在删除过程中发生的异常会被捕获并写入 System.Diagnostics.Debug 输出，不会向调用者抛出异常。
        /// </remarks>
        public static void Reset()
        {
            lock (fileLock)
            {
                try
                {
                    if (File.Exists(CountFilePath))
                        File.Delete(CountFilePath);
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            }
        }
    }
}