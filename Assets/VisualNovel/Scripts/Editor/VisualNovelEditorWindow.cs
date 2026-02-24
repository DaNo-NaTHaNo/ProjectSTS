using UnityEditor;
using UnityEditor.Experimental.GraphView; // 하이라이트 기능을 위해 NodeView 타입 필요
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

public class VisualNovelEditorWindow : EditorWindow
{
    // [추가] 필드 값 변경을 감지하기 위한 static 이벤트
    public static event Action<string> OnNodeModified;

    // JSON 직렬화 설정 (순환 참조 무시)
    public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public static Action OnProjectReset;

    private VisualNovelGraphView _graphView;
    private VisualNovelSO _currentEpisodeData;
    private string _currentFilePath = "New Episode";
    private Label _fileNameLabel;
    private string _copyBuffer;
    private Vector2 _mousePosition; // Vector2 -> UnityEngine.Vector2

    // --- 3단계: 하이라이트 기능용 필드 ---
    private NodeView _lastHighlightedNode;
    // ---

    public static void TriggerNodeModified(string undoName)
    {
        OnNodeModified?.Invoke(undoName);
    }
    [MenuItem("Tools/Visual Novel Editor")]
    public static void OpenWindow()
    {
        GetWindow<VisualNovelEditorWindow>("Visual Novel Editor");
    }

    private void OnEnable()
    {
        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);
        rootVisualElement.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        // [추가] Undo/Redo 실행 시와 필드 값 변경 시 호출될 메서드 등록
        Undo.undoRedoPerformed += OnUndoRedo;
        OnNodeModified += RequestSave;

        // --- 3단계: 하이라이트 이벤트 구독 (최종 아키텍처) ---
        LiveTestHelper.OnNodeStartProcessing += HandleNodeStartProcessing;
        // ---

