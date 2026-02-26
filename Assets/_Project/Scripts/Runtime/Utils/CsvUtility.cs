using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class CsvUtility
{
    // ========================================
    // CSV �Ľ� ���� �޼���
    // ========================================

    public static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }

    // ========================================
    // CardGame �Ľ� �޼���
    // ========================================

    /// <summary>
    /// Cards.csv 파싱
    /// </summary>
    public static List<CardRow> ParseCards(string csvText)
    {
        var data = new List<CardRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 14 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CardRow
            {
                id = cols[0],
                cardName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                artworkPath = cols[3],
                cost = int.TryParse(cols[4], out var cost) ? cost : 0,
                cardEffectId = cols[5],
                cardType = cols[6],
                rarity = cols[7],
                targetType = cols[8],
                targetSelectionRule = cols[9],
                targetCount = int.TryParse(cols[10], out var tc) ? tc : 1,
                targetFilter = cols[11],
                element = cols[12],
                isDisposable = bool.TryParse(cols[13], out var disp) ? disp : false
            });
        }
        return data;
    }

    /// <summary>
    /// CardEffects.csv 파싱
    /// </summary>
    public static List<CardEffectRow> ParseCardEffects(string csvText)
    {
        var data = new List<CardEffectRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 9 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CardEffectRow
            {
                id = cols[0],
                effectType = cols[1],
                value = float.TryParse(cols[2], out var v) ? v : 0f,
                statusEffectId = cols[3],
                modificationType = cols[4],
                modDuration = cols[5],
                cardTargetSelection = cols[6],
                targetCardType = cols[7],
                addCardId = cols[8]
            });
        }
        return data;
    }

    /// <summary>
    /// StatusEffects.csv 파싱
    /// </summary>
    public static List<StatusEffectRow> ParseStatusEffects(string csvText)
    {
        var data = new List<StatusEffectRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 15 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new StatusEffectRow
            {
                id = cols[0],
                effectName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                iconPath = cols[3],
                statusType = cols[4],
                triggerTiming = cols[5],
                effectType = cols[6],
                effectElement = cols[7],
                value = float.TryParse(cols[8], out var v) ? v : 0f,
                modifierType = cols[9],
                isStackable = bool.TryParse(cols[10], out var s) ? s : false,
                maxStacks = int.TryParse(cols[11], out var ms) ? ms : 1,
                isExpendable = bool.TryParse(cols[12], out var exp) ? exp : false,
                expendCount = int.TryParse(cols[13], out var ec) ? ec : 0,
                duration = int.TryParse(cols[14], out var dur) ? dur : 0
            });
        }
        return data;
    }

    /// <summary>
    /// Units.csv 파싱
    /// </summary>
    public static List<UnitRow> ParseUnits(string csvText)
    {
        var data = new List<UnitRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 11 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new UnitRow
            {
                id = cols[0],
                unitName = cols[1],
                unitType = cols[2],
                element = cols[3],
                portraitPath = cols[4],
                maxHp = int.TryParse(cols[5], out var hp) ? hp : 0,
                maxEnergy = int.TryParse(cols[6], out var e) ? e : 0,
                maxAP = int.TryParse(cols[7], out var ap) ? ap : 0,
                initialDeckIds = cols[8],
                initialSkillId = cols[9],
                aiPatternId = cols[10]
            });
        }
        return data;
    }

    /// <summary>
    /// AIPatterns.csv �Ľ�
    /// </summary>
    public static List<AIPatternRow> ParseAIPatterns(string csvText)
    {
        var data = new List<AIPatternRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 6 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new AIPatternRow
            {
                id = cols[0],
                patternName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                defaultActionType = cols[3],
                defaultCardId = cols[4],
                defaultTargetSelection = cols[5]
            });
        }
        return data;
    }

    /// <summary>
    /// AIPatternRules.csv 파싱
    /// </summary>
    public static List<AIPatternRuleRow> ParseAIPatternRules(string csvText)
    {
        var data = new List<AIPatternRuleRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 9 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new AIPatternRuleRow
            {
                aiPatternId = cols[0],
                ruleId = cols[1],
                priority = int.TryParse(cols[2], out var p) ? p : 0,
                actionType = cols[3],
                cardId = cols[4],
                targetSelection = cols[5],
                speechLine = cols[6],
                cutInEffect = cols[7],
                zoomIn = bool.TryParse(cols[8], out var zi) ? zi : false
            });
        }
        return data;
    }

    /// <summary>
    /// AIConditions.csv 파싱
    /// </summary>
    public static List<AIConditionRow> ParseAIConditions(string csvText)
    {
        var data = new List<AIConditionRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 6 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new AIConditionRow
            {
                ruleId = cols[0],
                conditionType = cols[1],
                comparisonOperator = cols[2],
                value = cols[3],
                divisor = int.TryParse(cols[4], out var d) ? d : 0,
                remainder = int.TryParse(cols[5], out var r) ? r : 0
            });
        }
        return data;
    }

    /// <summary>
    /// CombatScenarios.csv �Ľ� (���� ���� ����)
    /// </summary>
    public static List<CombatScenarioRow> ParseCombatScenarios(string csvText)
    {
        var data = new List<CombatScenarioRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 5 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CombatScenarioRow
            {
                id = cols[0],
                scenarioName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                playerUnitId = cols[3],
                enemyUnitIds = cols[4]
            });
        }
        return data;
    }

    // ========================================
    // 신규 데이터 파싱 메서드
    // ========================================

    /// <summary>
    /// Skills.csv 파싱
    /// </summary>
    public static List<SkillRow> ParseSkills(string csvText)
    {
        var data = new List<SkillRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 19 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new SkillRow
            {
                id = cols[0],
                skillName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                unitId = cols[3],
                artworkPath = cols[4],
                rarity = cols[5],
                element = cols[6],
                triggerTarget = cols[7],
                triggerStatus = cols[8],
                comparisonOperator = cols[9],
                triggerValue = cols[10],
                triggerElement = cols[11],
                cardEffectId = cols[12],
                targetType = cols[13],
                targetFilter = cols[14],
                targetSelectionRule = cols[15],
                targetCount = int.TryParse(cols[16], out var tc) ? tc : 0,
                limitType = cols[17],
                limitValue = int.TryParse(cols[18], out var lv) ? lv : 0
            });
        }
        return data;
    }

    /// <summary>
    /// Items.csv 파싱
    /// </summary>
    public static List<ItemRow> ParseItems(string csvText)
    {
        var data = new List<ItemRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 13 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new ItemRow
            {
                id = cols[0],
                itemName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                artworkPath = cols[3],
                rarity = cols[4],
                itemType = cols[5],
                targetUnit = cols[6],
                targetStatus = cols[7],
                modifyValue = cols[8],
                isDisposable = bool.TryParse(cols[9], out var disp) ? disp : false,
                disposeTrigger = cols[10],
                disposePercentage = float.TryParse(cols[11], out var dp) ? dp : 0f,
                stackCount = int.TryParse(cols[12], out var sc) ? sc : 1
            });
        }
        return data;
    }

    /// <summary>
    /// Events.csv 파싱
    /// </summary>
    public static List<EventRow> ParseEvents(string csvText)
    {
        var data = new List<EventRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 13 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new EventRow
            {
                areaId = cols[0],
                id = cols[1],
                eventType = cols[2],
                eventValue = cols[3],
                spawnTrigger = cols[4],
                comparisonOperator = cols[5],
                spawnTriggerValue = cols[6],
                minLevel = int.TryParse(cols[7], out var min) ? min : 0,
                maxLevel = int.TryParse(cols[8], out var max) ? max : 0,
                rarity = cols[9],
                rewardId = cols[10],
                rewardMinCount = int.TryParse(cols[11], out var rmin) ? rmin : 0,
                rewardMaxCount = int.TryParse(cols[12], out var rmax) ? rmax : 0
            });
        }
        return data;
    }

    /// <summary>
    /// Areas.csv 파싱
    /// </summary>
    public static List<AreaRow> ParseAreas(string csvText)
    {
        var data = new List<AreaRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 15 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new AreaRow
            {
                id = cols[0],
                name = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                areaLevelMin = int.TryParse(cols[3], out var lmin) ? lmin : 0,
                areaLevelMax = int.TryParse(cols[4], out var lmax) ? lmax : 0,
                areaCardinalPoint = cols[5],
                logoImagePath = cols[6],
                floorImagePath = cols[7],
                skyboxPath = cols[8],
                cellVisualNovelPath = cols[9],
                cellEncountPath = cols[10],
                cellBattleNormalPath = cols[11],
                cellBattleElitePath = cols[12],
                cellBattleBossPath = cols[13],
                cellBattleEventPath = cols[14]
            });
        }
        return data;
    }

    /// <summary>
    /// EnemyCombinations.csv 파싱
    /// </summary>
    public static List<EnemyCombinationRow> ParseEnemyCombinations(string csvText)
    {
        var data = new List<EnemyCombinationRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 9 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new EnemyCombinationRow
            {
                id = cols[0],
                name = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                waveCount = int.TryParse(cols[3], out var wc) ? wc : 1,
                enemyUnit1 = cols[4],
                enemyUnit2 = cols[5],
                enemyUnit3 = cols[6],
                enemyUnit4 = cols[7],
                enemyUnit5 = cols[8]
            });
        }
        return data;
    }

    /// <summary>
    /// RewardTable.csv 파싱
    /// </summary>
    public static List<RewardTableRow> ParseRewardTable(string csvText)
    {
        var data = new List<RewardTableRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 4 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new RewardTableRow
            {
                id = cols[0],
                itemId = cols[1],
                rarity = cols[2],
                dropRate = float.TryParse(cols[3], out var dr) ? dr : 0f
            });
        }
        return data;
    }

    /// <summary>
    /// ElementAffinity.csv 파싱
    /// </summary>
    public static List<ElementAffinityRow> ParseElementAffinity(string csvText)
    {
        var data = new List<ElementAffinityRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 3 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new ElementAffinityRow
            {
                attackElement = cols[0],
                targetElement = cols[1],
                modValue = float.TryParse(cols[2], out var mv) ? mv : 1.0f
            });
        }
        return data;
    }

    /// <summary>
    /// BattleActions.csv 파싱
    /// </summary>
    public static List<BattleActionRow> ParseBattleActions(string csvText)
    {
        var data = new List<BattleActionRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 6 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new BattleActionRow
            {
                groupId = cols[0],
                sequence = int.TryParse(cols[1], out var seq) ? seq : 0,
                actionType = cols[2],
                actionValue = cols[3],
                targetUnit = cols[4],
                waitNext = bool.TryParse(cols[5], out var wn) ? wn : false
            });
        }
        return data;
    }

    /// <summary>
    /// BattleTimelines.csv 파싱
    /// </summary>
    public static List<BattleTimelineRow> ParseBattleTimelines(string csvText)
    {
        var data = new List<BattleTimelineRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 8 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new BattleTimelineRow
            {
                id = cols[0],
                eventId = cols[1],
                triggerTarget = cols[2],
                triggerType = cols[3],
                triggerValue = cols[4],
                priority = int.TryParse(cols[5], out var p) ? p : 0,
                isRepeatable = bool.TryParse(cols[6], out var rep) ? rep : false,
                actionGroupId = cols[7]
            });
        }
        return data;
    }

    /// <summary>
    /// Campaigns.csv 파싱
    /// </summary>
    public static List<CampaignRow> ParseCampaigns(string csvText)
    {
        var data = new List<CampaignRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 10 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CampaignRow
            {
                id = cols[0],
                name = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                artworkPath = cols[3],
                unlockType = cols[4],
                unlockId = cols[5],
                groupId = cols[6],
                rewards = cols[7],
                isCompleted = bool.TryParse(cols[8], out var ic) ? ic : false,
                afterComplete = cols[9]
            });
        }
        return data;
    }

    /// <summary>
    /// CampaignGoalGroups.csv 파싱
    /// </summary>
    public static List<CampaignGoalGroupRow> ParseCampaignGoalGroups(string csvText)
    {
        var data = new List<CampaignGoalGroupRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 10 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CampaignGoalGroupRow
            {
                groupId = cols[0],
                sequence = int.TryParse(cols[1], out var seq) ? seq : 0,
                name = cols[2],
                description = cols[3].Replace("\\n", "\n"),
                isEssential = bool.TryParse(cols[4], out var ess) ? ess : false,
                triggerType = cols[5],
                triggerValue = cols[6],
                additionalRewards = cols[7],
                isClearTrigger = bool.TryParse(cols[8], out var ct) ? ct : false,
                isCompleted = bool.TryParse(cols[9], out var ic) ? ic : false
            });
        }
        return data;
    }

    /// <summary>
    /// DropRates.csv 파싱
    /// </summary>
    public static List<DropRateRow> ParseDropRates(string csvText)
    {
        var data = new List<DropRateRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 3 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new DropRateRow
            {
                category = cols[0],
                rarity = cols[1],
                dropValue = float.TryParse(cols[2], out var dv) ? dv : 0f
            });
        }
        return data;
    }

    /// <summary>
    /// OwnedUnits.csv 파싱 (플레이어 세이브 데이터)
    /// </summary>
    public static List<OwnedUnitRow> ParseOwnedUnits(string csvText)
    {
        var data = new List<OwnedUnitRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 7 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new OwnedUnitRow
            {
                unitId = cols[0],
                cardElement = cols[1],
                editedDeck = cols[2],
                editedSkill = cols[3],
                equipItem1 = cols[4],
                equipItem2 = cols[5],
                partyPosition = int.TryParse(cols[6], out var pp) ? pp : 0
            });
        }
        return data;
    }

    /// <summary>
    /// InventoryItems.csv 파싱 (플레이어 세이브 데이터)
    /// </summary>
    public static List<InventoryItemRow> ParseInventoryItems(string csvText)
    {
        var data = new List<InventoryItemRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 13 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new InventoryItemRow
            {
                category = cols[0],
                productId = cols[1],
                productName = cols[2],
                description = cols[3].Replace("\\n", "\n"),
                rarity = cols[4],
                ownStack = int.TryParse(cols[5], out var os) ? os : 0,
                useStack = int.TryParse(cols[6], out var us) ? us : 0,
                cardElement = cols[7],
                cardType = cols[8],
                cardCost = int.TryParse(cols[9], out var cc) ? cc : 0,
                itemType = cols[10],
                targetStatus = cols[11],
                isDisposable = bool.TryParse(cols[12], out var disp) ? disp : false
            });
        }
        return data;
    }

    /// <summary>
    /// InGameBagItems.csv 파싱 (플레이어 세이브 데이터)
    /// </summary>
    public static List<InGameBagItemRow> ParseInGameBagItems(string csvText)
    {
        var data = new List<InGameBagItemRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 6 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new InGameBagItemRow
            {
                category = cols[0],
                productId = cols[1],
                productName = cols[2],
                description = cols[3].Replace("\\n", "\n"),
                rarity = cols[4],
                isNewForNow = bool.TryParse(cols[5], out var nfn) ? nfn : false
            });
        }
        return data;
    }

    /// <summary>
    /// ExplorationRecords.csv 파싱 (플레이어 세이브 데이터)
    /// </summary>
    public static List<ExplorationRecordRow> ParseExplorationRecords(string csvText)
    {
        var data = new List<ExplorationRecordRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 12) continue;

            data.Add(new ExplorationRecordRow
            {
                countDepart = int.TryParse(cols[0], out var cd) ? cd : 0,
                countComplete = int.TryParse(cols[1], out var cc) ? cc : 0,
                countBattleAll = int.TryParse(cols[2], out var cba) ? cba : 0,
                countVisualNovelAll = int.TryParse(cols[3], out var cvn) ? cvn : 0,
                countEncountAll = int.TryParse(cols[4], out var cea) ? cea : 0,
                countBattleNow = int.TryParse(cols[5], out var cbn) ? cbn : 0,
                countVisualNovelNow = int.TryParse(cols[6], out var cvnn) ? cvnn : 0,
                countEncountNow = int.TryParse(cols[7], out var cen) ? cen : 0,
                countEnemyEliminated = int.TryParse(cols[8], out var cee) ? cee : 0,
                eliminatedBossId = cols[9],
                visitedAreaId = cols[10],
                countRewardComplete = int.TryParse(cols[11], out var crc) ? crc : 0
            });
        }
        return data;
    }

    // ========================================
    // 유틸 메서드
    // ========================================

    public static string Escape(string s)
    {
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
        {
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }
        return s;
    }
}