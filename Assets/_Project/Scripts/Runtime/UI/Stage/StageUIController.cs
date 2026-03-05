using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Stage;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 스테이지 씬 UI의 중앙 컨트롤러.
    /// StageUIBridge의 9개 이벤트를 구독하여 하위 UI 컴포넌트에 분배하고,
    /// 노드 클릭, 이벤트 진입, 보상 선택 플로우를 관리한다.
    /// </summary>
    public class StageUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Stage Manager")]
        [SerializeField] private StageManager _stageManagerRef;

        [Header("Sub Components")]
        [SerializeField] private UIWorldMap _worldMap;
        [SerializeField] private UIStageHUD _stageHUD;
        [SerializeField] private UIBagPanel _bagPanel;
        [SerializeField] private UIEventPopup _eventPopup;
        [SerializeField] private UIStageResult _stageResult;

        #endregion

        #region Private Fields

        private StageManager _stageManager;
        private StageUIBridge _uiBridge;
        private HexNode _currentNode;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            StageManager manager = _stageManagerRef;

            if (manager == null)
            {
                ServiceLocator.TryGet<StageManager>(out manager);
            }

            if (manager != null)
            {
                BindStageManager(manager);
            }
        }

        private void OnDisable()
        {
            UnbindStageManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스테이지 UI를 초기화한다. StageManager 연결 후 호출된다.
        /// </summary>
        public void InitializeStageUI()
        {
            if (_stageManager == null)
            {
                return;
            }

            StageState state = _stageManager.State;

            if (state == null)
            {
                return;
            }

            DataManager dataManager = null;
            ServiceLocator.TryGet<DataManager>(out dataManager);

            if (_worldMap != null)
            {
                _worldMap.Initialize(state.Grid, _stageManager.ZoneManager, dataManager);
                _worldMap.OnTileClicked += HandleTileClicked;

                if (state.CurrentNode != null)
                {
                    _worldMap.SetCurrentNode(state.CurrentNode);
                    _worldMap.FocusOnNode(state.CurrentNode);
                }
            }

            if (_stageHUD != null)
            {
                _stageHUD.InitializeHUD(state.CurrentAP, state.MaxAP);
            }

            if (_bagPanel != null)
            {
                _bagPanel.Initialize(_stageManager.BagManager);
            }

            _currentNode = state.CurrentNode;
        }

        #endregion

        #region Private Methods — Binding

        private void BindStageManager(StageManager manager)
        {
            _stageManager = manager;
            _uiBridge = manager.UIBridge;

            if (_uiBridge == null)
            {
                Debug.LogWarning("[StageUIController] UIBridge가 null입니다.");
                return;
            }

            _uiBridge.OnNodeMoved += HandleNodeMoved;
            _uiBridge.OnEventTriggered += HandleEventTriggered;
            _uiBridge.OnEventCompleted += HandleEventCompleted;
            _uiBridge.OnAPChanged += HandleAPChanged;
            _uiBridge.OnZoneRevealed += HandleZoneRevealed;
            _uiBridge.OnNodeRevealed += HandleNodeRevealed;
            _uiBridge.OnBagItemAdded += HandleBagItemAdded;
            _uiBridge.OnStagePhaseChanged += HandleStagePhaseChanged;
            _uiBridge.OnStageResultShown += HandleStageResultShown;

            InitializeStageUI();
        }

        private void UnbindStageManager()
        {
            if (_worldMap != null)
            {
                _worldMap.OnTileClicked -= HandleTileClicked;
            }

            if (_uiBridge != null)
            {
                _uiBridge.OnNodeMoved -= HandleNodeMoved;
                _uiBridge.OnEventTriggered -= HandleEventTriggered;
                _uiBridge.OnEventCompleted -= HandleEventCompleted;
                _uiBridge.OnAPChanged -= HandleAPChanged;
                _uiBridge.OnZoneRevealed -= HandleZoneRevealed;
                _uiBridge.OnNodeRevealed -= HandleNodeRevealed;
                _uiBridge.OnBagItemAdded -= HandleBagItemAdded;
                _uiBridge.OnStagePhaseChanged -= HandleStagePhaseChanged;
                _uiBridge.OnStageResultShown -= HandleStageResultShown;
            }

            _uiBridge = null;
            _stageManager = null;
            _currentNode = null;
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandleNodeMoved(HexNode node)
        {
            _currentNode = node;

            if (_worldMap != null)
            {
                _worldMap.SetCurrentNode(node);
                _worldMap.FocusOnNode(node);
                _worldMap.UpdateNodeState(node);
            }
        }

        private void HandleEventTriggered(HexNode node, EventData eventData)
        {
            if (_eventPopup != null && eventData != null)
            {
                _eventPopup.Show(
                    eventData,
                    onConfirm: () => HandleEventConfirmed(eventData),
                    onCancel: null
                );
            }
        }

        private void HandleEventCompleted(EventData eventData, bool success)
        {
            if (_currentNode != null && _worldMap != null)
            {
                _worldMap.UpdateNodeState(_currentNode);
            }

            if (_stageManager != null && _stageManager.State != null)
            {
                if (_stageHUD != null)
                {
                    _stageHUD.SetEventCount(_stageManager.State.CompletedEventCount);
                }
            }
        }

        private void HandleAPChanged(int current, int max)
        {
            if (_stageHUD != null)
            {
                _stageHUD.SetAP(current, max);
            }
        }

        private void HandleZoneRevealed(string areaId)
        {
            if (_worldMap != null)
            {
                _worldMap.RevealZone(areaId);
            }

            if (_stageHUD != null)
            {
                DataManager dataManager = null;
                ServiceLocator.TryGet<DataManager>(out dataManager);

                if (dataManager != null)
                {
                    AreaData area = dataManager.GetArea(areaId);

                    if (area != null)
                    {
                        _stageHUD.SetZoneInfo(area);
                    }
                }
            }
        }

        private void HandleNodeRevealed(HexNode node)
        {
            if (_worldMap != null)
            {
                _worldMap.RevealNode(node);
            }
        }

        private void HandleBagItemAdded(InGameBagItemData item)
        {
            if (_bagPanel != null)
            {
                _bagPanel.AddItem(item);
            }
        }

        private void HandleStagePhaseChanged(StagePhase phase)
        {
            switch (phase)
            {
                case StagePhase.Exploration:
                    SetExplorationUI(true);
                    break;

                case StagePhase.EventExecuting:
                    SetExplorationUI(false);
                    break;

                case StagePhase.Ended:
                    SetExplorationUI(false);
                    break;
            }
        }

        private void HandleStageResultShown(StageResult result, StageEndReason reason)
        {
            if (_stageResult == null || _stageManager == null)
            {
                return;
            }

            StageSettlementData settlement = _stageManager.CalculateSettlement();

            if (settlement == null)
            {
                return;
            }

            _stageResult.Show(settlement, HandleRewardConfirmed);
        }

        #endregion

        #region Private Methods — Actions

        private void HandleTileClicked(HexNode node)
        {
            if (_stageManager == null || !_stageManager.IsExploring)
            {
                return;
            }

            if (_eventPopup != null && _eventPopup.IsShowing)
            {
                return;
            }

            _stageManager.MoveToNode(node);
        }

        private void HandleEventConfirmed(EventData eventData)
        {
            // 이벤트 진입은 StageManager의 HandleEventTriggered에서 이미
            // 페이즈를 EventExecuting으로 전환하고 처리한다.
            // UI에서의 확인은 플레이어에게 시각적 피드백을 제공하는 역할이다.
            // 실제 전투/VN 씬 전환은 GameFlowController가 담당한다.
        }

        private void HandleRewardConfirmed(List<InGameBagItemData> selectedRewards)
        {
            // 보상 선택 확정 후 정산 완료 처리.
            // GameFlowController.CompleteSettlement()가 호출되어야 하며,
            // 이는 상위 흐름 제어자가 OnRewardSelectionConfirmed를 구독하여 처리한다.
        }

        private void SetExplorationUI(bool active)
        {
            // 탐험 중에만 월드맵 상호작용 활성화
            if (_worldMap != null)
            {
                _worldMap.enabled = active;
            }
        }

        #endregion
    }
}
