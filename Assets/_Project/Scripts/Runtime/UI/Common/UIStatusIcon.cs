using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 상태이상 아이콘 + 스택/지속 턴 표시 위젯.
    /// 전투 유닛 패널에서 적용 중인 상태이상을 표시하는 데 사용된다.
    /// </summary>
    public class UIStatusIcon : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Icon")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _background;

        [Header("Stack")]
        [SerializeField] private TextMeshProUGUI _stackText;
        [SerializeField] private GameObject _stackRoot;

        [Header("Duration")]
        [SerializeField] private TextMeshProUGUI _durationText;
        [SerializeField] private GameObject _durationRoot;

        [Header("Colors")]
        [SerializeField] private Color _buffColor = new Color32(0x42, 0xA5, 0xF5, 0xFF);
        [SerializeField] private Color _debuffColor = new Color32(0xEF, 0x53, 0x50, 0xFF);

        #endregion

        #region Private Fields

        private string _statusEffectId;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 바인딩된 상태이상 데이터의 ID.
        /// </summary>
        public string StatusEffectId => _statusEffectId;

        #endregion

        #region Public Methods

        /// <summary>
        /// 상태이상 데이터를 바인딩한다.
        /// </summary>
        /// <param name="effect">적용 중인 상태이상</param>
        public void SetData(ActiveStatusEffect effect)
        {
            if (effect == null || effect.BaseData == null)
            {
                Clear();
                return;
            }

            _statusEffectId = effect.BaseData.id;

            ApplyBackground(effect.BaseData.statusType);
            ApplyPlaceholderIcon(effect.BaseData);
            UpdateValues(effect.CurrentStacks, effect.RemainingDuration);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 스택 수와 지속 턴만 갱신한다. 풀링 최적화를 위해 전체 재바인딩 없이 값만 변경.
        /// </summary>
        /// <param name="stacks">현재 스택 수</param>
        /// <param name="duration">남은 지속 턴</param>
        public void UpdateValues(int stacks, int duration)
        {
            bool showStacks = stacks > 1;
            if (_stackRoot != null)
            {
                _stackRoot.SetActive(showStacks);
            }
            if (_stackText != null)
            {
                _stackText.text = showStacks ? $"×{stacks}" : string.Empty;
            }

            bool showDuration = duration > 0;
            if (_durationRoot != null)
            {
                _durationRoot.SetActive(showDuration);
            }
            if (_durationText != null)
            {
                _durationText.text = showDuration ? $"{duration}T" : string.Empty;
            }
        }

        /// <summary>
        /// 위젯을 빈 상태로 초기화한다.
        /// </summary>
        public void Clear()
        {
            _statusEffectId = null;

            if (_iconImage != null)
            {
                _iconImage.color = Color.clear;
            }

            if (_background != null)
            {
                _background.color = Color.clear;
            }

            if (_stackRoot != null)
            {
                _stackRoot.SetActive(false);
            }

            if (_durationRoot != null)
            {
                _durationRoot.SetActive(false);
            }

            if (_stackText != null)
            {
                _stackText.text = string.Empty;
            }

            if (_durationText != null)
            {
                _durationText.text = string.Empty;
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void ApplyBackground(StatusType statusType)
        {
            if (_background == null)
            {
                return;
            }

            _background.color = statusType == StatusType.Buff ? _buffColor : _debuffColor;
        }

        private void ApplyPlaceholderIcon(StatusEffectData data)
        {
            if (_iconImage == null)
            {
                return;
            }

            Color elementColor = UIElementBadge.GetElementColor(data.effectElement);
            _iconImage.color = elementColor;
        }

        #endregion
    }
}
