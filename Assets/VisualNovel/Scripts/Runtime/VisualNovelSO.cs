using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrentEpisode", menuName = "VisualNovel/Current Episode Data")]

public class VisualNovelSO : ScriptableObject // 현재 에피소드 데이터를 저장하는 ScriptableObject
{
    [Header("CSV Data")]
    // CSV로부터 불러온 데이터를 저장할 공간입니다.
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();
    public List<EpisodePortrait> episodePortraits = new List<EpisodePortrait>();
    public List<LocationPreset> locationPresets = new List<LocationPreset>();

    [Header("Node Graph Data")]
    // 나중에 노드 에디터의 데이터를 저장할 공간입니다.
    [TextArea(15, 20)]
    // JSON 형식으로 노드 그래프 데이터를 저장합니다.
    public string nodeGraphJson;
}