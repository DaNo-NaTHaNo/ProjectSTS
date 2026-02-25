using UnityEngine;
using ProjectStS.Data;

namespace ProjectStS.Battle
{
    /// <summary>
    /// StatusEffectData의 런타임 래퍼.
    /// 전투 중 유닛에 적용된 상태이상의 현재 스택, 남은 턴, 부여자 정보를 추적한다.
    /// </summary>
    public class ActiveStatusEffect
    {
        #region Private Fields

        private int _currentStacks;
        private int _remainingDuration;

        #endregion

        #region Public Properties

        /// <summary>
        /// 마스터 데이터 참조.
        /// </summary>
        public StatusEffectData BaseData { get; }

        /// <summary>
        /// 현재 중첩 스택 수.
        /// </summary>
        public int CurrentStacks => _currentStacks;

        /// <summary>
        /// 남은 지속 턴 수. 0이면 만료 대상.
        /// </summary>
        public int RemainingDuration => _remainingDuration;

        /// <summary>
        /// 이 상태이상을 부여한 유닛의 ID.
        /// </summary>
        public string SourceUnitId { get; }

        /// <summary>
        /// 만료 여부 (duration이 0 이하).
        /// </summary>
        public bool IsExpired => _remainingDuration <= 0;

        #endregion

        #region Constructor

        /// <summary>
        /// ActiveStatusEffect를 생성한다.
        /// </summary>
        /// <param name="baseData">상태이상 마스터 데이터</param>
        /// <param name="initialStacks">초기 스택 수</param>
        /// <param name="sourceUnitId">부여자 유닛 ID</param>
        public ActiveStatusEffect(StatusEffectData baseData, int initialStacks, string sourceUnitId)
        {
            BaseData = baseData;
            _currentStacks = Mathf.Clamp(initialStacks, 1, baseData.isStackable ? baseData.maxStacks : 1);
            _remainingDuration = baseData.duration;
            SourceUnitId = sourceUnitId;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스택을 추가한다. isStackable이 true일 때만 maxStacks까지 누적된다.
        /// </summary>
        /// <param name="amount">추가할 스택 수</param>
        public void AddStacks(int amount)
        {
            if (!BaseData.isStackable)
            {
                return;
            }

            _currentStacks = Mathf.Min(_currentStacks + amount, BaseData.maxStacks);
        }

        /// <summary>
        /// 지속 시간을 갱신한다 (같은 상태이상 재적용 시).
        /// </summary>
        public void RefreshDuration()
        {
            _remainingDuration = BaseData.duration;
        }

        /// <summary>
        /// 턴 종료 시 duration을 1 감소시킨다.
        /// </summary>
        /// <returns>만료 여부 (true이면 제거 대상)</returns>
        public bool TickDuration()
        {
            _remainingDuration = Mathf.Max(_remainingDuration - 1, 0);
            return _remainingDuration <= 0;
        }

        /// <summary>
        /// 소모형 상태이상의 스택을 소모한다.
        /// isExpendable이 true이고 스택이 expendCount 이상일 때만 성공한다.
        /// </summary>
        /// <returns>소모 성공 여부</returns>
        public bool TryExpend()
        {
            if (!BaseData.isExpendable)
            {
                return false;
            }

            if (_currentStacks < BaseData.expendCount)
            {
                return false;
            }

            _currentStacks -= BaseData.expendCount;
            return true;
        }

        #endregion
    }
}
