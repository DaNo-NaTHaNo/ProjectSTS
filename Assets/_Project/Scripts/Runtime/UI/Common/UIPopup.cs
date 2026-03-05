using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ProjectStS.UI
{
    /// <summary>
    /// 범용 팝업 프레임워크.
    /// 확인/취소, 정보 표시 등 다양한 팝업을 DOTween 애니메이션과 함께 제공한다.
    /// Canvas Overlay에 부착하여 사용한다.
    /// </summary>
    public class UIPopup : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Root")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Image _dimBackground;

        [Header("Panel")]
        [SerializeField] private RectTransform _panelTransform;
        [SerializeField] private TextMeshProUGUI _messageText;

        [Header("Buttons")]
        [SerializeField] private GameObject _confirmButtonRoot;
        [SerializeField] private GameObject _cancelButtonRoot;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;
        [SerializeField] private TextMeshProUGUI _cancelButtonText;

        [Header("Settings")]
        [SerializeField] private float _animDuration = 0.25f;
        [SerializeField] private string _defaultConfirmText = "확인";
        [SerializeField] private string _defaultCancelText = "취소";

        #endregion

        #region Private Fields

        private static UIPopup _instance;
        private Action _onConfirm;
        private Action _onCancel;
        private Sequence _currentSequence;
        private bool _isOpen;

        #endregion

        #region Public Properties

        /// <summary>
        /// 팝업이 현재 열려 있는지.
        /// </summary>
        public static bool IsOpen => _instance != null && _instance._isOpen;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _instance = this;

            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }

            _isOpen = false;
        }

        private void OnDestroy()
        {
            KillSequence();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods (Static)

        /// <summary>
        /// 확인/취소 팝업을 표시한다.
        /// </summary>
        /// <param name="message">팝업 메시지</param>
        /// <param name="onConfirm">확인 버튼 콜백</param>
        /// <param name="onCancel">취소 버튼 콜백 (null 허용)</param>
        public static void ShowConfirm(string message, Action onConfirm, Action onCancel = null)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ShowConfirmInternal(message, onConfirm, onCancel);
        }

        /// <summary>
        /// 정보 팝업을 표시한다. 확인 버튼만 노출된다.
        /// </summary>
        /// <param name="message">팝업 메시지</param>
        /// <param name="onClose">닫기 콜백 (null 허용)</param>
        public static void ShowInfo(string message, Action onClose = null)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ShowInfoInternal(message, onClose);
        }

        /// <summary>
        /// 현재 열려 있는 팝업을 닫는다.
        /// </summary>
        public static void Close()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.CloseInternal();
        }

        #endregion

        #region Public Methods (Button Events)

        /// <summary>
        /// 확인 버튼 클릭 시 호출된다. Inspector에서 바인딩.
        /// </summary>
        public void OnConfirmClicked()
        {
            Action callback = _onConfirm;
            CloseInternal();
            callback?.Invoke();
        }

        /// <summary>
        /// 취소 버튼 클릭 시 호출된다. Inspector에서 바인딩.
        /// </summary>
        public void OnCancelClicked()
        {
            Action callback = _onCancel;
            CloseInternal();
            callback?.Invoke();
        }

        #endregion

        #region Private Methods

        private void ShowConfirmInternal(string message, Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_confirmButtonRoot != null)
            {
                _confirmButtonRoot.SetActive(true);
            }

            if (_cancelButtonRoot != null)
            {
                _cancelButtonRoot.SetActive(true);
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = _defaultConfirmText;
            }

            if (_cancelButtonText != null)
            {
                _cancelButtonText.text = _defaultCancelText;
            }

            PlayOpenAnimation();
        }

        private void ShowInfoInternal(string message, Action onClose)
        {
            _onConfirm = onClose;
            _onCancel = null;

            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_confirmButtonRoot != null)
            {
                _confirmButtonRoot.SetActive(true);
            }

            if (_cancelButtonRoot != null)
            {
                _cancelButtonRoot.SetActive(false);
            }

            if (_confirmButtonText != null)
            {
                _confirmButtonText.text = _defaultConfirmText;
            }

            PlayOpenAnimation();
        }

        private void CloseInternal()
        {
            if (!_isOpen)
            {
                return;
            }

            PlayCloseAnimation();
        }

        private void PlayOpenAnimation()
        {
            KillSequence();

            _isOpen = true;

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

        private void PlayCloseAnimation()
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
                _isOpen = false;
                _onConfirm = null;
                _onCancel = null;

                if (_popupRoot != null)
                {
                    _popupRoot.SetActive(false);
                }
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
