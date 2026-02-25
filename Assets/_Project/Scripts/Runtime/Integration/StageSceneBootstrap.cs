using System.Collections.Generic;
using UnityEngine;
using ProjectStS.Core;
using ProjectStS.Data;
using ProjectStS.Meta;
using ProjectStS.Stage;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 스테이지 씬의 부트스트랩.
    /// 씬 로드 시 StageManager를 초기화하고 GameFlowController에 연결한다.
    /// 스테이지 씬 루트 오브젝트에 배치한다.
    /// </summary>
    public class StageSceneBootstrap : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private StageManager _stageManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeStage();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// StageManager를 초기화하고 GameFlowController에 알린다.
        /// </summary>
        private void InitializeStage()
        {
            if (_stageManager == null)
            {
                Debug.LogError("[StageSceneBootstrap] StageManager 참조가 없습니다.");
                return;
            }

            List<OwnedUnitData> party = null;
            ExplorationRecordData record = null;

            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData))
            {
                party = playerData.GetPartyMembers();
                record = playerData.GetExplorationRecord();
                playerData.ResetCurrentExplorationCounters();
            }

            if (party == null || party.Count == 0)
            {
                Debug.LogError("[StageSceneBootstrap] 파티 데이터가 없습니다.");
                return;
            }

            _stageManager.InitializeStage(party, record);

            if (ServiceLocator.TryGet<GameFlowController>(out var flowController))
            {
                flowController.OnStageReady(_stageManager);
            }
            else
            {
                Debug.LogWarning("[StageSceneBootstrap] GameFlowController를 찾을 수 없습니다.");
            }

            Debug.Log("[StageSceneBootstrap] 스테이지 씬 부트스트랩 완료.");
        }

        #endregion
    }
}
