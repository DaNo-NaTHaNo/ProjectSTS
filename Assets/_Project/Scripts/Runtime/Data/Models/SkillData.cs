namespace ProjectStS.Data
{
    /// <summary>
    /// 스킬 테이블 데이터 모델.
    /// 조건 충족 시 자동 발동하는 스킬의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class SkillData
    {
        /// <summary>시스템 상에서 참조하는 스킬 ID</summary>
        public string id;

        /// <summary>UI 상에 표시되는 스킬의 이름</summary>
        public string skillName;

        /// <summary>UI 상에 표시되는 스킬 효과 설명문</summary>
        public string description;

        /// <summary>해당 스킬을 장비 가능한 유닛의 ID</summary>
        public string unitId;

        /// <summary>아트 리소스의 어드레서블 ID</summary>
        public string artworkPath;

        /// <summary>스킬의 표시 레어도</summary>
        public Rarity rarity;

        /// <summary>장비 및 상성 시스템에서 참조할 스킬의 속성</summary>
        public ElementType element;

        /// <summary>스킬 발동 트리거를 감지할 대상</summary>
        public SkillTriggerTarget triggerTarget;

        /// <summary>스킬 발동 트리거가 되는 상태의 타입</summary>
        public SkillTriggerStatus triggerStatus;

        /// <summary>변수 값을 비교할 부등호</summary>
        public ComparisonOperator comparisonOperator;

        /// <summary>스킬 발동 트리거가 되는 상태의 변수 혹은 ID 값</summary>
        public string triggerValue;

        /// <summary>스킬 발동 트리거가 되는 상태의 속성 값</summary>
        public ElementType triggerElement;

        /// <summary>해당 스킬 사용 시 적용할 효과 데이터 ID</summary>
        public string cardEffectId;

        /// <summary>적을 상대로 하는 효과인지, 아군을 상대로 하는 효과인지</summary>
        public TargetType targetType;

        /// <summary>타겟으로 지정 가능한 제한 규칙</summary>
        public TargetFilter targetFilter;

        /// <summary>타겟을 자동 선택하는 규칙 (수동 선택일 경우 Manual)</summary>
        public TargetSelectionRule targetSelectionRule;

        /// <summary>카드 효과가 적용되는 타겟의 최대 수 (0일 시 상한 없음)</summary>
        public int targetCount;

        /// <summary>스킬의 사용횟수를 제약하는 방식</summary>
        public SkillLimitType limitType;

        /// <summary>스킬의 사용횟수를 제약하는 변수 값</summary>
        public int limitValue;
    }
}
