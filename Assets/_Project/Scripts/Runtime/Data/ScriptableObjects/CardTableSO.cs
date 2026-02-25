using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 카드 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "CardTable", menuName = "ProjectStS/Data/CardTable")]
    public class CardTableSO : BaseTableSO<CardData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(CardData entry)
        {
            return entry.id;
        }
    }
}
