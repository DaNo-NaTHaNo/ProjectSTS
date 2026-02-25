using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 유닛 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitTable", menuName = "ProjectStS/Data/UnitTable")]
    public class UnitTableSO : BaseTableSO<UnitData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(UnitData entry)
        {
            return entry.id;
        }
    }
}
