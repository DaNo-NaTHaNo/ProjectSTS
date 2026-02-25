using System.Collections.Generic;
using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 마스터 데이터 테이블 SO의 공통 베이스 클래스.
    /// List로 데이터를 보유하고, Dictionary 캐시로 ID 기반 O(1) 조회를 지원한다.
    /// </summary>
    public abstract class BaseTableSO<T> : ScriptableObject where T : class
    {
        #region Serialized Fields

        [SerializeField] private List<T> _entries = new List<T>();

        #endregion

        #region Private Fields

        private Dictionary<string, T> _cache;

        #endregion

        #region Public Properties

        /// <summary>
        /// 테이블 내 전체 데이터 목록.
        /// </summary>
        public List<T> Entries => _entries;

        /// <summary>
        /// 테이블 내 데이터 수.
        /// </summary>
        public int Count => _entries.Count;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            RebuildCache();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ID로 데이터를 조회한다.
        /// </summary>
        public T GetById(string id)
        {
            if (_cache == null)
            {
                RebuildCache();
            }

            if (_cache.TryGetValue(id, out T entry))
            {
                return entry;
            }

            Debug.LogWarning($"[{GetType().Name}] ID '{id}'을(를) 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// ID로 데이터를 시도 조회한다.
        /// </summary>
        public bool TryGetById(string id, out T entry)
        {
            if (_cache == null)
            {
                RebuildCache();
            }

            return _cache.TryGetValue(id, out entry);
        }

        /// <summary>
        /// 데이터를 설정하고 캐시를 재구축한다. 에디터 임포트 시 사용.
        /// </summary>
        public void SetEntries(List<T> entries)
        {
            _entries = entries;
            RebuildCache();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// 각 데이터 항목에서 ID를 추출한다. 하위 클래스에서 구현한다.
        /// </summary>
        protected abstract string GetEntryId(T entry);

        #endregion

        #region Private Methods

        private void RebuildCache()
        {
            _cache = new Dictionary<string, T>(_entries.Count);

            foreach (T entry in _entries)
            {
                string id = GetEntryId(entry);

                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!_cache.ContainsKey(id))
                {
                    _cache.Add(id, entry);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] 중복 ID: '{id}'");
                }
            }
        }

        #endregion
    }
}
