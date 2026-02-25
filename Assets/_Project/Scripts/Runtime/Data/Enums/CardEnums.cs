namespace ProjectStS.Data
{
    /// <summary>
    /// 카드의 효과 포맷 형식.
    /// </summary>
    public enum CardType
    {
        Attack,
        Defend,
        StatusEffect,
        InHandEffect
    }

    /// <summary>
    /// 카드 효과의 종류.
    /// </summary>
    public enum CardEffectType
    {
        Damage,
        Block,
        Heal,
        Energy,
        /// <summary>캐릭터에게 상태이상 적용</summary>
        ApplyStatus,
        /// <summary>손패에 효과 적용</summary>
        ModifyCard,
        /// <summary>덱 탑을 드로우</summary>
        Draw,
        /// <summary>특정 카드를 손패에 추가</summary>
        AddCard
    }

    /// <summary>
    /// 카드 수치 변경 대상 타입.
    /// </summary>
    public enum ModificationType
    {
        Damage,
        Block,
        Cost,
        Duration,
        StatusStacks,
        TargetCount
    }

    /// <summary>
    /// 카드 수치 변경 지속 시간.
    /// </summary>
    public enum ModDuration
    {
        /// <summary>적용된 카드 사용 시까지</summary>
        UntilPlayed,
        /// <summary>전투 끝까지</summary>
        Combat
    }

    /// <summary>
    /// 수치 변경 카드 타겟 선정 규칙.
    /// </summary>
    public enum CardTargetSelection
    {
        /// <summary>플레이어가 손패에서 선택</summary>
        PlayerSelect,
        /// <summary>손패 랜덤</summary>
        RandomInHand,
        /// <summary>손패 전부</summary>
        AllInHand,
        /// <summary>손패의 특정 타입</summary>
        TypeInHand,
        /// <summary>손패의 특정 속성</summary>
        Element,
        /// <summary>덱 전체</summary>
        AllInDeck
    }
}
