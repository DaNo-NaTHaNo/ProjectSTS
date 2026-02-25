namespace ProjectStS.Data
{
    /// <summary>
    /// 적 유닛 AI 패턴 룰 테이블 데이터 모델.
    /// 특정 조건에서 발동하는 AI 행동 규칙을 정의한다.
    /// </summary>
    [System.Serializable]
    public class AIPatternRuleData
    {
        /// <summary>해당 룰이 적용될 AI 패턴 테이블 ID</summary>
        public string aiPatternId;

        /// <summary>해당 패턴 룰의 ID</summary>
        public string ruleId;

        /// <summary>패턴 룰 조건 중복 시 판단 기준이 되는 우선도</summary>
        public int priority;

        /// <summary>해당 패턴 룰의 행동 타입</summary>
        public AIActionType actionType;

        /// <summary>얻거나 사용할 카드의 ID</summary>
        public string cardId;

        /// <summary>해당 패턴 룰의 타겟 선정 기준</summary>
        public TargetSelectionRule targetSelection;

        /// <summary>해당 패턴 룰 행동 시 출력되는 말풍선 대사</summary>
        public string speechLine;

        /// <summary>해당 패턴 룰 행동 시 출력되는 컷 인 효과의 어드레서블 ID</summary>
        public string cutInEffect;

        /// <summary>해당 패턴 룰 행동 시 유닛을 포커싱 및 줌 인 하는지</summary>
        public bool zoomIn;
    }
}
