namespace ProjectStS.Data
{
    /// <summary>
    /// 보상 테이블 데이터 모델.
    /// 랜덤 보상 목록의 개별 항목 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class RewardTableData
    {
        /// <summary>랜덤 보상 목록의 ID</summary>
        public string id;

        /// <summary>해당 구역의 랜덤 보상에서 드랍시킬 카드 혹은 아이템의 ID</summary>
        public string itemId;

        /// <summary>해당 아이템의 출현 희귀도</summary>
        public Rarity rarity;

        /// <summary>해당 아이템의 출현 확률 보정치</summary>
        public float dropRate;
    }
}
