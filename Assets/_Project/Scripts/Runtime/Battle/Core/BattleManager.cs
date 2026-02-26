using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 수명주기 오케스트레이터.
    /// 모든 전투 서브시스템의 생성, 의존성 주입, 이벤트 연결, 전투 진행 API를 제공한다.
    /// ServiceLocator에 등록되어 외부(씬, UI)에서 접근한다.
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        #region Private Fields

        private BattleState _state;
        private DeckManager _deckManager;
        private HandManager _handManager;
        private CardExecutor _cardExecutor;
        private SkillExecutor _skillExecutor;
        private StatusEffectManager _statusEffectManager;
        private BattleAI _battleAI;
        private ItemEffectProcessor _itemEffectProcessor;
        private TurnManager _turnManager;
        private PhaseHandler _phaseHandler;
        private BattleResultHandler _resultHandler;
        private BattleUIBridge _uiBridge;
        private BattleTimelineManager _timelineManager;

        private bool _isInitialized;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 전투 상태.
        /// </summary>
        public BattleState State => _state;

        /// <summary>
        /// 전투가 초기화되었는지 여부.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 플레이어 행동 페이즈인지 여부.
        /// </summary>
        public bool IsPlayerActionPhase => _turnManager != null && _turnManager.IsPlayerActionPhase;

        /// <summary>
        /// 손패 매니저 접근 (UI에서 손패 조회용).
        /// </summary>
        public HandManager HandManager => _handManager;

        /// <summary>
        /// UI 브릿지 접근.
        /// </summary>
        public BattleUIBridge UIBridge => _uiBridge;

        /// <summary>
        /// 턴 매니저 접근.
        /// </summary>
        public TurnManager TurnManager => _turnManager;

        #endregion

        #region Events

        /// <summary>
        /// 전투 초기화 완료 시 발행.
        /// </summary>
        public event Action OnBattleInitialized;

        /// <summary>
        /// 전투 종료 시 발행.
        /// </summary>
        public event Action<BattleResult> OnBattleEnded;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            CleanupBattle();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투를 초기화하고 시작한다.
        /// </summary>
        /// <param name="party">파티 편성 데이터 목록</param>
        /// <param name="waves">웨이브별 적 조합 데이터 목록</param>
        /// <param name="eventId">전투 이벤트 ID</param>
        public void InitializeBattle(List<OwnedUnitData> party, List<EnemyCombinationData> waves, string eventId)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[BattleManager] 이미 전투가 초기화되어 있습니다. 기존 전투를 정리합니다.");
                CleanupBattle();
            }

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[BattleManager] DataManager를 찾을 수 없습니다.");
                return;
            }

            _state = new BattleState();
            _state.Initialize(party, waves, eventId);

            _deckManager = new DeckManager();
            _handManager = new HandManager();
            _statusEffectManager = new StatusEffectManager(dataManager);
            _cardExecutor = new CardExecutor(_deckManager, _handManager, _statusEffectManager, dataManager);
            _skillExecutor = new SkillExecutor(dataManager);
            _skillExecutor.Initialize(_cardExecutor);
            _battleAI = new BattleAI(dataManager);
            _itemEffectProcessor = new ItemEffectProcessor(dataManager, _statusEffectManager, _handManager, _deckManager);
            _resultHandler = new BattleResultHandler();
            _phaseHandler = new PhaseHandler(
                _deckManager, _handManager, _cardExecutor, _skillExecutor,
                _statusEffectManager, _battleAI, _itemEffectProcessor, _resultHandler);
            _turnManager = new TurnManager(_phaseHandler, _state);

            _timelineManager = new BattleTimelineManager(dataManager);
            _timelineManager.Initialize(eventId);

            _uiBridge = new BattleUIBridge();
            _uiBridge.BindEvents(_turnManager, _cardExecutor, _handManager,
                _statusEffectManager, _skillExecutor, _battleAI, _resultHandler);

            BindInternalEvents();

            _isInitialized = true;

            ServiceLocator.Register(this);

            OnBattleInitialized?.Invoke();

            Debug.Log($"[BattleManager] 전투 초기화 완료. 아군 {_state.Allies.Count}명, 적 {_state.Enemies.Count}명, 웨이브 {_state.TotalWaves}개.");

            _turnManager.StartBattle();
        }

        /// <summary>
        /// 플레이어가 카드를 사용한다 (UI에서 호출).
        /// </summary>
        /// <param name="handIndex">손패 인덱스</param>
        /// <param name="targets">선택된 타겟 목록</param>
        public void PlayCard(int handIndex, List<BattleUnit> targets)
        {
            if (!_isInitialized || !_turnManager.IsPlayerActionPhase)
            {
                Debug.LogWarning("[BattleManager] 카드를 사용할 수 없는 상태입니다.");
                return;
            }

            RuntimeCard card = _handManager.GetCard(handIndex);

            if (card == null)
            {
                Debug.LogWarning($"[BattleManager] 유효하지 않은 손패 인덱스: {handIndex}");
                return;
            }

            if (_state.CurrentEnergy < card.ModifiedCost)
            {
                Debug.LogWarning($"[BattleManager] 에너지 부족. 필요: {card.ModifiedCost}, 현재: {_state.CurrentEnergy}");
                return;
            }

            _state.CurrentEnergy -= card.ModifiedCost;
            _uiBridge.NotifyEnergyChanged(_state.CurrentEnergy);

            BattleUnit caster = FindCardOwner(card);

            _handManager.PlayCard(card, _deckManager);
            _cardExecutor.ExecuteCard(card, caster, targets, _state);

            _skillExecutor.CheckTriggers(
                SkillTriggerStatus.PlayCard, caster, _state,
                card.BaseData.element);

            CheckAutoEndTurn();
        }

        /// <summary>
        /// 플레이어가 턴을 종료한다 (UI에서 호출).
        /// </summary>
        public void EndTurn()
        {
            if (!_isInitialized || !_turnManager.IsPlayerActionPhase)
            {
                return;
            }

            _turnManager.EndPlayerAction();
        }

        /// <summary>
        /// 플레이어가 항복한다 (UI에서 호출).
        /// </summary>
        public void Surrender()
        {
            if (!_isInitialized)
            {
                return;
            }

            _turnManager.ForceBattleEnd(BattleResult.Defeat, BattleEndReason.Surrender);
        }

        /// <summary>
        /// 전투를 정리한다.
        /// </summary>
        public void CleanupBattle()
        {
            if (!_isInitialized)
            {
                return;
            }

            UnbindInternalEvents();

            if (_uiBridge != null)
            {
                _uiBridge.UnbindEvents();
            }

            ServiceLocator.Unregister<BattleManager>();

            _isInitialized = false;

            Debug.Log("[BattleManager] 전투 정리 완료.");
        }

        #endregion

        #region Private Methods

        private BattleUnit FindCardOwner(RuntimeCard card)
        {
            for (int i = 0; i < _state.Allies.Count; i++)
            {
                if (_state.Allies[i].UnitId == card.OwnerUnitId)
                {
                    return _state.Allies[i];
                }
            }

            if (_state.Allies.Count > 0)
            {
                return _state.Allies[0];
            }

            return null;
        }

        private void CheckAutoEndTurn()
        {
            if (_state.CurrentEnergy <= 0 || _handManager.HandCount <= 0)
            {
                _turnManager.EndPlayerAction();
            }
        }

        private void BindInternalEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnBattleEnded += HandleBattleEnded;
            }

            if (_cardExecutor != null)
            {
                _cardExecutor.OnDamageDealt += HandleDamageDealt;
            }
        }

        private void UnbindInternalEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnBattleEnded -= HandleBattleEnded;
            }

            if (_cardExecutor != null)
            {
                _cardExecutor.OnDamageDealt -= HandleDamageDealt;
            }
        }

        private void HandleBattleEnded(BattleResult result, BattleEndReason reason)
        {
            _resultHandler.DetermineResult(result, reason, _state);
            OnBattleEnded?.Invoke(result);

            Debug.Log($"[BattleManager] 전투 종료: {result} ({reason})");
        }

        private void HandleDamageDealt(BattleUnit attacker, BattleUnit target, int damage)
        {
            if (target.IsAlive)
            {
                _itemEffectProcessor.ProcessTrigger(target, ItemType.HasDamage, _state);

                _skillExecutor.CheckTriggers(
                    SkillTriggerStatus.HasDamage, target, _state,
                    attacker.Element);

                _skillExecutor.CheckTriggers(
                    SkillTriggerStatus.CauseDamage, attacker, _state,
                    attacker.Element);
            }
            else
            {
                _itemEffectProcessor.ProcessTrigger(target, ItemType.HasDown, _state);

                _timelineManager.CheckTriggers(
                    TimelineTriggerType.UnitDown, target.UnitId, null, _state);
            }
        }

        #endregion
    }
}
