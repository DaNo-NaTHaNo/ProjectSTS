using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 유닛 초상화 + 기본 정보 위젯.
    /// 로비 파티 편성, 전투 유닛 패널 등에서 공용으로 사용된다.
    /// 아트 에셋 미완성 시 속성 색상 원형 + 이름 텍스트로 Placeholder를 표시한다.
    /// </summary>
    public class UIUnitPortrait : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Portrait")]
        [SerializeField] private Image _portraitImage;
        [SerializeField] private TextMeshProUGUI _placeholderNameText;

        [Header("Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private UIElementBadge _elementBadge;

        [Header("Frame")]
        [SerializeField] private Image _frameBorder;
        [SerializeField] private Color _highlightColor = new Color32(0xFF, 0xD5, 0x4F, 0xFF);
        [SerializeField] private Color _normalColor = new Color32(0x42, 0x42, 0x42, 0xFF);

        #endregion

        #region Private Fields

        private UnitData _unitData;
        private bool _isHighlighted;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 바인딩된 유닛 마스터 데이터.
        /// </summary>
        public UnitData CurrentUnitData => _unitData;

        /// <summary>
        /// 하이라이트 활성 여부.
        /// </summary>
        public bool IsHighlighted => _isHighlighted;

        #endregion

        #region Public Methods

        /// <summary>
        /// 기본 유닛 데이터를 바인딩한다.
        /// </summary>
        /// <param name="unit">유닛 마스터 데이터</param>
        public void SetData(UnitData unit)
        {
            _unitData = unit;

            if (unit == null)
            {
                Clear();
                return;
            }

            ApplyVisuals(unit);
        }

        /// <summary>
        /// 보유 유닛 데이터를 함께 바인딩한다. 로비 파티 편성에서 사용.
        /// </summary>
        /// <param name="unit">유닛 마스터 데이터</param>
        /// <param name="owned">보유 유닛 편성 데이터</param>
        public void SetData(UnitData unit, OwnedUnitData owned)
        {
            _unitData = unit;

            if (unit == null)
            {
                Clear();
                return;
            }

            ApplyVisuals(unit);
        }

        /// <summary>
        /// 테두리 하이라이트를 토글한다.
        /// </summary>
        /// <param name="active">활성 여부</param>
        public void SetHighlight(bool active)
        {
            _isHighlighted = active;

            if (_frameBorder != null)
            {
                _frameBorder.color = active ? _highlightColor : _normalColor;
            }
        }

        /// <summary>
        /// 위젯을 빈 상태로 리셋한다.
        /// </summary>
        public void Clear()
        {
            _unitData = null;

            if (_portraitImage != null)
            {
                _portraitImage.color = Color.clear;
            }

            if (_placeholderNameText != null)
            {
                _placeholderNameText.text = string.Empty;
            }

            if (_nameText != null)
            {
                _nameText.text = string.Empty;
            }

            if (_elementBadge != null)
            {
                _elementBadge.Clear();
            }

            SetHighlight(false);
        }

        #endregion

        #region Private Methods

        private void ApplyVisuals(UnitData unit)
        {
            if (_nameText != null)
            {
                _nameText.text = unit.unitName;
            }

            if (_elementBadge != null)
            {
                _elementBadge.SetElement(unit.element);
            }

            ApplyPlaceholderPortrait(unit);
        }

        private void ApplyPlaceholderPortrait(UnitData unit)
        {
            if (_portraitImage != null)
            {
                Color elementColor = UIElementBadge.GetElementColor(unit.element);
                _portraitImage.color = elementColor;
            }

            if (_placeholderNameText != null)
            {
                _placeholderNameText.text = unit.unitName;
            }
        }

        #endregion
    }
}
