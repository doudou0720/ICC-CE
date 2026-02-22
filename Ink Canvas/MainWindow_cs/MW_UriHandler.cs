using Ink_Canvas.Helpers;
using System;
using System.Windows;

namespace Ink_Canvas
{
    public partial class MainWindow
    {
        /// <summary>
        /// 处理URI命令方法
        /// </summary>
        /// <param name="uri">URI命令字符串</param>
        /// <remarks>
        /// 处理ICC协议的URI命令，包括以下步骤：
        /// 1. 检查URI是否为空
        /// 2. 检查是否启用了外部协议
        /// 3. 记录处理URI命令的日志
        /// 4. 解析URI获取命令
        /// 5. 根据命令执行相应的操作：
        ///    - fold: 进入收纳模式
        ///    - unfold/show: 退出收纳模式
        ///    - toggle: 切换收纳模式
        ///    - thoroughhideon: 开启收起时彻底隐藏
        ///    - thoroughhideoff: 关闭收起时彻底隐藏
        ///    - thoroughhidetoggle: 切换收起时彻底隐藏状态
        ///    - randone: 随机一个
        ///    - rand: 随机
        ///    - timer: 计时器
        ///    - whiteboard/board: 白板
        /// 6. 捕获并记录可能出现的异常
        /// <summary>
        /// 处理基于 ICC URI 方案的外部命令并对窗口或设置执行相应操作。
        /// </summary>
        /// <param name="uri">要处理的 ICC URI（例如 "icc://fold"、"icc:toggle" 或 "icc:thoroughhideon"），大小写不敏感。</param>
        /// <remarks>
        /// 支持的命令（不区分大小写）：
        /// - fold / unfold / show / toggle：控制浮动工具栏的收起与展开并显示对应通知。  
        /// - thoroughhideon / thoroughhideoff / thoroughhidetoggle：设置并保存“收起时彻底隐藏”选项，必要时更新窗口可见性。  
        /// - randone / rand：触发符号随机选择相关操作。  
        /// - timer：触发倒计时相关操作。  
        /// - whiteboard / board：打开白板相关操作。  
        /// 如果 URI 或命令不可识别，则记录警告并忽略请求。方法不抛出异常；发生错误时仅记录日志。
        /// </remarks>
        public void HandleUriCommand(string uri)
        {
            try
            {
                if (string.IsNullOrEmpty(uri)) return;

                // 检查是否启用了外部协议
                if (!Settings.Advanced.IsEnableUriScheme)
                {
                    LogHelper.WriteLogToFile($"URI协议已禁用，忽略请求: {uri}", LogHelper.LogType.Warning);
                    return;
                }

                LogHelper.WriteLogToFile($"正在处理URI命令: {uri}", LogHelper.LogType.Event);

                // 解析URI
                // 格式: icc://command?param=value
                // 如果URI以icc:开头但不是标准URI格式，尝试手动解析
                string command = "";

                if (Uri.TryCreate(uri, UriKind.Absolute, out Uri uriObj))
                {
                    command = uriObj.Host.ToLower();
                    // 处理像 icc:fold 这样 Host 可能为空的情况
                    if (string.IsNullOrEmpty(command))
                    {
                        command = uriObj.AbsolutePath.Trim('/').ToLower();
                    }
                }

                // 如果解析失败且是 icc: 协议，则手动处理
                if (string.IsNullOrEmpty(command) && uri.StartsWith("icc:", StringComparison.OrdinalIgnoreCase))
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

                    case "thoroughhideon":
                        Settings.Automation.ThoroughlyHideWhenFolded = true;
                        SaveSettingsToFile();
                        ShowNotification("已开启：收起时彻底隐藏");
                        // 如果当前已经是在收纳模式，立即隐藏
                        if (isFloatingBarFolded)
                        {
                            this.Visibility = Visibility.Hidden;
                        }
                        break;

                    case "thoroughhideoff":
                        Settings.Automation.ThoroughlyHideWhenFolded = false;
                        SaveSettingsToFile();
                        ShowNotification("已关闭：收起时彻底隐藏");
                        // 确保窗口可见
                        this.Visibility = Visibility.Visible;
                        break;

                    case "thoroughhidetoggle":
                        Settings.Automation.ThoroughlyHideWhenFolded = !Settings.Automation.ThoroughlyHideWhenFolded;
                        SaveSettingsToFile();
                        ShowNotification(Settings.Automation.ThoroughlyHideWhenFolded ? "已开启：收起时彻底隐藏" : "已关闭：收起时彻底隐藏");
                        if (isFloatingBarFolded)
                        {
                            this.Visibility = Settings.Automation.ThoroughlyHideWhenFolded ? Visibility.Hidden : Visibility.Visible;
                        }
                        break;

                    case "randone":
                        SymbolIconRandOne_MouseUp(null, null);
                        break;

                    case "rand":
                        SymbolIconRand_MouseUp(null, null);
                        break;

                    case "timer":
                        ImageCountdownTimer_MouseUp(null, null);
                        break;

                    case "whiteboard":
                    case "board":
                        ImageBlackboard_MouseUp(null, null);
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