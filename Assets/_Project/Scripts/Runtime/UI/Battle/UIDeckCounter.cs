using UnityEngine;
using TMPro;

namespace ProjectStS.UI
{
    /// <summary>
    /// 드로우 파일과 디스카드 파일의 카드 수를 표시하는 UI 컴포넌트.
    /// </summary>
    public class UIDeckCounter : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private TextMeshProUGUI _drawPileText;
        [SerializeField] private TextMeshProUGUI _discardPileText;

        #endregion

        #region Public Methods

        /// <summary>
        /// 덱/묘지 카운트를 갱신한다.
        /// </summary>
        /// <param name="drawCount">드로우 파일 카드 수</param>
        /// <param name="discardCount">디스카드 파일 카드 수</param>
        public void UpdateCounts(int drawCount, int discardCount)
        {
            if (_drawPileText != null)
            {
                _drawPileText.text = drawCount.ToString();
            }

            if (_discardPileText != null)
            {
                _discardPileText.text = discardCount.ToString();
            }
        }

        #endregion
    }
}
