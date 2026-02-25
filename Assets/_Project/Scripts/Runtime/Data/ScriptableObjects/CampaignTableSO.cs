using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 캠페인 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "CampaignTable", menuName = "ProjectStS/Data/CampaignTable")]
    public class CampaignTableSO : BaseTableSO<CampaignData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(CampaignData entry)
        {
            return entry.id;
        }
    }
}
