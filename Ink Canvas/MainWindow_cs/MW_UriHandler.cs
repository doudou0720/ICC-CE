using Ink_Canvas.Helpers;
using System;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        public void HandleUriCommand(string uri)
        {
            try
            {
                if (string.IsNullOrEmpty(uri)) return;

                LogHelper.WriteLogToFile($"正在处理URI命令: {uri}", LogHelper.LogType.Event);

                // 解析URI
                // 格式: icc://command?param=value
                // 如果URI以icc:开头但不是标准URI格式，尝试手动解析
                string command = "";
                
                if (Uri.TryCreate(uri, UriKind.Absolute, out Uri uriObj))
                {
                    command = uriObj.Host.ToLower();
                }
                else if (uri.StartsWith("icc:", StringComparison.OrdinalIgnoreCase))
                {
                    // 简单的手动解析: icc:fold
                    string path = uri.Substring(4);
                    // 移除可能的斜杠
                    command = path.Trim('/').ToLower();
                }

                switch (command)
                {
                    case "fold":
                        if (!isFloatingBarFolded)
                        {
                            FoldFloatingBar_MouseUp(new object(), null);
                            ShowNotification("已进入收纳模式");
                        }
                        break;

                    case "unfold":
                    case "show": // 兼容旧习惯
                        if (isFloatingBarFolded)
                        {
                            UnFoldFloatingBar_MouseUp(new object(), null);
                            ShowNotification("已退出收纳模式");
                        }
                        break;
                        
                    case "toggle":
                        if (isFloatingBarFolded)
                        {
                            UnFoldFloatingBar_MouseUp(new object(), null);
                            ShowNotification("已退出收纳模式");
                        }
                        else
                        {
                            FoldFloatingBar_MouseUp(new object(), null);
                            ShowNotification("已进入收纳模式");
                        }
                        break;

                    default:
                        LogHelper.WriteLogToFile($"未知的URI命令: {command}", LogHelper.LogType.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLogToFile($"处理URI命令时出错: {ex.Message}", LogHelper.LogType.Error);
            }
        }
    }
}
