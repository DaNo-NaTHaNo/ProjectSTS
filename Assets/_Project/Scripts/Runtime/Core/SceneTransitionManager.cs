using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectStS.Core
{
    /// <summary>
    /// 비동기 씬 전환을 관리한다.
    /// 씬 간 데이터 전달 및 로딩 상태 이벤트를 제공한다.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        #region Private Fields

        private bool _isLoading;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 씬 전환이 진행 중인지 여부.
        /// </summary>
        public bool IsLoading => _isLoading;

        #endregion

        #region Events

        /// <summary>
        /// 씬 전환 시작 시 발행. 대상 씬 이름을 전달한다.
        /// </summary>
        public event Action<string> OnSceneLoadStarted;

        /// <summary>
        /// 씬 전환 진행률 갱신 시 발행. 0~1 사이 값을 전달한다.
        /// </summary>
        public event Action<float> OnSceneLoadProgress;

        /// <summary>
        /// 씬 전환 완료 시 발행. 로드된 씬 이름을 전달한다.
        /// </summary>
        public event Action<string> OnSceneLoadCompleted;

        #endregion

        #region Public Methods

        /// <summary>
        /// 지정된 씬을 비동기로 로드한다.
        /// </summary>
        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneTransition] 이미 씬 전환이 진행 중입니다. '{sceneName}' 로드 요청을 무시합니다.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName, mode));
        }

        /// <summary>
        /// 지정된 씬을 비동기로 로드하고 완료 콜백을 실행한다.
        /// </summary>
        public void LoadScene(string sceneName, Action onComplete, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (_isLoading)
            {
                Debug.LogWarning($"[SceneTransition] 이미 씬 전환이 진행 중입니다. '{sceneName}' 로드 요청을 무시합니다.");
                return;
            }

            StartCoroutine(LoadSceneAsync(sceneName, mode, onComplete));
        }

        #endregion

        #region Private Methods

        private IEnumerator LoadSceneAsync(string sceneName, LoadSceneMode mode, Action onComplete = null)
        {
            _isLoading = true;
            OnSceneLoadStarted?.Invoke(sceneName);

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, mode);

            if (asyncOp == null)
            {
                Debug.LogError($"[SceneTransition] '{sceneName}' 씬을 로드할 수 없습니다.");
                _isLoading = false;
                yield break;
            }

            asyncOp.allowSceneActivation = false;

            while (asyncOp.progress < 0.9f)
            {
                OnSceneLoadProgress?.Invoke(asyncOp.progress);
                yield return null;
            }

            OnSceneLoadProgress?.Invoke(1f);
            asyncOp.allowSceneActivation = true;

            yield return asyncOp;

            _isLoading = false;
            OnSceneLoadCompleted?.Invoke(sceneName);
            onComplete?.Invoke();

            Debug.Log($"[SceneTransition] '{sceneName}' 씬 로드 완료.");
        }

        #endregion
    }
}
