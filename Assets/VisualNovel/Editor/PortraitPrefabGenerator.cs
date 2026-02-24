using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

public class PortraitPrefabGenerator : EditorWindow
{
    private PortraitCoordinates coordinatesAsset;
    private Texture2D psdTexture;
    private string saveFolder = "Assets/GeneratedPortraits";

    [MenuItem("Tools/Portrait/Generate Portrait Prefab")]
    private static void OpenWindow()
    {
        GetWindow<PortraitPrefabGenerator>("Portrait Prefab Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Portrait Prefab Generator", EditorStyles.boldLabel);

        coordinatesAsset = (PortraitCoordinates)EditorGUILayout.ObjectField("Coordinates Asset", coordinatesAsset, typeof(PortraitCoordinates), false);
        psdTexture = (Texture2D)EditorGUILayout.ObjectField("PSD Texture (Atlas)", psdTexture, typeof(Texture2D), false);

        if (GUILayout.Button("Generate Prefab"))
        {
            if (coordinatesAsset == null || psdTexture == null)
            {
                Debug.LogError("Coordinates Asset과 PSD Texture를 모두 지정해야 합니다.");
                return;
            }
            GeneratePortraitPrefab();
        }
    }

    private void GeneratePortraitPrefab()
    {
        string texturePath = AssetDatabase.GetAssetPath(psdTexture);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
            {
                sprites.Add(sprite);
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogError("PSD 파일에서 Sprite를 찾지 못했습니다.");
            return;
        }

        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다.");
            return;
        }

        GameObject root = new GameObject("Portrait_Root", typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(2048, 2048);

        foreach (var layerInfo in coordinatesAsset.layers)
        {
            GameObject child = new GameObject("Portrait_" + layerInfo.layerName, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = layerInfo.anchoredPosition;
            rect.sizeDelta = layerInfo.size;
            rect.localScale = Vector3.one;

            Image img = child.AddComponent<Image>();

            // 이름으로 Sprite 매칭
            Sprite matchedSprite = sprites.Find(x => x.name == layerInfo.layerName);
            if (matchedSprite != null)
            {
                img.sprite = matchedSprite;
                img.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"레이어 '{layerInfo.layerName}'에 매칭되는 Sprite를 찾지 못했습니다.");
            }
        }

        // Emote Point 추가
        GameObject emotePoint = new GameObject("EmotePoint", typeof(RectTransform));
        emotePoint.transform.SetParent(root.transform, false);
        RectTransform emoteRect = emotePoint.GetComponent<RectTransform>();
        emoteRect.anchoredPosition = new Vector2(0, 200);

        // 폴더가 없다면 생성
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        string prefabPath = Path.Combine(saveFolder, psdTexture.name + "_Portrait.prefab").Replace("\\", "/");

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log($"✅ Portrait 프리팹 생성 완료: {prefabPath}");
    }
}
