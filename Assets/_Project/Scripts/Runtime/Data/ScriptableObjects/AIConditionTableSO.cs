using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// AI 컨디션 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "AIConditionTable", menuName = "ProjectStS/Data/AIConditionTable")]
    public class AIConditionTableSO : BaseTableSO<AIConditionData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(AIConditionData entry)
        {
            return entry.ruleId;
        }
    }
}
