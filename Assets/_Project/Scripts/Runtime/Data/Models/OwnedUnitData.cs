namespace ProjectStS.Data
{
    /// <summary>
    /// 보유 유닛 및 파티 편성 데이터 모델.
    /// 플레이어가 보유한 유닛의 덱/스킬/아이템 편성 상태와 파티 위치를 정의한다.
    /// </summary>
    [System.Serializable]
    public class OwnedUnitData
    {
        /// <summary>유닛 테이블 상의 ID</summary>
        public string unitId;

        /// <summary>해당 캐릭터 덱을 구성 가능한 카드 속성</summary>
        public ElementType cardElement;

        /// <summary>편성 화면에서 편집한 캐릭터의 덱 (cardID, 복수 입력 가능)</summary>
        public string editedDeck;

        /// <summary>편성 화면에서 편집한 캐릭터의 스킬</summary>
        public string editedSkill;

        /// <summary>첫번째 아이템 슬롯에 장비한 장비품 아이템</summary>
        public string equipItem1;

        /// <summary>두번째 아이템 슬롯에 장비한 장비품 아이템</summary>
        public string equipItem2;

        /// <summary>해당 캐릭터가 파티의 어느 자리에 위치하는지 (파티 구성원이 아니면 0)</summary>
        public int partyPosition;
    }
}
