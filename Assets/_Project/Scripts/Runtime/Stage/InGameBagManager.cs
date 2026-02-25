using System;
using System.Collections.Generic;
using ProjectStS.Data;

namespace ProjectStS.Stage
{
    /// <summary>
    /// 탐험 중 획득한 아이템을 관리하는 인게임 가방 매니저.
    /// isNewForNow 플래그로 신규 획득 아이템을 구분하고, 스테이지 종료 시 고정 보상 선택 대상을 필터링한다.
    /// </summary>
    public class InGameBagManager
    {
        #region Constants

        private const int INITIAL_BAG_CAPACITY = 32;

        #endregion

        #region Private Fields

        private readonly List<InGameBagItemData> _bagItems;

        #endregion

        #region Events

        /// <summary>
        /// 아이템이 가방에 추가되었을 때 발행.
        /// </summary>
        public event Action<InGameBagItemData> OnItemAdded;

        #endregion

        #region Public Properties

        /// <summary>
        /// 가방 내 전체 아이템 목록.
        /// </summary>
        public List<InGameBagItemData> BagItems => _bagItems;

        /// <summary>
        /// 가방 내 아이템 수.
        /// </summary>
        public int Count => _bagItems.Count;

        #endregion

        #region Constructor

        /// <summary>
        /// InGameBagManager를 생성한다.
        /// </summary>
        public InGameBagManager()
        {
            _bagItems = new List<InGameBagItemData>(INITIAL_BAG_CAPACITY);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 아이템을 가방에 추가한다.
        /// </summary>
        /// <param name="item">추가할 아이템 데이터</param>
        public void AddItem(InGameBagItemData item)
        {
            if (item == null)
            {
                return;
            }

            _bagItems.Add(item);
            OnItemAdded?.Invoke(item);
        }

        /// <summary>
        /// 복수 아이템을 가방에 추가한다.
        /// </summary>
        /// <param name="items">추가할 아이템 목록</param>
        public void AddItems(List<InGameBagItemData> items)
        {
            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                AddItem(items[i]);
            }
        }

        /// <summary>
        /// 이번 탐험에서 새로 획득한 아이템 목록을 반환한다.
        /// </summary>
        public List<InGameBagItemData> GetNewItems()
        {
            var result = new List<InGameBagItemData>(_bagItems.Count);

            for (int i = 0; i < _bagItems.Count; i++)
            {
                if (_bagItems[i].isNewForNow)
                {
                    result.Add(_bagItems[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 특정 레어도의 아이템 목록을 반환한다.
        /// </summary>
        /// <param name="rarity">대상 레어도</param>
        public List<InGameBagItemData> GetItemsByRarity(Rarity rarity)
        {
            var result = new List<InGameBagItemData>(_bagItems.Count);

            for (int i = 0; i < _bagItems.Count; i++)
            {
                if (_bagItems[i].rarity == rarity)
                {
                    result.Add(_bagItems[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 신규 아이템 중 Epic을 제외한 목록을 반환한다 (실패 시 고정 보상 대상).
        /// </summary>
        public List<InGameBagItemData> GetNewItemsExcludeEpic()
        {
            var result = new List<InGameBagItemData>(_bagItems.Count);

            for (int i = 0; i < _bagItems.Count; i++)
            {
                if (_bagItems[i].isNewForNow && _bagItems[i].rarity != Rarity.Epic)
                {
                    result.Add(_bagItems[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// 가방을 초기화한다.
        /// </summary>
        public void Clear()
        {
            _bagItems.Clear();
        }

        #endregion
    }
}
