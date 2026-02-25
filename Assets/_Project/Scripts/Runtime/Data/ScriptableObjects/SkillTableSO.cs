using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 스킬 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillTable", menuName = "ProjectStS/Data/SkillTable")]
    public class SkillTableSO : BaseTableSO<SkillData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(SkillData entry)
        {
            return entry.id;
        }
    }
}
