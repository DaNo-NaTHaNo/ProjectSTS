using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 캠페인 목표 그룹 마스터 데이터 테이블.
    /// groupId와 sequence를 조합하여 고유 키를 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "CampaignGoalGroupTable", menuName = "ProjectStS/Data/CampaignGoalGroupTable")]
    public class CampaignGoalGroupTableSO : BaseTableSO<CampaignGoalGroupData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(CampaignGoalGroupData entry)
        {
            return entry.groupId + "_" + entry.sequence;
        }
    }
}
