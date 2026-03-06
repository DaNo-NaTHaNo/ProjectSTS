using UnityEngine;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.UI
{
    /// <summary>
    /// 추적 캠페인 HUD 오버레이.
    /// 현재 추적 중인 캠페인의 이름과 현재 목표를 표시한다.
    /// 로비 메인 화면 및 스테이지에서 공용으로 사용된다.
    /// </summary>
    public class UICampaignTracker : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI _campaignNameText;
        [SerializeField] private TextMeshProUGUI _currentGoalText;

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.3f;

        #endregion

        #region Private Fields

        private bool _isVisible;

        #endregion

        #region Public Methods

        /// <summary>
        /// 캠페인 매니저의 추적 정보로 트래커를 초기화한다.
        /// </summary>
        /// <param name="campaignManager">캠페인 매니저</param>
        public void Initialize(CampaignManager campaignManager)
        {
            Refresh(campaignManager);
        }

        /// <summary>
        /// 추적 캠페인/목표를 갱신한다.
        /// </summary>
        /// <param name="campaignManager">캠페인 매니저</param>
        public void Refresh(CampaignManager campaignManager)
        {
            if (campaignManager == null)
            {
                Hide();
                return;
            }

            CampaignData tracked = campaignManager.GetTrackedCampaign();

            if (tracked == null)
            {
                Hide();
                return;
            }

            if (_campaignNameText != null)
            {
                _campaignNameText.text = tracked.name;
            }

            CampaignGoalGroupData currentGoal = campaignManager.GetTrackedCurrentGoal();

            if (_currentGoalText != null)
            {
                _currentGoalText.text = currentGoal != null ? currentGoal.name : "목표 완료";
            }

            Show();
        }

        /// <summary>
        /// 트래커를 표시한다.
        /// </summary>
        public void Show()
        {
            if (_isVisible)
            {
                return;
            }

            _isVisible = true;

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

        /// <summary>
        /// 트래커를 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible)
            {
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .OnComplete(() =>
                    {
                        _isVisible = false;

                        if (_panelRoot != null)
                        {
                            _panelRoot.SetActive(false);
                        }
                    });
            }
            else
            {
                _isVisible = false;

                if (_panelRoot != null)
                {
                    _panelRoot.SetActive(false);
                }
            }
        }

        #endregion
    }
}
