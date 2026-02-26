#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectStS.Data;
using EventType = ProjectStS.Data.EventType;

namespace ProjectStS.Editor
{
    /// <summary>
    /// CSV 파일을 파싱하여 ScriptableObject 테이블에 임포트하는 에디터 윈도우.
    /// Row 데이터를 런타임 Data 모델로 변환하고, 각 TableSO에 SetEntries()로 주입한다.
    /// </summary>
    public class CsvToSOImporter : EditorWindow
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

        #endregion

        #region Private Fields

        private Vector2 _scrollPosition;
        private string _bulkFolderPath = "Assets/_Project/Data";

        #endregion

        #region Editor Window

        /// <summary>
        /// 메뉴에서 에디터 윈도우를 연다.
        /// </summary>
        [MenuItem("ProjectStS/Data/CSV → SO Importer")]
        public static void ShowWindow()
        {
            GetWindow<CsvToSOImporter>("CSV → SO Importer");
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("CSV → SO 임포터", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 일괄 임포트
            EditorGUILayout.LabelField("일괄 임포트", EditorStyles.boldLabel);
            _bulkFolderPath = EditorGUILayout.TextField("CSV 폴더 경로", _bulkFolderPath);
            if (GUILayout.Button("전체 CSV 일괄 임포트", GUILayout.Height(28)))
            {
                ImportAllFromFolder(_bulkFolderPath);
            }

            EditorGUILayout.Space(8);
            DrawSeparator();

            // Unit / Card / Skill
            EditorGUILayout.LabelField("Unit / Card / Skill", EditorStyles.boldLabel);
            DrawImportRow("Units", ref _unitTable, ImportUnits);
            DrawImportRow("Cards", ref _cardTable, ImportCards);
            DrawImportRow("CardEffects", ref _cardEffectTable, ImportCardEffects);
            DrawImportRow("Skills", ref _skillTable, ImportSkills);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // Item / StatusEffect
            EditorGUILayout.LabelField("Item / StatusEffect", EditorStyles.boldLabel);
            DrawImportRow("Items", ref _itemTable, ImportItems);
            DrawImportRow("StatusEffects", ref _statusEffectTable, ImportStatusEffects);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // Stage / Event / Area
            EditorGUILayout.LabelField("Stage / Event / Area", EditorStyles.boldLabel);
            DrawImportRow("Events", ref _eventTable, ImportEvents);
            DrawImportRow("Areas", ref _areaTable, ImportAreas);
            DrawImportRow("EnemyCombinations", ref _enemyCombinationTable, ImportEnemyCombinations);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // AI
            EditorGUILayout.LabelField("AI", EditorStyles.boldLabel);
            DrawImportRow("AIPatterns", ref _aiPatternTable, ImportAIPatterns);
            DrawImportRow("AIPatternRules", ref _aiPatternRuleTable, ImportAIPatternRules);
            DrawImportRow("AIConditions", ref _aiConditionTable, ImportAIConditions);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // Reward / Element
            EditorGUILayout.LabelField("Reward / Element", EditorStyles.boldLabel);
            DrawImportRow("RewardTable", ref _rewardTable, ImportRewardTable);
            DrawImportRow("ElementAffinity", ref _elementAffinityTable, ImportElementAffinity);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // Battle
            EditorGUILayout.LabelField("Battle", EditorStyles.boldLabel);
            DrawImportRow("BattleActions", ref _battleActionTable, ImportBattleActions);
            DrawImportRow("BattleTimelines", ref _battleTimelineTable, ImportBattleTimelines);

            EditorGUILayout.Space(4);
            DrawSeparator();

            // Campaign / DropRate
            EditorGUILayout.LabelField("Campaign / DropRate", EditorStyles.boldLabel);
            DrawImportRow("Campaigns", ref _campaignTable, ImportCampaigns);
            DrawImportRow("CampaignGoalGroups", ref _campaignGoalGroupTable, ImportCampaignGoalGroups);
            DrawImportRow("DropRates", ref _dropRateTable, ImportDropRates);

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI Helpers

        private delegate void ImportAction(string csvPath);

        /// <summary>
        /// SO 참조 필드 + Import 버튼을 한 줄로 그린다.
        /// </summary>
        private void DrawImportRow<T>(string label, ref T so, ImportAction importAction) where T : Object
        {
            EditorGUILayout.BeginHorizontal();
            so = EditorGUILayout.ObjectField(label, so, typeof(T), false) as T;

            if (GUILayout.Button("Import", GUILayout.Width(60)))
            {
                if (so == null)
                {
                    Debug.LogWarning($"[CsvToSOImporter] {label} SO가 할당되지 않았습니다.");
                }
                else
                {
                    string path = EditorUtility.OpenFilePanel($"{label} CSV 선택", "Assets/_Project/Data", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        importAction(path);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Bulk Import

        /// <summary>
        /// 지정 폴더 내 CSV 파일을 이름 규칙으로 자동 매칭하여 일괄 임포트한다.
        /// 파일명은 "{테이블명}.csv" 형식이어야 한다.
        /// </summary>
        private void ImportAllFromFolder(string folderPath)
        {
            string absolutePath = Path.GetFullPath(folderPath);
            if (!Directory.Exists(absolutePath))
            {
                Debug.LogError($"[CsvToSOImporter] 폴더를 찾을 수 없습니다: {absolutePath}");
                return;
            }

            int importedCount = 0;
            var mapping = new Dictionary<string, System.Action<string>>
            {
                { "Units", ImportUnits },
                { "Cards", ImportCards },
                { "CardEffects", ImportCardEffects },
                { "Skills", ImportSkills },
                { "Items", ImportItems },
                { "StatusEffects", ImportStatusEffects },
                { "Events", ImportEvents },
                { "Areas", ImportAreas },
                { "EnemyCombinations", ImportEnemyCombinations },
                { "AIPatterns", ImportAIPatterns },
                { "AIPatternRules", ImportAIPatternRules },
                { "AIConditions", ImportAIConditions },
                { "RewardTable", ImportRewardTable },
                { "ElementAffinity", ImportElementAffinity },
                { "BattleActions", ImportBattleActions },
                { "BattleTimelines", ImportBattleTimelines },
                { "Campaigns", ImportCampaigns },
                { "CampaignGoalGroups", ImportCampaignGoalGroups },
                { "DropRates", ImportDropRates }
            };

            foreach (var kvp in mapping)
            {
                string csvPath = Path.Combine(absolutePath, kvp.Key + ".csv");
                if (File.Exists(csvPath))
                {
                    kvp.Value(csvPath);
                    importedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CsvToSOImporter] 일괄 임포트 완료: {importedCount}개 테이블");
        }

        #endregion

        #region Individual Import Methods

        private void ImportUnits(string csvPath)
        {
            if (_unitTable == null) { LogMissingSO("UnitTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseUnits(csv);
            var dataList = new List<UnitData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new UnitData
                {
                    id = row.id,
                    unitName = row.unitName,
                    unitType = ParseEnum<UnitType>(row.unitType),
                    element = ParseEnum<ElementType>(row.element),
                    portraitPath = row.portraitPath,
                    maxHP = row.maxHp,
                    maxEnergy = row.maxEnergy,
                    maxAP = row.maxAP,
                    initialDeckIds = row.initialDeckIds,
                    initialSkillId = row.initialSkillId,
                    aiPatternId = row.aiPatternId
                });
            }

            ApplyToSO(_unitTable, dataList, "Units");
        }

        private void ImportCards(string csvPath)
        {
            if (_cardTable == null) { LogMissingSO("CardTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseCards(csv);
            var dataList = new List<CardData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new CardData
                {
                    id = row.id,
                    cardName = row.cardName,
                    description = row.description,
                    artworkPath = row.artworkPath,
                    rarity = ParseEnum<Rarity>(row.rarity),
                    element = ParseEnum<ElementType>(row.element),
                    cost = row.cost,
                    cardEffectId = row.cardEffectId,
                    cardType = ParseEnum<CardType>(row.cardType),
                    targetType = ParseEnum<TargetType>(row.targetType),
                    targetFilter = ParseEnum<TargetFilter>(row.targetFilter),
                    targetSelectionRule = ParseEnum<TargetSelectionRule>(row.targetSelectionRule),
                    targetCount = row.targetCount,
                    isDisposable = row.isDisposable
                });
            }

            ApplyToSO(_cardTable, dataList, "Cards");
        }

        private void ImportCardEffects(string csvPath)
        {
            if (_cardEffectTable == null) { LogMissingSO("CardEffectTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseCardEffects(csv);
            var dataList = new List<CardEffectData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new CardEffectData
                {
                    id = row.id,
                    effectType = ParseEnum<CardEffectType>(row.effectType),
                    value = row.value,
                    statusEffectId = row.statusEffectId,
                    modificationType = ParseEnum<ModificationType>(row.modificationType),
                    modDuration = ParseEnum<ModDuration>(row.modDuration),
                    cardTargetSelection = ParseEnum<CardTargetSelection>(row.cardTargetSelection),
                    targetCardType = ParseEnum<CardType>(row.targetCardType),
                    addCardId = row.addCardId
                });
            }

            ApplyToSO(_cardEffectTable, dataList, "CardEffects");
        }

        private void ImportSkills(string csvPath)
        {
            if (_skillTable == null) { LogMissingSO("SkillTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseSkills(csv);
            var dataList = new List<SkillData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new SkillData
                {
                    id = row.id,
                    skillName = row.skillName,
                    description = row.description,
                    unitId = row.unitId,
                    artworkPath = row.artworkPath,
                    rarity = ParseEnum<Rarity>(row.rarity),
                    element = ParseEnum<ElementType>(row.element),
                    triggerTarget = ParseEnum<SkillTriggerTarget>(row.triggerTarget),
                    triggerStatus = ParseEnum<SkillTriggerStatus>(row.triggerStatus),
                    comparisonOperator = ParseEnum<ComparisonOperator>(row.comparisonOperator),
                    triggerValue = row.triggerValue,
                    triggerElement = ParseEnum<ElementType>(row.triggerElement),
                    cardEffectId = row.cardEffectId,
                    targetType = ParseEnum<TargetType>(row.targetType),
                    targetFilter = ParseEnum<TargetFilter>(row.targetFilter),
                    targetSelectionRule = ParseEnum<TargetSelectionRule>(row.targetSelectionRule),
                    targetCount = row.targetCount,
                    limitType = ParseEnum<SkillLimitType>(row.limitType),
                    limitValue = row.limitValue
                });
            }

            ApplyToSO(_skillTable, dataList, "Skills");
        }

        private void ImportItems(string csvPath)
        {
            if (_itemTable == null) { LogMissingSO("ItemTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseItems(csv);
            var dataList = new List<ItemData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new ItemData
                {
                    id = row.id,
                    itemName = row.itemName,
                    description = row.description,
                    artworkPath = row.artworkPath,
                    rarity = ParseEnum<Rarity>(row.rarity),
                    itemType = ParseEnum<ItemType>(row.itemType),
                    targetUnit = ParseEnum<ItemTargetUnit>(row.targetUnit),
                    targetStatus = ParseEnum<ItemTargetStatus>(row.targetStatus),
                    modifyValue = row.modifyValue,
                    isDisposable = row.isDisposable,
                    disposeTrigger = ParseEnum<DisposeTrigger>(row.disposeTrigger),
                    disposePercentage = row.disposePercentage,
                    stackCount = row.stackCount
                });
            }

            ApplyToSO(_itemTable, dataList, "Items");
        }

        private void ImportStatusEffects(string csvPath)
        {
            if (_statusEffectTable == null) { LogMissingSO("StatusEffectTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseStatusEffects(csv);
            var dataList = new List<StatusEffectData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new StatusEffectData
                {
                    id = row.id,
                    effectName = row.effectName,
                    description = row.description,
                    iconPath = row.iconPath,
                    statusType = ParseEnum<StatusType>(row.statusType),
                    triggerTiming = ParseEnum<TriggerTiming>(row.triggerTiming),
                    effectType = ParseEnum<StatusEffectType>(row.effectType),
                    effectElement = ParseEnum<ElementType>(row.effectElement),
                    value = row.value,
                    modifierType = ParseEnum<ModifierType>(row.modifierType),
                    isStackable = row.isStackable,
                    maxStacks = row.maxStacks,
                    isExpendable = row.isExpendable,
                    expendCount = row.expendCount,
                    duration = row.duration
                });
            }

            ApplyToSO(_statusEffectTable, dataList, "StatusEffects");
        }

        private void ImportEvents(string csvPath)
        {
            if (_eventTable == null) { LogMissingSO("EventTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseEvents(csv);
            var dataList = new List<EventData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new EventData
                {
                    areaId = row.areaId,
                    id = row.id,
                    eventType = ParseEnum<EventType>(row.eventType),
                    eventValue = row.eventValue,
                    spawnTrigger = ParseEnum<SpawnTrigger>(row.spawnTrigger),
                    comparisonOperator = ParseEnum<ComparisonOperator>(row.comparisonOperator),
                    spawnTriggerValue = row.spawnTriggerValue,
                    minLevel = row.minLevel,
                    maxLevel = row.maxLevel,
                    rarity = ParseEnum<Rarity>(row.rarity),
                    rewardId = row.rewardId,
                    rewardMinCount = row.rewardMinCount,
                    rewardMaxCount = row.rewardMaxCount
                });
            }

            ApplyToSO(_eventTable, dataList, "Events");
        }

        private void ImportAreas(string csvPath)
        {
            if (_areaTable == null) { LogMissingSO("AreaTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseAreas(csv);
            var dataList = new List<AreaData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new AreaData
                {
                    id = row.id,
                    name = row.name,
                    description = row.description,
                    areaLevelMin = row.areaLevelMin,
                    areaLevelMax = row.areaLevelMax,
                    areaCardinalPoint = row.areaCardinalPoint,
                    logoImagePath = row.logoImagePath,
                    floorImagePath = row.floorImagePath,
                    skyboxPath = row.skyboxPath,
                    cellVisualNovelPath = row.cellVisualNovelPath,
                    cellEncountPath = row.cellEncountPath,
                    cellBattleNormalPath = row.cellBattleNormalPath,
                    cellBattleElitePath = row.cellBattleElitePath,
                    cellBattleBossPath = row.cellBattleBossPath,
                    cellBattleEventPath = row.cellBattleEventPath
                });
            }

            ApplyToSO(_areaTable, dataList, "Areas");
        }

        private void ImportEnemyCombinations(string csvPath)
        {
            if (_enemyCombinationTable == null) { LogMissingSO("EnemyCombinationTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseEnemyCombinations(csv);
            var dataList = new List<EnemyCombinationData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new EnemyCombinationData
                {
                    id = row.id,
                    name = row.name,
                    description = row.description,
                    waveCount = row.waveCount,
                    enemyUnit1 = row.enemyUnit1,
                    enemyUnit2 = row.enemyUnit2,
                    enemyUnit3 = row.enemyUnit3,
                    enemyUnit4 = row.enemyUnit4,
                    enemyUnit5 = row.enemyUnit5
                });
            }

            ApplyToSO(_enemyCombinationTable, dataList, "EnemyCombinations");
        }

        private void ImportAIPatterns(string csvPath)
        {
            if (_aiPatternTable == null) { LogMissingSO("AIPatternTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseAIPatterns(csv);
            var dataList = new List<AIPatternData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new AIPatternData
                {
                    id = row.id,
                    patternName = row.patternName,
                    description = row.description,
                    defaultActionType = ParseEnum<AIActionType>(row.defaultActionType),
                    defaultCardId = row.defaultCardId,
                    defaultTargetSelection = ParseEnum<TargetSelectionRule>(row.defaultTargetSelection)
                });
            }

            ApplyToSO(_aiPatternTable, dataList, "AIPatterns");
        }

        private void ImportAIPatternRules(string csvPath)
        {
            if (_aiPatternRuleTable == null) { LogMissingSO("AIPatternRuleTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseAIPatternRules(csv);
            var dataList = new List<AIPatternRuleData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new AIPatternRuleData
                {
                    aiPatternId = row.aiPatternId,
                    ruleId = row.ruleId,
                    priority = row.priority,
                    actionType = ParseEnum<AIActionType>(row.actionType),
                    cardId = row.cardId,
                    targetSelection = ParseEnum<TargetSelectionRule>(row.targetSelection),
                    speechLine = row.speechLine,
                    cutInEffect = row.cutInEffect,
                    zoomIn = row.zoomIn
                });
            }

            ApplyToSO(_aiPatternRuleTable, dataList, "AIPatternRules");
        }

        private void ImportAIConditions(string csvPath)
        {
            if (_aiConditionTable == null) { LogMissingSO("AIConditionTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseAIConditions(csv);
            var dataList = new List<AIConditionData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new AIConditionData
                {
                    ruleId = row.ruleId,
                    conditionType = ParseEnum<AIConditionType>(row.conditionType),
                    comparisonOperator = ParseEnum<ComparisonOperator>(row.comparisonOperator),
                    value = row.value,
                    divisor = row.divisor,
                    remainder = row.remainder
                });
            }

            ApplyToSO(_aiConditionTable, dataList, "AIConditions");
        }

        private void ImportRewardTable(string csvPath)
        {
            if (_rewardTable == null) { LogMissingSO("RewardTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseRewardTable(csv);
            var dataList = new List<RewardTableData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new RewardTableData
                {
                    id = row.id,
                    itemId = row.itemId,
                    rarity = ParseEnum<Rarity>(row.rarity),
                    dropRate = row.dropRate
                });
            }

            ApplyToSO(_rewardTable, dataList, "RewardTable");
        }

        private void ImportElementAffinity(string csvPath)
        {
            if (_elementAffinityTable == null) { LogMissingSO("ElementAffinityTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseElementAffinity(csv);
            var dataList = new List<ElementAffinityData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new ElementAffinityData
                {
                    attackElement = ParseEnum<ElementType>(row.attackElement),
                    targetElement = ParseEnum<ElementType>(row.targetElement),
                    modValue = row.modValue
                });
            }

            Undo.RecordObject(_elementAffinityTable, "Import ElementAffinity CSV");
            _elementAffinityTable.SetEntries(dataList);
            EditorUtility.SetDirty(_elementAffinityTable);
            Debug.Log($"[CsvToSOImporter] ElementAffinity 임포트 완료: {dataList.Count}건");
        }

        private void ImportBattleActions(string csvPath)
        {
            if (_battleActionTable == null) { LogMissingSO("BattleActionTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseBattleActions(csv);
            var dataList = new List<BattleActionData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new BattleActionData
                {
                    groupId = row.groupId,
                    sequence = row.sequence,
                    actionType = ParseEnum<BattleActionType>(row.actionType),
                    actionValue = row.actionValue,
                    targetUnit = row.targetUnit,
                    waitNext = row.waitNext
                });
            }

            ApplyToSO(_battleActionTable, dataList, "BattleActions");
        }

        private void ImportBattleTimelines(string csvPath)
        {
            if (_battleTimelineTable == null) { LogMissingSO("BattleTimelineTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseBattleTimelines(csv);
            var dataList = new List<BattleTimelineData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new BattleTimelineData
                {
                    id = row.id,
                    eventId = row.eventId,
                    triggerTarget = row.triggerTarget,
                    triggerType = ParseEnum<TimelineTriggerType>(row.triggerType),
                    triggerValue = row.triggerValue,
                    priority = row.priority,
                    isRepeatable = row.isRepeatable,
                    actionGroupId = row.actionGroupId
                });
            }

            ApplyToSO(_battleTimelineTable, dataList, "BattleTimelines");
        }

        private void ImportCampaigns(string csvPath)
        {
            if (_campaignTable == null) { LogMissingSO("CampaignTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseCampaigns(csv);
            var dataList = new List<CampaignData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new CampaignData
                {
                    id = row.id,
                    name = row.name,
                    description = row.description,
                    artworkPath = row.artworkPath,
                    unlockType = ParseEnum<CampaignTriggerType>(row.unlockType),
                    unlockId = row.unlockId,
                    groupId = row.groupId,
                    rewards = row.rewards,
                    isCompleted = row.isCompleted,
                    afterComplete = row.afterComplete
                });
            }

            ApplyToSO(_campaignTable, dataList, "Campaigns");
        }

        private void ImportCampaignGoalGroups(string csvPath)
        {
            if (_campaignGoalGroupTable == null) { LogMissingSO("CampaignGoalGroupTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseCampaignGoalGroups(csv);
            var dataList = new List<CampaignGoalGroupData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new CampaignGoalGroupData
                {
                    groupId = row.groupId,
                    sequence = row.sequence,
                    name = row.name,
                    description = row.description,
                    isEssential = row.isEssential,
                    triggerType = ParseEnum<CampaignTriggerType>(row.triggerType),
                    triggerValue = row.triggerValue,
                    additionalRewards = row.additionalRewards,
                    isClearTrigger = row.isClearTrigger,
                    isCompleted = row.isCompleted
                });
            }

            ApplyToSO(_campaignGoalGroupTable, dataList, "CampaignGoalGroups");
        }

        private void ImportDropRates(string csvPath)
        {
            if (_dropRateTable == null) { LogMissingSO("DropRateTable"); return; }
            string csv = ReadCsvFile(csvPath);
            var rows = CsvUtility.ParseDropRates(csv);
            var dataList = new List<DropRateData>(rows.Count);

            foreach (var row in rows)
            {
                dataList.Add(new DropRateData
                {
                    category = ParseEnum<DropRateCategory>(row.category),
                    rarity = ParseEnum<Rarity>(row.rarity),
                    dropValue = row.dropValue
                });
            }

            Undo.RecordObject(_dropRateTable, "Import DropRates CSV");
            _dropRateTable.SetEntries(dataList);
            EditorUtility.SetDirty(_dropRateTable);
            Debug.Log($"[CsvToSOImporter] DropRates 임포트 완료: {dataList.Count}건");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// BaseTableSO 계열 SO에 데이터를 적용한다.
        /// </summary>
        private static void ApplyToSO<T>(BaseTableSO<T> so, List<T> dataList, string tableName) where T : class
        {
            Undo.RecordObject(so, $"Import {tableName} CSV");
            so.SetEntries(dataList);
            EditorUtility.SetDirty(so);
            Debug.Log($"[CsvToSOImporter] {tableName} 임포트 완료: {dataList.Count}건");
        }

        /// <summary>
        /// CSV 파일을 UTF-8로 읽어 문자열을 반환한다.
        /// </summary>
        private static string ReadCsvFile(string path)
        {
            return File.ReadAllText(path, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// 문자열을 Enum 값으로 변환한다. 실패 시 기본값을 반환하고 경고 로그를 출력한다.
        /// </summary>
        private static T ParseEnum<T>(string value) where T : struct, System.Enum
        {
            if (string.IsNullOrEmpty(value))
            {
                return default;
            }

            if (System.Enum.TryParse<T>(value, true, out T result))
            {
                return result;
            }

            Debug.LogWarning($"[CsvToSOImporter] Enum 파싱 실패 - {typeof(T).Name}: '{value}'");
            return default;
        }

        private static void LogMissingSO(string soName)
        {
            Debug.LogWarning($"[CsvToSOImporter] {soName} SO가 할당되지 않았습니다.");
        }

        #endregion
    }
}
#endif
