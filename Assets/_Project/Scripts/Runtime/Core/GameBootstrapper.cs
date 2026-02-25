using UnityEngine;
using UnityEngine.SceneManagement;

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
            LoadLobbyScene();
        }

        #endregion

        #region Private Methods

        private void InitializeServices()
        {
            ServiceLocator.Clear();

            // SceneTransitionManager 생성 및 등록
            var sceneTransitionManager = gameObject.AddComponent<SceneTransitionManager>();
            ServiceLocator.Register(sceneTransitionManager);

            Debug.Log("[GameBootstrapper] 서비스 초기화 완료.");
        }

        private void LoadLobbyScene()
        {
            if (ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                sceneManager.LoadScene(_lobbySceneName);
            }
            else
            {
                Debug.LogError("[GameBootstrapper] SceneTransitionManager를 찾을 수 없습니다. 직접 씬을 로드합니다.");
                SceneManager.LoadScene(_lobbySceneName);
            }
        }

        #endregion
    }
}
