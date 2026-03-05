using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Battle;

namespace ProjectStS.UI
{
    /// <summary>
    /// 한 유닛의 전투 상태(HP, 방어도, 상태이상, 스킬)를 표시하는 UI 컴포넌트.
    /// 아군(최대 3개)과 적(최대 5개) 유닛 모두에 공용으로 사용된다.
    /// </summary>
    public class UIBattleUnitPanel : MonoBehaviour, IPointerClickHandler
    {
        #region Serialized Fields

        [Header("Portrait")]
        [SerializeField] private UIUnitPortrait _portrait;

        [Header("HP & Block")]
        [SerializeField] private UIHPBar _hpBar;

        [Header("Status Effects")]
        [SerializeField] private Transform _statusIconContainer;
        [SerializeField] private UIStatusIcon _statusIconPrefab;

        [Header("Skill")]
        [SerializeField] private GameObject _skillRoot;
        [SerializeField] private Image _skillIcon;
        [SerializeField] private Image _skillElementBackground;
        [SerializeField] private TextMeshProUGUI _skillCooldownText;

        [Header("Enemy Intent")]
        [SerializeField] private UIEnemyIntent _enemyIntent;

        [Header("Floating Text")]
        [SerializeField] private RectTransform _floatingTextAnchor;

        [Header("Target Selection")]
        [SerializeField] private GameObject _targetHighlight;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Floating Text Settings")]
        [SerializeField] private float _floatDistance = 80f;
        [SerializeField] private float _floatDuration = 0.8f;

        [Header("Colors")]
        [SerializeField] private Color _damageTextColor = new Color(0.898f, 0.224f, 0.208f, 1f);
        [SerializeField] private Color _healTextColor = new Color(0.263f, 0.647f, 0.278f, 1f);

        #endregion

        #region Private Fields

        private BattleUnit _boundUnit;
        private readonly List<UIStatusIcon> _activeStatusIcons = new List<UIStatusIcon>(8);
        private readonly Queue<UIStatusIcon> _statusIconPool = new Queue<UIStatusIcon>(8);
        private bool _isTargetable;

        private const int INITIAL_STATUS_POOL_SIZE = 6;
        private const float DEFEATED_ALPHA = 0.3f;

        #endregion

        #region Public Properties

        /// <summary>
        /// 바인딩된 전투 유닛.
        /// </summary>
        public BattleUnit BoundUnit => _boundUnit;

        /// <summary>
        /// 타겟 선택 가능 상태 여부.
        /// </summary>
        public bool IsTargetable => _isTargetable;

        #endregion

        #region Events

        /// <summary>
        /// 유닛 패널 클릭 시 발행. 타겟 선택 모드에서 사용된다.
        /// </summary>
        public event Action<BattleUnit> OnUnitClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeStatusIconPool();

            if (_targetHighlight != null)
            {
                _targetHighlight.SetActive(false);
            }
        }

        private void OnDisable()
        {
            DOTween.Kill(GetInstanceID());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 전투 유닛을 바인딩하고 초기 상태를 표시한다.
        /// </summary>
        /// <param name="unit">전투 유닛</param>
        public void SetUnit(BattleUnit unit)
        {
            _boundUnit = unit;

            if (unit == null)
            {
                Clear();
                return;
            }

            gameObject.SetActive(true);

            if (_portrait != null && unit.BaseData != null)
            {
                _portrait.SetData(unit.BaseData);
            }

            if (_hpBar != null)
            {
                _hpBar.SetImmediate(unit.CurrentHP, unit.MaxHP, unit.Block);
            }

            UpdateStatusEffects(unit.StatusEffects);
            UpdateSkillDisplay(unit);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            if (_enemyIntent != null)
            {
                _enemyIntent.HideIntent();
            }
        }

        /// <summary>
        /// HP를 갱신한다.
        /// </summary>
        /// <param name="currentHP">현재 HP</param>
        /// <param name="maxHP">최대 HP</param>
        public void UpdateHP(int currentHP, int maxHP)
        {
            if (_hpBar != null)
            {
                _hpBar.SetHP(currentHP, maxHP);
            }
        }

        /// <summary>
        /// 방어도를 갱신한다.
        /// </summary>
        /// <param name="block">현재 방어도</param>
        public void UpdateBlock(int block)
        {
            if (_hpBar != null)
            {
                _hpBar.SetBlock(block);
            }
        }

        /// <summary>
        /// 상태이상 아이콘을 전체 갱신한다.
        /// </summary>
        /// <param name="effects">현재 적용된 상태이상 목록</param>
        public void UpdateStatusEffects(List<ActiveStatusEffect> effects)
        {
            ReturnAllStatusIcons();

            if (effects == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                UIStatusIcon icon = GetStatusIconFromPool();
                icon.SetData(effects[i]);
                _activeStatusIcons.Add(icon);
            }
        }

        /// <summary>
        /// 대미지 플로팅 텍스트를 표시한다.
        /// </summary>
        /// <param name="amount">대미지량</param>
        public void ShowDamageText(int amount)
        {
            ShowFloatingText($"-{amount}", _damageTextColor);
        }

        /// <summary>
        /// 회복 플로팅 텍스트를 표시한다.
        /// </summary>
        /// <param name="amount">회복량</param>
        public void ShowHealText(int amount)
        {
            ShowFloatingText($"+{amount}", _healTextColor);
        }

        /// <summary>
        /// 유닛 사망 연출을 재생한다.
        /// </summary>
        public void ShowDefeated()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(DEFEATED_ALPHA, 0.5f)
                    .SetId(GetInstanceID());
            }

