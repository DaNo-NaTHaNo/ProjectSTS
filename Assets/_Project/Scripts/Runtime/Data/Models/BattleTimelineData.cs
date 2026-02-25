namespace ProjectStS.Data
{
    /// <summary>
    /// 전투 이벤트 타임라인 테이블 데이터 모델.
    /// 전투 중 특정 조건 충족 시 실행할 연출 타임라인의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class BattleTimelineData
    {
        /// <summary>시스템 상에서 참조하는 타임라인 ID</summary>
        public string id;

        /// <summary>해당 타임라인이 적용될 이벤트 ID</summary>
        public string eventId;

        /// <summary>발동 트리거를 감지할 대상 (유닛보다 상위의 정보일 경우 공란)</summary>
        public string triggerTarget;

        /// <summary>발동 트리거가 되는 상태의 타입</summary>
        public TimelineTriggerType triggerType;

        /// <summary>조건에 필요한 값</summary>
        public string triggerValue;

        /// <summary>조건 중복 시 실행 우선순위</summary>
        public int priority;

        /// <summary>반복 실행 여부 (1회성일 경우 false)</summary>
        public bool isRepeatable;

        /// <summary>조건을 만족 시 실행할 전투 연출 행동 그룹 테이블 ID</summary>
        public string actionGroupId;
    }
}
