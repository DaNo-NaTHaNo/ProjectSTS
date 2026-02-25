namespace ProjectStS.Data
{
    /// <summary>
    /// 속성 간 상성 보정치 데이터 모델.
    /// 공격 속성과 피격 속성 간의 대미지 배율 보정치를 정의한다.
    /// </summary>
    [System.Serializable]
    public class ElementAffinityData
    {
        /// <summary>공격하는 속성</summary>
        public ElementType attackElement;

        /// <summary>공격을 받는 속성</summary>
        public ElementType targetElement;

        /// <summary>공격 시 피해량에 곱해지는 보정치</summary>
        public float modValue;
    }
}
