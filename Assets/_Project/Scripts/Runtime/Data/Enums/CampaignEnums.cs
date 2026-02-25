namespace ProjectStS.Data
{
    /// <summary>
    /// 캠페인 해금/목표 트리거 타입.
    /// </summary>
    public enum CampaignTriggerType
    {
        /// <summary>특정 캠페인을 클리어</summary>
        ClearCampaign,
        /// <summary>특정 이벤트를 클리어</summary>
        ClearEvent,
        /// <summary>특정 유닛을 획득</summary>
        EarnUnit,
        /// <summary>특정 카드를 획득</summary>
        EarnCard,
        /// <summary>특정 아이템을 획득</summary>
        EarnItem,
        /// <summary>특정 스킬을 획득</summary>
        EarnSkill,
        /// <summary>배틀을 ~회 완료</summary>
        BattleCount,
        /// <summary>월드맵 상에서 ~회 이동</summary>
        MoveCount,
        /// <summary>이벤트를 ~회 완료</summary>
        EventCount
    }
}
