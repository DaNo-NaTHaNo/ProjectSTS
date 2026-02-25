using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 전투 타임라인 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleTimelineTable", menuName = "ProjectStS/Data/BattleTimelineTable")]
    public class BattleTimelineTableSO : BaseTableSO<BattleTimelineData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(BattleTimelineData entry)
        {
            return entry.id;
        }
    }
}
