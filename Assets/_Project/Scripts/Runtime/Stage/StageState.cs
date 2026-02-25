using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 스테이지 전체의 공유 상태 컨테이너.
    /// 그리드, 현재 노드, 행동력, 방문 이력, 탐험 기록, 보상 카운트 등을 보유한다.
    /// </summary>
    public class StageState
    {
        #region Public Properties

        /// <summary>
        /// 생성된 그리드 노드 맵.
        /// </summary>
        public Dictionary<(int, int, int), HexNode> Grid { get; set; }

        /// <summary>
        /// 시작점 노드.
        /// </summary>
        public HexNode StartNode { get; set; }

        /// <summary>
        /// 현재 플레이어 위치 노드.
        /// </summary>
        public HexNode CurrentNode { get; set; }

        /// <summary>
        /// 최대 행동력.
        /// </summary>
        public int MaxAP { get; set; }

        /// <summary>
        /// 현재 행동력.
        /// </summary>
        public int CurrentAP { get; set; }

        /// <summary>
        /// 현재 스테이지 페이즈.
        /// </summary>
        public StagePhase CurrentPhase { get; set; }

        /// <summary>
        /// 스테이지 결과.
        /// </summary>
        public StageResult Result { get; set; }

        /// <summary>
        /// 스테이지 종료 사유.
        /// </summary>
        public StageEndReason EndReason { get; set; }

        /// <summary>
        /// 방문한 구역 ID 목록.
        /// </summary>
        public List<string> VisitedAreaIds { get; }

        /// <summary>
        /// 현재 탐험 기록 데이터.
        /// </summary>
        public ExplorationRecordData Record { get; set; }

        /// <summary>
        /// 완료한 이벤트 수.
        /// </summary>
        public int CompletedEventCount { get; set; }

        /// <summary>
        /// 추가 보상 카운트 (엘리트/사건/VN 완료 시 +1).
        /// </summary>
        public int BonusRewardCount { get; set; }

        /// <summary>
        /// 파티 편성 데이터 목록.
        /// </summary>
        public List<OwnedUnitData> Party { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// StageState를 생성한다.
        /// </summary>
        public StageState()
        {
            VisitedAreaIds = new List<string>(8);
            Party = new List<OwnedUnitData>(3);
            CurrentPhase = StagePhase.None;
            Result = StageResult.None;
            EndReason = StageEndReason.None;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스테이지 상태를 초기화한다.
        /// </summary>
        /// <param name="party">파티 편성 데이터</param>
        public void Initialize(List<OwnedUnitData> party)
        {
            Party.Clear();
            Party.AddRange(party);

            VisitedAreaIds.Clear();
            CompletedEventCount = 0;
            BonusRewardCount = 0;
            CurrentPhase = StagePhase.Initializing;
            Result = StageResult.None;
            EndReason = StageEndReason.None;

            Record = new ExplorationRecordData();
        }

        /// <summary>
        /// 이벤트 완료를 기록한다.
        /// </summary>
        /// <param name="eventType">완료된 이벤트 종류</param>
        public void RecordEventCompletion(EventType eventType)
        {
            CompletedEventCount++;

            switch (eventType)
            {
                case EventType.BattleNormal:
                case EventType.BattleElite:
                case EventType.BattleBoss:
                case EventType.BattleEvent:
                    Record.countBattleNow++;
                    Record.countBattleAll++;
                    break;

                case EventType.VisualNovel:
                    Record.countVisualNovelNow++;
                    Record.countVisualNovelAll++;
                    break;

                case EventType.Encounter:
                    Record.countEncountNow++;
                    Record.countEncountAll++;
                    break;
            }

            if (eventType == EventType.BattleElite ||
                eventType == EventType.Encounter ||
                eventType == EventType.VisualNovel)
            {
                BonusRewardCount++;
            }
        }

        #endregion
    }
}
