using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// AI 패턴 룰 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "AIPatternRuleTable", menuName = "ProjectStS/Data/AIPatternRuleTable")]
    public class AIPatternRuleTableSO : BaseTableSO<AIPatternRuleData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(AIPatternRuleData entry)
        {
            return entry.ruleId;
        }
    }
}
