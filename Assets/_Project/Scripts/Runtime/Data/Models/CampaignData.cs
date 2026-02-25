namespace ProjectStS.Data
{
    /// <summary>
    /// 캠페인 테이블 데이터 모델.
    /// 메인/서브 캠페인의 기본 정보, 해금 조건, 보상을 정의한다.
    /// </summary>
    [System.Serializable]
    public class CampaignData
    {
        /// <summary>시스템 상에서 참조하는 캠페인 ID</summary>
        public string id;

        /// <summary>UI 상에 표시될 캠페인의 제목 텍스트</summary>
        public string name;

        /// <summary>UI 상에 표시될 캠페인에 대한 설명 텍스트</summary>
        public string description;

        /// <summary>UI에 표시될 아트 리소스의 어드레서블 ID</summary>
        public string artworkPath;

        /// <summary>해당 캠페인을 해금하기 위한 트리거의 타입</summary>
        public CampaignTriggerType unlockType;

        /// <summary>해금 트리거의 변수 값 (대상 캠페인, 미션, 유닛, 횟수 등)</summary>
        public string unlockId;

        /// <summary>캠페인 내 달성 항목 그룹 ID</summary>
        public string groupId;

        /// <summary>캠페인 완료 보상 유닛이나 스킬, 혹은 아이템의 ID (복수 입력 가능)</summary>
        public string rewards;

        /// <summary>해당 캠페인을 완료했는지 여부</summary>
        public bool isCompleted;

        /// <summary>해당 캠페인 완료 직후 재생할 연출 파일의 어드레서블 ID (없으면 공란)</summary>
        public string afterComplete;
    }
}
