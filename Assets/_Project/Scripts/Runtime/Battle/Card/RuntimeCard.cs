using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// CardData의 런타임 래퍼.
    /// ModifyCard 효과에 의한 임시 수치 변경(코스트, 대미지, 방어도 등)을 추적한다.
    /// </summary>
    public class RuntimeCard
    {
        #region Private Fields

        private int _costModifierUntilPlayed;
        private int _costModifierCombat;
        private float _damageModifierUntilPlayed;
        private float _damageModifierCombat;
        private float _blockModifierUntilPlayed;
        private float _blockModifierCombat;

        #endregion

        #region Public Properties

        /// <summary>
        /// 카드 마스터 데이터 참조.
        /// </summary>
        public CardData BaseData { get; }

        /// <summary>
        /// 이 카드를 소유한 유닛의 ID.
        /// </summary>
        public string OwnerUnitId { get; }

        /// <summary>
        /// 현재 적용된 코스트 (기본값 + 임시 보정).
        /// </summary>
        public int ModifiedCost => BaseData.cost + _costModifierUntilPlayed + _costModifierCombat;

        /// <summary>
        /// 현재 적용된 추가 대미지 보정.
        /// </summary>
        public float DamageModifier => _damageModifierUntilPlayed + _damageModifierCombat;

        /// <summary>
        /// 현재 적용된 추가 방어도 보정.
        /// </summary>
        public float BlockModifier => _blockModifierUntilPlayed + _blockModifierCombat;

        /// <summary>
        /// UntilPlayed 보정이 적용되어 있는지.
        /// </summary>
        public bool HasUntilPlayedModification =>
            _costModifierUntilPlayed != 0 ||
            _damageModifierUntilPlayed != 0f ||
            _blockModifierUntilPlayed != 0f;

        /// <summary>
        /// Combat 보정이 적용되어 있는지.
        /// </summary>
        public bool HasCombatModification =>
            _costModifierCombat != 0 ||
            _damageModifierCombat != 0f ||
            _blockModifierCombat != 0f;

        #endregion

        #region Constructor

        /// <summary>
        /// RuntimeCard를 생성한다.
        /// </summary>
        /// <param name="baseData">카드 마스터 데이터</param>
        /// <param name="ownerUnitId">소유 유닛 ID</param>
        public RuntimeCard(CardData baseData, string ownerUnitId)
        {
            BaseData = baseData;
            OwnerUnitId = ownerUnitId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 카드 수치 변경을 적용한다.
        /// </summary>
        /// <param name="type">변경 대상 (Damage, Block, Cost 등)</param>
        /// <param name="value">변경 값</param>
        /// <param name="duration">지속 기간 (UntilPlayed 또는 Combat)</param>
        public void ApplyModification(ModificationType type, float value, ModDuration duration)
        {
            switch (type)
            {
                case ModificationType.Damage:
                    if (duration == ModDuration.UntilPlayed)
                        _damageModifierUntilPlayed += value;
                    else
                        _damageModifierCombat += value;
                    break;

                case ModificationType.Block:
                    if (duration == ModDuration.UntilPlayed)
                        _blockModifierUntilPlayed += value;
                    else
                        _blockModifierCombat += value;
                    break;

                case ModificationType.Cost:
                    if (duration == ModDuration.UntilPlayed)
                        _costModifierUntilPlayed += (int)value;
                    else
                        _costModifierCombat += (int)value;
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// 카드 사용 시 UntilPlayed 보정을 제거한다.
        /// </summary>
        public void ClearPlayedModifications()
        {
            _costModifierUntilPlayed = 0;
            _damageModifierUntilPlayed = 0f;
            _blockModifierUntilPlayed = 0f;
        }

        /// <summary>
        /// 모든 수치 변경을 초기화한다.
        /// </summary>
        public void ResetToBase()
        {
            _costModifierUntilPlayed = 0;
            _costModifierCombat = 0;
            _damageModifierUntilPlayed = 0f;
            _damageModifierCombat = 0f;
            _blockModifierUntilPlayed = 0f;
            _blockModifierCombat = 0f;
        }

        #endregion
    }
}
