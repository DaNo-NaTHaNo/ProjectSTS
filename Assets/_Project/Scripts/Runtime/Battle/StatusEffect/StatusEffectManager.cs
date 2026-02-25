using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;
using ProjectStS.Core;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 상태이상 관리 클래스.
    /// 적용, 스택 관리, 기간 관리, 트리거 타이밍별 실행, 소모형 처리를 담당한다.
    /// </summary>
    public class StatusEffectManager
    {
        #region Private Fields

        private readonly DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// 상태이상이 유닛에 적용되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, ActiveStatusEffect> OnStatusApplied;

        /// <summary>
        /// 상태이상이 유닛에서 수동 제거되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, ActiveStatusEffect> OnStatusRemoved;

        /// <summary>
        /// 상태이상이 만료되어 제거되었을 때 발행.
        /// </summary>
        public event Action<BattleUnit, ActiveStatusEffect> OnStatusExpired;

        /// <summary>
        /// 상태이상 효과가 실행되었을 때 발행. (유닛, 효과, 적용 값)
        /// </summary>
        public event Action<BattleUnit, ActiveStatusEffect, int> OnStatusTriggered;

        #endregion

        #region Constructor

        /// <summary>
        /// StatusEffectManager를 생성한다.
        /// </summary>
        /// <param name="dataManager">데이터 매니저</param>
        public StatusEffectManager(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #endregion

        #region Public Methods — 적용/제거

        /// <summary>
        /// 유닛에 상태이상을 적용한다.
        /// 이미 같은 ID가 있으면 스택 추가 또는 duration 리프레시한다.
        /// </summary>
        /// <param name="target">대상 유닛</param>
        /// <param name="statusEffectId">상태이상 ID</param>
        /// <param name="stacks">적용 스택 수</param>
        /// <param name="sourceUnitId">부여자 유닛 ID</param>
        public void ApplyStatus(BattleUnit target, string statusEffectId, int stacks, string sourceUnitId)
        {
            StatusEffectData effectData = _dataManager.GetStatusEffect(statusEffectId);

            if (effectData == null)
            {
                Debug.LogWarning($"[StatusEffectManager] 상태이상 ID '{statusEffectId}'을(를) 찾을 수 없습니다.");
                return;
            }

            ActiveStatusEffect existing = FindStatus(target, statusEffectId);

            if (existing != null)
            {
                if (effectData.isStackable)
                {
                    existing.AddStacks(stacks);
                }

                existing.RefreshDuration();
                OnStatusApplied?.Invoke(target, existing);
            }
            else
            {
                var newEffect = new ActiveStatusEffect(effectData, stacks, sourceUnitId);
                target.StatusEffects.Add(newEffect);
                OnStatusApplied?.Invoke(target, newEffect);
            }
        }

        /// <summary>
        /// 유닛에서 특정 상태이상을 제거한다.
        /// </summary>
        /// <param name="target">대상 유닛</param>
        /// <param name="statusEffectId">제거할 상태이상 ID</param>
        public void RemoveStatus(BattleUnit target, string statusEffectId)
        {
            for (int i = target.StatusEffects.Count - 1; i >= 0; i--)
            {
                if (target.StatusEffects[i].BaseData.id == statusEffectId)
                {
                    ActiveStatusEffect removed = target.StatusEffects[i];
                    target.StatusEffects.RemoveAt(i);
                    OnStatusRemoved?.Invoke(target, removed);
                    return;
                }
            }
        }

        /// <summary>
        /// 유닛의 모든 상태이상을 제거한다.
        /// </summary>
        /// <param name="target">대상 유닛</param>
        public void RemoveAllStatuses(BattleUnit target)
        {
            for (int i = target.StatusEffects.Count - 1; i >= 0; i--)
            {
                ActiveStatusEffect removed = target.StatusEffects[i];
                target.StatusEffects.RemoveAt(i);
                OnStatusRemoved?.Invoke(target, removed);
            }
        }

        #endregion

        #region Public Methods — 트리거 타이밍별 실행

        /// <summary>
        /// 턴 시작 시 TriggerTiming.TurnStart 상태이상을 처리한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ProcessTurnStart(BattleState state)
        {
            ProcessTimingForAllUnits(state, TriggerTiming.TurnStart);
        }

        /// <summary>
        /// 턴 종료 시 TriggerTiming.TurnEnd 상태이상을 처리하고,
        /// 모든 상태이상의 duration을 1 감소시킨 뒤 만료된 것을 제거한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        public void ProcessTurnEnd(BattleState state)
        {
            ProcessTimingForAllUnits(state, TriggerTiming.TurnEnd);
            TickAllDurations(state);
        }

        #endregion

        #region Public Methods — 보정값 조회

        /// <summary>
        /// 유닛의 ModifyDamage 상태이상에 의한 대미지 곱 보정을 반환한다.
        /// Multiplicative 효과를 모두 곱산한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="timing">트리거 타이밍 (OnAttack 또는 OnDamage)</param>
        /// <returns>대미지 곱 보정 (기본 1.0)</returns>
        public float GetDamageModifier(BattleUnit unit, TriggerTiming timing)
        {
            float multiplier = 1.0f;

            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = unit.StatusEffects[i];
                StatusEffectData data = effect.BaseData;

                if (data.effectType == StatusEffectType.ModifyDamage &&
                    data.triggerTiming == timing &&
                    data.modifierType == ModifierType.Multiplicative)
                {
                    multiplier *= data.value * effect.CurrentStacks;
                }
            }

            return multiplier;
        }

        /// <summary>
        /// 유닛의 ModifyDamage 상태이상에 의한 대미지 가산 보정을 반환한다.
        /// Additive 효과를 모두 합산한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="timing">트리거 타이밍</param>
        /// <returns>대미지 가산 보정 (기본 0)</returns>
        public float GetAdditiveModifier(BattleUnit unit, TriggerTiming timing)
        {
            float additive = 0f;

            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = unit.StatusEffects[i];
                StatusEffectData data = effect.BaseData;

                if (data.effectType == StatusEffectType.ModifyDamage &&
                    data.triggerTiming == timing &&
                    data.modifierType == ModifierType.Additive)
                {
                    additive += data.value * effect.CurrentStacks;
                }
            }

            return additive;
        }

        /// <summary>
        /// 피격 유닛의 ReduceDamage 상태이상에 의한 대미지 감소 곱 보정을 반환한다.
        /// </summary>
        /// <param name="unit">피격 유닛</param>
        /// <returns>대미지 감소 곱 보정 (기본 1.0, 감소 시 1.0 미만)</returns>
        public float GetDamageReductionModifier(BattleUnit unit)
        {
            float multiplier = 1.0f;

            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = unit.StatusEffects[i];
                StatusEffectData data = effect.BaseData;

                if (data.effectType == StatusEffectType.ReduceDamage)
                {
                    if (data.modifierType == ModifierType.Multiplicative)
                    {
                        multiplier *= data.value;
                    }
                    else
                    {
                        multiplier -= data.value * effect.CurrentStacks;
                    }
                }
            }

            return Mathf.Max(multiplier, 0f);
        }

        /// <summary>
        /// 유닛의 ModifyBlock 상태이상에 의한 방어도 곱 보정을 반환한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <returns>방어도 곱 보정 (기본 1.0)</returns>
        public float GetBlockModifier(BattleUnit unit)
        {
            float multiplier = 1.0f;

            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = unit.StatusEffects[i];
                StatusEffectData data = effect.BaseData;

                if (data.effectType == StatusEffectType.ModifyBlock)
                {
                    if (data.modifierType == ModifierType.Multiplicative)
                    {
                        multiplier *= data.value * effect.CurrentStacks;
                    }
                }
            }

            return multiplier;
        }

        /// <summary>
        /// 유닛에 Stun 상태이상이 있는지 확인한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <returns>Stun 여부</returns>
        public bool HasStun(BattleUnit unit)
        {
            return unit.HasStun;
        }

        /// <summary>
        /// 소모형 상태이상의 스택을 소모한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="statusEffectId">상태이상 ID</param>
        /// <returns>소모 성공 여부</returns>
        public bool TryExpendStatus(BattleUnit unit, string statusEffectId)
        {
            ActiveStatusEffect effect = FindStatus(unit, statusEffectId);

            if (effect == null)
            {
                return false;
            }

            bool result = effect.TryExpend();

            if (result && effect.CurrentStacks <= 0)
            {
                unit.StatusEffects.Remove(effect);
                OnStatusExpired?.Invoke(unit, effect);
            }

            return result;
        }

        /// <summary>
        /// 유닛의 주기적 효과 (DamageOverTime, HealOverTime)를 실행한다.
        /// </summary>
        /// <param name="unit">대상 유닛</param>
        /// <param name="timing">트리거 타이밍</param>
        public void ExecutePeriodicEffects(BattleUnit unit, TriggerTiming timing)
        {
            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = unit.StatusEffects[i];
                StatusEffectData data = effect.BaseData;

                if (data.triggerTiming != timing)
                {
                    continue;
                }

                if (data.isExpendable && !effect.TryExpend())
                {
                    continue;
                }

                int effectValue = Mathf.RoundToInt(data.value * effect.CurrentStacks);

                switch (data.effectType)
                {
                    case StatusEffectType.DamageOverTime:
                        unit.TakeDamage(effectValue);
                        OnStatusTriggered?.Invoke(unit, effect, effectValue);
                        break;

                    case StatusEffectType.HealOverTime:
                        unit.Heal(effectValue);
                        OnStatusTriggered?.Invoke(unit, effect, effectValue);
                        break;
                }
            }
        }

        #endregion

        #region Private Methods

        private ActiveStatusEffect FindStatus(BattleUnit unit, string statusEffectId)
        {
            for (int i = 0; i < unit.StatusEffects.Count; i++)
            {
                if (unit.StatusEffects[i].BaseData.id == statusEffectId)
                {
                    return unit.StatusEffects[i];
                }
            }

            return null;
        }

        private void ProcessTimingForAllUnits(BattleState state, TriggerTiming timing)
        {
            var allUnits = state.AllUnits;

            for (int i = 0; i < allUnits.Count; i++)
            {
                BattleUnit unit = allUnits[i];

                if (!unit.IsAlive)
                {
                    continue;
                }

                ExecutePeriodicEffects(unit, timing);
            }
        }

        private void TickAllDurations(BattleState state)
        {
            var allUnits = state.AllUnits;

            for (int i = 0; i < allUnits.Count; i++)
            {
                BattleUnit unit = allUnits[i];

                for (int j = unit.StatusEffects.Count - 1; j >= 0; j--)
                {
                    bool expired = unit.StatusEffects[j].TickDuration();

                    if (expired)
                    {
                        ActiveStatusEffect expiredEffect = unit.StatusEffects[j];
                        unit.StatusEffects.RemoveAt(j);
                        OnStatusExpired?.Invoke(unit, expiredEffect);
                    }
                }
            }
        }

        #endregion
    }
}
