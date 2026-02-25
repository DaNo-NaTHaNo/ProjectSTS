using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 중 유닛의 런타임 상태를 관리한다.
    /// UnitData(마스터)와 OwnedUnitData(편성)를 기반으로 생성되며,
    /// 현재 HP, 방어도, 상태이상, 스킬 사용 추적 등을 담당한다.
    /// </summary>
    public class BattleUnit
    {
        #region Private Fields

        private int _currentHP;
        private int _maxHP;
        private int _block;

        #endregion

        #region Public Properties

        /// <summary>
        /// 유닛 테이블 상의 ID.
        /// </summary>
        public string UnitId { get; }

        /// <summary>
        /// 유닛 마스터 데이터 참조.
        /// </summary>
        public UnitData BaseData { get; }

        /// <summary>
        /// 유닛 소속 타입 (아군/적/NPC).
        /// </summary>
        public UnitType UnitType { get; }

        /// <summary>
        /// 유닛의 속성.
        /// </summary>
        public ElementType Element { get; }

        /// <summary>
        /// 전투 내 위치. 아군은 파티 포지션(1-3), 적은 배치 위치(1-5).
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 최대 HP (아이템 보정 후 최종값).
        /// </summary>
        public int MaxHP
        {
            get => _maxHP;
            set => _maxHP = Mathf.Max(value, 1);
        }

        /// <summary>
        /// 현재 HP.
        /// </summary>
        public int CurrentHP
        {
            get => _currentHP;
            set => _currentHP = Mathf.Clamp(value, 0, _maxHP);
        }

        /// <summary>
        /// 파티 에너지 합산에 기여하는 에너지 값.
        /// </summary>
        public int MaxEnergy { get; }

        /// <summary>
        /// 현재 방어도. 턴 시작 시 리셋된다.
        /// </summary>
        public int Block
        {
            get => _block;
            set => _block = Mathf.Max(value, 0);
        }

        /// <summary>
        /// 생존 여부.
        /// </summary>
        public bool IsAlive => _currentHP > 0;

        /// <summary>
        /// 장착 스킬 ID. 없으면 null 또는 빈 문자열.
        /// </summary>
        public string SkillId { get; }

        /// <summary>
        /// 장착 아이템 ID 목록 (최대 2개).
        /// </summary>
        public List<string> EquippedItemIds { get; }

        /// <summary>
        /// 초기 덱 카드 ID 목록.
        /// </summary>
        public List<string> DeckCardIds { get; }

        /// <summary>
        /// 현재 적용 중인 상태이상 목록.
        /// </summary>
        public List<ActiveStatusEffect> StatusEffects { get; }

        /// <summary>
        /// Stun 상태이상 보유 여부.
        /// </summary>
        public bool HasStun
        {
            get
            {
                for (int i = 0; i < StatusEffects.Count; i++)
                {
                    if (StatusEffects[i].BaseData.effectType == StatusEffectType.Stun)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 스킬 쿨다운 남은 턴 수.
        /// </summary>
        public int SkillCooldownRemaining { get; set; }

        /// <summary>
        /// 현재 턴에서의 스킬 사용 횟수.
        /// </summary>
        public int SkillUsedThisTurn { get; set; }

        /// <summary>
        /// 전투 전체에서의 스킬 사용 횟수.
        /// </summary>
        public int SkillUsedThisBattle { get; set; }

        /// <summary>
        /// 현재 AI 패턴 ID (적 유닛 전용). 런타임에 변경 가능.
        /// </summary>
        public string CurrentAIPatternId { get; set; }

        #endregion

        #region Constructor

        private BattleUnit(UnitData baseData, int position, string skillId,
            List<string> equippedItemIds, List<string> deckCardIds, string aiPatternId)
        {
            UnitId = baseData.id;
            BaseData = baseData;
            UnitType = baseData.unitType;
            Element = baseData.element;
            Position = position;

            _maxHP = baseData.maxHP;
            _currentHP = baseData.maxHP;
            MaxEnergy = baseData.maxEnergy;
            _block = 0;

            SkillId = skillId;
            EquippedItemIds = equippedItemIds ?? new List<string>(2);
            DeckCardIds = deckCardIds ?? new List<string>(6);
            StatusEffects = new List<ActiveStatusEffect>(8);

            SkillCooldownRemaining = 0;
            SkillUsedThisTurn = 0;
            SkillUsedThisBattle = 0;

            CurrentAIPatternId = aiPatternId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 대미지를 받는다. HP가 0 이하로 내려가지 않는다.
        /// </summary>
        /// <param name="amount">대미지 양 (양수)</param>
        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentHP = Mathf.Max(_currentHP - amount, 0);
        }

        /// <summary>
        /// HP를 회복한다. MaxHP를 초과하지 않는다.
        /// </summary>
        /// <param name="amount">회복량 (양수)</param>
        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentHP = Mathf.Min(_currentHP + amount, _maxHP);
        }

        /// <summary>
        /// 방어도를 추가한다.
        /// </summary>
        /// <param name="amount">추가할 방어도 (양수)</param>
        public void AddBlock(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _block += amount;
        }

        /// <summary>
        /// 턴 시작 시 방어도를 0으로 리셋한다.
        /// </summary>
        public void ResetBlockForTurn()
        {
            _block = 0;
        }

        /// <summary>
        /// 턴 시작 시 PerTurn 스킬 사용 횟수를 리셋한다.
        /// </summary>
        public void ResetSkillUsageForTurn()
        {
            SkillUsedThisTurn = 0;

            if (SkillCooldownRemaining > 0)
            {
                SkillCooldownRemaining--;
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// 아군 BattleUnit을 생성한다.
        /// </summary>
        /// <param name="data">유닛 마스터 데이터</param>
        /// <param name="owned">보유 유닛 편성 데이터</param>
        /// <param name="position">파티 포지션 (1-3)</param>
        /// <returns>생성된 BattleUnit</returns>
        public static BattleUnit CreateAlly(UnitData data, OwnedUnitData owned, int position)
        {
            string skillId = !string.IsNullOrEmpty(owned.editedSkill) ? owned.editedSkill : data.initialSkillId;

            var equippedItems = new List<string>(2);
            if (!string.IsNullOrEmpty(owned.equipItem1))
            {
                equippedItems.Add(owned.equipItem1);
            }
            if (!string.IsNullOrEmpty(owned.equipItem2))
            {
                equippedItems.Add(owned.equipItem2);
            }

            var deckCards = new List<string>(6);
            string deckSource = !string.IsNullOrEmpty(owned.editedDeck) ? owned.editedDeck : data.initialDeckIds;
            if (!string.IsNullOrEmpty(deckSource))
            {
                string[] cardIds = deckSource.Split(';');
                for (int i = 0; i < cardIds.Length; i++)
                {
                    string trimmed = cardIds[i].Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        deckCards.Add(trimmed);
                    }
                }
            }

            return new BattleUnit(data, position, skillId, equippedItems, deckCards, null);
        }

        /// <summary>
        /// 적 BattleUnit을 생성한다.
        /// </summary>
        /// <param name="data">유닛 마스터 데이터</param>
        /// <param name="position">배치 위치 (1-5)</param>
        /// <returns>생성된 BattleUnit</returns>
        public static BattleUnit CreateEnemy(UnitData data, int position)
        {
            string skillId = data.initialSkillId;

            var deckCards = new List<string>(6);
            if (!string.IsNullOrEmpty(data.initialDeckIds))
            {
                string[] cardIds = data.initialDeckIds.Split(';');
                for (int i = 0; i < cardIds.Length; i++)
                {
                    string trimmed = cardIds[i].Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        deckCards.Add(trimmed);
                    }
                }
            }

            return new BattleUnit(data, position, skillId, new List<string>(0), deckCards, data.aiPatternId);
        }

        #endregion
    }
}
