using System.Collections.Generic;
using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 레어도별 기본 드랍율 데이터 테이블.
    /// 카테고리와 레어도 조합으로 드랍율을 조회한다.
    /// </summary>
    [CreateAssetMenu(fileName = "DropRateTable", menuName = "ProjectStS/Data/DropRateTable")]
    public class DropRateTableSO : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField] private List<DropRateData> _entries = new List<DropRateData>();

        #endregion

        #region Private Fields

        private Dictionary<(DropRateCategory, Rarity), float> _cache;

        #endregion

        #region Public Properties

        /// <summary>
        /// 테이블 내 전체 데이터 목록.
        /// </summary>
        public List<DropRateData> Entries => _entries;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            RebuildCache();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 데이터를 설정하고 캐시를 재구축한다. 에디터 임포트 시 사용.
        /// </summary>
        public void SetEntries(List<DropRateData> entries)
        {
            _entries = entries;
            RebuildCache();
        }

        /// <summary>
        /// 카테고리와 레어도에 해당하는 드랍율을 반환한다.
        /// 정의되지 않은 조합은 0f를 반환한다.
        /// </summary>
        public float GetDropValue(DropRateCategory category, Rarity rarity)
        {
            if (_cache == null)
            {
                RebuildCache();
            }

            if (_cache.TryGetValue((category, rarity), out float dropValue))
            {
                return dropValue;
            }

            return 0f;
        }

        #endregion

        #region Private Methods

        private void RebuildCache()
        {
            _cache = new Dictionary<(DropRateCategory, Rarity), float>(_entries.Count);

            foreach (DropRateData entry in _entries)
            {
                var key = (entry.category, entry.rarity);

                if (!_cache.ContainsKey(key))
                {
                    _cache.Add(key, entry.dropValue);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] 중복 키: ({entry.category}, {entry.rarity})");
                }
            }
        }

        #endregion
    }
}
