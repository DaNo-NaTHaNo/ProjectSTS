using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 육각 그리드 노드에 구역(Area)을 할당하고 조회하는 매니저.
    /// 큐브 좌표 기반으로 방위와 거리를 판별하여 구역을 결정한다.
    /// </summary>
    public class ZoneManager
    {
        #region Constants

        /// <summary>메인 구역 C의 최대 레벨</summary>
        private const int CENTER_ZONE_MAX_LEVEL = 7;

        /// <summary>방향별 구역의 최소 레벨</summary>
        private const int DIRECTIONAL_ZONE_MIN_LEVEL = 8;

        /// <summary>방향별 구역의 최대 레벨</summary>
        private const int DIRECTIONAL_ZONE_MAX_LEVEL = 28;

        #endregion

        #region Private Fields

        private readonly DataManager _dataManager;
        private Dictionary<string, AreaData> _areaLookup;
        private Dictionary<string, List<HexNode>> _zoneNodes;

        #endregion

        #region Constructor

        /// <summary>
        /// ZoneManager를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public ZoneManager(DataManager dataManager)
        {
            _dataManager = dataManager;
            _areaLookup = new Dictionary<string, AreaData>(8);
            _zoneNodes = new Dictionary<string, List<HexNode>>(8);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 그리드의 모든 노드에 구역을 할당한다.
        /// </summary>
        /// <param name="grid">생성된 그리드 노드 맵</param>
        public void AssignZones(Dictionary<(int, int, int), HexNode> grid)
        {
            BuildAreaLookup();
            _zoneNodes.Clear();

            foreach (HexNode node in grid.Values)
            {
                string areaId = DetermineAreaId(node);
                node.AreaId = areaId;
                node.IsBoundary = IsBoundaryCoord(node.Q, node.R, node.S, node.Level);

                if (!string.IsNullOrEmpty(areaId))
                {
                    if (!_zoneNodes.ContainsKey(areaId))
                    {
                        _zoneNodes[areaId] = new List<HexNode>(64);
                    }

                    _zoneNodes[areaId].Add(node);
                }
            }

            Debug.Log($"[ZoneManager] 구역 할당 완료. {_zoneNodes.Count}개 구역.");
        }

        /// <summary>
        /// 노드의 구역 데이터를 반환한다.
        /// </summary>
        public AreaData GetAreaForNode(HexNode node)
        {
            if (string.IsNullOrEmpty(node.AreaId))
            {
                return null;
            }

            _areaLookup.TryGetValue(node.AreaId, out AreaData area);
            return area;
        }

        /// <summary>
        /// 특정 구역의 모든 노드를 반환한다.
        /// </summary>
        public List<HexNode> GetNodesInZone(string areaId)
        {
            if (_zoneNodes.TryGetValue(areaId, out List<HexNode> nodes))
            {
                return nodes;
            }

            return new List<HexNode>(0);
        }

        /// <summary>
        /// 경계 영역 노드인지 판별한다.
        /// 큐브 좌표에서 q, r, s 중 하나가 0인 셀이 경계 영역이다 (레벨 8+).
        /// </summary>
        public bool IsBoundaryNode(HexNode node)
        {
            return IsBoundaryCoord(node.Q, node.R, node.S, node.Level);
        }

        /// <summary>
        /// 특정 구역 내 특정 레벨의 노드 목록을 반환한다.
        /// </summary>
        public List<HexNode> GetNodesInZoneAtLevel(string areaId, int level)
        {
            var zoneNodes = GetNodesInZone(areaId);
            var result = new List<HexNode>(zoneNodes.Count);

            for (int i = 0; i < zoneNodes.Count; i++)
            {
                if (zoneNodes[i].Level == level)
                {
                    result.Add(zoneNodes[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 레벨의 경계 영역 노드 목록을 반환한다.
        /// </summary>
        public List<HexNode> GetBoundaryNodesAtLevel(int level)
        {
            var result = new List<HexNode>(6);

            foreach (var pair in _zoneNodes)
            {
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    HexNode node = pair.Value[i];

                    if (node.Level == level && node.IsBoundary)
                    {
                        result.Add(node);
                    }
                }
            }

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// AreaTableSO에서 구역 데이터를 조회하여 룩업 테이블을 구축한다.
        /// </summary>
        private void BuildAreaLookup()
        {
            _areaLookup.Clear();

            if (_dataManager.Areas == null)
            {
                Debug.LogWarning("[ZoneManager] AreaTableSO가 null입니다.");
                return;
            }

            List<AreaData> entries = _dataManager.Areas.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                AreaData area = entries[i];

                if (!string.IsNullOrEmpty(area.id))
                {
                    _areaLookup[area.id] = area;
                }
            }
        }

        /// <summary>
        /// 노드의 큐브 좌표와 레벨을 기반으로 구역 ID를 결정한다.
        /// </summary>
        private string DetermineAreaId(HexNode node)
        {
            int level = node.Level;

            if (level == 0)
            {
                return FindAreaIdByCardinalPoint("C");
            }

            if (level <= CENTER_ZONE_MAX_LEVEL)
            {
                return FindAreaIdByCardinalPoint("C");
            }

            if (level > DIRECTIONAL_ZONE_MAX_LEVEL)
            {
                return null;
            }

            if (IsBoundaryCoord(node.Q, node.R, node.S, level))
            {
                return FindBoundaryAreaId(level);
            }

            CardinalPoint direction = DetermineDirection(node.Q, node.R, node.S);
            return FindAreaIdByCardinalPoint(direction.ToString());
        }

        /// <summary>
        /// 큐브 좌표의 최대 절대값 성분으로 6방위를 결정한다.
        /// </summary>
        private CardinalPoint DetermineDirection(int q, int r, int s)
        {
            int absQ = Math.Abs(q);
            int absR = Math.Abs(r);
            int absS = Math.Abs(s);

            if (absS >= absQ && absS >= absR)
            {
                return s < 0 ? CardinalPoint.N : CardinalPoint.S;
            }

            if (absQ >= absR && absQ >= absS)
            {
                return q > 0 ? CardinalPoint.NE : CardinalPoint.SW;
            }

            return r > 0 ? CardinalPoint.SW : CardinalPoint.NE;
        }

        /// <summary>
        /// 경계 영역인지 판별한다.
        /// 레벨 8 이상에서 큐브 좌표의 q, r, s 중 하나가 0이면 경계.
        /// </summary>
        private bool IsBoundaryCoord(int q, int r, int s, int level)
        {
            if (level < DIRECTIONAL_ZONE_MIN_LEVEL)
            {
                return false;
            }

            return q == 0 || r == 0 || s == 0;
        }

        /// <summary>
        /// 방향 문자열로 AreaData를 찾는다.
        /// AreaData의 areaCardinalPoint에 해당 방향이 포함된 항목을 반환한다.
        /// </summary>
        private string FindAreaIdByCardinalPoint(string direction)
        {
            foreach (var pair in _areaLookup)
            {
                AreaData area = pair.Value;
                string points = area.areaCardinalPoint;

                if (string.IsNullOrEmpty(points))
                {
                    continue;
                }

                if (points.Contains(direction))
                {
                    return area.id;
                }
            }

            return null;
        }

        /// <summary>
        /// 경계 영역의 AreaData ID를 반환한다.
        /// 경계 영역 전용 AreaData가 없으면, 레벨에 맞는 범위의 구역 중 첫 번째를 반환한다.
        /// </summary>
        private string FindBoundaryAreaId(int level)
        {
            foreach (var pair in _areaLookup)
            {
                AreaData area = pair.Value;

                if (area.areaLevelMin <= level && area.areaLevelMax >= level)
                {
                    string points = area.areaCardinalPoint;

                    if (string.IsNullOrEmpty(points))
                    {
                        return area.id;
                    }
                }
            }

            return FindAreaIdByCardinalPoint("C");
        }

        #endregion
    }
}
