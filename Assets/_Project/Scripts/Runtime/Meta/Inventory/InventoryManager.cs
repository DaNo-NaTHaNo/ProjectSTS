using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Meta
{
    /// <summary>
    /// 인벤토리 필터링 및 정렬 기능을 제공한다.
    /// PlayerDataManager의 인벤토리 데이터에 대한 뷰 레이어 역할을 한다.
    /// </summary>
    public class InventoryManager
    {
        #region Private Fields

        private readonly PlayerDataManager _playerData;

        #endregion

        #region Constructor

        /// <summary>
        /// InventoryManager를 생성한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        public InventoryManager(PlayerDataManager playerData)
        {
            _playerData = playerData;
        }

        #endregion

        #region Public Methods — 카드 필터링

        /// <summary>
        /// 카드를 조건에 따라 필터링한다.
        /// null 파라미터는 해당 조건을 적용하지 않음을 의미한다.
        /// </summary>
        /// <param name="element">속성 필터 (null이면 무시)</param>
        /// <param name="cost">코스트 필터 (null이면 무시)</param>
        /// <param name="cardType">카드 타입 필터 (null이면 무시)</param>
        /// <returns>필터링된 카드 목록</returns>
        public List<InventoryItemData> GetFilteredCards(
            ElementType? element = null,
            int? cost = null,
            CardType? cardType = null)
        {
            List<InventoryItemData> cards = _playerData.GetInventoryByCategory(InventoryCategory.Card);
            var result = new List<InventoryItemData>(cards.Count);

            for (int i = 0; i < cards.Count; i++)
            {
                InventoryItemData card = cards[i];

                if (element.HasValue && card.cardElement != element.Value)
                {
                    continue;
                }

                if (cost.HasValue && card.cardCost != cost.Value)
                {
                    continue;
                }

                if (cardType.HasValue && card.cardType != cardType.Value)
                {
                    continue;
                }

                result.Add(card);
            }

            return result;
        }

        #endregion

        #region Public Methods — 아이템 필터링

        /// <summary>
        /// 아이템을 조건에 따라 필터링한다.
        /// null 파라미터는 해당 조건을 적용하지 않음을 의미한다.
        /// </summary>
        /// <param name="itemType">아이템 타입 필터 (null이면 무시)</param>
        /// <param name="targetStatus">효과 적용 스탯 필터 (null이면 무시)</param>
        /// <param name="isDisposable">소모 여부 필터 (null이면 무시)</param>
        /// <returns>필터링된 아이템 목록</returns>
        public List<InventoryItemData> GetFilteredItems(
            ItemType? itemType = null,
            ItemTargetStatus? targetStatus = null,
            bool? isDisposable = null)
        {
            List<InventoryItemData> items = _playerData.GetInventoryByCategory(InventoryCategory.Item);
            var result = new List<InventoryItemData>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                InventoryItemData item = items[i];

                if (itemType.HasValue && item.itemType != itemType.Value)
                {
                    continue;
                }

                if (targetStatus.HasValue && item.targetStatus != targetStatus.Value)
                {
                    continue;
                }

                if (isDisposable.HasValue && item.isDisposable != isDisposable.Value)
                {
                    continue;
                }

                result.Add(item);
            }

            return result;
        }

        #endregion

        #region Public Methods — 정렬

        /// <summary>
        /// 카테고리별 인벤토리를 정렬 기준에 따라 정렬하여 반환한다.
        /// </summary>
        /// <param name="category">소지품 카테고리</param>
        /// <param name="sortType">정렬 기준</param>
        /// <param name="ascending">오름차순 여부</param>
        /// <returns>정렬된 인벤토리 목록</returns>
        public List<InventoryItemData> GetSortedInventory(
            InventoryCategory category,
            InventorySortType sortType,
            bool ascending = true)
        {
            List<InventoryItemData> items = _playerData.GetInventoryByCategory(category);
            SortItems(items, sortType, ascending);
            return items;
        }

        #endregion

        #region Public Methods — 필터 + 정렬 복합

        /// <summary>
        /// 카드를 필터링하고 정렬하여 반환한다.
        /// </summary>
        public List<InventoryItemData> GetFilteredAndSortedCards(
            ElementType? element = null,
            int? cost = null,
            CardType? cardType = null,
            InventorySortType sortType = InventorySortType.Name,
            bool ascending = true)
        {
            List<InventoryItemData> filtered = GetFilteredCards(element, cost, cardType);
            SortItems(filtered, sortType, ascending);
            return filtered;
        }

        /// <summary>
        /// 아이템을 필터링하고 정렬하여 반환한다.
        /// </summary>
        public List<InventoryItemData> GetFilteredAndSortedItems(
            ItemType? itemType = null,
            ItemTargetStatus? targetStatus = null,
            bool? isDisposable = null,
            InventorySortType sortType = InventorySortType.Name,
            bool ascending = true)
        {
            List<InventoryItemData> filtered = GetFilteredItems(itemType, targetStatus, isDisposable);
            SortItems(filtered, sortType, ascending);
            return filtered;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 인벤토리 목록을 정렬 기준에 따라 정렬한다.
        /// </summary>
        private void SortItems(List<InventoryItemData> items, InventorySortType sortType, bool ascending)
        {
            Comparison<InventoryItemData> comparison = GetComparison(sortType);

            items.Sort((a, b) =>
            {
                int result = comparison(a, b);
                return ascending ? result : -result;
            });
        }

        /// <summary>
        /// 정렬 기준에 따른 Comparison 델리게이트를 반환한다.
        /// </summary>
        private Comparison<InventoryItemData> GetComparison(InventorySortType sortType)
        {
            switch (sortType)
            {
                case InventorySortType.Name:
                    return (a, b) => string.Compare(a.productName, b.productName, StringComparison.Ordinal);

                case InventorySortType.Element:
                    return (a, b) => a.cardElement.CompareTo(b.cardElement);

                case InventorySortType.Rarity:
                    return (a, b) => a.rarity.CompareTo(b.rarity);

                case InventorySortType.Cost:
                    return (a, b) => a.cardCost.CompareTo(b.cardCost);

                case InventorySortType.Quantity:
                    return (a, b) => (a.ownStack + a.useStack).CompareTo(b.ownStack + b.useStack);

                default:
                    return (a, b) => string.Compare(a.productName, b.productName, StringComparison.Ordinal);
            }
        }

        #endregion
    }
}
