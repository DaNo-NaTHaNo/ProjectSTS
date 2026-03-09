using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 세이브 데이터가 없는 최초 실행 시 주인공 유닛과 초기 인벤토리를 생성한다.
    /// IntegrationBootstrap에서 LoadSaveData() 이후 호출한다.
    /// </summary>
    public static class FirstRunInitializer
    {
        #region Public Methods

        /// <summary>
        /// 신규 플레이어 초기 데이터를 생성한다.
        /// GameSettings의 주인공 ID로 UnitTable을 조회하여 OwnedUnitData를 만들고,
        /// 초기 덱 카드를 인벤토리에 등록한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public static void InitializeNewPlayer(PlayerDataManager playerData, DataManager dataManager)
        {
            GameSettings settings = dataManager.Settings;

            if (settings == null || string.IsNullOrEmpty(settings.ProtagonistUnitId))
            {
                Debug.LogError("[FirstRunInitializer] GameSettings 또는 ProtagonistUnitId가 설정되지 않았습니다.");
                return;
            }

            string protagonistId = settings.ProtagonistUnitId;
            UnitData unitData = dataManager.GetUnit(protagonistId);

            if (unitData == null)
            {
                Debug.LogError($"[FirstRunInitializer] UnitTable에서 주인공을 찾을 수 없습니다: {protagonistId}");
                return;
            }

            // 1. OwnedUnitData 생성
            var ownedUnit = new OwnedUnitData
            {
                unitId = unitData.id,
                cardElement = unitData.element,
                editedDeck = unitData.initialDeckIds,
                editedSkill = unitData.initialSkillId ?? "",
                equipItem1 = "",
                equipItem2 = "",
                partyPosition = 1
            };

            playerData.AddUnit(ownedUnit);

            // 2. 초기 덱 카드를 인벤토리에 추가
            RegisterInitialDeckToInventory(playerData, dataManager, unitData.initialDeckIds);

            Debug.Log($"[FirstRunInitializer] 주인공 '{unitData.unitName}' ({protagonistId}) 생성 완료. 파티 슬롯 1 배치.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 초기 덱 카드를 인벤토리에 등록한다.
        /// 카드 ID가 중복될 경우 ownStack을 가산한다.
        /// </summary>
        private static void RegisterInitialDeckToInventory(
            PlayerDataManager playerData, DataManager dataManager, string initialDeckIds)
        {
            if (string.IsNullOrEmpty(initialDeckIds))
            {
                return;
            }

            string[] cardIds = initialDeckIds.Split(';');

            for (int i = 0; i < cardIds.Length; i++)
            {
                string cardId = cardIds[i].Trim();

                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                CardData cardData = dataManager.GetCard(cardId);

                if (cardData == null)
                {
                    Debug.LogWarning($"[FirstRunInitializer] CardTable에서 카드를 찾을 수 없습니다: {cardId}");
                    continue;
                }

                var inventoryItem = new InventoryItemData
                {
                    category = InventoryCategory.Card,
                    productId = cardData.id,
                    productName = cardData.cardName,
                    description = cardData.description ?? "",
                    rarity = cardData.rarity,
                    ownStack = 0,
                    useStack = 1,
                    cardElement = cardData.element,
                    cardType = cardData.cardType,
                    cardCost = cardData.cost
                };

                playerData.AddInventoryItem(inventoryItem);
            }
        }

        #endregion
    }
}
