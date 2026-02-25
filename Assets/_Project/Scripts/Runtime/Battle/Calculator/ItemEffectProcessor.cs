using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 아이템 효과 처리 클래스.
    /// 패시브 스탯 보정(Equipment), 소모형 트리거(HasDamage/HasDown),
    /// modifyValue 수식 파싱을 담당한다.
    /// </summary>
    public class ItemEffectProcessor
    {
        #region Private Fields

        private readonly DataManager _dataManager;
        private readonly StatusEffectManager _statusEffectManager;
        private readonly HandManager _handManager;
        private readonly DeckManager _deckManager;

        #endregion

        #region Events

        /// <summary>
        /// 아이템 효과가 적용되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, ItemData> OnItemEffectApplied;

        /// <summary>
        /// 아이템이 소모되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, ItemData> OnItemDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// ItemEffectProcessor를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        /// <param name="statusEffectManager">상태이상 매니저</param>
        /// <param name="handManager">손패 매니저</param>
        /// <param name="deckManager">덱 매니저</param>
        public ItemEffectProcessor(DataManager dataManager, StatusEffectManager statusEffectManager,
            HandManager handManager, DeckManager deckManager)
        {
            _dataManager = dataManager;
            _statusEffectManager = statusEffectManager;
            _handManager = handManager;
            _deckManager = deckManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 시작 시 모든 아군 유닛의 Equipment 타입 아이템 패시브 효과를 적용한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ApplyPassiveEffects(BattleState state)
        {
            for (int i = 0; i < state.Allies.Count; i++)
            {
                BattleUnit ally = state.Allies[i];

                for (int j = 0; j < ally.EquippedItemIds.Count; j++)
                {
                    ItemData item = _dataManager.GetItem(ally.EquippedItemIds[j]);

                    if (item == null || item.itemType != ItemType.Equipment)
                    {
                        continue;
                    }

                    ApplyItemEffect(ally, item, state);
                    OnItemEffectApplied?.Invoke(ally, item);
                }
            }
        }

        /// <summary>
        /// 트리거 조건에 해당하는 아이템 효과를 처리한다 (HasDamage / HasDown).
        /// </summary>
        /// <param name="unit">트리거 대상 유닛</param>
        /// <param name="triggerType">트리거 타입</param>
        /// <param name="state">전투 상태</param>
        public void ProcessTrigger(BattleUnit unit, ItemType triggerType, BattleState state)
        {
            for (int i = unit.EquippedItemIds.Count - 1; i >= 0; i--)
            {
                ItemData item = _dataManager.GetItem(unit.EquippedItemIds[i]);

                if (item == null || item.itemType != triggerType)
                {
                    continue;
                }

                List<BattleUnit> targets = ResolveItemTargets(unit, item.targetUnit, state);

                for (int j = 0; j < targets.Count; j++)
                {
                    ApplyItemEffect(targets[j], item, state);
                }

                OnItemEffectApplied?.Invoke(unit, item);

                if (item.isDisposable)
                {
                    TryDisposeItem(unit, item, i);
                }
            }
        }

        #endregion

        #region Private Methods

        private void ApplyItemEffect(BattleUnit target, ItemData item, BattleState state)
        {
            switch (item.targetStatus)
            {
                case ItemTargetStatus.MaxHP:
                {
                    float newMaxHP = ParseModifyValue(item.modifyValue, target.MaxHP);
                    int diff = Mathf.RoundToInt(newMaxHP) - target.MaxHP;
                    target.MaxHP = Mathf.RoundToInt(newMaxHP);
                    if (diff > 0)
                    {
                        target.CurrentHP = Mathf.Min(target.CurrentHP + diff, target.MaxHP);
                    }
                    break;
                }

                case ItemTargetStatus.NowHP:
                {
                    float newHP = ParseModifyValue(item.modifyValue, target.CurrentHP);
                    int healAmount = Mathf.RoundToInt(newHP) - target.CurrentHP;
                    if (healAmount > 0)
                    {
                        target.Heal(healAmount);
                    }
                    else if (healAmount < 0)
                    {
                        target.TakeDamage(-healAmount);
                    }
                    break;
                }

                case ItemTargetStatus.MaxEnergy:
                {
                    float newEnergy = ParseModifyValue(item.modifyValue, state.BaseEnergy);
                    state.BaseEnergy = Mathf.Max(Mathf.RoundToInt(newEnergy), 0);
                    break;
                }

                case ItemTargetStatus.NowEnergy:
                {
                    float newEnergy = ParseModifyValue(item.modifyValue, state.CurrentEnergy);
                    state.CurrentEnergy = Mathf.Max(Mathf.RoundToInt(newEnergy), 0);
                    break;
                }

                case ItemTargetStatus.CostInHand:
                {
                    var handCards = _handManager.GetAllCards();
                    float modValue = ParseModifyValueRaw(item.modifyValue);
                    for (int i = 0; i < handCards.Count; i++)
                    {
                        handCards[i].ApplyModification(ModificationType.Cost, modValue, ModDuration.UntilPlayed);
                    }
                    break;
                }

                case ItemTargetStatus.StatusEffect:
                {
                    if (!string.IsNullOrEmpty(item.modifyValue))
                    {
                        _statusEffectManager.ApplyStatus(target, item.modifyValue, 1, target.UnitId);
                    }
                    break;
                }

                case ItemTargetStatus.AddCard:
                {
                    if (!string.IsNullOrEmpty(item.modifyValue))
                    {
                        CardData cardData = _dataManager.GetCard(item.modifyValue);
                        if (cardData != null)
                        {
                            var runtimeCard = new RuntimeCard(cardData, target.UnitId);
                            _handManager.AddToHand(runtimeCard);
                        }
                    }
                    break;
                }

                case ItemTargetStatus.AddDeck:
                {
                    if (!string.IsNullOrEmpty(item.modifyValue))
                    {
                        _deckManager.AddCardToDeck(item.modifyValue, target.UnitId);
                    }
                    break;
                }

                case ItemTargetStatus.MaxAP:
                case ItemTargetStatus.NowAP:
                    break;
            }
        }

        private List<BattleUnit> ResolveItemTargets(BattleUnit owner, ItemTargetUnit targetUnit, BattleState state)
        {
            var targets = new List<BattleUnit>(3);

            switch (targetUnit)
            {
                case ItemTargetUnit.Self:
                    targets.Add(owner);
                    break;

                case ItemTargetUnit.Player:
                    if (state.Allies.Count > 0)
                    {
                        targets.Add(state.Allies[0]);
                    }
                    break;

                case ItemTargetUnit.AllParty:
                    targets.AddRange(state.AliveAllies);
                    break;

                case ItemTargetUnit.LowestHp:
                    List<BattleUnit> alive = state.AliveAllies;
                    if (alive.Count > 0)
                    {
                        BattleUnit lowest = alive[0];
                        for (int i = 1; i < alive.Count; i++)
                        {
                            if (alive[i].CurrentHP < lowest.CurrentHP)
                            {
                                lowest = alive[i];
                            }
                        }
                        targets.Add(lowest);
                    }
                    break;
            }

            return targets;
        }

        /// <summary>
        /// modifyValue 문자열을 파싱하여 현재 값에 적용한 결과를 반환한다.
        /// "+100" → currentValue + 100, "*1.5" → currentValue * 1.5, "-20" → currentValue - 20
        /// </summary>
        private float ParseModifyValue(string modifyValue, float currentValue)
        {
            if (string.IsNullOrEmpty(modifyValue))
            {
                return currentValue;
            }

            string trimmed = modifyValue.Trim();

            if (trimmed.StartsWith("*"))
            {
                if (float.TryParse(trimmed.Substring(1), out float multiplier))
                {
                    return currentValue * multiplier;
                }
            }
            else if (trimmed.StartsWith("+"))
            {
                if (float.TryParse(trimmed.Substring(1), out float addend))
                {
                    return currentValue + addend;
                }
            }
            else if (trimmed.StartsWith("-"))
            {
                if (float.TryParse(trimmed, out float negativeValue))
                {
                    return currentValue + negativeValue;
                }
            }
            else
            {
                if (float.TryParse(trimmed, out float value))
                {
                    return currentValue + value;
                }
            }

            Debug.LogWarning($"[ItemEffectProcessor] modifyValue 파싱 실패: '{modifyValue}'");
            return currentValue;
        }

        /// <summary>
        /// modifyValue의 원시 수치를 반환한다 (코스트 변경 등 직접 적용 시).
        /// </summary>
        private float ParseModifyValueRaw(string modifyValue)
        {
            if (string.IsNullOrEmpty(modifyValue))
            {
                return 0f;
            }

            string trimmed = modifyValue.Trim();

            if (trimmed.StartsWith("+") || trimmed.StartsWith("-") || trimmed.StartsWith("*"))
            {
                if (float.TryParse(trimmed.Substring(trimmed.StartsWith("*") ? 1 : 0), out float val))
                {
                    return trimmed.StartsWith("-") ? -Mathf.Abs(val) : val;
                }
            }

            if (float.TryParse(trimmed, out float result))
            {
                return result;
            }

            return 0f;
        }

        private void TryDisposeItem(BattleUnit unit, ItemData item, int itemIndex)
        {
            if (UnityEngine.Random.value < item.disposePercentage)
            {
                if (itemIndex >= 0 && itemIndex < unit.EquippedItemIds.Count)
                {
                    unit.EquippedItemIds.RemoveAt(itemIndex);
                }

                OnItemDisposed?.Invoke(unit, item);
            }
        }

        #endregion
    }
}
