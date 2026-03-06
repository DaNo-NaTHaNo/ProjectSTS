using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 인벤토리 그리드 리스트. UIItemIcon 오브젝트 풀링을 사용하여
    /// 대량의 아이템을 효율적으로 표시한다.
    /// </summary>
    public class UIInventoryGrid : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Pool Settings")]
        [SerializeField] private UIItemIcon _iconPrefab;
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private int _initialPoolSize = 60;

        #endregion

        #region Private Fields

        private readonly Queue<UIItemIcon> _pool = new Queue<UIItemIcon>(60);
        private readonly List<UIItemIcon> _activeIcons = new List<UIItemIcon>(60);
        private readonly List<InventoryItemData> _currentItems = new List<InventoryItemData>(60);

        #endregion

        #region Events

        /// <summary>
        /// 아이템 아이콘이 클릭되었을 때 발행한다.
        /// </summary>
        public event Action<InventoryItemData> OnItemSelected;

        #endregion

        #region Public Methods

        /// <summary>
        /// 풀을 초기화한다. 최초 1회 호출.
        /// </summary>
        public void Initialize()
        {
            if (_iconPrefab == null || _gridContainer == null)
            {
                Debug.LogWarning("[UIInventoryGrid] Prefab 또는 Container가 설정되지 않았습니다.");
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                UIItemIcon icon = CreatePooledIcon();
                icon.gameObject.SetActive(false);
                _pool.Enqueue(icon);
            }
        }

        /// <summary>
        /// 인벤토리 아이템 목록을 그리드에 표시한다.
        /// 기존 활성 아이콘을 모두 회수한 뒤 새로 바인딩한다.
        /// </summary>
        /// <param name="items">표시할 아이템 목록</param>
        public void SetItems(List<InventoryItemData> items)
        {
            ReturnAllToPool();
            _currentItems.Clear();

            if (items == null)
            {
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                InventoryItemData item = items[i];
                _currentItems.Add(item);

                UIItemIcon icon = GetFromPool();
                icon.SetData(item);

                Button btn = icon.GetComponent<Button>();

                if (btn == null)
                {
                    btn = icon.gameObject.AddComponent<Button>();
                }

                int index = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => HandleIconClicked(index));

                icon.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 모든 활성 아이콘을 회수하고 그리드를 비운다.
        /// </summary>
        public void Clear()
        {
            ReturnAllToPool();
            _currentItems.Clear();
        }

        #endregion

        #region Private Methods — Pool

        private UIItemIcon GetFromPool()
        {
            UIItemIcon icon;

            if (_pool.Count > 0)
            {
                icon = _pool.Dequeue();
            }
            else
            {
                icon = CreatePooledIcon();
            }

            _activeIcons.Add(icon);
            return icon;
        }

        private void ReturnAllToPool()
        {
            for (int i = 0; i < _activeIcons.Count; i++)
            {
                UIItemIcon icon = _activeIcons[i];

                if (icon == null)
                {
                    continue;
                }

                icon.Clear();
                icon.gameObject.SetActive(false);

                Button btn = icon.GetComponent<Button>();

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                }

                _pool.Enqueue(icon);
            }

            _activeIcons.Clear();
        }

        private UIItemIcon CreatePooledIcon()
        {
            UIItemIcon icon = Instantiate(_iconPrefab, _gridContainer);
            return icon;
        }

        #endregion

        #region Private Methods — Event Handlers

        private void HandleIconClicked(int index)
        {
            if (index >= 0 && index < _currentItems.Count)
            {
                OnItemSelected?.Invoke(_currentItems[index]);
            }
        }

        #endregion
    }
}
