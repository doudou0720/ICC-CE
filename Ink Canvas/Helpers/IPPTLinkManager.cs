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
/// 开始监视 PowerPoint 应用的连接与幻灯片放映状态。
/// </summary>
/// <remarks>
/// 启用后接口会在连接或放映状态变化时触发相应事件（例如 <c>PPTConnectionChanged</c>、<c>SlideShowStateChanged</c>、<c>SlideShowBegin</c> 等）。
/// </remarks>
        void StartMonitoring();
        /// <summary>
/// 停止监视 PowerPoint 应用的连接状态和幻灯片放映状态，暂停与监控相关的事件触发和资源使用。
/// </summary>
void StopMonitoring();

        /// <summary>
/// 重新建立或刷新与 PowerPoint 应用的连接。
/// </summary>
/// <remarks>
/// 调用此方法应使实现尝试恢复或重置与 PPT 的通信状态，以便在连接中断或需要重连时恢复正常交互。
/// </remarks>
void ReloadConnection();

        /// <summary>
/// 尝试启动当前演示文稿的放映模式。
/// </summary>
/// <returns>`true` 表示放映已成功启动，`false` 表示未能启动放映。</returns>
        bool TryStartSlideShow();
        /// <summary>
/// 尝试结束当前的幻灯片放映。
/// </summary>
/// <returns>`true` 如果成功结束放映，`false` 否则。</returns>
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

