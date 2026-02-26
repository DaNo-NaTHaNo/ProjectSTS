using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// VisualNovel 전용 CSV 파싱/생성 유틸리티.
/// CsvUtility에서 분리된 VN 데이터 처리 메서드를 제공한다.
/// </summary>
public static class CsvUtilityVN
{
    // ========================================
    // VisualNovel - DialogueLine 파싱
    // ========================================

    public static List<DialogueLine> ParseDialogueLines(string csvText)
    {
        var data = new List<DialogueLine>();
        var lines = csvText.Split('\n').Skip(1);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = CsvUtility.ParseCsvLine(line.Trim());

            if (cols.Length < 16)
            {
                Debug.LogWarning($"Invalid DialogueLine: expected 16 columns, got {cols.Length}. Line: {line}");
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
            var cols = CsvUtility.ParseCsvLine(line.Trim());
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
            var cols = CsvUtility.ParseCsvLine(line.Trim());
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
    // VisualNovel - CSV 생성 (Export)
    // ========================================

    public static string GenerateDialogueLines(List<DialogueLine> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("sceneName,locationPreset,speakerPortrait,speakerName,speakerGroup,speakerText,face,animKey,emote,sfx,spotLight,textSpeed,skippable,autoNext,voice,wait");

        foreach (var line in data)
        {
            string exportText = line.speakerText?.Replace("\n", "\\n") ?? "";

            sb.AppendLine(string.Join(",", new string[] {
                CsvUtility.Escape(line.sceneName),
                CsvUtility.Escape(line.locationPreset),
                CsvUtility.Escape(line.speakerPortrait),
                CsvUtility.Escape(line.speakerName),
                CsvUtility.Escape(line.speakerGroup),
                CsvUtility.Escape(exportText),
                CsvUtility.Escape(line.face),
                CsvUtility.Escape(line.animKey),
                CsvUtility.Escape(line.emote),
                CsvUtility.Escape(line.sfx),
                line.spotLight.ToString(),
                line.textSpeed.ToString(),
                line.skippable.ToString(),
                line.autoNext.ToString(),
                CsvUtility.Escape(line.voice),
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
            sb.AppendLine($"{CsvUtility.Escape(line.portraitID)},{CsvUtility.Escape(line.portraitName)}");
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
                CsvUtility.Escape(line.locationPreset),
                CsvUtility.Escape(line.leftMost),
                CsvUtility.Escape(line.left),
                CsvUtility.Escape(line.center),
                CsvUtility.Escape(line.right),
                CsvUtility.Escape(line.rightMost),
                CsvUtility.Escape(line.ease),
                line.duration.ToString()
            }));
        }
        return sb.ToString();
    }
}
