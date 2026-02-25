namespace ProjectStS.Data
{
    /// <summary>
    /// 아이템의 사용 방식 타입.
    /// </summary>
    public enum ItemType
    {
        /// <summary>상시 효과 발동</summary>
        Equipment,
        /// <summary>장착 유닛이 대미지를 입었을 경우</summary>
        HasDamage,
        /// <summary>장착 유닛의 HP가 0이 될 경우</summary>
        HasDown
    }

    /// <summary>
    /// 아이템 효과를 적용할 대상 유닛.
    /// </summary>
    public enum ItemTargetUnit
    {
        Self,
        Player,
        AllParty,
        LowestHp
    }

    /// <summary>
    /// 아이템 효과를 적용할 스탯 종류.
    /// </summary>
    public enum ItemTargetStatus
    {
        /// <summary>행동력 최대치</summary>
        MaxAP,
        /// <summary>현재 행동력</summary>
        NowAP,
        /// <summary>HP 최대치</summary>
        MaxHP,
        /// <summary>현재 HP</summary>
        NowHP,
        /// <summary>에너지 최대치</summary>
        MaxEnergy,
        /// <summary>현재 에너지</summary>
        NowEnergy,
        /// <summary>손패 카드의 코스트</summary>
        CostInHand,
        /// <summary>상태이상 부여</summary>
        StatusEffect,
        /// <summary>손패에 카드 추가</summary>
        AddCard,
        /// <summary>덱에 카드 추가</summary>
        AddDeck
    }

    /// <summary>
    /// 아이템 소모 조건.
    /// </summary>
    public enum DisposeTrigger
    {
        None,
        /// <summary>효과가 발동될 경우</summary>
        Used,
        /// <summary>장착 유닛이 대미지를 입었을 경우</summary>
        HasDamage,
        /// <summary>장착 유닛의 HP가 0이 될 경우</summary>
        HasDown
    }
}
