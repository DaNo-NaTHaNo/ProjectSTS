using UnityEngine;
using TMPro;
using DG.Tweening;

namespace ProjectStS.UI
{
    /// <summary>
    /// 전투 에너지(현재/기본)를 텍스트로 표시하는 UI 컴포넌트.
    /// 에너지 변경 시 DOTween Scale 펀치 연출을 재생한다.
    /// </summary>
    public class UIEnergyDisplay : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _energyText;
        [SerializeField] private RectTransform _punchTarget;

        [Header("Settings")]
        [SerializeField] private float _punchScale = 0.2f;
        [SerializeField] private float _punchDuration = 0.3f;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _lowColor = new Color(0.9f, 0.3f, 0.3f, 1f);

        #endregion

        #region Private Fields

        private int _baseEnergy;
        private int _currentEnergy;
        private Tweener _punchTween;

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            _punchTween?.Kill();
            _punchTween = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 기본 에너지 값을 설정한다.
        /// </summary>
        /// <param name="baseEnergy">파티 합산 기본 에너지</param>
        public void SetBaseEnergy(int baseEnergy)
        {
            _baseEnergy = baseEnergy;
        }

        /// <summary>
        /// 현재 에너지를 갱신하고 연출을 재생한다.
        /// </summary>
        /// <param name="currentEnergy">현재 에너지 값</param>
        public void UpdateEnergy(int currentEnergy)
        {
            _currentEnergy = currentEnergy;
            RefreshDisplay();
            PlayPunchAnimation();
        }

        /// <summary>
        /// 에너지를 즉시 설정한다 (초기화용, 연출 없음).
        /// </summary>
        /// <param name="currentEnergy">현재 에너지</param>
        /// <param name="baseEnergy">기본 에너지</param>
        public void SetImmediate(int currentEnergy, int baseEnergy)
        {
            _baseEnergy = baseEnergy;
            _currentEnergy = currentEnergy;
            RefreshDisplay();
        }

        #endregion

        #region Private Methods

        private void RefreshDisplay()
        {
            if (_energyText == null)
            {
                return;
            }

            _energyText.text = $"{_currentEnergy}/{_baseEnergy}";
            _energyText.color = _currentEnergy <= 0 ? _lowColor : _normalColor;
        }

        private void PlayPunchAnimation()
        {
            if (_punchTarget == null)
            {
                return;
            }

            _punchTween?.Kill();
            _punchTarget.localScale = Vector3.one;
            _punchTween = _punchTarget.DOPunchScale(Vector3.one * _punchScale, _punchDuration, 1, 0.5f);
        }

        #endregion
    }
}
