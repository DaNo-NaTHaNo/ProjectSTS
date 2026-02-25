namespace ProjectStS.Data
{
    /// <summary>
    /// 상태이상의 버프/디버프 분류.
    /// </summary>
    public enum StatusType
    {
        Buff,
        Debuff
    }

    /// <summary>
    /// 상태이상 효과 적용 시점.
    /// </summary>
    public enum TriggerTiming
    {
        TurnStart,
        TurnEnd,
        OnAttack,
        OnDamage
    }

    /// <summary>
    /// 상태이상이 발생시키는 효과의 종류.
    /// </summary>
    public enum StatusEffectType
    {
        DamageOverTime,
        HealOverTime,
        ModifyDamage,
        ModifyBlock,
        Stun,
        ReduceDamage,
        ReflectDamage
    }

    /// <summary>
    /// 수치 수정 방식.
    /// </summary>
    public enum ModifierType
    {
        Additive,
        Multiplicative
    }
}
