using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Meta
{
    /// <summary>
    /// 탐험 개시 전 검증 및 씬 전환을 처리한다.
    /// 파티 구성, 덱 크기, 속성, 스킬, 아이템을 검증하고
    /// 카드 제외 규칙을 적용한 후 스테이지 씬으로 전환한다.
    /// </summary>
    public class ExpeditionLauncher
    {
        #region Private Fields

        private readonly PlayerDataManager _playerData;
        private readonly PartyEditManager _partyEdit;
        private readonly DataManager _dataManager;
        private readonly GameSettings _settings;

        #endregion

        #region Events

        /// <summary>
        /// 탐험 개시 시 발행.
        /// </summary>
        public event Action OnExpeditionLaunched;

        #endregion

        #region Constructor

        /// <summary>
        /// ExpeditionLauncher를 생성한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="partyEdit">파티 편성 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public ExpeditionLauncher(
            PlayerDataManager playerData,
            PartyEditManager partyEdit,
            DataManager dataManager)
        {
            _playerData = playerData;
            _partyEdit = partyEdit;
            _dataManager = dataManager;
            _settings = dataManager.Settings;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 탐험 개시 전 전체 검증을 수행한다.
        /// </summary>
        /// <returns>검증 결과</returns>
        public ExpeditionValidationResult ValidateExpedition()
        {
            // 1. 파티 유효성 검증
            if (!_partyEdit.ValidateParty())
            {
                return ExpeditionValidationResult.Fail("파티가 유효하지 않습니다. 주인공을 포함한 1~3명의 파티가 필요합니다.");
            }

            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();

            for (int i = 0; i < partyMembers.Count; i++)
            {
                OwnedUnitData member = partyMembers[i];
                string unitId = member.unitId;

                // 2. 덱 크기 검증
                List<string> deck = _partyEdit.GetEditedDeck(unitId);

                if (deck.Count < _settings.MinDeckSize || deck.Count > _settings.MaxDeckSize)
                {
                    UnitData unitData = _dataManager.GetUnit(unitId);
                    string unitName = unitData != null ? unitData.unitName : unitId;
                    return ExpeditionValidationResult.Fail(
                        $"{unitName}의 덱 크기가 유효하지 않습니다: {deck.Count}장 (필요: {_settings.MinDeckSize}~{_settings.MaxDeckSize}장)");
                }

                // 3. 덱 카드 속성 검증
                for (int j = 0; j < deck.Count; j++)
                {
                    CardData card = _dataManager.GetCard(deck[j]);

                    if (card == null)
                    {
                        return ExpeditionValidationResult.Fail($"존재하지 않는 카드입니다: {deck[j]}");
                    }

                    if (!IsCardElementValid(member.cardElement, card.element))
                    {
                        return ExpeditionValidationResult.Fail($"속성이 일치하지 않는 카드가 덱에 포함되어 있습니다: {card.cardName}");
                    }
                }

                // 4. 스킬 검증
                string skillId = _partyEdit.GetEditedSkill(unitId);

                if (!string.IsNullOrEmpty(skillId))
                {
                    if (!_partyEdit.CanEquipSkill(unitId, skillId))
                    {
                        return ExpeditionValidationResult.Fail($"장비할 수 없는 스킬이 설정되어 있습니다: {skillId}");
                    }
                }

                // 5. 아이템 슬롯 검증
                (string item1, string item2) = _partyEdit.GetEquippedItems(unitId);
                int itemCount = 0;

                if (!string.IsNullOrEmpty(item1))
                {
                    itemCount++;
                }

                if (!string.IsNullOrEmpty(item2))
                {
                    itemCount++;
                }

                if (itemCount > _settings.MaxItemSlots)
                {
                    return ExpeditionValidationResult.Fail($"아이템 슬롯 수를 초과했습니다: {itemCount}/{_settings.MaxItemSlots}");
                }
            }

            return ExpeditionValidationResult.Success();
        }

        /// <summary>
        /// 검증 통과 후 탐험을 개시한다.
        /// 카드 제외 규칙 적용, 탐험 기록 갱신, 씬 전환을 수행한다.
        /// </summary>
        /// <returns>탐험 개시 성공 여부</returns>
        public bool LaunchExpedition()
        {
            ExpeditionValidationResult validation = ValidateExpedition();

            if (!validation.IsValid)
            {
                Debug.LogWarning($"[ExpeditionLauncher] 탐험 개시 실패: {validation.ErrorMessage}");
                return false;
            }

            // 탐험 기록 갱신
            ExplorationRecordData record = _playerData.GetExplorationRecord();
            record.countDepart++;
            _playerData.ResetCurrentExplorationCounters();

            // 씬 전환
            if (ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                OnExpeditionLaunched?.Invoke();
                sceneManager.LoadScene(_settings.StageSceneName);
                Debug.Log("[ExpeditionLauncher] 탐험 개시. 스테이지 씬으로 전환합니다.");
                return true;
            }

            Debug.LogError("[ExpeditionLauncher] SceneTransitionManager를 찾을 수 없습니다.");
            return false;
        }

        /// <summary>
        /// 카드 제외 규칙을 적용하여 전투 덱을 생성한다.
        /// 파티 1명: 제외 없음, 2명: 6번째 카드 제외, 3명: 5~6번째 카드 제외.
        /// </summary>
        /// <returns>전투에 사용할 카드 ID 목록</returns>
        public List<string> BuildBattleDeck()
        {
            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();
            int partySize = partyMembers.Count;
            var battleDeck = new List<string>(18);

            for (int i = 0; i < partyMembers.Count; i++)
            {
                List<string> unitDeck = _partyEdit.GetEditedDeck(partyMembers[i].unitId);
                int maxCards = GetMaxCardsPerUnit(partySize, unitDeck.Count);

                for (int j = 0; j < maxCards && j < unitDeck.Count; j++)
                {
                    battleDeck.Add(unitDeck[j]);
                }
            }

            return battleDeck;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 카드 속성이 유닛에 유효한지 확인한다.
        /// </summary>
        private bool IsCardElementValid(ElementType unitElement, ElementType cardElement)
        {
            if (unitElement == ElementType.Wild)
            {
                return true;
            }

            return cardElement == unitElement || cardElement == ElementType.Wild;
        }

        /// <summary>
        /// 파티 인원 수에 따라 유닛 당 최대 카드 수를 반환한다.
        /// </summary>
        /// <param name="partySize">파티 인원 수</param>
        /// <param name="deckSize">유닛 덱 크기</param>
        /// <returns>전투에 포함할 최대 카드 수</returns>
        private int GetMaxCardsPerUnit(int partySize, int deckSize)
        {
            switch (partySize)
            {
                case 1:
                    return deckSize; // 제외 없음
                case 2:
                    return Mathf.Min(deckSize, 5); // 6번째 카드 제외
                case 3:
                    return Mathf.Min(deckSize, 4); // 5~6번째 카드 제외
                default:
                    return deckSize;
            }
        }

        #endregion
    }

    /// <summary>
    /// 탐험 개시 검증 결과.
    /// </summary>
    public struct ExpeditionValidationResult
    {
        /// <summary>
        /// 검증 통과 여부.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// 검증 실패 시 오류 메시지.
        /// </summary>
        public string ErrorMessage;

        /// <summary>
        /// 성공 결과를 생성한다.
        /// </summary>
        public static ExpeditionValidationResult Success()
        {
            return new ExpeditionValidationResult { IsValid = true, ErrorMessage = "" };
        }

        /// <summary>
        /// 실패 결과를 생성한다.
        /// </summary>
        public static ExpeditionValidationResult Fail(string message)
        {
            return new ExpeditionValidationResult { IsValid = false, ErrorMessage = message };
        }
    }
}
