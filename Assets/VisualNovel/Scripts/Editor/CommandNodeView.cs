using UnityEngine.UIElements;
using UnityEngine;

public class CommandNodeView : NodeView
{
    public CommandNodeView() : base("Command", true, true)
    {
        titleContainer.style.backgroundColor = new Color(128f / 255f, 0f / 255f, 128f / 255f);

        var commandKeyField = new UndoableTextField("commandKey", "Change commandKey") { name = "commandKey" };
        extensionContainer.Add(commandKeyField);

        var commandValueField = new UndoableTextField("commandValue", "Change commandValue") { name = "commandValue" };
        extensionContainer.Add(commandValueField);

        RefreshExpandedState();
        style.minHeight = 30f + (2 * 25f + 5);
        style.maxHeight = 30f + (2 * 25f + 5);
        style.minWidth = 220;
        style.maxWidth = 220;
    }

    public override BaseNodeFields SaveData()
    {
        return new CommandNodeFields
        {
            commandKey = this.Q<TextField>("commandKey").value,
            commandValue = this.Q<TextField>("commandValue").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        var cmdData = data as CommandNodeFields;
        if (cmdData != null)
        {
            this.Q<TextField>("commandKey").SetValueWithoutNotify(cmdData.commandKey);
            this.Q<TextField>("commandValue").SetValueWithoutNotify(cmdData.commandValue);
        }
    }
}
