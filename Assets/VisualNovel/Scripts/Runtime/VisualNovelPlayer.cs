using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;
using System;

public class VisualNovelPlayer : MonoBehaviour
{
    // --- �׽�Ʈ ���(PFH) ���� ���� ���� ---
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

    // --- 4�ܰ�: LiveTestHelper ���� ---
    private LiveTestHelper _liveTestHelperInstance;
    // ---

    /// <summary>
    /// 에피소드 재생이 완료되었을 때 발행되는 이벤트.
    /// </summary>
    public event Action OnEpisodeCompleted;

    void Start()
    {
        PlayEpisode();
    }

    public void PlayEpisode()
    {
        // ������ �ε� �� �ʱ�ȭ (���� ���� �и�)
        if (LoadAndInitialize())
        {
            // �Ϲ� ����̹Ƿ� FF ���� ����
            TestModeGlobals.IsFastForwarding = false;
            TestModeGlobals.FastForwardTargetID = null;
            StartCoroutine(RunEpisode());
        }
    }

    /// <summary>
    /// 외부에서 EpisodeData를 직접 전달하여 에피소드를 재생한다.
    /// GameFlowController/VisualNovelBridge 등이 호출한다.
    /// </summary>
    /// <param name="data">재생할 에피소드 데이터</param>
    /// <param name="onCompleted">재생 완료 시 호출될 콜백 (일회성)</param>
    public void PlayEpisode(EpisodeData data, Action onCompleted)
    {
        StopAllCoroutines();
        ResetAllControllers();

        _currentEpisodeData = data;

        if (onCompleted != null)
        {
            Action handler = null;
            handler = () =>
            {
                OnEpisodeCompleted -= handler;
                onCompleted.Invoke();
            };
            OnEpisodeCompleted += handler;
        }

        InitializeControllers();
        BuildNodeLookup();

        TestModeGlobals.IsFastForwarding = false;
        TestModeGlobals.FastForwardTargetID = null;
        StartCoroutine(RunEpisode());
    }

    // --- 4�ܰ�: Play From Here ������ ---
    public void PlayFromHere(string targetNodeID)
    {
        Debug.Log($"[Player] Play from Here ����: {targetNodeID}");

        // 1. ���� ���� �ڷ�ƾ ����
        StopAllCoroutines();

        // 2. FF ���� ���� �ʱ�ȭ (���� ���� ����) - **�ٽ� ����**
        //    ResetAllControllers���� ���� �ʱ�ȭ�Ͽ� ���� ���� ����
        TestModeGlobals.IsFastForwarding = false;
        TestModeGlobals.FastForwardTargetID = null;

        // 3. ��Ʈ�ѷ� �� ���� ����
        ResetAllControllers();

        // 4. ������ ���� ��ε� �� �ʱ�ȭ (�ٽ� ����: ������ ����)
        //    �̸� ���� �������� �ֽ� ���� ���װ� ��ȿ�� Ÿ�� ID�� Ȯ���մϴ�.
        if (LoadAndInitialize())
        {
            // 5. ���� �÷��� ���� (��� �ʱ�ȭ�� ���� �� ����)
            TestModeGlobals.IsFastForwarding = true;
            TestModeGlobals.FastForwardTargetID = targetNodeID;

            Debug.Log($"[Player] FF ���� ���� �Ϸ�. IsFastForwarding: {TestModeGlobals.IsFastForwarding}, Target: {TestModeGlobals.FastForwardTargetID}");

            // 6. ���Ǽҵ� �����
            StartCoroutine(RunEpisode());
        }
    }
    // ---

    // [�����丵] ������ �ε�� �ʱ�ȭ�� ����ϴ� ���� �޼���
    private bool LoadAndInitialize()
    {
        // 1. ������ LiveTestHelper ����
        _liveTestHelperInstance = FindFirstObjectByType<LiveTestHelper>();

        // 2. ������ �ε� �õ�
        if (!LoadEpisodeData())
        {
            Debug.LogError("���Ǽҵ� �����͸� �ε��� �� �����ϴ�.");
            return false;
        }

        // 3. ��Ʈ�ѷ� �� ��� ��� ����
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
                Debug.Log("[Player] LiveTestHelper ����. VisualNovelSO���� �����͸� �ε��մϴ�.");
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
            Debug.LogError($"������ �ε� �� ���� �߻�: {ex.Message}");
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

        // ������ġ: ���⼭�� �ʱ�ȭ������ PlayFromHere ���������� �̹� ������
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

        OnEpisodeCompleted?.Invoke();
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

            // [����] �� �˻�, �� ó�� (���� ��庸�� �տ� �ִ� ��带 Ÿ������ ���� �� ���� �ذ�)
            if (currentNode.id == TestModeGlobals.FastForwardTargetID)
            {
                Debug.Log($"[Player] FF Ÿ�� {currentNode.id} ����. FF�� �����մϴ�.");
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
                    Debug.Log("[Player] FF: �б��� ����. ��� ����.");
                    yield return ExecuteBranchNode(node);
                    yield return new WaitUntil(() => _branchChoice != -1);
                    Debug.Log("[Player] FF: �Է� �Ϸ�. �簳.");
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
            .OrderBy(conn => conn.sourceOutputIndex) // Branch ������ ���� ���� �߰�
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
        return null; // �߸��� ����
    }
}