using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// AI 행동 결정 결과.
    /// </summary>
    public struct AIDecision
    {
        /// <summary>행동 타입 (EarnCard, PlayCard, Pass)</summary>
        public AIActionType ActionType;
        /// <summary>사용/추가할 카드 ID</summary>
        public string CardId;
        /// <summary>타겟 선정 규칙</summary>
        public TargetSelectionRule TargetSelection;
        /// <summary>말풍선 대사 (UI용)</summary>
        public string SpeechLine;
        /// <summary>컷인 연출 어드레서블 ID (UI용)</summary>
        public string CutInEffect;
        /// <summary>줌인 연출 여부 (UI용)</summary>
        public bool ZoomIn;
    }

    /// <summary>
    /// 적 유닛 AI 행동 결정 클래스.
    /// AIPattern → Rules (우선순위 평가) → Conditions (AND 조건) 기반으로 행동을 결정한다.
    /// 매칭되는 규칙이 없으면 Default Action을 실행한다.
    /// </summary>
    public class BattleAI
    {
        #region Private Fields

        private readonly DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// AI 행동이 결정되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, AIDecision> OnAIActionDecided;

        #endregion

        #region Constructor

        /// <summary>
        /// BattleAI를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public BattleAI(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 적 유닛의 다음 턴 행동을 결정한다.
        /// </summary>
        /// <param name="enemy">적 유닛</param>
        /// <param name="state">전투 상태</param>
        /// <returns>AI 결정 결과</returns>
        public AIDecision DecideAction(BattleUnit enemy, BattleState state)
        {
            if (string.IsNullOrEmpty(enemy.CurrentAIPatternId))
            {
                return CreatePassDecision();
            }

            AIPatternData pattern = _dataManager.GetAIPattern(enemy.CurrentAIPatternId);

            if (pattern == null)
            {
                Debug.LogWarning($"[BattleAI] AI 패턴 ID '{enemy.CurrentAIPatternId}'을(를) 찾을 수 없습니다.");
                return CreatePassDecision();
            }

            List<AIPatternRuleData> rules = GetRulesForPattern(pattern.id);
            rules.Sort((a, b) => b.priority.CompareTo(a.priority));

            for (int i = 0; i < rules.Count; i++)
            {
                AIPatternRuleData rule = rules[i];
                List<AIConditionData> conditions = GetConditionsForRule(rule.ruleId);

                if (EvaluateAllConditions(conditions, enemy, state))
                {
                    var decision = new AIDecision
                    {
                        ActionType = rule.actionType,
                        CardId = rule.cardId,
                        TargetSelection = rule.targetSelection,
                        SpeechLine = rule.speechLine,
                        CutInEffect = rule.cutInEffect,
                        ZoomIn = rule.zoomIn
                    };

                    OnAIActionDecided?.Invoke(enemy, decision);
                    return decision;
                }
            }

            var defaultDecision = new AIDecision
            {
                ActionType = pattern.defaultActionType,
                CardId = pattern.defaultCardId,
                TargetSelection = pattern.defaultTargetSelection,
                SpeechLine = null,
                CutInEffect = null,
                ZoomIn = false
            };

            OnAIActionDecided?.Invoke(enemy, defaultDecision);
            return defaultDecision;
        }

        /// <summary>
        /// AI 결정에 따라 행동을 실행한다.
        /// </summary>
        /// <param name="decision">AI 결정</param>
        /// <param name="enemy">적 유닛</param>
        /// <param name="state">전투 상태</param>
        /// <param name="cardExecutor">카드 실행기</param>
        public void ExecuteAction(AIDecision decision, BattleUnit enemy,
            BattleState state, CardExecutor cardExecutor)
        {
            switch (decision.ActionType)
            {
                case AIActionType.EarnCard:
                    ExecuteEarnCard(decision, enemy);
                    break;

                case AIActionType.PlayCard:
                    ExecutePlayCard(decision, enemy, state, cardExecutor);
                    break;

                case AIActionType.Pass:
                    break;
            }
        }

        /// <summary>
        /// AI 타겟 선택 규칙에 따라 타겟을 결정한다 (적 시점에서 아군을 공격).
        /// </summary>
        /// <param name="rule">타겟 선정 규칙</param>
        /// <param name="enemy">적 유닛 (시전자)</param>
        /// <param name="state">전투 상태</param>
        /// <returns>선택된 타겟 목록</returns>
        public List<BattleUnit> ResolveAITarget(TargetSelectionRule rule,
            BattleUnit enemy, BattleState state)
        {
            var targets = new List<BattleUnit>(3);
            List<BattleUnit> aliveAllies = state.AliveAllies;

            switch (rule)
            {
                case TargetSelectionRule.LowestHp:
                    if (aliveAllies.Count > 0)
                    {
                        BattleUnit lowest = aliveAllies[0];
                        for (int i = 1; i < aliveAllies.Count; i++)
                        {
                            if (aliveAllies[i].CurrentHP < lowest.CurrentHP)
                            {
                                lowest = aliveAllies[i];
                            }
                        }
                        targets.Add(lowest);
                    }
                    break;

                case TargetSelectionRule.HighestHp:
                    if (aliveAllies.Count > 0)
                    {
                        BattleUnit highest = aliveAllies[0];
                        for (int i = 1; i < aliveAllies.Count; i++)
                        {
                            if (aliveAllies[i].CurrentHP > highest.CurrentHP)
                            {
                                highest = aliveAllies[i];
                            }
                        }
                        targets.Add(highest);
                    }
                    break;

                case TargetSelectionRule.Random:
                    if (aliveAllies.Count > 0)
                    {
                        int index = UnityEngine.Random.Range(0, aliveAllies.Count);
                        targets.Add(aliveAllies[index]);
                    }
                    break;

                case TargetSelectionRule.Self:
                    targets.Add(enemy);
                    break;

                case TargetSelectionRule.AllTargets:
                    targets.AddRange(aliveAllies);
                    break;

                default:
                    if (aliveAllies.Count > 0)
                    {
                        targets.Add(aliveAllies[UnityEngine.Random.Range(0, aliveAllies.Count)]);
                    }
                    break;
            }

            return targets;
        }

        #endregion

        #region Private Methods

        private List<AIPatternRuleData> GetRulesForPattern(string patternId)
        {
            var rules = new List<AIPatternRuleData>(4);
            List<AIPatternRuleData> allRules = _dataManager.AIPatternRules.Entries;

            for (int i = 0; i < allRules.Count; i++)
            {
                if (allRules[i].aiPatternId == patternId)
                {
                    rules.Add(allRules[i]);
                }
            }

            return rules;
        }

        private List<AIConditionData> GetConditionsForRule(string ruleId)
        {
            var conditions = new List<AIConditionData>(4);
            List<AIConditionData> allConditions = _dataManager.AIConditions.Entries;

            for (int i = 0; i < allConditions.Count; i++)
            {
                if (allConditions[i].ruleId == ruleId)
                {
                    conditions.Add(allConditions[i]);
                }
            }

            return conditions;
        }

        private bool EvaluateAllConditions(List<AIConditionData> conditions,
            BattleUnit enemy, BattleState state)
        {
            if (conditions.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < conditions.Count; i++)
            {
                if (!EvaluateCondition(conditions[i], enemy, state))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateCondition(AIConditionData condition, BattleUnit enemy, BattleState state)
        {
            switch (condition.conditionType)
            {
                case AIConditionType.TurnCount:
                    return CompareFloat(state.CurrentTurn, condition.comparisonOperator, ParseFloat(condition.value));

                case AIConditionType.TurnMod:
                    if (condition.divisor <= 0) return false;
                    int modResult = state.CurrentTurn % condition.divisor;
                    return modResult == condition.remainder;

                case AIConditionType.HpPercent:
                    float hpPercent = (enemy.MaxHP > 0)
                        ? ((float)enemy.CurrentHP / enemy.MaxHP) * 100f
                        : 0f;
                    return CompareFloat(hpPercent, condition.comparisonOperator, ParseFloat(condition.value));

                case AIConditionType.EnemyHpPercent:
                    float targetHpPercent = CalculateAverageAllyHpPercent(state);
                    return CompareFloat(targetHpPercent, condition.comparisonOperator, ParseFloat(condition.value));

                case AIConditionType.HasCard:
                    bool hasCard = enemy.DeckCardIds.Contains(condition.value);
                    return condition.comparisonOperator == ComparisonOperator.Equal ? hasCard : !hasCard;

                case AIConditionType.StatusActive:
                    bool statusActive = HasStatusEffect(enemy, condition.value);
                    return condition.comparisonOperator == ComparisonOperator.Equal ? statusActive : !statusActive;

                default:
                    return false;
            }
        }

        private bool HasStatusEffect(BattleUnit unit, string statusEffectId)
        {
            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                if (unit.StatusEffects[i].BaseData.id == statusEffectId)
                {
                    return true;
                }
            }
            return false;
        }

        private float CalculateAverageAllyHpPercent(BattleState state)
        {
            List<BattleUnit> aliveAllies = state.AliveAllies;
            if (aliveAllies.Count == 0) return 0f;

            float totalPercent = 0f;
            for (int i = 0; i < aliveAllies.Count; i++)
            {
                if (aliveAllies[i].MaxHP > 0)
                {
                    totalPercent += ((float)aliveAllies[i].CurrentHP / aliveAllies[i].MaxHP) * 100f;
                }
            }
            return totalPercent / aliveAllies.Count;
        }

        private float ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
            {
                return result;
            }
            return 0f;
        }

        private bool CompareFloat(float actual, ComparisonOperator op, float expected)
        {
            switch (op)
            {
                case ComparisonOperator.Equal:
                    return Mathf.Approximately(actual, expected);
                case ComparisonOperator.NotEqual:
                    return !Mathf.Approximately(actual, expected);
                case ComparisonOperator.GreaterThan:
                    return actual > expected;
                case ComparisonOperator.LessThan:
                    return actual < expected;
                case ComparisonOperator.GreaterOrEqual:
                    return actual >= expected;
                case ComparisonOperator.LessOrEqual:
                    return actual <= expected;
                default:
                    return false;
            }
        }

        private void ExecuteEarnCard(AIDecision decision, BattleUnit enemy)
        {
            if (string.IsNullOrEmpty(decision.CardId))
            {
                return;
            }

            if (!enemy.DeckCardIds.Contains(decision.CardId))
            {
                enemy.DeckCardIds.Add(decision.CardId);
            }
        }

        private void ExecutePlayCard(AIDecision decision, BattleUnit enemy,
            BattleState state, CardExecutor cardExecutor)
        {
            if (string.IsNullOrEmpty(decision.CardId))
            {
                return;
            }

            CardData cardData = _dataManager.GetCard(decision.CardId);

            if (cardData == null)
            {
                Debug.LogWarning($"[BattleAI] AI 카드 ID '{decision.CardId}'을(를) 찾을 수 없습니다.");
                return;
            }

            var runtimeCard = new RuntimeCard(cardData, enemy.UnitId);
            List<BattleUnit> targets;

            if (cardData.targetType == TargetType.Ally)
            {
                targets = ResolveAITargetForSelf(decision.TargetSelection, enemy, state);
            }
            else
            {
                targets = ResolveAITarget(decision.TargetSelection, enemy, state);
            }

            cardExecutor.ExecuteCard(runtimeCard, enemy, targets, state);
        }

        private List<BattleUnit> ResolveAITargetForSelf(TargetSelectionRule rule,
            BattleUnit enemy, BattleState state)
        {
            var targets = new List<BattleUnit>(5);
            List<BattleUnit> aliveEnemies = state.AliveEnemies;

            switch (rule)
            {
                case TargetSelectionRule.Self:
                    targets.Add(enemy);
                    break;

                case TargetSelectionRule.LowestHp:
                    if (aliveEnemies.Count > 0)
                    {
                        BattleUnit lowest = aliveEnemies[0];
                        for (int i = 1; i < aliveEnemies.Count; i++)
                        {
                            if (aliveEnemies[i].CurrentHP < lowest.CurrentHP)
                            {
                                lowest = aliveEnemies[i];
                            }
                        }
                        targets.Add(lowest);
                    }
                    break;

                case TargetSelectionRule.AllTargets:
                    targets.AddRange(aliveEnemies);
                    break;

                default:
                    targets.Add(enemy);
                    break;
            }

            return targets;
        }

        private AIDecision CreatePassDecision()
        {
            return new AIDecision
            {
                ActionType = AIActionType.Pass,
                CardId = null,
                TargetSelection = TargetSelectionRule.Random,
                SpeechLine = null,
                CutInEffect = null,
                ZoomIn = false
            };
        }

        #endregion
    }
}
