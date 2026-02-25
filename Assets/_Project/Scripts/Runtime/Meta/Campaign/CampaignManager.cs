using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Meta
{
    /// <summary>
    /// 캠페인 해금, 목표 진행, 완료 처리, 추적 기능을 관리한다.
    /// 마스터 데이터 SO를 변경하지 않고, 런타임 상태를 별도 Dictionary로 추적한다.
    /// </summary>
    public class CampaignManager
    {
        #region Private Fields

        private readonly PlayerDataManager _playerData;
        private readonly DataManager _dataManager;

        private Dictionary<string, bool> _campaignUnlockedState;
        private Dictionary<string, bool> _campaignCompletionState;
        private Dictionary<string, bool> _goalCompletionState;
        private string _trackedCampaignId;

        #endregion

        #region Events

        /// <summary>
        /// 캠페인 해금 시 발행.
        /// </summary>
        public event Action<CampaignData> OnCampaignUnlocked;

        /// <summary>
        /// 캠페인 완료 시 발행.
        /// </summary>
        public event Action<CampaignData> OnCampaignCompleted;

        /// <summary>
        /// 목표 완료 시 발행.
        /// </summary>
        public event Action<CampaignGoalGroupData> OnGoalCompleted;

        #endregion

        #region Constructor

        /// <summary>
        /// CampaignManager를 생성한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public CampaignManager(PlayerDataManager playerData, DataManager dataManager)
        {
            _playerData = playerData;
            _dataManager = dataManager;
            _campaignUnlockedState = new Dictionary<string, bool>(16);
            _campaignCompletionState = new Dictionary<string, bool>(16);
            _goalCompletionState = new Dictionary<string, bool>(64);
        }

        #endregion

        #region Public Methods — 해금 평가

        /// <summary>
        /// 전체 캠페인의 해금 조건을 재평가한다. 메타 레이어 진입 시 호출한다.
        /// </summary>
        public void EvaluateUnlocks()
        {
            List<CampaignData> allCampaigns = _dataManager.Campaigns.Entries;

            for (int i = 0; i < allCampaigns.Count; i++)
            {
                CampaignData campaign = allCampaigns[i];
                string id = campaign.id;

                if (IsCampaignUnlocked(id))
                {
                    continue;
                }

                if (CheckUnlockCondition(campaign))
                {
                    _campaignUnlockedState[id] = true;
                    OnCampaignUnlocked?.Invoke(campaign);
                    Debug.Log($"[CampaignManager] 캠페인 해금: {campaign.name} ({id})");
                }
            }
        }

        /// <summary>
        /// 단일 캠페인의 해금 조건을 확인한다.
        /// </summary>
        public bool CheckUnlockCondition(CampaignData campaign)
        {
            return EvaluateTrigger(campaign.unlockType, campaign.unlockId);
        }

        /// <summary>
        /// 캠페인이 해금되었는지 확인한다.
        /// </summary>
        public bool IsCampaignUnlocked(string campaignId)
        {
            return _campaignUnlockedState.TryGetValue(campaignId, out bool unlocked) && unlocked;
        }

        #endregion

        #region Public Methods — 목표 진행

        /// <summary>
        /// 전체 활성 캠페인의 목표 진행 상태를 평가한다.
        /// </summary>
        public void EvaluateGoalProgress()
        {
            List<CampaignData> activeCampaigns = GetActiveCampaigns();

            for (int i = 0; i < activeCampaigns.Count; i++)
            {
                CampaignData campaign = activeCampaigns[i];

                if (string.IsNullOrEmpty(campaign.groupId))
                {
                    continue;
                }

                EvaluateGoalsForGroup(campaign.groupId);
            }
        }

        /// <summary>
        /// 현재 진행해야 할 목표를 반환한다 (isEssential 미완료 시 대기).
        /// </summary>
        public CampaignGoalGroupData GetCurrentGoal(string groupId)
        {
            List<CampaignGoalGroupData> goals = GetGoalsByGroup(groupId);
            int currentSequence = GetCurrentSequence(goals);

            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].sequence == currentSequence && !IsGoalCompleted(groupId, goals[i].sequence))
                {
                    return goals[i];
                }
            }

            return null;
        }

        #endregion

        #region Public Methods — 완료 처리

        /// <summary>
        /// 완료 가능한 캠페인을 확인하고 완료 처리한다.
        /// </summary>
        /// <returns>이번에 완료된 캠페인 목록</returns>
        public List<CampaignData> CheckAndCompleteCampaigns()
        {
            var completedNow = new List<CampaignData>(4);
            List<CampaignData> activeCampaigns = GetActiveCampaigns();

            for (int i = 0; i < activeCampaigns.Count; i++)
            {
                CampaignData campaign = activeCampaigns[i];

                if (string.IsNullOrEmpty(campaign.groupId))
                {
                    continue;
                }

                if (IsGroupCleared(campaign.groupId))
                {
                    CompleteCampaign(campaign);
                    completedNow.Add(campaign);
                }
            }

            return completedNow;
        }

        /// <summary>
        /// 캠페인 보상을 수집한다 (기본 rewards + additionalRewards).
        /// </summary>
        /// <returns>보상 ID 목록</returns>
        public List<string> CollectRewards(CampaignData campaign)
        {
            var rewards = new List<string>(8);

            if (!string.IsNullOrEmpty(campaign.rewards))
            {
                AddSemicolonIds(rewards, campaign.rewards);
            }

            if (!string.IsNullOrEmpty(campaign.groupId))
            {
                List<CampaignGoalGroupData> goals = GetGoalsByGroup(campaign.groupId);

                for (int i = 0; i < goals.Count; i++)
                {
                    if (!string.IsNullOrEmpty(goals[i].additionalRewards))
                    {
                        AddSemicolonIds(rewards, goals[i].additionalRewards);
                    }
                }
            }

            return rewards;
        }

        /// <summary>
        /// 캠페인을 완료 처리한다.
        /// </summary>
        public void CompleteCampaign(CampaignData campaign)
        {
            _campaignCompletionState[campaign.id] = true;
            OnCampaignCompleted?.Invoke(campaign);
            Debug.Log($"[CampaignManager] 캠페인 완료: {campaign.name} ({campaign.id})");
        }

        #endregion

        #region Public Methods — 추적

        /// <summary>
        /// 추적 캠페인을 설정한다.
        /// </summary>
        public void SetTrackedCampaign(string campaignId)
        {
            _trackedCampaignId = campaignId;
        }

        /// <summary>
        /// 현재 추적 중인 캠페인 ID를 반환한다.
        /// </summary>
        public string GetTrackedCampaignId()
        {
            return _trackedCampaignId;
        }

        /// <summary>
        /// 추적 중인 캠페인 데이터를 반환한다.
        /// </summary>
        public CampaignData GetTrackedCampaign()
        {
            if (string.IsNullOrEmpty(_trackedCampaignId))
            {
                return null;
            }

            return _dataManager.GetCampaign(_trackedCampaignId);
        }

        /// <summary>
        /// 추적 캠페인의 현재 목표를 반환한다.
        /// </summary>
        public CampaignGoalGroupData GetTrackedCurrentGoal()
        {
            CampaignData campaign = GetTrackedCampaign();

            if (campaign == null || string.IsNullOrEmpty(campaign.groupId))
            {
                return null;
            }

            return GetCurrentGoal(campaign.groupId);
        }

        #endregion

        #region Public Methods — 조회

        /// <summary>
        /// 해금되었으나 미완료인 캠페인 목록을 반환한다.
        /// </summary>
        public List<CampaignData> GetActiveCampaigns()
        {
            List<CampaignData> allCampaigns = _dataManager.Campaigns.Entries;
            var result = new List<CampaignData>(8);

            for (int i = 0; i < allCampaigns.Count; i++)
            {
                string id = allCampaigns[i].id;

                if (IsCampaignUnlocked(id) && !IsCampaignCompleted(id))
                {
                    result.Add(allCampaigns[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 완료된 캠페인 목록을 반환한다.
        /// </summary>
        public List<CampaignData> GetCompletedCampaigns()
        {
            List<CampaignData> allCampaigns = _dataManager.Campaigns.Entries;
            var result = new List<CampaignData>(8);

            for (int i = 0; i < allCampaigns.Count; i++)
            {
                if (IsCampaignCompleted(allCampaigns[i].id))
                {
                    result.Add(allCampaigns[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 그룹 내 목표 목록을 sequence 순으로 반환한다.
        /// </summary>
        public List<CampaignGoalGroupData> GetGoalsByGroup(string groupId)
        {
            List<CampaignGoalGroupData> allGoals = _dataManager.CampaignGoalGroups.Entries;
            var result = new List<CampaignGoalGroupData>(8);

            for (int i = 0; i < allGoals.Count; i++)
            {
                if (allGoals[i].groupId == groupId)
                {
                    result.Add(allGoals[i]);
                }
            }

            result.Sort((a, b) => a.sequence.CompareTo(b.sequence));
            return result;
        }

        /// <summary>
        /// 캠페인이 완료되었는지 확인한다.
        /// </summary>
        public bool IsCampaignCompleted(string campaignId)
        {
            return _campaignCompletionState.TryGetValue(campaignId, out bool completed) && completed;
        }

        #endregion

        #region Public Methods — 상태 복원

        /// <summary>
        /// 캠페인 해금 상태를 외부에서 설정한다 (세이브 데이터 복원용).
        /// </summary>
        public void SetCampaignUnlockedState(string campaignId, bool unlocked)
        {
            _campaignUnlockedState[campaignId] = unlocked;
        }

        /// <summary>
        /// 캠페인 완료 상태를 외부에서 설정한다 (세이브 데이터 복원용).
        /// </summary>
        public void SetCampaignCompletionState(string campaignId, bool completed)
        {
            _campaignCompletionState[campaignId] = completed;
        }

        /// <summary>
        /// 목표 완료 상태를 외부에서 설정한다 (세이브 데이터 복원용).
        /// </summary>
        public void SetGoalCompletionState(string goalKey, bool completed)
        {
            _goalCompletionState[goalKey] = completed;
        }

        #endregion

        #region Private Methods — 트리거 평가

        /// <summary>
        /// CampaignTriggerType에 따라 조건을 평가한다.
        /// </summary>
        private bool EvaluateTrigger(CampaignTriggerType triggerType, string triggerValue)
        {
            ExplorationRecordData record = _playerData.GetExplorationRecord();

            switch (triggerType)
            {
                case CampaignTriggerType.ClearCampaign:
                    return IsCampaignCompleted(triggerValue);

                case CampaignTriggerType.ClearEvent:
                    // 이벤트 클리어 여부는 탐험 기록에서 확인 (구체적 이벤트 ID 추적은 향후 확장)
                    return false;

                case CampaignTriggerType.EarnUnit:
                    return _playerData.HasUnit(triggerValue);

                case CampaignTriggerType.EarnCard:
                    return _playerData.HasCard(triggerValue);

                case CampaignTriggerType.EarnItem:
                    return _playerData.HasItem(triggerValue);

                case CampaignTriggerType.EarnSkill:
                    return HasSkill(triggerValue);

                case CampaignTriggerType.BattleCount:
                    return CompareCount(record.countBattleAll, triggerValue);

                case CampaignTriggerType.MoveCount:
                    // moveCount 필드가 ExplorationRecordData에 없으므로 향후 확장
                    return false;

                case CampaignTriggerType.EventCount:
                    int totalEvents = record.countBattleAll + record.countVisualNovelAll + record.countEncountAll;
                    return CompareCount(totalEvents, triggerValue);

                default:
                    return false;
            }
        }

        /// <summary>
        /// 스킬 보유 여부를 확인한다.
        /// 보유 유닛 중 하나라도 해당 스킬을 장비하고 있으면 true.
        /// </summary>
        private bool HasSkill(string skillId)
        {
            List<OwnedUnitData> units = _playerData.GetOwnedUnits();

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].editedSkill == skillId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 실제 카운트가 목표 값 이상인지 확인한다.
        /// </summary>
        private bool CompareCount(int actualCount, string targetValueStr)
        {
            if (int.TryParse(targetValueStr, out int targetValue))
            {
                return actualCount >= targetValue;
            }

            return false;
        }

        #endregion

        #region Private Methods — 목표 관리

        /// <summary>
        /// 특정 그룹의 목표들을 평가한다.
        /// </summary>
        private void EvaluateGoalsForGroup(string groupId)
        {
            List<CampaignGoalGroupData> goals = GetGoalsByGroup(groupId);
            int currentSequence = GetCurrentSequence(goals);

            for (int i = 0; i < goals.Count; i++)
            {
                CampaignGoalGroupData goal = goals[i];

                if (goal.sequence != currentSequence)
                {
                    continue;
                }

                string goalKey = MakeGoalKey(groupId, goal.sequence);

                if (IsGoalCompleted(groupId, goal.sequence))
                {
                    continue;
                }

                if (EvaluateTrigger(goal.triggerType, goal.triggerValue))
                {
                    _goalCompletionState[goalKey] = true;
                    OnGoalCompleted?.Invoke(goal);
                    Debug.Log($"[CampaignManager] 목표 완료: {goal.name} ({goalKey})");
                }
            }
        }

        /// <summary>
        /// 현재 진행해야 할 시퀀스를 반환한다.
        /// isEssential이 미완료인 가장 낮은 시퀀스를 반환한다.
        /// </summary>
        private int GetCurrentSequence(List<CampaignGoalGroupData> sortedGoals)
        {
            int maxSequence = 0;

            for (int i = 0; i < sortedGoals.Count; i++)
            {
                CampaignGoalGroupData goal = sortedGoals[i];

                if (goal.sequence > maxSequence)
                {
                    maxSequence = goal.sequence;
                }

                if (goal.isEssential && !IsGoalCompleted(goal.groupId, goal.sequence))
                {
                    return goal.sequence;
                }
            }

            // 모든 essential 완료 → 마지막 시퀀스
            return maxSequence;
        }

        /// <summary>
        /// 그룹의 모든 isClearTrigger 목표가 완료되었는지 확인한다.
        /// </summary>
        private bool IsGroupCleared(string groupId)
        {
            List<CampaignGoalGroupData> goals = GetGoalsByGroup(groupId);

            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].isClearTrigger && !IsGoalCompleted(groupId, goals[i].sequence))
                {
                    return false;
                }
            }

            return goals.Count > 0;
        }

        /// <summary>
        /// 목표 완료 여부를 확인한다.
        /// </summary>
        private bool IsGoalCompleted(string groupId, int sequence)
        {
            string key = MakeGoalKey(groupId, sequence);
            return _goalCompletionState.TryGetValue(key, out bool completed) && completed;
        }

        /// <summary>
        /// 목표의 고유 키를 생성한다 (CampaignGoalGroupTableSO와 동일한 포맷).
        /// </summary>
        private string MakeGoalKey(string groupId, int sequence)
        {
            return groupId + "_" + sequence;
        }

        #endregion

        #region Private Methods — 유틸

        /// <summary>
        /// 세미콜론 구분 문자열의 ID를 목록에 추가한다.
        /// </summary>
        private void AddSemicolonIds(List<string> target, string semicolonDelimited)
        {
            string[] parts = semicolonDelimited.Split(';');

            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();

                if (!string.IsNullOrEmpty(trimmed))
                {
                    target.Add(trimmed);
                }
            }
        }

        #endregion
    }
}
