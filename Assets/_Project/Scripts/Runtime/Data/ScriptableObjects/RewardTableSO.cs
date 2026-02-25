using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 보상 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "RewardTable", menuName = "ProjectStS/Data/RewardTable")]
    public class RewardTableSO : BaseTableSO<RewardTableData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(RewardTableData entry)
        {
            return entry.id;
        }
    }
}
