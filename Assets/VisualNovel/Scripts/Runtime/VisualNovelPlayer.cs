using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System;

public class VisualNovelPlayer : MonoBehaviour
{
    // --- 테스트 모드(PFH) 전역 상태 관리 ---
    public static class TestModeGlobals
    {
        public static bool IsFastForwarding = false;
        public static string FastForwardTargetID = null;
    }
    // ---

    [Header("Episode Data")]
    [SerializeField] private TextAsset episodeJson;

    [Header("Controllers")]
    [SerializeField] private TextController textController;
    [SerializeField] private PortraitController portraitController;
    [SerializeField] private ImageController imageController;
    [SerializeField] private SoundController soundController;

    private EpisodeData _currentEpisodeData;
    private Dictionary<string, NodeData> _nodeLookup;
    private int _branchChoice = -1;
    private int _activeBranches = 0;

    // --- 4단계: LiveTestHelper 참조 ---
    private LiveTestHelper _liveTestHelperInstance;
    // ---

    void Start()
    {
        PlayEpisode();
    }

    public void PlayEpisode()
    {
        // 데이터 로드 및 초기화 (공통 로직 분리)
        if (LoadAndInitialize())
        {
            // 일반 재생이므로 FF 상태 끄기
            TestModeGlobals.IsFastForwarding = false;
            TestModeGlobals.FastForwardTargetID = null;
            StartCoroutine(RunEpisode());
        }
    }

    // --- 4단계: Play From Here 진입점 ---
    public void PlayFromHere(string targetNodeID)
    {
        Debug.Log($"[Player] Play from Here 실행: {targetNodeID}");

        // 1. 실행 중인 코루틴 중지
        StopAllCoroutines();

        // 2. FF 상태 강력 초기화 (이전 상태 제거) - **핵심 수정**
        //    ResetAllControllers보다 먼저 초기화하여 상태 꼬임 방지
        TestModeGlobals.IsFastForwarding = false;
        TestModeGlobals.FastForwardTargetID = null;

        // 3. 컨트롤러 및 상태 리셋
        ResetAllControllers();

        // 4. 데이터 강제 재로드 및 초기화 (핵심 수정: 무조건 실행)
        //    이를 통해 에디터의 최신 변경 사항과 유효한 타겟 ID를 확보합니다.
        if (LoadAndInitialize())
        {
            // 5. 전역 플래그 설정 (모든 초기화가 끝난 후 설정)
            TestModeGlobals.IsFastForwarding = true;
            TestModeGlobals.FastForwardTargetID = targetNodeID;

            Debug.Log($"[Player] FF 상태 설정 완료. IsFastForwarding: {TestModeGlobals.IsFastForwarding}, Target: {TestModeGlobals.FastForwardTargetID}");

            // 6. 에피소드 재시작
            StartCoroutine(RunEpisode());
        }
    }
    // ---

    // [리팩토링] 데이터 로드와 초기화를 담당하는 통합 메서드
    private bool LoadAndInitialize()
    {
        // 1. 씬에서 LiveTestHelper 갱신
        _liveTestHelperInstance = FindFirstObjectByType<LiveTestHelper>();

        // 2. 데이터 로드 시도
        if (!LoadEpisodeData())
        {
            Debug.LogError("에피소드 데이터를 로드할 수 없습니다.");
            return false;
        }

        // 3. 컨트롤러 및 노드 룩업 빌드
        InitializeControllers();
        BuildNodeLookup();

        return true;
    }

    private bool LoadEpisodeData()
    {
        try
        {
            if (_liveTestHelperInstance != null && _liveTestHelperInstance.testEpisodeSO != null)
            {
                Debug.Log("[Player] LiveTestHelper 감지. VisualNovelSO에서 데이터를 로드합니다.");
                LoadEpisodeDataFromSO(_liveTestHelperInstance.testEpisodeSO);
                return true;
            }
            else if (episodeJson != null)
            {
                _currentEpisodeData = JsonConvert.DeserializeObject<EpisodeData>(episodeJson.text);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"데이터 로드 중 오류 발생: {ex.Message}");
            return false;
        }
    }

