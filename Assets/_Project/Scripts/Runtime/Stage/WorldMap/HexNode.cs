using System;
using System.Collections.Generic;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 육각 그리드 셀의 런타임 데이터.
    /// 큐브 좌표(Q, R, S)와 구역, 이벤트, 노드 상태를 보유한다.
    /// </summary>
    public class HexNode
    {
        #region Constants

        /// <summary>
        /// 큐브 좌표 6방향 오프셋.
        /// 순서: N(+q,-s), NE(+q,-r), SE(-r,+s), S(-q,+s), SW(-q,+r), NW(+r,-s)
        /// </summary>
        public static readonly (int dq, int dr, int ds)[] CubeDirections = new (int, int, int)[]
        {
            ( 0, -1, +1),  // N
            (+1, -1,  0),  // NE
            (+1,  0, -1),  // SE
            ( 0, +1, -1),  // S
            (-1, +1,  0),  // SW
            (-1,  0, +1)   // NW
        };

        #endregion

        #region Public Properties

        /// <summary>큐브 좌표 Q</summary>
        public int Q { get; }

        /// <summary>큐브 좌표 R</summary>
        public int R { get; }

        /// <summary>큐브 좌표 S</summary>
        public int S { get; }

        /// <summary>시작점으로부터의 거리 (레벨)</summary>
        public int Level { get; }

        /// <summary>할당된 구역 ID</summary>
        public string AreaId { get; set; }

        /// <summary>배치된 이벤트 데이터 (null이면 빈 칸)</summary>
        public EventData AssignedEvent { get; set; }

        /// <summary>경계 영역 여부</summary>
        public bool IsBoundary { get; set; }

        /// <summary>플레이어가 방문한 노드인지</summary>
        public bool IsVisited { get; set; }

        /// <summary>플레이어에게 공개된 노드인지</summary>
        public bool IsRevealed { get; set; }

        /// <summary>이벤트가 완료된 상태인지</summary>
        public bool IsEventCompleted { get; set; }

        /// <summary>인접 노드 목록</summary>
        public List<HexNode> Neighbors { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// 큐브 좌표로 HexNode를 생성한다.
        /// </summary>
        /// <param name="q">큐브 좌표 Q</param>
        /// <param name="r">큐브 좌표 R</param>
        /// <param name="s">큐브 좌표 S</param>
        public HexNode(int q, int r, int s)
        {
            Q = q;
            R = r;
            S = s;
            Level = CubeDistance(0, 0, 0, q, r, s);
            Neighbors = new List<HexNode>(6);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 좌표 키를 반환한다. Dictionary 키로 사용.
        /// </summary>
        public (int, int, int) Key => (Q, R, S);

        /// <summary>
        /// 두 큐브 좌표 간 거리를 계산한다.
        /// </summary>
        public static int CubeDistance(int q1, int r1, int s1, int q2, int r2, int s2)
        {
            return Math.Max(Math.Max(Math.Abs(q1 - q2), Math.Abs(r1 - r2)), Math.Abs(s1 - s2));
        }

        /// <summary>
        /// 지정 방향의 이웃 좌표를 반환한다.
        /// </summary>
        /// <param name="directionIndex">CubeDirections 인덱스 (0~5)</param>
        public (int q, int r, int s) GetNeighborCoord(int directionIndex)
        {
            var dir = CubeDirections[directionIndex];
            return (Q + dir.dq, R + dir.dr, S + dir.ds);
        }

        /// <summary>
        /// 이 노드의 이웃인지 확인한다.
        /// </summary>
        public bool IsNeighbor(HexNode other)
        {
            return CubeDistance(Q, R, S, other.Q, other.R, other.S) == 1;
        }

        /// <summary>
        /// 디버그 문자열.
        /// </summary>
        public override string ToString()
        {
            return $"HexNode({Q},{R},{S}) Lv{Level} Area={AreaId ?? "None"}";
        }

        #endregion
    }
}
