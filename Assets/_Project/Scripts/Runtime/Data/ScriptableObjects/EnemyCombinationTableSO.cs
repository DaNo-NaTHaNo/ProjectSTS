using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 적 조합 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyCombinationTable", menuName = "ProjectStS/Data/EnemyCombinationTable")]
    public class EnemyCombinationTableSO : BaseTableSO<EnemyCombinationData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(EnemyCombinationData entry)
        {
            return entry.id;
        }
    }
}
