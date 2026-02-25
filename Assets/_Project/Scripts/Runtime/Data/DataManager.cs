using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 모든 마스터 데이터 ScriptableObject에 대한 중앙 집중 접근을 제공하는 런타임 매니저.
    /// 게임 부트 시 ServiceLocator에 등록하여 사용한다.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Unit / Card / Skill")]
        [SerializeField] private UnitTableSO _unitTable;
        [SerializeField] private CardTableSO _cardTable;
        [SerializeField] private CardEffectTableSO _cardEffectTable;
        [SerializeField] private SkillTableSO _skillTable;

        [Header("Item / StatusEffect")]
        [SerializeField] private ItemTableSO _itemTable;
        [SerializeField] private StatusEffectTableSO _statusEffectTable;

        [Header("Stage / Event / Area")]
        [SerializeField] private EventTableSO _eventTable;
        [SerializeField] private AreaTableSO _areaTable;
        [SerializeField] private EnemyCombinationTableSO _enemyCombinationTable;

        [Header("AI")]
        [SerializeField] private AIPatternTableSO _aiPatternTable;
        [SerializeField] private AIPatternRuleTableSO _aiPatternRuleTable;
        [SerializeField] private AIConditionTableSO _aiConditionTable;

        [Header("Reward / Element")]
        [SerializeField] private RewardTableSO _rewardTable;
        [SerializeField] private ElementAffinityTableSO _elementAffinityTable;

        [Header("Battle")]
        [SerializeField] private BattleActionTableSO _battleActionTable;
        [SerializeField] private BattleTimelineTableSO _battleTimelineTable;

        [Header("Campaign / DropRate")]
        [SerializeField] private CampaignTableSO _campaignTable;
        [SerializeField] private CampaignGoalGroupTableSO _campaignGoalGroupTable;
        [SerializeField] private DropRateTableSO _dropRateTable;

        [Header("Settings")]
        [SerializeField] private GameSettings _gameSettings;

        #endregion

        #region Public Properties

        /// <summary>
        /// 유닛 마스터 데이터 테이블.
        /// </summary>
        public UnitTableSO Units => _unitTable;

        /// <summary>
        /// 카드 마스터 데이터 테이블.
        /// </summary>
        public CardTableSO Cards => _cardTable;

        /// <summary>
        /// 카드 효과 마스터 데이터 테이블.
        /// </summary>
        public CardEffectTableSO CardEffects => _cardEffectTable;

        /// <summary>
        /// 스킬 마스터 데이터 테이블.
        /// </summary>
        public SkillTableSO Skills => _skillTable;

        /// <summary>
        /// 아이템 마스터 데이터 테이블.
        /// </summary>
        public ItemTableSO Items => _itemTable;

        /// <summary>
        /// 상태 효과 마스터 데이터 테이블.
        /// </summary>
        public StatusEffectTableSO StatusEffects => _statusEffectTable;

        /// <summary>
        /// 이벤트 마스터 데이터 테이블.
        /// </summary>
        public EventTableSO Events => _eventTable;

        /// <summary>
        /// 구역 마스터 데이터 테이블.
        /// </summary>
        public AreaTableSO Areas => _areaTable;

        /// <summary>
        /// 적 조합 마스터 데이터 테이블.
        /// </summary>
        public EnemyCombinationTableSO EnemyCombinations => _enemyCombinationTable;

        /// <summary>
        /// AI 패턴 마스터 데이터 테이블.
        /// </summary>
        public AIPatternTableSO AIPatterns => _aiPatternTable;

        /// <summary>
        /// AI 패턴 룰 마스터 데이터 테이블.
        /// </summary>
        public AIPatternRuleTableSO AIPatternRules => _aiPatternRuleTable;

        /// <summary>
        /// AI 컨디션 마스터 데이터 테이블.
        /// </summary>
        public AIConditionTableSO AIConditions => _aiConditionTable;

        /// <summary>
        /// 보상 마스터 데이터 테이블.
        /// </summary>
        public RewardTableSO Rewards => _rewardTable;

        /// <summary>
        /// 속성 상성 보정치 데이터 테이블.
        /// </summary>
        public ElementAffinityTableSO ElementAffinities => _elementAffinityTable;

        /// <summary>
        /// 전투 연출 행동 마스터 데이터 테이블.
        /// </summary>
        public BattleActionTableSO BattleActions => _battleActionTable;

        /// <summary>
        /// 전투 타임라인 마스터 데이터 테이블.
        /// </summary>
        public BattleTimelineTableSO BattleTimelines => _battleTimelineTable;

        /// <summary>
        /// 캠페인 마스터 데이터 테이블.
        /// </summary>
        public CampaignTableSO Campaigns => _campaignTable;

        /// <summary>
        /// 캠페인 목표 그룹 마스터 데이터 테이블.
        /// </summary>
        public CampaignGoalGroupTableSO CampaignGoalGroups => _campaignGoalGroupTable;

        /// <summary>
        /// 레어도별 기본 드랍율 데이터 테이블.
        /// </summary>
        public DropRateTableSO DropRates => _dropRateTable;

        /// <summary>
        /// 게임 전역 설정.
        /// </summary>
        public GameSettings Settings => _gameSettings;

        #endregion

        #region Public Methods

        /// <summary>
        /// ID로 유닛 데이터를 조회한다.
        /// </summary>
        public UnitData GetUnit(string id)
        {
            return _unitTable.GetById(id);
        }

        /// <summary>
        /// ID로 카드 데이터를 조회한다.
        /// </summary>
        public CardData GetCard(string id)
        {
            return _cardTable.GetById(id);
        }

        /// <summary>
        /// ID로 카드 효과 데이터를 조회한다.
        /// </summary>
        public CardEffectData GetCardEffect(string id)
        {
            return _cardEffectTable.GetById(id);
        }

        /// <summary>
        /// ID로 스킬 데이터를 조회한다.
        /// </summary>
        public SkillData GetSkill(string id)
        {
            return _skillTable.GetById(id);
        }

        /// <summary>
        /// ID로 아이템 데이터를 조회한다.
        /// </summary>
        public ItemData GetItem(string id)
        {
            return _itemTable.GetById(id);
        }

        /// <summary>
        /// ID로 상태 효과 데이터를 조회한다.
        /// </summary>
        public StatusEffectData GetStatusEffect(string id)
        {
            return _statusEffectTable.GetById(id);
        }

        /// <summary>
        /// ID로 이벤트 데이터를 조회한다.
        /// </summary>
        public EventData GetEvent(string id)
        {
            return _eventTable.GetById(id);
        }

        /// <summary>
        /// ID로 구역 데이터를 조회한다.
        /// </summary>
        public AreaData GetArea(string id)
        {
            return _areaTable.GetById(id);
        }

        /// <summary>
        /// ID로 적 조합 데이터를 조회한다.
        /// </summary>
        public EnemyCombinationData GetEnemyCombination(string id)
        {
            return _enemyCombinationTable.GetById(id);
        }

        /// <summary>
        /// ID로 AI 패턴 데이터를 조회한다.
        /// </summary>
        public AIPatternData GetAIPattern(string id)
        {
            return _aiPatternTable.GetById(id);
        }

        /// <summary>
        /// ID로 캠페인 데이터를 조회한다.
        /// </summary>
        public CampaignData GetCampaign(string id)
        {
            return _campaignTable.GetById(id);
        }

        /// <summary>
        /// 공격 속성과 피격 속성에 해당하는 대미지 보정 배율을 반환한다.
        /// 정의되지 않은 조합은 1.0f를 반환한다.
        /// </summary>
        public float GetElementAffinity(ElementType attack, ElementType target)
        {
            return _elementAffinityTable.GetModifier(attack, target);
        }

        #endregion
    }
}
