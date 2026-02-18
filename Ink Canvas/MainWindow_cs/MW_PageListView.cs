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
        /// <para>刷新白板的缩略图页面列表。</para>
        /// <summary>
        /// 重新构建并刷新黑板左/右侧的页面缩略列表项集合及其选中状态。
        /// </summary>
        /// <remarks>
        /// 对比当前集合长度与白板总页数：若相等则逐项替换，否则清空并重新添加所有页的缩略项。每个缩略项由应用历史记录生成的笔迹集合构成，且会被裁切到当前 inkCanvas 的可见区域。随后用当前画布的实时笔迹替换对应的当前页缩略项，并将左右侧列表的选中索引同步为当前白板页索引减一（基于 0 的列表索引）。
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
        /// 根据在滚动视图内的触点位置，通过缩略图命中检测切换到对应的白板页面并同步左右面板的选中状态。
        /// </summary>
        /// <param name="listView">展示页缩略图的 ListView 控件。</param>
        /// <param name="scrollViewer">包含该 ListView 的 ScrollViewer；触点坐标以此为参考系。</param>
        /// <param name="pointInScrollViewer">在 scrollViewer 坐标系下的触点位置，用于在 ListView 中进行命中检测。</param>
        /// <param name="isLeftSide">指示触发来源是否为左侧黑板（用于调用上下文区分），不改变方法的切换逻辑。</param>
        private void TrySwitchWhiteboardPageByTouchPoint(ListView listView, ScrollViewer scrollViewer, Point pointInScrollViewer, bool isLeftSide)
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
        /// 在可视树中向上遍历并查找第一个类型为 <typeparamref name="T"/> 的父级元素。
        /// </summary>
        /// <typeparam name="T">要查找的父级元素类型，必须派生自 <see cref="DependencyObject"/>。</typeparam>
        /// <param name="current">起始节点；从此节点开始向上搜索其父级。</param>
        /// <returns>找到的第一个类型为 <typeparamref name="T"/> 的祖先元素；未找到时返回 <c>null</c>。</returns>
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
        /// 将指定元素的顶部滚动到 ScrollViewer 当前的垂直可见区域顶部位置。
        /// </summary>
        /// <param name="element">位于 ScrollViewer 中的目标元素，其顶部将与 ScrollViewer 的当前垂直偏移对齐。</param>
        /// <param name="scrollViewer">目标 ScrollViewer。</param>
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