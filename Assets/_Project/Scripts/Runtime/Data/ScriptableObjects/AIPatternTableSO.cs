using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// AI 패턴 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "AIPatternTable", menuName = "ProjectStS/Data/AIPatternTable")]
    public class AIPatternTableSO : BaseTableSO<AIPatternData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(AIPatternData entry)
        {
            return entry.id;
        }
    }
}
