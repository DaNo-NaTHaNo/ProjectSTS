namespace ProjectStS.Data
{
    /// <summary>
    /// 레어도별 기본 드랍율 데이터 모델.
    /// 드랍율 적용 대상 카테고리와 레어도에 따른 합산 드랍율을 정의한다.
    /// </summary>
    [System.Serializable]
    public class DropRateData
    {
        /// <summary>드랍율을 적용할 대상 (SpawnEvent, EventReward)</summary>
        public DropRateCategory category;

        /// <summary>드랍율을 적용할 레어도</summary>
        public Rarity rarity;

        /// <summary>테이블 내 해당 레어도의 드랍율 합산치 (%)</summary>
        public float dropValue;
    }
}
