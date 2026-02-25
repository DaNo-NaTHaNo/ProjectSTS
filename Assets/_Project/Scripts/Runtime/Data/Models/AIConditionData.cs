namespace ProjectStS.Data
{
    /// <summary>
    /// 적 유닛 AI 컨디션 테이블 데이터 모델.
    /// AI 패턴 룰의 발동 조건을 정의한다.
    /// </summary>
    [System.Serializable]
    public class AIConditionData
    {
        /// <summary>해당 컨디션이 적용될 패턴 룰 ID</summary>
        public string ruleId;

        /// <summary>해당 컨디션의 동작 방식</summary>
        public AIConditionType conditionType;

        /// <summary>변수 값을 비교할 부등호</summary>
        public ComparisonOperator comparisonOperator;

        /// <summary>컨디션 타입에 대입할 변수 값 (수치, cardId, statusEffectId)</summary>
        public string value;

        /// <summary>TurnMod 타입에서 해당 패턴이 발동하는 턴 주기</summary>
        public int divisor;

        /// <summary>TurnMod 타입에서 해당 패턴을 처음 발동하는 턴</summary>
        public int remainder;
    }
}
