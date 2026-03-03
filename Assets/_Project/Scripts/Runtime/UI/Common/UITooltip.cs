using UnityEngine;
using TMPro;

namespace ProjectStS.UI
{
    /// <summary>
    /// 호버 시 표시되는 툴팁 시스템.
    /// 씬당 하나의 인스턴스로 동작하며, 화면 경계 자동 클램핑을 지원한다.
    /// Canvas Overlay에 부착하여 사용한다.
    /// </summary>
    public class UITooltip : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private RectTransform _tooltipPanel;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Settings")]
        [SerializeField] private Vector2 _offset = new Vector2(16f, -16f);

        #endregion

        #region Private Fields

        private static UITooltip _instance;
        private RectTransform _canvasRectTransform;
        private Canvas _parentCanvas;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _instance = this;
            _parentCanvas = GetComponentInParent<Canvas>();

            if (_parentCanvas != null)
            {
                _canvasRectTransform = _parentCanvas.GetComponent<RectTransform>();
            }

            HideInternal();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Public Methods (Static)

        /// <summary>
        /// 툴팁을 지정된 위치에 표시한다.
        /// </summary>
        /// <param name="title">툴팁 제목</param>
        /// <param name="body">툴팁 본문</param>
        /// <param name="screenPosition">화면 좌표 (Input.mousePosition 등)</param>
        public static void Show(string title, string body, Vector2 screenPosition)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ShowInternal(title, body, screenPosition);
        }

        /// <summary>
        /// 툴팁을 숨긴다.
        /// </summary>
        public static void Hide()
        {
            if (_instance == null)
            {
                return;
            }

            _instance.HideInternal();
        }

        #endregion

        #region Private Methods

        private void ShowInternal(string title, string body, Vector2 screenPosition)
        {
            if (_titleText != null)
            {
                bool hasTitle = !string.IsNullOrEmpty(title);
                _titleText.gameObject.SetActive(hasTitle);

                if (hasTitle)
                {
                    _titleText.text = title;
                }
            }

            if (_bodyText != null)
            {
                _bodyText.text = body;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_tooltipPanel != null)
            {
                _tooltipPanel.gameObject.SetActive(true);
            }

            PositionTooltip(screenPosition);
        }

        private void HideInternal()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            if (_tooltipPanel != null)
            {
                _tooltipPanel.gameObject.SetActive(false);
            }
        }

        private void PositionTooltip(Vector2 screenPosition)
        {
            if (_tooltipPanel == null || _parentCanvas == null)
            {
                return;
            }

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform, screenPosition, _parentCanvas.worldCamera, out localPoint);

            Vector2 tooltipPosition = localPoint + _offset;

            Canvas.ForceUpdateCanvases();

            Vector2 tooltipSize = _tooltipPanel.rect.size;
            Vector2 canvasSize = _canvasRectTransform.rect.size;
            Vector2 canvasHalfSize = canvasSize * 0.5f;

            float rightEdge = tooltipPosition.x + tooltipSize.x;
            if (rightEdge > canvasHalfSize.x)
            {
                tooltipPosition.x = localPoint.x - _offset.x - tooltipSize.x;
            }

            float bottomEdge = tooltipPosition.y - tooltipSize.y;
            if (bottomEdge < -canvasHalfSize.y)
            {
                tooltipPosition.y = localPoint.y - _offset.y + tooltipSize.y;
            }

            float leftEdge = tooltipPosition.x;
            if (leftEdge < -canvasHalfSize.x)
            {
                tooltipPosition.x = -canvasHalfSize.x;
            }

            float topEdge = tooltipPosition.y;
            if (topEdge > canvasHalfSize.y)
            {
                tooltipPosition.y = canvasHalfSize.y;
            }

            _tooltipPanel.anchoredPosition = tooltipPosition;
        }

        #endregion
    }
}
