namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투의 현재 페이즈를 나타내는 열거형.
    /// TurnManager의 상태 머신에서 사용된다.
    /// </summary>
    public enum BattlePhase
    {
        /// <summary>페이즈 없음 (전투 미시작)</summary>
        None,
        /// <summary>전투 시작 (덱 구축, 초기화)</summary>
        BattleStart,
        /// <summary>턴 시작 (드로우, 에너지 회복, AI 결정)</summary>
        TurnStart,
        /// <summary>플레이어 행동 (카드 사용)</summary>
        PlayerAction,
        /// <summary>적 행동 (AI 결정 실행)</summary>
        EnemyAction,
        /// <summary>턴 종료 (상태이상 처리, 승패 체크)</summary>
        TurnEnd,
        /// <summary>웨이브 전환 (다음 웨이브 적 생성)</summary>
        WaveTransition,
        /// <summary>전투 종료</summary>
        BattleEnd
    }

    /// <summary>
    /// 전투 결과.
    /// </summary>
    public enum BattleResult
    {
        /// <summary>미확정</summary>
        None,
        /// <summary>승리</summary>
        Victory,
        /// <summary>패배</summary>
        Defeat
    }

    /// <summary>
    /// 전투 종료 사유.
    /// </summary>
    public enum BattleEndReason
    {
        /// <summary>적 유닛 전멸</summary>
        AllEnemiesDown,
        /// <summary>아군 유닛 전멸</summary>
        AllAlliesDown,
        /// <summary>플레이어 항복</summary>
        Surrender,
        /// <summary>이벤트에 의한 강제 종료</summary>
        EventTriggered
    }
}
