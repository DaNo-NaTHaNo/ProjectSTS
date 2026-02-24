using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine;

public class BranchNodeView : NodeView
{
    public BranchNodeView() : base("Branch", true, false)
    {
        titleContainer.style.backgroundColor = new Color(15f / 255f, 100f / 255f, 20f / 255f);
        mainContainer.style.flexDirection = FlexDirection.Column;

        // [수정] TextField를 Undo/Redo 기능이 내장된 UndoableTextField로 교체합니다.
        var sceneNameField = new UndoableTextField("sceneName", "Change sceneName") { name = "sceneName" };
        extensionContainer.Add(sceneNameField); // extensionContainer 사용

        inputContainer.style.display = DisplayStyle.Flex;
        outputContainer.style.display = DisplayStyle.Flex;

        for (int i = 0; i < 4; i++)
        {
            var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            outputPort.portName = $"Branch {i + 1}";
            outputContainer.Add(outputPort);
        }

        RefreshExpandedState();
        style.minHeight = 170;
        style.maxHeight = 170;
        style.minWidth = 200;
        style.maxWidth = 200;
    }

    public override BaseNodeFields SaveData()
    {
        // UndoableTextField는 TextField를 상속하므로 이 코드는 수정할 필요가 없습니다.
        return new BranchNodeFields
        {
            sceneName = this.Q<TextField>("sceneName").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // UndoableTextField는 TextField를 상속하므로 이 코드는 수정할 필요가 없습니다.
        var branchData = data as BranchNodeFields;
        if (branchData != null)
        {
            this.Q<TextField>("sceneName").SetValueWithoutNotify(branchData.sceneName);
        }
    }
}