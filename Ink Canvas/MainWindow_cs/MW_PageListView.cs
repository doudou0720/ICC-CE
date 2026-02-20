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
        /// 刷新侧边白板页缩略图集合并同步当前页的缩略图与选中索引。
        /// </summary>
        /// <remarks>
        /// 该方法确保侧边页集合与 WhiteboardTotalCount 一致，为每页生成并裁剪到画布尺寸的笔画缩略数据，
        /// 将当前白板页的实时笔画快照更新到集合对应位置，并将左右侧的 ListView 选中项设置为当前页索引。
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
        /// 根据在滚动视图中的触控点切换白板页面（如果触控命中某个缩略项）。 
        /// </summary>
        /// <param name="listView">承载白板页面缩略项的 ListView，用于命中检测和索引查找。</param>
        /// <param name="scrollViewer">包含触控点的 ScrollViewer，用于将点坐标转换到 ListView 坐标系。</param>
        /// <param name="pointInScrollViewer">相对于 <paramref name="scrollViewer"/> 的触控点坐标。</param>
        /// <remarks>
        /// 在命中某个缩略项时会隐藏左右侧缩略面板、保存并恢复当前编辑状态与笔迹、并同步左右缩略视图的选中项。方法内部会捕获并忽略命中检测或切换过程中的异常，不会抛出异常给调用方。
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
        /// 在视觉树中向上查找并返回第一个匹配类型 T 的祖先元素。
        /// </summary>
        /// <typeparam name="T">要查找的祖先类型，必须派生自 <see cref="DependencyObject"/>。</typeparam>
        /// <param name="current">起始节点；从该节点开始向上遍历其父节点。</param>
        /// <returns>找到的类型为 T 的最近祖先节点；未找到时返回 <c>null</c>。</returns>
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
        /// 将指定元素在给定 ScrollViewer 中滚动，使该元素的顶部与 ScrollViewer 可视区域的顶部对齐。
        /// 如果 <paramref name="element"/> 或 <paramref name="scrollViewer"/> 为 null，则不执行任何操作。
        /// </summary>
        /// <param name="element">需对齐到视图顶部的元素（相对于目标 <see cref="ScrollViewer"/>）。</param>
        /// <param name="scrollViewer">目标 <see cref="ScrollViewer"/>，将在其内部进行垂直滚动。</param>
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