using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 구역 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "AreaTable", menuName = "ProjectStS/Data/AreaTable")]
    public class AreaTableSO : BaseTableSO<AreaData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(AreaData entry)
        {
            return entry.id;
        }
    }
}
