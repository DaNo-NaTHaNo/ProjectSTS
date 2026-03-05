using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;

namespace ProjectStS.UI
{
    /// <summary>
    /// 속성 타입을 색상 + 텍스트로 표시하는 배지 위젯.
    /// 카드, 유닛 초상화 등 다른 UI 위젯의 하위 컴포넌트로 재사용된다.
    /// </summary>
    public class UIElementBadge : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _label;

        #endregion

        #region Private Fields

        private static readonly Dictionary<ElementType, ElementDisplayInfo> ELEMENT_DISPLAY_MAP =
            new Dictionary<ElementType, ElementDisplayInfo>(7)
            {
                { ElementType.Sword, new ElementDisplayInfo("바람", new Color32(0x4C, 0xAF, 0x50, 0xFF)) },
                { ElementType.Baton, new ElementDisplayInfo("불",   new Color32(0xF4, 0x43, 0x36, 0xFF)) },
                { ElementType.Medal, new ElementDisplayInfo("땅",   new Color32(0xFF, 0x98, 0x00, 0xFF)) },
                { ElementType.Grail, new ElementDisplayInfo("물",   new Color32(0x21, 0x96, 0xF3, 0xFF)) },
                { ElementType.Sola,  new ElementDisplayInfo("빛",   new Color32(0xFF, 0xC1, 0x07, 0xFF)) },
                { ElementType.Luna,  new ElementDisplayInfo("어둠", new Color32(0x9C, 0x27, 0xB0, 0xFF)) },
                { ElementType.Wild,  new ElementDisplayInfo("무",   new Color32(0x9E, 0x9E, 0x9E, 0xFF)) },
            };

        #endregion

        #region Public Methods

        /// <summary>
        /// 지정한 속성에 맞는 색상과 텍스트를 설정한다.
        /// </summary>
        /// <param name="element">표시할 속성 타입</param>
        public void SetElement(ElementType element)
        {
            if (ELEMENT_DISPLAY_MAP.TryGetValue(element, out ElementDisplayInfo info))
            {
                if (_background != null)
                {
                    _background.color = info.Color;
                }

                if (_label != null)
                {
                    _label.text = info.DisplayName;
                }
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 배지를 빈 상태로 리셋한다.
        /// </summary>
        public void Clear()
        {
            if (_background != null)
            {
                _background.color = Color.clear;
            }

            if (_label != null)
            {
                _label.text = string.Empty;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 지정한 속성 타입에 해당하는 색상을 반환한다.
        /// 다른 위젯에서 속성 색상을 참조할 때 사용한다.
        /// </summary>
        /// <param name="element">속성 타입</param>
        /// <returns>해당 속성의 표시 색상</returns>
        public static Color GetElementColor(ElementType element)
        {
            if (ELEMENT_DISPLAY_MAP.TryGetValue(element, out ElementDisplayInfo info))
            {
                return info.Color;
            }

            return Color.gray;
        }

        /// <summary>
        /// 지정한 속성 타입에 해당하는 표시명을 반환한다.
        /// </summary>
        /// <param name="element">속성 타입</param>
        /// <returns>해당 속성의 한국어 표시명</returns>
        public static string GetElementDisplayName(ElementType element)
        {
            if (ELEMENT_DISPLAY_MAP.TryGetValue(element, out ElementDisplayInfo info))
            {
                return info.DisplayName;
            }

            return string.Empty;
        }

        #endregion

        #region Inner Types

        /// <summary>
        /// 속성별 표시 정보 (이름, 색상).
        /// </summary>
        private readonly struct ElementDisplayInfo
        {
            public readonly string DisplayName;
            public readonly Color Color;

            public ElementDisplayInfo(string displayName, Color color)
            {
                DisplayName = displayName;
                Color = color;
            }
        }

        #endregion
    }
}
