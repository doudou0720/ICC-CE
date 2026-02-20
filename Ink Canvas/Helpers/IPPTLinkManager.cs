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
        /// 开始监视与 PowerPoint 的连接以及幻灯片放映相关状态，并在状态变化时触发对应事件。
        /// <summary>
/// 开始监视与 PowerPoint 的连接及放映状态，并在连接状态、放映开始/结束、幻灯片切换或演示文稿打开/关闭等变化时触发对应事件。
/// </summary>
        void StartMonitoring();
        /// <summary>
        /// 停止监控 PowerPoint 的连接与事件，停止接收并处理与演示文稿和幻灯放映相关的通知。
        /// <summary>
/// 停止监视 PowerPoint 的连接与放映相关状态，停止接收并触发与演示和放映相关的通知事件。
/// </summary>
        void StopMonitoring();

        /// <summary>
        /// 重新加载或重建与 PowerPoint 的连接。
        /// </summary>
        /// <remarks>
        /// 调用后实现应刷新内部连接与状态，必要时重建与 PowerPoint 的会话；此操作可能导致 IsConnected 变化并触发 PPTConnectionChanged 或其他相关事件（例如 SlideShowStateChanged）。
        /// <summary>
/// 重新建立或刷新与 PowerPoint 的内部连接和状态。
/// </summary>
/// <remarks>
/// 调用后会重建或更新与 PowerPoint 应用和当前演示文稿的会话/引用，刷新内部状态。此操作可能导致 IsConnected 或 IsInSlideShow 的值发生变化，并触发相应的 PPTConnectionChanged 或 SlideShowStateChanged 事件。
/// </remarks>
        void ReloadConnection();

        /// <summary>
        /// 尝试启动当前演示文稿的放映模式。
        /// </summary>
        /// <summary>
/// 尝试将当前演示文稿切换到放映（幻灯片播放）模式。
/// </summary>
/// <returns>`true` 如果放映已成功启动，`false` 否则。</returns>
        bool TryStartSlideShow();
        /// <summary>
        /// 尝试结束当前正在进行的幻灯片放映。
        /// </summary>
        /// <summary>
/// 尝试结束当前演示的幻灯片放映。
/// </summary>
/// <returns>`true` 表示放映已成功结束，`false` 表示未能结束放映。</returns>
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
