using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PortraitGenerator : EditorWindow
{
    private Texture2D psdTexture;
    private PortraitCoordinates coordinatesAsset;
    private Vector2 rootPosition = Vector2.zero; // ★ 추가: Root Position 입력
    private string saveFolder = "Assets/GeneratedPortraits";

    private Vector2 scrollPos;

    [MenuItem("Tools/Portrait/Portrait Generator")]
    private static void OpenWindow()
    {
        GetWindow<PortraitGenerator>("Portrait Generator");
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("1. PSD 파일로 좌표 추출", EditorStyles.boldLabel);

        psdTexture = (Texture2D)EditorGUILayout.ObjectField("PSD Texture (Atlas)", psdTexture, typeof(Texture2D), false);

        if (GUILayout.Button("Extract Coordinates from PSD"))
        {
            if (psdTexture == null)
            {
                Debug.LogError("PSD Texture를 지정해야 합니다.");
            }
            else
            {
                ExtractCoordinates(psdTexture);
            }
        }

        GUILayout.Space(20);

        GUILayout.Label("2. Portrait 프리팹 생성", EditorStyles.boldLabel);

        coordinatesAsset = (PortraitCoordinates)EditorGUILayout.ObjectField("Coordinates Asset", coordinatesAsset, typeof(PortraitCoordinates), false);

        rootPosition = EditorGUILayout.Vector2Field("Root Position", rootPosition); // ★ 추가: Root Position 입력필드

        if (GUILayout.Button("Generate Portrait Prefab"))
        {
            if (coordinatesAsset == null || psdTexture == null)
            {
                Debug.LogError("Coordinates Asset과 PSD Texture를 모두 지정해야 합니다.");
            }
            else
            {
                GeneratePortraitPrefab(coordinatesAsset, psdTexture, rootPosition);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void ExtractCoordinates(Texture2D texture)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);

        if (string.IsNullOrEmpty(texturePath))
        {
            Debug.LogError("PSD Texture 경로를 찾을 수 없습니다.");
            return;
        }

        byte[] bytes = File.ReadAllBytes(texturePath);
        using (MemoryStream stream = new MemoryStream(bytes))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            string signature = ReadAsciiString(reader, 4);
            if (signature != "8BPS")
            {
                Debug.LogError("올바른 PSD 파일이 아닙니다.");
                return;
            }

            short version = ReadInt16BigEndian(reader);
            if (version != 1)
            {
                Debug.LogError($"지원하지 않는 PSD 버전입니다. (버전: {version})");
                return;
            }

            reader.BaseStream.Seek(6, SeekOrigin.Current); // Reserved
            short channelCount = ReadInt16BigEndian(reader);
            int height = ReadInt32BigEndian(reader);
            int width = ReadInt32BigEndian(reader);
            short depth = ReadInt16BigEndian(reader);
            short colorMode = ReadInt16BigEndian(reader);

            int colorModeDataLength = ReadInt32BigEndian(reader);
            reader.BaseStream.Seek(colorModeDataLength, SeekOrigin.Current);

            int imageResourcesLength = ReadInt32BigEndian(reader);
            reader.BaseStream.Seek(imageResourcesLength, SeekOrigin.Current);

            int layerAndMaskLength = ReadInt32BigEndian(reader);
            if (layerAndMaskLength == 0)
            {
                Debug.LogWarning("Layer and Mask Section이 비어 있습니다. 레이어 데이터 없음.");
                return;
            }

            int layerInfoLength = ReadInt32BigEndian(reader);
            if (layerInfoLength == 0)
            {
                Debug.LogWarning("Layer Info Length가 0입니다. 레이어 데이터 없음.");
                return;
            }

            long layerInfoEndPos = reader.BaseStream.Position + layerInfoLength;
            int layerCount = ReadInt16BigEndian(reader);
            if (layerCount < 0) layerCount = -layerCount;

            var layerList = new List<PortraitCoordinates.LayerInfo>();

            int fixedNameIndex = 0;

            string[] fixedLayerNames = new string[]
            {
                "Default",
                "Smile",
                "Laugh",
                "Angry",
                "Fury",
                "Sad",
                "Cry",
                "Think",
                "Surprised",
                "ClosedEyes"
            };

            while (reader.BaseStream.Position < layerInfoEndPos && fixedNameIndex < fixedLayerNames.Length)
            {
                int top = ReadInt32BigEndian(reader);
                int left = ReadInt32BigEndian(reader);
                int bottom = ReadInt32BigEndian(reader);
                int right = ReadInt32BigEndian(reader);

                short channelCountInLayer = ReadInt16BigEndian(reader);

                for (int ch = 0; ch < channelCountInLayer; ch++)
                {
                    reader.BaseStream.Seek(6, SeekOrigin.Current);
                }

                reader.BaseStream.Seek(12, SeekOrigin.Current);
                int extraDataSize = ReadInt32BigEndian(reader);

                if (extraDataSize > 0)
                {
                    reader.BaseStream.Seek(extraDataSize, SeekOrigin.Current);
                }

                int layerWidth = right - left;
                int layerHeight = bottom - top;
                Vector2 center = new Vector2(left + layerWidth / 2f, top + layerHeight / 2f);
                Vector2 canvasCenterOffset = new Vector2(width / 2f, height / 2f);

                Vector2 anchoredPos = new Vector2(
                    center.x - canvasCenterOffset.x,
                    -(center.y - canvasCenterOffset.y)
                );

                layerList.Add(new PortraitCoordinates.LayerInfo
                {
                    layerName = fixedLayerNames[fixedNameIndex],
                    anchoredPosition = anchoredPos,
                    size = new Vector2(layerWidth, layerHeight)
                });

                fixedNameIndex++;
            }

            PortraitCoordinates coordAsset = ScriptableObject.CreateInstance<PortraitCoordinates>();
            coordAsset.layers = layerList;

            string saveDir = "Assets/Generated";
            Directory.CreateDirectory(saveDir);

            string savePath = Path.Combine(saveDir, texture.name + "_Coordinates.asset").Replace('\\', '/');
            AssetDatabase.CreateAsset(coordAsset, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ 좌표 추출 완료: {savePath}");

            coordinatesAsset = AssetDatabase.LoadAssetAtPath<PortraitCoordinates>(savePath);
        }
    }

    private void GeneratePortraitPrefab(PortraitCoordinates coordAsset, Texture2D texture, Vector2 rootPos)
    {
        string texturePath = AssetDatabase.GetAssetPath(texture);
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);

        List<Sprite> sprites = new List<Sprite>();
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            Debug.LogError("PSD 파일에서 Sprite를 찾지 못했습니다.");
            return;
        }

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
        rootRect.anchoredPosition = rootPos; // ★ 추가: 입력한 Root Pos 적용

        foreach (var layerInfo in coordAsset.layers)
        {
            GameObject child = new GameObject("Portrait_" + layerInfo.layerName, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);

            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchoredPosition = layerInfo.anchoredPosition;
            rect.sizeDelta = layerInfo.size;
            rect.localScale = Vector3.one;

            Image img = child.AddComponent<Image>();
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

        GameObject emotePoint = new GameObject("EmotePoint", typeof(RectTransform));
        emotePoint.transform.SetParent(root.transform, false);
        RectTransform emoteRect = emotePoint.GetComponent<RectTransform>();
        emoteRect.anchoredPosition = new Vector2(0, 200);
        emoteRect.sizeDelta = new Vector2(160, 160); // ★ 추가: EmotePoint Size 설정

        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        string prefabPath = Path.Combine(saveFolder, texture.name + "_Portrait.prefab").Replace("\\", "/");
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        Debug.Log($"✅ Portrait 프리팹 생성 완료: {prefabPath}");
    }

    private int ReadInt32BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes);
        return System.BitConverter.ToInt32(bytes, 0);
    }

    private short ReadInt16BigEndian(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(2);
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes);
        return System.BitConverter.ToInt16(bytes, 0);
    }

    private string ReadAsciiString(BinaryReader reader, int length)
    {
        byte[] bytes = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(bytes);
    }
}
