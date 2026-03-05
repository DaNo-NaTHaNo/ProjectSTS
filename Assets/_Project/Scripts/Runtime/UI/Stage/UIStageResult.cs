using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Stage;

namespace ProjectStS.UI
{
    /// <summary>
    /// 스테이지 결과/보상 선택 화면.
    /// 탐험 완료/실패 결과를 표시하고, 고정 보상 선택 및 완료/추가 보상을 보여준다.
    /// </summary>
    public class UIStageResult : MonoBehaviour
    {
        #region Constants

        private static readonly Color32 COLOR_VICTORY = new Color32(0xFF, 0xC1, 0x07, 0xFF);
        private static readonly Color32 COLOR_FAILURE = new Color32(0xF4, 0x43, 0x36, 0xFF);
        private static readonly Color32 COLOR_SELECTED = new Color32(0x4C, 0xAF, 0x50, 0xC0);
        private static readonly Color32 COLOR_UNSELECTED = new Color32(0xFF, 0xFF, 0xFF, 0x00);

        #endregion

        #region Serialized Fields

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultRoot;
        [SerializeField] private Image _dimBackground;
        [SerializeField] private RectTransform _panelTransform;

        [Header("Result Info")]
        [SerializeField] private TextMeshProUGUI _resultTitle;
        [SerializeField] private TextMeshProUGUI _resultReasonText;
        [SerializeField] private Image _resultBanner;

        [Header("Fixed Rewards")]
        [SerializeField] private GameObject _fixedRewardArea;
        [SerializeField] private TextMeshProUGUI _fixedRewardLabel;
        [SerializeField] private RectTransform _fixedRewardContainer;

        [Header("Completion Rewards")]
        [SerializeField] private GameObject _completionRewardArea;
        [SerializeField] private TextMeshProUGUI _completionRewardLabel;
        [SerializeField] private RectTransform _completionRewardContainer;

        [Header("Bonus Rewards")]
        [SerializeField] private GameObject _bonusRewardArea;
        [SerializeField] private TextMeshProUGUI _bonusRewardLabel;
        [SerializeField] private RectTransform _bonusRewardContainer;

