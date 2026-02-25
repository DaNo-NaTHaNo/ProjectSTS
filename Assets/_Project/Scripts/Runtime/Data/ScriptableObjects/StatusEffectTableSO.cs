using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 상태 효과 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "StatusEffectTable", menuName = "ProjectStS/Data/StatusEffectTable")]
    public class StatusEffectTableSO : BaseTableSO<StatusEffectData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(StatusEffectData entry)
        {
            return entry.id;
        }
    }
}
