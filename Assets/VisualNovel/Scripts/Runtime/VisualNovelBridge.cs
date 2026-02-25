using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using ProjectStS.Core;

/// <summary>
/// IVisualNovelBridge의 구현체.
/// VisualNovelPlayer를 래핑하여 외부 시스템(GameFlowController)이
/// 에피소드 ID만으로 VN 재생을 요청할 수 있게 한다.
/// VN 오버레이 루트를 활성화/비활성화하여 씬 전환 없이 VN을 재생한다.
/// </summary>
public class VisualNovelBridge : MonoBehaviour, IVisualNovelBridge
{
    #region Serialized Fields

    [Header("VN References")]
    [SerializeField] private VisualNovelPlayer _player;
    [SerializeField] private GameObject _vnRoot;

    [Header("Episode Registry")]
    [SerializeField] private List<EpisodeRegistryEntry> _episodeRegistry = new List<EpisodeRegistryEntry>();

    #endregion

    #region Private Fields

    private Dictionary<string, VisualNovelSO> _episodeLookup;
    private bool _isPlaying;

    #endregion

    #region Public Properties

    /// <summary>
    /// 현재 VN이 재생 중인지 여부.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ServiceLocator.Register<IVisualNovelBridge>(this);
        BuildEpisodeLookup();

        if (_vnRoot != null)
        {
            _vnRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IVisualNovelBridge>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 지정된 에피소드를 재생하고 완료 시 콜백을 호출한다.
    /// </summary>
    /// <param name="episodeId">재생할 에피소드 ID</param>
    /// <param name="onCompleted">재생 완료 시 호출될 콜백</param>
    public void PlayEpisode(string episodeId, Action onCompleted)
    {
        if (_isPlaying)
        {
            Debug.LogWarning("[VisualNovelBridge] 이미 VN이 재생 중입니다.");
            return;
        }

        if (_player == null)
        {
            Debug.LogError("[VisualNovelBridge] VisualNovelPlayer 참조가 없습니다.");
            onCompleted?.Invoke();
            return;
        }

        VisualNovelSO episodeSO = FindEpisodeSO(episodeId);

        if (episodeSO == null)
        {
            Debug.LogError($"[VisualNovelBridge] 에피소드를 찾을 수 없습니다: {episodeId}");
            onCompleted?.Invoke();
            return;
        }

        EpisodeData episodeData = ConvertSOToEpisodeData(episodeSO);

        if (episodeData == null)
        {
            Debug.LogError($"[VisualNovelBridge] 에피소드 데이터 변환 실패: {episodeId}");
            onCompleted?.Invoke();
            return;
        }

        _isPlaying = true;

        if (_vnRoot != null)
        {
            _vnRoot.SetActive(true);
        }

        _player.PlayEpisode(episodeData, () =>
        {
            _isPlaying = false;

            if (_vnRoot != null)
            {
                _vnRoot.SetActive(false);
            }

            onCompleted?.Invoke();
        });
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 에피소드 레지스트리로부터 Dictionary 룩업을 구축한다.
    /// </summary>
    private void BuildEpisodeLookup()
    {
        _episodeLookup = new Dictionary<string, VisualNovelSO>(_episodeRegistry.Count);

        for (int i = 0; i < _episodeRegistry.Count; i++)
        {
            EpisodeRegistryEntry entry = _episodeRegistry[i];

            if (string.IsNullOrEmpty(entry.episodeId) || entry.episodeSO == null)
            {
                continue;
            }

            if (!_episodeLookup.ContainsKey(entry.episodeId))
            {
                _episodeLookup.Add(entry.episodeId, entry.episodeSO);
            }
            else
            {
                Debug.LogWarning($"[VisualNovelBridge] 중복된 에피소드 ID: {entry.episodeId}");
            }
        }
    }

    /// <summary>
    /// 에피소드 ID로 VisualNovelSO를 조회한다.
    /// </summary>
    private VisualNovelSO FindEpisodeSO(string episodeId)
    {
        if (_episodeLookup != null && _episodeLookup.TryGetValue(episodeId, out VisualNovelSO so))
        {
            return so;
        }

        return null;
    }

    /// <summary>
    /// VisualNovelSO를 런타임 EpisodeData로 변환한다.
    /// 기존 VisualNovelPlayer.LoadEpisodeDataFromSO 패턴을 따른다.
    /// </summary>
    private EpisodeData ConvertSOToEpisodeData(VisualNovelSO so)
    {
        try
        {
            return new EpisodeData
            {
                episodeInfo = new EpisodeInfo { title = so.name },
                datasets = new Datasets
                {
                    dialogueLines = so.dialogueLines,
                    episodePortraits = so.episodePortraits,
                    locationPresets = so.locationPresets
                },
                nodeGraph = string.IsNullOrEmpty(so.nodeGraphJson)
                    ? new NodeGraph { nodes = new List<NodeData>(), connections = new List<ConnectionData>() }
                    : JsonConvert.DeserializeObject<NodeGraph>(so.nodeGraphJson)
            };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[VisualNovelBridge] SO→EpisodeData 변환 실패: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// 에피소드 ID와 VisualNovelSO를 매핑하는 레지스트리 항목.
    /// 인스펙터에서 설정한다.
    /// </summary>
    [System.Serializable]
    public class EpisodeRegistryEntry
    {
        /// <summary>이벤트 테이블의 eventValue에 대응하는 에피소드 ID</summary>
        public string episodeId;

        /// <summary>해당 에피소드의 VisualNovelSO 에셋</summary>
        public VisualNovelSO episodeSO;
    }

    #endregion
}