    private void LoadEpisodeDataFromSO(VisualNovelSO so)
    {
        _currentEpisodeData = new EpisodeData
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

    private void ResetAllControllers()
    {
        if (textController != null) textController.HideAll();
        if (portraitController != null) portraitController.Reset();
        if (imageController != null) imageController.Reset();
        if (soundController != null) soundController.Reset();

        _branchChoice = -1;
        _activeBranches = 0;

        // 안전장치: 여기서도 초기화하지만 PlayFromHere 진입점에서 이미 수행함
        TestModeGlobals.IsFastForwarding = false;
        TestModeGlobals.FastForwardTargetID = null;
    }

    private void InitializeControllers()
    {
        textController.HideAll();
        if (_currentEpisodeData.datasets != null)
        {
            portraitController.Initialize(_currentEpisodeData.datasets);
        }
        else
        {
            _currentEpisodeData.datasets = new Datasets();
            portraitController.Initialize(_currentEpisodeData.datasets);
        }
    }

    private void BuildNodeLookup()
    {
        if (_currentEpisodeData.nodeGraph?.nodes == null) return;
        _nodeLookup = _currentEpisodeData.nodeGraph.nodes.ToDictionary(node => node.id);
    }

    private IEnumerator RunEpisode()
    {
        _activeBranches = 0;
        NodeData startNode = _currentEpisodeData.nodeGraph.nodes.FirstOrDefault(node => node.type == "Start");
        if (startNode == null)
        {
            Debug.LogError("Start node not found!");
            yield break;
        }

        var startingNodes = GetAllNextNodes(startNode);
        if (!startingNodes.Any())
        {
            Debug.Log("Episode finished: Start node has no connections.");
            yield break;
        }

        foreach (var node in startingNodes)
        {
            StartCoroutine(ExecuteBranch(node));
        }

        yield return new WaitUntil(() => _activeBranches == 0);
        Debug.Log("Episode playback finished.");

        TestModeGlobals.IsFastForwarding = false;
        TestModeGlobals.FastForwardTargetID = null;
    }

    private IEnumerator ExecuteBranch(NodeData startNode)
    {
        _activeBranches++;
        var currentNode = startNode;

        while (currentNode != null && currentNode.type != "End")
        {
            if (_liveTestHelperInstance != null)
            {
                LiveTestHelper.TriggerNodeProcessing(currentNode.id);
            }

            // [수정] 선 검사, 후 처리 (이전 노드보다 앞에 있는 노드를 타겟으로 했을 때 문제 해결)
            if (currentNode.id == TestModeGlobals.FastForwardTargetID)
            {
                Debug.Log($"[Player] FF 타겟 {currentNode.id} 도달. FF를 종료합니다.");
                TestModeGlobals.IsFastForwarding = false;
                TestModeGlobals.FastForwardTargetID = null;
            }

            yield return ProcessNode(currentNode, TestModeGlobals.IsFastForwarding);

            if (currentNode.type == "Branch")
            {
                if (TestModeGlobals.IsFastForwarding)
                {
                    yield return new WaitUntil(() => _branchChoice != -1);
                }
                else
                {
                    yield return new WaitUntil(() => _branchChoice != -1);
                }

                currentNode = GetNextBranchNode(currentNode, _branchChoice);
                _branchChoice = -1;
            }
            else
            {
                var nextNodes = GetAllNextNodes(currentNode);
                if (!nextNodes.Any())
                {
                    currentNode = null;
                    continue;
                }

                currentNode = nextNodes[0];

                foreach (var parallelNode in nextNodes.Skip(1))
                {
                    StartCoroutine(ExecuteBranch(parallelNode));
                }
            }
        }
        _activeBranches--;
    }

    private IEnumerator ProcessNode(NodeData node, bool isFastForwarding)
    {
        switch (node.type)
        {
            case "Text":
                yield return ExecuteTextNode(node, isFastForwarding);
                break;

            case "Branch":
                if (isFastForwarding)
                {
                    Debug.Log("[Player] FF: 분기점 도달. 잠시 멈춤.");
                    yield return ExecuteBranchNode(node);
                    yield return new WaitUntil(() => _branchChoice != -1);
                    Debug.Log("[Player] FF: 입력 완료. 재개.");
                }
                else
                {
                    yield return ExecuteBranchNode(node);
                    yield return new WaitUntil(() => _branchChoice != -1);
                }
                break;

            case "Wait":
                if (!isFastForwarding)
                {
                    var waitFields = JsonConvert.DeserializeObject<WaitNodeFields>(node.fields);
                    yield return new WaitForSeconds(waitFields.duration);
                }
                break;

            case "Portrait Enter":
            case "Portrait Anim":
            case "Portrait Exit":
                yield return portraitController.ProcessNode(node, isFastForwarding);
                break;
            case "Image":
                yield return imageController.ProcessNode(node, isFastForwarding);
                break;
            case "Sound":
                yield return soundController.ProcessNode(node, isFastForwarding);
                break;

            case "Start":
            case "End":
            case "Comment":
                break;
            default:
                break;
        }
    }

    private IEnumerator ExecuteTextNode(NodeData node, bool isFastForwarding)
    {
        var fields = JsonConvert.DeserializeObject<TextNodeFields>(node.fields);
        if (fields == null) yield break;

        var lines = _currentEpisodeData.datasets.dialogueLines.Where(line => line.sceneName == fields.sceneName).ToList();
        foreach (var line in lines)
        {
            bool isPortraitPlaying = true, isTextPlaying = true;
            StartCoroutine(RunCoroutineWithCallback(portraitController.UpdatePortraitFromDialogue(line), () => isPortraitPlaying = false));
            StartCoroutine(RunCoroutineWithCallback(textController.PlayDialogueLine(line, fields.display), () => isTextPlaying = false));

            if (isFastForwarding)
            {
                portraitController.SkipAnimation();
                textController.SkipTyping();
                continue;
            }

            while (isPortraitPlaying || isTextPlaying)
            {
                if (line.skippable && Input.GetMouseButtonDown(0))
                {
                    portraitController.SkipAnimation();
                    textController.SkipTyping();
                    break;
                }
                yield return null;
            }
            yield return textController.WaitForConfirmInput();
        }
    }

    private IEnumerator RunCoroutineWithCallback(IEnumerator coroutine, Action onComplete)
    {
        yield return coroutine;
        onComplete?.Invoke();
    }

    private IEnumerator ExecuteBranchNode(NodeData node)
    {
        var fields = JsonConvert.DeserializeObject<BranchNodeFields>(node.fields);
        if (fields == null) { _branchChoice = 0; yield break; }

        var branchDialogues = _currentEpisodeData.datasets.dialogueLines.Where(line => line.sceneName == fields.sceneName).ToList();
        if (branchDialogues.Any())
        {
            _branchChoice = -1;
            textController.SetBranchCallback(index => _branchChoice = index);
            textController.ShowBranches(branchDialogues.Select(d => d.speakerText).ToList());
        }
        else
        {
            _branchChoice = 0;
        }
    }

    private List<NodeData> GetAllNextNodes(NodeData node)
    {
        if (node == null || _currentEpisodeData.nodeGraph?.connections == null) return new List<NodeData>();
        return _currentEpisodeData.nodeGraph.connections
            .Where(conn => conn.sourceNodeId == node.id)
            .OrderBy(conn => conn.sourceOutputIndex) // Branch 순서를 위해 정렬 추가
            .Select(conn => _nodeLookup.ContainsKey(conn.targetNodeId) ? _nodeLookup[conn.targetNodeId] : null)
            .Where(n => n != null)
            .ToList();
    }

    private NodeData GetNextBranchNode(NodeData branchNode, int choice)
    {
        var connections = _currentEpisodeData.nodeGraph.connections
            .Where(conn => conn.sourceNodeId == branchNode.id)
            .OrderBy(conn => conn.sourceOutputIndex)
            .ToList();

        if (choice >= 0 && choice < connections.Count)
        {
            string targetNodeId = connections[choice].targetNodeId;
            return _nodeLookup.ContainsKey(targetNodeId) ? _nodeLookup[targetNodeId] : null;
        }
        return null; // 잘못된 선택
    }
}