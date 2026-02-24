using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

public class PortraitAnimNodeView : NodeView
{
    public PortraitAnimNodeView() : base("Portrait Anim", true, true)
    {
        titleContainer.style.backgroundColor = new Color(15f / 255f, 15f / 255f, 115f / 255f);

        // [수정] 모든 필드를 Undo/Redo 기능이 내장된 'Undoable' 스마트 필드로 교체합니다.
        extensionContainer.Add(new UndoableTextField("portraitName", "Change portraitName") { name = "portraitName" });
        extensionContainer.Add(new UndoableDropdownField("location", new List<string> { "leftMost", "left", "center", "right", "rightMost" }, 2, "Change location") { name = "location" });

        var offsetField = new UndoableVector2Field("offset", "Change offset") { name = "offset" };
        offsetField.AddToClassList("custom-vector2-field");
        extensionContainer.Add(offsetField);

        extensionContainer.Add(new UndoableDropdownField("face", new List<string> { "Default", "Smile", "Laugh", "Angry", "Fury", "Sad", "Cry", "Think", "Surprised", "ClosedEyes" }, 0, "Change face") { name = "face" });
        extensionContainer.Add(new UndoableDropdownField("animKey", new List<string> { "None", "Shake", "Jump" }, 0, "Change animKey") { name = "animKey" });
        extensionContainer.Add(new UndoableColorField("endTint", "Change endTint") { name = "endTint", value = Color.white });
        extensionContainer.Add(new UndoableFloatField("duration", "Change duration") { name = "duration", value = 1.0f });
        extensionContainer.Add(new UndoableDropdownField("ease", new List<string>(Enum.GetNames(typeof(Ease))), (int)Ease.OutQuad, "Change ease") { name = "ease" });
        extensionContainer.Add(new UndoableToggle("spotLight", "Change spotLight") { name = "spotLight" });
        extensionContainer.Add(new UndoableToggle("skippable", "Change skippable") { name = "skippable" });

        RefreshExpandedState();
        style.minHeight = 30f + (10 * 22f);
        style.maxHeight = 30f + (10 * 22f);
        style.minWidth = 240;
    }

    public override BaseNodeFields SaveData()
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 SaveData 로직은 수정할 필요가 없습니다.
        Enum.TryParse<Ease>(this.Q<DropdownField>("ease").value, out var ease);
        var offsetValue = this.Q<Vector2Field>("offset").value;
        var endTintValue = this.Q<ColorField>("endTint").value;
        return new PortraitAnimNodeFields
        {
            portraitName = this.Q<TextField>("portraitName").value,
            location = this.Q<DropdownField>("location").value,
            offset = new PositionData { x = offsetValue.x, y = offsetValue.y },
            face = this.Q<DropdownField>("face").value,
            animKey = this.Q<DropdownField>("animKey").value,
            endTint = new ColorData { r = endTintValue.r, g = endTintValue.g, b = endTintValue.b, a = endTintValue.a },
            duration = this.Q<FloatField>("duration").value,
            ease = ease,
            spotLight = this.Q<Toggle>("spotLight").value,
            skippable = this.Q<Toggle>("skippable").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 LoadData 로직은 수정할 필요가 없습니다.
        var pData = data as PortraitAnimNodeFields;

        if (pData != null)
        {
            this.Q<TextField>("portraitName").SetValueWithoutNotify(pData.portraitName);
            this.Q<DropdownField>("location").SetValueWithoutNotify(pData.location);
            if (pData.offset != null)
            {
                var offsetVector = new Vector2(pData.offset.x, pData.offset.y);
                this.Q<Vector2Field>("offset").SetValueWithoutNotify(offsetVector);
            }
            this.Q<DropdownField>("face").SetValueWithoutNotify(pData.face);
            this.Q<DropdownField>("animKey").SetValueWithoutNotify(pData.animKey);
            if (pData.endTint != null)
            {
                var endTintColor = new Color(pData.endTint.r, pData.endTint.g, pData.endTint.b, pData.endTint.a);
                this.Q<ColorField>("endTint").SetValueWithoutNotify(endTintColor);
            }
            this.Q<FloatField>("duration").SetValueWithoutNotify(pData.duration);
            this.Q<DropdownField>("ease").SetValueWithoutNotify(pData.ease.ToString());
            this.Q<Toggle>("spotLight").SetValueWithoutNotify(pData.spotLight);
            this.Q<Toggle>("skippable").SetValueWithoutNotify(pData.skippable);
        }
    }
}