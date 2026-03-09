using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ProjectStS.Data;
using ProjectStS.Meta;
using ProjectStS.Core;

namespace ProjectStS.UI
{
    /// <summary>
    /// 로비 씬 UI의 중앙 컨트롤러.
    /// 메인 화면(파티 미리보기, 캠페인 트래커, 네비게이션)과
    /// 하위 화면(파티편성, 인벤토리, 캠페인)의 전환을 관리한다.
    /// Meta 매니저(PlayerDataManager, PartyEditManager, InventoryManager,
    /// CampaignManager, ExpeditionLauncher)를 생성/연결하고 이벤트를 구독한다.
    /// </summary>
    public class LobbyUIController : MonoBehaviour
    {
        #region Inner Types

        /// <summary>
        /// 로비 화면 상태.
        /// </summary>
        private enum LobbyScreen
        {
            Main,
            PartyEdit,
            Inventory,
            Campaign
        }

        #endregion

        #region Serialized Fields

        [Header("Sub Controllers")]
        [SerializeField] private PartyEditUIController _partyEditController;
        [SerializeField] private InventoryUIController _inventoryController;
        [SerializeField] private CampaignUIController _campaignController;

        [Header("Main Screen")]
        [SerializeField] private GameObject _mainScreenRoot;

        [Header("Party Preview")]
        [SerializeField] private UIUnitPortrait[] _partyPreviewPortraits = new UIUnitPortrait[3];

        [Header("Campaign Tracker")]
        [SerializeField] private UICampaignTracker _campaignTracker;

        [Header("Navigation Buttons")]
        [SerializeField] private Button _partyEditButton;
        [SerializeField] private Button _inventoryButton;
        [SerializeField] private Button _campaignButton;

        [Header("Expedition")]
        [SerializeField] private Button _expeditionButton;
        [SerializeField] private TextMeshProUGUI _expeditionButtonText;

        [Header("Popup")]
        [SerializeField] private UIPopup _popup;

        #endregion

        #region Private Fields

        private PlayerDataManager _playerData;
        private DataManager _dataManager;
        private PartyEditManager _partyEdit;
        private InventoryManager _inventoryManager;
        private CampaignManager _campaignManager;
        private ExpeditionLauncher _expeditionLauncher;
        private LobbyScreen _currentScreen;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            BindManagers();
            BindNavigationButtons();
        }

        private void OnDisable()
        {
            UnbindManagers();
            UnbindNavigationButtons();
        }

        #endregion

        #region Private Methods — Manager Binding

        private void BindManagers()
        {
            // ServiceLocator에서 기본 매니저 획득
            ServiceLocator.TryGet<PlayerDataManager>(out _playerData);
            ServiceLocator.TryGet<DataManager>(out _dataManager);

            if (_playerData == null || _dataManager == null)
            {
                Debug.LogError("[LobbyUIController] PlayerDataManager 또는 DataManager를 찾을 수 없습니다. " +
                    "BootScene에서 게임을 시작했는지 확인하세요.");
                return;
            }

            if (_playerData.GetOwnedUnits().Count == 0)
            {
                Debug.LogWarning("[LobbyUIController] 보유 유닛이 없습니다. 첫 실행 초기화가 수행되지 않았을 수 있습니다.");
            }

            // 파생 매니저 생성
            _partyEdit = new PartyEditManager(_playerData, _dataManager);
            _inventoryManager = new InventoryManager(_playerData);
            _campaignManager = new CampaignManager(_playerData, _dataManager);
            _expeditionLauncher = new ExpeditionLauncher(_playerData, _partyEdit, _dataManager);

            // 캠페인 해금 평가 (메타 레이어 진입 시)
            _campaignManager.EvaluateUnlocks();
            _campaignManager.EvaluateGoalProgress();

            // PlayerDataManager 이벤트 구독
            _playerData.OnPartyChanged += HandlePartyChanged;
            _playerData.OnUnitAdded += HandleUnitAdded;
            _playerData.OnInventoryChanged += HandleInventoryChanged;

            // ExpeditionLauncher 이벤트 구독
            _expeditionLauncher.OnExpeditionLaunched += HandleExpeditionLaunched;

            // 하위 컨트롤러 매니저 이벤트 바인딩
            if (_partyEditController != null)
            {
                _partyEditController.BindManagerEvents(_partyEdit);
            }

            if (_campaignController != null)
            {
                _campaignController.BindManagerEvents(_campaignManager);
            }

            // 로비 초기화
            InitializeLobby();
        }

        private void UnbindManagers()
        {
            if (_playerData != null)
            {
                _playerData.OnPartyChanged -= HandlePartyChanged;
                _playerData.OnUnitAdded -= HandleUnitAdded;
                _playerData.OnInventoryChanged -= HandleInventoryChanged;
            }

            if (_expeditionLauncher != null)
            {
                _expeditionLauncher.OnExpeditionLaunched -= HandleExpeditionLaunched;
            }

            if (_partyEditController != null)
            {
                _partyEditController.UnbindManagerEvents(_partyEdit);
            }

            if (_campaignController != null)
            {
                _campaignController.UnbindManagerEvents(_campaignManager);
            }

            _partyEdit = null;
            _inventoryManager = null;
            _campaignManager = null;
            _expeditionLauncher = null;
        }

        #endregion

        #region Private Methods — Initialization