        EditorApplication.delayCall += () =>
        {
            _currentEpisodeData = AssetDatabase.LoadAssetAtPath<VisualNovelSO>("Assets/VisualNovel/Scripts/Data/CurrentEpisode.asset");
            if (_currentEpisodeData == null)
            {
                Debug.LogError("CurrentEpisode.asset을 찾을 수 없습니다. Scripts/Data 폴더에 생성했는지 확인해주세요.");
                Close();
                return;
            }
            CreateAndRestoreGUI();
        };
    }

    private void OnDisable()
    {
        rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        rootVisualElement.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        // [추가] 등록 해제
        Undo.undoRedoPerformed -= OnUndoRedo;
        OnNodeModified -= RequestSave;

        // --- 3단계: 하이라이트 이벤트 구독 해제 (최종 아키텍처) ---
        LiveTestHelper.OnNodeStartProcessing -= HandleNodeStartProcessing;
        // ---
    }

    // --- 3단계: 하이라이트 처리 핸들러 ---
    private void HandleNodeStartProcessing(string nodeID)
    {
        // UI 조작은 메인 스레드에서 실행되도록 보장
        rootVisualElement.schedule.Execute(() =>
        {
            // 이전에 하이라이트된 노드의 스타일 제거
            _lastHighlightedNode?.RemoveFromClassList("playing-node");

            if (_graphView == null) return;

            // 새 노드를 찾아 스타일 추가
            _lastHighlightedNode = _graphView.GetNodeByGuid(nodeID) as NodeView;
            _lastHighlightedNode?.AddToClassList("playing-node");
        });
    }
    // ---

    private void CreateAndRestoreGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/VisualNovel/Scripts/Editor/VisualNovelEditorWindow.uxml");
        visualTree.CloneTree(rootVisualElement);

        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/VisualNovel/Scripts/Editor/VisualNovelEditorWindow.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        _graphView = new VisualNovelGraphView();
        // [수정] GraphView의 변경 신호를 RequestSave 메서드에 연결
        _graphView.OnGraphModified = RequestSave;

        var graphViewContainer = rootVisualElement.Q<VisualElement>("graph-view-container");
        graphViewContainer.Add(_graphView);

        _fileNameLabel = rootVisualElement.Q<Label>("file-name-label");

        rootVisualElement.Q<Button>("new-button").clicked += OnNewButtonClicked;
        rootVisualElement.Q<Button>("save-button").clicked += SaveToFile;
        rootVisualElement.Q<Button>("load-button").clicked += LoadFromFile;

        rootVisualElement.Q<Button>("start-node-button").clicked += () => _graphView.CreateNode("Start", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("end-node-button").clicked += () => _graphView.CreateNode("End", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("comment-node-button").clicked += () => _graphView.CreateNode("Comment", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("wait-node-button").clicked += () => _graphView.CreateNode("Wait", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("text-node-button").clicked += () => _graphView.CreateNode("Text", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("branch-node-button").clicked += () => _graphView.CreateNode("Branch", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("portrait-enter-node-button").clicked += () => _graphView.CreateNode("Portrait Enter", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("portrait-anim-node-button").clicked += () => _graphView.CreateNode("Portrait Anim", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("portrait-exit-node-button").clicked += () => _graphView.CreateNode("Portrait Exit", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("image-node-button").clicked += () => _graphView.CreateNode("Image", GetMousePositionInGraph());
        rootVisualElement.Q<Button>("sound-node-button").clicked += () => _graphView.CreateNode("Sound", GetMousePositionInGraph());

        rootVisualElement.Q<Button>("dialogueline-button").clicked += () => DialogueLineEditor.OpenWindow(_currentEpisodeData);
        rootVisualElement.Q<Button>("episodeportrait-button").clicked += () => EpisodePortraitEditor.OpenWindow(_currentEpisodeData);
        rootVisualElement.Q<Button>("locationpreset-button").clicked += () => LocationPresetEditor.OpenWindow(_currentEpisodeData);

        _graphView.PopulateView(_currentEpisodeData);
    }

    private Vector2 GetMousePositionInGraph()
    {
        // Vector2 -> UnityEngine.Vector2
        return _graphView.contentViewContainer.WorldToLocal(_mousePosition);
    }

    // [추가] Undo 기록과 저장을 담당하는 단일 메서드
    private void RequestSave(string undoName)
    {
        if (_currentEpisodeData == null) return;

        // 1. "변경 전" 상태를 즉시 스냅샷으로 기록합니다.
        Undo.RegisterCompleteObjectUndo(_currentEpisodeData, undoName);

        // 2. "변경 후" 상태를 저장하는 로직은 GraphView가 안정화된 후(다음 프레임) 실행합니다.
        EditorApplication.delayCall += () =>
        {
            // 에디터 창이 닫혔거나 데이터가 없는 경우를 대비한 방어 코드
            if (this == null || _currentEpisodeData == null)
            {
                return;
            }

            // 3. 안정화된 '변경 후' 상태를 SO에 저장합니다.
            SaveGraphStateToSO();

            // 4. SO에 변경사항이 있음을 Unity에 명시적으로 알립니다. (디스크 저장용)
            EditorUtility.SetDirty(_currentEpisodeData);
        };
    }
    // [추가] Undo/Redo 실행 시 화면을 다시 그리는 메서드
    private void OnUndoRedo()
    {
        if (_graphView != null && _currentEpisodeData != null)
        {
            _graphView.PopulateView(_currentEpisodeData);
        }
    }

    private void SaveGraphStateToSO()
    {
        if (_graphView == null || _currentEpisodeData == null) return;

        var nodeGraphData = new NodeGraph { nodes = new List<NodeData>(), connections = new List<ConnectionData>() };

        // [수정] graphElements.OfType<NodeView>() 사용 확인
        _graphView.graphElements.OfType<NodeView>().ToList().ForEach(nodeView =>
        {
            var nodePosition = nodeView.GetPosition().position;
            var nodeFieldsData = nodeView.SaveData();
            string fieldsJson = (nodeFieldsData != null) ? JsonConvert.SerializeObject(nodeFieldsData, JsonSettings) : "{}";
            nodeGraphData.nodes.Add(new NodeData
            {
                id = nodeView.GUID,
                type = nodeView.title,
                position = new PositionData { x = nodePosition.x, y = nodePosition.y },
                fields = fieldsJson
            });
        });

        // [수정] graphElements.OfType<Edge>() 사용 확인
        _graphView.graphElements.OfType<Edge>().ToList().ForEach(edge =>
        {
            var sourceNode = edge.output?.node as NodeView;
            var targetNode = edge.input?.node as NodeView;
            if (sourceNode == null || targetNode == null) return;

            var outputPorts = sourceNode.Query<Port>().Where(p => p.direction == Direction.Output).ToList();
            var sourcePortIndex = outputPorts.IndexOf(edge.output);

            nodeGraphData.connections.Add(new ConnectionData
            {
                sourceNodeId = sourceNode.GUID,
                sourceOutputIndex = sourcePortIndex,
                targetNodeId = targetNode.GUID
            });
        });

        _currentEpisodeData.nodeGraphJson = JsonConvert.SerializeObject(nodeGraphData, Formatting.Indented, JsonSettings);
        // SetDirty는 RegisterCompleteObjectUndo에 포함되어 있으므로 제거
        // EditorUtility.SetDirty(_currentEpisodeData);
    }

    private void SaveToFile()
    {
        string filePath = EditorUtility.SaveFilePanel("Save Episode", "", _currentFilePath, "json");
        if (string.IsNullOrEmpty(filePath)) return;

        _currentFilePath = Path.GetFileName(filePath);
        if (_fileNameLabel != null) _fileNameLabel.text = _currentFilePath;

        SaveGraphStateToSO(); // SaveToFile 호출 시 SO에도 저장되도록 유지

        var episodeData = new EpisodeData
        {
            episodeInfo = new EpisodeInfo { title = Path.GetFileNameWithoutExtension(filePath) },
            datasets = new Datasets
            {
                dialogueLines = _currentEpisodeData.dialogueLines,
                episodePortraits = _currentEpisodeData.episodePortraits,
                locationPresets = _currentEpisodeData.locationPresets
            },
            nodeGraph = JsonConvert.DeserializeObject<NodeGraph>(_currentEpisodeData.nodeGraphJson, JsonSettings)
        };

        string jsonString = JsonConvert.SerializeObject(episodeData, Formatting.Indented, JsonSettings);
        File.WriteAllText(filePath, jsonString);

        AssetDatabase.Refresh();
        Debug.Log($"Episode saved to: {filePath}");
    }

    private void LoadFromFile()
    {
        string filePath = EditorUtility.OpenFilePanel("Load Episode", "", "json");
        if (string.IsNullOrEmpty(filePath)) return;

        _currentFilePath = Path.GetFileName(filePath);
        if (_fileNameLabel != null) _fileNameLabel.text = _currentFilePath;

        string jsonString = File.ReadAllText(filePath);
        var episodeData = JsonConvert.DeserializeObject<EpisodeData>(jsonString, JsonSettings);

        // Null 체크 강화
        if (episodeData == null) episodeData = new EpisodeData();
        if (episodeData.datasets == null) episodeData.datasets = new Datasets();
        if (episodeData.datasets.dialogueLines == null) episodeData.datasets.dialogueLines = new List<DialogueLine>();
        if (episodeData.datasets.episodePortraits == null) episodeData.datasets.episodePortraits = new List<EpisodePortrait>();
        if (episodeData.datasets.locationPresets == null) episodeData.datasets.locationPresets = new List<LocationPreset>();
        if (episodeData.nodeGraph == null) episodeData.nodeGraph = new NodeGraph { nodes = new List<NodeData>(), connections = new List<ConnectionData>() };

        // [오타 수정] "V"라고 적혀있던 잘못된 라인 제거

        // Undo 기록 없이 SO를 직접 업데이트
        _currentEpisodeData.dialogueLines = episodeData.datasets.dialogueLines;
        _currentEpisodeData.episodePortraits = episodeData.datasets.episodePortraits;
        _currentEpisodeData.locationPresets = episodeData.datasets.locationPresets;
        _currentEpisodeData.nodeGraphJson = JsonConvert.SerializeObject(episodeData.nodeGraph, JsonSettings);
        EditorUtility.SetDirty(_currentEpisodeData); // 로드 후에는 SetDirty 필요

        _graphView.PopulateView(_currentEpisodeData);

        OnProjectReset?.Invoke(); // 다른 에디터 창에도 변경 알림

        Debug.Log($"Episode loaded from: {filePath}");
    }

    private void OnKeyDown(KeyDownEvent e)
    {
        if (e.ctrlKey && e.keyCode == KeyCode.S)
        {
            e.StopPropagation();
            SaveToFile();
        }

        if (e.ctrlKey && e.keyCode == KeyCode.A)
        {
            e.StopPropagation();
            _graphView.ClearSelection();
            if (_currentEpisodeData != null && !string.IsNullOrEmpty(_currentEpisodeData.nodeGraphJson))
            {
                var nodeGraph = JsonConvert.DeserializeObject<NodeGraph>(_currentEpisodeData.nodeGraphJson);
                if (nodeGraph?.nodes != null)
                {
                    foreach (var nodeData in nodeGraph.nodes)
                    {
                        var nodeView = _graphView.GetNodeByGuid(nodeData.id) as NodeView;
                        if (nodeView != null) _graphView.AddToSelection(nodeView);
                    }
                }
            }
        }

        if (e.ctrlKey && e.keyCode == KeyCode.C)
        {
            e.StopPropagation();
            CopySelectionToBuffer();
        }

        if (e.ctrlKey && e.keyCode == KeyCode.V)
        {
            e.StopPropagation();
            PasteFromBuffer();
        }
    }

    private void OnMouseMove(MouseMoveEvent e)
    {
        _mousePosition = e.mousePosition; // Vector2 -> UnityEngine.Vector2
    }

    private void CopySelectionToBuffer()
    {
        var selectedNodes = _graphView.selection.OfType<NodeView>().ToList();
        if (!selectedNodes.Any()) return;

        var nodeGraphData = new NodeGraph
        {
            nodes = new List<NodeData>(),
            connections = new List<ConnectionData>()
        };

        foreach (var nodeView in selectedNodes)
        {
            var nodeFieldsData = nodeView.SaveData();
            string fieldsJson = (nodeFieldsData != null) ? JsonConvert.SerializeObject(nodeFieldsData, JsonSettings) : "{}";
            nodeGraphData.nodes.Add(new NodeData
            {
                id = nodeView.GUID,
                type = nodeView.title,
                position = new PositionData { x = nodeView.GetPosition().position.x, y = nodeView.GetPosition().position.y },
                fields = fieldsJson
            });
        }

        var selectedNodeGuids = new HashSet<string>(selectedNodes.Select(n => n.GUID));
        foreach (var edge in _graphView.edges)
        {
            var sourceNode = edge.output?.node as NodeView;
            var targetNode = edge.input?.node as NodeView;

            if (sourceNode != null && targetNode != null &&
                selectedNodeGuids.Contains(sourceNode.GUID) &&
                selectedNodeGuids.Contains(targetNode.GUID))
            {
                var outputPorts = sourceNode.Query<Port>().Where(p => p.direction == Direction.Output).ToList();
                int sourcePortIndex = outputPorts.IndexOf(edge.output);

                nodeGraphData.connections.Add(new ConnectionData
                {
                    sourceNodeId = sourceNode.GUID,
                    sourceOutputIndex = sourcePortIndex,
                    targetNodeId = targetNode.GUID
                });
            }
        }

        _copyBuffer = JsonConvert.SerializeObject(nodeGraphData, Formatting.Indented, JsonSettings);
    }

    private void PasteFromBuffer()
    {
        if (string.IsNullOrEmpty(_copyBuffer)) return;

        var nodeGraph = JsonConvert.DeserializeObject<NodeGraph>(_copyBuffer, JsonSettings);
        if (nodeGraph == null || !nodeGraph.nodes.Any()) return;

        _graphView.ClearSelection();

        var oldToNewGuids = new Dictionary<string, string>();

        var pastePosition = _graphView.contentViewContainer.WorldToLocal(_mousePosition);
        UnityEngine.Vector2 averagePosition = UnityEngine.Vector2.zero; // Vector2 -> UnityEngine.Vector2
        nodeGraph.nodes.ForEach(n => averagePosition += new UnityEngine.Vector2(n.position.x, n.position.y)); // Vector2 -> UnityEngine.Vector2
        if (nodeGraph.nodes.Count > 0) averagePosition /= nodeGraph.nodes.Count;

        foreach (var nodeData in nodeGraph.nodes)
        {
            var originalPosition = new UnityEngine.Vector2(nodeData.position.x, nodeData.position.y); // Vector2 -> UnityEngine.Vector2
            var offset = originalPosition - averagePosition;
            var newPosition = pastePosition + offset;

            var newNode = _graphView.CreateNode(nodeData.type, newPosition);
            oldToNewGuids[nodeData.id] = newNode.GUID;

            if (!string.IsNullOrEmpty(nodeData.fields) && nodeData.fields != "{}")
            {
                try
                {
                    Type fieldType = _graphView.GetFieldType(nodeData.type);
                    if (fieldType != null)
                    {
                        var fieldData = (BaseNodeFields)JsonConvert.DeserializeObject(nodeData.fields, fieldType);
                        if (fieldData != null) newNode.LoadData(fieldData);
                    }
                }
                catch (Exception e) { Debug.LogError($"Failed to load field data for pasted node: {e.Message}"); }
            }

            _graphView.AddToSelection(newNode);
        }

        _graphView.schedule.Execute(() =>
        {
            foreach (var connectionData in nodeGraph.connections)
            {
                if (!oldToNewGuids.ContainsKey(connectionData.sourceNodeId) || !oldToNewGuids.ContainsKey(connectionData.targetNodeId)) continue;

                var newSourceGuid = oldToNewGuids[connectionData.sourceNodeId];
                var newTargetGuid = oldToNewGuids[connectionData.targetNodeId];

                var sourceNode = _graphView.GetNodeByGuid(newSourceGuid) as NodeView;
                var targetNode = _graphView.GetNodeByGuid(newTargetGuid) as NodeView;

                if (sourceNode == null || targetNode == null) continue;

                var outputPorts = sourceNode.Query<Port>().Where(p => p.direction == Direction.Output).ToList();
                var inputPort = targetNode.Query<Port>().Where(p => p.direction == Direction.Input).ToList().FirstOrDefault();

                if (inputPort != null && outputPorts.Count > connectionData.sourceOutputIndex)
                {
                    var outputPort = outputPorts[connectionData.sourceOutputIndex];
                    var edge = outputPort.ConnectTo(inputPort);
                    _graphView.AddElement(edge);
                }
            }
        });
    }

    // ▼▼▼ New 버튼에 OnNewButtonClicked 함수 연결 ▼▼▼ (이 메서드는 CreateAndRestoreGUI 안에 이미 있음)
    // rootVisualElement.Q<Button>("new-button").clicked += OnNewButtonClicked;

    // 'New' 버튼 클릭 시 호출되는 메서드
    private void OnNewButtonClicked()
    {
        bool confirmed = EditorUtility.DisplayDialog("새 파일", "현재 작업 내용을 저장하지 않고 모두 초기화하시겠습니까?", "확인", "취소");
        if (confirmed)
        {
            Undo.RecordObject(_currentEpisodeData, "New Episode"); // Undo 기록 추가

            _currentEpisodeData.dialogueLines.Clear();
            _currentEpisodeData.episodePortraits.Clear();
            _currentEpisodeData.locationPresets.Clear();
            _currentEpisodeData.nodeGraphJson = "";
            _currentFilePath = "New Episode";
            if (_fileNameLabel != null) _fileNameLabel.text = _currentFilePath;

            // [오타 수정] "Such_as_Two_Points:" 라는 잘못된 레이블 제거
            EditorUtility.SetDirty(_currentEpisodeData); // SetDirty는 RegisterCompleteObjectUndo 전에 필요할 수 있음
                                                         // AssetDatabase.SaveAssets(); // 즉시 저장보다는 SetDirty 유지

            _graphView.PopulateView(_currentEpisodeData);
            OnProjectReset?.Invoke();
            Debug.Log("새 에피소드를 시작합니다.");
        }
    }
}

