using System.Collections.Generic;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 연출 행동 실행 인터페이스.
    /// 후속 Phase에서 연출 시스템이 구현할 때 이 인터페이스를 구현한다.
    /// </summary>
    public interface IBattleTimelineHandler
    {
        /// <summary>
        /// 단일 연출 행동을 실행한다.
        /// </summary>
        /// <param name="action">실행할 행동 데이터</param>
        void ExecuteAction(BattleActionData action);

        /// <summary>
        /// 연출 행동 그룹을 순서대로 실행한다.
        /// </summary>
        /// <param name="actions">실행할 행동 데이터 목록</param>
        void ExecuteActionGroup(List<BattleActionData> actions);
    }
}
