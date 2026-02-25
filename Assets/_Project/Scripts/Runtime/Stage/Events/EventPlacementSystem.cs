using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 구역별 이벤트 배치 시스템.
    /// 레벨별 이벤트 수 계산, 인접성 규칙, SpawnTrigger 조건 평가,
    /// 레어도별 가중 랜덤 선택을 수행한다.
    /// </summary>
    public class EventPlacementSystem
    {
        #region Private Fields

        private readonly DataManager _dataManager;
        private Dictionary<string, List<EventData>> _eventPoolByArea;
        private Dictionary<Rarity, float> _spawnDropRates;

        #endregion

        #region Events

        /// <summary>
        /// 외부 데이터 조회 콜백 (OwnCharacter, OwnItem 등).
        /// (spawnTrigger, triggerValue) → 조건 충족 여부.
        /// </summary>
        public Func<SpawnTrigger, string, bool> OnExternalConditionCheck;

        #endregion

        #region Constructor

        /// <summary>
        /// EventPlacementSystem을 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public EventPlacementSystem(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 그리드에 이벤트를 배치한다.
        /// </summary>
        /// <param name="grid">생성된 그리드</param>
        /// <param name="zones">구역 매니저</param>
        /// <param name="record">현재 탐험 기록 (SpawnTrigger 평가용)</param>
        /// <param name="maxLevel">최대 레벨</param>
        public void PlaceEvents(
            Dictionary<(int, int, int), HexNode> grid,
            ZoneManager zones,
            ExplorationRecordData record,
            int maxLevel = HexGridGenerator.DEFAULT_MAX_LEVEL)
        {
            BuildEventPools();
            BuildSpawnDropRates();

            PlaceBoundaryEvents(grid, zones, record, maxLevel);
            PlaceZoneEvents(grid, zones, record, maxLevel);

            int totalPlaced = CountPlacedEvents(grid);
            Debug.Log($"[EventPlacementSystem] 이벤트 배치 완료. 총 {totalPlaced}개 이벤트 배치.");
        }

        /// <summary>
        /// SpawnTrigger 조건을 평가한다.
        /// </summary>
        public bool EvaluateSpawnTrigger(EventData eventData, ExplorationRecordData record)
        {
            if (eventData.spawnTrigger == SpawnTrigger.None)
            {
                return true;
            }

            switch (eventData.spawnTrigger)
            {
                case SpawnTrigger.ClearCampaign:
                    return EvaluateComparison(
                        record.countComplete,
                        eventData.comparisonOperator,
                        eventData.spawnTriggerValue);

                case SpawnTrigger.WinToBoss:
                    return ContainsId(record.eliminatedBossId, eventData.spawnTriggerValue);

                case SpawnTrigger.OwnCharacter:
                case SpawnTrigger.OwnItem:
                    if (OnExternalConditionCheck != null)
                    {
                        return OnExternalConditionCheck(eventData.spawnTrigger, eventData.spawnTriggerValue);
                    }
                    return false;

                default:
                    return true;
            }
        }

        #endregion

        #region Private Methods — Pool 구축

        /// <summary>
        /// EventTableSO에서 구역별 이벤트 풀을 구축한다.
        /// </summary>
        private void BuildEventPools()
        {
            _eventPoolByArea = new Dictionary<string, List<EventData>>(8);

            if (_dataManager.Events == null)
            {
                Debug.LogWarning("[EventPlacementSystem] EventTableSO가 null입니다.");
                return;
            }

            List<EventData> allEvents = _dataManager.Events.Entries;

            for (int i = 0; i < allEvents.Count; i++)
            {
                EventData evt = allEvents[i];
                string areaId = evt.areaId;

                if (string.IsNullOrEmpty(areaId))
                {
                    continue;
                }

                if (!_eventPoolByArea.ContainsKey(areaId))
                {
                    _eventPoolByArea[areaId] = new List<EventData>(32);
                }

                _eventPoolByArea[areaId].Add(evt);
            }
        }

        /// <summary>
        /// DropRateTableSO에서 SpawnEvent 카테고리의 레어도별 드랍율을 구축한다.
        /// </summary>
        private void BuildSpawnDropRates()
        {
            _spawnDropRates = new Dictionary<Rarity, float>(5);

            if (_dataManager.DropRates == null)
            {
                SetDefaultDropRates();
                return;
            }

            List<DropRateData> entries = _dataManager.DropRates.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                DropRateData rate = entries[i];

                if (rate.category == DropRateCategory.SpawnEvent)
                {
                    _spawnDropRates[rate.rarity] = rate.dropValue;
                }
            }

            if (_spawnDropRates.Count == 0)
            {
                SetDefaultDropRates();
            }
        }

        /// <summary>
        /// 기본 드랍율 설정 (DropRateTableSO 미설정 시).
        /// </summary>
        private void SetDefaultDropRates()
        {
            _spawnDropRates[Rarity.Common] = 50f;
            _spawnDropRates[Rarity.Uncommon] = 25f;
            _spawnDropRates[Rarity.Rare] = 15f;
            _spawnDropRates[Rarity.Unique] = 7.5f;
            _spawnDropRates[Rarity.Epic] = 2.5f;
        }

        #endregion

        #region Private Methods — 배치

        /// <summary>
        /// 경계 영역의 모든 칸에 이벤트를 배치한다.
        /// </summary>
        private void PlaceBoundaryEvents(
            Dictionary<(int, int, int), HexNode> grid,
            ZoneManager zones,
            ExplorationRecordData record,
            int maxLevel)
        {
            foreach (HexNode node in grid.Values)
            {
                if (!node.IsBoundary || node.Level < 8)
                {
                    continue;
                }

                EventData selectedEvent = SelectEventForNode(node, record);

                if (selectedEvent != null)
                {
                    node.AssignedEvent = selectedEvent;
                }
            }
        }

        /// <summary>
        /// 일반 구역의 이벤트를 레벨별로 배치한다.
        /// </summary>
        private void PlaceZoneEvents(
            Dictionary<(int, int, int), HexNode> grid,
            ZoneManager zones,
            ExplorationRecordData record,
            int maxLevel)
        {
            for (int level = 1; level <= maxLevel; level++)
            {
                PlaceEventsAtLevel(grid, zones, record, level);
            }
        }

        /// <summary>
        /// 특정 레벨의 이벤트를 배치한다.
        /// 인접성 규칙: 레벨 n에 이벤트가 있는 셀과 인접한 레벨 n+1 셀 중에서 선택.
        /// </summary>
        private void PlaceEventsAtLevel(
            Dictionary<(int, int, int), HexNode> grid,
            ZoneManager zones,
            ExplorationRecordData record,
            int level)
        {
            var levelNodes = GetNonBoundaryNodesAtLevel(grid, level);

            if (levelNodes.Count == 0)
            {
                return;
            }

            List<HexNode> candidates;

            if (level == 1)
            {
                candidates = levelNodes;
            }
            else
            {
                candidates = GetAdjacentToEventNodes(grid, levelNodes, level);

                if (candidates.Count == 0)
                {
                    candidates = levelNodes;
                }
            }

            int tileCount = levelNodes.Count;
            int minEvents = Mathf.Max(1, Mathf.CeilToInt(tileCount / 2f));
            int maxEvents = Mathf.Max(minEvents, tileCount - level);

            if (maxEvents < minEvents)
            {
                maxEvents = minEvents;
            }

            int eventCount = UnityEngine.Random.Range(minEvents, maxEvents + 1);
            eventCount = Mathf.Min(eventCount, candidates.Count);

            ShuffleList(candidates);

            int placed = 0;

            for (int i = 0; i < candidates.Count && placed < eventCount; i++)
            {
                HexNode node = candidates[i];

                if (node.AssignedEvent != null)
                {
                    continue;
                }

                EventData selectedEvent = SelectEventForNode(node, record);

                if (selectedEvent != null)
                {
                    node.AssignedEvent = selectedEvent;
                    placed++;
                }
            }
        }

        /// <summary>
        /// 경계가 아닌 특정 레벨의 노드 목록을 반환한다.
        /// </summary>
        private List<HexNode> GetNonBoundaryNodesAtLevel(
            Dictionary<(int, int, int), HexNode> grid, int level)
        {
            var result = new List<HexNode>(6 * level);

            foreach (HexNode node in grid.Values)
            {
                if (node.Level == level && !node.IsBoundary)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        /// <summary>
        /// 이전 레벨에 이벤트가 있는 노드와 인접한 현재 레벨 노드를 반환한다.
        /// </summary>
        private List<HexNode> GetAdjacentToEventNodes(
            Dictionary<(int, int, int), HexNode> grid,
            List<HexNode> levelNodes,
            int level)
        {
            var result = new List<HexNode>(levelNodes.Count);
            var addedSet = new HashSet<(int, int, int)>(levelNodes.Count);

            foreach (HexNode node in levelNodes)
            {
                if (addedSet.Contains(node.Key))
                {
                    continue;
                }

                for (int n = 0; n < node.Neighbors.Count; n++)
                {
                    HexNode neighbor = node.Neighbors[n];

                    if (neighbor.Level == level - 1 && neighbor.AssignedEvent != null)
                    {
                        result.Add(node);
                        addedSet.Add(node.Key);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 노드에 적합한 이벤트를 가중 랜덤으로 선택한다.
        /// </summary>
        private EventData SelectEventForNode(HexNode node, ExplorationRecordData record)
        {
            string areaId = node.AreaId;

            if (string.IsNullOrEmpty(areaId) || !_eventPoolByArea.ContainsKey(areaId))
            {
                return null;
            }

            List<EventData> pool = _eventPoolByArea[areaId];
            var validEvents = new List<EventData>(pool.Count);
            var weights = new List<float>(pool.Count);

            for (int i = 0; i < pool.Count; i++)
            {
                EventData evt = pool[i];

                if (node.Level < evt.minLevel || node.Level > evt.maxLevel)
                {
                    continue;
                }

                if (!EvaluateSpawnTrigger(evt, record))
                {
                    continue;
                }

                validEvents.Add(evt);

                float weight = 1f;

                if (_spawnDropRates.TryGetValue(evt.rarity, out float rarityWeight))
                {
                    weight = rarityWeight;
                }

                weights.Add(weight);
            }

            if (validEvents.Count == 0)
            {
                return null;
            }

            return WeightedRandomSelect(validEvents, weights);
        }

        #endregion

        #region Private Methods — 유틸

        /// <summary>
        /// 가중 랜덤 선택.
        /// </summary>
        private EventData WeightedRandomSelect(List<EventData> items, List<float> weights)
        {
            float totalWeight = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f)
            {
                return items[UnityEngine.Random.Range(0, items.Count)];
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float accumulated = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                accumulated += weights[i];

                if (roll <= accumulated)
                {
                    return items[i];
                }
            }

            return items[items.Count - 1];
        }

        /// <summary>
        /// 비교 연산자를 평가한다.
        /// </summary>
        private bool EvaluateComparison(int actualValue, ComparisonOperator op, string targetValueStr)
        {
            if (!int.TryParse(targetValueStr, out int targetValue))
            {
                return false;
            }

            switch (op)
            {
                case ComparisonOperator.Equal: return actualValue == targetValue;
                case ComparisonOperator.NotEqual: return actualValue != targetValue;
                case ComparisonOperator.GreaterThan: return actualValue > targetValue;
                case ComparisonOperator.LessThan: return actualValue < targetValue;
                case ComparisonOperator.GreaterOrEqual: return actualValue >= targetValue;
                case ComparisonOperator.LessOrEqual: return actualValue <= targetValue;
                default: return false;
            }
        }

        /// <summary>
        /// 세미콜론 구분 문자열에 특정 ID가 포함되어 있는지 확인한다.
        /// </summary>
        private bool ContainsId(string idList, string targetId)
        {
            if (string.IsNullOrEmpty(idList) || string.IsNullOrEmpty(targetId))
            {
                return false;
            }

            string[] ids = idList.Split(';');

            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i].Trim() == targetId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Fisher-Yates 셔플.
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// 배치된 이벤트 총 수를 센다.
        /// </summary>
        private int CountPlacedEvents(Dictionary<(int, int, int), HexNode> grid)
        {
            int count = 0;

            foreach (HexNode node in grid.Values)
            {
                if (node.AssignedEvent != null)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion
    }
}
