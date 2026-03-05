using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 전투 손패 영역 UI 컴포넌트.
    /// UICard 오브젝트 풀을 관리하며, 카드 추가/제거/사용 시 가로 정렬과
    /// DOTween 연출(드로우, 사용, 재정렬)을 처리한다.
    /// </summary>
    public class UIHandArea : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private UICard _cardPrefab;

        [Header("Layout")]
        [SerializeField] private float _cardSpacing = 120f;
        [SerializeField] private float _rearrangeDuration = 0.2f;

        [Header("Draw Animation")]
        [SerializeField] private Vector3 _drawStartPosition = new Vector3(-500f, -200f, 0f);
        [SerializeField] private float _drawDuration = 0.3f;

        [Header("Play Animation")]
        [SerializeField] private Vector3 _playTargetOffset = new Vector3(0f, 300f, 0f);
        [SerializeField] private float _playDuration = 0.25f;

        [Header("Pool")]
        [SerializeField] private int _initialPoolSize = 18;

        #endregion

        #region Private Fields

        private readonly List<UICard> _activeCards = new List<UICard>(8);
        private readonly Queue<UICard> _cardPool = new Queue<UICard>(24);
        private readonly Dictionary<RuntimeCard, UICard> _cardMap = new Dictionary<RuntimeCard, UICard>(8);

        #endregion

        #region Events

        /// <summary>
        /// 카드 클릭 시 발행.
        /// </summary>
        public event Action<UICard> OnCardSelected;

        /// <summary>
        /// 카드 드래그 종료 시 발행.
        /// </summary>
        public event Action<UICard> OnCardDraggedToTarget;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePool();
        }

        private void OnDisable()
        {
            DOTween.Kill(GetInstanceID());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 손패에 카드를 추가하고 드로우 연출을 재생한다.
        /// </summary>
        /// <param name="runtimeCard">추가할 런타임 카드</param>
        public void AddCard(RuntimeCard runtimeCard)
        {
            if (runtimeCard == null)
            {
                return;
            }

            UICard uiCard = GetCardFromPool();

            if (uiCard == null)
            {
                return;
            }

            uiCard.SetData(runtimeCard);
            uiCard.SetInteractable(true);
            _activeCards.Add(uiCard);
            _cardMap[runtimeCard] = uiCard;

            BindCardEvents(uiCard);
            PlayDrawAnimation(uiCard);
            RearrangeCards();
        }

        /// <summary>
        /// 손패에서 카드를 제거하고 재정렬한다.
        /// </summary>
        /// <param name="runtimeCard">제거할 런타임 카드</param>
        public void RemoveCard(RuntimeCard runtimeCard)
        {
            if (runtimeCard == null || !_cardMap.TryGetValue(runtimeCard, out UICard uiCard))
            {
                return;
            }

            UnbindCardEvents(uiCard);
            _activeCards.Remove(uiCard);
            _cardMap.Remove(runtimeCard);
            ReturnCardToPool(uiCard);
            RearrangeCards();
        }

        /// <summary>
        /// 카드 사용 연출을 재생한 뒤 풀에 반환한다.
        /// </summary>
        /// <param name="runtimeCard">사용된 런타임 카드</param>
        public void PlayCardAnimation(RuntimeCard runtimeCard)
        {
            if (runtimeCard == null || !_cardMap.TryGetValue(runtimeCard, out UICard uiCard))
            {
                return;
            }

            UnbindCardEvents(uiCard);
            _activeCards.Remove(uiCard);
            _cardMap.Remove(runtimeCard);

            uiCard.SetInteractable(false);

            var cardTransform = uiCard.transform as RectTransform;

            if (cardTransform == null)
            {
                ReturnCardToPool(uiCard);
                RearrangeCards();
                return;
            }

            var canvasGroup = uiCard.GetComponent<CanvasGroup>();

            Sequence seq = DOTween.Sequence();
            seq.SetId(GetInstanceID());
            seq.Append(cardTransform.DOAnchorPos(
                cardTransform.anchoredPosition + (Vector2)_playTargetOffset,
                _playDuration).SetEase(Ease.InBack));

            if (canvasGroup != null)
            {
                seq.Join(canvasGroup.DOFade(0f, _playDuration));
            }

            seq.OnComplete(() =>
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 1f;
                }

                ReturnCardToPool(uiCard);
            });

            RearrangeCards();
        }

        /// <summary>
        /// 에너지에 따라 각 카드의 인터랙션 가능 여부를 갱신한다.
        /// </summary>
        /// <param name="currentEnergy">현재 에너지</param>
        public void UpdateCardInteractability(int currentEnergy)
        {
            for (int i = 0; i < _activeCards.Count; i++)
            {
                UICard card = _activeCards[i];
                RuntimeCard runtimeCard = card.CurrentRuntimeCard;

                if (runtimeCard != null)
                {
                    card.SetInteractable(currentEnergy >= runtimeCard.ModifiedCost);
                }
            }
        }

        /// <summary>
        /// 모든 카드를 풀에 반환하고 손패를 비운다.
        /// </summary>
        public void Clear()
        {
            for (int i = _activeCards.Count - 1; i >= 0; i--)
            {
                UICard card = _activeCards[i];
                UnbindCardEvents(card);
                ReturnCardToPool(card);
            }

            _activeCards.Clear();
            _cardMap.Clear();
        }

        /// <summary>
        /// 특정 UICard 인스턴스를 찾는다.
        /// </summary>
        /// <param name="runtimeCard">런타임 카드</param>
        /// <returns>대응하는 UICard (없으면 null)</returns>
        public UICard FindUICard(RuntimeCard runtimeCard)
        {
            if (runtimeCard != null && _cardMap.TryGetValue(runtimeCard, out UICard uiCard))
            {
                return uiCard;
            }

            return null;
        }

        /// <summary>
        /// 현재 손패의 UICard 목록을 반환한다.
        /// </summary>
        /// <returns>읽기 전용 UICard 리스트</returns>
        public IReadOnlyList<UICard> GetActiveCards()
        {
            return _activeCards;
        }

        /// <summary>
        /// 지정 카드의 손패 내 인덱스를 반환한다.
        /// </summary>
        /// <param name="card">검색할 UICard</param>
        /// <returns>손패 인덱스 (없으면 -1)</returns>
        public int GetCardIndex(UICard card)
        {
            return _activeCards.IndexOf(card);
        }

        #endregion

        #region Private Methods

        private void InitializePool()
        {
            if (_cardPrefab == null || _cardContainer == null)
            {
                return;
            }

            for (int i = 0; i < _initialPoolSize; i++)
            {
                UICard card = Instantiate(_cardPrefab, _cardContainer);
                card.gameObject.SetActive(false);
                _cardPool.Enqueue(card);
            }
        }

        private UICard GetCardFromPool()
        {
            UICard card;

            if (_cardPool.Count > 0)
            {
                card = _cardPool.Dequeue();
            }
            else if (_cardPrefab != null && _cardContainer != null)
            {
                card = Instantiate(_cardPrefab, _cardContainer);
            }
            else
            {
                return null;
            }

            card.gameObject.SetActive(true);
            return card;
        }

        private void ReturnCardToPool(UICard card)
        {
            if (card == null)
            {
                return;
            }

            card.Clear();
            card.gameObject.SetActive(false);
            _cardPool.Enqueue(card);
        }

        private void BindCardEvents(UICard card)
        {
            card.OnClicked += HandleCardClicked;
            card.OnDragEnded += HandleCardDragEnded;
        }

        private void UnbindCardEvents(UICard card)
        {
            card.OnClicked -= HandleCardClicked;
            card.OnDragEnded -= HandleCardDragEnded;
        }

        private void HandleCardClicked(UICard card)
        {
            OnCardSelected?.Invoke(card);
        }

        private void HandleCardDragEnded(UICard card)
        {
            OnCardDraggedToTarget?.Invoke(card);
        }

        private void PlayDrawAnimation(UICard card)
        {
            var rectTransform = card.transform as RectTransform;

            if (rectTransform == null)
            {
                return;
            }

            Vector2 targetPos = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = (Vector2)_drawStartPosition;
            rectTransform.localScale = Vector3.one * 0.5f;

            Sequence seq = DOTween.Sequence();
            seq.SetId(GetInstanceID());
            seq.Append(rectTransform.DOAnchorPos(targetPos, _drawDuration)
                .SetEase(Ease.OutCubic));
            seq.Join(rectTransform.DOScale(1f, _drawDuration)
                .SetEase(Ease.OutBack));
        }

        private void RearrangeCards()
        {
            int count = _activeCards.Count;

            if (count == 0)
            {
                return;
            }

            float totalWidth = (count - 1) * _cardSpacing;
            float startX = -totalWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var rectTransform = _activeCards[i].transform as RectTransform;

                if (rectTransform == null)
                {
                    continue;
                }

                float targetX = startX + i * _cardSpacing;
                var targetPos = new Vector2(targetX, 0f);

                rectTransform.DOAnchorPos(targetPos, _rearrangeDuration)
                    .SetEase(Ease.OutCubic)
                    .SetId(GetInstanceID());
            }
        }

        #endregion
    }
}
