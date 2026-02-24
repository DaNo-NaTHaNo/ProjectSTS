using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.UIElements;

public class EpisodePortraitEditor : EditorWindow
{
    private VisualNovelSO _targetData;
    private ListView _listView;
    private VisualElement _dataView;
    private VisualElement _headerView;

    public static void OpenWindow(VisualNovelSO data)
    {
        var window = GetWindow<EpisodePortraitEditor>("EpisodePortrait");
        window.minSize = new Vector2(330, 480);
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
        if (_targetData.episodePortraits.Any())
        {
            int option = EditorUtility.DisplayDialogComplex(
                "새로운 EpisodePortrait", "기존 데이터를 지우고 새로 시작하시겠습니까?",
                "확인", "취소", "Export 후 초기화"
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
        if (_targetData == null || !_targetData.episodePortraits.Any())
        {
            EditorUtility.DisplayDialog("Export 실패", "내보낼 데이터가 없습니다.", "확인");
            return;
        }
        string path = EditorUtility.SaveFilePanel("Export EpisodePortrait CSV", "", "EpisodePortrait.csv", "csv");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            string csvText = CsvUtility.GenerateEpisodePortraits(_targetData.episodePortraits);
            File.WriteAllText(path, csvText, System.Text.Encoding.UTF8);
            Debug.Log($"EpisodePortrait 데이터가 성공적으로 Export 되었습니다: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV Export 중 에러 발생: {e.Message}");
        }
    }

    private void CreateNewSheet()
    {
        // [수정] Undo.RegisterCompleteObjectUndo를 사용하여 SO 전체 상태를 기록
        Undo.RegisterCompleteObjectUndo(_targetData, "Create New EpisodePortrait Sheet");
        _targetData.episodePortraits.Clear();
        _targetData.episodePortraits.Add(new EpisodePortrait());
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
    }

    private void OnImportButtonClicked()
    {
        string path = EditorUtility.OpenFilePanel("Import EpisodePortrait CSV", "", "csv");
        if (!string.IsNullOrEmpty(path))
        {
            // [수정] Undo.RegisterCompleteObjectUndo를 사용하여 SO 전체 상태를 기록
            Undo.RegisterCompleteObjectUndo(_targetData, "Import EpisodePortrait CSV");
            string csvText = File.ReadAllText(path);
            _targetData.episodePortraits = CsvUtility.ParseEpisodePortraits(csvText);
            EditorUtility.SetDirty(_targetData);
            RefreshListView();
        }
    }

    private void OnAddRowButtonClicked()
    {
        // [수정] Undo.RegisterCompleteObjectUndo를 사용하여 SO 전체 상태를 기록
        Undo.RegisterCompleteObjectUndo(_targetData, "Add Portrait Row");
        var newEntry = new EpisodePortrait();
        int insertIndex = _targetData.episodePortraits.Count;
        if (_listView.selectedItem != null) { insertIndex = _listView.selectedIndex + 1; }
        _targetData.episodePortraits.Insert(insertIndex, newEntry);
        EditorUtility.SetDirty(_targetData);
        RefreshListView();
        _listView.selectedIndex = insertIndex;
        _listView.ScrollToItem(insertIndex);
    }

    private void OnRemoveRowButtonClicked()
    {
        if (_listView.selectedItem == null) return;
        var selectedData = _listView.selectedItem as EpisodePortrait;
        // [수정] Undo.RegisterCompleteObjectUndo를 사용하여 SO 전체 상태를 기록
        Undo.RegisterCompleteObjectUndo(_targetData, "Remove Portrait Row");
        _targetData.episodePortraits.Remove(selectedData);
        EditorUtility.SetDirty(_targetData);
        _listView.ClearSelection();
        RefreshListView();
    }

    private void OnUndoRedo() { RefreshListView(); }

    private void RefreshListView()
    {
        if (_listView != null && _targetData != null)
        {
            _listView.itemsSource = _targetData.episodePortraits;
            _listView.Rebuild();
            ToggleViews(_targetData.episodePortraits.Any());
        }
    }

    private void SetupHeader()
    {
        _headerView = new VisualElement();
        _headerView.AddToClassList("row");
        AddHeaderLabel("No.", "header-no");
        AddHeaderLabel("portraitID", "header-speakerPortrait"); // 클래스 재활용
        AddHeaderLabel("portraitName", "header-speakerName"); // 클래스 재활용
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
        if (_targetData != null) _listView.itemsSource = _targetData.episodePortraits;

        _listView.makeItem = () => {
            var row = new VisualElement();
            row.AddToClassList("row");
            AddControlToRow(row, new Label(), "no", "col-no");
            AddControlToRow(row, new TextField(), "portraitID", "col-speakerPortrait");
            AddControlToRow(row, new TextField(), "portraitName", "col-speakerName");
            return row;
        };

        _listView.bindItem = (element, index) => {
            var data = _targetData.episodePortraits[index];
            element.Q<Label>("no").text = (index + 1).ToString();
            Bind(element.Q<TextField>("portraitID"), data, d => d.portraitID, (d, v) => d.portraitID = v);
            Bind(element.Q<TextField>("portraitName"), data, d => d.portraitName, (d, v) => d.portraitName = v);
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

    private void Bind<TValue>(BaseField<TValue> field, EpisodePortrait data, Func<EpisodePortrait, TValue> getter, Action<EpisodePortrait, TValue> setter)
    {
        field.SetValueWithoutNotify(getter(data));
        if (field.userData is EventCallback<ChangeEvent<TValue>> oldCallback)
        {
            field.UnregisterValueChangedCallback(oldCallback);
        }
        EventCallback<ChangeEvent<TValue>> newCallback = evt => {
            // [수정] 필드 값 변경 시 Undo/Redo를 기록합니다.
            Undo.RegisterCompleteObjectUndo(_targetData, "Change Portrait Field");
            setter(data, evt.newValue);
            EditorUtility.SetDirty(_targetData); // SO가 변경되었음을 알립니다.
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