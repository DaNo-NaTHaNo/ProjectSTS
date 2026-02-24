// ========================================
// CSV Row 클래스 정의
// CSV 파싱 결과를 담는 임시 데이터 구조
// ========================================

/// <summary>
/// Cards.csv의 행 데이터
/// </summary>
public class CardRow
{
    public string id;
    public string cardName;
    public string description;
    public string artworkPath;
    public int cost;
    public string cardType;
    public string rarity;
    public string targetType;
    public string targetSelectionRule;
    public int targetCount;
    public string targetFilter;
    public string element;
    public string keywords;
    public bool canUpgrade;
    public string upgradedCardId;
}

/// <summary>
/// CardEffects.csv의 행 데이터
/// </summary>
public class CardEffectRow
{
    public string cardId;
    public string effectType;
    public int value;
    public string statusEffectId;
    public int duration;
    public string modificationType;
    public string modDuration;
    public string cardTargetSelection;
    public string targetCardType;
}

/// <summary>
/// StatusEffects.csv의 행 데이터
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
    public float value;
    public string modifierType;
    public bool isStackable;
    public int maxStacks;
}

/// <summary>
/// Units.csv의 행 데이터
/// </summary>
public class UnitRow
{
    public string id;
    public string unitName;
    public string unitType;
    public string portraitPath;
    public int maxHp;
    public int maxEnergy;
    public string initialDeckIds;      // 세미콜론(;) 구분
    public string aiPatternId;
    public string element;
}

/// <summary>
/// AIPatterns.csv의 행 데이터
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
/// AIPatternRules.csv의 행 데이터
/// </summary>
public class AIPatternRuleRow
{
    public string aiPatternId;
    public string ruleId;
    public int priority;
    public string actionType;
    public string cardId;
    public string targetSelection;
}

/// <summary>
/// AIConditions.csv의 행 데이터
/// </summary>
public class AIConditionRow
{
    public string ruleId;
    public string conditionType;
    public string comparisonOperator;
    public float value;
    public int divisor;         // TurnMod 전용
    public int remainder;       // TurnMod 전용
}

/// <summary>
/// CombatScenarios.csv의 행 데이터 (손패 설정 제거)
/// </summary>
public class CombatScenarioRow
{
    public string id;
    public string scenarioName;
    public string description;
    public string playerUnitId;
    public string enemyUnitIds;     // 세미콜론(;) 구분
}