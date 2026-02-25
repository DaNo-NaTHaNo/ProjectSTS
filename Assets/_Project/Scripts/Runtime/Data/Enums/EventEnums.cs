namespace ProjectStS.Data
{
    /// <summary>
    /// 이벤트의 종류.
    /// </summary>
    public enum EventType
    {
        VisualNovel,
        BattleNormal,
        BattleElite,
        BattleBoss,
        BattleEvent,
        Encounter
    }

    /// <summary>
    /// 이벤트 출현 조건 타입.
    /// </summary>
    public enum SpawnTrigger
    {
        None,
        ClearCampaign,
        WinToBoss,
        OwnCharacter,
        OwnItem
    }
}
