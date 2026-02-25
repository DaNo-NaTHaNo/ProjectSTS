namespace ProjectStS.Data
{
    /// <summary>
    /// 효과의 대상 진영.
    /// </summary>
    public enum TargetType
    {
        Ally,
        Both,
        Enemy
    }

    /// <summary>
    /// 타겟 지정 가능 제한 규칙.
    /// </summary>
    public enum TargetFilter
    {
        None,
        OnlySelf
    }

    /// <summary>
    /// 타겟 자동 선택 규칙.
    /// </summary>
    public enum TargetSelectionRule
    {
        /// <summary>수동 선택</summary>
        Manual,
        /// <summary>모든 타겟</summary>
        AllTargets,
        /// <summary>HP가 가장 낮은 대상</summary>
        LowestHp,
        /// <summary>HP가 가장 높은 대상</summary>
        HighestHp,
        /// <summary>무작위</summary>
        Random,
        /// <summary>자기 자신</summary>
        Self
    }

    /// <summary>
    /// 변수 값 비교 부등호.
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }
}