        private void InitializeLobby()
        {
            RefreshPartyPreview();

            // 하위 컨트롤러 초기화
            if (_partyEditController != null)
            {
                _partyEditController.Initialize(_playerData, _partyEdit, _dataManager);
                _partyEditController.Hide();
            }

            if (_inventoryController != null)
            {
                _inventoryController.Initialize(_inventoryManager, _dataManager);
                _inventoryController.Hide();
            }

            if (_campaignController != null)
            {
                _campaignController.Initialize(_campaignManager, _dataManager);
                _campaignController.Hide();
            }

            if (_campaignTracker != null)
            {
                _campaignTracker.Initialize(_campaignManager);
            }

            ShowScreen(LobbyScreen.Main);
        }

        #endregion

        #region Private Methods — Screen Navigation

        private void ShowScreen(LobbyScreen screen)
        {
            _currentScreen = screen;

            // 메인 화면
            if (_mainScreenRoot != null)
            {
                _mainScreenRoot.SetActive(screen == LobbyScreen.Main);
            }

            // 하위 화면 전환
            switch (screen)
            {
                case LobbyScreen.Main:
                    HideAllSubScreens();
                    RefreshPartyPreview();

                    if (_campaignTracker != null && _campaignManager != null)
                    {
                        _campaignTracker.Refresh(_campaignManager);
                    }
                    break;

                case LobbyScreen.PartyEdit:
                    HideAllSubScreens();

                    if (_partyEditController != null)
                    {
                        _partyEditController.Show();
                    }
                    break;

                case LobbyScreen.Inventory:
                    HideAllSubScreens();

                    if (_inventoryController != null)
                    {
                        _inventoryController.Show();
                    }
                    break;

                case LobbyScreen.Campaign:
                    HideAllSubScreens();

                    if (_campaignController != null)
                    {
                        _campaignController.Show();
                    }
                    break;
            }
        }

        private void HideAllSubScreens()
        {
            if (_partyEditController != null)
            {
                _partyEditController.Hide();
            }

            if (_inventoryController != null)
            {
                _inventoryController.Hide();
            }

            if (_campaignController != null)
            {
                _campaignController.Hide();
            }
        }

        #endregion

        #region Private Methods — Party Preview

        private void RefreshPartyPreview()
        {
            if (_playerData == null || _dataManager == null)
            {
                return;
            }

            List<OwnedUnitData> partyMembers = _playerData.GetPartyMembers();

            for (int i = 0; i < _partyPreviewPortraits.Length; i++)
            {
                if (_partyPreviewPortraits[i] == null)
                {
                    continue;
                }

                if (i < partyMembers.Count)
                {
                    UnitData unitData = _dataManager.GetUnit(partyMembers[i].unitId);
                    _partyPreviewPortraits[i].SetData(unitData, partyMembers[i]);
                }
                else
                {
                    _partyPreviewPortraits[i].Clear();
                }
            }
        }

        #endregion

        #region Private Methods — Expedition

        private void HandleLaunchExpedition()
        {
            if (_expeditionLauncher == null)
            {
                return;
            }

            ExpeditionValidationResult validation = _expeditionLauncher.ValidateExpedition();

            if (!validation.IsValid)
            {
                UIPopup.ShowInfo(validation.ErrorMessage);
                return;
            }

            UIPopup.ShowConfirm(
                "탐험을 개시하시겠습니까?",
                () => _expeditionLauncher.LaunchExpedition()
            );
        }

        private void HandleExpeditionLaunched()
        {
            Debug.Log("[LobbyUIController] 탐험 개시. 스테이지 씬으로 전환합니다.");
        }

        #endregion

        #region Private Methods — Navigation Buttons

        private void BindNavigationButtons()
        {
            if (_partyEditButton != null)
            {
                _partyEditButton.onClick.AddListener(() => ShowScreen(LobbyScreen.PartyEdit));
            }

            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.AddListener(() => ShowScreen(LobbyScreen.Inventory));
            }

            if (_campaignButton != null)
            {
                _campaignButton.onClick.AddListener(() => ShowScreen(LobbyScreen.Campaign));
            }

            if (_expeditionButton != null)
            {
                _expeditionButton.onClick.AddListener(HandleLaunchExpedition);
            }

            // 하위 컨트롤러 뒤로가기 이벤트
            if (_partyEditController != null)
            {
                _partyEditController.OnBackRequested += () => ShowScreen(LobbyScreen.Main);
            }

            if (_inventoryController != null)
            {
                _inventoryController.OnBackRequested += () => ShowScreen(LobbyScreen.Main);
            }

            if (_campaignController != null)
            {
                _campaignController.OnBackRequested += () => ShowScreen(LobbyScreen.Main);
            }
        }

        private void UnbindNavigationButtons()
        {
            if (_partyEditButton != null)
            {
                _partyEditButton.onClick.RemoveAllListeners();
            }

            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.RemoveAllListeners();
            }

            if (_campaignButton != null)
            {
                _campaignButton.onClick.RemoveAllListeners();
            }

            if (_expeditionButton != null)
            {
                _expeditionButton.onClick.RemoveAllListeners();
            }
        }

        #endregion

        #region Private Methods — PlayerDataManager Event Handlers

        private void HandlePartyChanged()
        {
            RefreshPartyPreview();

            if (_partyEditController != null && _currentScreen == LobbyScreen.PartyEdit)
            {
                _partyEditController.RefreshPartySlots();
                _partyEditController.RefreshUnitList();
            }
        }

        private void HandleUnitAdded(OwnedUnitData unit)
        {
            if (_partyEditController != null && _currentScreen == LobbyScreen.PartyEdit)
            {
                _partyEditController.RefreshUnitList();
            }
        }

        private void HandleInventoryChanged(InventoryItemData item)
        {
            if (_inventoryController != null && _currentScreen == LobbyScreen.Inventory)
            {
                _inventoryController.RefreshGrid();
            }
        }

        #endregion
    }
}
