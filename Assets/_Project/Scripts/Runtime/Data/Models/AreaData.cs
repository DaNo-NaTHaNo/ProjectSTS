namespace ProjectStS.Data
{
    /// <summary>
    /// 구역 테이블 데이터 모델.
    /// 월드맵 내 구역의 기본 정보, 레벨 범위, 방향, 리소스 경로를 정의한다.
    /// </summary>
    [System.Serializable]
    public class AreaData
    {
        /// <summary>시스템에서 참조하기 위한 구역 ID</summary>
        public string id;

        /// <summary>UI 상에 표시할 해당 구역의 이름</summary>
        public string name;

        /// <summary>UI 상에 표시할 해당 구역에 대한 설명문</summary>
        public string description;

        /// <summary>해당 구역을 배치할 레벨의 최소 값</summary>
        public int areaLevelMin;

        /// <summary>해당 구역을 배치할 레벨의 최대 값</summary>
        public int areaLevelMax;

        /// <summary>해당 구역을 배치할 방향 값 (복수 입력 가능, 콤마 구분)</summary>
        public string areaCardinalPoint;

        /// <summary>해당 구역 진입 시 표시할 로고 이미지의 어드레서블 ID</summary>
        public string logoImagePath;

        /// <summary>해당 구역 진입 시 표시할 바닥 배경 이미지의 어드레서블 ID</summary>
        public string floorImagePath;

        /// <summary>해당 구역 진입 시 표시할 스카이박스의 어드레서블 ID</summary>
        public string skyboxPath;

        /// <summary>해당 구역에서 출현하는 비주얼 노벨 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellVisualNovelPath;

        /// <summary>해당 구역에서 출현하는 조우 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellEncountPath;

        /// <summary>해당 구역에서 출현하는 일반 전투 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellBattleNormalPath;

        /// <summary>해당 구역에서 출현하는 엘리트 전투 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellBattleElitePath;

        /// <summary>해당 구역에서 출현하는 보스 전투 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellBattleBossPath;

        /// <summary>해당 구역에서 출현하는 이벤트 전투 이벤트 칸에 배치할 모델의 어드레서블 ID</summary>
        public string cellBattleEventPath;
    }
}
