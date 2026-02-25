using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 이벤트 완료 시 보상을 롤링하는 프로세서.
    /// RewardTableSO와 DropRateTableSO를 기반으로 가중 랜덤 아이템을 선택한다.
    /// </summary>
    public class EventRewardProcessor
    {
        #region Private Fields

        private readonly DataManager _dataManager;
        private Dictionary<Rarity, float> _eventRewardDropRates;

        #endregion

        #region Constructor

        /// <summary>
        /// EventRewardProcessor를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public EventRewardProcessor(DataManager dataManager)
        {
            _dataManager = dataManager;
            BuildRewardDropRates();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 이벤트 보상을 처리하여 InGameBagItemData 목록을 반환한다.
        /// </summary>
        /// <param name="eventData">완료된 이벤트 데이터</param>
        /// <returns>획득한 아이템 목록</returns>
        public List<InGameBagItemData> ProcessEventReward(EventData eventData)
        {
            var rewards = new List<InGameBagItemData>(4);

            if (string.IsNullOrEmpty(eventData.rewardId))
            {
                return rewards;
            }

            int rewardCount = Random.Range(eventData.rewardMinCount, eventData.rewardMaxCount + 1);

            if (rewardCount <= 0)
            {
                return rewards;
            }

            List<InGameBagItemData> rolledItems = RollFromRewardTable(eventData.rewardId, rewardCount);
            rewards.AddRange(rolledItems);

            return rewards;
        }

        /// <summary>
        /// 보상 테이블에서 가중 랜덤으로 아이템을 롤링한다.
        /// 완료 보상/추가 보상 등 범용 롤링에 사용.
        /// </summary>
        /// <param name="rewardId">보상 테이블 ID (세미콜론 구분 시 복수 테이블)</param>
        /// <param name="count">롤링 횟수</param>
        /// <returns>롤링된 아이템 목록</returns>
        public List<InGameBagItemData> RollFromRewardTable(string rewardId, int count)
        {
            var results = new List<InGameBagItemData>(count);

            List<RewardTableData> rewardEntries = GetRewardEntries(rewardId);

            if (rewardEntries.Count == 0)
            {
                Debug.LogWarning($"[EventRewardProcessor] 보상 테이블 '{rewardId}'에 항목이 없습니다.");
                return results;
            }

            var weights = new List<float>(rewardEntries.Count);

            for (int i = 0; i < rewardEntries.Count; i++)
            {
                float weight = rewardEntries[i].dropRate;

                if (_eventRewardDropRates.TryGetValue(rewardEntries[i].rarity, out float rarityWeight))
                {
                    weight *= rarityWeight / 100f;
                }

                weights.Add(Mathf.Max(weight, 0.01f));
            }

            for (int roll = 0; roll < count; roll++)
            {
                int selectedIndex = WeightedRandomIndex(weights);

                if (selectedIndex >= 0 && selectedIndex < rewardEntries.Count)
                {
                    RewardTableData entry = rewardEntries[selectedIndex];
                    InGameBagItemData bagItem = CreateBagItem(entry);

                    if (bagItem != null)
                    {
                        results.Add(bagItem);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 완료/추가 보상용 범용 롤링. 구역의 드랍 테이블 ID를 사용.
        /// </summary>
        /// <param name="rewardId">보상 테이블 ID</param>
        /// <param name="count">롤링 횟수</param>
        /// <param name="excludeEpic">Epic 레어도 제외 여부 (실패 시)</param>
        /// <returns>롤링된 아이템 목록</returns>
        public List<InGameBagItemData> RollCompletionRewards(string rewardId, int count, bool excludeEpic)
        {
            var results = new List<InGameBagItemData>(count);

            List<RewardTableData> rewardEntries = GetRewardEntries(rewardId);

            if (rewardEntries.Count == 0)
            {
                return results;
            }

            if (excludeEpic)
            {
                rewardEntries = FilterExcludeEpic(rewardEntries);
            }

            var weights = new List<float>(rewardEntries.Count);

            for (int i = 0; i < rewardEntries.Count; i++)
            {
                float weight = rewardEntries[i].dropRate;

                if (_eventRewardDropRates.TryGetValue(rewardEntries[i].rarity, out float rarityWeight))
                {
                    weight *= rarityWeight / 100f;
                }

                weights.Add(Mathf.Max(weight, 0.01f));
            }

            for (int roll = 0; roll < count; roll++)
            {
                int selectedIndex = WeightedRandomIndex(weights);

                if (selectedIndex >= 0 && selectedIndex < rewardEntries.Count)
                {
                    RewardTableData entry = rewardEntries[selectedIndex];
                    InGameBagItemData bagItem = CreateBagItem(entry);

                    if (bagItem != null)
                    {
                        results.Add(bagItem);
                    }
                }
            }

            return results;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// DropRateTableSO에서 EventReward 카테고리의 레어도별 드랍율을 구축한다.
        /// </summary>
        private void BuildRewardDropRates()
        {
            _eventRewardDropRates = new Dictionary<Rarity, float>(5);

            if (_dataManager.DropRates == null)
            {
                SetDefaultDropRates();
                return;
            }

            List<DropRateData> entries = _dataManager.DropRates.Entries;

            for (int i = 0; i < entries.Count; i++)
            {
                DropRateData rate = entries[i];

                if (rate.category == DropRateCategory.EventReward)
                {
                    _eventRewardDropRates[rate.rarity] = rate.dropValue;
                }
            }

            if (_eventRewardDropRates.Count == 0)
            {
                SetDefaultDropRates();
            }
        }

        /// <summary>
        /// 기본 드랍율 설정.
        /// </summary>
        private void SetDefaultDropRates()
        {
            _eventRewardDropRates[Rarity.Common] = 50f;
            _eventRewardDropRates[Rarity.Uncommon] = 25f;
            _eventRewardDropRates[Rarity.Rare] = 15f;
            _eventRewardDropRates[Rarity.Unique] = 7.5f;
            _eventRewardDropRates[Rarity.Epic] = 2.5f;
        }

        /// <summary>
        /// 보상 테이블 ID로 항목을 조회한다. 세미콜론 구분 시 복수 테이블 합산.
        /// </summary>
        private List<RewardTableData> GetRewardEntries(string rewardId)
        {
            var entries = new List<RewardTableData>(16);

            if (_dataManager.Rewards == null)
            {
                return entries;
            }

            string[] ids = rewardId.Split(';');

            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i].Trim();

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                List<RewardTableData> allEntries = _dataManager.Rewards.Entries;

                for (int j = 0; j < allEntries.Count; j++)
                {
                    if (allEntries[j].id == id)
                    {
                        entries.Add(allEntries[j]);
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Epic 레어도를 제외한 목록을 반환한다.
        /// </summary>
        private List<RewardTableData> FilterExcludeEpic(List<RewardTableData> entries)
        {
            var filtered = new List<RewardTableData>(entries.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].rarity != Rarity.Epic)
                {
                    filtered.Add(entries[i]);
                }
            }

            return filtered;
        }

        /// <summary>
        /// RewardTableData에서 InGameBagItemData를 생성한다.
        /// </summary>
        private InGameBagItemData CreateBagItem(RewardTableData rewardEntry)
        {
            string itemId = rewardEntry.itemId;

            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            CardData card = _dataManager.GetCard(itemId);

            if (card != null)
            {
                return new InGameBagItemData
                {
                    category = InventoryCategory.Card,
                    productId = card.id,
                    productName = card.cardName,
                    description = card.description,
                    rarity = card.rarity,
                    isNewForNow = true
                };
            }

            ItemData item = _dataManager.GetItem(itemId);

            if (item != null)
            {
                return new InGameBagItemData
                {
                    category = InventoryCategory.Item,
                    productId = item.id,
                    productName = item.itemName,
                    description = item.description,
                    rarity = item.rarity,
                    isNewForNow = true
                };
            }

            Debug.LogWarning($"[EventRewardProcessor] 아이템 ID '{itemId}'을(를) 카드/아이템 테이블에서 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 가중치 기반 랜덤 인덱스 선택.
        /// </summary>
        private int WeightedRandomIndex(List<float> weights)
        {
            float totalWeight = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f)
            {
                return Random.Range(0, weights.Count);
            }

            float roll = Random.Range(0f, totalWeight);
            float accumulated = 0f;

            for (int i = 0; i < weights.Count; i++)
            {
                accumulated += weights[i];

                if (roll <= accumulated)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }

        #endregion
    }
}
