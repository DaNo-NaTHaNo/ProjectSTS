namespace ProjectStS.Data
{
    /// <summary>
    /// 이벤트 테이블 데이터 모델.
    /// 구역 내 출현하는 이벤트(전투, 비주얼 노벨, 조우 등)의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class EventData
    {
        /// <summary>이벤트가 등장하는 구역 ID</summary>
        public string areaId;

        /// <summary>시스템 상에서 참조하는 이벤트 ID</summary>
        public string id;

        /// <summary>해당 이벤트의 종류 (비주얼 노벨, 전투 등)</summary>
        public EventType eventType;

        /// <summary>해당 이벤트의 내용 (비주얼노벨 파일 어드레서블, 적 유닛 조합 ID 등)</summary>
        public string eventValue;

        /// <summary>해당 이벤트가 출현하기 위해 해결되어야 하는 조건</summary>
        public SpawnTrigger spawnTrigger;

        /// <summary>변수 값을 비교할 부등호</summary>
        public ComparisonOperator comparisonOperator;

        /// <summary>이벤트 출현 조건에 대입할 변수 혹은 ID 값</summary>
        public string spawnTriggerValue;

        /// <summary>해당 이벤트가 출현할 레벨 범위의 최소값</summary>
        public int minLevel;

        /// <summary>해당 이벤트가 출현할 레벨 범위의 최대값</summary>
        public int maxLevel;

        /// <summary>해당 이벤트의 출현 희귀도</summary>
        public Rarity rarity;

        /// <summary>해당 이벤트의 랜덤 보상 테이블 ID (복수 입력 가능)</summary>
        public string rewardId;

        /// <summary>해당 이벤트의 보상 갯수 최소치</summary>
        public int rewardMinCount;

        /// <summary>해당 이벤트의 보상 갯수 최대치</summary>
        public int rewardMaxCount;
    }
}
