using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;

public class SoundNodeView : NodeView
{
    public SoundNodeView() : base("Sound", true, true)
    {
        titleContainer.style.backgroundColor = new Color(255f / 255f, 165f / 255f, 0f / 255f);

        // [수정] 모든 필드를 Undo/Redo 기능이 내장된 'Undoable' 스마트 필드로 교체합니다.
        extensionContainer.Add(new UndoableDropdownField("controlType", new List<string> { "Play", "Stop" }, 0, "Change Control Type") { name = "controlType" });
        extensionContainer.Add(new UndoableDropdownField("display", new List<string> { "BGM", "SFX" }, 0, "Change Display") { name = "display" });
        extensionContainer.Add(new UndoableTextField("soundName", "Change Sound Name") { name = "soundName" });
        extensionContainer.Add(new UndoableFloatField("fadeDuration", "Change Fade Duration") { name = "fadeDuration" });
        extensionContainer.Add(new UndoableFloatField("volume", "Change Volume") { name = "volume", value = 1.0f });
        extensionContainer.Add(new UndoableToggle("isLoop", "Change Loop") { name = "isLoop" });
        extensionContainer.Add(new UndoableToggle("skippable", "Change Skippable") { name = "skippable" });

        RefreshExpandedState();
        style.minHeight = 30f + (7 * 22f);
        style.maxHeight = 30f + (7 * 22f);
        style.minWidth = 200;
    }

    public override BaseNodeFields SaveData()
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 SaveData 로직은 수정할 필요가 없습니다.
        return new SoundNodeFields
        {
            controlType = this.Q<DropdownField>("controlType").value,
            display = this.Q<DropdownField>("display").value,
            soundName = this.Q<TextField>("soundName").value,
            fadeDuration = this.Q<FloatField>("fadeDuration").value,
            volume = this.Q<FloatField>("volume").value,
            isLoop = this.Q<Toggle>("isLoop").value,
            skippable = this.Q<Toggle>("skippable").value
        };
    }

    public override void LoadData(BaseNodeFields data)
    {
        // 'Undoable' 필드들은 기존 필드를 상속하므로 LoadData 로직은 수정할 필요가 없습니다.
        var sData = data as SoundNodeFields;
        if (sData != null)
        {
            this.Q<DropdownField>("controlType").SetValueWithoutNotify(sData.controlType);
            this.Q<DropdownField>("display").SetValueWithoutNotify(sData.display);
            this.Q<TextField>("soundName").SetValueWithoutNotify(sData.soundName);
            this.Q<FloatField>("fadeDuration").SetValueWithoutNotify(sData.fadeDuration);
            this.Q<FloatField>("volume").SetValueWithoutNotify(sData.volume);
            this.Q<Toggle>("isLoop").SetValueWithoutNotify(sData.isLoop);
            this.Q<Toggle>("skippable").SetValueWithoutNotify(sData.skippable);
        }
    }
}