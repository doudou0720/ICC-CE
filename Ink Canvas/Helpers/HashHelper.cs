using System;
using System.Security.Cryptography;
using System.Text;

namespace Ink_Canvas.Helpers
{
    /// <summary>
    /// 哈希计算辅助类，用于路径/标识等短字符串的 MD5 前缀哈希。
    /// </summary>
    internal static class HashHelper
    {
        /// <summary>
        /// 对给定路径字符串计算 MD5 哈希，返回前 8 位十六进制字符串。
        /// </summary>
        /// <param name="filePath">文件路径或任意字符串</param>
        /// <summary>
        /// 计算输入字符串的 MD5 并返回前 8 个十六进制字符的哈希前缀，用于短路径或标识符的简短表示。
        /// </summary>
        /// <param name="filePath">要计算哈希的输入字符串（例如文件路径或标识符）。</param>
        /// <returns>8 个十六进制字符的哈希前缀；当 <paramref name="filePath"/> 为 null 或空字符串时返回 "unknown"，发生异常时返回 "error"。</returns>
        public static string GetFileHash(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath)) return "unknown";
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(filePath));
                    return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8);
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"计算文件哈希失败: {ex}", LogHelper.LogType.Error);
                return "error";
            }
        }
    }
}