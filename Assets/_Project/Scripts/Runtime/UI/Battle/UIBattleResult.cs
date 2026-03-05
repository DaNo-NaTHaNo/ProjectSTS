using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 전투 결과(승리/패배)를 표시하는 UI 컴포넌트.
    /// DOTween 등장 연출 후 확인 버튼으로 스테이지 복귀를 트리거한다.
    /// </summary>
    public class UIBattleResult : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelTransform;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _reasonText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Image _dimBackground;

        [Header("Settings")]
        [SerializeField] private float _animDuration = 0.35f;

        [Header("Colors")]
        [SerializeField] private Color _victoryColor = new Color(1f, 0.843f, 0.118f, 1f);
        [SerializeField] private Color _defeatColor = new Color(0.898f, 0.224f, 0.208f, 1f);

        #endregion

        #region Private Fields

        private const string VICTORY_TITLE = "승리";
        private const string DEFEAT_TITLE = "패배";

        private Sequence _showSequence;

        #endregion

        #region Events

        /// <summary>
        /// 결과 확인 버튼 클릭 시 발행.
        /// </summary>
        public event Action OnResultConfirmed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_resultRoot != null)
            {
                _resultRoot.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnDisable()
        {
            _showSequence?.Kill();
            _showSequence = null;

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 결과를 표시한다.
        /// </summary>
        /// <param name="result">전투 결과 (Victory/Defeat)</param>
        /// <param name="reason">전투 종료 사유</param>
        public void ShowResult(BattleResult result, BattleEndReason reason)
        {
            if (_resultRoot == null)
            {
                return;
            }

            bool isVictory = result == BattleResult.Victory;

            if (_resultTitleText != null)
            {
                _resultTitleText.text = isVictory ? VICTORY_TITLE : DEFEAT_TITLE;
                _resultTitleText.color = isVictory ? _victoryColor : _defeatColor;
            }

            if (_reasonText != null)
            {
                _reasonText.text = GetReasonText(reason);
            }

            PlayShowAnimation();
        }

        #endregion

        #region Private Methods

        private void OnConfirmClicked()
        {
            OnResultConfirmed?.Invoke();
        }

        private string GetReasonText(BattleEndReason reason)
        {
            switch (reason)
            {
                case BattleEndReason.AllEnemiesDown:
                    return "모든 적을 처치했습니다.";
                case BattleEndReason.AllAlliesDown:
                    return "모든 아군이 쓰러졌습니다.";
                case BattleEndReason.Surrender:
                    return "전투를 포기했습니다.";
                case BattleEndReason.EventTriggered:
                    return "이벤트에 의해 전투가 종료되었습니다.";
                default:
                    return string.Empty;
            }
        }

        private void PlayShowAnimation()
        {
            _resultRoot.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            if (_panelTransform != null)
            {
                _panelTransform.localScale = Vector3.zero;
            }

            _showSequence?.Kill();
            _showSequence = DOTween.Sequence();

            if (_dimBackground != null)
            {
                Color dimColor = _dimBackground.color;
                dimColor.a = 0f;
                _dimBackground.color = dimColor;
                _showSequence.Append(
                    _dimBackground.DOFade(0.5f, _animDuration));
            }

            if (_canvasGroup != null)
            {
                _showSequence.Join(
                    _canvasGroup.DOFade(1f, _animDuration));
            }

            if (_panelTransform != null)
            {
                _showSequence.Join(
                    _panelTransform.DOScale(1f, _animDuration)
                        .SetEase(Ease.OutBack));
            }
        }

        #endregion
    }
}
