using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

public class TextNodeView : NodeView
{
    public TextNodeView() : base("Text", true, true)
    {
        titleContainer.style.backgroundColor = new Color(34f / 255f, 139f / 255f, 34f / 255f);

        // [수정] TextField를 UndoableTextField로 교체
        var sceneNameField = new UndoableTextField("sceneName", "Change sceneName") { name = "sceneName" };
        extensionContainer.Add(sceneNameField);

        var displayChoices = new List<string> { "Bottom", "Monologue" };
        // [수정] DropdownField를 UndoableDropdownField로 교체
        var displayDropdown = new UndoableDropdownField("display", displayChoices, 0, "Change display") { name = "display" };
        extensionContainer.Add(displayDropdown);

        RefreshExpandedState();
        style.minHeight = 30f + (2 * 25f + 5);
        style.maxHeight = 30f + (2 * 25f + 5);
        style.minWidth = 200;
    }

    public override BaseNodeFields SaveData()
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 SaveData 로직은 수정할 필요가 없습니다.
        return new TextNodeFields
        {
            sceneName = this.Q<TextField>("sceneName").value,
            display = this.Q<DropdownField>("display").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 LoadData 로직은 수정할 필요가 없습니다.
        var textData = data as TextNodeFields;
        if (textData != null)
        {
            this.Q<TextField>("sceneName").SetValueWithoutNotify(textData.sceneName);
            this.Q<DropdownField>("display").SetValueWithoutNotify(textData.display);
        }
    }
}