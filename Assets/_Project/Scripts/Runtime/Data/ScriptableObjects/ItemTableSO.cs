using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 아이템 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemTable", menuName = "ProjectStS/Data/ItemTable")]
    public class ItemTableSO : BaseTableSO<ItemData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(ItemData entry)
        {
            return entry.id;
        }
    }
}
