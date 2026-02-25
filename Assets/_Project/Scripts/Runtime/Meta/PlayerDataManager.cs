using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Meta
{
    /// <summary>
    /// 플레이어의 영속 데이터(보유 유닛, 인벤토리, 탐험 기록)를 통합 관리하는 중앙 매니저.
    /// DontDestroyOnLoad 오브젝트에 배치하고, Awake에서 ServiceLocator에 자체 등록한다.
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        #region Private Fields

        private List<OwnedUnitData> _ownedUnits = new List<OwnedUnitData>(10);
        private List<InventoryItemData> _inventory = new List<InventoryItemData>(50);
        private ExplorationRecordData _explorationRecord = new ExplorationRecordData();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<PlayerDataManager>();
        }

        #endregion

        #region Events

        /// <summary>
        /// 새 유닛 획득 시 발행.
        /// </summary>
        public event Action<OwnedUnitData> OnUnitAdded;

        /// <summary>
        /// 인벤토리 변경 시 발행.
        /// </summary>
        public event Action<InventoryItemData> OnInventoryChanged;

        /// <summary>
        /// 파티 구성 변경 시 발행.
        /// </summary>
        public event Action OnPartyChanged;

        #endregion

        #region Public Methods — 유닛 조회

        /// <summary>
        /// 전체 보유 유닛 목록을 반환한다.
        /// </summary>
        public List<OwnedUnitData> GetOwnedUnits()
        {
            return _ownedUnits;
        }

        /// <summary>
        /// ID로 보유 유닛을 조회한다.
        /// </summary>
        public OwnedUnitData GetOwnedUnit(string unitId)
        {
            for (int i = 0; i < _ownedUnits.Count; i++)
            {
                if (_ownedUnits[i].unitId == unitId)
                {
                    return _ownedUnits[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 현재 파티에 편성된 유닛 목록을 partyPosition 오름차순으로 반환한다.
        /// </summary>
        public List<OwnedUnitData> GetPartyMembers()
        {
            var party = new List<OwnedUnitData>(3);

            for (int i = 0; i < _ownedUnits.Count; i++)
            {
                if (_ownedUnits[i].partyPosition > 0)
                {
                    party.Add(_ownedUnits[i]);
                }
            }

            party.Sort((a, b) => a.partyPosition.CompareTo(b.partyPosition));
            return party;
        }

        /// <summary>
        /// 해당 unitId의 유닛을 보유하고 있는지 확인한다.
        /// </summary>
        public bool HasUnit(string unitId)
        {
            for (int i = 0; i < _ownedUnits.Count; i++)
            {
                if (_ownedUnits[i].unitId == unitId)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Public Methods — 인벤토리 조회

        /// <summary>
        /// 전체 인벤토리 목록을 반환한다.
        /// </summary>
        public List<InventoryItemData> GetInventory()
        {
            return _inventory;
        }

        /// <summary>
        /// 카테고리별 인벤토리 목록을 반환한다.
        /// </summary>
        public List<InventoryItemData> GetInventoryByCategory(InventoryCategory category)
        {
            var result = new List<InventoryItemData>(32);

            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].category == category)
                {
                    result.Add(_inventory[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// productId로 인벤토리 아이템을 조회한다.
        /// </summary>
        public InventoryItemData GetInventoryItem(string productId)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].productId == productId)
                {
                    return _inventory[i];
                }
            }

            return null;
        }

        /// <summary>
        /// 해당 productId의 아이템을 보유하고 있는지 확인한다.
        /// </summary>
        public bool HasItem(string productId)
        {
            InventoryItemData item = GetInventoryItem(productId);
            return item != null && (item.ownStack + item.useStack) > 0;
        }

        /// <summary>
        /// 해당 productId의 카드를 보유하고 있는지 확인한다.
        /// </summary>
        public bool HasCard(string productId)
        {
            InventoryItemData item = GetInventoryItem(productId);
            return item != null && item.category == InventoryCategory.Card && (item.ownStack + item.useStack) > 0;
        }

        #endregion

        #region Public Methods — 탐험 기록

        /// <summary>
        /// 탐험 기록을 반환한다.
        /// </summary>
        public ExplorationRecordData GetExplorationRecord()
        {
            return _explorationRecord;
        }

        /// <summary>
        /// 탐험 기록을 갱신한다.
        /// </summary>
        public void UpdateExplorationRecord(ExplorationRecordData record)
        {
            _explorationRecord = record;
        }

        /// <summary>
        /// 이번 탐험 카운터(countBattleNow, countVisualNovelNow, countEncountNow)를 초기화한다.
        /// </summary>
        public void ResetCurrentExplorationCounters()
        {
            _explorationRecord.countBattleNow = 0;
            _explorationRecord.countVisualNovelNow = 0;
            _explorationRecord.countEncountNow = 0;
        }

        #endregion

        #region Public Methods — 유닛 변경

        /// <summary>
        /// 새 유닛을 보유 목록에 추가한다.
        /// </summary>
        public void AddUnit(OwnedUnitData unit)
        {
            if (unit == null)
            {
                Debug.LogWarning("[PlayerDataManager] null 유닛을 추가할 수 없습니다.");
                return;
            }

            if (HasUnit(unit.unitId))
            {
                Debug.LogWarning($"[PlayerDataManager] 이미 보유 중인 유닛입니다: {unit.unitId}");
                return;
            }

            _ownedUnits.Add(unit);
            OnUnitAdded?.Invoke(unit);
        }

        /// <summary>
        /// 보유 유닛 데이터를 갱신한다. 동일 unitId를 가진 기존 항목을 교체한다.
        /// </summary>
        public void UpdateOwnedUnit(OwnedUnitData unit)
        {
            for (int i = 0; i < _ownedUnits.Count; i++)
            {
                if (_ownedUnits[i].unitId == unit.unitId)
                {
                    _ownedUnits[i] = unit;
                    return;
                }
            }

            Debug.LogWarning($"[PlayerDataManager] 갱신 대상 유닛을 찾을 수 없습니다: {unit.unitId}");
        }

        #endregion

        #region Public Methods — 인벤토리 변경

        /// <summary>
        /// 인벤토리에 아이템을 추가한다. 동일 productId가 있으면 ownStack을 가산한다.
        /// </summary>
        public void AddInventoryItem(InventoryItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning("[PlayerDataManager] null 아이템을 추가할 수 없습니다.");
                return;
            }

            InventoryItemData existing = GetInventoryItem(item.productId);

            if (existing != null)
            {
                existing.ownStack += item.ownStack;
            }
            else
            {
                _inventory.Add(item);
            }

            OnInventoryChanged?.Invoke(item);
        }

        /// <summary>
        /// 인벤토리 아이템의 ownStack을 감소시킨다. 0 이하가 되면 목록에서 제거한다.
        /// </summary>
        public void RemoveInventoryItem(string productId, int count)
        {
            InventoryItemData item = GetInventoryItem(productId);

            if (item == null)
            {
                Debug.LogWarning($"[PlayerDataManager] 인벤토리에서 찾을 수 없습니다: {productId}");
                return;
            }

            item.ownStack -= count;

            if (item.ownStack <= 0 && item.useStack <= 0)
            {
                _inventory.Remove(item);
            }

            OnInventoryChanged?.Invoke(item);
        }

        #endregion

        #region Public Methods — 파티 변경 알림

        /// <summary>
        /// 파티 변경 이벤트를 발행한다. PartyEditManager에서 호출한다.
        /// </summary>
        public void NotifyPartyChanged()
        {
            OnPartyChanged?.Invoke();
        }

        #endregion
    }
}
