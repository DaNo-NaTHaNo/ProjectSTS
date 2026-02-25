// ========================================
// CSV Row Ŭ���� ����
// CSV �Ľ� ����� ��� �ӽ� ������ ����
// ========================================

/// <summary>
/// Cards.csv�� �� ������
/// </summary>
public class CardRow
{
    public string id;
    public string cardName;
    public string description;
    public string artworkPath;
    public int cost;
    public string cardEffectId;
    public string cardType;
    public string rarity;
    public string targetType;
    public string targetSelectionRule;
    public int targetCount;
    public string targetFilter;
    public string element;
    public bool isDisposable;
}

/// <summary>
/// CardEffects.csv�� �� ������
/// </summary>
public class CardEffectRow
{
    public string id;
    public string effectType;
    public int value;
    public string statusEffectId;
    public int duration;
    public string modificationType;
    public string modDuration;
    public string cardTargetSelection;
    public string targetCardType;
    public string addCardId;
}

/// <summary>
/// StatusEffects.csv�� �� ������
/// </summary>
public class StatusEffectRow
{
    public string id;
    public string effectName;
    public string description;
    public string iconPath;
    public string statusType;
    public string triggerTiming;
    public string effectType;
    public string effectElement;
    public float value;
    public string modifierType;
    public bool isStackable;
    public int maxStacks;
    public bool isExpendable;
    public int expendCount;
    public int duration;
}

/// <summary>
/// Units.csv�� �� ������
/// </summary>
public class UnitRow
{
    public string id;
    public string unitName;
    public string unitType;
    public string element;
    public string portraitPath;
    public int maxHp;
    public int maxEnergy;
    public string initialDeckIds;      // 세미콜론(;) 구분
    public string initialSkillId;
    public string aiPatternId;
}

/// <summary>
/// AIPatterns.csv�� �� ������
/// </summary>
public class AIPatternRow
{
    public string id;
    public string patternName;
    public string description;
    public string defaultActionType;
    public string defaultCardId;
    public string defaultTargetSelection;
}

/// <summary>
/// AIPatternRules.csv�� �� ������
/// </summary>
public class AIPatternRuleRow
{
    public string aiPatternId;
    public string ruleId;
    public int priority;
    public string actionType;
    public string cardId;
    public string targetSelection;
    public string speechLine;
    public string cutInEffect;
    public bool zoomIn;
}

/// <summary>
/// AIConditions.csv�� �� ������
/// </summary>
public class AIConditionRow
{
    public string ruleId;
    public string conditionType;
    public string comparisonOperator;
    public string value;
    public int divisor;         // TurnMod 전용
    public int remainder;       // TurnMod 전용
}

/// <summary>
/// CombatScenarios.csv�� �� ������ (���� ���� ����)
/// </summary>
public class CombatScenarioRow
{
    public string id;
    public string scenarioName;
    public string description;
    public string playerUnitId;
    public string enemyUnitIds;     // 세미콜론(;) 구분
}

/// <summary>
/// Skills.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class SkillRow
{
    public string id;
    public string skillName;
    public string description;
    public string unitId;
    public string artworkPath;
    public string rarity;
    public string element;
    public string triggerTarget;
    public string triggerStatus;
    public string comparisonOperator;
    public string triggerValue;
    public string triggerElement;
    public string cardEffectId;
    public string targetType;
    public string targetFilter;
    public string targetSelectionRule;
    public int targetCount;
    public string limitType;
    public int limitValue;
}

/// <summary>
/// Items.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class ItemRow
{
    public string id;
    public string itemName;
    public string description;
    public string artworkPath;
    public string rarity;
    public string itemType;
    public string targetUnit;
    public string targetStatus;
    public string modifyValue;
    public bool isDisposable;
    public string disposeTrigger;
    public float disposePercentage;
    public int stackCount;
}

/// <summary>
/// Events.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class EventRow
{
    public string areaId;
    public string id;
    public string eventType;
    public string eventValue;
    public string spawnTrigger;
    public string comparisonOperator;
    public string spawnTriggerValue;
    public int minLevel;
    public int maxLevel;
    public string rarity;
    public string rewardId;
    public int rewardMinCount;
    public int rewardMaxCount;
}

