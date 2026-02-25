namespace ProjectStS.Data
{
    /// <summary>
    /// 적 AI의 행동 타입.
    /// </summary>
    public enum AIActionType
    {
        /// <summary>덱에 없던 카드 추가</summary>
        EarnCard,
        /// <summary>덱의 특정 카드를 사용</summary>
        PlayCard,
        /// <summary>행동하지 않음</summary>
        Pass
    }

    /// <summary>
    /// AI 컨디션 판단 타입.
    /// </summary>
    public enum AIConditionType
    {
        /// <summary>현재 턴이 ~일 때</summary>
        TurnCount,
        /// <summary>~턴마다</summary>
        TurnMod,
        /// <summary>HP가 ~%일 때</summary>
        HpPercent,
        /// <summary>플레이어의 HP가 ~%일 때</summary>
        EnemyHpPercent,
        /// <summary>특정 카드를 보유했을 때</summary>
        HasCard,
        /// <summary>특정 상태이상이 적용되어 있을 때</summary>
        StatusActive
    }
}
