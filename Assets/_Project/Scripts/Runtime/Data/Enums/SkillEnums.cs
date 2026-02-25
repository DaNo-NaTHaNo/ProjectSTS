namespace ProjectStS.Data
{
    /// <summary>
    /// 스킬 발동 트리거를 감지할 대상.
    /// </summary>
    public enum SkillTriggerTarget
    {
        /// <summary>스킬을 장비한 유닛</summary>
        Self,
        /// <summary>플레이어 파티 멤버 중 누군가</summary>
        Ally,
        /// <summary>적 유닛 중 누군가</summary>
        Enemy,
        /// <summary>모든 파티 멤버 유닛</summary>
        AllAlly,
        /// <summary>모든 적 유닛</summary>
        AllEnemy,
        /// <summary>손패의 카드</summary>
        InHand
    }

    /// <summary>
    /// 스킬 발동 트리거가 되는 상태의 타입.
    /// </summary>
    public enum SkillTriggerStatus
    {
        /// <summary>~의 카드를 사용</summary>
        PlayCard,
        /// <summary>~의 대미지를 가함</summary>
        CauseDamage,
        /// <summary>~의 대미지를 입음</summary>
        HasDamage,
        /// <summary>~의 방어도를 얻음</summary>
        HasDefend,
        /// <summary>~의 상태이상을 가함</summary>
        CauseStatusEffect,
        /// <summary>~의 상태이상을 얻음</summary>
        HasStatusEffect,
        /// <summary>현재 보유한 상태이상의 수가 ~</summary>
        StatusEffectCount
    }

    /// <summary>
    /// 스킬 사용 횟수 제약 방식.
    /// </summary>
    public enum SkillLimitType
    {
        /// <summary>무제한 사용</summary>
        None,
        /// <summary>사용 시 ~턴 간 쿨다운</summary>
        CoolDown,
        /// <summary>1 턴에 ~회 사용 가능</summary>
        PerTurn,
        /// <summary>전투 당 ~회 사용 가능</summary>
        PerBattle
    }
}
