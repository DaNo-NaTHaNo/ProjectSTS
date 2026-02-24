using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class ImageNodeView : NodeView
{
    public ImageNodeView() : base("Image", true, true)
    {
        titleContainer.style.backgroundColor = new Color(200f / 255f, 120f / 255f, 0f / 255f);

        // [수정] 모든 필드를 Undo/Redo 기능이 내장된 'Undoable' 스마트 필드로 교체합니다.
        extensionContainer.Add(new UndoableDropdownField("controlType", new List<string> { "Enter", "Exit" }, 0, "Change Control Type") { name = "controlType" });
        extensionContainer.Add(new UndoableDropdownField("display", new List<string> { "Background", "CutIn", "PopUp" }, 0, "Change Display") { name = "display" });
        extensionContainer.Add(new UndoableTextField("imageName", "Change Image Name") { name = "imageName" });
        extensionContainer.Add(new UndoableColorField("startTint", "Change Start Tint") { name = "startTint", value = Color.clear });
        extensionContainer.Add(new UndoableColorField("endTint", "Change End Tint") { name = "endTint", value = Color.white });
        extensionContainer.Add(new UndoableFloatField("duration", "Change Duration") { name = "duration", value = 1.0f });
        extensionContainer.Add(new UndoableDropdownField("ease", new List<string>(Enum.GetNames(typeof(Ease))), (int)Ease.OutQuad, "Change Ease") { name = "ease" });
        extensionContainer.Add(new UndoableToggle("skippable", "Change Skippable") { name = "skippable" });

        RefreshExpandedState();
        style.minHeight = 30f + (8 * 22f);
        style.maxHeight = 30f + (8 * 22f);
        style.minWidth = 200;
    }

    public override BaseNodeFields SaveData()
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 SaveData 로직은 수정할 필요가 없습니다.
        Enum.TryParse<Ease>(this.Q<DropdownField>("ease").value, out var ease);
        var startTintValue = this.Q<ColorField>("startTint").value;
        var endTintValue = this.Q<ColorField>("endTint").value;
        return new ImageNodeFields
        {
            controlType = this.Q<DropdownField>("controlType").value,
            display = this.Q<DropdownField>("display").value,
            imageName = this.Q<TextField>("imageName").value,
            startTint = new ColorData { r = startTintValue.r, g = startTintValue.g, b = startTintValue.b, a = startTintValue.a },
            endTint = new ColorData { r = endTintValue.r, g = endTintValue.g, b = endTintValue.b, a = endTintValue.a },
            duration = this.Q<FloatField>("duration").value,
            ease = ease,
            skippable = this.Q<Toggle>("skippable").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 LoadData 로직은 수정할 필요가 없습니다.
        var iData = data as ImageNodeFields;
        if (iData != null)
        {
            this.Q<DropdownField>("controlType").SetValueWithoutNotify(iData.controlType);
            this.Q<DropdownField>("display").SetValueWithoutNotify(iData.display);
            this.Q<TextField>("imageName").SetValueWithoutNotify(iData.imageName);
            if (iData.startTint != null)
            {
                var startTintColor = new Color(iData.startTint.r, iData.startTint.g, iData.startTint.b, iData.startTint.a);
                this.Q<ColorField>("startTint").SetValueWithoutNotify(startTintColor);
            }
            if (iData.endTint != null)
            {
                var endTintColor = new Color(iData.endTint.r, iData.endTint.g, iData.endTint.b, iData.endTint.a);
                this.Q<ColorField>("endTint").SetValueWithoutNotify(endTintColor);
            }
            this.Q<FloatField>("duration").SetValueWithoutNotify(iData.duration);
            this.Q<DropdownField>("ease").SetValueWithoutNotify(iData.ease.ToString());
            this.Q<Toggle>("skippable").SetValueWithoutNotify(iData.skippable);
        }
    }
}