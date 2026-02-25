using UnityEngine;
using ProjectStS.Core;
using ProjectStS.Battle;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 전투 씬의 부트스트랩.
    /// 씬 로드 시 BattleManager를 GameFlowController에 전달하여
    /// 전투 초기화를 시작한다.
    /// 전투 씬 루트 오브젝트에 배치한다.
    /// </summary>
    public class BattleSceneBootstrap : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private BattleManager _battleManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeBattle();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// BattleManager를 GameFlowController에 전달한다.
        /// 전투 초기화는 GameFlowController.OnBattleReady 내부에서 수행된다.
        /// </summary>
        private void InitializeBattle()
        {
            if (_battleManager == null)
            {
                Debug.LogError("[BattleSceneBootstrap] BattleManager 참조가 없습니다.");
                return;
            }

            if (ServiceLocator.TryGet<GameFlowController>(out var flowController))
            {
                flowController.OnBattleReady(_battleManager);
            }
            else
            {
                Debug.LogWarning("[BattleSceneBootstrap] GameFlowController를 찾을 수 없습니다.");
            }

            Debug.Log("[BattleSceneBootstrap] 전투 씬 부트스트랩 완료.");
        }

        #endregion
    }
}
