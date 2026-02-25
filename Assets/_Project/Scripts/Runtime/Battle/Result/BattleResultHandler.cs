using System;

namespace ProjectStS.Battle
{
    /// <summary>
    /// 전투 승패 판정 클래스.
    /// 아군/적 유닛 생존 상태를 기반으로 승패를 판정하고 결과를 확정한다.
    /// </summary>
    public class BattleResultHandler
    {
        #region Events

        /// <summary>
        /// 전투 결과가 확정되었을 때 발행. (결과, 사유)
        /// </summary>
        public event Action<BattleResult, BattleEndReason> OnBattleResultDetermined;

        #endregion

        #region Public Methods

        /// <summary>
        /// 현재 전투 상태에서 승패를 판정한다.
        /// </summary>
        /// <param name="state">전투 상태</param>
        /// <returns>전투 결과. None이면 전투 계속.</returns>
        public BattleResult CheckResult(BattleState state)
        {
            if (state.AliveEnemies.Count == 0)
            {
                return BattleResult.Victory;
            }

            if (state.AliveAllies.Count == 0)
            {
                return BattleResult.Defeat;
            }

            return BattleResult.None;
        }

        /// <summary>
        /// 최종 결과를 확정하고 이벤트를 발행한다.
        /// </summary>
        /// <param name="result">전투 결과</param>
        /// <param name="reason">종료 사유</param>
        /// <param name="state">전투 상태</param>
        public void DetermineResult(BattleResult result, BattleEndReason reason, BattleState state)
        {
            state.Result = result;
            state.EndReason = reason;
            OnBattleResultDetermined?.Invoke(result, reason);
        }

        #endregion
    }
}
