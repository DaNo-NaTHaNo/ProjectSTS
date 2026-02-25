namespace ProjectStS.Data
{
    /// <summary>
    /// 인벤토리 소지품 분류.
    /// </summary>
    public enum InventoryCategory
    {
        Card,
        Item
    }

    /// <summary>
    /// 드랍율 적용 대상.
    /// </summary>
    public enum DropRateCategory
    {
        SpawnEvent,
        EventReward
    }

    /// <summary>
    /// 인벤토리 정렬 기준.
    /// </summary>
    public enum InventorySortType
    {
        /// <summary>이름 기준 정렬</summary>
        Name,
        /// <summary>속성 기준 정렬 (카드 전용)</summary>
        Element,
        /// <summary>레어도 기준 정렬</summary>
        Rarity,
        /// <summary>코스트 기준 정렬 (카드 전용)</summary>
        Cost,
        /// <summary>보유 수량 기준 정렬</summary>
        Quantity
    }
}
