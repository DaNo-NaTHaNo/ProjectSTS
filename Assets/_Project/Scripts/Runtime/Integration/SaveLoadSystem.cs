using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using ProjectStS.Data;
using ProjectStS.Meta;

namespace ProjectStS.Integration
{
    /// <summary>
    /// 플레이어 데이터를 JSON 파일로 저장/불러오기하는 시스템.
    /// Newtonsoft.Json을 사용하여 SaveData를 직렬화/역직렬화한다.
    /// </summary>
    public class SaveLoadSystem
    {
        #region Constants

        private const int SAVE_DATA_VERSION = 1;
        private const string SAVE_FILE_NAME = "save_data.json";

        #endregion

        #region Private Fields

        private readonly string _savePath;

        #endregion

        #region Constructor

        /// <summary>
        /// SaveLoadSystem을 생성한다.
        /// </summary>
        public SaveLoadSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 현재 플레이어 데이터와 캠페인 상태를 JSON 파일로 저장한다.
        /// </summary>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="campaignManager">캠페인 매니저</param>
        public void Save(PlayerDataManager playerData, CampaignManager campaignManager)
        {
            try
            {
                var saveData = new SaveData
                {
                    version = SAVE_DATA_VERSION,
                    ownedUnits = new List<OwnedUnitData>(playerData.GetOwnedUnits()),
                    inventory = new List<InventoryItemData>(playerData.GetInventory()),
                    explorationRecord = playerData.GetExplorationRecord(),
                    campaignUnlockedState = campaignManager.GetCampaignUnlockedStates(),
                    campaignCompletionState = campaignManager.GetCampaignCompletionStates(),
                    goalCompletionState = campaignManager.GetGoalCompletionStates(),
                    trackedCampaignId = campaignManager.GetTrackedCampaignId()
                };

                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                File.WriteAllText(_savePath, json);

                Debug.Log($"[SaveLoadSystem] 세이브 완료: {_savePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] 세이브 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 파일에서 세이브 데이터를 불러온다.
        /// </summary>
        /// <returns>역직렬화된 SaveData. 실패 시 null.</returns>
        public SaveData Load()
        {
            try
            {
                if (!File.Exists(_savePath))
                {
                    Debug.Log("[SaveLoadSystem] 세이브 파일이 없습니다.");
                    return null;
                }

                string json = File.ReadAllText(_savePath);
                var saveData = JsonConvert.DeserializeObject<SaveData>(json);

                Debug.Log($"[SaveLoadSystem] 로드 완료 (version: {saveData.version})");
                return saveData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] 로드 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 불러온 세이브 데이터를 PlayerDataManager와 CampaignManager에 적용한다.
        /// </summary>
        /// <param name="saveData">적용할 세이브 데이터</param>
        /// <param name="playerData">플레이어 데이터 매니저</param>
        /// <param name="campaignManager">캠페인 매니저</param>
        public void ApplyLoadedData(SaveData saveData, PlayerDataManager playerData, CampaignManager campaignManager)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[SaveLoadSystem] 적용할 세이브 데이터가 null입니다.");
                return;
            }

            playerData.LoadData(saveData.ownedUnits, saveData.inventory, saveData.explorationRecord);

            if (saveData.campaignUnlockedState != null)
            {
                foreach (var pair in saveData.campaignUnlockedState)
                {
                    campaignManager.SetCampaignUnlockedState(pair.Key, pair.Value);
                }
            }

            if (saveData.campaignCompletionState != null)
            {
                foreach (var pair in saveData.campaignCompletionState)
                {
                    campaignManager.SetCampaignCompletionState(pair.Key, pair.Value);
                }
            }

            if (saveData.goalCompletionState != null)
            {
                foreach (var pair in saveData.goalCompletionState)
                {
                    campaignManager.SetGoalCompletionState(pair.Key, pair.Value);
                }
            }

            if (!string.IsNullOrEmpty(saveData.trackedCampaignId))
            {
                campaignManager.SetTrackedCampaign(saveData.trackedCampaignId);
            }

            Debug.Log("[SaveLoadSystem] 세이브 데이터 적용 완료.");
        }

        /// <summary>
        /// 세이브 파일이 존재하는지 확인한다.
        /// </summary>
        public bool HasSaveData()
        {
            return File.Exists(_savePath);
        }

        /// <summary>
        /// 세이브 파일을 삭제한다.
        /// </summary>
        public void DeleteSaveData()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                    Debug.Log("[SaveLoadSystem] 세이브 파일 삭제 완료.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveLoadSystem] 세이브 파일 삭제 실패: {ex.Message}");
            }
        }

        #endregion
    }
}
