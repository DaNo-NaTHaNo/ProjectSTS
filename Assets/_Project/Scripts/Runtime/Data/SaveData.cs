using System.Collections.Generic;

namespace ProjectStS.Data
{
    /// <summary>
    /// 플레이어의 영속 데이터를 직렬화하기 위한 세이브 데이터 모델.
    /// Newtonsoft.Json으로 JSON 변환하여 파일에 저장한다.
    /// </summary>
    [System.Serializable]
    public class SaveData
    {
        /// <summary>세이브 데이터 버전 (마이그레이션 판별용)</summary>
        public int version;

        /// <summary>보유 유닛 목록</summary>
        public List<OwnedUnitData> ownedUnits;

        /// <summary>인벤토리 목록</summary>
        public List<InventoryItemData> inventory;

        /// <summary>탐험 기록</summary>
        public ExplorationRecordData explorationRecord;

        /// <summary>캠페인 해금 상태 (campaignId → 해금 여부)</summary>
        public Dictionary<string, bool> campaignUnlockedState;

        /// <summary>캠페인 완료 상태 (campaignId → 완료 여부)</summary>
        public Dictionary<string, bool> campaignCompletionState;

        /// <summary>목표 완료 상태 (goalKey → 완료 여부)</summary>
        public Dictionary<string, bool> goalCompletionState;

        /// <summary>현재 추적 중인 캠페인 ID</summary>
        public string trackedCampaignId;
    }
}
