using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Meta;
using ProjectStS.Stage;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 보상 정산 로직을 처리한다.
    /// 스테이지 완료 시 정산 데이터를 수신하고,
    /// 플레이어의 보상 선택을 반영하여 인벤토리에 추가한다.
    /// </summary>
    public class RewardSettlementProcessor
    {
        #region Private Fields

        private StageSettlementData _settlement;
        private List<int> _selectedFixedRewardIndices;

        #endregion

        #region Events

        /// <summary>
        /// 보상 적용 완료 시 발행.
        /// </summary>
        public event Action OnRewardsApplied;

        #endregion

        #region Public Methods — 초기화

        /// <summary>
        /// 정산 데이터를 수신하고 초기화한다.
        /// </summary>
        /// <param name="settlement">스테이지 정산 데이터</param>
        public void Initialize(StageSettlementData settlement)
        {
            _settlement = settlement;
            _selectedFixedRewardIndices = new List<int>(4);
        }

        #endregion

        #region Public Methods — 조회

        /// <summary>
        /// 고정 보상 선택 대상 목록을 반환한다.
        /// </summary>
        public List<InGameBagItemData> GetFixedRewardCandidates()
        {
            if (_settlement == null)
            {
                return new List<InGameBagItemData>();
            }

            return _settlement.FixedRewardCandidates ?? new List<InGameBagItemData>();
        }

        /// <summary>
        /// 고정 보상에서 선택 가능한 수를 반환한다.
        /// </summary>
        public int GetFixedRewardSelectCount()
        {
            if (_settlement == null)
            {
                return 0;
            }

            return _settlement.FixedRewardSelectCount;
        }

        /// <summary>
        /// 완료 보상 목록을 반환한다.
        /// </summary>
        public List<InGameBagItemData> GetCompletionRewards()
        {
            if (_settlement == null)
            {
                return new List<InGameBagItemData>();
            }

            return _settlement.CompletionRewards ?? new List<InGameBagItemData>();
        }

        /// <summary>
        /// 추가 보상 목록을 반환한다.
        /// </summary>
        public List<InGameBagItemData> GetBonusRewards()
        {
            if (_settlement == null)
            {
                return new List<InGameBagItemData>();
            }

            return _settlement.BonusRewards ?? new List<InGameBagItemData>();
        }

        #endregion

        #region Public Methods — 선택

        /// <summary>
        /// 플레이어가 고정 보상에서 선택한 인덱스를 기록한다.
        /// </summary>
        /// <param name="selectedIndices">선택된 인덱스 목록</param>
        public void SelectFixedRewards(List<int> selectedIndices)
        {
            _selectedFixedRewardIndices = selectedIndices ?? new List<int>();
        }

        #endregion

        #region Public Methods — 적용

        /// <summary>
        /// 선택된 고정 보상 + 완료 보상 + 추가 보상을 인벤토리에 적용한다.
        /// InGameBagItemData를 InventoryItemData로 변환하여 AddInventoryItem을 호출한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <returns>적용된 보상의 InventoryItemData 목록</returns>
        public List<InventoryItemData> ApplyRewards(PlayerDataManager playerData)
        {
            var appliedRewards = new List<InventoryItemData>(16);

            List<InGameBagItemData> fixedCandidates = GetFixedRewardCandidates();

            for (int i = 0; i < _selectedFixedRewardIndices.Count; i++)
            {
                int index = _selectedFixedRewardIndices[i];

                if (index >= 0 && index < fixedCandidates.Count)
                {
                    InventoryItemData item = ConvertToInventoryItem(fixedCandidates[index]);
                    playerData.AddInventoryItem(item);
                    appliedRewards.Add(item);
                }
            }

            List<InGameBagItemData> completionRewards = GetCompletionRewards();

            for (int i = 0; i < completionRewards.Count; i++)
            {
                InventoryItemData item = ConvertToInventoryItem(completionRewards[i]);
                playerData.AddInventoryItem(item);
                appliedRewards.Add(item);
            }

            List<InGameBagItemData> bonusRewards = GetBonusRewards();

            for (int i = 0; i < bonusRewards.Count; i++)
            {
                InventoryItemData item = ConvertToInventoryItem(bonusRewards[i]);
                playerData.AddInventoryItem(item);
                appliedRewards.Add(item);
            }

            OnRewardsApplied?.Invoke();

            Debug.Log($"[RewardSettlementProcessor] 보상 적용 완료. 총 {appliedRewards.Count}개 아이템.");

            return appliedRewards;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// InGameBagItemData를 InventoryItemData로 변환한다.
        /// </summary>
        private InventoryItemData ConvertToInventoryItem(InGameBagItemData bagItem)
        {
            return new InventoryItemData
            {
                category = bagItem.category,
                productId = bagItem.productId,
                productName = bagItem.productName,
                description = bagItem.description,
                rarity = bagItem.rarity,
                ownStack = 1
            };
        }

        #endregion
    }
}
