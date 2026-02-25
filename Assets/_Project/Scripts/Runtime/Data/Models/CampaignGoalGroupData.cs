namespace ProjectStS.Data
{
    /// <summary>
    /// 캠페인 항목 그룹 테이블 데이터 모델.
    /// 캠페인 내 개별 달성 목표의 순서, 조건, 보상을 정의한다.
    /// </summary>
    [System.Serializable]
    public class CampaignGoalGroupData
    {
        /// <summary>캠페인 목표 순서를 묶는 그룹 ID</summary>
        public string groupId;

        /// <summary>해당 그룹에서 몇 번째로 수행하여야 하는 목표인지</summary>
        public int sequence;

        /// <summary>UI 상에 표시될 수행 목표의 제목 텍스트</summary>
        public string name;

        /// <summary>UI 상에 표시될 수행 목표에 대한 설명 텍스트</summary>
        public string description;

        /// <summary>필수로 완료해야 하는 목표인지 (해당 목표의 완료를 기다린 후 다음 시퀀스를 진행하는지)</summary>
        public bool isEssential;

        /// <summary>해당 목표 완수의 트리거를 감지할 대상 타입</summary>
        public CampaignTriggerType triggerType;

        /// <summary>목표 완수 트리거의 변수 값</summary>
        public string triggerValue;

        /// <summary>해당 목표를 완수할 시 캠페인 완료 보상에 추가할 유닛/스킬/아이템의 ID</summary>
        public string additionalRewards;

        /// <summary>해당 그룹의 마지막 목표인지 (트리거 감지 시 해당 행동 그룹의 캠페인을 완료 처리)</summary>
        public bool isClearTrigger;

        /// <summary>해당 목표를 완료했는지 여부</summary>
        public bool isCompleted;
    }
}