/// <summary>
/// Areas.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class AreaRow
{
    public string id;
    public string name;
    public string description;
    public int areaLevelMin;
    public int areaLevelMax;
    public string areaCardinalPoint;
    public string logoImagePath;
    public string floorImagePath;
    public string skyboxPath;
    public string cellVisualNovelPath;
    public string cellEncountPath;
    public string cellBattleNormalPath;
    public string cellBattleElitePath;
    public string cellBattleBossPath;
    public string cellBattleEventPath;
}

/// <summary>
/// EnemyCombinations.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class EnemyCombinationRow
{
    public string id;
    public string name;
    public string description;
    public int waveCount;
    public string enemyUnit1;
    public string enemyUnit2;
    public string enemyUnit3;
    public string enemyUnit4;
    public string enemyUnit5;
}

/// <summary>
/// RewardTable.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class RewardTableRow
{
    public string id;
    public string itemId;
    public string rarity;
    public float dropRate;
}

/// <summary>
/// ElementAffinity.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class ElementAffinityRow
{
    public string attackElement;
    public string targetElement;
    public float modValue;
}

/// <summary>
/// BattleActions.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class BattleActionRow
{
    public string groupId;
    public int sequence;
    public string actionType;
    public string actionValue;
    public string targetUnit;
    public bool waitNext;
}

/// <summary>
/// BattleTimelines.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class BattleTimelineRow
{
    public string id;
    public string eventId;
    public string triggerTarget;
    public string triggerType;
    public string triggerValue;
    public int priority;
    public bool isRepeatable;
    public string actionGroupId;
}

/// <summary>
/// Campaigns.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class CampaignRow
{
    public string id;
    public string name;
    public string description;
    public string artworkPath;
    public string unlockType;
    public string unlockId;
    public string groupId;
    public string rewards;
    public bool isCompleted;
    public string afterComplete;
}

/// <summary>
/// CampaignGoalGroups.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class CampaignGoalGroupRow
{
    public string groupId;
    public int sequence;
    public string name;
    public string description;
    public bool isEssential;
    public string triggerType;
    public string triggerValue;
    public string additionalRewards;
    public bool isClearTrigger;
    public bool isCompleted;
}

/// <summary>
/// DropRates.csv의 행 데이터를 나타내는 클래스
/// </summary>
public class DropRateRow
{
    public string category;
    public string rarity;
    public float dropValue;
}

/// <summary>
/// OwnedUnits.csv의 행 데이터를 나타내는 클래스 (플레이어 보유 유닛 데이터)
/// </summary>
public class OwnedUnitRow
{
    public string unitId;
    public string cardElement;
    public string editedDeck;
    public string editedSkill;
    public string equipItem1;
    public string equipItem2;
    public int partyPosition;
}

/// <summary>
/// InventoryItems.csv의 행 데이터를 나타내는 클래스 (인벤토리 아이템 데이터)
/// </summary>
public class InventoryItemRow
{
    public string category;
    public string productId;
    public string productName;
    public string description;
    public string rarity;
    public int ownStack;
    public int useStack;
    public string cardElement;
    public string cardType;
    public int cardCost;
    public string itemType;
    public string targetStatus;
    public bool isDisposable;
}

/// <summary>
/// InGameBagItems.csv의 행 데이터를 나타내는 클래스 (인게임 가방 아이템 데이터)
/// </summary>
public class InGameBagItemRow
{
    public string category;
    public string productId;
    public string productName;
    public string description;
    public string rarity;
    public bool isNewForNow;
}

/// <summary>
/// ExplorationRecords.csv의 행 데이터를 나타내는 클래스 (탐험 기록 데이터)
/// </summary>
public class ExplorationRecordRow
{
    public int countDepart;
    public int countComplete;
    public int countBattleAll;
    public int countVisualNovelAll;
    public int countEncountAll;
    public int countBattleNow;
    public int countVisualNovelNow;
    public int countEncountNow;
    public int countEnemyEliminated;
    public string eliminatedBossId;
    public string visitedAreaId;
    public int countRewardComplete;
}