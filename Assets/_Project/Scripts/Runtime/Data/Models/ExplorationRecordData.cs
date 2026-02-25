namespace ProjectStS.Data
{
    /// <summary>
    /// 탐험 기록 데이터 모델.
    /// 플레이어의 누적 및 현재 탐험 통계 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class ExplorationRecordData
    {
        /// <summary>지금까지 탐험에 출발한 횟수</summary>
        public int countDepart;

        /// <summary>지금까지 탐험에 성공해 귀환한 횟수</summary>
        public int countComplete;

        /// <summary>지금까지 완료한 전투 이벤트의 횟수</summary>
        public int countBattleAll;

        /// <summary>지금까지 완료한 비주얼 노벨 이벤트의 횟수</summary>
        public int countVisualNovelAll;

        /// <summary>지금까지 완료한 조우 이벤트의 횟수</summary>
        public int countEncountAll;

        /// <summary>이번 탐험에서 완료한 전투 이벤트의 횟수</summary>
        public int countBattleNow;

        /// <summary>이번 탐험에서 완료한 비주얼 노벨 이벤트의 횟수</summary>
        public int countVisualNovelNow;

        /// <summary>이번 탐험에서 완료한 조우 이벤트의 횟수</summary>
        public int countEncountNow;

        /// <summary>지금까지 토벌한 적 유닛의 수</summary>
        public int countEnemyEliminated;

        /// <summary>지금까지 토벌한 보스의 유닛 ID (복수 입력 가능)</summary>
        public string eliminatedBossId;

        /// <summary>지금까지 탐험한 적이 있는 Area ID (복수 입력 가능)</summary>
        public string visitedAreaId;

        /// <summary>이번 탐험에서 획득할 수 있는 귀환 보상의 갯수</summary>
        public int countRewardComplete;
    }
}
