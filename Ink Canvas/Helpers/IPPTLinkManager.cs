using System;
using Microsoft.Office.Interop.PowerPoint;

namespace Ink_Canvas.Helpers
{
    public interface IPPTLinkManager : IDisposable
    {
        event Action<object> SlideShowBegin;
        event Action<object> SlideShowNextSlide;
        event Action<object> SlideShowEnd;
        event Action<object> PresentationOpen;
        event Action<object> PresentationClose;
        event Action<bool> PPTConnectionChanged;
        event Action<bool> SlideShowStateChanged;

        bool IsConnected { get; }
        bool IsInSlideShow { get; }
        bool IsSupportWPS { get; set; }
        int SlidesCount { get; }

        object PPTApplication { get; }

        /// <summary>
/// 开始监视 PowerPoint 的连接状态与播放/演示状态，并在相关状态变化时触发接口定义的事件。
/// </summary>
/// <remarks>
/// 监视将持续进行直到调用 StopMonitoring 或释放该管理器实例。
/// </remarks>
        void StartMonitoring();
        /// <summary>
/// 停止对 PowerPoint 连接与状态的监控，取消活动的监视并释放相关资源。
/// </summary>
void StopMonitoring();

        /// <summary>
/// 重新加载或刷新与 PowerPoint 的连接以尝试恢复或重建通信通道。
/// </summary>
/// <remarks>
/// 实现应在重建连接后更新相关状态并触发与连接变化相关的事件（例如 <c>PPTConnectionChanged</c>）。该方法不返回值，调用者应通过接口属性或事件判断最终结果。
/// </remarks>
void ReloadConnection();

        /// <summary>
/// 尝试将当前演示文稿切换到放映模式并开始放映。
/// </summary>
/// <returns>`true` 表示放映已成功开始，`false` 表示未能开始放映。</returns>
        bool TryStartSlideShow();
        /// <summary>
/// 尝试结束当前正在进行的幻灯放映。
/// </summary>
/// <returns>`true` 表示已成功结束幻灯放映，`false` 表示未能结束（例如没有活动的放映或操作失败）。</returns>
bool TryEndSlideShow();

        // 导航控制
        bool TryNavigateToSlide(int slideNumber);
        bool TryNavigateNext();
        bool TryNavigatePrevious();

        // 查询
        int GetCurrentSlideNumber();
        string GetPresentationName();
        bool TryShowSlideNavigation();
        object GetCurrentActivePresentation();
    }
}

