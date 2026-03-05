using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 스킬 발동 시 화면 중앙에 유닛명과 스킬명을 팝업으로 알리는 UI 컴포넌트.
    /// 연속 발동 시 대기열로 순차 재생하며, DOTween Scale+Fade 연출을 사용한다.
    /// </summary>
    public class UISkillNotify : MonoBehaviour
    {
        #region Inner Types

        private struct NotifyEntry
        {
            public string UnitName;
            public string SkillName;
            public ElementType Element;
        }

        #endregion

        #region Serialized Fields

        [Header("References")]
        [SerializeField] private CanvasGroup _notifyRoot;
        [SerializeField] private RectTransform _panelTransform;
        [SerializeField] private TextMeshProUGUI _unitNameText;
        [SerializeField] private TextMeshProUGUI _skillNameText;
        [SerializeField] private UIElementBadge _elementBadge;

        [Header("Settings")]
        [SerializeField] private float _displayDuration = 1.5f;
        [SerializeField] private float _fadeInDuration = 0.25f;
        [SerializeField] private float _fadeOutDuration = 0.3f;
        [SerializeField] private float _overshootScale = 1.2f;

        #endregion

        #region Private Fields

        private readonly Queue<NotifyEntry> _notifyQueue = new Queue<NotifyEntry>(8);
        private bool _isPlaying;
        private Sequence _currentSequence;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_notifyRoot != null)
            {
                _notifyRoot.alpha = 0f;
                _notifyRoot.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            _currentSequence?.Kill();
            _currentSequence = null;
            _isPlaying = false;
            _notifyQueue.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스킬 발동 알림을 대기열에 추가하고 재생을 시작한다.
        /// </summary>
        /// <param name="unit">스킬 발동 유닛</param>
        /// <param name="skill">발동된 스킬 데이터</param>
        public void EnqueueNotify(BattleUnit unit, SkillData skill)
        {
            if (unit == null || skill == null)
            {
                return;
            }

            var entry = new NotifyEntry
            {
                UnitName = unit.BaseData != null ? unit.BaseData.unitName : unit.UnitId,
                SkillName = skill.skillName,
                Element = skill.element
            };

            _notifyQueue.Enqueue(entry);

            if (!_isPlaying)
            {
                PlayNext();
            }
        }

        #endregion

        #region Private Methods

        private void PlayNext()
        {
            if (_notifyQueue.Count == 0)
            {
                _isPlaying = false;
                return;
            }

            _isPlaying = true;
            NotifyEntry entry = _notifyQueue.Dequeue();
            PlayNotify(entry);
        }

        private void PlayNotify(NotifyEntry entry)
        {
            if (_notifyRoot == null || _panelTransform == null)
            {
                _isPlaying = false;
                return;
            }

            if (_unitNameText != null)
            {
                _unitNameText.text = entry.UnitName;
            }

            if (_skillNameText != null)
            {
                _skillNameText.text = entry.SkillName;
            }

            if (_elementBadge != null)
            {
                _elementBadge.SetElement(entry.Element);
            }

            _notifyRoot.gameObject.SetActive(true);
            _notifyRoot.alpha = 0f;
            _panelTransform.localScale = Vector3.zero;

            _currentSequence?.Kill();
            _currentSequence = DOTween.Sequence();

            _currentSequence.Append(_notifyRoot.DOFade(1f, _fadeInDuration));
            _currentSequence.Join(
                _panelTransform.DOScale(_overshootScale, _fadeInDuration)
                    .SetEase(Ease.OutBack));
            _currentSequence.Append(
                _panelTransform.DOScale(1f, _fadeInDuration * 0.5f)
                    .SetEase(Ease.InOutSine));
            _currentSequence.AppendInterval(_displayDuration);
            _currentSequence.Append(_notifyRoot.DOFade(0f, _fadeOutDuration));
            _currentSequence.OnComplete(() =>
            {
                _notifyRoot.gameObject.SetActive(false);
                PlayNext();
            });
        }

        #endregion
    }
}
