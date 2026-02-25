using System;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 스테이지 시스템과 UI 레이어 간의 이벤트 브릿지.
    /// 서브시스템의 이벤트를 UI가 구독 가능한 통합 이벤트로 재발행한다.
    /// 실제 UI 프리팹/레이아웃 구현은 별도 작업에서 수행한다.
    /// </summary>
    public class StageUIBridge
    {
        #region Events

        /// <summary>
        /// 노드 이동 시.
        /// </summary>
        public event Action<HexNode> OnNodeMoved;

        /// <summary>
        /// 이벤트 트리거 시. (노드, 이벤트 데이터)
        /// </summary>
        public event Action<HexNode, EventData> OnEventTriggered;

        /// <summary>
        /// 이벤트 완료 시. (이벤트 데이터, 성공 여부)
        /// </summary>
        public event Action<EventData, bool> OnEventCompleted;

        /// <summary>
        /// 행동력 변경 시. (현재 AP, 최대 AP)
        /// </summary>
        public event Action<int, int> OnAPChanged;

        /// <summary>
        /// 구역 공개 시. (구역 ID)
        /// </summary>
        public event Action<string> OnZoneRevealed;

        /// <summary>
        /// 노드 공개 시.
        /// </summary>
        public event Action<HexNode> OnNodeRevealed;

        /// <summary>
        /// 인게임 가방에 아이템 추가 시.
        /// </summary>
        public event Action<InGameBagItemData> OnBagItemAdded;

        /// <summary>
        /// 스테이지 페이즈 변경 시.
        /// </summary>
        public event Action<StagePhase> OnStagePhaseChanged;

        /// <summary>
        /// 스테이지 결과 표시 시. (결과, 사유)
        /// </summary>
        public event Action<StageResult, StageEndReason> OnStageResultShown;

        #endregion

        #region Private Fields

        private ExplorationManager _explorationManager;
        private InGameBagManager _bagManager;

        #endregion

        #region Public Methods

        /// <summary>
        /// 서브시스템 이벤트를 구독한다.
        /// </summary>
        public void BindEvents(ExplorationManager explorationManager, InGameBagManager bagManager)
        {
            _explorationManager = explorationManager;
            _bagManager = bagManager;

            _explorationManager.OnMoved += HandleNodeMoved;
            _explorationManager.OnEventTriggered += HandleEventTriggered;
            _explorationManager.OnAPChanged += HandleAPChanged;
            _explorationManager.OnZoneEntered += HandleZoneRevealed;
            _explorationManager.OnNodeRevealed += HandleNodeRevealed;
            _explorationManager.OnAPDepleted += HandleAPDepleted;

            _bagManager.OnItemAdded += HandleBagItemAdded;
        }

        /// <summary>
        /// 서브시스템 이벤트 구독을 해제한다.
        /// </summary>
        public void UnbindEvents()
        {
            if (_explorationManager != null)
            {
                _explorationManager.OnMoved -= HandleNodeMoved;
                _explorationManager.OnEventTriggered -= HandleEventTriggered;
                _explorationManager.OnAPChanged -= HandleAPChanged;
                _explorationManager.OnZoneEntered -= HandleZoneRevealed;
                _explorationManager.OnNodeRevealed -= HandleNodeRevealed;
                _explorationManager.OnAPDepleted -= HandleAPDepleted;
            }

            if (_bagManager != null)
            {
                _bagManager.OnItemAdded -= HandleBagItemAdded;
            }
        }

        /// <summary>
        /// 이벤트 완료를 외부에서 통지한다.
        /// </summary>
        public void NotifyEventCompleted(EventData eventData, bool success)
        {
            OnEventCompleted?.Invoke(eventData, success);
        }

        /// <summary>
        /// 페이즈 변경을 외부에서 통지한다.
        /// </summary>
        public void NotifyPhaseChanged(StagePhase phase)
        {
            OnStagePhaseChanged?.Invoke(phase);
        }

        /// <summary>
        /// 스테이지 결과를 외부에서 통지한다.
        /// </summary>
        public void NotifyStageResult(StageResult result, StageEndReason reason)
        {
            OnStageResultShown?.Invoke(result, reason);
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandleNodeMoved(HexNode node)
        {
            OnNodeMoved?.Invoke(node);
        }

        private void HandleEventTriggered(HexNode node, EventData eventData)
        {
            OnEventTriggered?.Invoke(node, eventData);
        }

        private void HandleAPChanged(int current, int max)
        {
            OnAPChanged?.Invoke(current, max);
        }

        private void HandleZoneRevealed(string areaId)
        {
            OnZoneRevealed?.Invoke(areaId);
        }

        private void HandleNodeRevealed(HexNode node)
        {
            OnNodeRevealed?.Invoke(node);
        }

        private void HandleBagItemAdded(InGameBagItemData item)
        {
            OnBagItemAdded?.Invoke(item);
        }

        private void HandleAPDepleted()
        {
            // AP 고갈은 StageManager에서 처리하여 StageResult를 통지
        }

        #endregion
    }
}
