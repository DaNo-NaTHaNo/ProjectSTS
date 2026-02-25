using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 순수 대미지 계산 유틸리티.
    /// 속성 상성 배율, 상태이상 보정, 방어도 적용, 반사 대미지 등을 계산한다.
    /// 상태를 갖지 않는 static 클래스이다.
    /// </summary>
    public static class DamageCalculator
    {
        #region Public Methods

        /// <summary>
        /// 대미지를 계산한다. 카드/스킬 효과 → 속성 상성 → 상태이상 보정 순서로 적용한다.
        /// </summary>
        /// <param name="baseDamage">효과의 기본 대미지 값</param>
        /// <param name="card">사용한 카드 (스킬에서 호출 시 null 가능)</param>
        /// <param name="attackElement">공격 속성</param>
        /// <param name="attacker">공격자 유닛</param>
        /// <param name="target">피격자 유닛</param>
        /// <param name="statusEffectManager">상태이상 매니저 (보정값 조회용)</param>
        /// <param name="dataManager">데이터 매니저 (속성 상성 조회용)</param>
        /// <returns>최종 대미지 (최소 0)</returns>
        public static int CalculateDamage(
            float baseDamage,
            RuntimeCard card,
            ElementType attackElement,
            BattleUnit attacker,
            BattleUnit target,
            StatusEffectManager statusEffectManager,
            DataManager dataManager)
        {
            float damage = baseDamage;

            if (card != null)
            {
                damage += card.DamageModifier;
            }

            float elementModifier = dataManager.GetElementAffinity(attackElement, target.Element);
            damage *= elementModifier;

            if (statusEffectManager != null)
            {
                float attackModifier = statusEffectManager.GetDamageModifier(attacker, TriggerTiming.OnAttack);
                damage += statusEffectManager.GetAdditiveModifier(attacker, TriggerTiming.OnAttack);
                damage *= attackModifier;

                float defenseModifier = statusEffectManager.GetDamageReductionModifier(target);
                damage *= defenseModifier;
            }

            return Mathf.Max(Mathf.RoundToInt(damage), 0);
        }

        /// <summary>
        /// 방어도를 적용한 후 실제 HP 피해와 남은 방어도를 반환한다.
        /// </summary>
        /// <param name="damage">계산된 대미지</param>
        /// <param name="block">현재 방어도</param>
        /// <returns>실제 HP 피해와 남은 방어도</returns>
        public static (int actualDamage, int remainingBlock) ApplyBlock(int damage, int block)
        {
            if (block >= damage)
            {
                return (0, block - damage);
            }

            int actualDamage = damage - block;
            return (actualDamage, 0);
        }

        /// <summary>
        /// 피격 유닛의 반사 대미지 총량을 계산한다.
        /// ReflectDamage 상태이상의 value를 합산한다.
        /// </summary>
        /// <param name="target">피격 유닛</param>
        /// <returns>반사 대미지 총량</returns>
        public static int CalculateReflectDamage(BattleUnit target)
        {
            float totalReflect = 0f;

            for (int i = 0; i < target.StatusEffects.Count; i++)
            {
                ActiveStatusEffect effect = target.StatusEffects[i];

                if (effect.BaseData.effectType == StatusEffectType.ReflectDamage)
                {
                    totalReflect += effect.BaseData.value * effect.CurrentStacks;
                }
            }

            return Mathf.Max(Mathf.RoundToInt(totalReflect), 0);
        }

        /// <summary>
        /// 속성 상성 배율만 조회한다 (UI 표시용).
        /// </summary>
        /// <param name="attack">공격 속성</param>
        /// <param name="target">피격 속성</param>
        /// <param name="dataManager">데이터 매니저</param>
        /// <returns>대미지 배율 (1.0이 기본)</returns>
        public static float GetElementModifier(ElementType attack, ElementType target, DataManager dataManager)
        {
            return dataManager.GetElementAffinity(attack, target);
        }

        /// <summary>
        /// 방어도 효과를 계산한다. 카드 보정을 적용한다.
        /// </summary>
        /// <param name="baseBlock">효과의 기본 방어도 값</param>
        /// <param name="card">사용한 카드 (null 가능)</param>
        /// <param name="unit">방어도를 얻는 유닛</param>
        /// <param name="statusEffectManager">상태이상 매니저</param>
        /// <returns>최종 방어도 (최소 0)</returns>
        public static int CalculateBlock(
            float baseBlock,
            RuntimeCard card,
            BattleUnit unit,
            StatusEffectManager statusEffectManager)
        {
            float block = baseBlock;

            if (card != null)
            {
                block += card.BlockModifier;
            }

            if (statusEffectManager != null)
            {
                float modifier = statusEffectManager.GetBlockModifier(unit);
                block *= modifier;
            }

            return Mathf.Max(Mathf.RoundToInt(block), 0);
        }

        #endregion
    }
}
