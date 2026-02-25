using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 이벤트 마스터 데이터 테이블.
    /// </summary>
    [CreateAssetMenu(fileName = "EventTable", menuName = "ProjectStS/Data/EventTable")]
    public class EventTableSO : BaseTableSO<EventData>
    {
        /// <inheritdoc />
        protected override string GetEntryId(EventData entry)
        {
            return entry.id;
        }
    }
}
