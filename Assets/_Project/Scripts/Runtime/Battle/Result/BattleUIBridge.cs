using System;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 시스템과 UI 레이어 간의 이벤트 브릿지.
    /// 전투 서브시스템의 이벤트를 UI가 구독 가능한 통합 이벤트로 재발행한다.
    /// 실제 UI 프리팹/레이아웃 구현은 별도 Phase에서 수행한다.
    /// </summary>
    public class BattleUIBridge
    {
        #region Events

        /// <summary>
        /// 전투 페이즈가 변경되었을 때.
        /// </summary>
        public event Action<BattlePhase> OnPhaseChanged;

        /// <summary>
        /// 카드가 손패에 추가되었을 때.
        /// </summary>
        public event Action<RuntimeCard> OnCardAddedToHand;

        /// <summary>
        /// 카드가 손패에서 제거되었을 때.
        /// </summary>
        public event Action<RuntimeCard> OnCardRemovedFromHand;

        /// <summary>
        /// 유닛이 대미지를 받았을 때. (유닛, 대미지량)
        /// </summary>
        public event Action<BattleUnit, int> OnUnitDamaged;

        /// <summary>
        /// 유닛이 회복되었을 때. (유닛, 회복량)
        /// </summary>
        public event Action<BattleUnit, int> OnUnitHealed;

        /// <summary>
        /// 유닛이 방어도를 얻었을 때. (유닛, 방어도량)
        /// </summary>
        public event Action<BattleUnit, int> OnUnitBlockGained;

        /// <summary>
        /// 유닛이 쓰러졌을 때.
        /// </summary>
        public event Action<BattleUnit> OnUnitDefeated;

        /// <summary>
        /// 상태이상이 변경되었을 때. (유닛, 상태이상)
        /// </summary>
        public event Action<BattleUnit, ActiveStatusEffect> OnStatusEffectChanged;

        /// <summary>
        /// 에너지가 변경되었을 때. (현재 에너지)
        /// </summary>
        public event Action<int> OnEnergyChanged;

        /// <summary>
        /// 적 유닛의 행동 의도가 결정되었을 때.
        /// </summary>
        public event Action<BattleUnit, AIDecision> OnEnemyIntentShown;

        /// <summary>
        /// 전투 결과가 표시될 때. (결과, 사유)
        /// </summary>
        public event Action<BattleResult, BattleEndReason> OnBattleResultShown;

        /// <summary>
        /// 스킬이 발동되었을 때. (유닛, 스킬 데이터)
        /// </summary>
        public event Action<BattleUnit, SkillData> OnSkillActivated;

        /// <summary>
        /// 카드가 사용되었을 때. (시전자, 카드)
        /// </summary>
        public event Action<BattleUnit, RuntimeCard> OnCardPlayed;

        #endregion

        #region Private Fields

        private TurnManager _turnManager;
        private CardExecutor _cardExecutor;
        private HandManager _handManager;
        private StatusEffectManager _statusEffectManager;
        private SkillExecutor _skillExecutor;
        private BattleAI _battleAI;
        private BattleResultHandler _resultHandler;

        #endregion

        #region Public Methods

        /// <summary>
        /// 에너지 변경을 외부에서 통지한다.
        /// </summary>
        /// <param name="currentEnergy">현재 에너지</param>
        public void NotifyEnergyChanged(int currentEnergy)
        {
            OnEnergyChanged?.Invoke(currentEnergy);
        }

        /// <summary>
        /// 서브시스템 이벤트를 구독한다.
        /// </summary>
        public void BindEvents(
            TurnManager turnManager,
            CardExecutor cardExecutor,
            HandManager handManager,
            StatusEffectManager statusEffectManager,
            SkillExecutor skillExecutor,
            BattleAI battleAI,
            BattleResultHandler resultHandler)
        {
            _turnManager = turnManager;
            _cardExecutor = cardExecutor;
            _handManager = handManager;
            _statusEffectManager = statusEffectManager;
            _skillExecutor = skillExecutor;
            _battleAI = battleAI;
            _resultHandler = resultHandler;

            _turnManager.OnPhaseChanged += HandlePhaseChanged;
            _turnManager.OnBattleEnded += HandleBattleEnded;

            _cardExecutor.OnCardPlayed += HandleCardPlayed;
            _cardExecutor.OnDamageDealt += HandleDamageDealt;
            _cardExecutor.OnBlockGained += HandleBlockGained;
            _cardExecutor.OnHealApplied += HandleHealApplied;
            _cardExecutor.OnEnergyChanged += HandleEnergyChanged;

            _handManager.OnCardAddedToHand += HandleCardAddedToHand;
            _handManager.OnCardRemovedFromHand += HandleCardRemovedFromHand;

            _statusEffectManager.OnStatusApplied += HandleStatusChanged;
            _statusEffectManager.OnStatusRemoved += HandleStatusChanged;
            _statusEffectManager.OnStatusExpired += HandleStatusChanged;

            _skillExecutor.OnSkillTriggered += HandleSkillTriggered;
            _battleAI.OnAIActionDecided += HandleAIDecided;
        }

        /// <summary>
        /// 서브시스템 이벤트 구독을 해제한다.
        /// </summary>
        public void UnbindEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnPhaseChanged -= HandlePhaseChanged;
                _turnManager.OnBattleEnded -= HandleBattleEnded;
            }

            if (_cardExecutor != null)
            {
                _cardExecutor.OnCardPlayed -= HandleCardPlayed;
                _cardExecutor.OnDamageDealt -= HandleDamageDealt;
                _cardExecutor.OnBlockGained -= HandleBlockGained;
                _cardExecutor.OnHealApplied -= HandleHealApplied;
                _cardExecutor.OnEnergyChanged -= HandleEnergyChanged;
            }

            if (_handManager != null)
            {
                _handManager.OnCardAddedToHand -= HandleCardAddedToHand;
                _handManager.OnCardRemovedFromHand -= HandleCardRemovedFromHand;
            }

            if (_statusEffectManager != null)
            {
                _statusEffectManager.OnStatusApplied -= HandleStatusChanged;
                _statusEffectManager.OnStatusRemoved -= HandleStatusChanged;
                _statusEffectManager.OnStatusExpired -= HandleStatusChanged;
            }

            if (_skillExecutor != null)
            {
                _skillExecutor.OnSkillTriggered -= HandleSkillTriggered;
            }

            if (_battleAI != null)
            {
                _battleAI.OnAIActionDecided -= HandleAIDecided;
            }
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandlePhaseChanged(BattlePhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
        }

        private void HandleCardPlayed(BattleUnit caster, RuntimeCard card)
        {
            OnCardPlayed?.Invoke(caster, card);
        }

        private void HandleDamageDealt(BattleUnit attacker, BattleUnit target, int damage)
        {
            OnUnitDamaged?.Invoke(target, damage);

            if (!target.IsAlive)
            {
                OnUnitDefeated?.Invoke(target);
            }
        }

        private void HandleBlockGained(BattleUnit unit, int block)
        {
            OnUnitBlockGained?.Invoke(unit, block);
        }

        private void HandleHealApplied(BattleUnit unit, int amount)
        {
            OnUnitHealed?.Invoke(unit, amount);
        }

        private void HandleEnergyChanged(int energy)
        {
            OnEnergyChanged?.Invoke(energy);
        }

        private void HandleCardAddedToHand(RuntimeCard card)
        {
            OnCardAddedToHand?.Invoke(card);
        }

        private void HandleCardRemovedFromHand(RuntimeCard card)
        {
            OnCardRemovedFromHand?.Invoke(card);
        }

        private void HandleStatusChanged(BattleUnit unit, ActiveStatusEffect effect)
        {
            OnStatusEffectChanged?.Invoke(unit, effect);
        }

        private void HandleSkillTriggered(BattleUnit unit, SkillData skill)
        {
            OnSkillActivated?.Invoke(unit, skill);
        }

        private void HandleAIDecided(BattleUnit enemy, AIDecision decision)
        {
            OnEnemyIntentShown?.Invoke(enemy, decision);
        }

        private void HandleBattleEnded(BattleResult result, BattleEndReason reason)
        {
            OnBattleResultShown?.Invoke(result, reason);
        }

        #endregion
    }
}
