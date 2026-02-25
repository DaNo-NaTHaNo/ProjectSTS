using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Meta
{
    /// <summary>
    /// 파티 편성, 덱 편집, 스킬/아이템 장비 로직을 담당한다.
    /// PlayerDataManager 및 DataManager에 의존하며, ServiceLocator를 통해 접근한다.
    /// </summary>
    public class PartyEditManager
    {
        #region Private Fields

        private readonly PlayerDataManager _playerData;
        private readonly DataManager _dataManager;
        private readonly GameSettings _settings;

        #endregion

        #region Events

        /// <summary>
        /// 덱 변경 시 발행. unitId를 전달한다.
        /// </summary>
        public event Action<string> OnDeckChanged;

        /// <summary>
        /// 스킬 변경 시 발행. unitId를 전달한다.
        /// </summary>
        public event Action<string> OnSkillChanged;

        /// <summary>
        /// 아이템 변경 시 발행. unitId를 전달한다.
        /// </summary>
        public event Action<string> OnItemChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// PartyEditManager를 생성한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public PartyEditManager(PlayerDataManager playerData, DataManager dataManager)
        {
            _playerData = playerData;
            _dataManager = dataManager;
            _settings = dataManager.Settings;
        }

        #endregion

        #region Public Methods — 파티 편성

        /// <summary>
        /// 유닛을 파티 슬롯에 배치한다.
        /// </summary>
        /// <param name="unitId">배치할 유닛 ID</param>
        /// <param name="position">파티 슬롯 (1~MaxPartySize)</param>
        /// <returns>배치 성공 여부</returns>
        public bool TryAssignToParty(string unitId, int position)
        {
            if (position < 1 || position > _settings.MaxPartySize)
            {
                Debug.LogWarning($"[PartyEditManager] 유효하지 않은 파티 슬롯: {position}");
                return false;
            }

            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                Debug.LogWarning($"[PartyEditManager] 보유하지 않은 유닛: {unitId}");
                return false;
            }

            // 해당 슬롯에 이미 다른 유닛이 있으면 해제
            List<OwnedUnitData> allUnits = _playerData.GetOwnedUnits();

            for (int i = 0; i < allUnits.Count; i++)
            {
                if (allUnits[i].partyPosition == position && allUnits[i].unitId != unitId)
                {
                    allUnits[i].partyPosition = 0;
                }
            }

            // 이미 다른 슬롯에 있었으면 해제
            if (unit.partyPosition > 0 && unit.partyPosition != position)
            {
                unit.partyPosition = 0;
            }

            unit.partyPosition = position;
            _playerData.NotifyPartyChanged();
            return true;
        }

        /// <summary>
        /// 유닛을 파티에서 제거한다. 주인공이거나 최소 인원이면 거부한다.
        /// </summary>
        /// <param name="unitId">제거할 유닛 ID</param>
        /// <returns>제거 성공 여부</returns>
        public bool TryRemoveFromParty(string unitId)
        {
            if (IsProtagonist(unitId))
            {
                Debug.LogWarning("[PartyEditManager] 주인공은 파티에서 제거할 수 없습니다.");
                return false;
            }

            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null || unit.partyPosition <= 0)
            {
                return false;
            }

            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();

            if (partyMembers.Count <= _settings.MinPartySize)
            {
                Debug.LogWarning($"[PartyEditManager] 최소 파티 인원({_settings.MinPartySize}명)을 유지해야 합니다.");
                return false;
            }

            unit.partyPosition = 0;
            _playerData.NotifyPartyChanged();
            return true;
        }

        /// <summary>
        /// 현재 파티가 유효한지 검증한다.
        /// </summary>
        /// <returns>유효 여부</returns>
        public bool ValidateParty()
        {
            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();

            if (partyMembers.Count < _settings.MinPartySize || partyMembers.Count > _settings.MaxPartySize)
            {
                return false;
            }

            bool hasProtagonist = false;

            for (int i = 0; i < partyMembers.Count; i++)
            {
                if (IsProtagonist(partyMembers[i].unitId))
                {
                    hasProtagonist = true;
                    break;
                }
            }

            return hasProtagonist;
        }

        /// <summary>
        /// 해당 유닛이 주인공인지 확인한다.
        /// </summary>
        public bool IsProtagonist(string unitId)
        {
            if (_settings == null || string.IsNullOrEmpty(_settings.ProtagonistUnitId))
            {
                return false;
            }

            return unitId == _settings.ProtagonistUnitId;
        }

        #endregion

        #region Public Methods — 덱 편집

        /// <summary>
        /// 유닛의 덱을 설정한다. 카드 수와 속성을 검증한다.
        /// </summary>
        /// <param name="unitId">대상 유닛 ID</param>
        /// <param name="cardIds">설정할 카드 ID 목록</param>
        /// <returns>설정 성공 여부</returns>
        public bool TrySetDeck(string unitId, List<string> cardIds)
        {
            if (cardIds == null || cardIds.Count < _settings.MinDeckSize || cardIds.Count > _settings.MaxDeckSize)
            {
                Debug.LogWarning($"[PartyEditManager] 덱 크기가 유효하지 않습니다: {cardIds?.Count ?? 0} (필요: {_settings.MinDeckSize}~{_settings.MaxDeckSize})");
                return false;
            }

            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return false;
            }

            // 모든 카드 속성 검증
            for (int i = 0; i < cardIds.Count; i++)
            {
                if (!CanEquipCard(unitId, cardIds[i]))
                {
                    Debug.LogWarning($"[PartyEditManager] 유닛 {unitId}에 카드 {cardIds[i]}를 장비할 수 없습니다.");
                    return false;
                }
            }

            // 기존 덱 카드의 ownStack/useStack 복원
            RestoreDeckStacks(unit);

            // 새 덱 적용
            unit.editedDeck = string.Join(";", cardIds);

            // 새 덱 카드의 ownStack/useStack 갱신
            ApplyDeckStacks(cardIds);

            OnDeckChanged?.Invoke(unitId);
            return true;
        }

        /// <summary>
        /// 유닛이 해당 카드를 장비할 수 있는지 확인한다.
        /// Wild 속성 유닛은 모든 카드, 그 외는 동일 속성 + Wild 카드만 가능.
        /// </summary>
        public bool CanEquipCard(string unitId, string cardId)
        {
            OwnedUnitData ownedUnit = _playerData.GetOwnedUnit(unitId);

            if (ownedUnit == null)
            {
                return false;
            }

            CardData card = _dataManager.GetCard(cardId);

            if (card == null)
            {
                return false;
            }

            // 인벤토리 보유 확인
            InventoryItemData invItem = _playerData.GetInventoryItem(cardId);

            if (invItem == null || invItem.ownStack <= 0)
            {
                return false;
            }

            // 속성 검증
            ElementType unitElement = ownedUnit.cardElement;

            if (unitElement == ElementType.Wild)
            {
                return true;
            }

            return card.element == unitElement || card.element == ElementType.Wild;
        }

        /// <summary>
        /// 유닛의 현재 편집 덱을 반환한다. 편집 덱이 없으면 초기 덱을 반환한다.
        /// </summary>
        public List<string> GetEditedDeck(string unitId)
        {
            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return new List<string>(0);
            }

            if (!string.IsNullOrEmpty(unit.editedDeck))
            {
                return ParseSemicolonList(unit.editedDeck);
            }

            // 편집 덱이 없으면 마스터 데이터의 초기 덱 반환
            UnitData masterUnit = _dataManager.GetUnit(unitId);

            if (masterUnit != null && !string.IsNullOrEmpty(masterUnit.initialDeckIds))
            {
                return ParseSemicolonList(masterUnit.initialDeckIds);
            }

            return new List<string>(0);
        }

        /// <summary>
        /// 유닛이 장비 가능한 카드 목록을 반환한다 (속성 필터 적용, ownStack > 0).
        /// </summary>
        public List<InventoryItemData> GetAvailableCards(string unitId)
        {
            OwnedUnitData ownedUnit = _playerData.GetOwnedUnit(unitId);

            if (ownedUnit == null)
            {
                return new List<InventoryItemData>(0);
            }

            List<InventoryItemData> cards = _playerData.GetInventoryByCategory(InventoryCategory.Card);
            var result = new List<InventoryItemData>(cards.Count);
            ElementType unitElement = ownedUnit.cardElement;

            for (int i = 0; i < cards.Count; i++)
            {
                InventoryItemData card = cards[i];

                if (card.ownStack <= 0)
                {
                    continue;
                }

                if (unitElement == ElementType.Wild)
                {
                    result.Add(card);
                }
                else if (card.cardElement == unitElement || card.cardElement == ElementType.Wild)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        #endregion

        #region Public Methods — 스킬 편집

        /// <summary>
        /// 유닛에 스킬을 장비한다.
        /// </summary>
        /// <param name="unitId">대상 유닛 ID</param>
        /// <param name="skillId">장비할 스킬 ID (빈 문자열로 해제)</param>
        /// <returns>장비 성공 여부</returns>
        public bool TrySetSkill(string unitId, string skillId)
        {
            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(skillId))
            {
                unit.editedSkill = "";
                OnSkillChanged?.Invoke(unitId);
                return true;
            }

            if (!CanEquipSkill(unitId, skillId))
            {
                Debug.LogWarning($"[PartyEditManager] 유닛 {unitId}에 스킬 {skillId}을 장비할 수 없습니다.");
                return false;
            }

            unit.editedSkill = skillId;
            OnSkillChanged?.Invoke(unitId);
            return true;
        }

        /// <summary>
        /// 유닛이 해당 스킬을 장비할 수 있는지 확인한다.
        /// SkillData.unitId가 해당 유닛 ID와 일치해야 한다.
        /// </summary>
        public bool CanEquipSkill(string unitId, string skillId)
        {
            SkillData skill = _dataManager.GetSkill(skillId);

            if (skill == null)
            {
                return false;
            }

            return skill.unitId == unitId;
        }

        /// <summary>
        /// 유닛의 현재 장비 스킬 ID를 반환한다. 편집 스킬이 없으면 초기 스킬을 반환한다.
        /// </summary>
        public string GetEditedSkill(string unitId)
        {
            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return "";
            }

            if (!string.IsNullOrEmpty(unit.editedSkill))
            {
                return unit.editedSkill;
            }

            UnitData masterUnit = _dataManager.GetUnit(unitId);

            if (masterUnit != null)
            {
                return masterUnit.initialSkillId ?? "";
            }

            return "";
        }

        /// <summary>
        /// 유닛이 장비 가능한 스킬 목록을 반환한다.
        /// </summary>
        public List<SkillData> GetAvailableSkills(string unitId)
        {
            List<SkillData> allSkills = _dataManager.Skills.Entries;
            var result = new List<SkillData>(4);

            for (int i = 0; i < allSkills.Count; i++)
            {
                if (allSkills[i].unitId == unitId)
                {
                    result.Add(allSkills[i]);
                }
            }

            return result;
        }

        #endregion

        #region Public Methods — 아이템 편집

        /// <summary>
        /// 유닛의 아이템 슬롯에 아이템을 장비한다.
        /// </summary>
        /// <param name="unitId">대상 유닛 ID</param>
        /// <param name="slot">아이템 슬롯 (1 또는 2)</param>
        /// <param name="itemId">장비할 아이템 ID</param>
        /// <returns>장비 성공 여부</returns>
        public bool TrySetItem(string unitId, int slot, string itemId)
        {
            if (slot < 1 || slot > _settings.MaxItemSlots)
            {
                Debug.LogWarning($"[PartyEditManager] 유효하지 않은 아이템 슬롯: {slot}");
                return false;
            }

            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return false;
            }

            InventoryItemData invItem = _playerData.GetInventoryItem(itemId);

            if (invItem == null || invItem.category != InventoryCategory.Item || invItem.ownStack <= 0)
            {
                Debug.LogWarning($"[PartyEditManager] 장비 가능한 아이템이 없습니다: {itemId}");
                return false;
            }

            // 기존 아이템 해제
            TryRemoveItem(unitId, slot);

            // 새 아이템 장비
            if (slot == 1)
            {
                unit.equipItem1 = itemId;
            }
            else
            {
                unit.equipItem2 = itemId;
            }

            invItem.ownStack--;
            invItem.useStack++;

            OnItemChanged?.Invoke(unitId);
            return true;
        }

        /// <summary>
        /// 유닛의 아이템 슬롯에서 아이템을 해제한다.
        /// </summary>
        /// <param name="unitId">대상 유닛 ID</param>
        /// <param name="slot">아이템 슬롯 (1 또는 2)</param>
        /// <returns>해제 성공 여부</returns>
        public bool TryRemoveItem(string unitId, int slot)
        {
            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return false;
            }

            string currentItemId = (slot == 1) ? unit.equipItem1 : unit.equipItem2;

            if (string.IsNullOrEmpty(currentItemId))
            {
                return false;
            }

            InventoryItemData invItem = _playerData.GetInventoryItem(currentItemId);

            if (invItem != null)
            {
                invItem.ownStack++;
                invItem.useStack--;
            }

            if (slot == 1)
            {
                unit.equipItem1 = "";
            }
            else
            {
                unit.equipItem2 = "";
            }

            OnItemChanged?.Invoke(unitId);
            return true;
        }

        /// <summary>
        /// 유닛의 장비 아이템 2개를 반환한다.
        /// </summary>
        /// <returns>(슬롯1 아이템 ID, 슬롯2 아이템 ID)</returns>
        public (string, string) GetEquippedItems(string unitId)
        {
            OwnedUnitData unit = _playerData.GetOwnedUnit(unitId);

            if (unit == null)
            {
                return ("", "");
            }

            return (unit.equipItem1 ?? "", unit.equipItem2 ?? "");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 세미콜론 구분 문자열을 List로 파싱한다.
        /// </summary>
        private List<string> ParseSemicolonList(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new List<string>(0);
            }

            string[] parts = value.Split(';');
            var result = new List<string>(parts.Length);

            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();

                if (!string.IsNullOrEmpty(trimmed))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        /// <summary>
        /// 기존 덱 카드의 ownStack 복원 (useStack 감소).
        /// </summary>
        private void RestoreDeckStacks(OwnedUnitData unit)
        {
            if (string.IsNullOrEmpty(unit.editedDeck))
            {
                return;
            }

            List<string> oldDeck = ParseSemicolonList(unit.editedDeck);

            for (int i = 0; i < oldDeck.Count; i++)
            {
                InventoryItemData invItem = _playerData.GetInventoryItem(oldDeck[i]);

                if (invItem != null)
                {
                    invItem.ownStack++;
                    invItem.useStack--;
                }
            }
        }

        /// <summary>
        /// 새 덱 카드의 useStack 적용 (ownStack 감소).
        /// </summary>
        private void ApplyDeckStacks(List<string> cardIds)
        {
            for (int i = 0; i < cardIds.Count; i++)
            {
                InventoryItemData invItem = _playerData.GetInventoryItem(cardIds[i]);

                if (invItem != null)
                {
                    invItem.ownStack--;
                    invItem.useStack++;
                }
            }
        }

        #endregion
    }
}
