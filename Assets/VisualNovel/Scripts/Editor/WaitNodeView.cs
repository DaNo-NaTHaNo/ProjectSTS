using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class WaitNodeView : NodeView
{
    public WaitNodeView() : base("Wait", true, true)
    {
        // [수정] FloatField를 Undo/Redo 기능이 내장된 UndoableFloatField로 교체합니다.
        var durationField = new UndoableFloatField("duration", "Change Wait Duration") { name = "duration" };
        extensionContainer.Add(durationField);

        RefreshExpandedState();
        style.minHeight = 30f + (1 * 30f + 5);
        style.maxHeight = 30f + (1 * 30f + 5);
        style.minWidth = 160;
    }

    public override BaseNodeFields SaveData()
    {
        // 'Undoable' 필드는 기존 필드를 상속하므로 SaveData 로직은 수정할 필요가 없습니다.
        return new WaitNodeFields
        {
            duration = this.Q<FloatField>("duration").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // 'Undoable' 필드는 기존 필드를 상속하므로 LoadData 로직은 수정할 필요가 없습니다.
        var waitData = data as WaitNodeFields;
        if (waitData != null)
        {
            this.Q<FloatField>("duration").SetValueWithoutNotify(waitData.duration);
        }
    }
}