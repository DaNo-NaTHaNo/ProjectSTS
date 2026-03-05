using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Stage;

namespace ProjectStS.UI
{
    /// <summary>
    /// 인게임 가방 패널.
    /// 스테이지 탐험 중 획득한 아이템 목록을 UIItemIcon으로 표시한다.
    /// 토글 버튼으로 열기/닫기를 지원한다.
    /// </summary>
    public class UIBagPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private CanvasGroup _panelCanvasGroup;
        [SerializeField] private Button _toggleButton;
        [SerializeField] private TextMeshProUGUI _toggleButtonText;

        [Header("Item List")]
        [SerializeField] private RectTransform _itemContainer;
        [SerializeField] private UIItemIcon _itemIconPrefab;
        [SerializeField] private int _poolSize = 30;

        [Header("Settings")]
        [SerializeField] private float _animDuration = 0.3f;
        [SerializeField] private Vector2 _closedPosition = new Vector2(300f, 0f);
        [SerializeField] private Vector2 _openPosition = Vector2.zero;

        #endregion

        #region Private Fields

        private bool _isOpen;
        private readonly List<UIItemIcon> _iconPool = new List<UIItemIcon>(30);
        private readonly List<UIItemIcon> _activeIcons = new List<UIItemIcon>(30);
        private readonly List<InGameBagItemData> _bagItems = new List<InGameBagItemData>(30);
        private InGameBagManager _bagManager;
        private Tweener _slideTween;

        #endregion

        #region Public Properties

        /// <summary>
        /// 패널이 열려 있는지 여부.
        /// </summary>
        public bool IsOpen => _isOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.AddListener(Toggle);
            }
        }

        private void OnDisable()
        {
            KillSlideTween();
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(Toggle);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 가방 패널을 초기화한다.
        /// </summary>
        /// <param name="bagManager">인게임 가방 매니저</param>
        public void Initialize(InGameBagManager bagManager)
        {
            _bagManager = bagManager;
            _isOpen = false;

            InitializePool();

            if (_panelRoot != null)
            {
                _panelRoot.anchoredPosition = _closedPosition;
            }

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.alpha = 0f;
                _panelCanvasGroup.blocksRaycasts = false;
            }

            if (_bagManager != null)
            {
                _bagItems.Clear();
                List<InGameBagItemData> existing = _bagManager.BagItems;

                for (int i = 0; i < existing.Count; i++)
                {
                    _bagItems.Add(existing[i]);
                }
            }

            RefreshAll();
            UpdateToggleText();
        }

        /// <summary>
        /// 아이템 추가 시 호출. 목록에 새 아이콘을 추가한다.
        /// </summary>
        /// <param name="item">추가된 아이템 데이터</param>
        public void AddItem(InGameBagItemData item)
        {
            if (item == null)
            {
                return;
            }

            _bagItems.Add(item);

            UIItemIcon icon = GetOrCreateIcon();
            BindBagItemToIcon(icon, item);
            _activeIcons.Add(icon);

            UpdateToggleText();
        }

        /// <summary>
        /// 전체 목록을 재구성한다.
        /// </summary>
        public void RefreshAll()
        {
            for (int i = 0; i < _activeIcons.Count; i++)
            {
                _activeIcons[i].Clear();
                _activeIcons[i].gameObject.SetActive(false);
            }

            _activeIcons.Clear();

            for (int i = 0; i < _bagItems.Count; i++)
            {
                UIItemIcon icon = GetOrCreateIcon();
                BindBagItemToIcon(icon, _bagItems[i]);
                _activeIcons.Add(icon);
            }

            UpdateToggleText();
        }

        /// <summary>
        /// 열기/닫기를 토글한다.
        /// </summary>
        public void Toggle()
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// <summary>
        /// 패널을 연다.
        /// </summary>
        public void Open()
        {
            if (_isOpen)
            {
                return;
            }

            _isOpen = true;
            PlayOpenAnimation();
        }

        /// <summary>
        /// 패널을 닫는다.
        /// </summary>
        public void Close()
        {
            if (!_isOpen)
            {
                return;
            }

            _isOpen = false;
            PlayCloseAnimation();
        }

        #endregion

        #region Private Methods

        private void InitializePool()
        {
            if (_itemIconPrefab == null || _itemContainer == null)
            {
                return;
            }

            for (int i = 0; i < _poolSize; i++)
            {
                UIItemIcon icon = Instantiate(_itemIconPrefab, _itemContainer);
                icon.gameObject.SetActive(false);
                _iconPool.Add(icon);
            }
        }

        private UIItemIcon GetOrCreateIcon()
        {
            for (int i = 0; i < _iconPool.Count; i++)
            {
                if (!_iconPool[i].gameObject.activeSelf)
                {
                    _iconPool[i].gameObject.SetActive(true);
                    return _iconPool[i];
                }
            }

            if (_itemIconPrefab == null || _itemContainer == null)
            {
                return null;
            }

            UIItemIcon newIcon = Instantiate(_itemIconPrefab, _itemContainer);
            _iconPool.Add(newIcon);
            return newIcon;
        }

        private void BindBagItemToIcon(UIItemIcon icon, InGameBagItemData bagItem)
        {
            if (icon == null || bagItem == null)
            {
                return;
            }

            var inventoryItem = new InventoryItemData
            {
                productId = bagItem.productId,
                productName = bagItem.productName,
                rarity = bagItem.rarity,
                category = bagItem.category,
                ownStack = 1
            };

            icon.SetData(inventoryItem);
        }

        private void UpdateToggleText()
        {
            if (_toggleButtonText != null)
            {
                _toggleButtonText.text = $"가방 ({_bagItems.Count})";
            }
        }

        private void PlayOpenAnimation()
        {
            KillSlideTween();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.blocksRaycasts = true;
                _panelCanvasGroup.DOFade(1f, _animDuration);
            }

            if (_panelRoot != null)
            {
                _slideTween = _panelRoot.DOAnchorPos(_openPosition, _animDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        private void PlayCloseAnimation()
        {
            KillSlideTween();

            if (_panelCanvasGroup != null)
            {
                _panelCanvasGroup.DOFade(0f, _animDuration);
            }

            if (_panelRoot != null)
            {
                _slideTween = _panelRoot.DOAnchorPos(_closedPosition, _animDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        if (_panelCanvasGroup != null)
                        {
                            _panelCanvasGroup.blocksRaycasts = false;
                        }
                    });
            }
        }

        private void KillSlideTween()
        {
            if (_slideTween != null && _slideTween.IsActive())
            {
                _slideTween.Kill();
                _slideTween = null;
            }
        }

        #endregion
    }
}
