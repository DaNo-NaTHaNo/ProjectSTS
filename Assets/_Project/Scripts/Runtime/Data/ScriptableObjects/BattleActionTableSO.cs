using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 전투 연출 행동 마스터 데이터 테이블.
    /// groupId와 sequence를 조합하여 고유 키를 생성한다.
    /// </summary>
    [CreateAssetMenu(fileName = "BattleActionTable", menuName = "ProjectStS/Data/BattleActionTable")]
    public class BattleActionTableSO : BaseTableSO<BattleActionData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(BattleActionData entry)
        {
            return entry.groupId + "_" + entry.sequence;
        }
    }
}
