namespace ProjectStS.Data
{
    /// <summary>
    /// 전투 연출 행동 테이블 데이터 모델.
    /// 전투 중 실행할 연출 그룹 내 개별 행동의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class BattleActionData
    {
        /// <summary>실행할 연출들의 그룹 ID</summary>
        public string groupId;

        /// <summary>그룹 내의 실행 순서 (1, 2, 3...)</summary>
        public int sequence;

        /// <summary>실행할 연출/기능의 종류</summary>
        public BattleActionType actionType;

        /// <summary>연출에 필요한 데이터</summary>
        public string actionValue;

        /// <summary>연출의 대상 유닛/위치값</summary>
        public string targetUnit;

        /// <summary>해당 연출이 종료된 후에 다음 sequence를 진행하는지 여부</summary>
        public bool waitNext;
    }
}
