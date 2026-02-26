using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class DialogueLineEditor : EditorWindow
{
    private VisualNovelSO _targetData;
    private ListView _listView;
    private VisualElement _dataView;
    private VisualElement _headerView;

    public static void OpenWindow(VisualNovelSO data)
    {
        var window = GetWindow<DialogueLineEditor>("DialogueLine");
        window.minSize = new Vector2(1860, 240);
        window._targetData = data;

        if (window._listView != null && window._targetData != null)
        {
            window._listView.itemsSource = window._targetData.dialogueLines;
            window._listView.Rebuild();
            window.ToggleViews(window._targetData.dialogueLines.Count > 0);
        }
    }

    private void OnEnable()
    {
        Undo.undoRedoPerformed += OnUndoRedo;
        VisualNovelEditorWindow.OnProjectReset += RefreshListView;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        VisualNovelEditorWindow.OnProjectReset -= RefreshListView;
    }

    private void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/VisualNovel/Scripts/Editor/DialogueLineEditor.uxml");
        visualTree.CloneTree(rootVisualElement);
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/VisualNovel/Scripts/Editor/DialogueLineEditor.uss");
        rootVisualElement.styleSheets.Add(styleSheet);

        _dataView = rootVisualElement.Q<VisualElement>("data-view");

        SetupHeader();
        SetupListView();

        var newButton = rootVisualElement.Q<Button>("new-button");
        newButton.clicked += OnNewButtonClicked;
        var importButton = rootVisualElement.Q<Button>("import-button");
        importButton.clicked += OnImportButtonClicked;
        var exportButton = rootVisualElement.Q<Button>("export-button");
        exportButton.clicked += OnExportButtonClicked;
        var addButton = rootVisualElement.Q<Button>("add-row-button");
        addButton.clicked += OnAddRowButtonClicked;
        var removeButton = rootVisualElement.Q<Button>("remove-row-button");
        removeButton.clicked += OnRemoveRowButtonClicked;

        ToggleViews(_targetData != null && _targetData.dialogueLines.Any());
    }

    private void SetupHeader()
    {
        _headerView = new VisualElement();
        _headerView.AddToClassList("row");

        AddHeaderLabel("No.", "header-no");
        AddHeaderLabel("sceneName", "header-sceneName");
        AddHeaderLabel("locationPreset", "header-locationPreset");
        AddHeaderLabel("speakerPortrait", "header-speakerPortrait");
        AddHeaderLabel("speakerName", "header-speakerName");
        AddHeaderLabel("speakerGroup", "header-speakerGroup");
        AddHeaderLabel("speakerText", "header-speakerText");
        AddHeaderLabel("face", "header-face");
        AddHeaderLabel("animKey", "header-animKey");
        AddHeaderLabel("emote", "header-emote");
        AddHeaderLabel("sfx", "header-sfx");
        AddHeaderLabel("spotLight", "header-spotLight");
        AddHeaderLabel("textSpeed", "header-textSpeed");
        AddHeaderLabel("skippable", "header-skippable");
        AddHeaderLabel("autoNext", "header-autoNext");
        AddHeaderLabel("voice", "header-voice");
        AddHeaderLabel("wait", "header-wait");

        foreach (var label in _headerView.Query<Label>().ToList())
        {
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.AddToClassList("col");
        }
    }

    private void AddHeaderLabel(string text, string className)
    {
        var label = new Label(text);
        label.AddToClassList(className);
        _headerView.Add(label);
    }

    private void SetupListView()
    {
        _listView = new ListView();
        _listView.style.flexGrow = 1;
        _listView.selectionType = SelectionType.Single;

        _listView.schedule.Execute(() =>
        {
            var internalScrollView = _listView.Q<ScrollView>();
            if (internalScrollView != null)
            {
                internalScrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
                internalScrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            }
        });

        if (_targetData != null)
        {
            _listView.itemsSource = _targetData.dialogueLines;
        }

        _listView.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            AddControlToRow(row, new Label(), "no", "col-no");
            AddControlToRow(row, new TextField(), "sceneName", "col-sceneName");
            AddControlToRow(row, new TextField(), "locationPreset", "col-locationPreset");
            AddControlToRow(row, new TextField(), "speakerPortrait", "col-speakerPortrait");
            AddControlToRow(row, new TextField(), "speakerName", "col-speakerName");
            AddControlToRow(row, new TextField(), "speakerGroup", "col-speakerGroup");
            AddControlToRow(row, new TextField(), "speakerText", "col-speakerText");
            AddControlToRow(row, new TextField(), "face", "col-face");
            AddControlToRow(row, new TextField(), "animKey", "col-animKey");
            AddControlToRow(row, new TextField(), "emote", "col-emote");
            AddControlToRow(row, new TextField(), "sfx", "col-sfx");
            AddControlToRow(row, new Toggle(), "spotLight", "col-spotLight");
            AddControlToRow(row, new FloatField(), "textSpeed", "col-textSpeed");
            AddControlToRow(row, new Toggle(), "skippable", "col-skippable");
            AddControlToRow(row, new Toggle(), "autoNext", "col-autoNext");
            AddControlToRow(row, new TextField(), "voice", "col-voice");
            AddControlToRow(row, new FloatField(), "wait", "col-wait");
            return row;
        };

        _listView.bindItem = (element, index) =>
        {
            var data = _targetData.dialogueLines[index];
            element.Q<Label>("no").text = (index + 1).ToString();
            Bind(element.Q<TextField>("sceneName"), data, d => d.sceneName, (d, v) => d.sceneName = v);
            Bind(element.Q<TextField>("locationPreset"), data, d => d.locationPreset, (d, v) => d.locationPreset = v);
            Bind(element.Q<TextField>("speakerPortrait"), data, d => d.speakerPortrait, (d, v) => d.speakerPortrait = v);
            Bind(element.Q<TextField>("speakerName"), data, d => d.speakerName, (d, v) => d.speakerName = v);
            Bind(element.Q<TextField>("speakerGroup"), data, d => d.speakerGroup, (d, v) => d.speakerGroup = v);
            Bind(element.Q<TextField>("speakerText"), data, d => d.speakerText, (d, v) => d.speakerText = v);
            Bind(element.Q<TextField>("face"), data, d => d.face, (d, v) => d.face = v);
            Bind(element.Q<TextField>("animKey"), data, d => d.animKey, (d, v) => d.animKey = v);
            Bind(element.Q<TextField>("emote"), data, d => d.emote, (d, v) => d.emote = v);
            Bind(element.Q<TextField>("sfx"), data, d => d.sfx, (d, v) => d.sfx = v);
            Bind(element.Q<TextField>("voice"), data, d => d.voice, (d, v) => d.voice = v);
            Bind(element.Q<Toggle>("spotLight"), data, d => d.spotLight, (d, v) => d.spotLight = v);
            Bind(element.Q<Toggle>("skippable"), data, d => d.skippable, (d, v) => d.skippable = v);
            Bind(element.Q<Toggle>("autoNext"), data, d => d.autoNext, (d, v) => d.autoNext = v);
            Bind(element.Q<FloatField>("textSpeed"), data, d => d.textSpeed, (d, v) => d.textSpeed = v);
            Bind(element.Q<FloatField>("wait"), data, d => d.wait, (d, v) => d.wait = v);
        };

        _dataView.Clear();
        _dataView.Add(_headerView);
        _dataView.Add(_listView);
    }

    private void AddControlToRow(VisualElement row, VisualElement control, string name, string className)
    {
        control.name = name;
        control.AddToClassList(className);
        control.AddToClassList("col");
        row.Add(control);
    }

    private void Bind<TValue>(BaseField<TValue> field, DialogueLine data, System.Func<DialogueLine, TValue> getter, System.Action<DialogueLine, TValue> setter)
    {
        field.SetValueWithoutNotify(getter(data));
        if (field.userData is EventCallback<ChangeEvent<TValue>> oldCallback)
        {
            field.UnregisterValueChangedCallback(oldCallback);
        }
        EventCallback<ChangeEvent<TValue>> newCallback = evt =>
        {
            // [����] �ʵ� �� ���� �� Undo/Redo�� ����մϴ�.
            Undo.RegisterCompleteObjectUndo(_targetData, "Change Dialogue Field");
            setter(data, evt.newValue);
            EditorUtility.SetDirty(_targetData); // SO�� ����Ǿ����� �˸��ϴ�.
        };
        field.RegisterValueChangedCallback(newCallback);
        field.userData = newCallback;
    }

    private void OnNewButtonClicked()
    {
        if (_targetData.dialogueLines.Any())
        {
            int option = EditorUtility.DisplayDialogComplex(
                "���ο� DialogueLine", "���� �����͸� ����� ���� �����Ͻðڽ��ϱ�?",
                "Ȯ��", "���", "Export �� �ʱ�ȭ"
            );
            switch (option)
            {
                case 0: CreateNewSheet(); break;
                case 1: break;
                case 2:
                    OnExportButtonClicked();
                    CreateNewSheet();
                    break;
            }
        }
        else
        {
            CreateNewSheet();
        }
    }

    private void OnExportButtonClicked()
    {
        if (_targetData == null || !_targetData.dialogueLines.Any())
        {
            EditorUtility.DisplayDialog("Export ����", "������ �����Ͱ� �����ϴ�.", "Ȯ��");
            return;
        }
        string path = EditorUtility.SaveFilePanel("Export DialogueLine CSV", "", "DialogueLine.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            string csvText = CsvUtilityVN.GenerateDialogueLines(_targetData.dialogueLines);
            File.WriteAllText(path, csvText, System.Text.Encoding.UTF8);
            Debug.Log($"DialogueLine �����Ͱ� ���������� Export �Ǿ����ϴ�: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV Export �� ���� �߻�: {e.Message}");
        }
    }

    private void CreateNewSheet()
    {
        // [����] Undo.RegisterCompleteObjectUndo�� �����Ͽ� ��ü ���¸� ���
        Undo.RegisterCompleteObjectUndo(_targetData, "Create New DialogueLine Sheet");
        _targetData.dialogueLines.Clear();
        _targetData.dialogueLines.Add(new DialogueLine());
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
    }

    private void RefreshListView()
    {
        if (_listView != null && _targetData != null)
        {
            _listView.itemsSource = _targetData.dialogueLines;
            _listView.Rebuild();
            ToggleViews(_targetData.dialogueLines.Any());
        }
    }

    private void OnImportButtonClicked()
    {
        string path = EditorUtility.OpenFilePanel("Import DialogueLine CSV", "", "csv");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        // [����] Undo.RegisterCompleteObjectUndo�� ����
        Undo.RegisterCompleteObjectUndo(_targetData, "Import CSV");
        string csvText = File.ReadAllText(path);
        _targetData.dialogueLines = CsvUtilityVN.ParseDialogueLines(csvText);
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
    }

    private void OnAddRowButtonClicked()
    {
        // [����] Undo.RegisterCompleteObjectUndo�� ����
        Undo.RegisterCompleteObjectUndo(_targetData, "Add Row");
        var newEntry = new DialogueLine();
        int insertIndex = _targetData.dialogueLines.Count;
        if (_listView.selectedItem != null)
        {
            insertIndex = _listView.selectedIndex + 1;
        }
        _targetData.dialogueLines.Insert(insertIndex, newEntry);
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
        _listView.selectedIndex = insertIndex;
        _listView.ScrollToItem(insertIndex);
    }

    private void OnRemoveRowButtonClicked()
    {
        if (_listView.selectedItem == null)
        {
            return;
        }
        var selectedData = _listView.selectedItem as DialogueLine;
        // [����] Undo.RegisterCompleteObjectUndo�� ����
        Undo.RegisterCompleteObjectUndo(_targetData, "Remove Row");
        _targetData.dialogueLines.Remove(selectedData);
        EditorUtility.SetDirty(_targetData);
        _listView.ClearSelection();
        RefreshListView();
    }

    private void OnUndoRedo()
    {
        // OnEnable�� �̹� ��ϵǾ� �����Ƿ�, �� �޼��尡 Undo/Redo �� ȣ��˴ϴ�.
        RefreshListView();
    }

    private void ToggleViews(bool hasData)
    {
        var emptyView = rootVisualElement.Q<VisualElement>("empty-view");
        _dataView.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
        emptyView.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
    }
}