            SetTargetable(false);
        }

        /// <summary>
        /// 타겟 선택 가능 상태를 설정한다.
        /// </summary>
        /// <param name="targetable">선택 가능 여부</param>
        public void SetTargetable(bool targetable)
        {
            _isTargetable = targetable;

            if (_targetHighlight != null)
            {
                _targetHighlight.SetActive(targetable);
            }
        }

        /// <summary>
        /// 적 행동 의도를 표시한다.
        /// </summary>
        /// <param name="decision">AI 결정 데이터</param>
        public void ShowEnemyIntent(AIDecision decision)
        {
            if (_enemyIntent != null)
            {
                _enemyIntent.ShowIntent(decision);
            }
        }

        /// <summary>
        /// 적 행동 의도를 숨긴다.
        /// </summary>
        public void HideEnemyIntent()
        {
            if (_enemyIntent != null)
            {
                _enemyIntent.HideIntent();
            }
        }

        /// <summary>
        /// 패널을 초기화한다.
        /// </summary>
        public void Clear()
        {
            _boundUnit = null;
            gameObject.SetActive(false);
            ReturnAllStatusIcons();
            SetTargetable(false);

            if (_portrait != null)
            {
                _portrait.Clear();
            }

            if (_enemyIntent != null)
            {
                _enemyIntent.HideIntent();
            }
        }

        #endregion

        #region IPointerClickHandler

        /// <summary>
        /// 패널 클릭 시 타겟 선택 이벤트를 발행한다.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isTargetable && _boundUnit != null && _boundUnit.IsAlive)
            {
                OnUnitClicked?.Invoke(_boundUnit);
            }
        }

        #endregion

        #region Private Methods

        private void InitializeStatusIconPool()
        {
            if (_statusIconPrefab == null || _statusIconContainer == null)
            {
                return;
            }

            for (int i = 0; i < INITIAL_STATUS_POOL_SIZE; i++)
            {
                UIStatusIcon icon = Instantiate(_statusIconPrefab, _statusIconContainer);
                icon.Clear();
                _statusIconPool.Enqueue(icon);
            }
        }

        private UIStatusIcon GetStatusIconFromPool()
        {
            if (_statusIconPool.Count > 0)
            {
                return _statusIconPool.Dequeue();
            }

            if (_statusIconPrefab != null && _statusIconContainer != null)
            {
                return Instantiate(_statusIconPrefab, _statusIconContainer);
            }

            return null;
        }

        private void ReturnAllStatusIcons()
        {
            for (int i = 0; i < _activeStatusIcons.Count; i++)
            {
                UIStatusIcon icon = _activeStatusIcons[i];

                if (icon != null)
                {
                    icon.Clear();
                    _statusIconPool.Enqueue(icon);
                }
            }

            _activeStatusIcons.Clear();
        }

        private void UpdateSkillDisplay(BattleUnit unit)
        {
            if (_skillRoot == null)
            {
                return;
            }

            bool hasSkill = !string.IsNullOrEmpty(unit.SkillId);
            _skillRoot.SetActive(hasSkill);

            if (!hasSkill)
            {
                return;
            }

            if (_skillElementBackground != null)
            {
                _skillElementBackground.color = UIElementBadge.GetElementColor(unit.Element);
            }

            if (_skillCooldownText != null)
            {
                UpdateSkillCooldownText(unit);
            }
        }

        private void UpdateSkillCooldownText(BattleUnit unit)
        {
            if (_skillCooldownText == null)
            {
                return;
            }

            if (unit.SkillCooldownRemaining > 0)
            {
                _skillCooldownText.text = $"{unit.SkillCooldownRemaining}T";
                _skillCooldownText.gameObject.SetActive(true);
            }
            else
            {
                _skillCooldownText.gameObject.SetActive(false);
            }
        }

        private void ShowFloatingText(string text, Color color)
        {
            if (_floatingTextAnchor == null)
            {
                return;
            }

            GameObject textObj = new GameObject("FloatingText");
            textObj.transform.SetParent(_floatingTextAnchor, false);

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var rectTransform = textObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(160f, 40f);

            var canvasGroup = textObj.AddComponent<CanvasGroup>();

            Sequence seq = DOTween.Sequence();
            seq.SetId(GetInstanceID());
            seq.Append(rectTransform.DOAnchorPosY(_floatDistance, _floatDuration)
                .SetRelative(true)
                .SetEase(Ease.OutCubic));
            seq.Join(canvasGroup.DOFade(0f, _floatDuration)
                .SetEase(Ease.InQuad));
            seq.OnComplete(() => Destroy(textObj));
        }

        #endregion
    }
}
