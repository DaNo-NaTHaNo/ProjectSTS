namespace ProjectStS.Data
{
    /// <summary>
    /// 인벤토리 데이터 모델.
    /// 플레이어가 보유한 카드 및 아이템의 인벤토리 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class InventoryItemData
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

        /// <summary>해당 소지품의 미사용(장비) 보유 수</summary>
        public int ownStack;

        /// <summary>해당 소지품이 사용(장비)되고 있는 수</summary>
        public int useStack;

        /// <summary>Card 소지품의 필터링을 위한 속성 정보</summary>
        public ElementType cardElement;

        /// <summary>Card 소지품의 필터링을 위한 효과 포맷 형식</summary>
        public CardType cardType;

        /// <summary>Card 소지품의 필터링을 위한 코스트 정보</summary>
        public int cardCost;

        /// <summary>Item 소지품의 필터링을 위한 효과 포맷 형식</summary>
        public ItemType itemType;

        /// <summary>Item 소지품의 필터링을 위한 효과 적용 스탯 종류</summary>
        public ItemTargetStatus targetStatus;

        /// <summary>Item 소지품의 필터링을 위한 아이템의 소모 여부</summary>
        public bool isDisposable;
    }
}
