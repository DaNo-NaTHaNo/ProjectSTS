using System;
using UnityEngine;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 페이즈 상태 머신 및 전환 제어 클래스.
    /// BattleStart → TurnStart → PlayerAction → EnemyAction → TurnEnd 사이클을 관리한다.
    /// </summary>
    public class TurnManager
    {
        #region Private Fields

        private readonly PhaseHandler _phaseHandler;
        private BattleState _state;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 플레이어 행동 페이즈인지 여부.
        /// </summary>
        public bool IsPlayerActionPhase => _state != null && _state.CurrentPhase == BattlePhase.PlayerAction;

        #endregion

        #region Events

        /// <summary>
        /// 페이즈가 변경되었을 때 발행.
        /// </summary>
        public event Action<BattlePhase> OnPhaseChanged;

        /// <summary>
        /// 새 턴이 시작되었을 때 발행. (턴 수)
        /// </summary>
        public event Action<int> OnTurnStarted;

        /// <summary>
        /// 턴이 종료되었을 때 발행. (턴 수)
        /// </summary>
        public event Action<int> OnTurnEnded;

        /// <summary>
        /// 전투가 종료되었을 때 발행. (결과, 사유)
        /// </summary>
        public event Action<BattleResult, BattleEndReason> OnBattleEnded;

        #endregion

        #region Constructor

        /// <summary>
        /// TurnManager를 생성한다.
        /// </summary>
        /// <param name="phaseHandler">페이즈 핸들러</param>
        /// <param name="state">전투 상태</param>
        public TurnManager(PhaseHandler phaseHandler, BattleState state)
        {
            _phaseHandler = phaseHandler;
            _state = state;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투를 시작한다. BattleStart → TurnStart로 진행한다.
        /// </summary>
        public void StartBattle()
        {
            SetPhase(BattlePhase.BattleStart);
            _phaseHandler.ExecuteBattleStart(_state);
            AdvancePhase();
        }

        /// <summary>
        /// 현재 페이즈에서 다음 페이즈로 전환한다.
        /// </summary>
        public void AdvancePhase()
        {
            switch (_state.CurrentPhase)
            {
                case BattlePhase.BattleStart:
                    ExecuteTurnStart();
                    break;

                case BattlePhase.TurnStart:
                    SetPhase(BattlePhase.PlayerAction);
                    break;

                case BattlePhase.PlayerAction:
                    SetPhase(BattlePhase.EnemyAction);
                    _phaseHandler.ExecuteEnemyAction(_state);
                    AdvanceAfterEnemyAction();
                    break;

                case BattlePhase.EnemyAction:
                    ExecuteTurnEnd();
                    break;

                case BattlePhase.TurnEnd:
                    HandleTurnEndTransition();
                    break;

                case BattlePhase.WaveTransition:
                    ExecuteTurnStart();
                    break;

                case BattlePhase.BattleEnd:
                    break;
            }
        }

        /// <summary>
        /// 플레이어가 턴 종료를 요청한다.
        /// </summary>
        public void EndPlayerAction()
        {
            if (_state.CurrentPhase != BattlePhase.PlayerAction)
            {
                Debug.LogWarning("[TurnManager] PlayerAction 페이즈가 아닌 상태에서 턴 종료가 요청되었습니다.");
                return;
            }

            AdvancePhase();
        }

        /// <summary>
        /// 이벤트에 의한 강제 전투 종료.
        /// </summary>
        /// <param name="result">전투 결과</param>
        /// <param name="reason">종료 사유</param>
        public void ForceBattleEnd(BattleResult result, BattleEndReason reason)
        {
            _state.Result = result;
            _state.EndReason = reason;
            SetPhase(BattlePhase.BattleEnd);
            OnBattleEnded?.Invoke(result, reason);
        }

        #endregion

        #region Private Methods

        private void SetPhase(BattlePhase phase)
        {
            _state.CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        private void ExecuteTurnStart()
        {
            SetPhase(BattlePhase.TurnStart);
            _phaseHandler.ExecuteTurnStart(_state);

            BattleResult result = _phaseHandler.CheckBattleResult(_state);

            if (result != BattleResult.None)
            {
                EndBattle(result);
                return;
            }

            OnTurnStarted?.Invoke(_state.CurrentTurn);
            AdvancePhase();
        }

        private void AdvanceAfterEnemyAction()
        {
            BattleResult result = _phaseHandler.CheckBattleResult(_state);

            if (result != BattleResult.None)
            {
                EndBattle(result);
                return;
            }

            ExecuteTurnEnd();
        }

        private void ExecuteTurnEnd()
        {
            SetPhase(BattlePhase.TurnEnd);
            _phaseHandler.ExecuteTurnEnd(_state);

            BattleResult result = _phaseHandler.CheckBattleResult(_state);

            if (result != BattleResult.None)
            {
                EndBattle(result);
                return;
            }

            OnTurnEnded?.Invoke(_state.CurrentTurn);
            HandleTurnEndTransition();
        }

        private void HandleTurnEndTransition()
        {
            if (_state.AliveEnemies.Count == 0 && _state.CurrentWave < _state.TotalWaves)
            {
                SetPhase(BattlePhase.WaveTransition);
                _phaseHandler.ExecuteWaveTransition(_state);
                AdvancePhase();
            }
            else
            {
                ExecuteTurnStart();
            }
        }

        private void EndBattle(BattleResult result)
        {
            BattleEndReason reason = result == BattleResult.Victory
                ? BattleEndReason.AllEnemiesDown
                : BattleEndReason.AllAlliesDown;

            _state.Result = result;
            _state.EndReason = reason;
            SetPhase(BattlePhase.BattleEnd);
            OnBattleEnded?.Invoke(result, reason);
        }

        #endregion
    }
}
