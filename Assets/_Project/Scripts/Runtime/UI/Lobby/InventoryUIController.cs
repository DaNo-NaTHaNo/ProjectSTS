using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.UI
{
    /// <summary>
    /// 인벤토리 화면 전체 컨트롤러.
    /// UIInventoryFilter, UIInventoryGrid, UIItemDetail을 관리하며,
    /// InventoryManager의 필터/정렬 API를 호출하여 그리드를 갱신한다.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sub Components")]
        [SerializeField] private UIInventoryFilter _filter;
        [SerializeField] private UIInventoryGrid _grid;
        [SerializeField] private UIItemDetail _itemDetail;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        [Header("Panel")]
        [SerializeField] private GameObject _screenRoot;

        #endregion

        #region Private Fields

        private InventoryManager _inventoryManager;
        private DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// 뒤로가기 버튼이 눌렸을 때 발행한다.
        /// </summary>
        public event Action OnBackRequested;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_filter != null)
            {
                _filter.OnFilterChanged += HandleFilterChanged;
            }

            if (_grid != null)
            {
                _grid.OnItemSelected += HandleItemSelected;
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void OnDisable()
        {
            if (_filter != null)
            {
                _filter.OnFilterChanged -= HandleFilterChanged;
            }

            if (_grid != null)
            {
                _grid.OnItemSelected -= HandleItemSelected;
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 인벤토리 화면을 초기화한다.
        /// </summary>
        /// <param name="inventoryManager">인벤토리 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public void Initialize(InventoryManager inventoryManager, DataManager dataManager)
        {
            _inventoryManager = inventoryManager;
            _dataManager = dataManager;

            if (_filter != null)
            {
                _filter.Initialize();
            }

            if (_grid != null)
            {
                _grid.Initialize();
            }

            if (_itemDetail != null)
            {
                _itemDetail.Hide();
            }

            RefreshGrid();
        }

        /// <summary>
        /// 화면을 표시한다.
        /// </summary>
        public void Show()
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(true);
            }

            RefreshGrid();
        }

        /// <summary>
        /// 화면을 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (_itemDetail != null)
            {
                _itemDetail.Hide();
            }

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 현재 필터/정렬 기준으로 그리드를 갱신한다.
        /// </summary>
        public void RefreshGrid()
        {
            if (_inventoryManager == null || _filter == null || _grid == null)
            {
                return;
            }

            InventoryCategory category = _filter.CurrentCategory;
            InventorySortType sortType = _filter.GetCurrentSortType();
            bool ascending = _filter.IsAscending();

            List<InventoryItemData> items;

            if (category == InventoryCategory.Card)
            {
                (ElementType? element, int? cost, CardType? cardType) = _filter.GetCardFilters();
                items = _inventoryManager.GetFilteredAndSortedCards(element, cost, cardType, sortType, ascending);
            }
            else
            {
                (ItemType? itemType, ItemTargetStatus? targetStatus, bool? isDisposable) = _filter.GetItemFilters();
                items = _inventoryManager.GetFilteredAndSortedItems(itemType, targetStatus, isDisposable, sortType, ascending);
            }

            _grid.SetItems(items);
        }

        #endregion

        #region Private Methods

        private void HandleFilterChanged()
        {
            RefreshGrid();

            // 필터 변경 시 상세 패널 닫기
            if (_itemDetail != null)
            {
                _itemDetail.Hide();
            }
        }

        private void HandleItemSelected(InventoryItemData item)
        {
            if (_itemDetail == null || _dataManager == null)
            {
                return;
            }

            _itemDetail.Show(item, _dataManager);
        }

        private void HandleBackClicked()
        {
            OnBackRequested?.Invoke();
        }

        #endregion
    }
}
