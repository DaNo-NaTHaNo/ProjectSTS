namespace ProjectStS.Data
{
    /// <summary>
    /// 인 게임 가방 데이터 모델.
    /// 탐험 중 획득한 카드 및 아이템의 가방 내 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class InGameBagItemData
    {
        /// <summary>해당 소지품이 카드인지 아이템인지</summary>
        public InventoryCategory category;

        /// <summary>시스템 상에서 참조하는 카드 혹은 아이템 테이블에서의 ID 값</summary>
        public string productId;

        /// <summary>UI 상에 표시되는 소지품의 이름</summary>
        public string productName;

        /// <summary>UI 상에 표시되는 소지품의 설명문</summary>
        public string description;

        /// <summary>해당 소지품의 표시 레어도</summary>
        public Rarity rarity;

        /// <summary>해당 카드 혹은 아이템이 이번 탐험에서 얻은 것인지 (완료 보상 리스트 필터링을 위한 값)</summary>
        public bool isNewForNow;
    }
}
