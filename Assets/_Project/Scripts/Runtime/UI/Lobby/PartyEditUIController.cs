using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectStS.Data;
using ProjectStS.Meta;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 파티 편성 화면 전체 컨트롤러.
    /// 파티 슬롯 3개, 보유 유닛 리스트, 유닛 편집 패널을 관리한다.
    /// PartyEditManager의 이벤트를 구독하여 UI를 갱신한다.
    /// </summary>
    public class PartyEditUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Party Slots")]
        [SerializeField] private UIPartySlot[] _partySlots = new UIPartySlot[3];

        [Header("Unit List")]
        [SerializeField] private ScrollRect _unitListScrollRect;
        [SerializeField] private Transform _unitListContainer;
        [SerializeField] private UIUnitPortrait _unitPortraitPrefab;

        [Header("Unit Edit Panel")]
        [SerializeField] private UIUnitEditPanel _unitEditPanel;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        [Header("Panel")]
        [SerializeField] private GameObject _screenRoot;

        #endregion

        #region Private Fields

        private PlayerDataManager _playerData;
        private PartyEditManager _partyEdit;
        private DataManager _dataManager;
        private readonly List<UIUnitPortrait> _unitPortraitInstances = new List<UIUnitPortrait>(10);
        private string _selectedUnitId;

        #endregion

        #region Events

        /// <summary>
        /// 뒤로가기 버튼이 눌렸을 때 발행한다.
        /// </summary>
        public event Action OnBackRequested;

        #endregion

        #region Public Methods

        /// <summary>
        /// 파티 편성 화면을 초기화한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="partyEdit">파티 편성 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public void Initialize(PlayerDataManager playerData, PartyEditManager partyEdit, DataManager dataManager)
        {
            _playerData = playerData;
            _partyEdit = partyEdit;
            _dataManager = dataManager;

            RefreshPartySlots();
            RefreshUnitList();

            if (_unitEditPanel != null)
            {
                _unitEditPanel.Hide();
            }
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

            RefreshPartySlots();
            RefreshUnitList();
        }

        /// <summary>
        /// 화면을 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (_unitEditPanel != null)
            {
                _unitEditPanel.Hide();
            }

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 보유 유닛 리스트를 갱신한다.
        /// </summary>
        public void RefreshUnitList()
        {
            ClearUnitList();

            if (_playerData == null || _unitPortraitPrefab == null || _unitListContainer == null)
            {
                return;
            }

            List<OwnedUnitData> ownedUnits = _playerData.GetOwnedUnits();

            for (int i = 0; i < ownedUnits.Count; i++)
            {
                OwnedUnitData owned = ownedUnits[i];
                UnitData unit = _dataManager.GetUnit(owned.unitId);

                if (unit == null)
                {
                    continue;
                }

                UIUnitPortrait portrait = Instantiate(_unitPortraitPrefab, _unitListContainer);
                portrait.SetData(unit, owned);

                // 파티에 포함된 유닛은 하이라이트
                portrait.SetHighlight(owned.partyPosition > 0);

                // 클릭 이벤트 바인딩
                Button btn = portrait.GetComponent<Button>();

                if (btn == null)
                {
                    btn = portrait.gameObject.AddComponent<Button>();
                }

                string unitId = owned.unitId;
                btn.onClick.AddListener(() => HandleUnitSelected(unitId));

                _unitPortraitInstances.Add(portrait);
            }
        }

        /// <summary>
        /// 파티 슬롯을 갱신한다.
        /// </summary>
        public void RefreshPartySlots()
        {
            if (_playerData == null || _dataManager == null)
            {
                return;
            }

            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();

            for (int i = 0; i < _partySlots.Length; i++)
            {
                if (_partySlots[i] == null)
                {
                    continue;
                }

                int position = _partySlots[i].SlotPosition;
                OwnedUnitData member = FindMemberByPosition(partyMembers, position);

                if (member != null)
                {
                    UnitData unitData = _dataManager.GetUnit(member.unitId);
                    (string item1Id, string item2Id) = _partyEdit.GetEquippedItems(member.unitId);

                    ItemData item1 = !string.IsNullOrEmpty(item1Id) ? _dataManager.GetItem(item1Id) : null;
                    ItemData item2 = !string.IsNullOrEmpty(item2Id) ? _dataManager.GetItem(item2Id) : null;

                    _partySlots[i].SetUnit(member, unitData, item1, item2);
                }
                else
                {
                    _partySlots[i].Clear();
                }
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindSlotEvents();

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void OnDisable()
        {
            UnbindSlotEvents();

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        #endregion

        #region Private Methods — Event Binding

        private void BindSlotEvents()
        {
            for (int i = 0; i < _partySlots.Length; i++)
            {
                if (_partySlots[i] != null)
                {
                    _partySlots[i].OnUnitDropped += HandleUnitDroppedOnSlot;
                    _partySlots[i].OnSlotClicked += HandleSlotClicked;
                }
            }
        }

        private void UnbindSlotEvents()
        {
            for (int i = 0; i < _partySlots.Length; i++)
            {
                if (_partySlots[i] != null)
                {
                    _partySlots[i].OnUnitDropped -= HandleUnitDroppedOnSlot;
                    _partySlots[i].OnSlotClicked -= HandleSlotClicked;
                }
            }
        }

        /// <summary>
        /// PartyEditManager 이벤트를 구독한다. LobbyUIController에서 호출.
        /// </summary>
        public void BindManagerEvents(PartyEditManager partyEdit)
        {
            if (partyEdit == null)
            {
                return;
            }

            partyEdit.OnDeckChanged += HandleDeckChanged;
            partyEdit.OnSkillChanged += HandleSkillChanged;
            partyEdit.OnItemChanged += HandleItemChanged;
        }

        /// <summary>
        /// PartyEditManager 이벤트 구독을 해제한다. LobbyUIController에서 호출.
        /// </summary>
        public void UnbindManagerEvents(PartyEditManager partyEdit)
        {
            if (partyEdit == null)
            {
                return;
            }

            partyEdit.OnDeckChanged -= HandleDeckChanged;
            partyEdit.OnSkillChanged -= HandleSkillChanged;
            partyEdit.OnItemChanged -= HandleItemChanged;
        }

        #endregion

        #region Private Methods — Handlers

        private void HandleUnitSelected(string unitId)
        {
            _selectedUnitId = unitId;

            if (_unitEditPanel == null || _partyEdit == null || _dataManager == null)
            {
                return;
            }

            OwnedUnitData owned = _playerData.GetOwnedUnit(unitId);

            if (owned == null)
            {
                return;
            }

            _unitEditPanel.Initialize(owned, _partyEdit, _dataManager);
        }

        private void HandleUnitDroppedOnSlot(int position, string unitId)
        {
            if (_partyEdit == null)
            {
                return;
            }

            bool success = _partyEdit.TryAssignToParty(unitId, position);

            if (success)
            {
                RefreshPartySlots();
                RefreshUnitList();
            }
        }

        private void HandleSlotClicked(string unitId)
        {
            if (!string.IsNullOrEmpty(unitId))
            {
                HandleUnitSelected(unitId);
            }
        }

        private void HandleBackClicked()
        {
            OnBackRequested?.Invoke();
        }

        private void HandleDeckChanged(string unitId)
        {
            if (_unitEditPanel != null && unitId == _selectedUnitId)
            {
                _unitEditPanel.RefreshDeck(unitId);
            }
        }

        private void HandleSkillChanged(string unitId)
        {
            if (_unitEditPanel != null && unitId == _selectedUnitId)
            {
                _unitEditPanel.RefreshSkill(unitId);
            }
        }

        private void HandleItemChanged(string unitId)
        {
            if (_unitEditPanel != null && unitId == _selectedUnitId)
            {
                _unitEditPanel.RefreshItems(unitId);
            }

            RefreshPartySlots();
        }

        #endregion

        #region Private Methods — Utility

        private void ClearUnitList()
        {
            for (int i = 0; i < _unitPortraitInstances.Count; i++)
            {
                if (_unitPortraitInstances[i] != null)
                {
                    Destroy(_unitPortraitInstances[i].gameObject);
                }
            }

            _unitPortraitInstances.Clear();
        }

        private static OwnedUnitData FindMemberByPosition(List<OwnedUnitData> members, int position)
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].partyPosition == position)
                {
                    return members[i];
                }
            }

            return null;
        }

        #endregion
    }
}
