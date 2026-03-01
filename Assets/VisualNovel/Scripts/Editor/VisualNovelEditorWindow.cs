using UnityEditor;
using UnityEditor.Experimental.GraphView; // ���̶���Ʈ ����� ���� NodeView Ÿ�� �ʿ�
using UnityEngine;
using UnityEngine.UIElements;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

public class VisualNovelEditorWindow : EditorWindow
{
    // [�߰�] �ʵ� �� ������ �����ϱ� ���� static �̺�Ʈ
    public static event Action<string> OnNodeModified;

    // JSON ����ȭ ���� (��ȯ ���� ����)
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

    // --- 3�ܰ�: ���̶���Ʈ ��ɿ� �ʵ� ---
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
        // [�߰�] Undo/Redo ���� �ÿ� �ʵ� �� ���� �� ȣ��� �޼��� ���
        Undo.undoRedoPerformed += OnUndoRedo;
        OnNodeModified += RequestSave;

        // --- 3�ܰ�: ���̶���Ʈ �̺�Ʈ ���� (���� ��Ű��ó) ---
        LiveTestHelper.OnNodeStartProcessing += HandleNodeStartProcessing;
        // ---

        EditorApplication.delayCall += () =>
        {
            _currentEpisodeData = AssetDatabase.LoadAssetAtPath<VisualNovelSO>("Assets/VisualNovel/Scripts/Data/CurrentEpisode.asset");
            if (_currentEpisodeData == null)
            {
                Debug.LogError("CurrentEpisode.asset�� ã�� �� �����ϴ�. Scripts/Data ������ �����ߴ��� Ȯ�����ּ���.");
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
        // [�߰�] ��� ����
        Undo.undoRedoPerformed -= OnUndoRedo;
        OnNodeModified -= RequestSave;

        // --- 3�ܰ�: ���̶���Ʈ �̺�Ʈ ���� ���� (���� ��Ű��ó) ---
        LiveTestHelper.OnNodeStartProcessing -= HandleNodeStartProcessing;
        // ---
    }

    // --- 3�ܰ�: ���̶���Ʈ ó�� �ڵ鷯 ---
    private void HandleNodeStartProcessing(string nodeID)
    {
        // UI ������ ���� �����忡�� ����ǵ��� ����
        rootVisualElement.schedule.Execute(() =>
        {
            // ������ ���̶���Ʈ�� ����� ��Ÿ�� ����
            _lastHighlightedNode?.RemoveFromClassList("playing-node");

            if (_graphView == null) return;

            // �� ��带 ã�� ��Ÿ�� �߰�
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
        // [����] GraphView�� ���� ��ȣ�� RequestSave �޼��忡 ����
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
        rootVisualElement.Q<Button>("command-node-button").clicked += () => _graphView.CreateNode("Command", GetMousePositionInGraph());

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

    // [�߰�] Undo ��ϰ� ������ ����ϴ� ���� �޼���
    private void RequestSave(string undoName)
    {
        if (_currentEpisodeData == null) return;

        // 1. "���� ��" ���¸� ��� ���������� ����մϴ�.
        Undo.RegisterCompleteObjectUndo(_currentEpisodeData, undoName);

        // 2. "���� ��" ���¸� �����ϴ� ������ GraphView�� ����ȭ�� ��(���� ������) �����մϴ�.
        EditorApplication.delayCall += () =>
        {
            // ������ â�� �����ų� �����Ͱ� ���� ��츦 ����� ��� �ڵ�
            if (this == null || _currentEpisodeData == null)
            {
                return;
            }

            // 3. ����ȭ�� '���� ��' ���¸� SO�� �����մϴ�.
            SaveGraphStateToSO();

            // 4. SO�� ��������� ������ Unity�� ���������� �˸��ϴ�. (��ũ �����)
            EditorUtility.SetDirty(_currentEpisodeData);
        };
    }
    // [�߰�] Undo/Redo ���� �� ȭ���� �ٽ� �׸��� �޼���
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

        // [����] graphElements.OfType<NodeView>() ��� Ȯ��
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

        // [����] graphElements.OfType<Edge>() ��� Ȯ��
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
        // SetDirty�� RegisterCompleteObjectUndo�� ���ԵǾ� �����Ƿ� ����
        // EditorUtility.SetDirty(_currentEpisodeData);
    }

    private void SaveToFile()
    {
        string filePath = EditorUtility.SaveFilePanel("Save Episode", "", _currentFilePath, "json");
        if (string.IsNullOrEmpty(filePath)) return;

        _currentFilePath = Path.GetFileName(filePath);
        if (_fileNameLabel != null) _fileNameLabel.text = _currentFilePath;

        SaveGraphStateToSO(); // SaveToFile ȣ�� �� SO���� ����ǵ��� ����

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

        // Null üũ ��ȭ
        if (episodeData == null) episodeData = new EpisodeData();
        if (episodeData.datasets == null) episodeData.datasets = new Datasets();
        if (episodeData.datasets.dialogueLines == null) episodeData.datasets.dialogueLines = new List<DialogueLine>();
        if (episodeData.datasets.episodePortraits == null) episodeData.datasets.episodePortraits = new List<EpisodePortrait>();
        if (episodeData.datasets.locationPresets == null) episodeData.datasets.locationPresets = new List<LocationPreset>();
        if (episodeData.nodeGraph == null) episodeData.nodeGraph = new NodeGraph { nodes = new List<NodeData>(), connections = new List<ConnectionData>() };

        // [��Ÿ ����] "V"��� �����ִ� �߸��� ���� ����

        // Undo ��� ���� SO�� ���� ������Ʈ
        _currentEpisodeData.dialogueLines = episodeData.datasets.dialogueLines;
        _currentEpisodeData.episodePortraits = episodeData.datasets.episodePortraits;
        _currentEpisodeData.locationPresets = episodeData.datasets.locationPresets;
        _currentEpisodeData.nodeGraphJson = JsonConvert.SerializeObject(episodeData.nodeGraph, JsonSettings);
        EditorUtility.SetDirty(_currentEpisodeData); // �ε� �Ŀ��� SetDirty �ʿ�

        _graphView.PopulateView(_currentEpisodeData);

        OnProjectReset?.Invoke(); // �ٸ� ������ â���� ���� �˸�

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

    // ���� New ��ư�� OnNewButtonClicked �Լ� ���� ���� (�� �޼���� CreateAndRestoreGUI �ȿ� �̹� ����)
    // rootVisualElement.Q<Button>("new-button").clicked += OnNewButtonClicked;

    // 'New' ��ư Ŭ�� �� ȣ��Ǵ� �޼���
    private void OnNewButtonClicked()
    {
        bool confirmed = EditorUtility.DisplayDialog("�� ����", "���� �۾� ������ �������� �ʰ� ��� �ʱ�ȭ�Ͻðڽ��ϱ�?", "Ȯ��", "���");
        if (confirmed)
        {
            Undo.RecordObject(_currentEpisodeData, "New Episode"); // Undo ��� �߰�

            _currentEpisodeData.dialogueLines.Clear();
            _currentEpisodeData.episodePortraits.Clear();
            _currentEpisodeData.locationPresets.Clear();
            _currentEpisodeData.nodeGraphJson = "";
            _currentFilePath = "New Episode";
            if (_fileNameLabel != null) _fileNameLabel.text = _currentFilePath;

            // [��Ÿ ����] "Such_as_Two_Points:" ��� �߸��� ���̺� ����
            EditorUtility.SetDirty(_currentEpisodeData); // SetDirty�� RegisterCompleteObjectUndo ���� �ʿ��� �� ����
                                                         // AssetDatabase.SaveAssets(); // ��� ���庸�ٴ� SetDirty ����

            _graphView.PopulateView(_currentEpisodeData);
            OnProjectReset?.Invoke();
            Debug.Log("�� ���Ǽҵ带 �����մϴ�.");
        }
    }
}

