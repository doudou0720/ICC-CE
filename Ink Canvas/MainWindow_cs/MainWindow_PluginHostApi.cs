using System;
using System.Windows;
using System.Windows.Ink;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace Ink_Canvas
{
    /// <summary>
    /// 供 <see cref="Helpers.Plugins.PluginSdkHostContext"/> 调用的宿主 API，封装 UI 线程与内部墨迹逻辑。
    /// </summary>
    public partial class MainWindow : Window
    {
        internal void PluginHost_RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                Dispatcher.Invoke(action);
            }
        }

        internal void PluginHost_Undo()
        {
            PluginHost_RunOnUiThread(() =>
            {
                if (inkCanvas.GetSelectedStrokes().Count != 0)
                {
                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                    inkCanvas.Select(new StrokeCollection());
                }

                var item = timeMachine.Undo();
                ApplyHistoryToCanvas(item);
            });
        }

        internal void PluginHost_Redo()
        {
            PluginHost_RunOnUiThread(() =>
            {
                if (inkCanvas.GetSelectedStrokes().Count != 0)
                {
                    GridInkCanvasSelectionCover.Visibility = Visibility.Collapsed;
                    inkCanvas.Select(new StrokeCollection());
                }

                var item = timeMachine.Redo();
                ApplyHistoryToCanvas(item);
            });
        }

        internal void PluginHost_ClearInk(bool erasedByCode)
        {
            PluginHost_RunOnUiThread(() => ClearStrokes(erasedByCode));
        }

        internal bool PluginHost_CanUndo()
        {
            if (Dispatcher.CheckAccess())
            {
                return timeMachine != null && timeMachine.CanUndo;
            }

            return Dispatcher.Invoke(() => timeMachine != null && timeMachine.CanUndo);
        }

        internal bool PluginHost_CanRedo()
        {
            if (Dispatcher.CheckAccess())
            {
                return timeMachine != null && timeMachine.CanRedo;
            }

            return Dispatcher.Invoke(() => timeMachine != null && timeMachine.CanRedo);
        }

        internal void PluginHost_ShowInfo(string title, string message)
        {
            PluginHost_RunOnUiThread(() =>
            {
                try
                {
                    MessageBox.Show(message ?? string.Empty, title ?? string.Empty);
                }
                catch
                {
                    // 忽略对话框失败，避免插件拖垮宿主
                }
            });
        }

        internal bool PluginHost_ShowConfirm(string title, string message)
        {
            if (Dispatcher.CheckAccess())
            {
                try
                {
                    return MessageBox.Show(message ?? string.Empty, title ?? string.Empty, MessageBoxButton.YesNo) ==
                           MessageBoxResult.Yes;
                }
                catch
                {
                    return false;
                }
            }

            return Dispatcher.Invoke(() =>
            {
                try
                {
                    return MessageBox.Show(message ?? string.Empty, title ?? string.Empty, MessageBoxButton.YesNo) ==
                           MessageBoxResult.Yes;
                }
                catch
                {
                    return false;
                }
            });
        }

        internal string PluginHost_ShowInput(string title, string message, string defaultValue)
        {
            string Show()
            {
                try
                {
                    return Microsoft.VisualBasic.Interaction.InputBox(message ?? string.Empty, title ?? string.Empty,
                        defaultValue ?? string.Empty);
                }
                catch
                {
                    return defaultValue ?? string.Empty;
                }
            }

            if (Dispatcher.CheckAccess())
            {
                return Show();
            }

            return Dispatcher.Invoke(Show);
        }
    }
}
