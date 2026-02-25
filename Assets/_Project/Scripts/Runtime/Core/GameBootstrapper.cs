using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectStS.Data;

namespace ProjectStS.Core
{
    /// <summary>
    /// 게임의 단일 진입점. BootScene에서 1회 실행되며,
    /// 서비스를 초기화하고 LobbyScene으로 전환한다.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private string _lobbySceneName = "LobbyScene";

        [Header("Data")]
        [SerializeField] private DataManager _dataManager;

        #endregion

        #region Private Fields

        private static bool _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_isInitialized)
            {
                Destroy(gameObject);
                return;
            }

            _isInitialized = true;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void Start()
        {
            // IntegrationBootstrap이 없는 경우 (독립 실행 등) 직접 로비 씬 로드
            // IntegrationBootstrap이 있는 경우 해당 컴포넌트가 씬 전환을 관장한다.
            if (GetComponent("IntegrationBootstrap") == null)
            {
                LoadLobbyScene();
            }
        }

        #endregion

        #region Private Methods

        private void InitializeServices()
        {
            ServiceLocator.Clear();

            // DataManager 등록
            if (_dataManager != null)
            {
                ServiceLocator.Register(_dataManager);
            }
            else
            {
                Debug.LogError("[GameBootstrapper] DataManager가 할당되지 않았습니다.");
            }

            // SceneTransitionManager 생성 및 등록
            var sceneTransitionManager = gameObject.AddComponent<SceneTransitionManager>();
            ServiceLocator.Register(sceneTransitionManager);

            Debug.Log("[GameBootstrapper] 서비스 초기화 완료.");
        }

        /// <summary>
        /// 로비 씬으로 전환한다. 외부에서도 호출 가능하다.
        /// </summary>
        public void LoadLobbyScene()
        {
            string sceneName = (_dataManager != null && _dataManager.Settings != null)
                ? _dataManager.Settings.LobbySceneName
                : _lobbySceneName;

            if (ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                sceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError("[GameBootstrapper] SceneTransitionManager를 찾을 수 없습니다. 직접 씬을 로드합니다.");
                SceneManager.LoadScene(sceneName);
            }
        }

        #endregion
    }
}
