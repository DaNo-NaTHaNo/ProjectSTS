using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 이벤트 타임라인 관리 클래스.
    /// 전투 중 특정 조건 충족 시 연출 타임라인을 트리거하고,
    /// IBattleTimelineHandler를 통해 실행한다.
    /// 현재 Phase에서는 인터페이스 + 트리거 감지만 구현한다 (연출 실행은 스텁).
    /// </summary>
    public class BattleTimelineManager
    {
        #region Private Fields

        private readonly DataManager _dataManager;
        private IBattleTimelineHandler _handler;
        private readonly List<BattleTimelineData> _timelines;
        private readonly HashSet<string> _triggeredOnce;

        #endregion

        #region Constructor

        /// <summary>
        /// BattleTimelineManager를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public BattleTimelineManager(DataManager dataManager)
        {
            _dataManager = dataManager;
            _timelines = new List<BattleTimelineData>(8);
            _triggeredOnce = new HashSet<string>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// eventId에 해당하는 타임라인을 로드한다.
        /// </summary>
        /// <param name="eventId">전투 이벤트 ID</param>
        public void Initialize(string eventId)
        {
            _timelines.Clear();
            _triggeredOnce.Clear();

            if (string.IsNullOrEmpty(eventId))
            {
                return;
            }

            List<BattleTimelineData> allTimelines = _dataManager.BattleTimelines.Entries;

            for (int i = 0; i < allTimelines.Count; i++)
            {
                if (allTimelines[i].eventId == eventId)
                {
                    _timelines.Add(allTimelines[i]);
                }
            }

            if (_timelines.Count > 0)
            {
                Debug.Log($"[BattleTimelineManager] 이벤트 '{eventId}'의 타임라인 {_timelines.Count}개 로드.");
            }
        }

        /// <summary>
        /// 연출 핸들러를 설정한다. null이면 트리거 감지만 수행하고 실행은 스킵한다.
        /// </summary>
        /// <param name="handler">연출 핸들러</param>
        public void SetHandler(IBattleTimelineHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// 트리거 조건을 체크하고 매칭 타임라인을 실행한다.
        /// </summary>
        /// <param name="triggerType">트리거 타입</param>
        /// <param name="triggerTarget">트리거 대상 (유닛 ID 등)</param>
        /// <param name="triggerValue">트리거 값</param>
        /// <param name="state">전투 상태</param>
        public void CheckTriggers(TimelineTriggerType triggerType, string triggerTarget,
            string triggerValue, BattleState state)
        {
            var matched = new List<BattleTimelineData>(4);

            for (int i = 0; i < _timelines.Count; i++)
            {
                BattleTimelineData timeline = _timelines[i];

                if (timeline.triggerType != triggerType)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(timeline.triggerTarget) &&
                    timeline.triggerTarget != triggerTarget)
                {
                    continue;
                }

                if (!timeline.isRepeatable && _triggeredOnce.Contains(timeline.id))
                {
                    continue;
                }

                if (EvaluateTriggerValue(timeline, triggerValue, state))
                {
                    matched.Add(timeline);
                }
            }

            if (matched.Count == 0)
            {
                return;
            }

            matched.Sort((a, b) => b.priority.CompareTo(a.priority));

            for (int i = 0; i < matched.Count; i++)
            {
                BattleTimelineData timeline = matched[i];

                if (!timeline.isRepeatable)
                {
                    _triggeredOnce.Add(timeline.id);
                }

                ExecuteTimeline(timeline);
            }
        }

        #endregion

        #region Private Methods

        private bool EvaluateTriggerValue(BattleTimelineData timeline, string triggerValue, BattleState state)
        {
            if (string.IsNullOrEmpty(timeline.triggerValue))
            {
                return true;
            }

            switch (timeline.triggerType)
            {
                case TimelineTriggerType.TurnCount:
                    if (int.TryParse(timeline.triggerValue, out int turnCount))
                    {
                        return state.CurrentTurn == turnCount;
                    }
                    return false;

                case TimelineTriggerType.HpPercent:
                case TimelineTriggerType.EnemyCount:
                case TimelineTriggerType.PartyCount:
                    if (triggerValue != null && float.TryParse(triggerValue, out float actual) &&
                        float.TryParse(timeline.triggerValue, out float expected))
                    {
                        return actual <= expected;
                    }
                    return false;

                case TimelineTriggerType.UnitDown:
                case TimelineTriggerType.UnitSpawn:
                    return true;

                case TimelineTriggerType.OnStatus:
                    return triggerValue == timeline.triggerValue;

                default:
                    return true;
            }
        }

        private void ExecuteTimeline(BattleTimelineData timeline)
        {
            if (string.IsNullOrEmpty(timeline.actionGroupId))
            {
                return;
            }

            List<BattleActionData> actions = GetActionGroup(timeline.actionGroupId);

            if (actions.Count == 0)
            {
                Debug.LogWarning($"[BattleTimelineManager] 액션 그룹 '{timeline.actionGroupId}'에 행동이 없습니다.");
                return;
            }

            if (_handler != null)
            {
                _handler.ExecuteActionGroup(actions);
            }
            else
            {
                Debug.Log($"[BattleTimelineManager] 타임라인 '{timeline.id}' 트리거 감지 (핸들러 미설정, 연출 스킵).");
            }
        }

        private List<BattleActionData> GetActionGroup(string groupId)
        {
            var actions = new List<BattleActionData>(4);
            List<BattleActionData> allActions = _dataManager.BattleActions.Entries;

            for (int i = 0; i < allActions.Count; i++)
            {
                if (allActions[i].groupId == groupId)
                {
                    actions.Add(allActions[i]);
                }
            }

            actions.Sort((a, b) => a.sequence.CompareTo(b.sequence));
            return actions;
        }

        #endregion
    }
}
