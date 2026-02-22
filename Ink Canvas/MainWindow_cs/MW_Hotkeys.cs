using System;
using System.Windows;
using System.Windows.Input;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 鼠标滚轮事件处理，用于PPT翻页
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在幻灯片放映且处于默认模式时，根据鼠标滚轮触发幻灯片上翻或下翻操作。
        /// </summary>
        /// <param name="e">鼠标滚轮事件参数；当 Delta >= 120 时触发上翻，当 Delta <= -120 时触发下翻。</param>
        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible || currentMode != 0) return;
            if (e.Delta >= 120)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
            else if (e.Delta <= -120)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
        }

        /// <summary>
        /// 键盘按键预览事件处理，用于PPT翻页
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理预览键盘按下事件；在幻灯片放映且当前模式为常规放映（currentMode == 0）且结束指示器可见时，根据按键执行上一页或下一页操作。
        /// </summary>
        /// <param name="e">包含被按下的键及其相关信息，用于决定向前（Down/PageDown/Right/N/Space）或向后（Up/PageUp/Left/P）翻页。</param>
        private void Main_Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (BtnPPTSlideShowEnd.Visibility != Visibility.Visible || currentMode != 0) return;

            if (e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Right || e.Key == Key.N || e.Key == Key.Space)
            {
                BtnPPTSlidesDown_Click(null, null);
            }
            if (e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Left || e.Key == Key.P)
            {
                BtnPPTSlidesUp_Click(null, null);
            }
        }


        /// <summary>
        /// 撤销操作热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理撤销快捷键并触发画布的撤销操作。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">执行路由事件参数。</param>
        private void HotKey_Undo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconUndo_MouseUp(lastBorderMouseDownObject, null);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 重做操作热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理重做热键并触发重做操作。
        /// </summary>
        /// <param name="sender">触发该命令的源对象。</param>
        /// <param name="e">执行路由事件参数。</param>
        private void HotKey_Redo(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                SymbolIconRedo_MouseUp(lastBorderMouseDownObject, null);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }

        /// <summary>
        /// 清空画布热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 处理清除画布的快捷键并触发删除操作。
        /// </summary>
        /// <param name="sender">事件的发送者（通常为命令源）。</param>
        /// <param name="e">路由执行参数，包含命令上下文。</param>
        private void HotKey_Clear(object sender, ExecutedRoutedEventArgs e)
        {
            SymbolIconDelete_MouseUp(lastBorderMouseDownObject, null);
        }


        /// <summary>
        /// 退出PPT放映热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 在幻灯片放映结束按钮可见时触发其点击以退出幻灯片放映模式。
        /// </summary>
        /// <param name="sender">事件的发送者。</param>
        /// <param name="e">执行路由事件参数。</param>
        internal void KeyExit(object sender, ExecutedRoutedEventArgs e)
        {
            if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible) BtnPPTSlideShowEnd_Click(BtnPPTSlideShowEnd, null);
        }

        /// <summary>
        /// 切换到绘图工具热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 将当前工具切换为笔（绘图）模式。
        /// </summary>
        /// <param name="sender">命令的发送者。</param>
        /// <param name="e">执行路由事件参数。</param>
        private void KeyChangeToDrawTool(object sender, ExecutedRoutedEventArgs e)
        {
            PenIcon_Click(lastBorderMouseDownObject, null);
        }

        /// <summary>
        /// 退出绘图工具热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">执行路由事件参数</param>
        /// <remarks>
        /// 在白板模式下，alt+q 退出白板模式
        /// 在非白板模式下，alt+q 切换到鼠标模式
        /// <summary>
        /// 根据当前模式退出绘图工具：如果处于白板模式则退出白板，否则切换为鼠标模式。
        /// </summary>
        internal void KeyChangeToQuitDrawTool(object sender, ExecutedRoutedEventArgs e)
        {
            if (currentMode != 0)
            {
                // 在白板模式下，alt+q 退出白板模式
                ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
            }
            else
            {
                // 在非白板模式下，alt+q 切换到鼠标模式
                CursorIcon_Click(lastBorderMouseDownObject, null);
            }
        }

        /// <summary>
        /// 切换到选择工具热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">执行路由事件参数</param>
        /// <summary>
        /// 切换到选择工具（仅在画布控件面板可见时生效）。
        /// </summary>
        /// <remarks>仅当画布控件面板可见时执行选择工具的切换操作。</remarks>
        private void KeyChangeToSelect(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                SymbolIconSelect_MouseUp(lastBorderMouseDownObject, null);
        }

        /// <summary>
        /// 切换到橡皮擦工具热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">执行路由事件参数</param>
        /// <summary>
        /// 切换到橡皮擦工具：根据当前橡皮擦状态在“按笔迹擦除”和“普通橡皮擦”之间切换，仅在画布控件面板可见时生效。
        /// </summary>
        /// <remarks>仅当画布控件面板可见时生效，根据当前橡皮擦状态选择相应的橡皮擦模式。</remarks>
        private void KeyChangeToEraser(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible)
            {
                if (Eraser_Icon.Background != null)
                    EraserIconByStrokes_Click(lastBorderMouseDownObject, null);
                else
                    EraserIcon_Click(lastBorderMouseDownObject, null);
            }
        }

        /// <summary>
        /// 切换到白板模式热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 切换到白板模式并触发相应的白板鼠标弹起处理。
        /// </summary>
        private void KeyChangeToBoard(object sender, ExecutedRoutedEventArgs e)
        {
            ImageBlackboard_MouseUp(lastBorderMouseDownObject, null);
        }

        /// <summary>
        /// 屏幕截图热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 将当前屏幕截图并保存到桌面。
        /// </summary>
        private void KeyCapture(object sender, ExecutedRoutedEventArgs e)
        {
            SaveScreenShotToDesktop();
        }

        /// <summary>
        /// 绘制直线热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">执行路由事件参数</param>
        /// <summary>
        /// 处理快捷键事件以切换到画直线工具并触发相应按钮点击。
        /// </summary>
        /// <remarks>仅当画布控件面板可见时生效。</remarks>
        private void KeyDrawLine(object sender, ExecutedRoutedEventArgs e)
        {
            if (StackPanelCanvasControls.Visibility == Visibility.Visible) BtnDrawLine_Click(lastMouseDownSender, null);
        }

        /// <summary>
        /// 隐藏工具栏热键处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <summary>
        /// 隐藏或切换画布工具面板的可见性（由快捷键触发）。
        /// </summary>
        private void KeyHide(object sender, ExecutedRoutedEventArgs e)
        {
            SymbolIconEmoji_MouseUp(null, null);
        }
    }
}