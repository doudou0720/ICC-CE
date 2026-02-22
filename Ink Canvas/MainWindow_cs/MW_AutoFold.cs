using Ink_Canvas.Helpers;
using iNKORE.UI.WPF.Modern;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 浮动栏是否折叠的标志。
        /// </summary>
        public bool isFloatingBarFolded;
        
        /// <summary>
        /// 浮动栏正在改变隐藏模式的标志，用于防止重复操作。
        /// </summary>
        private bool isFloatingBarChangingHideMode;

        /// <summary>
        /// 立即关闭白板模式，恢复到批注模式。
        /// </summary>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查是否正在显示或隐藏黑板，如果是则直接返回
        /// 2. 设置显示/隐藏黑板的标志为true
        /// 3. 立即隐藏所有子面板
        /// 4. 如果启用了自动切换多指手势，则关闭多指平移
        /// 5. 隐藏所有水印
        /// 6. 切换到批注模式
        /// 7. 设置退出按钮前景色为白色
        /// 8. 设置应用主题为深色
        /// 9. 200毫秒后重置显示/隐藏黑板的标志为false
        /// <summary>
        /// 立即退出白板模式并将界面恢复到标注/普通显示状态。
        /// </summary>
        /// <remarks>
        /// 隐藏所有子面板、去除水印并切换回标注/普通模式；如果配置了自动切换两指手势，则禁用两指平移。方法使用内部标志防止并发切换，并在短延时后重置该标志以允许后续操作。
        /// </remarks>
        private void CloseWhiteboardImmediately()
        {
            if (isDisplayingOrHidingBlackboard) return;
            isDisplayingOrHidingBlackboard = true;
            HideSubPanelsImmediately();
            if (Settings.Gesture.AutoSwitchTwoFingerGesture) // 自动启用多指书写
                ToggleSwitchEnableTwoFingerTranslate.IsOn = false;
            WaterMarkTime.Visibility = Visibility.Collapsed;
            WaterMarkDate.Visibility = Visibility.Collapsed;
            BlackBoardWaterMark.Visibility = Visibility.Collapsed;
            ICCWaterMarkDark.Visibility = Visibility.Collapsed;
            ICCWaterMarkWhite.Visibility = Visibility.Collapsed;
            BtnSwitch_Click(BtnSwitch, null);
            BtnExit.Foreground = Brushes.White;
            ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
            new Thread(() =>
            {
                Thread.Sleep(200);
                Application.Current.Dispatcher.Invoke(() => { isDisplayingOrHidingBlackboard = false; });
            }).Start();
        }

        /// <summary>
        /// 处理折叠浮动栏的鼠标点击事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <summary>
        /// 处理折叠浮动栏的鼠标弹起事件并触发折叠操作。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        public async void FoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await FoldFloatingBar(sender);
        }

        /// <summary>
        /// 折叠浮动栏，将其收纳到侧边栏。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="isAutoFoldCommand">是否为自动折叠命令。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查是否应该拒绝操作（如点击了折叠图标但上次鼠标按下的对象不是折叠图标）
        /// 2. 设置折叠/展开标志
        /// 3. 检查浮动栏是否已经折叠或正在改变隐藏模式，如果是则直接返回
        /// 4. 处理墨迹重放相关的UI元素
        /// 5. 设置浮动栏状态标志，关闭白板模式（如果当前在白板模式）
        /// 6. 如果是用户手动折叠且画布上有较多墨迹，显示通知
        /// 7. 清空画布墨迹
        /// 8. 隐藏PPT导航面板和浮动栏拖动网格
        /// 9. 执行浮动栏和侧边栏的动画
        /// 10. 如果开启了彻底隐藏，则隐藏主窗口
        /// <summary>
        /// 将浮动工具栏收纳到侧边栏并同步更新相关 UI 状态与动画。
        /// </summary>
        /// <param name="sender">触发折叠操作的来源控件（通常为折叠图标）；传入 null 表示由代码或自动逻辑触发。</param>
        /// <param name="isAutoFoldCommand">为 true 表示由自动折叠命令触发；为 false 表示用户发起的折叠。</param>
        /// <returns>表示折叠操作完成的任务。</returns>
        public async Task FoldFloatingBar(object sender, bool isAutoFoldCommand = false)
        {
            var isShouldRejectAction = false;

            await Dispatcher.InvokeAsync(() =>
            {
                if (lastBorderMouseDownObject != null && lastBorderMouseDownObject is Panel)
                    ((Panel)lastBorderMouseDownObject).Background = new SolidColorBrush(Colors.Transparent);
                if (sender == Fold_Icon && lastBorderMouseDownObject != Fold_Icon) isShouldRejectAction = true;
            });

            if (isShouldRejectAction) 
            {
                return;
            }

            // FloatingBarIcons_MouseUp_New(sender);
            if (sender == null)
                foldFloatingBarByUser = false;
            else
                foldFloatingBarByUser = true;
            unfoldFloatingBarByUser = false;

            if (isFloatingBarFolded) return;

            if (isFloatingBarChangingHideMode) 
            {
                return;
            }

            await Dispatcher.InvokeAsync(() =>
            {
                InkCanvasForInkReplay.Visibility = Visibility.Collapsed;
                InkCanvasGridForInkReplay.Visibility = Visibility.Visible;
                InkCanvasGridForInkReplay.IsHitTestVisible = true;
                FloatingbarUIForInkReplay.Visibility = Visibility.Visible;
                FloatingbarUIForInkReplay.IsHitTestVisible = true;
                BlackboardUIGridForInkReplay.Visibility = Visibility.Visible;
                BlackboardUIGridForInkReplay.IsHitTestVisible = true;
                AnimationsHelper.HideWithFadeOut(BorderInkReplayToolBox);
                isStopInkReplay = true;
            });

            await Dispatcher.InvokeAsync(() =>
            {
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = true;
                if (currentMode != 0) CloseWhiteboardImmediately();
                if (StackPanelCanvasControls.Visibility == Visibility.Visible)
                    if (foldFloatingBarByUser && inkCanvas.Strokes.Count > 2)
                        ShowNotification("正在清空墨迹并收纳至侧边栏，可进入批注模式后通过【撤销】功能来恢复原先墨迹。");
                lastBorderMouseDownObject = sender;
                CursorWithDelIcon_Click(sender, null);
            });

            await Task.Delay(300);

            await Dispatcher.InvokeAsync(() =>
            {
                LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                GridForFloatingBarDraging.Visibility = Visibility.Collapsed;
                ViewboxFloatingBarMarginAnimation(-60);
                HideSubPanels("cursor");
                SidePannelMarginAnimation(-10);
            });

            // 新增：如果开启了彻底隐藏，则隐藏主窗口
            if (Settings.Automation.ThoroughlyHideWhenFolded)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                });
            }
        }

        /// <summary>
        /// 处理左侧展开按钮显示快捷面板的鼠标点击事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查是否显示快捷面板
        /// 2. 如果显示快捷面板，则隐藏右侧快捷面板，显示左侧快捷面板并执行动画
        /// 3. 否则，调用展开浮动栏的方法
        /// <summary>
        /// 在左侧“展开”按钮上松开鼠标时处理点击：当快速面板可见时显示左侧快速面板并隐藏右侧快速面板（带入场边距动画），否则触发折叠栏的展开逻辑。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">与鼠标按键相关的事件数据。</param>
        private async void LeftUnFoldButtonDisplayQuickPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Appearance.IsShowQuickPanel)
            {
                HideRightQuickPanel();
                LeftUnFoldButtonQuickPanel.Visibility = Visibility.Visible;
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = new Thickness(-50, 0, 0, -150),
                        To = new Thickness(-1, 0, 0, -150)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    LeftUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() =>
                {
                    LeftUnFoldButtonQuickPanel.Margin = new Thickness(-1, 0, 0, -150);
                });
            }
            else
            {
                UnFoldFloatingBar_MouseUp(sender, e);
            }
        }

        /// <summary>
        /// 处理右侧展开按钮显示快捷面板的鼠标点击事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查是否显示快捷面板
        /// 2. 如果显示快捷面板，则隐藏左侧快捷面板，显示右侧快捷面板并执行动画
        /// 3. 否则，调用展开浮动栏的方法
        /// <summary>
        /// 处理右侧“展开并显示快速面板”按钮的鼠标释放事件：当启用快速面板时以滑入动画显示右侧快速面板，否则触发展开浮动栏的逻辑。
        /// </summary>
        /// <remarks>
        /// 如果 Settings.Appearance.IsShowQuickPanel 为 true，则隐藏左侧快速面板、显示右侧快速面板并执行短时的边距（滑入）动画；
        /// 否则调用 UnFoldFloatingBar_MouseUp 以展开浮动工具栏。
        /// </remarks>
        private async void RightUnFoldButtonDisplayQuickPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Appearance.IsShowQuickPanel)
            {
                HideLeftQuickPanel();
                RightUnFoldButtonQuickPanel.Visibility = Visibility.Visible;
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = new Thickness(0, 0, -50, -150),
                        To = new Thickness(0, 0, -1, -150)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    RightUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() =>
                {
                    RightUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, -1, -150);
                });
            }
            else
            {
                UnFoldFloatingBar_MouseUp(sender, e);
            }
        }

        /// <summary>
        /// 隐藏左侧快捷面板。
        /// </summary>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查左侧快捷面板是否可见，如果不可见则直接返回
        /// 2. 执行左侧快捷面板的隐藏动画
        /// 3. 等待动画完成后，设置左侧快捷面板的边距并将其折叠
        /// <summary>
        /// 以动画方式隐藏左侧快速面板（仅在面板当前可见时执行）。
        /// </summary>
        /// <remarks>
        /// 完成滑出动画后将面板的 Margin 调整为折叠位置并将 Visibility 设置为 Collapsed。
        /// </remarks>
        private async void HideLeftQuickPanel()
        {
            if (LeftUnFoldButtonQuickPanel.Visibility == Visibility.Visible)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = new Thickness(-1, 0, 0, -150),
                        To = new Thickness(-50, 0, 0, -150)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    LeftUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() =>
                {
                    LeftUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, -50, -150);
                    LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        /// <summary>
        /// 隐藏右侧快捷面板。
        /// </summary>
        /// <remarks>
        /// 操作包括：
        /// 1. 检查右侧快捷面板是否可见，如果不可见则直接返回
        /// 2. 执行右侧快捷面板的隐藏动画
        /// 3. 等待动画完成后，设置右侧快捷面板的边距并将其折叠
        /// <summary>
        /// 隐藏右侧快速面板（如果当前可见），通过短动画将其移出视图并在动画完成后将其折叠。
        /// </summary>
        /// <remarks>
        /// 当 RightUnFoldButtonQuickPanel 的 Visibility 为 Visible 时，执行约 100 毫秒的边距动画将控件向右移出，并在动画结束后将其 Margin 设置为目标值并将 Visibility 设为 Collapsed。
        /// </remarks>
        private async void HideRightQuickPanel()
        {
            if (RightUnFoldButtonQuickPanel.Visibility == Visibility.Visible)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    var marginAnimation = new ThicknessAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1),
                        From = new Thickness(0, 0, -1, -150),
                        To = new Thickness(0, 0, -50, -150)
                    };
                    marginAnimation.EasingFunction = new CubicEase();
                    RightUnFoldButtonQuickPanel.BeginAnimation(MarginProperty, marginAnimation);
                });
                await Task.Delay(100);

                await Dispatcher.InvokeAsync(() =>
                {
                    RightUnFoldButtonQuickPanel.Margin = new Thickness(0, 0, -50, -150);
                    RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        /// <summary>
        /// 处理隐藏快捷面板的鼠标点击事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">鼠标按钮事件参数。</param>
        /// <remarks>
        /// 操作包括：
        /// 1. 隐藏左侧快捷面板
        /// 2. 隐藏右侧快捷面板
        /// <summary>
        /// 在鼠标抬起时隐藏左侧和右侧的快速面板。
        /// </summary>
        private void HideQuickPanel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            HideLeftQuickPanel();
            HideRightQuickPanel();
        }

        /// <summary>
        /// 处理展开浮动栏的鼠标点击事件。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <summary>
        /// 在鼠标左键/右键松开时触发，展开已折叠的悬浮工具栏并恢复相关面板显示状态。
        /// </summary>
        /// <param name="sender">触发事件的控件（通常为折叠/展开按钮）。</param>
        /// <param name="e">鼠标按钮事件参数，包含有关按键和位置的信息。</param>
        public async void UnFoldFloatingBar_MouseUp(object sender, MouseButtonEventArgs e)
        {
            await UnFoldFloatingBar(sender);
        }

        /// <summary>
        /// 展开浮动栏，将其从侧边栏恢复到正常状态。
        /// </summary>
        /// <param name="sender">事件发送者。</param>
        /// <returns>表示异步操作的任务。</returns>
        /// <remarks>
        /// 操作包括：
        /// 1. 如果之前彻底隐藏了主窗口，先恢复显示
        /// 2. 隐藏左右侧快捷面板
        /// 3. 设置展开/折叠标志
        /// 4. 检查浮动栏是否正在改变隐藏模式，如果是则直接返回
        /// 5. 设置浮动栏状态标志，标记为未折叠
        /// 6. 根据设置决定是否自动切换至批注模式
        /// 7. 根据PPT放映模式和设置显示或隐藏翻页按钮
        /// 8. 在屏幕模式下显示浮动栏并执行动画
        /// 9. 执行侧边栏动画
        /// 10. 等待UI完全更新后，重新设置当前选中模式的按钮高亮状态
        /// <summary>
        /// 将折叠的浮动栏还原为展开状态并更新相关 UI 可见性、边距动画和高亮位置。
        /// </summary>
        /// <param name="sender">触发展开的来源；为 null 表示非用户直接触发（例如自动展开或程序调用）。</param>
        /// <returns>表示展开操作及其 UI 更新完成的任务。</returns>
        /// <remarks>
        /// 会根据应用设置自动切换到批注模式（如启用）、恢复被彻底隐藏的窗体、显示或隐藏 PPT 导航控件、执行浮动栏与侧边面板的边距/显示动画，并在动画结束后尝试重置浮动栏按钮的高亮状态。该方法会在正在进行折叠/展开变更时立即返回以避免并发冲突。
        /// </remarks>
        public async Task UnFoldFloatingBar(object sender)
        {
            // 新增：如果之前彻底隐藏了，先恢复显示
            if (this.Visibility != Visibility.Visible)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                });
            }

            await Dispatcher.InvokeAsync(() =>
            {
                LeftUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
                RightUnFoldButtonQuickPanel.Visibility = Visibility.Collapsed;
            });
            if (sender == null || StackPanelPPTControls.Visibility == Visibility.Visible)
                unfoldFloatingBarByUser = false;
            else
                unfoldFloatingBarByUser = true;
            foldFloatingBarByUser = false;

            if (isFloatingBarChangingHideMode) 
            {
                return;
            }

            
            await Dispatcher.InvokeAsync(() =>
            {
                isFloatingBarChangingHideMode = true;
                isFloatingBarFolded = false;
            });

            await Task.Delay(0);

            await Dispatcher.InvokeAsync(() =>
            {
                // 根据设置决定是否自动切换至批注模式
                if (Settings.Automation.IsAutoEnterAnnotationModeWhenExitFoldMode && currentMode == 0)
                {
                    // 切换至批注模式
                    PenIcon_Click(null, null);
                }

                // 只有在PPT放映模式下且页数有效时才显示翻页按钮
                if (StackPanelPPTControls.Visibility == Visibility.Visible &&
                    BtnPPTSlideShowEnd.Visibility == Visibility.Visible &&
                    PPTManager?.IsInSlideShow == true &&
                    PPTManager?.SlidesCount > 0)
                {
                    var dops = Settings.PowerPointSettings.PPTButtonsDisplayOption.ToString();
                    var dopsc = dops.ToCharArray();
                    if (dopsc[0] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(LeftBottomPanelForPPTNavigation);
                    if (dopsc[1] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(RightBottomPanelForPPTNavigation);
                    if (dopsc[2] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(LeftSidePanelForPPTNavigation);
                    if (dopsc[3] == '2' && !isDisplayingOrHidingBlackboard) AnimationsHelper.ShowWithFadeIn(RightSidePanelForPPTNavigation);
                }
                else
                {
                    // 如果条件不满足，确保隐藏翻页按钮
                    LeftBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    RightBottomPanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    LeftSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                    RightSidePanelForPPTNavigation.Visibility = Visibility.Collapsed;
                }

                // 新只在屏幕模式下显示浮动栏
                if (currentMode == 0)
                {
                    // 强制更新布局以确保ActualWidth正确
                    ViewboxFloatingBar.UpdateLayout();

                    // 等待一小段时间让布局完全更新
                    Task.Delay(50);

                    // 再次强制更新布局
                    ViewboxFloatingBar.UpdateLayout();

                    // 强制重新测量和排列
                    ViewboxFloatingBar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    ViewboxFloatingBar.Arrange(new Rect(ViewboxFloatingBar.DesiredSize));

                    if (BtnPPTSlideShowEnd.Visibility == Visibility.Visible)
                        ViewboxFloatingBarMarginAnimation(60);
                    else
                        ViewboxFloatingBarMarginAnimation(100, true);
                }
                SidePannelMarginAnimation(-50, !unfoldFloatingBarByUser);
            });

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // 等待UI完全更新
                    await Task.Delay(100);

                    // 获取当前选中的模式并重新设置高光位置
                    string selectedToolMode = GetCurrentSelectedMode();
                    if (!string.IsNullOrEmpty(selectedToolMode))
                    {
                        SetFloatingBarHighlightPosition(selectedToolMode);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.WriteLogToFile($"浮动栏展开后重新设置按钮高亮状态失败: {ex.Message}", LogHelper.LogType.Error);
                }
            });

        }

        /// <summary>
        /// 执行侧边栏边距动画，用于折叠或展开侧边栏。
        /// </summary>
        /// <param name="MarginFromEdge">侧边栏距边缘的边距值。可能的值：-50（完全折叠）, -10（半展开）</param>
        /// <param name="isNoAnimation">是否禁用动画效果。</param>
        /// <remarks>
        /// 操作包括：
        /// 1. 如果边距值为-10（半展开），则显示左侧边栏
        /// 2. 创建并执行左侧边栏的边距动画
        /// 3. 创建并执行右侧边栏的边距动画
        /// 4. 等待600毫秒让动画完成
        /// 5. 直接设置侧边栏的最终边距值
        /// 6. 如果边距值为-50（完全折叠），则隐藏左侧边栏
        /// 7. 重置浮动栏正在改变隐藏模式的标志为false
        /// <summary>
        /// 对左右侧边面板执行边距动画以折叠或展开浮动工具栏。
        /// </summary>
        /// <param name="MarginFromEdge">目标边距值（例如 -50 表示完全折叠，-10 表示展开到可见位置）。</param>
        /// <param name="isNoAnimation">为 true 时立即应用最终边距而不执行过渡动画。</param>
        private async void SidePannelMarginAnimation(int MarginFromEdge, bool isNoAnimation = false) // Possible value: -50, -10
        {
            await Dispatcher.InvokeAsync(() =>
            {
                if (MarginFromEdge == -10) LeftSidePanel.Visibility = Visibility.Visible;

                var LeftSidePanelmarginAnimation = new ThicknessAnimation
                {
                    Duration = isNoAnimation ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(0.175),
                    From = LeftSidePanel.Margin,
                    To = new Thickness(MarginFromEdge, 0, 0, -150)
                };
                LeftSidePanelmarginAnimation.EasingFunction = new CubicEase();
                var RightSidePanelmarginAnimation = new ThicknessAnimation
                {
                    Duration = isNoAnimation ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(0.175),
                    From = RightSidePanel.Margin,
                    To = new Thickness(0, 0, MarginFromEdge, -150)
                };
                RightSidePanelmarginAnimation.EasingFunction = new CubicEase();
                LeftSidePanel.BeginAnimation(MarginProperty, LeftSidePanelmarginAnimation);
                RightSidePanel.BeginAnimation(MarginProperty, RightSidePanelmarginAnimation);
            });

            await Task.Delay(600);

            await Dispatcher.InvokeAsync(() =>
            {
                LeftSidePanel.Margin = new Thickness(MarginFromEdge, 0, 0, -150);
                RightSidePanel.Margin = new Thickness(0, 0, MarginFromEdge, -150);

                if (MarginFromEdge == -50) LeftSidePanel.Visibility = Visibility.Collapsed;
            });
            isFloatingBarChangingHideMode = false;
        }
    }
}