using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

namespace ProjectStS.UI
{
    /// <summary>
    /// DOTween UI 확장 메서드 래퍼.
    /// DOTweenModuleUI(Assembly-CSharp-firstpass)가 asmdef 어셈블리에서 접근 불가하므로
    /// DOTween.To() 제너릭 API로 동일 기능을 제공한다.
    /// </summary>
    public static class DOTweenUIExtensions
    {
        #region CanvasGroup

        /// <summary>
        /// CanvasGroup의 alpha를 트윈한다.
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOFade(
            this CanvasGroup target, float endValue, float duration)
        {
            return DOTween.To(() => target.alpha, x => target.alpha = x, endValue, duration)
                .SetTarget(target);
        }

        #endregion

        #region Image

        /// <summary>
        /// Image의 color alpha를 트윈한다.
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOFade(
            this Image target, float endValue, float duration)
        {
            return DOTween.To(
                () => target.color.a,
                x =>
                {
                    Color c = target.color;
                    c.a = x;
                    target.color = c;
                },
                endValue, duration
            ).SetTarget(target);
        }

        #endregion

        #region RectTransform

        /// <summary>
        /// RectTransform의 anchoredPosition을 트윈한다.
        /// </summary>
        public static Tweener DOAnchorPos(
            this RectTransform target, Vector2 endValue, float duration, bool snapping = false)
        {
            return DOTween.To(
                () => target.anchoredPosition,
                x => target.anchoredPosition = x,
                endValue, duration
            ).SetOptions(snapping).SetTarget(target);
        }

        /// <summary>
        /// RectTransform의 anchoredPosition.y를 트윈한다.
        /// </summary>
        public static TweenerCore<float, float, FloatOptions> DOAnchorPosY(
            this RectTransform target, float endValue, float duration, bool snapping = false)
        {
            return DOTween.To(
                () => target.anchoredPosition.y,
                y => target.anchoredPosition = new Vector2(target.anchoredPosition.x, y),
                endValue, duration
            ).SetTarget(target);
        }

        #endregion
    }
}
