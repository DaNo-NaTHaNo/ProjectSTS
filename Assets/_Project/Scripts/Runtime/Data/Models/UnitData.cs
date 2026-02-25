namespace ProjectStS.Data
{
    /// <summary>
    /// 유닛 테이블 데이터 모델.
    /// 아군, 적, NPC 유닛의 기본 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class UnitData
    {
        /// <summary>시스템 상에서 참조하는 유닛 ID</summary>
        public string id;

        /// <summary>UI 상에 표시되는 유닛의 이름</summary>
        public string unitName;

        /// <summary>유닛이 아군인지 적인지 (Ally, Enemy, Stranger)</summary>
        public UnitType unitType;

        /// <summary>유닛의 속성 상성 타입</summary>
        public ElementType element;

        /// <summary>아트 리소스의 어드레서블 ID</summary>
        public string portraitPath;

        /// <summary>유닛의 기본 HP 값</summary>
        public int maxHP;

        /// <summary>유닛이 파티 합류 시 가산되는 에너지 수치</summary>
        public int maxEnergy;

        /// <summary>유닛의 행동력 (파티 합류 시 가산)</summary>
        public int maxAP;

        /// <summary>유닛의 초기 덱을 구성하는 카드 ID (세미콜론 구분)</summary>
        public string initialDeckIds;

        /// <summary>유닛의 초기 장비 스킬 ID</summary>
        public string initialSkillId;

        /// <summary>(적일 경우) 유닛의 행동 패턴 ID</summary>
        public string aiPatternId;
    }
}
