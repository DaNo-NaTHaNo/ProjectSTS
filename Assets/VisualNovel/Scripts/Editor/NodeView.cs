using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System;

// 이제 이 클래스는 직접 사용할 수 없는 '설계도' 역할만 합니다.
public abstract class NodeView : Node
{
    public string GUID;

    public NodeView(string nodeName, bool hasInputPort = true, bool hasOutputPort = true)
    {
        title = nodeName;
        GUID = Guid.NewGuid().ToString();

        AddToClassList("node-view");
        capabilities |= Capabilities.Resizable;

        // 포트 생성 로직
        if (hasInputPort)
        {
            var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "";
            titleContainer.Insert(0, inputPort);
        }

        if (hasOutputPort)
        {
            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = "";
            titleContainer.Add(outputPort);
        }
    }

    // ▼▼▼ 두 개의 추상 메서드 추가 ▼▼▼
    // 이 클래스를 상속받는 모든 자식은 반드시 이 두 메서드를 구현해야 합니다.

    /// <summary>
    /// 현재 노드의 UI 필드 상태를 데이터 객체로 변환하여 반환합니다.
    /// </summary>
    public abstract BaseNodeFields SaveData();

    /// <summary>
    /// 데이터 객체로부터 값을 읽어와 노드의 UI 필드를 채웁니다.
    /// </summary>
    public abstract void LoadData(BaseNodeFields data);
}