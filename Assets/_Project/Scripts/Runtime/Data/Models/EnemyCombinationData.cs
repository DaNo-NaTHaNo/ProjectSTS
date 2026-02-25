namespace ProjectStS.Data
{
    /// <summary>
    /// 적 유닛 조합 테이블 데이터 모델.
    /// 전투 웨이브에서 등장하는 적 유닛 조합을 정의한다.
    /// </summary>
    [System.Serializable]
    public class EnemyCombinationData
    {
        /// <summary>시스템 상에서 참조하는 적 조합 ID</summary>
        public string id;

        /// <summary>인스펙터 상에 표시되는 해당 조합의 명칭</summary>
        public string name;

        /// <summary>인스펙터 상에 표시되는 해당 조합에 대한 설명</summary>
        public string description;

        /// <summary>해당 조합이 배틀의 몇 번째 웨이브에 등장하는지</summary>
        public int waveCount;

        /// <summary>적 영역 가장 좌측에 위치하는 유닛 ID</summary>
        public string enemyUnit1;

        /// <summary>적 영역 좌측에 위치하는 유닛 ID</summary>
        public string enemyUnit2;

        /// <summary>적 영역 중앙에 위치하는 유닛 ID</summary>
        public string enemyUnit3;

        /// <summary>적 영역 우측에 위치하는 유닛 ID</summary>
        public string enemyUnit4;

        /// <summary>적 영역 가장 우측에 위치하는 유닛 ID</summary>
        public string enemyUnit5;
    }
}
