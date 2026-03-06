using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 파티 편성 화면의 슬롯 1개를 표시한다.
    /// 유닛 드래그 드롭 수신, 유닛 초상화 + 장비 아이템 표시를 담당한다.
    /// </summary>
    public class UIPartySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        #region Serialized Fields

        [Header("Slot Info")]
        [SerializeField] private int _slotPosition = 1;

        [Header("Unit Display")]
        [SerializeField] private UIUnitPortrait _unitPortrait;
        [SerializeField] private UIItemIcon _itemIcon1;
        [SerializeField] private UIItemIcon _itemIcon2;

        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateRoot;
        [SerializeField] private TextMeshProUGUI _emptyLabel;

        [Header("Visual")]
        [SerializeField] private GameObject _filledStateRoot;
        [SerializeField] private TextMeshProUGUI _positionLabel;

        #endregion

        #region Private Fields

        private string _currentUnitId;
        private bool _isOccupied;

        #endregion

        #region Public Properties

        /// <summary>
        /// 이 슬롯의 파티 포지션 (1~3).
        /// </summary>
        public int SlotPosition => _slotPosition;

        /// <summary>
        /// 현재 배치된 유닛 ID. 빈 슬롯이면 null.
        /// </summary>
        public string CurrentUnitId => _currentUnitId;

        /// <summary>
        /// 슬롯에 유닛이 배치되어 있는지.
        /// </summary>
        public bool IsOccupied => _isOccupied;

        #endregion

        #region Events

        /// <summary>
        /// 유닛이 드롭되었을 때 발행한다. (슬롯 포지션, 유닛 ID)
        /// </summary>
        public event Action<int, string> OnUnitDropped;

        /// <summary>
        /// 슬롯이 클릭되었을 때 발행한다. (유닛 ID, null이면 빈 슬롯)
        /// </summary>
        public event Action<string> OnSlotClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_positionLabel != null)
            {
                _positionLabel.text = _slotPosition.ToString();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 슬롯에 유닛 데이터를 바인딩한다.
        /// </summary>
        /// <param name="owned">보유 유닛 데이터</param>
        /// <param name="unit">유닛 마스터 데이터</param>
        /// <param name="item1">장비 슬롯1 아이템 데이터 (null 허용)</param>
        /// <param name="item2">장비 슬롯2 아이템 데이터 (null 허용)</param>
        public void SetUnit(OwnedUnitData owned, UnitData unit, ItemData item1, ItemData item2)
        {
            _currentUnitId = owned != null ? owned.unitId : null;
            _isOccupied = owned != null && unit != null;

            if (_isOccupied)
            {
                ShowFilledState(unit, owned, item1, item2);
            }
            else
            {
                Clear();
            }
        }

        /// <summary>
        /// 슬롯을 빈 상태로 리셋한다.
        /// </summary>
        public void Clear()
        {
            _currentUnitId = null;
            _isOccupied = false;

            if (_unitPortrait != null)
            {
                _unitPortrait.Clear();
            }

            if (_itemIcon1 != null)
            {
                _itemIcon1.Clear();
            }

            if (_itemIcon2 != null)
            {
                _itemIcon2.Clear();
            }

            SetStateVisibility(false);
        }

        #endregion

        #region IDropHandler

        /// <summary>
        /// 유닛 드래그 드롭 수신. UIUnitPortrait에서 드래그된 유닛을 받는다.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null)
            {
                return;
            }

            UIUnitPortrait portrait = eventData.pointerDrag.GetComponent<UIUnitPortrait>();

            if (portrait == null || portrait.CurrentUnitData == null)
            {
                return;
            }

            string unitId = portrait.CurrentUnitData.id;
            OnUnitDropped?.Invoke(_slotPosition, unitId);
        }

        #endregion

        #region IPointerClickHandler

        /// <summary>
        /// 슬롯 클릭 시 유닛 선택 이벤트를 발행한다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            OnSlotClicked?.Invoke(_currentUnitId);
        }

        #endregion

        #region Private Methods

        private void ShowFilledState(UnitData unit, OwnedUnitData owned, ItemData item1, ItemData item2)
        {
            if (_unitPortrait != null)
            {
                _unitPortrait.SetData(unit, owned);
            }

            if (_itemIcon1 != null)
            {
                if (item1 != null)
                {
                    _itemIcon1.SetData(item1);
                }
                else
                {
                    _itemIcon1.Clear();
                }
            }

            if (_itemIcon2 != null)
            {
                if (item2 != null)
                {
                    _itemIcon2.SetData(item2);
                }
                else
                {
                    _itemIcon2.Clear();
                }
            }

            SetStateVisibility(true);
        }

        private void SetStateVisibility(bool isFilled)
        {
            if (_filledStateRoot != null)
            {
                _filledStateRoot.SetActive(isFilled);
            }

            if (_emptyStateRoot != null)
            {
                _emptyStateRoot.SetActive(!isFilled);
            }
        }

        #endregion
    }
}