        [Header("Confirm")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private TextMeshProUGUI _confirmButtonText;

        [Header("Prefab")]
        [SerializeField] private UIItemIcon _rewardIconPrefab;

        [Header("Settings")]
        [SerializeField] private float _animDuration = 0.5f;
        [SerializeField] private int _iconPoolSize = 20;

        #endregion

        #region Private Fields

        private StageSettlementData _settlementData;
        private int _maxSelectCount;
        private readonly List<UIItemIcon> _iconPool = new List<UIItemIcon>(20);
        private readonly List<UIItemIcon> _activeIcons = new List<UIItemIcon>(20);
        private readonly List<int> _selectedFixedIndices = new List<int>(5);
        private readonly List<InGameBagItemData> _fixedCandidates = new List<InGameBagItemData>(10);
        private readonly Dictionary<UIItemIcon, int> _iconToIndex = new Dictionary<UIItemIcon, int>(10);
        private Action<List<InGameBagItemData>> _onConfirm;
        private Sequence _currentSequence;
        private bool _isShowing;

        #endregion

        #region Events

        /// <summary>
        /// 보상 선택이 확정되었을 때.
        /// </summary>
        public event Action<List<InGameBagItemData>> OnRewardSelectionConfirmed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_resultRoot != null)
            {
                _resultRoot.SetActive(false);
            }

            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnDestroy()
        {
            KillSequence();

            if (_confirmButton != null)
            {
                _confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 스테이지 결과 화면을 표시한다.
        /// </summary>
        /// <param name="data">보상 정산 데이터</param>
        /// <param name="onConfirm">확인 콜백 (선택된 고정 보상 목록)</param>
        public void Show(StageSettlementData data, Action<List<InGameBagItemData>> onConfirm)
        {
            if (data == null)
            {
                return;
            }

            _settlementData = data;
            _onConfirm = onConfirm;
            _selectedFixedIndices.Clear();
            _fixedCandidates.Clear();
            _iconToIndex.Clear();

            InitializePoolIfNeeded();
            ClearActiveIcons();

            PopulateResultInfo(data.Result, data.EndReason);
            PopulateFixedRewards(data.FixedRewardCandidates, data.FixedRewardSelectCount);
            PopulateRewardList(data.CompletionRewards, _completionRewardArea, _completionRewardLabel,
                _completionRewardContainer, "완료 보상");
            PopulateRewardList(data.BonusRewards, _bonusRewardArea, _bonusRewardLabel,
                _bonusRewardContainer, "추가 보상");

            UpdateConfirmButton();
            PlayOpenAnimation();
        }

        /// <summary>
        /// 결과 화면을 닫는다.
        /// </summary>
        public void Hide()
        {
            if (!_isShowing)
            {
                return;
            }

            PlayCloseAnimation();
        }

        #endregion

        #region Private Methods — Content

        private void PopulateResultInfo(StageResult result, StageEndReason reason)
        {
            if (_resultTitle != null)
            {
                _resultTitle.text = GetResultTitle(result);
            }

            if (_resultReasonText != null)
            {
                _resultReasonText.text = GetReasonText(reason);
            }

            if (_resultBanner != null)
            {
                _resultBanner.color = result == StageResult.Victory ? COLOR_VICTORY : COLOR_FAILURE;
            }
        }

        private void PopulateFixedRewards(List<InGameBagItemData> candidates, int selectCount)
        {
            _maxSelectCount = selectCount;

            bool hasFixedRewards = candidates != null && candidates.Count > 0 && selectCount > 0;

            if (_fixedRewardArea != null)
            {
                _fixedRewardArea.SetActive(hasFixedRewards);
            }

            if (!hasFixedRewards)
            {
                return;
            }

            _fixedCandidates.AddRange(candidates);

            if (_fixedRewardLabel != null)
            {
                _fixedRewardLabel.text = $"보상 {selectCount}개를 선택하세요";
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                UIItemIcon icon = GetOrCreateIcon(_fixedRewardContainer);

                if (icon == null)
                {
                    continue;
                }

                BindBagItemToIcon(icon, candidates[i]);
                _iconToIndex[icon] = i;

                Button iconButton = icon.GetComponent<Button>();

                if (iconButton == null)
                {
                    iconButton = icon.gameObject.AddComponent<Button>();
                }

                int index = i;
                iconButton.onClick.RemoveAllListeners();
                iconButton.onClick.AddListener(() => HandleFixedRewardClicked(icon, index));
            }
        }

        private void PopulateRewardList(
            List<InGameBagItemData> rewards,
            GameObject areaRoot,
            TextMeshProUGUI label,
            RectTransform container,
            string labelText)
        {
            bool hasRewards = rewards != null && rewards.Count > 0;

            if (areaRoot != null)
            {
                areaRoot.SetActive(hasRewards);
            }

            if (!hasRewards)
            {
                return;
            }

            if (label != null)
            {
                label.text = labelText;
            }

            for (int i = 0; i < rewards.Count; i++)
            {
                UIItemIcon icon = GetOrCreateIcon(container);

                if (icon != null)
                {
                    BindBagItemToIcon(icon, rewards[i]);
                }
            }
        }

        private void HandleFixedRewardClicked(UIItemIcon icon, int index)
        {
            if (_selectedFixedIndices.Contains(index))
            {
                _selectedFixedIndices.Remove(index);
                SetIconSelected(icon, false);
            }
            else
            {
                if (_selectedFixedIndices.Count >= _maxSelectCount)
                {
                    return;
                }

                _selectedFixedIndices.Add(index);
                SetIconSelected(icon, true);
            }

            UpdateConfirmButton();
        }

        private void SetIconSelected(UIItemIcon icon, bool selected)
        {
            Image rarityBorder = icon.GetComponentInChildren<Image>();

            if (rarityBorder != null && icon.transform.childCount > 0)
            {
                Transform highlight = icon.transform.Find("SelectionHighlight");

                if (highlight != null)
                {
                    Image highlightImage = highlight.GetComponent<Image>();

                    if (highlightImage != null)
                    {
                        highlightImage.color = selected ? (Color)COLOR_SELECTED : (Color)COLOR_UNSELECTED;
                    }
                }
            }

            icon.transform.localScale = selected ? Vector3.one * 1.1f : Vector3.one;
        }

        private void UpdateConfirmButton()
        {
            bool canConfirm;

            if (_maxSelectCount > 0 && _fixedCandidates.Count > 0)
            {
                canConfirm = _selectedFixedIndices.Count >= _maxSelectCount;
            }
            else
            {
                canConfirm = true;
            }

            if (_confirmButton != null)
            {
                _confirmButton.interactable = canConfirm;
            }

            if (_confirmButtonText != null)
            {
                if (_maxSelectCount > 0 && _fixedCandidates.Count > 0)
                {
                    _confirmButtonText.text = $"확인 ({_selectedFixedIndices.Count}/{_maxSelectCount})";
                }
                else
                {
                    _confirmButtonText.text = "확인";
                }
            }
        }

        private void OnConfirmClicked()
        {
            if (!_isShowing)
            {
                return;
            }

            List<InGameBagItemData> selectedRewards = new List<InGameBagItemData>(_selectedFixedIndices.Count);

            for (int i = 0; i < _selectedFixedIndices.Count; i++)
            {
                int idx = _selectedFixedIndices[i];

                if (idx >= 0 && idx < _fixedCandidates.Count)
                {
                    selectedRewards.Add(_fixedCandidates[idx]);
                }
            }

            Action<List<InGameBagItemData>> callback = _onConfirm;

            PlayCloseAnimation();

            callback?.Invoke(selectedRewards);
            OnRewardSelectionConfirmed?.Invoke(selectedRewards);
        }

        #endregion

        #region Private Methods — Text Helpers

        private static string GetResultTitle(StageResult result)
        {
            switch (result)
            {
                case StageResult.Victory: return "탐험 완료";
                case StageResult.Failure: return "탐험 실패";
                case StageResult.Retreat: return "긴급 귀환";
                default: return "탐험 종료";
            }
        }

        private static string GetReasonText(StageEndReason reason)
        {
            switch (reason)
            {
                case StageEndReason.BossCleared:   return "보스 격파";
                case StageEndReason.CampaignGoal:  return "캠페인 목표 달성";
                case StageEndReason.APDepleted:    return "행동력 소진";
                case StageEndReason.EventEscape:   return "긴급 귀환";
                case StageEndReason.PartyWipe:     return "파티 전멸";
                case StageEndReason.EventFailed:   return "이벤트 실패";
                default: return string.Empty;
            }
        }

        #endregion

        #region Private Methods — Pool

        private void InitializePoolIfNeeded()
        {
            if (_iconPool.Count > 0 || _rewardIconPrefab == null)
            {
                return;
            }

            for (int i = 0; i < _iconPoolSize; i++)
            {
                UIItemIcon icon = Instantiate(_rewardIconPrefab, transform);
                icon.gameObject.SetActive(false);
                _iconPool.Add(icon);
            }
        }

        private UIItemIcon GetOrCreateIcon(RectTransform parent)
        {
            UIItemIcon icon = null;

            for (int i = 0; i < _iconPool.Count; i++)
            {
                if (!_iconPool[i].gameObject.activeSelf)
                {
                    icon = _iconPool[i];
                    break;
                }
            }

            if (icon == null)
            {
                if (_rewardIconPrefab == null)
                {
                    return null;
                }

                icon = Instantiate(_rewardIconPrefab, parent);
                _iconPool.Add(icon);
            }

            icon.transform.SetParent(parent, false);
            icon.gameObject.SetActive(true);
            _activeIcons.Add(icon);

            return icon;
        }

        private void ClearActiveIcons()
        {
            for (int i = 0; i < _activeIcons.Count; i++)
            {
                _activeIcons[i].Clear();
                _activeIcons[i].gameObject.SetActive(false);
                _activeIcons[i].transform.localScale = Vector3.one;
            }

            _activeIcons.Clear();
        }

        private void BindBagItemToIcon(UIItemIcon icon, InGameBagItemData bagItem)
        {
            if (icon == null || bagItem == null)
            {
                return;
            }

            var inventoryItem = new InventoryItemData
            {
                productId = bagItem.productId,
                productName = bagItem.productName,
                rarity = bagItem.rarity,
                category = bagItem.category,
                ownStack = 1
            };

            icon.SetData(inventoryItem);
        }

        #endregion

        #region Private Methods — Animation

        private void PlayOpenAnimation()
        {
            KillSequence();

            _isShowing = true;

            if (_resultRoot != null)
            {
                _resultRoot.SetActive(true);
            }

            _currentSequence = DOTween.Sequence();

            if (_dimBackground != null)
            {
                _dimBackground.color = new Color(0f, 0f, 0f, 0f);
                _currentSequence.Join(
                    DOTween.To(
                        () => _dimBackground.color.a,
                        x => { Color c = _dimBackground.color; c.a = x; _dimBackground.color = c; },
                        0.5f, _animDuration
                    )
                );
            }

            if (_panelTransform != null)
            {
                _panelTransform.localScale = Vector3.zero;
                _currentSequence.Join(
                    _panelTransform.DOScale(Vector3.one, _animDuration)
                        .SetEase(Ease.OutBack)
                );
            }
        }

        private void PlayCloseAnimation()
        {
            KillSequence();

            _currentSequence = DOTween.Sequence();

            if (_panelTransform != null)
            {
                _currentSequence.Join(
                    _panelTransform.DOScale(Vector3.zero, _animDuration)
                        .SetEase(Ease.InBack)
                );
            }

            if (_dimBackground != null)
            {
                _currentSequence.Join(
                    DOTween.To(
                        () => _dimBackground.color.a,
                        x => { Color c = _dimBackground.color; c.a = x; _dimBackground.color = c; },
                        0f, _animDuration
                    )
                );
            }

            _currentSequence.OnComplete(() =>
            {
                _isShowing = false;
                _onConfirm = null;
                _settlementData = null;

                if (_resultRoot != null)
                {
                    _resultRoot.SetActive(false);
                }
            });
        }

        private void KillSequence()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
                _currentSequence = null;
            }
        }

        #endregion
    }
}
