using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 스킬 자동 발동 관리 클래스.
    /// 전투 이벤트에 의한 트리거 조건 평가, 우선순위 정렬, 사용 제한 체크를 담당한다.
    /// 스킬 효과는 CardExecutor를 통해 실행된다.
    /// </summary>
    public class SkillExecutor
    {
        #region Private Fields

        private CardExecutor _cardExecutor;
        private readonly DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// 스킬이 발동되었을 때 발행. (소유 유닛, 스킬 데이터)
        /// </summary>
        public event Action<BattleUnit, SkillData> OnSkillTriggered;

        #endregion

        #region Constructor

        /// <summary>
        /// SkillExecutor를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public SkillExecutor(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// CardExecutor를 연결한다. 전투 초기화 시 호출.
        /// </summary>
        /// <param name="cardExecutor">카드 실행기</param>
        public void Initialize(CardExecutor cardExecutor)
        {
            _cardExecutor = cardExecutor;
        }

        /// <summary>
        /// 특정 트리거 이벤트 발생 시 모든 유닛의 스킬 조건을 검사하고 발동한다.
        /// </summary>
        /// <param name="triggerStatus">발생한 트리거 상태</param>
        /// <param name="triggerSource">트리거를 발생시킨 유닛</param>
        /// <param name="state">전투 상태</param>
        /// <param name="element">관련 속성 (해당 시)</param>
        /// <param name="chainDepth">현재 체인 깊이</param>
        public void CheckTriggers(SkillTriggerStatus triggerStatus, BattleUnit triggerSource,
            BattleState state, ElementType? element = null, int chainDepth = 0)
        {
            if (chainDepth >= CardExecutor.MAX_CHAIN_DEPTH)
            {
                return;
            }

            var triggered = new List<(BattleUnit owner, SkillData skill)>(8);
            var allUnits = state.AllUnits;

            for (int i = 0; i < allUnits.Count; i++)
            {
                BattleUnit unit = allUnits[i];

                if (!unit.IsAlive || string.IsNullOrEmpty(unit.SkillId))
                {
                    continue;
                }

                SkillData skill = _dataManager.GetSkill(unit.SkillId);

                if (skill == null)
                {
                    continue;
                }

                if (!CanUseSkill(skill, unit))
                {
                    continue;
                }

                if (EvaluateTrigger(skill, unit, triggerStatus, triggerSource, state, element))
                {
                    triggered.Add((unit, skill));
                }
            }

            if (triggered.Count == 0)
            {
                return;
            }

            var sorted = SortByPriority(triggered, state);

            for (int i = 0; i < sorted.Count; i++)
            {
                ExecuteSkill(sorted[i].owner, sorted[i].skill, state, chainDepth);
            }
        }

        /// <summary>
        /// 스킬의 트리거 조건을 평가한다.
        /// </summary>
        /// <param name="skill">스킬 데이터</param>
        /// <param name="skillOwner">스킬 소유 유닛</param>
        /// <param name="triggerStatus">발생한 트리거 상태</param>
        /// <param name="triggerSource">트리거 발생 유닛</param>
        /// <param name="state">전투 상태</param>
        /// <param name="element">관련 속성</param>
        /// <returns>트리거 조건 충족 여부</returns>
        public bool EvaluateTrigger(SkillData skill, BattleUnit skillOwner,
            SkillTriggerStatus triggerStatus, BattleUnit triggerSource,
            BattleState state, ElementType? element)
        {
            if (skill.triggerStatus != triggerStatus)
            {
                return false;
            }

            if (!MatchesTriggerTarget(skill.triggerTarget, skillOwner, triggerSource, state))
            {
                return false;
            }

            if (element.HasValue && skill.triggerElement != ElementType.Wild && skill.triggerElement != element.Value)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(skill.triggerValue))
            {
                if (!EvaluateTriggerValue(skill, triggerSource, state))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 스킬 사용 가능 여부를 확인한다.
        /// </summary>
        /// <param name="skill">스킬 데이터</param>
        /// <param name="owner">소유 유닛</param>
        /// <returns>사용 가능 여부</returns>
        public bool CanUseSkill(SkillData skill, BattleUnit owner)
        {
            switch (skill.limitType)
            {
                case SkillLimitType.None:
                    return true;

                case SkillLimitType.CoolDown:
                    return owner.SkillCooldownRemaining <= 0;

                case SkillLimitType.PerTurn:
                    return owner.SkillUsedThisTurn < skill.limitValue;

                case SkillLimitType.PerBattle:
                    return owner.SkillUsedThisBattle < skill.limitValue;

                default:
                    return true;
            }
        }

        #endregion

        #region Private Methods

        private void ExecuteSkill(BattleUnit owner, SkillData skill, BattleState state, int chainDepth)
        {
            if (_cardExecutor == null)
            {
                Debug.LogError("[SkillExecutor] CardExecutor가 초기화되지 않았습니다.");
                return;
            }

            List<BattleUnit> targets = _cardExecutor.ResolveTargets(
                owner, skill.targetType, skill.targetFilter,
                skill.targetSelectionRule, skill.targetCount, state);

            _cardExecutor.ExecuteEffect(skill.cardEffectId, owner, targets, state, chainDepth + 1);

            owner.SkillUsedThisTurn++;
            owner.SkillUsedThisBattle++;

            if (skill.limitType == SkillLimitType.CoolDown)
            {
                owner.SkillCooldownRemaining = skill.limitValue;
            }

            OnSkillTriggered?.Invoke(owner, skill);
        }

        private bool MatchesTriggerTarget(SkillTriggerTarget triggerTarget,
            BattleUnit skillOwner, BattleUnit triggerSource, BattleState state)
        {
            if (triggerSource == null)
            {
                return triggerTarget == SkillTriggerTarget.AllAlly ||
                       triggerTarget == SkillTriggerTarget.AllEnemy;
            }

            switch (triggerTarget)
            {
                case SkillTriggerTarget.Self:
                    return triggerSource.UnitId == skillOwner.UnitId;

                case SkillTriggerTarget.Ally:
                    return triggerSource.UnitType == skillOwner.UnitType;

                case SkillTriggerTarget.Enemy:
                    return triggerSource.UnitType != skillOwner.UnitType;

                case SkillTriggerTarget.AllAlly:
                    return triggerSource.UnitType == skillOwner.UnitType;

                case SkillTriggerTarget.AllEnemy:
                    return triggerSource.UnitType != skillOwner.UnitType;

                case SkillTriggerTarget.InHand:
                    return true;

                default:
                    return false;
            }
        }

        private bool EvaluateTriggerValue(SkillData skill, BattleUnit triggerSource, BattleState state)
        {
            if (!float.TryParse(skill.triggerValue, out float numericValue))
            {
                return true;
            }

            float compareValue = 0f;

            switch (skill.triggerStatus)
            {
                case SkillTriggerStatus.CauseDamage:
                case SkillTriggerStatus.HasDamage:
                case SkillTriggerStatus.HasDefend:
                    compareValue = numericValue;
                    break;

                case SkillTriggerStatus.StatusEffectCount:
                    if (triggerSource != null)
                    {
                        compareValue = triggerSource.StatusEffects.Count;
                    }
                    break;

                default:
                    return true;
            }

            return CompareValues(compareValue, skill.comparisonOperator, numericValue);
        }

        private bool CompareValues(float actual, ComparisonOperator op, float expected)
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

        private List<(BattleUnit owner, SkillData skill)> SortByPriority(
            List<(BattleUnit owner, SkillData skill)> triggered, BattleState state)
        {
            triggered.Sort((a, b) =>
            {
                int priorityA = GetUnitPriority(a.owner);
                int priorityB = GetUnitPriority(b.owner);

                if (priorityA != priorityB)
                {
                    return priorityA.CompareTo(priorityB);
                }

                return a.owner.Position.CompareTo(b.owner.Position);
            });

            return triggered;
        }

        /// <summary>
        /// 유닛 타입에 따른 스킬 우선순위를 반환한다.
        /// 아군(0) > 보스/일반 적(1). 같은 그룹 내에서는 Position으로 정렬.
        /// </summary>
        private int GetUnitPriority(BattleUnit unit)
        {
            if (unit.UnitType == UnitType.Ally)
            {
                return 0;
            }

            return 1;
        }

        #endregion
    }
}
