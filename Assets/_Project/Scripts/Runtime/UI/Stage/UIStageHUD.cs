using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 스테이지 HUD 오버레이.
    /// 행동력(AP), 구역 정보, 이벤트 완료 카운터를 표시한다.
    /// </summary>
    public class UIStageHUD : MonoBehaviour
    {
        #region Serialized Fields

        [Header("AP Display")]
        [SerializeField] private TextMeshProUGUI _apText;
        [SerializeField] private Image _apFillBar;
        [SerializeField] private float _apPunchScale = 1.2f;
        [SerializeField] private float _apAnimDuration = 0.3f;

        [Header("Zone Info")]
        [SerializeField] private TextMeshProUGUI _zoneNameText;
        [SerializeField] private TextMeshProUGUI _zoneDescText;
        [SerializeField] private CanvasGroup _zoneInfoGroup;
        [SerializeField] private float _zoneFadeDuration = 0.5f;

        [Header("Stage Info")]
        [SerializeField] private TextMeshProUGUI _eventCountText;

        #endregion

        #region Private Fields

        private int _currentAP;
        private int _maxAP;
        private RectTransform _apTextRT;
        private Tweener _apPunchTween;
        private Sequence _zoneFadeSequence;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_apText != null)
            {
                _apTextRT = _apText.GetComponent<RectTransform>();
            }
        }

        private void OnDisable()
        {
            KillApTween();
            KillZoneSequence();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// HUD를 초기값으로 세팅한다. 애니메이션 없이 즉시 적용.
        /// </summary>
        /// <param name="ap">현재 AP</param>
        /// <param name="maxAP">최대 AP</param>
        public void InitializeHUD(int ap, int maxAP)
        {
            _currentAP = ap;
            _maxAP = maxAP;

            UpdateAPDisplay(false);
        }

        /// <summary>
        /// AP 값을 갱신한다. DOTween 펀치 스케일 애니메이션 포함.
        /// </summary>
        /// <param name="current">현재 AP</param>
        /// <param name="max">최대 AP</param>
        public void SetAP(int current, int max)
        {
            _currentAP = current;
            _maxAP = max;

            UpdateAPDisplay(true);
        }

        /// <summary>
        /// 구역 정보를 갱신한다. 페이드 연출 포함.
        /// </summary>
        /// <param name="area">구역 데이터</param>
        public void SetZoneInfo(AreaData area)
        {
            if (area == null)
            {
                return;
            }

            KillZoneSequence();

            _zoneFadeSequence = DOTween.Sequence();

            if (_zoneInfoGroup != null)
            {
                _zoneFadeSequence.Append(_zoneInfoGroup.DOFade(0f, _zoneFadeDuration * 0.3f));
            }

            _zoneFadeSequence.AppendCallback(() =>
            {
                if (_zoneNameText != null)
                {
                    _zoneNameText.text = area.name ?? string.Empty;
                }

                if (_zoneDescText != null)
                {
                    _zoneDescText.text = area.description ?? string.Empty;
                }
            });

            if (_zoneInfoGroup != null)
            {
                _zoneFadeSequence.Append(_zoneInfoGroup.DOFade(1f, _zoneFadeDuration * 0.7f));
            }
        }

        /// <summary>
        /// 이벤트 완료 카운터를 갱신한다.
        /// </summary>
        /// <param name="count">완료 이벤트 수</param>
        public void SetEventCount(int count)
        {
            if (_eventCountText != null)
            {
                _eventCountText.text = $"이벤트: {count}";
            }
        }

        #endregion

        #region Private Methods

        private void UpdateAPDisplay(bool animate)
        {
            if (_apText != null)
            {
                _apText.text = $"AP: {_currentAP} / {_maxAP}";
            }

            if (_apFillBar != null)
            {
                float fill = _maxAP > 0 ? (float)_currentAP / _maxAP : 0f;
                _apFillBar.fillAmount = fill;
            }

            if (animate && _apTextRT != null)
            {
                KillApTween();

                _apPunchTween = _apTextRT.DOPunchScale(
                    Vector3.one * (_apPunchScale - 1f),
                    _apAnimDuration,
                    vibrato: 1,
                    elasticity: 0.5f
                );
            }
        }

        private void KillApTween()
        {
            if (_apPunchTween != null && _apPunchTween.IsActive())
            {
                _apPunchTween.Kill();
                _apPunchTween = null;
            }

            if (_apTextRT != null)
            {
                _apTextRT.localScale = Vector3.one;
            }
        }

        private void KillZoneSequence()
        {
            if (_zoneFadeSequence != null && _zoneFadeSequence.IsActive())
            {
                _zoneFadeSequence.Kill();
                _zoneFadeSequence = null;
            }
        }

        #endregion
    }
}
