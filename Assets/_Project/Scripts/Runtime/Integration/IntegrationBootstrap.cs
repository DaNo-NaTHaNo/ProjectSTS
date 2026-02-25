using UnityEngine;
using ProjectStS.Core;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 통합 레이어의 부트스트랩.
    /// GameBootstrapper와 동일한 DontDestroyOnLoad 오브젝트에 배치한다.
    /// SaveLoadSystem, GameFlowController를 생성/등록하고 세이브 데이터를 복원한다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class IntegrationBootstrap : MonoBehaviour
    {
        #region Unity Lifecycle

        private void Start()
        {
            InitializeIntegrationServices();
            LoadSaveData();
            LoadLobbyScene();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 통합 레이어 서비스를 초기화한다.
        /// </summary>
        private void InitializeIntegrationServices()
        {
            // SaveLoadSystem 생성 및 등록
            var saveLoadSystem = new SaveLoadSystem();
            ServiceLocator.Register(saveLoadSystem);

            // CampaignManager 생성 및 등록
            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData)
                && ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                var campaignManager = new CampaignManager(playerData, dataManager);
                ServiceLocator.Register(campaignManager);
            }
            else
            {
                Debug.LogWarning("[IntegrationBootstrap] PlayerDataManager 또는 DataManager를 찾을 수 없어 CampaignManager 생성을 건너뜁니다.");
            }

            // GameFlowController 생성 및 등록
            var gameFlowController = gameObject.AddComponent<GameFlowController>();
            gameFlowController.Initialize(saveLoadSystem);

            Debug.Log("[IntegrationBootstrap] 통합 서비스 초기화 완료.");
        }

        /// <summary>
        /// 세이브 데이터를 불러와 매니저에 적용한다.
        /// </summary>
        private void LoadSaveData()
        {
            if (!ServiceLocator.TryGet<SaveLoadSystem>(out var saveSystem))
            {
                return;
            }

            if (!saveSystem.HasSaveData())
            {
                return;
            }

            SaveData saveData = saveSystem.Load();

            if (saveData == null)
            {
                return;
            }

            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData)
                && ServiceLocator.TryGet<CampaignManager>(out var campaignManager))
            {
                saveSystem.ApplyLoadedData(saveData, playerData, campaignManager);
                Debug.Log("[IntegrationBootstrap] 세이브 데이터 복원 완료.");
            }
            else
            {
                Debug.LogWarning("[IntegrationBootstrap] 매니저를 찾을 수 없어 세이브 데이터 복원을 건너뜁니다.");
            }
        }

        /// <summary>
        /// 로비 씬을 로드한다.
        /// </summary>
        private void LoadLobbyScene()
        {
            if (ServiceLocator.TryGet<DataManager>(out var dataManager)
                && ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                string lobbySceneName = dataManager.Settings.LobbySceneName;
                sceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.LogError("[IntegrationBootstrap] 로비 씬 로드에 필요한 서비스를 찾을 수 없습니다.");
            }
        }

        #endregion
    }
}
