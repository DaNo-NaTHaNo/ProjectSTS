using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ProjectStS.UI
{
    /// <summary>
    /// HP/방어도 게이지 바 위젯.
    /// DOTween으로 증감 애니메이션을 적용하며, 대미지 잔상 효과를 지원한다.
    /// </summary>
    public class UIHPBar : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Fill")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _damageFillImage;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _blockText;

        [Header("Block")]
        [SerializeField] private GameObject _blockRoot;

        [Header("Settings")]
        [SerializeField] private float _tweenDuration = 0.4f;
        [SerializeField] private float _damageTrailDelay = 0.2f;
        [SerializeField] private float _damageTrailDurationMultiplier = 1.5f;

        #endregion

        #region Private Fields

        private Tweener _fillTween;
        private Tweener _damageFillTween;
        private int _lastMaxHP;

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            KillTweens();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// HP 바를 갱신한다. DOTween으로 부드러운 게이지 이동을 적용한다.
        /// </summary>
        /// <param name="current">현재 HP</param>
        /// <param name="max">최대 HP</param>
        public void SetHP(int current, int max)
        {
            _lastMaxHP = max;

            float ratio = max > 0 ? (float)current / max : 0f;

            if (_hpText != null)
            {
                _hpText.text = $"{current}/{max}";
            }

            KillTweens();

            if (_fillImage != null)
            {
                _fillTween = DOTween.To(
                    () => _fillImage.fillAmount, x => _fillImage.fillAmount = x,
                    ratio, _tweenDuration
                ).SetEase(Ease.OutQuad);
            }

            if (_damageFillImage != null)
            {
                float damageDuration = _tweenDuration * _damageTrailDurationMultiplier;
                _damageFillTween = DOTween.To(
                    () => _damageFillImage.fillAmount, x => _damageFillImage.fillAmount = x,
                    ratio, damageDuration
                ).SetDelay(_damageTrailDelay).SetEase(Ease.InQuad);
            }
        }

        /// <summary>
        /// 방어도 표시를 갱신한다. 0이면 방어도 영역을 비활성화한다.
        /// </summary>
        /// <param name="block">방어도 수치</param>
        public void SetBlock(int block)
        {
            if (_blockRoot != null)
            {
                _blockRoot.SetActive(block > 0);
            }

            if (_blockText != null)
            {
                _blockText.text = block.ToString();
            }
        }

        /// <summary>
        /// 트윈 없이 즉시 HP와 방어도를 설정한다. 초기화 시 사용한다.
        /// </summary>
        /// <param name="current">현재 HP</param>
        /// <param name="max">최대 HP</param>
        /// <param name="block">방어도</param>
        public void SetImmediate(int current, int max, int block)
        {
            _lastMaxHP = max;

            KillTweens();

            float ratio = max > 0 ? (float)current / max : 0f;

            if (_fillImage != null)
            {
                _fillImage.fillAmount = ratio;
            }

            if (_damageFillImage != null)
            {
                _damageFillImage.fillAmount = ratio;
            }

            if (_hpText != null)
            {
                _hpText.text = $"{current}/{max}";
            }

            SetBlock(block);
        }

        #endregion

        #region Private Methods

        private void KillTweens()
        {
            if (_fillTween != null && _fillTween.IsActive())
            {
                _fillTween.Kill();
                _fillTween = null;
            }

            if (_damageFillTween != null && _damageFillTween.IsActive())
            {
                _damageFillTween.Kill();
                _damageFillTween = null;
            }
        }

        #endregion
    }
}
