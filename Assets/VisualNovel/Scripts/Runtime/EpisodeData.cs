using System.Collections.Generic;
using UnityEngine;

// JSON 파일의 최상위 구조
[System.Serializable]
public class EpisodeData
{
    public EpisodeInfo episodeInfo;
    public AssetManifest assetManifest;
    public Datasets datasets;
    public NodeGraph nodeGraph;
}

// 1. 에피소드 메타데이터
[System.Serializable]
public class EpisodeInfo
{
    public string id;
    public string title;
    public string version;
}

// 2. 에셋 목록
[System.Serializable]
public class AssetManifest
{
    public List<string> portraits;
    public List<string> backgroundImages;
    public List<string> cutInImages;
    public List<string> popUpImages;
    public List<string> sfx;
    public List<string> bgm;
}

// 3. 3종의 CSV 데이터셋
[System.Serializable]
public class Datasets
{
    public List<DialogueLine> dialogueLines;
    public List<EpisodePortrait> episodePortraits;
    public List<LocationPreset> locationPresets;
}

// 4. 노드 그래프 정보
[System.Serializable]
public class NodeGraph
{
    public List<NodeData> nodes;
    public List<ConnectionData> connections;
}

[System.Serializable]
public class NodeData
{
    public string id;
    public string type;
    public PositionData position;
    public string fields; // 각 노드의 필드 데이터는 JSON 문자열로 저장
}

[System.Serializable]
public class PositionData
{
    public float x;
    public float y;
}

[System.Serializable]
public class ConnectionData
{
    public string sourceNodeId;
    public int sourceOutputIndex;
    public string targetNodeId;
    // public int targetInputIndex; // 현재 구조상 항상 0이므로 제거 가능
}

[System.Serializable]
public class ColorData
{
    public float r;
    public float g;
    public float b;
    public float a;
}