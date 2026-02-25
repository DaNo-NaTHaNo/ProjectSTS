namespace ProjectStS.Data
{
    /// <summary>
    /// 구역의 방향.
    /// </summary>
    public enum CardinalPoint
    {
        N,
        NE,
        NW,
        S,
        SE,
        SW
    }

    /// <summary>
    /// 스테이지 진행 페이즈.
    /// </summary>
    public enum StagePhase
    {
        /// <summary>초기 상태</summary>
        None,
        /// <summary>스테이지 초기화 중</summary>
        Initializing,
        /// <summary>탐험 진행 중 (플레이어 이동 가능)</summary>
        Exploration,
        /// <summary>이벤트 실행 중 (전투, VN 등)</summary>
        EventExecuting,
        /// <summary>귀환 중 (스테이지 종료 전환)</summary>
        Returning,
        /// <summary>스테이지 종료</summary>
        Ended
    }

    /// <summary>
    /// 스테이지 결과.
    /// </summary>
    public enum StageResult
    {
        /// <summary>결과 미결정</summary>
        None,
        /// <summary>승리 (보스전 완료, 캠페인 목표 달성, 행동력 소진 귀환, 이벤트 탈출)</summary>
        Victory,
        /// <summary>실패 (파티 전멸, 이벤트 실패)</summary>
        Failure,
        /// <summary>중도 귀환 (이벤트 탈출 등)</summary>
        Retreat
    }

    /// <summary>
    /// 스테이지 종료 사유.
    /// </summary>
    public enum StageEndReason
    {
        /// <summary>사유 없음</summary>
        None,
        /// <summary>보스전 완료</summary>
        BossCleared,
        /// <summary>캠페인 주 목표 달성</summary>
        CampaignGoal,
        /// <summary>행동력 고갈로 인한 귀환</summary>
        APDepleted,
        /// <summary>탈출 이벤트를 통한 중도 귀환</summary>
        EventEscape,
        /// <summary>아군 파티 전멸</summary>
        PartyWipe,
        /// <summary>특정 이벤트 실패</summary>
        EventFailed
    }
}
