using System.Collections.Generic;
using UnityEngine;

namespace ProjectStS.Data
{
    /// <summary>
    /// 속성 상성 보정치 데이터 테이블.
    /// 공격 속성과 피격 속성의 조합으로 대미지 배율 보정치를 조회한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ElementAffinityTable", menuName = "ProjectStS/Data/ElementAffinityTable")]
    public class ElementAffinityTableSO : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField] private List<ElementAffinityData> _entries = new List<ElementAffinityData>();

        #endregion

        #region Private Fields

        private Dictionary<(ElementType, ElementType), float> _cache;

        #endregion

        #region Public Properties

        /// <summary>
        /// 테이블 내 전체 데이터 목록.
        /// </summary>
        public List<ElementAffinityData> Entries => _entries;

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
        public void SetEntries(List<ElementAffinityData> entries)
        {
            _entries = entries;
            RebuildCache();
        }

        /// <summary>
        /// 공격 속성과 피격 속성에 해당하는 대미지 보정 배율을 반환한다.
        /// 정의되지 않은 조합은 1.0f를 반환한다.
        /// </summary>
        public float GetModifier(ElementType attack, ElementType target)
        {
            if (_cache == null)
            {
                RebuildCache();
            }

            if (_cache.TryGetValue((attack, target), out float modifier))
            {
                return modifier;
            }

            return 1.0f;
        }

        #endregion

        #region Private Methods

        private void RebuildCache()
        {
            _cache = new Dictionary<(ElementType, ElementType), float>(_entries.Count);

            foreach (ElementAffinityData entry in _entries)
            {
                var key = (entry.attackElement, entry.targetElement);

                if (!_cache.ContainsKey(key))
                {
                    _cache.Add(key, entry.modValue);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] 중복 키: ({entry.attackElement}, {entry.targetElement})");
                }
            }
        }

        #endregion
    }
}
