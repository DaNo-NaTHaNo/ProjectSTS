using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.UI
{
    /// <summary>
    /// 선택 유닛의 덱(최대 6카드), 스킬(1), 아이템(2) 편집 패널.
    /// PartyEditManager의 API를 호출하여 장비 변경을 처리한다.
    /// </summary>
    public class UIUnitEditPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Unit Info")]
        [SerializeField] private UIUnitPortrait _unitPortrait;
        [SerializeField] private TextMeshProUGUI _unitNameText;
        [SerializeField] private UIElementBadge _elementBadge;

        [Header("Deck Slots")]
        [SerializeField] private UICard[] _deckSlots = new UICard[6];
        [SerializeField] private TextMeshProUGUI _deckCountText;

        [Header("Skill Slot")]
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private UIElementBadge _skillElementBadge;
        [SerializeField] private Button _skillButton;

        [Header("Item Slots")]
        [SerializeField] private UIItemIcon _itemSlot1;
        [SerializeField] private UIItemIcon _itemSlot2;
        [SerializeField] private Button _itemButton1;
        [SerializeField] private Button _itemButton2;

        [Header("Card Selection")]
        [SerializeField] private GameObject _cardSelectionRoot;
        [SerializeField] private Transform _cardSelectionContainer;
        [SerializeField] private UICard _cardSelectionPrefab;

        [Header("Skill Selection")]
        [SerializeField] private GameObject _skillSelectionRoot;
        [SerializeField] private Transform _skillSelectionContainer;

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.2f;

        #endregion

        #region Private Fields

        private string _currentUnitId;
        private OwnedUnitData _currentOwnedUnit;
        private PartyEditManager _partyEdit;
        private DataManager _dataManager;
        private int _editingCardIndex = -1;
        private List<UICard> _cardSelectionInstances = new List<UICard>(16);
        private List<GameObject> _skillSelectionInstances = new List<GameObject>(8);

        #endregion

        #region Events

        /// <summary>
        /// 편집 패널이 닫힐 때 발행한다.
        /// </summary>
        public event Action OnPanelClosed;

        #endregion

        #region Public Methods

        /// <summary>
        /// 유닛 편집 패널을 초기화하고 표시한다.
        /// </summary>
        /// <param name="owned">보유 유닛 데이터</param>
        /// <param name="partyEdit">파티 편성 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public void Initialize(OwnedUnitData owned, PartyEditManager partyEdit, DataManager dataManager)
        {
            _currentOwnedUnit = owned;
            _currentUnitId = owned.unitId;
            _partyEdit = partyEdit;
            _dataManager = dataManager;

            UnitData unitData = dataManager.GetUnit(owned.unitId);

            SetupUnitInfo(unitData, owned);
            RefreshDeck(owned.unitId);
            RefreshSkill(owned.unitId);
            RefreshItems(owned.unitId);
            HideSelectionPanels();

            ShowPanel();
        }

        /// <summary>
        /// 덱 표시를 갱신한다.
        /// </summary>
        /// <param name="unitId">갱신 대상 유닛 ID</param>
        public void RefreshDeck(string unitId)
        {
            if (unitId != _currentUnitId || _partyEdit == null)
            {
                return;
            }

            List<string> deck = _partyEdit.GetEditedDeck(unitId);

            for (int i = 0; i < _deckSlots.Length; i++)
            {
                if (_deckSlots[i] == null)
                {
                    continue;
                }

                if (i < deck.Count)
                {
                    CardData card = _dataManager.GetCard(deck[i]);
                    _deckSlots[i].SetData(card);
                    _deckSlots[i].SetInteractable(true);
                }
                else
                {
                    _deckSlots[i].Clear();
                    _deckSlots[i].SetInteractable(false);
                }
            }

            if (_deckCountText != null)
            {
                _deckCountText.text = $"{deck.Count}/6";
            }
        }

        /// <summary>
        /// 스킬 표시를 갱신한다.
        /// </summary>
        /// <param name="unitId">갱신 대상 유닛 ID</param>
        public void RefreshSkill(string unitId)
        {
            if (unitId != _currentUnitId || _partyEdit == null)
            {
                return;
            }

            string skillId = _partyEdit.GetEditedSkill(unitId);

            if (string.IsNullOrEmpty(skillId))
            {
                if (_skillNameText != null)
                {
                    _skillNameText.text = "스킬 없음";
                }

                if (_skillElementBadge != null)
                {
                    _skillElementBadge.Clear();
                }
            }
            else
            {
                SkillData skill = _dataManager.GetSkill(skillId);

                if (skill != null)
                {
                    if (_skillNameText != null)
                    {
                        _skillNameText.text = skill.skillName;
                    }

                    if (_skillElementBadge != null)
                    {
                        _skillElementBadge.SetElement(skill.element);
                    }
                }
            }
        }

        /// <summary>
        /// 아이템 슬롯 표시를 갱신한다.
        /// </summary>
        /// <param name="unitId">갱신 대상 유닛 ID</param>
        public void RefreshItems(string unitId)
        {
            if (unitId != _currentUnitId || _partyEdit == null)
            {
                return;
            }

            (string item1Id, string item2Id) = _partyEdit.GetEquippedItems(unitId);

            RefreshItemSlot(_itemSlot1, item1Id);
            RefreshItemSlot(_itemSlot2, item2Id);
        }

        /// <summary>
        /// 패널을 숨긴다.
        /// </summary>
        public void Hide()
        {
            HideSelectionPanels();

            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .OnComplete(() =>
                    {
                        if (_panelRoot != null)
                        {
                            _panelRoot.SetActive(false);
                        }
                    });
            }
            else if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }

            _currentUnitId = null;
            _currentOwnedUnit = null;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindDeckSlotEvents();
            BindButtonEvents();
        }

        private void OnDisable()
        {
            UnbindDeckSlotEvents();
            UnbindButtonEvents();
        }

        #endregion

        #region Private Methods — Setup

        private void SetupUnitInfo(UnitData unit, OwnedUnitData owned)
        {
            if (_unitPortrait != null)
            {
                _unitPortrait.SetData(unit, owned);
            }

            if (_unitNameText != null)
            {
                _unitNameText.text = unit != null ? unit.unitName : owned.unitId;
            }

            if (_elementBadge != null && unit != null)
            {
                _elementBadge.SetElement(unit.element);
            }
        }

        #endregion

        #region Private Methods — Panel Animation

        private void ShowPanel()
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, _fadeDuration);
            }
        }

        #endregion

        #region Private Methods — Event Binding

        private void BindDeckSlotEvents()
        {
            for (int i = 0; i < _deckSlots.Length; i++)
            {
                if (_deckSlots[i] != null)
                {
                    int index = i;
                    _deckSlots[i].OnClicked += card => HandleCardSlotClicked(index);
                }
            }
        }

        private void UnbindDeckSlotEvents()
        {
            for (int i = 0; i < _deckSlots.Length; i++)
            {
                if (_deckSlots[i] != null)
                {
                    _deckSlots[i].OnClicked -= null;
                }
            }
        }

        private void BindButtonEvents()
        {
            if (_skillButton != null)
            {
                _skillButton.onClick.AddListener(HandleSkillClicked);
            }

            if (_itemButton1 != null)
            {
                _itemButton1.onClick.AddListener(() => HandleItemSlotClicked(1));
            }

            if (_itemButton2 != null)
            {
                _itemButton2.onClick.AddListener(() => HandleItemSlotClicked(2));
            }
        }

        private void UnbindButtonEvents()
        {
            if (_skillButton != null)
            {
                _skillButton.onClick.RemoveListener(HandleSkillClicked);
            }

            if (_itemButton1 != null)
            {
                _itemButton1.onClick.RemoveAllListeners();
            }

            if (_itemButton2 != null)
            {
                _itemButton2.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region Private Methods — Card Selection

        private void HandleCardSlotClicked(int index)
        {
            if (_partyEdit == null || string.IsNullOrEmpty(_currentUnitId))
            {
                return;
            }

            _editingCardIndex = index;
            ShowCardSelection();
        }

        private void ShowCardSelection()
        {
            ClearCardSelection();

            if (_cardSelectionRoot == null || _cardSelectionPrefab == null)
            {
                return;
            }

            List<InventoryItemData> available = _partyEdit.GetAvailableCards(_currentUnitId);

            for (int i = 0; i < available.Count; i++)
            {
                CardData card = _dataManager.GetCard(available[i].productId);

                if (card == null)
                {
                    continue;
                }

                UICard instance = Instantiate(_cardSelectionPrefab, _cardSelectionContainer);
                instance.SetData(card);
                instance.OnClicked += HandleCardSelected;
                _cardSelectionInstances.Add(instance);
            }

            _cardSelectionRoot.SetActive(true);

            if (_skillSelectionRoot != null)
            {
                _skillSelectionRoot.SetActive(false);
            }
        }

        private void HandleCardSelected(UICard selectedCard)
        {
            if (selectedCard.CurrentCardData == null || _partyEdit == null)
            {
                return;
            }

            List<string> currentDeck = _partyEdit.GetEditedDeck(_currentUnitId);

            if (_editingCardIndex >= 0 && _editingCardIndex < currentDeck.Count)
            {
                currentDeck[_editingCardIndex] = selectedCard.CurrentCardData.id;
            }
            else if (currentDeck.Count < 6)
            {
                currentDeck.Add(selectedCard.CurrentCardData.id);
            }

            _partyEdit.TrySetDeck(_currentUnitId, currentDeck);
            HideSelectionPanels();
        }

        private void ClearCardSelection()
        {
            for (int i = 0; i < _cardSelectionInstances.Count; i++)
            {
                if (_cardSelectionInstances[i] != null)
                {
                    _cardSelectionInstances[i].OnClicked -= HandleCardSelected;
                    Destroy(_cardSelectionInstances[i].gameObject);
                }
            }

            _cardSelectionInstances.Clear();
        }

        #endregion

        #region Private Methods — Skill Selection

        private void HandleSkillClicked()
        {
            if (_partyEdit == null || string.IsNullOrEmpty(_currentUnitId))
            {
                return;
            }

            ShowSkillSelection();
        }

        private void ShowSkillSelection()
        {
            ClearSkillSelection();

            if (_skillSelectionRoot == null || _skillSelectionContainer == null)
            {
                return;
            }

            List<SkillData> available = _partyEdit.GetAvailableSkills(_currentUnitId);

            // 해제 옵션
            CreateSkillOption("스킬 해제", () =>
            {
                _partyEdit.TrySetSkill(_currentUnitId, "");
                HideSelectionPanels();
            });

            for (int i = 0; i < available.Count; i++)
            {
                SkillData skill = available[i];
                CreateSkillOption(skill.skillName, () =>
                {
                    _partyEdit.TrySetSkill(_currentUnitId, skill.id);
                    HideSelectionPanels();
                });
            }

            _skillSelectionRoot.SetActive(true);

            if (_cardSelectionRoot != null)
            {
                _cardSelectionRoot.SetActive(false);
            }
        }

        private void CreateSkillOption(string label, Action onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Button));
            go.transform.SetParent(_skillSelectionContainer, false);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);

            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14f;
            text.alignment = TextAlignmentOptions.Center;

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            _skillSelectionInstances.Add(go);
        }

        private void ClearSkillSelection()
        {
            for (int i = 0; i < _skillSelectionInstances.Count; i++)
            {
                if (_skillSelectionInstances[i] != null)
                {
                    Destroy(_skillSelectionInstances[i]);
                }
            }

            _skillSelectionInstances.Clear();
        }

        #endregion

        #region Private Methods — Item

        private void HandleItemSlotClicked(int slot)
        {
            if (_partyEdit == null || string.IsNullOrEmpty(_currentUnitId))
            {
                return;
            }

            (string item1Id, string item2Id) = _partyEdit.GetEquippedItems(_currentUnitId);
            string currentItemId = (slot == 1) ? item1Id : item2Id;

            if (!string.IsNullOrEmpty(currentItemId))
            {
                // 아이템이 있으면 해제
                _partyEdit.TryRemoveItem(_currentUnitId, slot);
            }
        }

        private void RefreshItemSlot(UIItemIcon icon, string itemId)
        {
            if (icon == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(itemId))
            {
                icon.Clear();
                return;
            }

            ItemData item = _dataManager.GetItem(itemId);

            if (item != null)
            {
                icon.SetData(item);
            }
            else
            {
                icon.Clear();
            }
        }

        #endregion

        #region Private Methods — Utility

        private void HideSelectionPanels()
        {
            if (_cardSelectionRoot != null)
            {
                _cardSelectionRoot.SetActive(false);
            }

            if (_skillSelectionRoot != null)
            {
                _skillSelectionRoot.SetActive(false);
            }

            _editingCardIndex = -1;
        }

        #endregion
    }
}
