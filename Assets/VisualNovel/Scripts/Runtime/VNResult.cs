using System.Collections.Generic;

/// <summary>
/// VN 에피소드 재생 완료 시 외부 시스템으로 반환되는 결과 데이터.
/// Command 노드에서 수집된 명령 목록과 마지막 선택지 정보를 포함한다.
/// </summary>
[System.Serializable]
public class VNResult
{
    /// <summary>에피소드가 정상 완료(End 노드 도달)되었는지 여부</summary>
    public bool IsCompleted;

    /// <summary>마지막 Branch 노드에서의 선택지 인덱스. 선택지가 없었으면 -1</summary>
    public int LastBranchChoice;

    /// <summary>에피소드 재생 중 Command 노드에서 수집된 명령 레코드 목록</summary>
    public List<CommandRecord> Commands;
}

/// <summary>
/// Command 노드 실행 기록. VN 재생 중 통과한 Command 노드의 정보를 저장한다.
/// </summary>
[System.Serializable]
public class CommandRecord
{
    /// <summary>명령 식별 키 (예: "nextEpisode", "startBattle", "giveItem", "setFlag")</summary>
    public string CommandKey;

    /// <summary>명령 파라미터 (예: 에피소드 ID, 적 조합 ID, 아이템 ID 등)</summary>
    public string CommandValue;
}
