using System.Collections.Generic;
using UnityEngine;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 큐브 좌표 기반 방사형 육각 그리드 생성기.
    /// 시작점(0,0,0)을 중심으로 최대 레벨까지 링을 생성하고 인접 노드를 연결한다.
    /// </summary>
    public class HexGridGenerator
    {
        #region Constants

        /// <summary>기본 최대 레벨</summary>
        public const int DEFAULT_MAX_LEVEL = 28;

        #endregion

        #region Private Fields

        private Dictionary<(int, int, int), HexNode> _grid;

        #endregion

        #region Public Properties

        /// <summary>
        /// 생성된 그리드 전체 노드 맵.
        /// </summary>
        public Dictionary<(int, int, int), HexNode> Grid => _grid;

        /// <summary>
        /// 시작점 노드.
        /// </summary>
        public HexNode StartNode { get; private set; }

        /// <summary>
        /// 그리드 내 노드 총 수.
        /// </summary>
        public int NodeCount => _grid != null ? _grid.Count : 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// 방사형 육각 그리드를 생성한다.
        /// 레벨 0(시작점) ~ maxLevel까지 링을 생성하고, 인접 노드를 연결한다.
        /// </summary>
        /// <param name="maxLevel">최대 레벨 (기본 28)</param>
        /// <returns>생성된 그리드 노드 맵</returns>
        public Dictionary<(int, int, int), HexNode> GenerateGrid(int maxLevel = DEFAULT_MAX_LEVEL)
        {
            int estimatedCount = 1 + 3 * maxLevel * (maxLevel + 1);
            _grid = new Dictionary<(int, int, int), HexNode>(estimatedCount);

            StartNode = new HexNode(0, 0, 0);
            _grid[StartNode.Key] = StartNode;

            for (int ring = 1; ring <= maxLevel; ring++)
            {
                GenerateRing(ring);
            }

            ConnectNeighbors();

            Debug.Log($"[HexGridGenerator] 그리드 생성 완료. 총 {_grid.Count}개 노드, 최대 레벨 {maxLevel}.");

            return _grid;
        }

        /// <summary>
        /// 좌표로 노드를 조회한다.
        /// </summary>
        public HexNode GetNode(int q, int r, int s)
        {
            _grid.TryGetValue((q, r, s), out HexNode node);
            return node;
        }

        /// <summary>
        /// 특정 레벨의 모든 노드를 반환한다.
        /// </summary>
        /// <param name="level">레벨</param>
        public List<HexNode> GetNodesAtLevel(int level)
        {
            if (level == 0)
            {
                return new List<HexNode>(1) { StartNode };
            }

            int expectedCount = 6 * level;
            var result = new List<HexNode>(expectedCount);

            foreach (HexNode node in _grid.Values)
            {
                if (node.Level == level)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        /// <summary>
        /// 모든 노드를 리스트로 반환한다.
        /// </summary>
        public List<HexNode> GetAllNodes()
        {
            var result = new List<HexNode>(_grid.Count);
            result.AddRange(_grid.Values);
            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 지정 레벨(링)의 셀을 생성한다.
        /// 큐브 좌표 링 순회: 시작점(ring, -ring, 0)에서 6방향으로 각 ring번 이동.
        /// </summary>
        private void GenerateRing(int ring)
        {
            int q = ring;
            int r = -ring;
            int s = 0;

            for (int side = 0; side < 6; side++)
            {
                var dir = HexNode.CubeDirections[side];

                for (int step = 0; step < ring; step++)
                {
                    var key = (q, r, s);

                    if (!_grid.ContainsKey(key))
                    {
                        _grid[key] = new HexNode(q, r, s);
                    }

                    q += dir.dq;
                    r += dir.dr;
                    s += dir.ds;
                }
            }
        }

        /// <summary>
        /// 모든 노드의 인접 관계를 설정한다.
        /// </summary>
        private void ConnectNeighbors()
        {
            foreach (HexNode node in _grid.Values)
            {
                for (int d = 0; d < 6; d++)
                {
                    var neighborCoord = node.GetNeighborCoord(d);

                    if (_grid.TryGetValue(neighborCoord, out HexNode neighbor))
                    {
                        if (!node.Neighbors.Contains(neighbor))
                        {
                            node.Neighbors.Add(neighbor);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
