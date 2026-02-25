using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 스테이지 완료 시 보상을 정산하는 계산기.
    /// 고정 보상(신규 아이템 선택), 완료 보상(드랍 테이블), 추가 보상(엘리트/사건/VN)을 산출한다.
    /// </summary>
    public class StageResultCalculator
    {
        #region Constants

        /// <summary>보스전/캠페인 목표 달성 시 완료 보상 수</summary>
        private const int COMPLETION_REWARD_HIGH = 3;

        /// <summary>행동력 소진/이벤트 탈출 시 완료 보상 수</summary>
        private const int COMPLETION_REWARD_LOW = 1;

        #endregion

        #region Private Fields

        private readonly DataManager _dataManager;
        private readonly EventRewardProcessor _rewardProcessor;

        #endregion

        #region Constructor

        /// <summary>
        /// StageResultCalculator를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        /// <param name="rewardProcessor">이벤트 보상 프로세서</param>
        public StageResultCalculator(DataManager dataManager, EventRewardProcessor rewardProcessor)
        {
            _dataManager = dataManager;
            _rewardProcessor = rewardProcessor;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스테이지 완료 보상을 계산한다.
        /// </summary>
        /// <param name="state">스테이지 상태</param>
        /// <param name="bag">인게임 가방 매니저</param>
        /// <returns>정산 데이터</returns>
        public StageSettlementData CalculateResult(StageState state, InGameBagManager bag)
        {
            var settlement = new StageSettlementData();
            settlement.Result = state.Result;
            settlement.EndReason = state.EndReason;

            bool isFailed = state.Result == StageResult.Failure;

            CalculateFixedRewards(state, bag, settlement, isFailed);
            CalculateCompletionRewards(state, settlement, isFailed);
            CalculateBonusRewards(state, settlement, isFailed);

            Debug.Log($"[StageResultCalculator] 보상 정산 완료. " +
                      $"고정 보상 선택 대상: {settlement.FixedRewardCandidates.Count}개, " +
                      $"선택 가능 수: {settlement.FixedRewardSelectCount}개, " +
                      $"완료 보상: {settlement.CompletionRewards.Count}개, " +
                      $"추가 보상: {settlement.BonusRewards.Count}개.");

            return settlement;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 고정 보상을 계산한다.
        /// 신규 아이템 중 (완료 이벤트 수 / 3, 올림)개를 선택 가능.
        /// 실패 시 Epic 아이템 제외.
        /// </summary>
        private void CalculateFixedRewards(
            StageState state, InGameBagManager bag,
            StageSettlementData settlement, bool isFailed)
        {
            List<InGameBagItemData> candidates;

            if (isFailed)
            {
                candidates = bag.GetNewItemsExcludeEpic();
            }
            else
            {
                candidates = bag.GetNewItems();
            }

            int selectCount = Mathf.CeilToInt(state.CompletedEventCount / 3f);
            selectCount = Mathf.Max(1, selectCount);
            selectCount = Mathf.Min(selectCount, 5);
            selectCount = Mathf.Min(selectCount, candidates.Count);

            settlement.FixedRewardCandidates = candidates;
            settlement.FixedRewardSelectCount = selectCount;
        }

        /// <summary>
        /// 완료 보상을 계산한다.
        /// 보스전 완료/캠페인 목표 달성 = 3개, 그 외 = 1개.
        /// 드랍 테이블에서 무작위 롤링.
        /// </summary>
        private void CalculateCompletionRewards(
            StageState state, StageSettlementData settlement, bool isFailed)
        {
            if (isFailed)
            {
                settlement.CompletionRewards = new List<InGameBagItemData>(0);
                return;
            }

            int rewardCount;

            switch (state.EndReason)
            {
                case StageEndReason.BossCleared:
                case StageEndReason.CampaignGoal:
                    rewardCount = COMPLETION_REWARD_HIGH;
                    break;

                case StageEndReason.APDepleted:
                case StageEndReason.EventEscape:
                default:
                    rewardCount = COMPLETION_REWARD_LOW;
                    break;
            }

            string rewardTableId = FindCompletionRewardTableId(state);

            if (string.IsNullOrEmpty(rewardTableId))
            {
                settlement.CompletionRewards = new List<InGameBagItemData>(0);
                return;
            }

            settlement.CompletionRewards = _rewardProcessor.RollCompletionRewards(
                rewardTableId, rewardCount, false);
        }

        /// <summary>
        /// 추가 보상을 계산한다.
        /// 엘리트/사건/VN 완료 횟수만큼 드랍 테이블에서 롤링.
        /// 실패 시에도 수령 가능.
        /// </summary>
        private void CalculateBonusRewards(
            StageState state, StageSettlementData settlement, bool isFailed)
        {
            int bonusCount = state.BonusRewardCount;

            if (bonusCount <= 0)
            {
                settlement.BonusRewards = new List<InGameBagItemData>(0);
                return;
            }

            string rewardTableId = FindCompletionRewardTableId(state);

            if (string.IsNullOrEmpty(rewardTableId))
            {
                settlement.BonusRewards = new List<InGameBagItemData>(0);
                return;
            }

            settlement.BonusRewards = _rewardProcessor.RollCompletionRewards(
                rewardTableId, bonusCount, isFailed);
        }

        /// <summary>
        /// 현재 구역의 보상 테이블 ID를 조회한다.
        /// 마지막 방문 구역의 이벤트에서 rewardId를 참조한다.
        /// </summary>
        private string FindCompletionRewardTableId(StageState state)
        {
            if (state.CurrentNode != null && state.CurrentNode.AssignedEvent != null)
            {
                string rewardId = state.CurrentNode.AssignedEvent.rewardId;

                if (!string.IsNullOrEmpty(rewardId))
                {
                    return rewardId;
                }
            }

            if (_dataManager.Rewards != null && _dataManager.Rewards.Count > 0)
            {
                return _dataManager.Rewards.Entries[0].id;
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// 스테이지 완료 보상 정산 결과 데이터.
    /// </summary>
    public class StageSettlementData
    {
        /// <summary>스테이지 결과</summary>
        public StageResult Result { get; set; }

        /// <summary>스테이지 종료 사유</summary>
        public StageEndReason EndReason { get; set; }

        /// <summary>고정 보상 선택 대상 목록 (신규 아이템)</summary>
        public List<InGameBagItemData> FixedRewardCandidates { get; set; }

        /// <summary>고정 보상에서 선택 가능한 수</summary>
        public int FixedRewardSelectCount { get; set; }

        /// <summary>완료 보상 목록 (드랍 테이블 롤링)</summary>
        public List<InGameBagItemData> CompletionRewards { get; set; }

        /// <summary>추가 보상 목록 (엘리트/사건/VN 완료 횟수)</summary>
        public List<InGameBagItemData> BonusRewards { get; set; }

        /// <summary>
        /// StageSettlementData를 생성한다.
        /// </summary>
        public StageSettlementData()
        {
            FixedRewardCandidates = new List<InGameBagItemData>(0);
            CompletionRewards = new List<InGameBagItemData>(0);
            BonusRewards = new List<InGameBagItemData>(0);
        }
    }
}
