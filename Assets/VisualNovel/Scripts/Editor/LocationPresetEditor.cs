using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using DG.Tweening;

public class LocationPresetEditor : EditorWindow
{
    private VisualNovelSO _targetData;
    private ListView _listView;
    private VisualElement _dataView;
    private VisualElement _headerView;

    public static void OpenWindow(VisualNovelSO data)
    {
        var window = GetWindow<LocationPresetEditor>("LocationPreset");
        window.minSize = new Vector2(800, 160);
        window._targetData = data;
        window.RefreshListView();
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

        RefreshListView();
    }

    private void OnNewButtonClicked()
    {
        if (_targetData.locationPresets.Any())
        {
            int option = EditorUtility.DisplayDialogComplex("���ο� LocationPreset", "���� �����͸� ����� ���� �����Ͻðڽ��ϱ�?", "Ȯ��", "���", "Export �� �ʱ�ȭ");
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
        if (_targetData == null || !_targetData.locationPresets.Any())
        {
            EditorUtility.DisplayDialog("Export ����", "������ �����Ͱ� �����ϴ�.", "Ȯ��");
            return;
        }
        string path = EditorUtility.SaveFilePanel("Export LocationPreset CSV", "", "LocationPreset.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            string csvText = CsvUtilityVN.GenerateLocationPresets(_targetData.locationPresets);
            File.WriteAllText(path, csvText, System.Text.Encoding.UTF8);
            Debug.Log($"LocationPreset �����Ͱ� ���������� Export �Ǿ����ϴ�: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV Export �� ���� �߻�: {e.Message}");
        }
    }

    private void CreateNewSheet()
    {
        // [����] Undo.RegisterCompleteObjectUndo�� ����Ͽ� SO ��ü ���¸� ���
        Undo.RegisterCompleteObjectUndo(_targetData, "Create New LocationPreset Sheet");
        _targetData.locationPresets.Clear();
        _targetData.locationPresets.Add(new LocationPreset());
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
    }

    private void OnImportButtonClicked()
    {
        string path = EditorUtility.OpenFilePanel("Import LocationPreset CSV", "", "csv");
        if (!string.IsNullOrEmpty(path))
        {
            // [����] Undo.RegisterCompleteObjectUndo�� ����Ͽ� SO ��ü ���¸� ���
            Undo.RegisterCompleteObjectUndo(_targetData, "Import LocationPreset CSV");
            string csvText = File.ReadAllText(path);
            _targetData.locationPresets = CsvUtilityVN.ParseLocationPresets(csvText);
            EditorUtility.SetDirty(_targetData);
            RefreshListView();
        }
    }

    private void OnAddRowButtonClicked()
    {
        // [����] Undo.RegisterCompleteObjectUndo�� ����Ͽ� SO ��ü ���¸� ���
        Undo.RegisterCompleteObjectUndo(_targetData, "Add Preset Row");
        var newEntry = new LocationPreset();
        int insertIndex = _targetData.locationPresets.Count;
        if (_listView.selectedItem != null) { insertIndex = _listView.selectedIndex + 1; }
        _targetData.locationPresets.Insert(insertIndex, newEntry);
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
        _listView.selectedIndex = insertIndex;
        _listView.ScrollToItem(insertIndex);
    }

    private void OnRemoveRowButtonClicked()
    {
        if (_listView.selectedItem == null) return;
        var selectedData = _listView.selectedItem as LocationPreset;
        // [����] Undo.RegisterCompleteObjectUndo�� ����Ͽ� SO ��ü ���¸� ���
        Undo.RegisterCompleteObjectUndo(_targetData, "Remove Preset Row");
        _targetData.locationPresets.Remove(selectedData);
        EditorUtility.SetDirty(_targetData);
        _listView.ClearSelection();
        RefreshListView();
    }

    private void OnUndoRedo() { RefreshListView(); }

    private void RefreshListView()
    {
        if (_listView != null && _targetData != null)
        {
            _listView.itemsSource = _targetData.locationPresets;
            _listView.Rebuild();
            ToggleViews(_targetData.locationPresets.Any());
        }
    }

    private void SetupHeader()
    {
        _headerView = new VisualElement();
        _headerView.AddToClassList("row");
        AddHeaderLabel("No.", "header-no");
        AddHeaderLabel("locationPreset", "col-sceneName");
        AddHeaderLabel("leftMost", "header-leftMost");
        AddHeaderLabel("left", "header-left");
        AddHeaderLabel("center", "header-center");
        AddHeaderLabel("right", "header-right");
        AddHeaderLabel("rightMost", "header-rightMost");
        AddHeaderLabel("ease", "header-105px");
        AddHeaderLabel("duration", "header-wait");

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
        if (_targetData != null) _listView.itemsSource = _targetData.locationPresets;

        _listView.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            AddControlToRow(row, new Label(), "no", "col-no");
            AddControlToRow(row, new TextField(), "locationPreset", "col-locationPreset");
            AddControlToRow(row, new TextField(), "leftMost", "col-leftMost");
            AddControlToRow(row, new TextField(), "left", "col-left");
            AddControlToRow(row, new TextField(), "center", "col-center");
            AddControlToRow(row, new TextField(), "right", "col-right");
            AddControlToRow(row, new TextField(), "rightMost", "col-rightMost");
            AddControlToRow(row, new DropdownField(new List<string>(Enum.GetNames(typeof(Ease))), (int)Ease.OutQuad), "ease", "col-animKey");
            AddControlToRow(row, new FloatField(), "duration", "col-wait");
            return row;
        };

        _listView.bindItem = (element, index) =>
        {
            var data = _targetData.locationPresets[index];
            element.Q<Label>("no").text = (index + 1).ToString();
            Bind(element.Q<TextField>("locationPreset"), data, d => d.locationPreset, (d, v) => d.locationPreset = v);
            Bind(element.Q<TextField>("leftMost"), data, d => d.leftMost, (d, v) => d.leftMost = v);
            Bind(element.Q<TextField>("left"), data, d => d.left, (d, v) => d.left = v);
            Bind(element.Q<TextField>("center"), data, d => d.center, (d, v) => d.center = v);
            Bind(element.Q<TextField>("right"), data, d => d.right, (d, v) => d.right = v);
            Bind(element.Q<TextField>("rightMost"), data, d => d.rightMost, (d, v) => d.rightMost = v);
            Bind(element.Q<DropdownField>("ease"), data, d => d.ease, (d, v) => d.ease = v);
            Bind(element.Q<FloatField>("duration"), data, d => d.duration, (d, v) => d.duration = v);
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

    private void Bind<TValue>(BaseField<TValue> field, LocationPreset data, Func<LocationPreset, TValue> getter, Action<LocationPreset, TValue> setter)
    {
        field.SetValueWithoutNotify(getter(data));
        if (field.userData is EventCallback<ChangeEvent<TValue>> oldCallback)
        {
            field.UnregisterValueChangedCallback(oldCallback);
        }
        EventCallback<ChangeEvent<TValue>> newCallback = evt =>
        {
            // [����] �ʵ� �� ���� �� Undo/Redo�� ����մϴ�.
            Undo.RegisterCompleteObjectUndo(_targetData, "Change Preset Field");
            setter(data, evt.newValue);
            EditorUtility.SetDirty(_targetData); // SO�� ����Ǿ����� �˸��ϴ�.
        };
        field.RegisterValueChangedCallback(newCallback);
        field.userData = newCallback;
    }

    private void ToggleViews(bool hasData)
    {
        var emptyView = rootVisualElement.Q<VisualElement>("empty-view");
        _dataView.style.display = hasData ? DisplayStyle.Flex : DisplayStyle.None;
        emptyView.style.display = hasData ? DisplayStyle.None : DisplayStyle.Flex;
    }
}