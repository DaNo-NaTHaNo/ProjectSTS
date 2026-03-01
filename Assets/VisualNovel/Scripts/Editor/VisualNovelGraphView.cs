using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using UnityEditor; // EditorPrefs ����� ���� �ʿ�

public class VisualNovelGraphView : GraphView
{
    public Action<string> OnGraphModified;
    private bool _isPopulating = false;

    public VisualNovelGraphView()
    {
        this.StretchToParentSize();
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        graphViewChanged += OnGraphViewChanged;
    }

    public NodeView CreateNode(string nodeName, Vector2 position)
    {
        NodeView nodeView = null;
        switch (nodeName)
        {
            case "Start": nodeView = new StartNodeView(); break;
            case "End": nodeView = new EndNodeView(); break;
            case "Comment": nodeView = new CommentNodeView(); break;
            case "Wait": nodeView = new WaitNodeView(); break;
            case "Text": nodeView = new TextNodeView(); break;
            case "Branch": nodeView = new BranchNodeView(); break;
            case "Portrait Enter": nodeView = new PortraitEnterNodeView(); break;
            case "Portrait Anim": nodeView = new PortraitAnimNodeView(); break;
            case "Portrait Exit": nodeView = new PortraitExitNodeView(); break;
            case "Image": nodeView = new ImageNodeView(); break;
            case "Sound": nodeView = new SoundNodeView(); break;
            case "Command": nodeView = new CommandNodeView(); break;
        }

        if (nodeView != null)
        {
            nodeView.viewDataKey = nodeView.GUID;
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            AddElement(nodeView);
            OnGraphModified?.Invoke("Create Node");
        }

        return nodeView;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        if (!_isPopulating)
        {
            bool hasChanged = (graphViewChange.movedElements != null && graphViewChange.movedElements.Any()) ||
                              (graphViewChange.elementsToRemove != null && graphViewChange.elementsToRemove.Any()) ||
                              (graphViewChange.edgesToCreate != null && graphViewChange.edgesToCreate.Any());

            if (hasChanged)
            {
                OnGraphModified?.Invoke("Graph Change");
            }
        }

        if (graphViewChange.elementsToRemove != null)
        {
            var elementsToRemove = new List<GraphElement>(graphViewChange.elementsToRemove);
            foreach (var element in elementsToRemove)
            {
                if (element is NodeView nodeView)
                {
                    nodeView.titleContainer.Query<Port>().ForEach(port =>
                    {
                        foreach (var connection in port.connections)
                        {
                            if (!graphViewChange.elementsToRemove.Contains(connection))
                                graphViewChange.elementsToRemove.Add(connection);
                        }
                    });
                }
            }
        }

        return graphViewChange;
    }

    // --- 3�ܰ� ����: EditorPrefs�� ����ϴ� PFH Ʈ���� ---
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);

        if (evt.target is NodeView nodeView)
        {
            // [����] End ���� PFH ��󿡼� ���� (Start�� ���)
            if (nodeView.title == "End") return;

            evt.menu.AppendAction("Play from Here", (a) => {
                // 1. EditorPrefs�� ��û ���
                EditorPrefs.SetBool("PFH_Requested", true);
                EditorPrefs.SetString("PFH_TargetID", nodeView.GUID);

                // 2. �÷��� ��� ����
                if (!Application.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                }
            }, DropdownMenuAction.Status.Normal);
        }
    }
    // ---

    public void PopulateView(VisualNovelSO episodeData)
    {
        _isPopulating = true;
        try
        {
            DeleteElements(graphElements.ToList());
            if (string.IsNullOrEmpty(episodeData.nodeGraphJson)) return;

            var nodeGraph = JsonConvert.DeserializeObject<NodeGraph>(episodeData.nodeGraphJson);
            if (nodeGraph == null || nodeGraph.nodes == null) return;

            var nodeViewMap = new Dictionary<string, NodeView>();

            nodeGraph.nodes.ForEach(nodeData =>
            {
                var position = new Vector2(nodeData.position.x, nodeData.position.y);
                var nodeView = CreateNode(nodeData.type, position);
                if (nodeView == null) return;
                nodeViewMap[nodeData.id] = nodeView;

                if (!string.IsNullOrEmpty(nodeData.fields) && nodeData.fields != "{}")
                {
                    try
                    {
                        Type fieldType = GetFieldType(nodeData.type);
                        if (fieldType != null)
                        {
                            var fieldData = (BaseNodeFields)JsonConvert.DeserializeObject(nodeData.fields, fieldType);
                            if (fieldData != null) nodeView.LoadData(fieldData);
                        }
                    }
                    catch (Exception e) { Debug.LogError($"Failed to load field data for node {nodeData.id}: {e.Message}"); }
                }
            });

            nodeGraph.connections.ForEach(connectionData =>
            {
                if (!nodeViewMap.ContainsKey(connectionData.sourceNodeId) || !nodeViewMap.ContainsKey(connectionData.targetNodeId)) return;
                var sourceNode = nodeViewMap[connectionData.sourceNodeId];
                var targetNode = nodeViewMap[connectionData.targetNodeId];
                var outputPorts = sourceNode.Query<Port>().Where(p => p.direction == Direction.Output).ToList();
                var targetPort = targetNode.Query<Port>().Where(p => p.direction == Direction.Input).ToList().FirstOrDefault();
                if (targetPort != null && outputPorts.Count > connectionData.sourceOutputIndex)
                {
                    var sourcePort = outputPorts[connectionData.sourceOutputIndex];
                    var edge = sourcePort.ConnectTo(targetPort);
                    AddElement(edge);
                }
            });
        }
        finally
        {
            _isPopulating = false;
        }
    }

    public Type GetFieldType(string nodeType)
    {
        switch (nodeType)
        {
            case "Text": return typeof(TextNodeFields);
            case "Branch": return typeof(BranchNodeFields);
            case "Comment": return typeof(CommentNodeFields);
            case "Wait": return typeof(WaitNodeFields);
            case "Portrait Enter": return typeof(PortraitEnterNodeFields);
            case "Portrait Anim": return typeof(PortraitAnimNodeFields);
            case "Portrait Exit": return typeof(PortraitExitNodeFields);
            case "Image": return typeof(ImageNodeFields);
            case "Sound": return typeof(SoundNodeFields);
            case "Command": return typeof(CommandNodeFields);
            default: return null;
        }
    }
}