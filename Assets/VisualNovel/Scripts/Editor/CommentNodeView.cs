using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class CommentNodeView : NodeView
{
    public CommentNodeView() : base("Comment", true, true)
    {
        titleContainer.style.backgroundColor = new Color(139f / 255f, 0f / 255f, 0f / 255f);

        // [수정] TextField를 Undo/Redo 기능이 내장된 UndoableTextField로 교체합니다.
        // "Change Comment"는 Undo 스택에 표시될 작업 이름입니다.
        var commentField = new UndoableTextField("Comment", "Change Comment") { name = "Comment", multiline = true };
        extensionContainer.Add(commentField);

        RefreshExpandedState();
        style.minHeight = 30f + (1 * 30f + 5);
        style.maxHeight = 30f + (1 * 30f + 5);
        style.minWidth = 250;
    }

    public override BaseNodeFields SaveData()
    {
        return new CommentNodeFields
        {
            comment = this.Q<TextField>("Comment").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        var commentData = data as CommentNodeFields;
        if (commentData != null)
        {
            this.Q<TextField>("Comment").SetValueWithoutNotify(commentData.comment);
        }
    }
}