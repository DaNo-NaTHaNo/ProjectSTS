using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 카드 한 장을 표시하는 위젯.
    /// 전투 손패, 인벤토리, 덱 편집 화면에서 공용으로 사용된다.
    /// 드래그 + 클릭 인터랙션, 상태 전환, Placeholder 표시를 지원한다.
    /// </summary>
    public class UICard : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Inner Types

        /// <summary>
        /// 카드의 시각적 상태.
        /// </summary>
        public enum CardState
        {
            Normal,
            Hover,
            Selected,
            Disabled,
            Dragging
        }

        #endregion

        #region Serialized Fields

        [Header("Card Info")]
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private UIElementBadge _elementBadge;
        [SerializeField] private Image _artworkImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Image _rarityIndicator;

        [Header("Card Frame")]
        [SerializeField] private Image _cardBackground;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Interaction Settings")]
        [SerializeField] private float _hoverScale = 1.1f;
        [SerializeField] private float _hoverOffsetY = 30f;
        [SerializeField] private float _hoverDuration = 0.15f;
        [SerializeField] private float _dragAlpha = 0.7f;

        #endregion

        #region Private Fields

        private CardData _cardData;
        private RuntimeCard _runtimeCard;
        private CardState _currentState;
        private bool _isInteractable = true;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Tweener _scaleTween;
        private Tweener _moveTween;

        private static readonly Dictionary<CardType, Color32> CARD_TYPE_COLORS =
            new Dictionary<CardType, Color32>(4)
            {
                { CardType.Attack,       new Color32(0xE5, 0x39, 0x35, 0xFF) },
                { CardType.Defend,       new Color32(0x1E, 0x88, 0xE5, 0xFF) },
                { CardType.StatusEffect, new Color32(0x8E, 0x24, 0xAA, 0xFF) },
                { CardType.InHandEffect, new Color32(0x43, 0xA0, 0x47, 0xFF) },
            };

        private static readonly Dictionary<Rarity, Color32> RARITY_COLORS =
            new Dictionary<Rarity, Color32>(5)
            {
                { Rarity.Uncommon, new Color32(0x9E, 0x9E, 0x9E, 0xFF) },
                { Rarity.Common,   new Color32(0xFF, 0xFF, 0xFF, 0xFF) },
                { Rarity.Rare,     new Color32(0x42, 0xA5, 0xF5, 0xFF) },
                { Rarity.Unique,   new Color32(0xFF, 0xD5, 0x4F, 0xFF) },
                { Rarity.Epic,     new Color32(0xAB, 0x47, 0xBC, 0xFF) },
            };

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 바인딩된 카드 마스터 데이터.
        /// </summary>
        public CardData CurrentCardData => _cardData;

        /// <summary>
        /// 현재 바인딩된 런타임 카드 (전투 전용). 비전투 시 null.
        /// </summary>
        public RuntimeCard CurrentRuntimeCard => _runtimeCard;

        /// <summary>
        /// 현재 카드의 시각적 상태.
        /// </summary>
        public CardState CurrentState => _currentState;

        /// <summary>
        /// 인터랙션 가능 여부.
        /// </summary>
        public bool IsInteractable => _isInteractable;

        #endregion

        #region Events

        /// <summary>
        /// 카드가 클릭되었을 때.
        /// </summary>
        public event Action<UICard> OnClicked;

        /// <summary>
        /// 카드 드래그가 시작되었을 때.
        /// </summary>
        public event Action<UICard> OnDragStarted;

        /// <summary>
        /// 카드 드래그가 종료되었을 때.
        /// </summary>
        public event Action<UICard> OnDragEnded;

        /// <summary>
        /// 카드에 마우스가 진입했을 때.
        /// </summary>
        public event Action<UICard> OnHoverEnter;

        /// <summary>
        /// 카드에서 마우스가 이탈했을 때.
        /// </summary>
        public event Action<UICard> OnHoverExit;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _originalScale = transform.localScale;
            _originalPosition = transform.localPosition;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 정적 카드 데이터를 바인딩한다. 로비, 인벤토리, 덱 편집 화면에서 사용.
        /// </summary>
        /// <param name="card">카드 마스터 데이터</param>
        public void SetData(CardData card)
        {
            _cardData = card;
            _runtimeCard = null;

            if (card == null)
            {
                Clear();
                return;
            }

            ApplyVisuals(card.cardName, card.description, card.cost, card.element,
                card.cardType, card.rarity);
        }

        /// <summary>
        /// 런타임 카드를 바인딩한다. 전투 손패에서 사용. 수정 코스트가 반영된다.
        /// </summary>
        /// <param name="runtimeCard">런타임 카드</param>
        public void SetData(RuntimeCard runtimeCard)
        {
            if (runtimeCard == null)
            {
                _cardData = null;
                _runtimeCard = null;
                Clear();
                return;
            }

            _runtimeCard = runtimeCard;
            _cardData = runtimeCard.BaseData;

            CardData baseData = runtimeCard.BaseData;
            int displayCost = runtimeCard.ModifiedCost;

            ApplyVisuals(baseData.cardName, baseData.description, displayCost,
                baseData.element, baseData.cardType, baseData.rarity);

            UpdateCostDisplay(baseData.cost, displayCost);
        }

        /// <summary>
        /// 카드의 시각적 상태를 강제 전환한다.
        /// </summary>
        /// <param name="state">설정할 상태</param>
        public void SetState(CardState state)
        {
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;
            ApplyStateVisuals(state);
        }

        /// <summary>
        /// 카드 인터랙션의 활성/비활성을 설정한다.
        /// </summary>
        /// <param name="interactable">활성 여부</param>
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;

            if (!interactable && _currentState != CardState.Disabled)
            {
                SetState(CardState.Disabled);
            }
            else if (interactable && _currentState == CardState.Disabled)
            {
                SetState(CardState.Normal);
            }
        }

        /// <summary>
        /// 카드를 빈 상태로 리셋한다.
        /// </summary>
        public void Clear()
        {
            if (_costText != null) _costText.text = string.Empty;
            if (_nameText != null) _nameText.text = string.Empty;
            if (_descriptionText != null) _descriptionText.text = string.Empty;
            if (_elementBadge != null) _elementBadge.Clear();
            if (_artworkImage != null) _artworkImage.color = Color.clear;
            if (_rarityIndicator != null) _rarityIndicator.color = Color.clear;

            _currentState = CardState.Normal;
        }

        #endregion

        #region IPointerEnterHandler

        /// <summary>
        /// 마우스 진입 시 Hover 상태로 전환한다.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable || _currentState == CardState.Dragging)
            {
                return;
            }

            if (_currentState == CardState.Normal)
            {
                SetState(CardState.Hover);
            }

            OnHoverEnter?.Invoke(this);
        }

        #endregion

        #region IPointerExitHandler

        /// <summary>
        /// 마우스 이탈 시 Normal 상태로 복귀한다.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (_currentState == CardState.Hover)
            {
                SetState(CardState.Normal);
            }

            OnHoverExit?.Invoke(this);
        }

        #endregion

        #region IPointerClickHandler

        /// <summary>
        /// 클릭 시 OnClicked 이벤트를 발행한다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable || _currentState == CardState.Dragging)
            {
                return;
            }

            OnClicked?.Invoke(this);
        }

        #endregion

        #region IBeginDragHandler

        /// <summary>
        /// 드래그 시작 시 원본 위치를 저장하고 Dragging 상태로 전환한다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable)
            {
                return;
            }

            _originalPosition = transform.localPosition;
            _originalParent = transform.parent;
            _originalSiblingIndex = transform.GetSiblingIndex();

            SetState(CardState.Dragging);
            OnDragStarted?.Invoke(this);
        }

        #endregion

        #region IDragHandler

        /// <summary>
        /// 드래그 중 카드를 마우스 위치로 이동시킨다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isInteractable || _currentState != CardState.Dragging)
            {
                return;
            }

            transform.position += (Vector3)eventData.delta;
        }

        #endregion

        #region IEndDragHandler

        /// <summary>
        /// 드래그 종료 시 Normal 상태로 복귀하고 이벤트를 발행한다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_currentState != CardState.Dragging)
            {
                return;
            }

            OnDragEnded?.Invoke(this);
        }

        /// <summary>
        /// 카드를 원래 위치로 DOTween 애니메이션과 함께 복귀시킨다.
        /// 외부에서 드래그 실패(무효 영역 드롭) 시 호출한다.
        /// </summary>
        public void ReturnToOriginalPosition()
        {
            KillTweens();

            if (_originalParent != null)
            {
                transform.SetParent(_originalParent);
                transform.SetSiblingIndex(_originalSiblingIndex);
            }

            _moveTween = transform.DOLocalMove(_originalPosition, _hoverDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => SetState(CardState.Normal));
        }

        #endregion

        #region Private Methods

        private void ApplyVisuals(string cardName, string description, int cost,
            ElementType element, CardType cardType, Rarity rarity)
        {
            if (_costText != null)
            {
                _costText.text = cost.ToString();
            }

            if (_nameText != null)
            {
                _nameText.text = cardName;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = description;
            }

            if (_elementBadge != null)
            {
                _elementBadge.SetElement(element);
            }

            ApplyPlaceholderArtwork(cardType);
            ApplyRarityIndicator(rarity);
        }

        private void ApplyPlaceholderArtwork(CardType cardType)
        {
            if (_artworkImage == null)
            {
                return;
            }

            if (CARD_TYPE_COLORS.TryGetValue(cardType, out Color32 color))
            {
                _artworkImage.color = color;
            }
            else
            {
                _artworkImage.color = Color.gray;
            }
        }

        private void ApplyRarityIndicator(Rarity rarity)
        {
            if (_rarityIndicator == null)
            {
                return;
            }

            if (RARITY_COLORS.TryGetValue(rarity, out Color32 color))
            {
                _rarityIndicator.color = color;
            }
            else
            {
                _rarityIndicator.color = Color.white;
            }
        }

        private void UpdateCostDisplay(int baseCost, int modifiedCost)
        {
            if (_costText == null)
            {
                return;
            }

            if (modifiedCost < baseCost)
            {
                _costText.color = new Color32(0x4C, 0xAF, 0x50, 0xFF);
            }
            else if (modifiedCost > baseCost)
            {
                _costText.color = new Color32(0xF4, 0x43, 0x36, 0xFF);
            }
            else
            {
                _costText.color = Color.white;
            }
        }

        private void ApplyStateVisuals(CardState state)
        {
            KillTweens();

            switch (state)
            {
                case CardState.Normal:
                    _scaleTween = transform.DOScale(_originalScale, _hoverDuration)
                        .SetEase(Ease.OutQuad);

                    _moveTween = transform.DOLocalMove(_originalPosition, _hoverDuration)
                        .SetEase(Ease.OutQuad);

                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = 1f;
                        _canvasGroup.blocksRaycasts = true;
                    }
                    break;

                case CardState.Hover:
                    Vector3 hoverScale = _originalScale * _hoverScale;
                    _scaleTween = transform.DOScale(hoverScale, _hoverDuration)
                        .SetEase(Ease.OutQuad);

                    Vector3 hoverPos = _originalPosition + new Vector3(0f, _hoverOffsetY, 0f);
                    _moveTween = transform.DOLocalMove(hoverPos, _hoverDuration)
                        .SetEase(Ease.OutQuad);

                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = 1f;
                    }
                    break;

                case CardState.Selected:
                    Vector3 selectedScale = _originalScale * _hoverScale;
                    _scaleTween = transform.DOScale(selectedScale, _hoverDuration)
                        .SetEase(Ease.OutQuad);

                    if (_cardBackground != null)
                    {
                        _cardBackground.color = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
                    }
                    break;

                case CardState.Disabled:
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = 0.5f;
                        _canvasGroup.blocksRaycasts = false;
                    }
                    break;

                case CardState.Dragging:
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = _dragAlpha;
                        _canvasGroup.blocksRaycasts = false;
                    }
                    break;
            }
        }

        private void KillTweens()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
            {
                _scaleTween.Kill();
                _scaleTween = null;
            }

            if (_moveTween != null && _moveTween.IsActive())
            {
                _moveTween.Kill();
                _moveTween = null;
            }
        }

        #endregion
    }
}
