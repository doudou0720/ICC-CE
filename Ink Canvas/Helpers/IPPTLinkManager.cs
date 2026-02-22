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
        bool SkipAnimationsWhenNavigating { get; set; }
        int SlidesCount { get; }

        object PPTApplication { get; }

        /// <summary>
        /// 开始监视与 PowerPoint 的连接以及幻灯片放映相关状态，并在状态变化时触发对应事件。
        /// <summary>
/// 开始监视 PowerPoint 的连接和幻灯片放映相关状态，并在这些状态变化时触发对应事件。
/// </summary>
/// <remarks>
/// 监视包括连接状态、演示文稿打开/关闭、幻灯放映开始/结束以及幻灯切换等；监视期间会更新 IsConnected、IsInSlideShow、SlidesCount 等属性并在发生变化时触发接口中定义的事件（例如 PPTConnectionChanged、PresentationOpen、SlideShowBegin、SlideShowNextSlide、SlideShowEnd、SlideShowStateChanged）。调用此方法后实现应开始接收并处理来自 PowerPoint 的状态通知直至 StopMonitoring 被调用或连接中断。
/// </remarks>
        void StartMonitoring();
        /// <summary>
        /// 停止监控 PowerPoint 的连接与事件，停止接收并处理与演示文稿和幻灯片放映相关的通知。
        /// <summary>
/// 停止监视 PowerPoint 的连接和幻灯片放映状态；取消订阅相关事件并停止触发与演示文稿和放映相关的通知。
/// </summary>
        void StopMonitoring();

        /// <summary>
        /// 重新加载或重建与 PowerPoint 的连接。
        /// </summary>
        /// <remarks>
        /// 调用后实现应刷新内部连接与状态，必要时重建与 PowerPoint 的会话；此操作可能导致 IsConnected 变化并触发 PPTConnectionChanged 或其他相关事件（例如 SlideShowStateChanged）。
        /// <summary>
/// 重新初始化或重建与 PowerPoint 的连接并刷新内部状态。
/// </summary>
/// <remarks>
/// 在需要恢复、重置或替换当前会话时调用；可能会更改 IsConnected 状态，并在连接或幻灯片放映状态发生变化时触发相应事件（例如 PPTConnectionChanged、SlideShowStateChanged、PresentationOpen/Close）。实现者应尽量保证在方法返回后内部状态一致并且事件已发出（如适用）。
/// </remarks>
        void ReloadConnection();

        /// <summary>
        /// 尝试启动当前演示文稿的放映模式。
        /// </summary>
        /// <summary>
/// 尝试启动当前打开的演示文稿的放映（幻灯片放映）模式。
/// </summary>
/// <returns>`true` 如果放映已成功启动，`false` 否则。</returns>
        bool TryStartSlideShow();
        /// <summary>
        /// 尝试结束当前正在进行的幻灯片放映。
        /// </summary>
        /// <summary>
/// 尝试结束当前正在进行的幻灯片放映。
/// </summary>
/// <returns><c>true</c> 如果当前存在正在进行的放映且已成功结束，<c>false</c> 否则。</returns>
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
