using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 선택 아이템/카드의 상세 정보를 표시하는 패널.
    /// 카드와 아이템 각각의 고유 필드를 구분하여 표시한다.
    /// </summary>
    public class UIItemDetail : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelTransform;

        [Header("Common Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rarityText;
        [SerializeField] private Image _artworkImage;
        [SerializeField] private UIElementBadge _elementBadge;

        [Header("Card Specific")]
        [SerializeField] private GameObject _cardInfoRoot;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _cardTypeText;

        [Header("Item Specific")]
        [SerializeField] private GameObject _itemInfoRoot;
        [SerializeField] private TextMeshProUGUI _itemTypeText;
        [SerializeField] private TextMeshProUGUI _targetStatusText;
        [SerializeField] private TextMeshProUGUI _disposableText;

        [Header("Quantity")]
        [SerializeField] private TextMeshProUGUI _quantityText;

        [Header("Animation")]
        [SerializeField] private float _slideDuration = 0.25f;
        [SerializeField] private Vector2 _hideOffset = new Vector2(300f, 0f);

        #endregion

        #region Private Fields

        private Vector2 _showPosition;
        private bool _isVisible;
        private Tweener _slideTween;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_panelTransform != null)
            {
                _showPosition = _panelTransform.anchoredPosition;
            }

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            KillTween();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 인벤토리 아이템 데이터로 상세 패널을 표시한다.
        /// DataManager를 통해 마스터 데이터를 조회한다.
        /// </summary>
        /// <param name="inventoryItem">인벤토리 아이템 데이터</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public void Show(InventoryItemData inventoryItem, DataManager dataManager)
        {
            if (inventoryItem == null)
            {
                Hide();
                return;
            }

            SetCommonInfo(inventoryItem.productName, inventoryItem.description, inventoryItem.rarity);
            SetQuantity(inventoryItem.ownStack, inventoryItem.useStack);

            if (inventoryItem.category == InventoryCategory.Card)
            {
                CardData card = dataManager.GetCard(inventoryItem.productId);
                ShowCardInfo(card, inventoryItem);
            }
            else
            {
                ItemData item = dataManager.GetItem(inventoryItem.productId);
                ShowItemInfo(item, inventoryItem);
            }

            PlayShowAnimation();
        }

        /// <summary>
        /// 카드 마스터 데이터로 상세를 표시한다.
        /// </summary>
        /// <param name="card">카드 마스터 데이터</param>
        public void ShowCard(CardData card)
        {
            if (card == null)
            {
                Hide();
                return;
            }

            SetCommonInfo(card.cardName, card.description, card.rarity);
            ShowCardSpecific(card.cost, card.cardType, card.element);

            if (_quantityText != null)
            {
                _quantityText.text = string.Empty;
            }

            PlayShowAnimation();
        }

        /// <summary>
        /// 아이템 마스터 데이터로 상세를 표시한다.
        /// </summary>
        /// <param name="item">아이템 마스터 데이터</param>
        public void ShowItem(ItemData item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            SetCommonInfo(item.itemName, item.description, item.rarity);
            ShowItemSpecific(item);

            if (_quantityText != null)
            {
                _quantityText.text = string.Empty;
            }

            PlayShowAnimation();
        }

        /// <summary>
        /// 상세 패널을 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible)
            {
                return;
            }

            PlayHideAnimation();
        }

        #endregion

        #region Private Methods — Display

        private void SetCommonInfo(string itemName, string description, Rarity rarity)
        {
            if (_nameText != null)
            {
                _nameText.text = itemName;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }

            if (_rarityText != null)
            {
                _rarityText.text = GetRarityDisplayName(rarity);
            }
        }

        private void ShowCardInfo(CardData card, InventoryItemData inventoryItem)
        {
            if (card != null)
            {
                ShowCardSpecific(card.cost, card.cardType, card.element);
            }
            else
            {
                ShowCardSpecific(inventoryItem.cardCost, inventoryItem.cardType, inventoryItem.cardElement);
            }
        }

        private void ShowCardSpecific(int cost, CardType cardType, ElementType element)
        {
            if (_cardInfoRoot != null)
            {
                _cardInfoRoot.SetActive(true);
            }

            if (_itemInfoRoot != null)
            {
                _itemInfoRoot.SetActive(false);
            }

            if (_costText != null)
            {
                _costText.text = $"코스트: {cost}";
            }

            if (_cardTypeText != null)
            {
                _cardTypeText.text = $"타입: {GetCardTypeDisplayName(cardType)}";
            }

            if (_elementBadge != null)
            {
                _elementBadge.SetElement(element);
            }
        }

        private void ShowItemInfo(ItemData item, InventoryItemData inventoryItem)
        {
            ShowItemSpecific(item);
        }

        private void ShowItemSpecific(ItemData item)
        {
            if (_cardInfoRoot != null)
            {
                _cardInfoRoot.SetActive(false);
            }

            if (_itemInfoRoot != null)
            {
                _itemInfoRoot.SetActive(true);
            }

            if (_elementBadge != null)
            {
                _elementBadge.Clear();
            }

            if (item != null)
            {
                if (_itemTypeText != null)
                {
                    _itemTypeText.text = $"타입: {GetItemTypeDisplayName(item.itemType)}";
                }

                if (_targetStatusText != null)
                {
                    _targetStatusText.text = $"효과: {item.targetStatus}";
                }

                if (_disposableText != null)
                {
                    _disposableText.text = item.isDisposable ? "소모품" : "영구 장비";
                }
            }
        }

        private void SetQuantity(int ownStack, int useStack)
        {
            if (_quantityText != null)
            {
                _quantityText.text = $"보유: {ownStack}  장비중: {useStack}";
            }
        }

        #endregion

        #region Private Methods — Animation

        private void PlayShowAnimation()
        {
            KillTween();
            _isVisible = true;

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            if (_panelTransform != null)
            {
                _panelTransform.anchoredPosition = _showPosition + _hideOffset;
                _slideTween = _panelTransform.DOAnchorPos(_showPosition, _slideDuration)
                    .SetEase(Ease.OutQuad);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, _slideDuration);
            }
        }

        private void PlayHideAnimation()
        {
            KillTween();

            if (_panelTransform != null)
            {
                Vector2 targetPos = _showPosition + _hideOffset;
                _slideTween = _panelTransform.DOAnchorPos(targetPos, _slideDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        _isVisible = false;

                        if (_panelRoot != null)
                        {
                            _panelRoot.SetActive(false);
                        }
                    });
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, _slideDuration);
            }
        }

        private void KillTween()
        {
            if (_slideTween != null && _slideTween.IsActive())
            {
                _slideTween.Kill();
                _slideTween = null;
            }
        }

        #endregion

        #region Private Methods — Display Names

        private static string GetRarityDisplayName(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Uncommon: return "언커먼";
                case Rarity.Common: return "커먼";
                case Rarity.Rare: return "레어";
                case Rarity.Unique: return "유니크";
                case Rarity.Epic: return "에픽";
                default: return rarity.ToString();
            }
        }

        private static string GetCardTypeDisplayName(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack: return "공격";
                case CardType.Defend: return "방어";
                case CardType.StatusEffect: return "상태이상";
                case CardType.InHandEffect: return "손패효과";
                default: return cardType.ToString();
            }
        }

        private static string GetItemTypeDisplayName(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment: return "장비";
                case ItemType.HasDamage: return "대미지";
                case ItemType.HasDown: return "디버프";
                default: return itemType.ToString();
            }
        }

        #endregion
    }
}
