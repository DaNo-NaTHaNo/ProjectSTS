namespace ProjectStS.Data
{
    /// <summary>
    /// 전투 연출 행동의 타입.
    /// </summary>
    public enum BattleActionType
    {
        /// <summary>비주얼 노벨 JSON 재생</summary>
        PlayDialogue,
        /// <summary>유닛 애니메이션 재생</summary>
        PlayAnim,
        /// <summary>컷인 연출 재생</summary>
        PlayCutIn,
        /// <summary>BGM 변경</summary>
        ChangeBGM,
        /// <summary>배경 이미지 변경</summary>
        ChangeBGI,
        /// <summary>유닛 생성</summary>
        SpawnUnit,
        /// <summary>AI 패턴 변경</summary>
        ChangeAIPattern,
        /// <summary>강제 전투 종료</summary>
        BattleEnd
    }

    /// <summary>
    /// 전투 이벤트 타임라인 발동 조건 타입.
    /// </summary>
    public enum TimelineTriggerType
    {
        /// <summary>현재 턴 수가 ~일 경우</summary>
        TurnCount,
        /// <summary>Target의 HP가 ~% 이하일 경우</summary>
        HpPercent,
        /// <summary>Target의 HP가 0이 되었을 경우</summary>
        UnitDown,
        /// <summary>Target 유닛이 전투에 등장했을 경우</summary>
        UnitSpawn,
        /// <summary>Target에게 ~ 상태이상이 부여되었을 경우</summary>
        OnStatus,
        /// <summary>적 유닛의 수가 ~일 경우</summary>
        EnemyCount,
        /// <summary>HP가 1 이상인 아군 파티 유닛 수가 ~일 경우</summary>
        PartyCount
    }
}
