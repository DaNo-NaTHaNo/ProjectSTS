using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.UI
{
    /// <summary>
    /// 캠페인 상세 패널.
    /// 캠페인 이름, 설명, 목표 리스트(미완료 상단/완료 하단), 추적 버튼을 표시한다.
    /// </summary>
    public class UICampaignDetail : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Campaign Info")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Goals")]
        [SerializeField] private Transform _goalContainer;
        [SerializeField] private GameObject _goalEntryPrefab;

        [Header("Track Button")]
        [SerializeField] private Button _trackButton;
        [SerializeField] private TextMeshProUGUI _trackButtonText;

        [Header("Close Button")]
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private float _fadeDuration = 0.2f;

        [Header("Goal Colors")]
        [SerializeField] private Color _incompleteGoalColor = Color.white;
        [SerializeField] private Color _completedGoalColor = new Color32(0x9E, 0x9E, 0x9E, 0xFF);

        #endregion

        #region Private Fields

        private CampaignData _currentCampaign;
        private CampaignManager _campaignManager;
        private readonly List<GameObject> _goalInstances = new List<GameObject>(8);
        private bool _isVisible;

        #endregion

        #region Events

        /// <summary>
        /// 상세 패널이 닫힐 때 발행한다.
        /// </summary>
        public event Action OnDetailClosed;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_trackButton != null)
            {
                _trackButton.onClick.AddListener(HandleTrackClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(Hide);
            }
        }

        private void OnDisable()
        {
            if (_trackButton != null)
            {
                _trackButton.onClick.RemoveListener(HandleTrackClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Hide);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 캠페인 상세 정보를 표시한다.
        /// </summary>
        /// <param name="campaign">캠페인 데이터</param>
        /// <param name="goals">캠페인 목표 리스트</param>
        /// <param name="campaignManager">캠페인 매니저</param>
        public void Show(CampaignData campaign, List<CampaignGoalGroupData> goals, CampaignManager campaignManager)
        {
            _currentCampaign = campaign;
            _campaignManager = campaignManager;

            if (_nameText != null)
            {
                _nameText.text = campaign.name;
            }

            if (_descriptionText != null)
            {
                _descriptionText.text = campaign.description;
            }

            RefreshGoals(goals);
            UpdateTrackButton();
            PlayShowAnimation();
        }

        /// <summary>
        /// 목표 리스트를 갱신한다. 미완료 목표를 상단, 완료 목표를 하단에 배치한다.
        /// </summary>
        /// <param name="goals">갱신할 목표 리스트</param>
        public void RefreshGoals(List<CampaignGoalGroupData> goals)
        {
            ClearGoals();

            if (goals == null)
            {
                return;
            }

            // 미완료 목표 우선 배치
            for (int i = 0; i < goals.Count; i++)
            {
                if (!goals[i].isCompleted)
                {
                    CreateGoalEntry(goals[i], false);
                }
            }

            // 완료 목표 하단 배치
            for (int i = 0; i < goals.Count; i++)
            {
                if (goals[i].isCompleted)
                {
                    CreateGoalEntry(goals[i], true);
                }
            }
        }

        /// <summary>
        /// 상세 패널을 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible)
            {
                return;
            }

            PlayHideAnimation();
            OnDetailClosed?.Invoke();
        }

        #endregion

        #region Private Methods — Goal Display

        private void CreateGoalEntry(CampaignGoalGroupData goal, bool isCompleted)
        {
            GameObject go;

            if (_goalEntryPrefab != null && _goalContainer != null)
            {
                go = Instantiate(_goalEntryPrefab, _goalContainer);
            }
            else if (_goalContainer != null)
            {
                go = new GameObject(goal.name, typeof(RectTransform));
                go.transform.SetParent(_goalContainer, false);

                var textGo = new GameObject("GoalText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform, false);
            }
            else
            {
                return;
            }

            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                string prefix = isCompleted ? "✓ " : "○ ";
                label.text = $"{prefix}{goal.name}";
                label.color = isCompleted ? _completedGoalColor : _incompleteGoalColor;

                if (isCompleted)
                {
                    label.fontStyle = FontStyles.Strikethrough;
                }
            }

            _goalInstances.Add(go);
        }

        private void ClearGoals()
        {
            for (int i = 0; i < _goalInstances.Count; i++)
            {
                if (_goalInstances[i] != null)
                {
                    Destroy(_goalInstances[i]);
                }
            }

            _goalInstances.Clear();
        }

        #endregion

        #region Private Methods — Track

        private void HandleTrackClicked()
        {
            if (_campaignManager == null || _currentCampaign == null)
            {
                return;
            }

            string currentTracked = _campaignManager.GetTrackedCampaignId();

            if (currentTracked == _currentCampaign.id)
            {
                // 이미 추적 중이면 추적 해제
                _campaignManager.SetTrackedCampaign(null);
            }
            else
            {
                _campaignManager.SetTrackedCampaign(_currentCampaign.id);
            }

            UpdateTrackButton();
        }

        private void UpdateTrackButton()
        {
            if (_trackButtonText == null || _campaignManager == null || _currentCampaign == null)
            {
                return;
            }

            string currentTracked = _campaignManager.GetTrackedCampaignId();
            bool isTracking = currentTracked == _currentCampaign.id;
            _trackButtonText.text = isTracking ? "추적 해제" : "추적하기";
        }

        #endregion

        #region Private Methods — Animation

        private void PlayShowAnimation()
        {
            _isVisible = true;

            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, _fadeDuration);
            }
        }

        private void PlayHideAnimation()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.DOFade(0f, _fadeDuration)
                    .OnComplete(() =>
                    {
                        _isVisible = false;

                        if (_panelRoot != null)
                        {
                            _panelRoot.SetActive(false);
                        }
                    });
            }
            else
            {
                _isVisible = false;

                if (_panelRoot != null)
                {
                    _panelRoot.SetActive(false);
                }
            }
        }

        #endregion
    }
}
