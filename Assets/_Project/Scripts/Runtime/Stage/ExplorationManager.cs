using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 탐험 진행 매니저.
    /// 이동, 행동력(AP) 관리, 시야 제어, 이벤트 트리거, 귀환 조건을 처리한다.
    /// </summary>
    public class ExplorationManager
    {
        #region Private Fields

        private readonly DataManager _dataManager;
        private StageState _state;
        private ZoneManager _zoneManager;
        private HashSet<string> _revealedZones;

        #endregion

        #region Events

        /// <summary>
        /// 노드 이동 완료 시 발행.
        /// </summary>
        public event Action<HexNode> OnMoved;

        /// <summary>
        /// 이벤트가 배치된 노드에 도착하여 이벤트가 트리거될 때 발행.
        /// </summary>
        public event Action<HexNode, EventData> OnEventTriggered;

        /// <summary>
        /// 행동력이 고갈되었을 때 발행.
        /// </summary>
        public event Action OnAPDepleted;

        /// <summary>
        /// 새로운 구역에 진입했을 때 발행.
        /// </summary>
        public event Action<string> OnZoneEntered;

        /// <summary>
        /// 노드가 공개되었을 때 발행.
        /// </summary>
        public event Action<HexNode> OnNodeRevealed;

        /// <summary>
        /// AP가 변경되었을 때 발행. (현재 AP, 최대 AP)
        /// </summary>
        public event Action<int, int> OnAPChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// ExplorationManager를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public ExplorationManager(DataManager dataManager)
        {
            _dataManager = dataManager;
            _revealedZones = new HashSet<string>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 행동력.
        /// </summary>
        public int CurrentAP => _state != null ? _state.CurrentAP : 0;

        /// <summary>
        /// 최대 행동력.
        /// </summary>
        public int MaxAP => _state != null ? _state.MaxAP : 0;

        /// <summary>
        /// 현재 위치 노드.
        /// </summary>
        public HexNode CurrentNode => _state?.CurrentNode;

        #endregion

        #region Public Methods

        /// <summary>
        /// 탐험을 초기화한다. 파티의 행동력을 합산하고 시작점에 배치한다.
        /// </summary>
        /// <param name="state">스테이지 상태</param>
        /// <param name="zoneManager">구역 매니저</param>
        /// <param name="party">파티 편성 데이터</param>
        public void Initialize(StageState state, ZoneManager zoneManager, List<OwnedUnitData> party)
        {
            _state = state;
            _zoneManager = zoneManager;
            _revealedZones.Clear();

            InitializeAP(party);

            _state.CurrentNode = _state.StartNode;
            _state.CurrentNode.IsVisited = true;
            _state.CurrentNode.IsRevealed = true;

            RevealAdjacentNodes(_state.CurrentNode);

            string startAreaId = _state.CurrentNode.AreaId;

            if (!string.IsNullOrEmpty(startAreaId))
            {
                RevealZone(startAreaId);
            }

            Debug.Log($"[ExplorationManager] 탐험 초기화 완료. AP: {_state.CurrentAP}/{_state.MaxAP}");
        }

        /// <summary>
        /// 타겟 노드로 이동할 수 있는지 확인한다.
        /// </summary>
        /// <param name="target">이동 대상 노드</param>
        /// <returns>이동 가능 여부</returns>
        public bool CanMoveTo(HexNode target)
        {
            if (target == null || _state.CurrentNode == null)
            {
                return false;
            }

            if (_state.CurrentPhase != StagePhase.Exploration)
            {
                return false;
            }

            return _state.CurrentNode.Neighbors.Contains(target);
        }

        /// <summary>
        /// 타겟 노드로 이동한다.
        /// 이벤트가 있으면 OnEventTriggered를 발행하고, 없으면 이동만 수행한다.
        /// </summary>
        /// <param name="target">이동 대상 노드</param>
        /// <returns>이동 성공 여부</returns>
        public bool MoveToNode(HexNode target)
        {
            if (!CanMoveTo(target))
            {
                Debug.LogWarning($"[ExplorationManager] 이동 불가: {target}");
                return false;
            }

            _state.CurrentNode = target;
            target.IsVisited = true;

            CheckZoneEntry(target);
            RevealAdjacentNodes(target);

            OnMoved?.Invoke(target);

            if (target.AssignedEvent != null && !target.IsEventCompleted)
            {
                OnEventTriggered?.Invoke(target, target.AssignedEvent);
            }

            return true;
        }

        /// <summary>
        /// 이벤트 실행 시 행동력을 1 소모한다.
        /// AP가 0이 되면 OnAPDepleted를 발행한다.
        /// </summary>
        public void ConsumeAP()
        {
            if (_state.CurrentAP <= 0)
            {
                return;
            }

            _state.CurrentAP--;
            OnAPChanged?.Invoke(_state.CurrentAP, _state.MaxAP);

            if (_state.CurrentAP <= 0)
            {
                OnAPDepleted?.Invoke();
            }
        }

        /// <summary>
        /// 구역 첫 입장 시 전체 노드를 공개한다.
        /// </summary>
        /// <param name="areaId">구역 ID</param>
        public void RevealZone(string areaId)
        {
            if (_revealedZones.Contains(areaId))
            {
                return;
            }

            _revealedZones.Add(areaId);

            List<HexNode> zoneNodes = _zoneManager.GetNodesInZone(areaId);

            for (int i = 0; i < zoneNodes.Count; i++)
            {
                HexNode node = zoneNodes[i];

                if (!node.IsRevealed)
                {
                    node.IsRevealed = true;
                    OnNodeRevealed?.Invoke(node);
                }
            }

            OnZoneEntered?.Invoke(areaId);

            if (!_state.VisitedAreaIds.Contains(areaId))
            {
                _state.VisitedAreaIds.Add(areaId);
            }
        }

        /// <summary>
        /// 현재 노드의 이웃 노드를 공개한다.
        /// </summary>
        /// <param name="current">현재 노드</param>
        public void RevealAdjacentNodes(HexNode current)
        {
            for (int i = 0; i < current.Neighbors.Count; i++)
            {
                HexNode neighbor = current.Neighbors[i];

                if (!neighbor.IsRevealed)
                {
                    neighbor.IsRevealed = true;
                    OnNodeRevealed?.Invoke(neighbor);
                }
            }
        }

        /// <summary>
        /// 캠페인 목표 노드를 항상 표시한다.
        /// </summary>
        /// <param name="campaignTargetEventIds">캠페인 목표 이벤트 ID 목록</param>
        /// <param name="grid">그리드</param>
        public void RevealCampaignTargets(
            List<string> campaignTargetEventIds,
            Dictionary<(int, int, int), HexNode> grid)
        {
            if (campaignTargetEventIds == null || campaignTargetEventIds.Count == 0)
            {
                return;
            }

            var targetSet = new HashSet<string>(campaignTargetEventIds.Count);

            for (int i = 0; i < campaignTargetEventIds.Count; i++)
            {
                targetSet.Add(campaignTargetEventIds[i]);
            }

            foreach (HexNode node in grid.Values)
            {
                if (node.AssignedEvent != null && targetSet.Contains(node.AssignedEvent.id))
                {
                    if (!node.IsRevealed)
                    {
                        node.IsRevealed = true;
                        OnNodeRevealed?.Invoke(node);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 파티원의 maxAP를 합산하여 행동력을 초기화한다.
        /// </summary>
        private void InitializeAP(List<OwnedUnitData> party)
        {
            int totalAP = 0;

            for (int i = 0; i < party.Count; i++)
            {
                UnitData unitData = _dataManager.GetUnit(party[i].unitId);

                if (unitData != null)
                {
                    totalAP += unitData.maxAP;
                }
            }

            _state.MaxAP = totalAP;
            _state.CurrentAP = totalAP;
        }

        /// <summary>
        /// 노드 이동 시 새 구역 진입을 체크한다.
        /// </summary>
        private void CheckZoneEntry(HexNode node)
        {
            string areaId = node.AreaId;

            if (string.IsNullOrEmpty(areaId))
            {
                return;
            }

            if (!_revealedZones.Contains(areaId))
            {
                RevealZone(areaId);
            }
        }

        #endregion
    }
}
