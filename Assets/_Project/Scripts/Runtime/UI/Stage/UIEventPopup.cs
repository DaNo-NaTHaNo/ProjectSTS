using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using EventType = ProjectStS.Data.EventType;

namespace ProjectStS.UI
{
    /// <summary>
    /// 이벤트 진입 팝업.
    /// 플레이어가 이벤트 노드에 진입할 때 이벤트 유형, 이름을 표시하고
    /// 확인/취소 선택을 제공한다.
    /// </summary>
    public class UIEventPopup : MonoBehaviour
    {
        #region Constants

        private static readonly Dictionary<EventType, (string displayName, Color32 bannerColor)> EVENT_TYPE_MAP =
            new Dictionary<EventType, (string, Color32)>(6)
            {
                { EventType.BattleNormal,  ("전투",       new Color32(0xF4, 0x43, 0x36, 0xFF)) },
                { EventType.BattleElite,   ("엘리트 전투", new Color32(0xFF, 0x98, 0x00, 0xFF)) },
                { EventType.BattleBoss,    ("보스 전투",   new Color32(0x9C, 0x27, 0xB0, 0xFF)) },
                { EventType.BattleEvent,   ("이벤트 전투", new Color32(0xFF, 0x57, 0x22, 0xFF)) },
                { EventType.VisualNovel,   ("스토리",     new Color32(0x21, 0x96, 0xF3, 0xFF)) },
                { EventType.Encounter,     ("조우",       new Color32(0x4C, 0xAF, 0x50, 0xFF)) },
            };

        #endregion

        #region Serialized Fields

        [Header("Popup")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Image _dimBackground;
        [SerializeField] private RectTransform _panelTransform;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _eventTypeLabel;
        [SerializeField] private TextMeshProUGUI _eventNameText;
        [SerializeField] private Image _eventTypeBanner;

        [Header("Buttons")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TextMeshProUGUI _confirmText;
        [SerializeField] private TextMeshProUGUI _cancelText;

        [Header("Settings")]
        [SerializeField] private float _animDuration = 0.25f;
        [SerializeField] private string _defaultConfirmText = "진입";
        [SerializeField] private string _defaultCancelText = "취소";

        #endregion

        #region Private Fields

        private Action _onConfirm;
        private Action _onCancel;
        private EventData _currentEventData;
        private Sequence _currentSequence;
        private bool _isShowing;

        #endregion

        #region Public Properties

        /// <summary>
        /// 팝업이 표시 중인지 여부.
        /// </summary>
        public bool IsShowing => _isShowing;

        #endregion

        #region Events

        /// <summary>
        /// 이벤트 진입이 확인되었을 때.
        /// </summary>
        public event Action<EventData> OnEventConfirmed;

        /// <summary>
        /// 이벤트 진입이 취소되었을 때.
        /// </summary>
        public event Action OnEventCancelled;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnDestroy()
        {
            KillSequence();

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.RemoveListener(OnCancelClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 이벤트 팝업을 표시한다.
        /// </summary>
        /// <param name="eventData">표시할 이벤트 데이터</param>
        /// <param name="onConfirm">확인 콜백</param>
        /// <param name="onCancel">취소 콜백 (null 허용)</param>
        public void Show(EventData eventData, Action onConfirm, Action onCancel = null)
        {
            if (eventData == null)
            {
                return;
            }

            _currentEventData = eventData;
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            PopulateContent(eventData);
            PlayOpenAnimation();
        }

        /// <summary>
        /// 팝업을 닫는다.
        /// </summary>
        public void Hide()
        {
            if (!_isShowing)
            {
                return;
            }

            PlayCloseAnimation(null);
        }

        #endregion

        #region Private Methods

        private void PopulateContent(EventData eventData)
        {
            string displayName = "이벤트";
            Color32 bannerColor = new Color32(0x78, 0x78, 0x78, 0xFF);

            if (EVENT_TYPE_MAP.TryGetValue(eventData.eventType, out var typeInfo))
            {
                displayName = typeInfo.displayName;
                bannerColor = typeInfo.bannerColor;
            }

            if (_eventTypeLabel != null)
            {
                _eventTypeLabel.text = displayName;
            }

            if (_eventNameText != null)
            {
                _eventNameText.text = eventData.id ?? string.Empty;
            }

            if (_eventTypeBanner != null)
            {
                _eventTypeBanner.color = bannerColor;
            }

            if (_confirmText != null)
            {
                _confirmText.text = _defaultConfirmText;
            }

            if (_cancelText != null)
            {
                _cancelText.text = _defaultCancelText;
            }

            if (_cancelButton != null)
            {
                _cancelButton.gameObject.SetActive(_onCancel != null);
            }
        }

        private void OnConfirmClicked()
        {
            if (!_isShowing)
            {
                return;
            }

            EventData confirmedEvent = _currentEventData;
            Action callback = _onConfirm;

            PlayCloseAnimation(() =>
            {
                callback?.Invoke();
                OnEventConfirmed?.Invoke(confirmedEvent);
            });
        }

        private void OnCancelClicked()
        {
            if (!_isShowing)
            {
                return;
            }

            Action callback = _onCancel;

            PlayCloseAnimation(() =>
            {
                callback?.Invoke();
                OnEventCancelled?.Invoke();
            });
        }

        private void PlayOpenAnimation()
        {
            KillSequence();

            _isShowing = true;

            if (_popupRoot != null)
            {
                _popupRoot.SetActive(true);
            }

            _currentSequence = DOTween.Sequence();

            if (_dimBackground != null)
            {
                _dimBackground.color = new Color(0f, 0f, 0f, 0f);
                _currentSequence.Join(
                    DOTween.To(
                        () => _dimBackground.color.a,
                        x => { Color c = _dimBackground.color; c.a = x; _dimBackground.color = c; },
                        0.5f, _animDuration
                    )
                );
            }

            if (_panelTransform != null)
            {
                _panelTransform.localScale = Vector3.zero;
                _currentSequence.Join(
                    _panelTransform.DOScale(Vector3.one, _animDuration)
                        .SetEase(Ease.OutBack)
                );
            }
        }

        private void PlayCloseAnimation(Action onComplete)
        {
            KillSequence();

            _currentSequence = DOTween.Sequence();

            if (_panelTransform != null)
            {
                _currentSequence.Join(
                    _panelTransform.DOScale(Vector3.zero, _animDuration)
                        .SetEase(Ease.InBack)
                );
            }

            if (_dimBackground != null)
            {
                _currentSequence.Join(
                    DOTween.To(
                        () => _dimBackground.color.a,
                        x => { Color c = _dimBackground.color; c.a = x; _dimBackground.color = c; },
                        0f, _animDuration
                    )
                );
            }

            _currentSequence.OnComplete(() =>
            {
                _isShowing = false;
                _currentEventData = null;
                _onConfirm = null;
                _onCancel = null;

                if (_popupRoot != null)
                {
                    _popupRoot.SetActive(false);
                }

                onComplete?.Invoke();
            });
        }

        private void KillSequence()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        #endregion
    }
}
