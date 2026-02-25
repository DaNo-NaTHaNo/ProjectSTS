using System;

namespace ProjectStS.Core
{
    /// <summary>
    /// 비주얼 노벨 시스템과의 연동 인터페이스.
    /// VisualNovelPlayer를 래핑하여 외부 시스템(GameFlowController 등)이
    /// 어셈블리 의존 없이 VN 재생을 요청할 수 있게 한다.
    /// </summary>
    public interface IVisualNovelBridge
    {
        /// <summary>
        /// 지정된 에피소드를 재생하고 완료 시 콜백을 호출한다.
        /// </summary>
        /// <param name="episodeId">재생할 에피소드 ID (이벤트의 eventValue)</param>
        /// <param name="onCompleted">재생 완료 시 호출될 콜백</param>
        void PlayEpisode(string episodeId, Action onCompleted);

        /// <summary>
        /// 현재 VN이 재생 중인지 여부.
        /// </summary>
        bool IsPlaying { get; }
    }
}
