using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 각 전투 페이즈의 실행 로직을 담당하는 클래스.
    /// TurnManager가 상태 머신 전환을, PhaseHandler가 실제 로직 실행을 분리한다.
    /// </summary>
    public class PhaseHandler
    {
        #region Private Fields

        private readonly DeckManager _deckManager;
        private readonly HandManager _handManager;
        private readonly CardExecutor _cardExecutor;
        private readonly SkillExecutor _skillExecutor;
        private readonly StatusEffectManager _statusEffectManager;
        private readonly BattleAI _battleAI;
        private readonly ItemEffectProcessor _itemEffectProcessor;
        private readonly BattleResultHandler _resultHandler;

        private readonly Dictionary<BattleUnit, AIDecision> _pendingAIDecisions;

        #endregion

        #region Public Properties

        /// <summary>
        /// 각 적 유닛의 이번 턴 AI 결정 (UI 표시용).
        /// </summary>
        public IReadOnlyDictionary<BattleUnit, AIDecision> PendingAIDecisions => _pendingAIDecisions;

        #endregion

        #region Constructor

        /// <summary>
        /// PhaseHandler를 생성한다.
        /// </summary>
        public PhaseHandler(
            DeckManager deckManager,
            HandManager handManager,
            CardExecutor cardExecutor,
            SkillExecutor skillExecutor,
            StatusEffectManager statusEffectManager,
            BattleAI battleAI,
            ItemEffectProcessor itemEffectProcessor,
            BattleResultHandler resultHandler)
        {
            _deckManager = deckManager;
            _handManager = handManager;
            _cardExecutor = cardExecutor;
            _skillExecutor = skillExecutor;
            _statusEffectManager = statusEffectManager;
            _battleAI = battleAI;
            _itemEffectProcessor = itemEffectProcessor;
            _resultHandler = resultHandler;

            _pendingAIDecisions = new Dictionary<BattleUnit, AIDecision>(5);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 시작 페이즈를 실행한다.
        /// 아이템 패시브 효과 적용 및 전투 덱 구축.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ExecuteBattleStart(BattleState state)
        {
            _itemEffectProcessor.ApplyPassiveEffects(state);
            _deckManager.BuildBattleDeck(state);

            Debug.Log("[PhaseHandler] BattleStart 페이즈 완료.");
        }

        /// <summary>
        /// 턴 시작 페이즈를 실행한다.
        /// 턴 수 증가, Block 리셋, 에너지 회복, 손패 리필, AI 결정, 상태이상 처리.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ExecuteTurnStart(BattleState state)
        {
            state.CurrentTurn++;

            for (int i = 0; i < state.Allies.Count; i++)
            {
                state.Allies[i].ResetBlockForTurn();
                state.Allies[i].ResetSkillUsageForTurn();
            }

            for (int i = 0; i < state.Enemies.Count; i++)
            {
                state.Enemies[i].ResetBlockForTurn();
                state.Enemies[i].ResetSkillUsageForTurn();
            }

            state.CurrentEnergy = state.BaseEnergy;

            _handManager.RefillHand(_deckManager);

            _pendingAIDecisions.Clear();
            List<BattleUnit> aliveEnemies = state.AliveEnemies;

            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                AIDecision decision = _battleAI.DecideAction(aliveEnemies[i], state);
                _pendingAIDecisions[aliveEnemies[i]] = decision;
            }

            _statusEffectManager.ProcessTurnStart(state);

            Debug.Log($"[PhaseHandler] TurnStart 페이즈 완료. 턴 {state.CurrentTurn}, 에너지 {state.CurrentEnergy}, 손패 {_handManager.HandCount}장.");
        }

        /// <summary>
        /// 적 행동 페이즈를 실행한다.
        /// Stun 상태이면 행동 불가, 그 외 AI 결정 실행.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ExecuteEnemyAction(BattleState state)
        {
            List<BattleUnit> aliveEnemies = state.AliveEnemies;

            for (int i = 0; i < aliveEnemies.Count; i++)
            {
                BattleUnit enemy = aliveEnemies[i];

                if (!enemy.IsAlive)
                {
                    continue;
                }

                if (_statusEffectManager.HasStun(enemy))
                {
                    Debug.Log($"[PhaseHandler] '{enemy.BaseData.unitName}'은(는) 행동 불능 상태이다.");
                    continue;
                }

                if (_pendingAIDecisions.TryGetValue(enemy, out AIDecision decision))
                {
                    _battleAI.ExecuteAction(decision, enemy, state, _cardExecutor);
                }
            }

            Debug.Log("[PhaseHandler] EnemyAction 페이즈 완료.");
        }

        /// <summary>
        /// 턴 종료 페이즈를 실행한다.
        /// 상태이상 처리, duration 감소, 승패 체크.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ExecuteTurnEnd(BattleState state)
        {
            _statusEffectManager.ProcessTurnEnd(state);

            Debug.Log($"[PhaseHandler] TurnEnd 페이즈 완료. 턴 {state.CurrentTurn}.");
        }

        /// <summary>
        /// 웨이브 전환 페이즈를 실행한다.
        /// 다음 웨이브의 적 유닛을 생성한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ExecuteWaveTransition(BattleState state)
        {
            int nextWaveIndex = state.CurrentWave;

            if (nextWaveIndex >= state.TotalWaves)
            {
                Debug.LogWarning("[PhaseHandler] 다음 웨이브가 없습니다.");
                return;
            }

            state.SetupWave(nextWaveIndex);

            Debug.Log($"[PhaseHandler] WaveTransition 완료. 웨이브 {state.CurrentWave}/{state.TotalWaves}.");
        }

        /// <summary>
        /// 승패 결과를 확인한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        /// <returns>전투 결과 (None이면 계속)</returns>
        public BattleResult CheckBattleResult(BattleState state)
        {
            return _resultHandler.CheckResult(state);
        }

        #endregion
    }
}
