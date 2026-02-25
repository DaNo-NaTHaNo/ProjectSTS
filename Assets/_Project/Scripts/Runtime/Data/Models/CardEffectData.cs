namespace ProjectStS.Data
{
    /// <summary>
    /// 카드 효과 테이블 데이터 모델.
    /// 카드 또는 스킬에서 참조하는 효과의 상세 정보를 정의한다.
    /// </summary>
    [System.Serializable]
    public class CardEffectData
    {
        /// <summary>해당 효과를 적용할 카드 혹은 스킬에서 참조할 ID</summary>
        public string id;

        /// <summary>부여할 효과의 타입 (Damage, Block, Heal, Energy, ApplyStatus, ModifyCard, Draw, AddCard)</summary>
        public CardEffectType effectType;

        /// <summary>부여할 효과 수치</summary>
        public float value;

        /// <summary>상태이상을 부여할 경우, 부여할 상태이상의 ID</summary>
        public string statusEffectId;

        /// <summary>변경할 카드 수치의 타입</summary>
        public ModificationType modificationType;

        /// <summary>카드 수치 변경이 끝나는 타이밍</summary>
        public ModDuration modDuration;

        /// <summary>수치 변경을 적용할 카드를 자동 선정하는 기준</summary>
        public CardTargetSelection cardTargetSelection;

        /// <summary>수치 변경을 적용할 카드의 타입</summary>
        public CardType targetCardType;

        /// <summary>카드 추가 시, 추가할 카드의 ID (복수 기입 가능)</summary>
        public string addCardId;
    }
}
