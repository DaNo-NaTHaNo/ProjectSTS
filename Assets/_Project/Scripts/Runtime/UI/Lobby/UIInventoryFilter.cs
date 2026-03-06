using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 인벤토리 필터/정렬 컨트롤.
    /// 카드/아이템 탭 전환, 속성·코스트·타입 필터 드롭다운, 정렬 드롭다운을 관리한다.
    /// </summary>
    public class UIInventoryFilter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Tab Buttons")]
        [SerializeField] private Button _cardTabButton;
        [SerializeField] private Button _itemTabButton;
        [SerializeField] private Image _cardTabHighlight;
        [SerializeField] private Image _itemTabHighlight;

        [Header("Card Filters")]
        [SerializeField] private GameObject _cardFilterRoot;
        [SerializeField] private TMP_Dropdown _elementDropdown;
        [SerializeField] private TMP_Dropdown _costDropdown;
        [SerializeField] private TMP_Dropdown _cardTypeDropdown;

        [Header("Item Filters")]
        [SerializeField] private GameObject _itemFilterRoot;
        [SerializeField] private TMP_Dropdown _itemTypeDropdown;
        [SerializeField] private TMP_Dropdown _targetStatusDropdown;

        [Header("Sort")]
        [SerializeField] private TMP_Dropdown _sortDropdown;
        [SerializeField] private Button _sortOrderButton;
        [SerializeField] private TextMeshProUGUI _sortOrderLabel;

        [Header("Visual")]
        [SerializeField] private Color _activeTabColor = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
        [SerializeField] private Color _inactiveTabColor = new Color32(0x42, 0x42, 0x42, 0xFF);

        #endregion

        #region Private Fields

        private InventoryCategory _currentCategory = InventoryCategory.Card;
        private bool _ascending = true;

        private static readonly string[] ELEMENT_OPTIONS = { "전체", "바람", "불", "땅", "물", "빛", "어둠", "무" };
        private static readonly string[] COST_OPTIONS = { "전체", "0", "1", "2", "3", "4", "5+" };
        private static readonly string[] CARD_TYPE_OPTIONS = { "전체", "공격", "방어", "상태이상", "손패효과" };
        private static readonly string[] ITEM_TYPE_OPTIONS = { "전체", "장비", "대미지", "디버프" };
        private static readonly string[] SORT_OPTIONS = { "이름", "속성", "레어도", "코스트", "수량" };

        #endregion

        #region Events

        /// <summary>
        /// 필터 또는 정렬 조건이 변경되었을 때 발행한다.
        /// </summary>
        public event Action OnFilterChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 선택된 카테고리.
        /// </summary>
        public InventoryCategory CurrentCategory => _currentCategory;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_cardTabButton != null)
            {
                _cardTabButton.onClick.AddListener(HandleCardTabClicked);
            }

            if (_itemTabButton != null)
            {
                _itemTabButton.onClick.AddListener(HandleItemTabClicked);
            }

            if (_sortOrderButton != null)
            {
                _sortOrderButton.onClick.AddListener(HandleSortOrderToggle);
            }

            BindDropdownEvents();
        }

        private void OnDisable()
        {
            if (_cardTabButton != null)
            {
                _cardTabButton.onClick.RemoveListener(HandleCardTabClicked);
            }

            if (_itemTabButton != null)
            {
                _itemTabButton.onClick.RemoveListener(HandleItemTabClicked);
            }

            if (_sortOrderButton != null)
            {
                _sortOrderButton.onClick.RemoveListener(HandleSortOrderToggle);
            }

            UnbindDropdownEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 필터 UI를 초기화한다. 드롭다운 옵션을 설정하고 카드 탭으로 시작한다.
        /// </summary>
        public void Initialize()
        {
            PopulateDropdown(_elementDropdown, ELEMENT_OPTIONS);
            PopulateDropdown(_costDropdown, COST_OPTIONS);
            PopulateDropdown(_cardTypeDropdown, CARD_TYPE_OPTIONS);
            PopulateDropdown(_itemTypeDropdown, ITEM_TYPE_OPTIONS);
            PopulateDropdown(_sortDropdown, SORT_OPTIONS);

            _currentCategory = InventoryCategory.Card;
            _ascending = true;
            UpdateTabVisuals();
            UpdateFilterVisibility();
            UpdateSortOrderLabel();
        }

        /// <summary>
        /// 현재 정렬 기준을 반환한다.
        /// </summary>
        public InventorySortType GetCurrentSortType()
        {
            if (_sortDropdown == null)
            {
                return InventorySortType.Name;
            }

            switch (_sortDropdown.value)
            {
                case 0: return InventorySortType.Name;
                case 1: return InventorySortType.Element;
                case 2: return InventorySortType.Rarity;
                case 3: return InventorySortType.Cost;
                case 4: return InventorySortType.Quantity;
                default: return InventorySortType.Name;
            }
        }

        /// <summary>
        /// 오름차순 여부를 반환한다.
        /// </summary>
        public bool IsAscending()
        {
            return _ascending;
        }

        /// <summary>
        /// 현재 카드 필터 조건을 반환한다.
        /// </summary>
        /// <returns>(속성, 코스트, 카드타입) — null이면 해당 필터 미적용</returns>
        public (ElementType?, int?, CardType?) GetCardFilters()
        {
            ElementType? element = GetSelectedElement();
            int? cost = GetSelectedCost();
            CardType? cardType = GetSelectedCardType();

            return (element, cost, cardType);
        }

        /// <summary>
        /// 현재 아이템 필터 조건을 반환한다.
        /// </summary>
        /// <returns>(아이템타입, 적용스탯, 소모여부) — null이면 해당 필터 미적용</returns>
        public (ItemType?, ItemTargetStatus?, bool?) GetItemFilters()
        {
            ItemType? itemType = GetSelectedItemType();

            return (itemType, null, null);
        }

        #endregion

        #region Private Methods — Tab

        private void HandleCardTabClicked()
        {
            if (_currentCategory == InventoryCategory.Card)
            {
                return;
            }

            _currentCategory = InventoryCategory.Card;
            UpdateTabVisuals();
            UpdateFilterVisibility();
            OnFilterChanged?.Invoke();
        }

        private void HandleItemTabClicked()
        {
            if (_currentCategory == InventoryCategory.Item)
            {
                return;
            }

            _currentCategory = InventoryCategory.Item;
            UpdateTabVisuals();
            UpdateFilterVisibility();
            OnFilterChanged?.Invoke();
        }

        private void UpdateTabVisuals()
        {
            bool isCard = _currentCategory == InventoryCategory.Card;

            if (_cardTabHighlight != null)
            {
                _cardTabHighlight.color = isCard ? _activeTabColor : _inactiveTabColor;
            }

            if (_itemTabHighlight != null)
            {
                _itemTabHighlight.color = isCard ? _inactiveTabColor : _activeTabColor;
            }
        }

        private void UpdateFilterVisibility()
        {
            bool isCard = _currentCategory == InventoryCategory.Card;

            if (_cardFilterRoot != null)
            {
                _cardFilterRoot.SetActive(isCard);
            }

            if (_itemFilterRoot != null)
            {
                _itemFilterRoot.SetActive(!isCard);
            }
        }

        #endregion

        #region Private Methods — Sort

        private void HandleSortOrderToggle()
        {
            _ascending = !_ascending;
            UpdateSortOrderLabel();
            OnFilterChanged?.Invoke();
        }

        private void UpdateSortOrderLabel()
        {
            if (_sortOrderLabel != null)
            {
                _sortOrderLabel.text = _ascending ? "▲" : "▼";
            }
        }

        #endregion

        #region Private Methods — Dropdown Events

        private void BindDropdownEvents()
        {
            if (_elementDropdown != null)
            {
                _elementDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            if (_costDropdown != null)
            {
                _costDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            if (_cardTypeDropdown != null)
            {
                _cardTypeDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            if (_itemTypeDropdown != null)
            {
                _itemTypeDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }
        }

        private void UnbindDropdownEvents()
        {
            if (_elementDropdown != null)
            {
                _elementDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }

            if (_costDropdown != null)
            {
                _costDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }

            if (_cardTypeDropdown != null)
            {
                _cardTypeDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }

            if (_itemTypeDropdown != null)
            {
                _itemTypeDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }
        }

        private void HandleDropdownChanged(int value)
        {
            OnFilterChanged?.Invoke();
        }

        #endregion

        #region Private Methods — Filter Parsing

        private ElementType? GetSelectedElement()
        {
            if (_elementDropdown == null || _elementDropdown.value == 0)
            {
                return null;
            }

            switch (_elementDropdown.value)
            {
                case 1: return ElementType.Sword;
                case 2: return ElementType.Baton;
                case 3: return ElementType.Medal;
                case 4: return ElementType.Grail;
                case 5: return ElementType.Sola;
                case 6: return ElementType.Luna;
                case 7: return ElementType.Wild;
                default: return null;
            }
        }

        private int? GetSelectedCost()
        {
            if (_costDropdown == null || _costDropdown.value == 0)
            {
                return null;
            }

            int costIndex = _costDropdown.value - 1;
            return costIndex >= 5 ? null : (int?)costIndex;
        }

        private CardType? GetSelectedCardType()
        {
            if (_cardTypeDropdown == null || _cardTypeDropdown.value == 0)
            {
                return null;
            }

            switch (_cardTypeDropdown.value)
            {
                case 1: return CardType.Attack;
                case 2: return CardType.Defend;
                case 3: return CardType.StatusEffect;
                case 4: return CardType.InHandEffect;
                default: return null;
            }
        }

        private ItemType? GetSelectedItemType()
        {
            if (_itemTypeDropdown == null || _itemTypeDropdown.value == 0)
            {
                return null;
            }

            switch (_itemTypeDropdown.value)
            {
                case 1: return ItemType.Equipment;
                case 2: return ItemType.HasDamage;
                case 3: return ItemType.HasDown;
                default: return null;
            }
        }

        #endregion

        #region Private Methods — Utility

        private void PopulateDropdown(TMP_Dropdown dropdown, string[] options)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            var optionList = new System.Collections.Generic.List<string>(options.Length);

            for (int i = 0; i < options.Length; i++)
            {
                optionList.Add(options[i]);
            }

            dropdown.AddOptions(optionList);
            dropdown.value = 0;
        }

        #endregion
    }
}
