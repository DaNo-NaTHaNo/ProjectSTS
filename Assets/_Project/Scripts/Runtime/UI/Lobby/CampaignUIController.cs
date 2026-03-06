using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.UI
{
    /// <summary>
    /// 캠페인 화면 전체 컨트롤러.
    /// UICampaignList, UICampaignDetail, UICampaignTracker를 관리하며,
    /// CampaignManager의 이벤트를 구독하여 캠페인 해금/완료/목표 진행을 반영한다.
    /// </summary>
    public class CampaignUIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sub Components")]
        [SerializeField] private UICampaignList _campaignList;
        [SerializeField] private UICampaignDetail _campaignDetail;
        [SerializeField] private UICampaignTracker _campaignTracker;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        [Header("Panel")]
        [SerializeField] private GameObject _screenRoot;

        #endregion

        #region Private Fields

        private CampaignManager _campaignManager;
        private DataManager _dataManager;

        #endregion

        #region Events

        /// <summary>
        /// 뒤로가기 버튼이 눌렸을 때 발행한다.
        /// </summary>
        public event Action OnBackRequested;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_campaignList != null)
            {
                _campaignList.OnCampaignSelected += HandleCampaignSelected;
            }

            if (_campaignDetail != null)
            {
                _campaignDetail.OnDetailClosed += HandleDetailClosed;
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void OnDisable()
        {
            if (_campaignList != null)
            {
                _campaignList.OnCampaignSelected -= HandleCampaignSelected;
            }

            if (_campaignDetail != null)
            {
                _campaignDetail.OnDetailClosed -= HandleDetailClosed;
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 캠페인 화면을 초기화한다.
        /// </summary>
        /// <param name="campaignManager">캠페인 매니저</param>
        /// <param name="dataManager">마스터 데이터 매니저</param>
        public void Initialize(CampaignManager campaignManager, DataManager dataManager)
        {
            _campaignManager = campaignManager;
            _dataManager = dataManager;

            if (_campaignList != null)
            {
                List<CampaignData> active = campaignManager.GetActiveCampaigns();
                List<CampaignData> completed = campaignManager.GetCompletedCampaigns();
                _campaignList.Initialize(active, completed);
            }

            if (_campaignDetail != null)
            {
                _campaignDetail.Hide();
            }

            if (_campaignTracker != null)
            {
                _campaignTracker.Initialize(campaignManager);
            }
        }

        /// <summary>
        /// 화면을 표시한다.
        /// </summary>
        public void Show()
        {
            if (_screenRoot != null)
            {
                _screenRoot.SetActive(true);
            }

            // 리스트 갱신
            if (_campaignManager != null && _campaignList != null)
            {
                List<CampaignData> active = _campaignManager.GetActiveCampaigns();
                List<CampaignData> completed = _campaignManager.GetCompletedCampaigns();
                _campaignList.RefreshList(active, completed);
            }
        }

        /// <summary>
        /// 화면을 숨긴다.
        /// </summary>
        public void Hide()
        {
            if (_campaignDetail != null)
            {
                _campaignDetail.Hide();
            }

            if (_screenRoot != null)
            {
                _screenRoot.SetActive(false);
            }
        }

        /// <summary>
        /// CampaignManager 이벤트를 구독한다. LobbyUIController에서 호출.
        /// </summary>
        public void BindManagerEvents(CampaignManager campaignManager)
        {
            if (campaignManager == null)
            {
                return;
            }

            campaignManager.OnCampaignUnlocked += HandleCampaignUnlocked;
            campaignManager.OnCampaignCompleted += HandleCampaignCompleted;
            campaignManager.OnGoalCompleted += HandleGoalCompleted;
        }

        /// <summary>
        /// CampaignManager 이벤트 구독을 해제한다. LobbyUIController에서 호출.
        /// </summary>
        public void UnbindManagerEvents(CampaignManager campaignManager)
        {
            if (campaignManager == null)
            {
                return;
            }

            campaignManager.OnCampaignUnlocked -= HandleCampaignUnlocked;
            campaignManager.OnCampaignCompleted -= HandleCampaignCompleted;
            campaignManager.OnGoalCompleted -= HandleGoalCompleted;
        }

        /// <summary>
        /// 캠페인 트래커를 갱신한다.
        /// </summary>
        public void RefreshTracker()
        {
            if (_campaignTracker != null && _campaignManager != null)
            {
                _campaignTracker.Refresh(_campaignManager);
            }
        }

        #endregion

        #region Private Methods — Handlers

        private void HandleCampaignSelected(CampaignData campaign)
        {
            if (_campaignDetail == null || _campaignManager == null)
            {
                return;
            }

            List<CampaignGoalGroupData> goals = null;

            if (!string.IsNullOrEmpty(campaign.groupId))
            {
                goals = _campaignManager.GetGoalsByGroup(campaign.groupId);
            }

            _campaignDetail.Show(campaign, goals, _campaignManager);
        }

        private void HandleCampaignUnlocked(CampaignData campaign)
        {
            if (_campaignList != null)
            {
                _campaignList.AddCampaign(campaign);
            }
        }

        private void HandleCampaignCompleted(CampaignData campaign)
        {
            if (_campaignList != null)
            {
                _campaignList.MoveToCompleted(campaign);
            }

            RefreshTracker();
        }

        private void HandleGoalCompleted(CampaignGoalGroupData goal)
        {
            // 상세 패널이 열려 있으면 목표 갱신
            RefreshTracker();
        }

        private void HandleDetailClosed()
        {
            // 상세 패널 닫힘 처리 (추적 상태가 변경되었을 수 있으므로 트래커 갱신)
            RefreshTracker();
        }

        private void HandleBackClicked()
        {
            OnBackRequested?.Invoke();
        }

        #endregion
    }
}
