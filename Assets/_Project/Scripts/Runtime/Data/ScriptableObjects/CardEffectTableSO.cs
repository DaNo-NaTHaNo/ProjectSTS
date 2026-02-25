using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 카드 효과 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "CardEffectTable", menuName = "ProjectStS/Data/CardEffectTable")]
    public class CardEffectTableSO : BaseTableSO<CardEffectData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(CardEffectData entry)
        {
            return entry.id;
        }
    }
}
