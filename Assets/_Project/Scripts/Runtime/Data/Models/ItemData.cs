namespace ProjectStS.Data
{
    /// <summary>
    /// 아이템 테이블 데이터 모델.
    /// 유닛에 장비하여 스펙/카드/스킬 수치를 강화하는 아이템의 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class ItemData
    {
        /// <summary>시스템 상에서 참조하는 아이템 ID</summary>
        public string id;

        /// <summary>UI 상에 표시되는 아이템의 이름</summary>
        public string itemName;

        /// <summary>UI 상에 표시되는 아이템 설명문</summary>
        public string description;

        /// <summary>아트 리소스의 어드레서블 ID</summary>
        public string artworkPath;

        /// <summary>아이템의 표시 레어도</summary>
        public Rarity rarity;

        /// <summary>아이템 사용 방식에 따른 타입 (Equipment, HasDamage, HasDown)</summary>
        public ItemType itemType;

        /// <summary>효과를 적용할 대상 유닛</summary>
        public ItemTargetUnit targetUnit;

        /// <summary>효과를 적용할 스탯 종류</summary>
        public ItemTargetStatus targetStatus;

        /// <summary>해당 스탯 값에 연산할 값 혹은 상태이상 ID</summary>
        public string modifyValue;

        /// <summary>특정 조건 하에 소모되는 아이템인지</summary>
        public bool isDisposable;

        /// <summary>아이템이 소모되는 조건 (isDisposable이 false일 때는 None)</summary>
        public DisposeTrigger disposeTrigger;

        /// <summary>조건을 만족했을 때 아이템이 소모될 확률 (0~1, 1일 경우 무조건 소모)</summary>
        public float disposePercentage;

        /// <summary>한 칸에 겹쳐 장비할 수 있는 갯수</summary>
        public int stackCount;
    }
}
