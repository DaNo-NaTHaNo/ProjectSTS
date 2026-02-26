using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;
using ProjectStS.Meta;
using EventType = ProjectStS.Data.EventType;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 스테이지 수명주기 오케스트레이터.
    /// 모든 스테이지 서브시스템의 생성, 의존성 주입, 이벤트 연결, 탐험 진행 API를 제공한다.
    /// ServiceLocator에 등록되어 외부(씬, UI)에서 접근한다.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        #region Private Fields

        private StageState _state;
        private HexGridGenerator _gridGenerator;
        private ZoneManager _zoneManager;
        private EventPlacementSystem _eventPlacement;
        private EventRewardProcessor _rewardProcessor;
        private ExplorationManager _explorationManager;
        private InGameBagManager _bagManager;
        private StageResultCalculator _resultCalculator;
        private StageUIBridge _uiBridge;

        private bool _isInitialized;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 스테이지 상태.
        /// </summary>
        public StageState State => _state;

        /// <summary>
        /// 스테이지가 초기화되었는지 여부.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 탐험 중인지 여부.
        /// </summary>
        public bool IsExploring => _state != null && _state.CurrentPhase == StagePhase.Exploration;

        /// <summary>
        /// UI 브릿지 접근.
        /// </summary>
        public StageUIBridge UIBridge => _uiBridge;

        /// <summary>
        /// 탐험 매니저 접근 (UI에서 이동 가능 노드 조회 등).
        /// </summary>
        public ExplorationManager ExplorationManager => _explorationManager;

        /// <summary>
        /// 인게임 가방 매니저 접근.
        /// </summary>
        public InGameBagManager BagManager => _bagManager;

        /// <summary>
        /// 구역 매니저 접근.
        /// </summary>
        public ZoneManager ZoneManager => _zoneManager;

        /// <summary>
        /// 그리드 생성기 접근.
        /// </summary>
        public HexGridGenerator GridGenerator => _gridGenerator;

        #endregion

        #region Events

        /// <summary>
        /// 스테이지 초기화 완료 시 발행.
        /// </summary>
        public event Action OnStageInitialized;

        /// <summary>
        /// 스테이지 종료 시 발행.
        /// </summary>
        public event Action<StageResult, StageEndReason> OnStageEnded;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            CleanupStage();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스테이지를 초기화하고 탐험을 시작한다.
        /// </summary>
        /// <param name="party">파티 편성 데이터 목록</param>
        /// <param name="existingRecord">기존 탐험 기록 (없으면 null)</param>
        public void InitializeStage(List<OwnedUnitData> party, ExplorationRecordData existingRecord = null)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[StageManager] 이미 스테이지가 초기화되어 있습니다. 기존 스테이지를 정리합니다.");
                CleanupStage();
            }

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[StageManager] DataManager를 찾을 수 없습니다.");
                return;
            }

            _state = new StageState();
            _state.Initialize(party);

            if (existingRecord != null)
            {
                _state.Record = existingRecord;
            }

            _gridGenerator = new HexGridGenerator();
            Dictionary<(int, int, int), HexNode> grid = _gridGenerator.GenerateGrid();
            _state.Grid = grid;
            _state.StartNode = _gridGenerator.StartNode;

            _zoneManager = new ZoneManager(dataManager);
            _zoneManager.AssignZones(grid);

            _eventPlacement = new EventPlacementSystem(dataManager);

            // 외부 조건 확인 콜백 연결 (OwnCharacter, OwnItem 평가용)
            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData))
            {
                _eventPlacement.OnExternalConditionCheck = (trigger, value) =>
                {
                    switch (trigger)
                    {
                        case SpawnTrigger.OwnCharacter: return playerData.HasUnit(value);
                        case SpawnTrigger.OwnItem: return playerData.HasItem(value);
                        default: return false;
                    }
                };
            }

            _eventPlacement.PlaceEvents(grid, _zoneManager, _state.Record);

            _rewardProcessor = new EventRewardProcessor(dataManager);

            _bagManager = new InGameBagManager();

            _explorationManager = new ExplorationManager(dataManager);
            _explorationManager.Initialize(_state, _zoneManager, party);

            _resultCalculator = new StageResultCalculator(dataManager, _rewardProcessor);

            _uiBridge = new StageUIBridge();
            _uiBridge.BindEvents(_explorationManager, _bagManager);

            BindInternalEvents();

            _state.CurrentPhase = StagePhase.Exploration;
            _isInitialized = true;

            ServiceLocator.Register(this);

            OnStageInitialized?.Invoke();
            _uiBridge.NotifyPhaseChanged(StagePhase.Exploration);

            Debug.Log($"[StageManager] 스테이지 초기화 완료. " +
                      $"노드 {grid.Count}개, AP {_state.CurrentAP}/{_state.MaxAP}.");
        }

        /// <summary>
        /// 타겟 노드로 이동한다 (UI에서 호출).
        /// </summary>
        /// <param name="target">이동 대상 노드</param>
        public void MoveToNode(HexNode target)
        {
            if (!_isInitialized || !IsExploring)
            {
                Debug.LogWarning("[StageManager] 이동할 수 없는 상태입니다.");
                return;
            }

            _explorationManager.MoveToNode(target);
        }

        /// <summary>
        /// 이벤트 완료를 처리한다 (전투/VN 씬에서 콜백).
        /// </summary>
        /// <param name="eventId">완료된 이벤트 ID</param>
        /// <param name="success">성공 여부</param>
        public void OnEventCompleted(string eventId, bool success)
        {
            if (!_isInitialized)
            {
                return;
            }

            HexNode currentNode = _state.CurrentNode;

            if (currentNode.AssignedEvent == null || currentNode.AssignedEvent.id != eventId)
            {
                Debug.LogWarning($"[StageManager] 이벤트 ID '{eventId}'가 현재 노드와 일치하지 않습니다.");
                return;
            }

            EventData completedEvent = currentNode.AssignedEvent;
            currentNode.IsEventCompleted = true;

            if (success)
            {
                _state.RecordEventCompletion(completedEvent.eventType);
                ProcessEventRewards(completedEvent);
                CheckStageEndCondition(completedEvent);
            }
            else
            {
                HandleEventFailure(completedEvent);
            }

            _uiBridge.NotifyEventCompleted(completedEvent, success);

            if (_state.CurrentPhase == StagePhase.EventExecuting)
            {
                _state.CurrentPhase = StagePhase.Exploration;
                _uiBridge.NotifyPhaseChanged(StagePhase.Exploration);
            }
        }

        /// <summary>
        /// 스테이지를 종료한다.
        /// </summary>
        /// <param name="result">결과</param>
        /// <param name="reason">종료 사유</param>
        public void EndStage(StageResult result, StageEndReason reason)
        {
            if (!_isInitialized)
            {
                return;
            }

            _state.Result = result;
            _state.EndReason = reason;
            _state.CurrentPhase = StagePhase.Ended;

            _uiBridge.NotifyPhaseChanged(StagePhase.Ended);
            _uiBridge.NotifyStageResult(result, reason);

            OnStageEnded?.Invoke(result, reason);

            Debug.Log($"[StageManager] 스테이지 종료: {result} ({reason})");
        }

        /// <summary>
        /// 스테이지 완료 보상을 계산한다.
        /// </summary>
        /// <returns>보상 정산 데이터</returns>
        public StageSettlementData CalculateSettlement()
        {
            if (!_isInitialized)
            {
                return null;
            }

            return _resultCalculator.CalculateResult(_state, _bagManager);
        }

        /// <summary>
        /// 스테이지를 정리한다.
        /// </summary>
        public void CleanupStage()
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

            ServiceLocator.Unregister<StageManager>();

            _isInitialized = false;

            Debug.Log("[StageManager] 스테이지 정리 완료.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 내부 이벤트를 구독한다.
        /// </summary>
        private void BindInternalEvents()
        {
            if (_explorationManager != null)
            {
                _explorationManager.OnEventTriggered += HandleEventTriggered;
                _explorationManager.OnAPDepleted += HandleAPDepleted;
            }
        }

        /// <summary>
        /// 내부 이벤트 구독을 해제한다.
        /// </summary>
        private void UnbindInternalEvents()
        {
            if (_explorationManager != null)
            {
                _explorationManager.OnEventTriggered -= HandleEventTriggered;
                _explorationManager.OnAPDepleted -= HandleAPDepleted;
            }
        }

        /// <summary>
        /// 이벤트 트리거 처리.
        /// </summary>
        private void HandleEventTriggered(HexNode node, EventData eventData)
        {
            _explorationManager.ConsumeAP();
            _state.CurrentPhase = StagePhase.EventExecuting;
            _uiBridge.NotifyPhaseChanged(StagePhase.EventExecuting);
        }

        /// <summary>
        /// 행동력 고갈 처리.
        /// </summary>
        private void HandleAPDepleted()
        {
            if (_state.CurrentPhase == StagePhase.EventExecuting)
            {
                return;
            }

            EndStage(StageResult.Victory, StageEndReason.APDepleted);
        }

        /// <summary>
        /// 이벤트 보상을 처리한다.
        /// </summary>
        private void ProcessEventRewards(EventData eventData)
        {
            List<InGameBagItemData> rewards = _rewardProcessor.ProcessEventReward(eventData);

            for (int i = 0; i < rewards.Count; i++)
            {
                _bagManager.AddItem(rewards[i]);
            }
        }

        /// <summary>
        /// 이벤트 완료 후 스테이지 종료 조건을 확인한다.
        /// </summary>
        private void CheckStageEndCondition(EventData eventData)
        {
            if (eventData.eventType == EventType.BattleBoss)
            {
                EndStage(StageResult.Victory, StageEndReason.BossCleared);
                return;
            }

            if (_state.CurrentAP <= 0)
            {
                EndStage(StageResult.Victory, StageEndReason.APDepleted);
            }
        }

        /// <summary>
        /// 이벤트 실패를 처리한다.
        /// </summary>
        private void HandleEventFailure(EventData eventData)
        {
            if (eventData.eventType == EventType.BattleNormal ||
                eventData.eventType == EventType.BattleElite ||
                eventData.eventType == EventType.BattleBoss ||
                eventData.eventType == EventType.BattleEvent)
            {
                EndStage(StageResult.Failure, StageEndReason.PartyWipe);
            }
            else
            {
                EndStage(StageResult.Failure, StageEndReason.EventFailed);
            }
        }

        #endregion
    }
}
