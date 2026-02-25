namespace ProjectStS.Data
{
    /// <summary>
    /// 카드 테이블 데이터 모델.
    /// 전투에서 사용하는 카드의 기본 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class CardData
    {
        /// <summary>시스템 상에서 참조하는 카드 ID</summary>
        public string id;

        /// <summary>UI 상에 표시되는 카드의 이름</summary>
        public string cardName;

        /// <summary>UI 상에 표시되는 카드 효과 설명문</summary>
        public string description;

        /// <summary>아트 리소스의 어드레서블 ID</summary>
        public string artworkPath;

        /// <summary>해당 카드의 표시 레어도</summary>
        public Rarity rarity;

        /// <summary>장비 및 상성 시스템에서 참조할 카드의 속성</summary>
        public ElementType element;

        /// <summary>해당 카드를 사용하는 데 필요한 에너지 값</summary>
        public int cost;

        /// <summary>해당 카드 사용 시 적용할 효과 데이터 ID</summary>
        public string cardEffectId;

        /// <summary>해당 카드의 효과 포맷 형식 (Attack, Defend, StatusEffect, InHandEffect)</summary>
        public CardType cardType;

        /// <summary>적을 상대로 하는 효과인지, 아군을 상대로 하는 효과인지</summary>
        public TargetType targetType;

        /// <summary>타겟으로 지정 가능한 제한 규칙</summary>
        public TargetFilter targetFilter;

        /// <summary>타겟을 자동 선택하는 규칙 (수동 선택일 경우 Manual)</summary>
        public TargetSelectionRule targetSelectionRule;

        /// <summary>카드 효과가 적용되는 타겟의 최대 수 (0일 시 상한 없음)</summary>
        public int targetCount;

        /// <summary>손패에서 사용했을 때 묘지로 가지 않고 제거되는 카드인지</summary>
        public bool isDisposable;
    }
}
