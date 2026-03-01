using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ProjectStS.Core;
using ProjectStS.Data;
using ProjectStS.Meta;
using ProjectStS.Battle;
using ProjectStS.Stage;
using EventType = ProjectStS.Data.EventType;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 게임 전체 루프를 오케스트레이션하는 최상위 컨트롤러.
    /// Boot → Lobby → Stage → Battle/VN → Settlement → Lobby 흐름을 관리한다.
    /// DontDestroyOnLoad 오브젝트에 배치한다.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        #region Private Fields

        private GameFlowPhase _currentPhase = GameFlowPhase.None;
        private SaveLoadSystem _saveLoadSystem;

        private string _pendingBattleEventId;
        private EventData _pendingBattleEventData;
        private List<EnemyCombinationData> _pendingBattleWaves;

        private StageManager _currentStageManager;

        #endregion

        #region Public Properties

        /// <summary>
        /// 현재 게임 흐름 페이즈.
        /// </summary>
        public GameFlowPhase CurrentPhase => _currentPhase;

        #endregion

        #region Events

        /// <summary>
        /// 페이즈 변경 시 발행.
        /// </summary>
        public event Action<GameFlowPhase> OnPhaseChanged;

        /// <summary>
        /// 정산 준비 완료 시 발행. UI가 구독하여 정산 화면을 표시한다.
        /// </summary>
        public event Action<StageSettlementData> OnSettlementReady;

        #endregion

        #region Public Methods — 초기화

        /// <summary>
        /// GameFlowController를 초기화한다. GameBootstrapper에서 호출.
        /// </summary>
        /// <param name="saveSystem">세이브/로드 시스템</param>
        public void Initialize(SaveLoadSystem saveSystem)
        {
            _saveLoadSystem = saveSystem;

            ServiceLocator.Register(this);

            SetPhase(GameFlowPhase.Lobby);

            Debug.Log("[GameFlowController] 초기화 완료.");
        }

        #endregion

        #region Public Methods — 탐험 개시

        /// <summary>
        /// 탐험을 개시한다. 로비에서 호출.
        /// 파티를 조회하고 스테이지 씬을 로드한다.
        /// </summary>
        public void StartExpedition()
        {
            if (_currentPhase != GameFlowPhase.Lobby)
            {
                Debug.LogWarning($"[GameFlowController] 탐험 개시 불가. 현재 페이즈: {_currentPhase}");
                return;
            }

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager) ||
                !ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                Debug.LogError("[GameFlowController] 필수 서비스를 찾을 수 없습니다.");
                return;
            }

            string stageSceneName = dataManager.Settings.StageSceneName;

            sceneManager.LoadScene(stageSceneName);

            SetPhase(GameFlowPhase.Stage);

            Debug.Log("[GameFlowController] 탐험 개시. 스테이지 씬 로드 중.");
        }

        #endregion

        #region Public Methods — 씬 부트스트랩 콜백

        /// <summary>
        /// 스테이지 씬이 준비되었을 때 StageSceneBootstrap에서 호출.
        /// StageManager 이벤트를 구독한다.
        /// </summary>
        /// <param name="stageManager">초기화된 StageManager</param>
        public void OnStageReady(StageManager stageManager)
        {
            _currentStageManager = stageManager;

            stageManager.UIBridge.OnEventTriggered += HandleEventTriggered;
            stageManager.OnStageEnded += HandleStageEnded;

            Debug.Log("[GameFlowController] 스테이지 준비 완료. 이벤트 구독.");
        }

        /// <summary>
        /// 전투 씬이 준비되었을 때 BattleSceneBootstrap에서 호출.
        /// BattleManager를 초기화하고 이벤트를 구독한다.
        /// </summary>
        /// <param name="battleManager">전투 씬의 BattleManager</param>
        public void OnBattleReady(BattleManager battleManager)
        {
            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData))
            {
                List<OwnedUnitData> party = playerData.GetPartyMembers();

                battleManager.InitializeBattle(party, _pendingBattleWaves, _pendingBattleEventId);
                battleManager.OnBattleEnded += HandleBattleEnded;

                Debug.Log("[GameFlowController] 전투 초기화 완료.");
            }
            else
            {
                Debug.LogError("[GameFlowController] PlayerDataManager를 찾을 수 없습니다.");
            }
        }

        #endregion

        #region Public Methods — 정산

        /// <summary>
        /// 정산을 완료하고 보상을 적용한 뒤 로비로 복귀한다.
        /// RewardSettlementProcessor 또는 UI에서 호출.
        /// </summary>
        /// <param name="selectedRewards">선택된 보상 목록</param>
        public void CompleteSettlement(List<InventoryItemData> selectedRewards)
        {
            if (_currentPhase != GameFlowPhase.Settlement)
            {
                Debug.LogWarning($"[GameFlowController] 정산 완료 불가. 현재 페이즈: {_currentPhase}");
                return;
            }

            if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData))
            {
                if (selectedRewards != null)
                {
                    for (int i = 0; i < selectedRewards.Count; i++)
                    {
                        playerData.AddInventoryItem(selectedRewards[i]);
                    }
                }

                ExplorationRecordData record = playerData.GetExplorationRecord();
                record.countComplete++;
                playerData.UpdateExplorationRecord(record);
            }

            if (_saveLoadSystem != null && ServiceLocator.TryGet<PlayerDataManager>(out var playerDataForSave))
            {
                CampaignManager campaignManager = ServiceLocator.TryGet<CampaignManager>(out var cm) ? cm : null;

                if (campaignManager != null)
                {
                    campaignManager.EvaluateGoalProgress();
                    campaignManager.CheckAndCompleteCampaigns();

                    _saveLoadSystem.Save(playerDataForSave, campaignManager);
                }
            }

            CleanupStage();
            ReturnToLobbyInternal();

            Debug.Log("[GameFlowController] 정산 완료. 로비로 복귀.");
        }

        /// <summary>
        /// 정산 없이 로비로 직접 복귀한다 (전투 패배 등).
        /// </summary>
        public void ReturnToLobby()
        {
            CleanupStage();
            ReturnToLobbyInternal();

            Debug.Log("[GameFlowController] 로비로 직접 복귀.");
        }

        #endregion

        #region Private Methods — 이벤트 핸들러

        /// <summary>
        /// 스테이지에서 이벤트가 트리거되었을 때 처리한다.
        /// eventType에 따라 전투 또는 VN을 시작한다.
        /// </summary>
        private void HandleEventTriggered(HexNode node, EventData eventData)
        {
            switch (eventData.eventType)
            {
                case EventType.BattleNormal:
                case EventType.BattleElite:
                case EventType.BattleBoss:
                case EventType.BattleEvent:
                    StartBattle(eventData);
                    break;

                case EventType.VisualNovel:
                case EventType.Encounter:
                    StartVisualNovel(eventData);
                    break;
            }
        }

        /// <summary>
        /// 전투를 시작한다.
        /// </summary>
        private void StartBattle(EventData eventData)
        {
            _pendingBattleEventId = eventData.id;
            _pendingBattleEventData = eventData;
            _pendingBattleWaves = BuildWaveList(eventData.eventValue);

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager) ||
                !ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                Debug.LogError("[GameFlowController] 필수 서비스를 찾을 수 없습니다.");
                return;
            }

            string battleSceneName = dataManager.Settings.BattleSceneName;

            sceneManager.LoadScene(battleSceneName, LoadSceneMode.Additive);

            SetPhase(GameFlowPhase.Battle);

            Debug.Log($"[GameFlowController] 전투 시작. 이벤트: {eventData.id}, 웨이브 수: {_pendingBattleWaves.Count}");
        }

        /// <summary>
        /// 전투 종료를 처리한다.
        /// </summary>
        private void HandleBattleEnded(BattleResult result)
        {
            if (ServiceLocator.TryGet<BattleManager>(out var battleManager))
            {
                battleManager.OnBattleEnded -= HandleBattleEnded;
                battleManager.CleanupBattle();
            }

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager) ||
                !ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                Debug.LogError("[GameFlowController] 필수 서비스를 찾을 수 없습니다.");
                return;
            }

            string battleSceneName = dataManager.Settings.BattleSceneName;

            sceneManager.UnloadScene(battleSceneName);

            bool success = result == BattleResult.Victory;

            if (_currentStageManager != null && _currentStageManager.IsInitialized)
            {
                _currentStageManager.OnEventCompleted(_pendingBattleEventId, success);
            }

            _pendingBattleEventId = null;
            _pendingBattleEventData = null;
            _pendingBattleWaves = null;

            if (success)
            {
                SetPhase(GameFlowPhase.Stage);
                Debug.Log("[GameFlowController] 전투 승리. 스테이지로 복귀.");
            }
            else
            {
                HandleStageEnded(StageResult.Failure, StageEndReason.PartyWipe);
            }
        }

        /// <summary>
        /// VN 재생을 시작한다.
        /// </summary>
        private void StartVisualNovel(EventData eventData)
        {
            if (ServiceLocator.TryGet<IVisualNovelBridge>(out var vnBridge))
            {
                SetPhase(GameFlowPhase.VisualNovel);

                vnBridge.PlayEpisode(eventData.eventValue, (VNResult result) => HandleVNCompleted(eventData, result));

                Debug.Log($"[GameFlowController] VN 재생 시작. 이벤트: {eventData.id}");
            }
            else
            {
                Debug.LogWarning("[GameFlowController] IVisualNovelBridge를 찾을 수 없습니다. 이벤트를 완료 처리합니다.");

                if (_currentStageManager != null && _currentStageManager.IsInitialized)
                {
                    _currentStageManager.OnEventCompleted(eventData.id, true);
                }
            }
        }

        /// <summary>
        /// VN 재생 완료를 처리한다.
        /// VNResult에 포함된 Command를 순차적으로 처리한 뒤 스테이지로 복귀한다.
        /// </summary>
        private void HandleVNCompleted(EventData eventData, VNResult result)
        {
            bool isCompleted = result != null && result.IsCompleted;

            if (_currentStageManager != null && _currentStageManager.IsInitialized)
            {
                _currentStageManager.OnEventCompleted(eventData.id, isCompleted);
            }

            if (result != null && result.Commands != null && result.Commands.Count > 0)
            {
                ProcessVNCommands(result.Commands);
            }

            SetPhase(GameFlowPhase.Stage);

            Debug.Log($"[GameFlowController] VN 재생 완료. 이벤트: {eventData.id}, 완료: {isCompleted}, Command 수: {result?.Commands?.Count ?? 0}");
        }

        /// <summary>
        /// VN 에피소드에서 수집된 Command 목록을 순차 처리한다.
        /// 각 Command의 구체적 구현은 해당 게임 시스템 완성 후 추가한다.
        /// </summary>
        private void ProcessVNCommands(List<CommandRecord> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                CommandRecord cmd = commands[i];

                switch (cmd.CommandKey)
                {
                    case "startBattle":
                        // TODO: EventData를 구성하여 StartBattle() 호출
                        Debug.Log($"[GameFlowController] VN Command — startBattle: {cmd.CommandValue}");
                        break;

                    case "nextEpisode":
                        // TODO: 후속 VN 에피소드 재생 예약
                        Debug.Log($"[GameFlowController] VN Command — nextEpisode: {cmd.CommandValue}");
                        break;

                    case "giveItem":
                        // TODO: PlayerDataManager를 통해 아이템 지급
                        Debug.Log($"[GameFlowController] VN Command — giveItem: {cmd.CommandValue}");
                        break;

                    case "setFlag":
                        // TODO: 세이브 데이터에 플래그 설정
                        Debug.Log($"[GameFlowController] VN Command — setFlag: {cmd.CommandValue}");
                        break;

                    default:
                        Debug.LogWarning($"[GameFlowController] 알 수 없는 VN Command: {cmd.CommandKey} = {cmd.CommandValue}");
                        break;
                }
            }
        }

        /// <summary>
        /// 스테이지 종료를 처리한다.
        /// </summary>
        private void HandleStageEnded(StageResult result, StageEndReason reason)
        {
            if (result == StageResult.Failure)
            {
                ReturnToLobby();
                return;
            }

            if (_currentStageManager != null && _currentStageManager.IsInitialized)
            {
                StageSettlementData settlement = _currentStageManager.CalculateSettlement();

                SetPhase(GameFlowPhase.Settlement);

                OnSettlementReady?.Invoke(settlement);

                Debug.Log($"[GameFlowController] 정산 준비 완료. 결과: {result}, 사유: {reason}");
            }
            else
            {
                ReturnToLobby();
            }
        }

        #endregion

        #region Private Methods — 유틸

        /// <summary>
        /// 페이즈를 변경하고 이벤트를 발행한다.
        /// </summary>
        private void SetPhase(GameFlowPhase phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }

        /// <summary>
        /// eventValue에서 적 웨이브 목록을 구성한다.
        /// 세미콜론으로 구분된 복수 ID를 지원한다.
        /// </summary>
        private List<EnemyCombinationData> BuildWaveList(string eventValue)
        {
            var waves = new List<EnemyCombinationData>(4);

            if (!ServiceLocator.TryGet<DataManager>(out var dataManager))
            {
                Debug.LogError("[GameFlowController] DataManager를 찾을 수 없습니다.");
                return waves;
            }

            if (string.IsNullOrEmpty(eventValue))
            {
                return waves;
            }

            string[] ids = eventValue.Split(';');

            for (int i = 0; i < ids.Length; i++)
            {
                string trimmedId = ids[i].Trim();

                if (string.IsNullOrEmpty(trimmedId))
                {
                    continue;
                }

                EnemyCombinationData combination = dataManager.GetEnemyCombination(trimmedId);

                if (combination != null)
                {
                    waves.Add(combination);
                }
                else
                {
                    Debug.LogWarning($"[GameFlowController] 적 조합을 찾을 수 없습니다: {trimmedId}");
                }
            }

            waves.Sort((a, b) => a.waveCount.CompareTo(b.waveCount));

            return waves;
        }

        /// <summary>
        /// 스테이지 관련 이벤트 구독을 해제하고 정리한다.
        /// </summary>
        private void CleanupStage()
        {
            if (_currentStageManager != null)
            {
                _currentStageManager.UIBridge.OnEventTriggered -= HandleEventTriggered;
                _currentStageManager.OnStageEnded -= HandleStageEnded;
                _currentStageManager.CleanupStage();
                _currentStageManager = null;
            }
        }

        /// <summary>
        /// 로비 씬으로 전환한다.
        /// </summary>
        private void ReturnToLobbyInternal()
        {
            if (!ServiceLocator.TryGet<DataManager>(out var dataManager) ||
                !ServiceLocator.TryGet<SceneTransitionManager>(out var sceneManager))
            {
                Debug.LogError("[GameFlowController] 필수 서비스를 찾을 수 없습니다.");
                return;
            }

            string lobbySceneName = dataManager.Settings.LobbySceneName;

            sceneManager.LoadScene(lobbySceneName);

            SetPhase(GameFlowPhase.Lobby);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GameFlowController>();
        }

        #endregion
    }
}
