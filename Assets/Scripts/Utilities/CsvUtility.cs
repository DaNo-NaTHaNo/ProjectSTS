using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class CsvUtility
{
    // ========================================
    // CSV ÆÄ½Ì ÇïÆÛ ¸Þ¼­µå
    // ========================================

    private static string[] ParseCsvLine(string line)
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
    // VisualNovel - DialogueLine ÆÄ½Ì
    // ========================================

    public static List<DialogueLine> ParseDialogueLines(string csvText)
    {
        var data = new List<DialogueLine>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line.Trim());

            if (cols.Length < 16)
            {
                UnityEngine.Debug.LogWarning($"Invalid DialogueLine: expected 16 columns, got {cols.Length}. Line: {line}");
                continue;
            }

            data.Add(new DialogueLine
            {
                sceneName = cols[0],
                locationPreset = cols[1],
                speakerPortrait = cols[2],
                speakerName = cols[3],
                speakerGroup = cols[4],
                speakerText = cols[5].Replace("\\n", "\n"),
                face = cols[6],
                animKey = cols[7],
                emote = cols[8],
                sfx = cols[9],
                spotLight = bool.TryParse(cols[10], out var sl) ? sl : false,
                textSpeed = float.TryParse(cols[11], out var ts) ? ts : 20f,
                skippable = bool.TryParse(cols[12], out var sk) ? sk : true,
                autoNext = bool.TryParse(cols[13], out var an) ? an : false,
                voice = cols[14],
                wait = float.TryParse(cols[15], out var w) ? w : 0f
            });
        }
        return data;
    }

    public static List<EpisodePortrait> ParseEpisodePortraits(string csvText)
    {
        var data = new List<EpisodePortrait>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 2 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new EpisodePortrait
            {
                portraitID = cols[0],
                portraitName = cols[1]
            });
        }
        return data;
    }

    public static List<LocationPreset> ParseLocationPresets(string csvText)
    {
        var data = new List<LocationPreset>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 8 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new LocationPreset
            {
                locationPreset = cols[0],
                leftMost = cols[1],
                left = cols[2],
                center = cols[3],
                right = cols[4],
                rightMost = cols[5],
                ease = cols[6],
                duration = float.TryParse(cols[7], out var d) ? d : 1.0f
            });
        }
        return data;
    }

    // ========================================
    // VisualNovel - CSV »ý¼º (Export)
    // ========================================

    public static string GenerateDialogueLines(List<DialogueLine> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sceneName,locationPreset,speakerPortrait,speakerName,speakerGroup,speakerText,face,animKey,emote,sfx,spotLight,textSpeed,skippable,autoNext,voice,wait");

        foreach (var line in data)
        {
            string exportText = line.speakerText?.Replace("\n", "\\n") ?? "";

            sb.AppendLine(string.Join(",", new string[] {
                Escape(line.sceneName),
                Escape(line.locationPreset),
                Escape(line.speakerPortrait),
                Escape(line.speakerName),
                Escape(line.speakerGroup),
                Escape(exportText),
                Escape(line.face),
                Escape(line.animKey),
                Escape(line.emote),
                Escape(line.sfx),
                line.spotLight.ToString(),
                line.textSpeed.ToString(),
                line.skippable.ToString(),
                line.autoNext.ToString(),
                Escape(line.voice),
                line.wait.ToString()
            }));
        }
        return sb.ToString();
    }

    public static string GenerateEpisodePortraits(List<EpisodePortrait> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("portraitID,portraitName");
        foreach (var line in data)
        {
            sb.AppendLine($"{Escape(line.portraitID)},{Escape(line.portraitName)}");
        }
        return sb.ToString();
    }

    public static string GenerateLocationPresets(List<LocationPreset> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("locationPreset,leftMost,left,center,right,rightMost,ease,duration");
        foreach (var line in data)
        {
            sb.AppendLine(string.Join(",", new string[] {
                Escape(line.locationPreset),
                Escape(line.leftMost),
                Escape(line.left),
                Escape(line.center),
                Escape(line.right),
                Escape(line.rightMost),
                Escape(line.ease),
                line.duration.ToString()
            }));
        }
        return sb.ToString();
    }

    // ========================================
    // CardGame ÆÄ½Ì ¸Þ¼­µå
    // ========================================

    /// <summary>
    /// Cards.csv ÆÄ½Ì
    /// </summary>
    public static List<CardRow> ParseCards(string csvText)
    {
        var data = new List<CardRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 15 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new CardRow
            {
                id = cols[0],
                cardName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                artworkPath = cols[3],
                cost = int.TryParse(cols[4], out var cost) ? cost : 0,
                cardType = cols[5],
                rarity = cols[6],
                targetType = cols[7],
                targetSelectionRule = cols[8],
                targetCount = int.TryParse(cols[9], out var tc) ? tc : 1,
                targetFilter = cols[10],
                element = cols[11],
                keywords = cols[12],
                canUpgrade = bool.TryParse(cols[13], out var cu) ? cu : false,
                upgradedCardId = cols[14]
            });
        }
        return data;
    }

    /// <summary>
    /// CardEffects.csv ÆÄ½Ì
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
                cardId = cols[0],
                effectType = cols[1],
                value = int.TryParse(cols[2], out var v) ? v : 0,
                statusEffectId = cols[3],
                duration = int.TryParse(cols[4], out var d) ? d : 0,
                modificationType = cols[5],
                modDuration = cols[6],
                cardTargetSelection = cols[7],
                targetCardType = cols[8]
            });
        }
        return data;
    }

    /// <summary>
    /// StatusEffects.csv ÆÄ½Ì
    /// </summary>
    public static List<StatusEffectRow> ParseStatusEffects(string csvText)
    {
        var data = new List<StatusEffectRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 11 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new StatusEffectRow
            {
                id = cols[0],
                effectName = cols[1],
                description = cols[2].Replace("\\n", "\n"),
                iconPath = cols[3],
                statusType = cols[4],
                triggerTiming = cols[5],
                effectType = cols[6],
                value = float.TryParse(cols[7], out var v) ? v : 0f,
                modifierType = cols[8],
                isStackable = bool.TryParse(cols[9], out var s) ? s : false,
                maxStacks = int.TryParse(cols[10], out var ms) ? ms : 1
            });
        }
        return data;
    }

    /// <summary>
    /// Units.csv ÆÄ½Ì
    /// </summary>
    public static List<UnitRow> ParseUnits(string csvText)
    {
        var data = new List<UnitRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 9 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new UnitRow
            {
                id = cols[0],
                unitName = cols[1],
                unitType = cols[2],
                portraitPath = cols[3],
                maxHp = int.TryParse(cols[4], out var hp) ? hp : 0,
                maxEnergy = int.TryParse(cols[5], out var e) ? e : 0,
                initialDeckIds = cols[6],
                aiPatternId = cols[7],
                element = cols[8]
            });
        }
        return data;
    }

    /// <summary>
    /// AIPatterns.csv ÆÄ½Ì
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
    /// AIPatternRules.csv ÆÄ½Ì
    /// </summary>
    public static List<AIPatternRuleRow> ParseAIPatternRules(string csvText)
    {
        var data = new List<AIPatternRuleRow>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = ParseCsvLine(line.Trim());
            if (cols.Length < 6 || string.IsNullOrEmpty(cols[0])) continue;

            data.Add(new AIPatternRuleRow
            {
                aiPatternId = cols[0],
                ruleId = cols[1],
                priority = int.TryParse(cols[2], out var p) ? p : 0,
                actionType = cols[3],
                cardId = cols[4],
                targetSelection = cols[5]
            });
        }
        return data;
    }

    /// <summary>
    /// AIConditions.csv ÆÄ½Ì
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
                value = float.TryParse(cols[3], out var v) ? v : 0f,
                divisor = int.TryParse(cols[4], out var d) ? d : 0,
                remainder = int.TryParse(cols[5], out var r) ? r : 0
            });
        }
        return data;
    }

    /// <summary>
    /// CombatScenarios.csv ÆÄ½Ì (¼ÕÆÐ ¼³Á¤ Á¦°Å)
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
    // ÇïÆÛ ¸Þ¼­µå
    // ========================================

    private static string Escape(string s)
    {
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
        {
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }
        return s;
    }
}