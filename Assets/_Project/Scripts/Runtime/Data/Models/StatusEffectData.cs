namespace ProjectStS.Data
{
    /// <summary>
    /// 상태이상 테이블 데이터 모델.
    /// 버프/디버프 등 유닛에 적용되는 상태이상의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class StatusEffectData
    {
        /// <summary>상태이상 데이터의 참조 ID</summary>
        public string id;

        /// <summary>UI에 표시될 상태이상 명칭</summary>
        public string effectName;

        /// <summary>UI에 표시될 상태이상 설명문</summary>
        public string description;

        /// <summary>UI에 표시될 아이콘 리소스의 어드레서블 ID</summary>
        public string iconPath;

        /// <summary>해당 상태이상이 버프인지 디버프인지</summary>
        public StatusType statusType;

        /// <summary>효과를 적용시키는 시점 (TurnStart, TurnEnd, OnAttack, OnDamage)</summary>
        public TriggerTiming triggerTiming;

        /// <summary>해당 상태이상이 발생시키는 효과의 종류</summary>
        public StatusEffectType effectType;

        /// <summary>해당 상태이상이 가하거나 흡수, 반사하는 피해의 속성</summary>
        public ElementType effectElement;

        /// <summary>적용할 효과의 수치</summary>
        public float value;

        /// <summary>상태 효과가 수치를 수정하는 방식 (Additive, Multiplicative)</summary>
        public ModifierType modifierType;

        /// <summary>효과의 중첩이 가능한지</summary>
        public bool isStackable;

        /// <summary>중첩 가능한 상한치 값</summary>
        public int maxStacks;

        /// <summary>효과를 적용하기 위해 스택을 소모하는 상태이상인지</summary>
        public bool isExpendable;

        /// <summary>효과를 적용하기 위해 소모해야 하는 스택의 최소 수</summary>
        public int expendCount;

        /// <summary>상태이상이 지속되는 턴 (턴 종료 시 1 감소, 0이 되면 제거)</summary>
        public int duration;
    }
}
