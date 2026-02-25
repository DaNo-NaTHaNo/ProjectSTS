using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 카드 효과 실행 클래스.
    /// 8개 CardEffectType(Damage, Block, Heal, Energy, ApplyStatus, ModifyCard, Draw, AddCard)의
    /// 분기 실행과 타겟 선택, 체인 깊이 관리를 담당한다.
    /// </summary>
    public class CardExecutor
    {
        #region Constants

        /// <summary>
        /// 스킬 → 효과 → 스킬 재귀 호출의 최대 깊이. 무한 루프를 방지한다.
        /// </summary>
        public const int MAX_CHAIN_DEPTH = 10;

        #endregion

        #region Private Fields

        private readonly DeckManager _deckManager;
        private readonly HandManager _handManager;
        private readonly StatusEffectManager _statusEffectManager;
        private readonly DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// 카드 사용이 완료되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, RuntimeCard> OnCardPlayed;

        /// <summary>
        /// 대미지가 적용되었을 때 발행. (공격자, 피격자, 대미지량)
        /// </summary>
        public event Action<BattleUnit, BattleUnit, int> OnDamageDealt;

        /// <summary>
        /// 방어도가 획득되었을 때 발행. (유닛, 방어도량)
        /// </summary>
        public event Action<BattleUnit, int> OnBlockGained;

        /// <summary>
        /// 회복이 적용되었을 때 발행. (유닛, 회복량)
        /// </summary>
        public event Action<BattleUnit, int> OnHealApplied;

        /// <summary>
        /// 상태이상이 적용되었을 때 발행. (시전자, 상태이상 ID)
        /// </summary>
        public event Action<BattleUnit, string> OnStatusEffectApplied;

        /// <summary>
        /// 에너지가 변경되었을 때 발행. (변경 후 에너지)
        /// </summary>
        public event Action<int> OnEnergyChanged;

        /// <summary>
        /// 카드 드로우가 발생했을 때 발행. (드로우 장수)
        /// </summary>
        public event Action<int> OnCardsDrawn;

        #endregion

        #region Constructor

        /// <summary>
        /// CardExecutor를 생성한다.
        /// </summary>
        /// <param name="deckManager">덱 매니저</param>
        /// <param name="handManager">손패 매니저</param>
        /// <param name="statusEffectManager">상태이상 매니저</param>
        /// <param name="dataManager">데이터 매니저</param>
        public CardExecutor(DeckManager deckManager, HandManager handManager,
            StatusEffectManager statusEffectManager, DataManager dataManager)
        {
            _deckManager = deckManager;
            _handManager = handManager;
            _statusEffectManager = statusEffectManager;
            _dataManager = dataManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 카드를 실행한다. CardEffectData를 조회하여 effectType별 핸들러를 호출한다.
        /// </summary>
        /// <param name="card">실행할 카드</param>
        /// <param name="caster">시전자 유닛</param>
        /// <param name="targets">대상 유닛 목록</param>
        /// <param name="state">전투 상태</param>
        /// <param name="chainDepth">현재 체인 깊이 (스킬 재귀 방지)</param>
        public void ExecuteCard(RuntimeCard card, BattleUnit caster, List<BattleUnit> targets,
            BattleState state, int chainDepth = 0)
        {
            if (chainDepth >= MAX_CHAIN_DEPTH)
            {
                Debug.LogWarning($"[CardExecutor] 체인 깊이 한도 도달 ({MAX_CHAIN_DEPTH}). 실행 중단.");
                return;
            }

            CardEffectData effectData = _dataManager.GetCardEffect(card.BaseData.cardEffectId);

            if (effectData == null)
            {
                Debug.LogWarning($"[CardExecutor] 카드 효과 ID '{card.BaseData.cardEffectId}'을(를) 찾을 수 없습니다.");
                return;
            }

            ExecuteEffectInternal(effectData, card, caster, targets, state, chainDepth);

            if (chainDepth == 0)
            {
                OnCardPlayed?.Invoke(caster, card);
            }
        }

        /// <summary>
        /// cardEffectId로 효과를 직접 실행한다. 스킬에서 호출할 때 사용한다.
        /// </summary>
        /// <param name="cardEffectId">카드 효과 ID</param>
        /// <param name="caster">시전자 유닛</param>
        /// <param name="targets">대상 유닛 목록</param>
        /// <param name="state">전투 상태</param>
        /// <param name="chainDepth">현재 체인 깊이</param>
        public void ExecuteEffect(string cardEffectId, BattleUnit caster, List<BattleUnit> targets,
            BattleState state, int chainDepth)
        {
            if (chainDepth >= MAX_CHAIN_DEPTH)
            {
                Debug.LogWarning($"[CardExecutor] 체인 깊이 한도 도달 ({MAX_CHAIN_DEPTH}). 실행 중단.");
                return;
            }

            CardEffectData effectData = _dataManager.GetCardEffect(cardEffectId);

            if (effectData == null)
            {
                Debug.LogWarning($"[CardExecutor] 카드 효과 ID '{cardEffectId}'을(를) 찾을 수 없습니다.");
                return;
            }

            ExecuteEffectInternal(effectData, null, caster, targets, state, chainDepth);
        }

        /// <summary>
        /// 타겟을 선택한다. TargetSelectionRule에 따라 자동 선택하거나 외부 지정을 그대로 사용한다.
        /// </summary>
        /// <param name="caster">시전자 유닛</param>
        /// <param name="targetType">대상 진영</param>
        /// <param name="filter">타겟 제한 규칙</param>
        /// <param name="rule">자동 선택 규칙</param>
        /// <param name="targetCount">최대 타겟 수 (0이면 무제한)</param>
        /// <param name="state">전투 상태</param>
        /// <returns>선택된 타겟 유닛 목록</returns>
        public List<BattleUnit> ResolveTargets(BattleUnit caster, TargetType targetType,
            TargetFilter filter, TargetSelectionRule rule, int targetCount, BattleState state)
        {
            List<BattleUnit> candidates = GetCandidates(caster, targetType, filter, state);

            if (candidates.Count == 0)
            {
                return candidates;
            }

            List<BattleUnit> selected;

            switch (rule)
            {
                case TargetSelectionRule.AllTargets:
                    selected = new List<BattleUnit>(candidates);
                    break;

                case TargetSelectionRule.LowestHp:
                    candidates.Sort((a, b) => a.CurrentHP.CompareTo(b.CurrentHP));
                    selected = TakeCount(candidates, targetCount);
                    break;

                case TargetSelectionRule.HighestHp:
                    candidates.Sort((a, b) => b.CurrentHP.CompareTo(a.CurrentHP));
                    selected = TakeCount(candidates, targetCount);
                    break;

                case TargetSelectionRule.Random:
                    ShuffleList(candidates);
                    selected = TakeCount(candidates, targetCount);
                    break;

                case TargetSelectionRule.Self:
                    selected = new List<BattleUnit>(1) { caster };
                    break;

                case TargetSelectionRule.Manual:
                default:
                    selected = new List<BattleUnit>(candidates);
                    if (targetCount > 0 && selected.Count > targetCount)
                    {
                        selected.RemoveRange(targetCount, selected.Count - targetCount);
                    }
                    break;
            }

            return selected;
        }

        #endregion

        #region Private Methods — 효과 실행

        private void ExecuteEffectInternal(CardEffectData effect, RuntimeCard card,
            BattleUnit caster, List<BattleUnit> targets, BattleState state, int chainDepth)
        {
            switch (effect.effectType)
            {
                case CardEffectType.Damage:
                    HandleDamage(effect, card, caster, targets, state);
                    break;

                case CardEffectType.Block:
                    HandleBlock(effect, card, caster, targets);
                    break;

                case CardEffectType.Heal:
                    HandleHeal(effect, caster, targets);
                    break;

                case CardEffectType.Energy:
                    HandleEnergy(effect, state);
                    break;

                case CardEffectType.ApplyStatus:
                    HandleApplyStatus(effect, caster, targets);
                    break;

                case CardEffectType.ModifyCard:
                    HandleModifyCard(effect, state);
                    break;

                case CardEffectType.Draw:
                    HandleDraw(effect);
                    break;

                case CardEffectType.AddCard:
                    HandleAddCard(effect, caster);
                    break;

                default:
                    Debug.LogWarning($"[CardExecutor] 알 수 없는 EffectType: {effect.effectType}");
                    break;
            }
        }

        private void HandleDamage(CardEffectData effect, RuntimeCard card,
            BattleUnit caster, List<BattleUnit> targets, BattleState state)
        {
            ElementType attackElement = card != null ? card.BaseData.element : caster.Element;

            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];

                if (!target.IsAlive)
                {
                    continue;
                }

                int damage = DamageCalculator.CalculateDamage(
                    effect.value, card, attackElement, caster, target,
                    _statusEffectManager, _dataManager);

                var (actualDamage, remainingBlock) = DamageCalculator.ApplyBlock(damage, target.Block);

                target.Block = remainingBlock;

                if (actualDamage > 0)
                {
                    target.TakeDamage(actualDamage);
                }

                OnDamageDealt?.Invoke(caster, target, damage);

                int reflectDamage = DamageCalculator.CalculateReflectDamage(target);

                if (reflectDamage > 0 && caster.IsAlive)
                {
                    caster.TakeDamage(reflectDamage);
                    OnDamageDealt?.Invoke(target, caster, reflectDamage);
                }
            }
        }

        private void HandleBlock(CardEffectData effect, RuntimeCard card,
            BattleUnit caster, List<BattleUnit> targets)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];

                if (!target.IsAlive)
                {
                    continue;
                }

                int block = DamageCalculator.CalculateBlock(
                    effect.value, card, target, _statusEffectManager);

                target.AddBlock(block);
                OnBlockGained?.Invoke(target, block);
            }
        }

        private void HandleHeal(CardEffectData effect, BattleUnit caster, List<BattleUnit> targets)
        {
            int healAmount = Mathf.RoundToInt(effect.value);

            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];

                if (!target.IsAlive)
                {
                    continue;
                }

                target.Heal(healAmount);
                OnHealApplied?.Invoke(target, healAmount);
            }
        }

        private void HandleEnergy(CardEffectData effect, BattleState state)
        {
            int energyChange = Mathf.RoundToInt(effect.value);
            state.CurrentEnergy = Mathf.Max(state.CurrentEnergy + energyChange, 0);
            OnEnergyChanged?.Invoke(state.CurrentEnergy);
        }

        private void HandleApplyStatus(CardEffectData effect, BattleUnit caster, List<BattleUnit> targets)
        {
            if (string.IsNullOrEmpty(effect.statusEffectId))
            {
                Debug.LogWarning("[CardExecutor] ApplyStatus 효과에 statusEffectId가 없습니다.");
                return;
            }

            int stacks = Mathf.Max(Mathf.RoundToInt(effect.value), 1);

            for (int i = 0; i < targets.Count; i++)
            {
                BattleUnit target = targets[i];

                if (!target.IsAlive)
                {
                    continue;
                }

                _statusEffectManager.ApplyStatus(target, effect.statusEffectId, stacks, caster.UnitId);
                OnStatusEffectApplied?.Invoke(caster, effect.statusEffectId);
            }
        }

        private void HandleModifyCard(CardEffectData effect, BattleState state)
        {
            List<RuntimeCard> targetCards = SelectTargetCards(effect);

            for (int i = 0; i < targetCards.Count; i++)
            {
                targetCards[i].ApplyModification(effect.modificationType, effect.value, effect.modDuration);
            }
        }

        private void HandleDraw(CardEffectData effect)
        {
            int drawCount = Mathf.RoundToInt(effect.value);

            if (drawCount <= 0)
            {
                return;
            }

            List<RuntimeCard> drawn = _deckManager.Draw(drawCount);

            for (int i = 0; i < drawn.Count; i++)
            {
                _handManager.AddToHand(drawn[i]);
            }

            OnCardsDrawn?.Invoke(drawn.Count);
        }

        private void HandleAddCard(CardEffectData effect, BattleUnit caster)
        {
            if (string.IsNullOrEmpty(effect.addCardId))
            {
                Debug.LogWarning("[CardExecutor] AddCard 효과에 addCardId가 없습니다.");
                return;
            }

            string[] cardIds = effect.addCardId.Split(';');

            for (int i = 0; i < cardIds.Length; i++)
            {
                string cardId = cardIds[i].Trim();

                if (string.IsNullOrEmpty(cardId))
                {
                    continue;
                }

                CardData cardData = _dataManager.GetCard(cardId);

                if (cardData == null)
                {
                    Debug.LogWarning($"[CardExecutor] AddCard 카드 ID '{cardId}'을(를) 찾을 수 없습니다.");
                    continue;
                }

                var runtimeCard = new RuntimeCard(cardData, caster.UnitId);
                _handManager.AddToHand(runtimeCard);
            }
        }

        #endregion

        #region Private Methods — 타겟 선택

        private List<BattleUnit> GetCandidates(BattleUnit caster, TargetType targetType,
            TargetFilter filter, BattleState state)
        {
            List<BattleUnit> candidates;
            bool casterIsAlly = caster.UnitType == UnitType.Ally;

            switch (targetType)
            {
                case TargetType.Ally:
                    candidates = casterIsAlly
                        ? new List<BattleUnit>(state.AliveAllies)
                        : new List<BattleUnit>(state.AliveEnemies);
                    break;

                case TargetType.Enemy:
                    candidates = casterIsAlly
                        ? new List<BattleUnit>(state.AliveEnemies)
                        : new List<BattleUnit>(state.AliveAllies);
                    break;

                case TargetType.Both:
                    var allAlive = new List<BattleUnit>(state.AliveAllies.Count + state.AliveEnemies.Count);
                    allAlive.AddRange(state.AliveAllies);
                    allAlive.AddRange(state.AliveEnemies);
                    candidates = allAlive;
                    break;

                default:
                    candidates = new List<BattleUnit>(0);
                    break;
            }

            if (filter == TargetFilter.OnlySelf)
            {
                candidates.Clear();
                if (caster.IsAlive)
                {
                    candidates.Add(caster);
                }
            }

            return candidates;
        }

        private List<RuntimeCard> SelectTargetCards(CardEffectData effect)
        {
            var result = new List<RuntimeCard>(8);

            switch (effect.cardTargetSelection)
            {
                case CardTargetSelection.AllInHand:
                    result.AddRange(_handManager.GetAllCards());
                    break;

                case CardTargetSelection.TypeInHand:
                    result.AddRange(_handManager.GetCardsByType(effect.targetCardType));
                    break;

                case CardTargetSelection.Element:
                    var allCards = _handManager.GetAllCards();
                    for (int i = 0; i < allCards.Count; i++)
                    {
                        if (allCards[i].BaseData.element == ElementType.Wild ||
                            allCards[i].BaseData.element == (ElementType)((int)effect.targetCardType))
                        {
                            result.Add(allCards[i]);
                        }
                    }
                    break;

                case CardTargetSelection.RandomInHand:
                    var handCards = _handManager.GetAllCards();
                    if (handCards.Count > 0)
                    {
                        ShuffleList(handCards);
                        result.Add(handCards[0]);
                    }
                    break;

                case CardTargetSelection.AllInDeck:
                    break;

                case CardTargetSelection.PlayerSelect:
                default:
                    break;
            }

            return result;
        }

        private List<BattleUnit> TakeCount(List<BattleUnit> list, int count)
        {
            if (count <= 0 || count >= list.Count)
            {
                return new List<BattleUnit>(list);
            }

            return list.GetRange(0, count);
        }

        private void ShuffleList<T>(List<T> list)
        {
            var random = new System.Random();

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        #endregion
    }
}
