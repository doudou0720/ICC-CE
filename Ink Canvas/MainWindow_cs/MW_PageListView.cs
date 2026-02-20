using Ink_Canvas.Helpers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace Ink_Canvas
{
    public partial class MainWindow : Window
    {
        private class PageListViewItem
        {
            public int Index { get; set; }
            public StrokeCollection Strokes { get; set; }
        }

        ObservableCollection<PageListViewItem> blackBoardSidePageListViewObservableCollection = new ObservableCollection<PageListViewItem>();

        /// <summary>
        /// 刷新白板的缩略图页面列表，更新左右侧缩略页列表，使其与当前白板页及历史快照一致，并将左右列表的选中项同步到当前白板页。
        /// </summary>
        /// <remarks>
        /// 为每页生成或更新对应的 PageListViewItem（通过应用时间线历史并裁剪到画布边界），用当前画布的笔迹替换当前页的条目，并将两个侧边 ListView 的 SelectedIndex 设置为当前白板索引 - 1。
        /// <summary>
        /// 更新两侧缩略页列表，使其反映当前白板页及其历史快照，并同步选中页。
        /// </summary>
        /// <remarks>
        /// 根据 WhiteboardTotalCount 和 TimeMachineHistories 构建或替换 blackBoardSidePageListViewObservableCollection 中的项；
        /// 为每页生成代表该页笔迹的缩略 StrokeCollection（按画布边界裁剪），并将当前画布的实时笔迹替换为当前页的缩略表示；
        /// 最后将左、右侧的列表视图选中索引同步为 CurrentWhiteboardIndex - 1。
        /// </remarks>
        private void RefreshBlackBoardSidePageListView()
        {
            if (blackBoardSidePageListViewObservableCollection.Count == WhiteboardTotalCount)
            {
                foreach (int index in Enumerable.Range(1, WhiteboardTotalCount))
                {
                    var st = ApplyHistoriesToNewStrokeCollection(TimeMachineHistories[index]);
                    st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
                    var pitem = new PageListViewItem
                    {
                        Index = index,
                        Strokes = st,
                    };
                    blackBoardSidePageListViewObservableCollection[index - 1] = pitem;
                }
            }
            else
            {
                blackBoardSidePageListViewObservableCollection.Clear();
                foreach (int index in Enumerable.Range(1, WhiteboardTotalCount))
                {
                    var st = ApplyHistoriesToNewStrokeCollection(TimeMachineHistories[index]);
                    st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
                    var pitem = new PageListViewItem
                    {
                        Index = index,
                        Strokes = st,
                    };
                    blackBoardSidePageListViewObservableCollection.Add(pitem);
                }
            }

            var _st = inkCanvas.Strokes.Clone();
            _st.Clip(new Rect(0, 0, (int)inkCanvas.ActualWidth, (int)inkCanvas.ActualHeight));
            var _pitem = new PageListViewItem
            {
                Index = CurrentWhiteboardIndex,
                Strokes = _st,
            };
            blackBoardSidePageListViewObservableCollection[CurrentWhiteboardIndex - 1] = _pitem;

            BlackBoardLeftSidePageListView.SelectedIndex = CurrentWhiteboardIndex - 1;
            BlackBoardRightSidePageListView.SelectedIndex = CurrentWhiteboardIndex - 1;
        }

        /// <summary>
        /// 根据传入相对于 <paramref name="scrollViewer"/> 的点，查找并选中列表中对应的缩略图项；在需要时切换当前白板页并更新画布状态与左右侧缩略图选择状态。
        /// </summary>
        /// <param name="listView">承载页面缩略图的 ListView。</param>
        /// <param name="scrollViewer">包含该 ListView 的 ScrollViewer，用于将触点坐标从滚动视图坐标系转换到 ListView。</param>
        /// <param name="pointInScrollViewer">相对于 <paramref name="scrollViewer"/> 的触点坐标（用于命中测试）。</param>
        /// <remarks>
        /// - 如果命中到 ListViewItem，会隐藏左右侧页面边框、在必要时保存/清空/恢复画笔笔迹并更新 CurrentWhiteboardIndex 与显示信息；还会将左右两侧 ListView 的 SelectedIndex 同步为命中项索引。 
        /// - 在查找命中或切换过程中发生的异常将被捕获并忽略，不会向上抛出。
        /// <summary>
        /// 根据 ScrollViewer 中的触点坐标查找对应的缩略图项并切换到该白板页，同时同步左右缩略图的选中项。
        /// </summary>
        /// <param name="listView">包含白板缩略图项的目标 ListView。</param>
        /// <param name="scrollViewer">触点坐标所基于的 ScrollViewer。</param>
        /// <param name="pointInScrollViewer">以 <paramref name="scrollViewer"/> 坐标系表示的触点位置。</param>
        /// <remarks>
        /// 如果找到命中的缩略图且其序号与当前白板页不同，则保存并清除当前画布笔迹、切换至目标页并恢复对应笔迹；无论是否切换，都会将左右侧缩略图列表的 SelectedIndex 同步到命中的项。方法会捕获并忽略命中测试或切换过程中的异常，因此不会向上抛出这些错误。
        /// </remarks>
        private void TrySwitchWhiteboardPageByTouchPoint(ListView listView, ScrollViewer scrollViewer, Point pointInScrollViewer)
        {
            if (listView == null || scrollViewer == null) return;
            try
            {
                var transform = scrollViewer.TransformToVisual(listView);
                if (transform == null) return;
                var pointInListView = transform.Transform(pointInScrollViewer);
                var hit = VisualTreeHelper.HitTest(listView, pointInListView);
                if (hit?.VisualHit == null) return;
                var container = FindAncestorOfType<ListViewItem>(hit.VisualHit);
                if (container == null) return;
                int index = listView.ItemContainerGenerator.IndexFromContainer(container);
                if (index < 0 || index >= blackBoardSidePageListViewObservableCollection.Count) return;
                var item = blackBoardSidePageListViewObservableCollection[index];
                if (item == null) return;
                AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
                AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
                if (index + 1 != CurrentWhiteboardIndex)
                {
                    if (currentSelectedElement != null)
                    {
                        var previousEditingMode = inkCanvas.EditingMode;
                        UnselectElement(currentSelectedElement);
                        inkCanvas.EditingMode = previousEditingMode;
                        currentSelectedElement = null;
                    }
                    SaveStrokes();
                    ClearStrokes(true);
                    CurrentWhiteboardIndex = index + 1;
                    RestoreStrokes();
                    UpdateIndexInfoDisplay();
                }
                BlackBoardLeftSidePageListView.SelectedIndex = index;
                BlackBoardRightSidePageListView.SelectedIndex = index;
            }
            catch
            {
                // 忽略命中测试或切换过程中的异常
            }
        }

        /// <summary>
        /// 在视觉树中自下而上查找并返回第一个匹配指定类型的祖先元素。
        /// </summary>
        /// <typeparam name="T">要查找的祖先类型，必须继承自 <see cref="DependencyObject"/>。</typeparam>
        /// <param name="current">起始节点；从此节点开始向上遍历视觉树。</param>
        /// <summary>
        /// 在视觉树中自给定节点向上遍历并返回第一个匹配类型 <typeparamref name="T"/> 的祖先元素。
        /// </summary>
        /// <typeparam name="T">要查找的祖先类型。</typeparam>
        /// <param name="current">搜索起始节点（从此节点开始向上遍历）。</param>
        /// <returns>找到的第一个类型为 <typeparamref name="T"/> 的祖先；未找到时返回 <c>null</c>。</returns>
        private static T FindAncestorOfType<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T found) return found;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        /// <summary>
        /// 将指定元素在给定 ScrollViewer 中滚动，使该元素与可视区域的顶部对齐。
        /// </summary>
        /// <param name="element">要对齐到顶部的元素。</param>
        /// <summary>
        /// 将指定元素在给定 ScrollViewer 中垂直滚动到视口顶端，使该元素的顶部与 ScrollViewer 的可视区域顶部对齐。
        /// </summary>
        /// <param name="element">要对齐到顶端的子元素。</param>
        /// <param name="scrollViewer">包含该元素的目标 ScrollViewer。</param>
        /// <remarks>当任一参数为 null 或无法获取到元素到 ScrollViewer 的变换时，不执行任何操作。</remarks>
        public static void ScrollViewToVerticalTop(FrameworkElement element, ScrollViewer scrollViewer)
        {
            if (element == null || scrollViewer == null)
            {
                return;
            }

            var scrollViewerOffset = scrollViewer.VerticalOffset;
            var point = new Point(0, scrollViewerOffset);
            var transform = element.TransformToVisual(scrollViewer);
            if (transform == null)
            {
                return;
            }

            var tarPos = transform.Transform(point);
            scrollViewer.ScrollToVerticalOffset(tarPos.Y);
        }


        private void BlackBoardLeftSidePageListView_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            var item = BlackBoardLeftSidePageListView.SelectedItem;
            var index = BlackBoardLeftSidePageListView.SelectedIndex;
            if (item != null)
            {
                // 只有当选择的页面与当前页面不同时才进行切换
                if (index + 1 != CurrentWhiteboardIndex)
                {
                    // 隐藏图片选择工具栏
                    if (currentSelectedElement != null)
                    {
                        // 保存当前编辑模式
                        var previousEditingMode = inkCanvas.EditingMode;
                        UnselectElement(currentSelectedElement);
                        // 恢复编辑模式
                        inkCanvas.EditingMode = previousEditingMode;
                        currentSelectedElement = null;
                    }

                    SaveStrokes();
                    ClearStrokes(true);
                    CurrentWhiteboardIndex = index + 1;
                    RestoreStrokes();
                    UpdateIndexInfoDisplay();
                }
                // 无论是否切换页面，都更新选择索引
                BlackBoardLeftSidePageListView.SelectedIndex = index;
            }
        }

        /// <summary>
        /// 处理右侧页面缩略图的鼠标抬起事件并在需要时切换当前白板页。
        /// </summary>
        /// <remarks>
        /// 隐藏左右侧页面边框视觉效果；若所选缩略图对应的页码与当前白板页不同，则保存当前笔迹、清空画布、切换到目标页并恢复该页笔迹与索引显示；无论是否切换，都会将列表的 SelectedIndex 同步为当前选择索引。若有正在编辑的元素，会先取消选中并在取消后恢复画布的编辑模式。
        /// </remarks>
        /// <param name="sender">事件源（通常为 ListView）。</param>
        /// <param name="e">鼠标事件参数。</param>
        private void BlackBoardRightSidePageListView_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            AnimationsHelper.HideWithSlideAndFade(BoardBorderLeftPageListView);
            AnimationsHelper.HideWithSlideAndFade(BoardBorderRightPageListView);
            var item = BlackBoardRightSidePageListView.SelectedItem;
            var index = BlackBoardRightSidePageListView.SelectedIndex;
            if (item != null)
            {
                // 只有当选择的页面与当前页面不同时才进行切换
                if (index + 1 != CurrentWhiteboardIndex)
                {
                    // 隐藏图片选择工具栏
                    if (currentSelectedElement != null)
                    {
                        // 保存当前编辑模式
                        var previousEditingMode = inkCanvas.EditingMode;
                        UnselectElement(currentSelectedElement);
                        // 恢复编辑模式
                        inkCanvas.EditingMode = previousEditingMode;
                        currentSelectedElement = null;
                    }

                    SaveStrokes();
                    ClearStrokes(true);
                    CurrentWhiteboardIndex = index + 1;
                    RestoreStrokes();
                    UpdateIndexInfoDisplay();
                }
                // 无论是否切换页面，都更新选择索引
                BlackBoardRightSidePageListView.SelectedIndex = index;
            }
        }

    }
}