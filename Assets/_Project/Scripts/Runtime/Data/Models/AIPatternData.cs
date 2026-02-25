namespace ProjectStS.Data
{
    /// <summary>
    /// 적 유닛 AI 패턴 테이블 데이터 모델.
    /// 적 유닛의 기본 행동 패턴 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class AIPatternData
    {
        /// <summary>임포터에서 참조할 테이블 내 패턴 ID</summary>
        public string id;

        /// <summary>인스펙터 상에 표시될 패턴의 명칭</summary>
        public string patternName;

        /// <summary>인스펙터 상에 표시될 패턴에 대한 설명문</summary>
        public string description;

        /// <summary>패턴 항목에 해당하지 않는 기본 행동 타입</summary>
        public AIActionType defaultActionType;

        /// <summary>기본 행동 타입에서 사용할 카드 ID</summary>
        public string defaultCardId;

        /// <summary>기본 행동 시 타겟 선정 기준</summary>
        public TargetSelectionRule defaultTargetSelection;
    }
}
