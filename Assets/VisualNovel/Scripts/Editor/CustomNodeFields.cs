using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;

// 이 파일은 Undo/Redo 신호 기능이 내장된 커스텀 UI 필드들을 정의합니다.

public class UndoableTextField : TextField
{
    public UndoableTextField(string label, string undoName = "Change Text") : base(label)
    {
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}

public class UndoableDropdownField : DropdownField
{
    public UndoableDropdownField(string label, List<string> choices, int defaultIndex, string undoName = "Change Dropdown") : base(label, choices, defaultIndex)
    {
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}

public class UndoableVector2Field : Vector2Field
{
    public UndoableVector2Field(string label, string undoName = "Change Vector2") : base(label)
    {
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}

public class UndoableColorField : ColorField
{
    public UndoableColorField(string label, string undoName = "Change Color") : base(label)
    {
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}

public class UndoableFloatField : FloatField
{
    public UndoableFloatField(string label, string undoName = "Change Float") : base(label)
    {
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}

public class UndoableToggle : Toggle
{
    public UndoableToggle(string label, string undoName = "Change Toggle") : base(label)
    { 
        this.RegisterValueChangedCallback(evt => VisualNovelEditorWindow.TriggerNodeModified(undoName));
    }
}