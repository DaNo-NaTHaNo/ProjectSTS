using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class PsdCoordinateExtractor : EditorWindow
{
    private string psdPath = "";

    [MenuItem("Tools/Portrait/Extract PSD Coordinates")]
    private static void OpenWindow()
    {
        GetWindow<PsdCoordinateExtractor>("PSD Coordinate Extractor");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("PSD 파일 선택"))
        {
            psdPath = EditorUtility.OpenFilePanel("PSD 파일 선택", "Assets", "psd");
        }

        GUILayout.Space(10);
        GUILayout.Label("선택된 파일: " + psdPath);

        if (!string.IsNullOrEmpty(psdPath))
        {
            if (GUILayout.Button("좌표 추출 및 저장"))
            {
                ExtractCoordinates(psdPath);
            }
        }
    }

    private void ExtractCoordinates(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("PSD 파일이 존재하지 않습니다.");
            return;
        }

        byte[] bytes = File.ReadAllBytes(path);
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

            Debug.Log($"PSD 크기: {width}x{height}");

            // Color Mode Section
            int colorModeDataLength = ReadInt32BigEndian(reader);
            reader.BaseStream.Seek(colorModeDataLength, SeekOrigin.Current);

            // Image Resources Section
            int imageResourcesLength = ReadInt32BigEndian(reader);
            reader.BaseStream.Seek(imageResourcesLength, SeekOrigin.Current);

            // Layer and Mask Information Section
            int layerAndMaskLength = ReadInt32BigEndian(reader);
            if (layerAndMaskLength == 0)
            {
                Debug.LogWarning("Layer and Mask Section이 비어 있습니다. 레이어 데이터 없음.");
                return;
            }

            long layerInfoStartPos = reader.BaseStream.Position;
            int layerInfoLength = ReadInt32BigEndian(reader);
            if (layerInfoLength == 0)
            {
                Debug.LogWarning("Layer Info Length가 0입니다. 레이어 데이터 없음.");
                return;
            }

            long layerInfoEndPos = reader.BaseStream.Position + layerInfoLength;

            if (reader.BaseStream.Position + 2 > layerInfoEndPos)
            {
                Debug.LogWarning("Layer Info가 너무 짧아 레이어 수를 읽을 수 없습니다.");
                return;
            }

            int layerCount = ReadInt16BigEndian(reader);
            if (layerCount < 0) layerCount = -layerCount;

            Debug.Log($"레이어 수 (예상): {layerCount}");

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
                if (reader.BaseStream.Position + 18 > layerInfoEndPos)
                {
                    Debug.LogWarning("Layer 기본 정보 읽기 범위를 초과했습니다. 중단합니다.");
                    break;
                }

                int top = ReadInt32BigEndian(reader);
                int left = ReadInt32BigEndian(reader);
                int bottom = ReadInt32BigEndian(reader);
                int right = ReadInt32BigEndian(reader);

                short channelCountInLayer = ReadInt16BigEndian(reader);

                for (int ch = 0; ch < channelCountInLayer; ch++)
                {
                    if (reader.BaseStream.Position + 6 > layerInfoEndPos)
                    {
                        Debug.LogWarning("채널 데이터 읽기 범위를 초과했습니다. 중단합니다.");
                        break;
                    }
                    reader.BaseStream.Seek(6, SeekOrigin.Current); // channelID(2) + channelDataLength(4)
                }

                if (reader.BaseStream.Position + 12 > layerInfoEndPos)
                {
                    Debug.LogWarning("Blend Mode, Opacity 등 읽기 범위를 초과했습니다. 중단합니다.");
                    break;
                }
                reader.BaseStream.Seek(12, SeekOrigin.Current); // Blend Mode Key + Opacity + Flags

                if (reader.BaseStream.Position + 4 > layerInfoEndPos)
                {
                    Debug.LogWarning("ExtraDataSize 읽기 범위를 초과했습니다. 중단합니다.");
                    break;
                }
                int extraDataSize = ReadInt32BigEndian(reader);

                if (extraDataSize > 0 && reader.BaseStream.Position + extraDataSize <= layerInfoEndPos)
                {
                    reader.BaseStream.Seek(extraDataSize, SeekOrigin.Current);
                }

                int layerWidth = right - left;
                int layerHeight = bottom - top;
                Vector2 center = new Vector2(left + layerWidth / 2f, top + layerHeight / 2f);
                Vector2 canvasCenterOffset = new Vector2(width / 2f, height / 2f);

                // 여기! Y축 부호 반전 적용
                Vector2 anchoredPos = new Vector2(
                    center.x - canvasCenterOffset.x,
                    -(center.y - canvasCenterOffset.y)
                );

                string assignedName = fixedNameIndex < fixedLayerNames.Length
                    ? fixedLayerNames[fixedNameIndex]
                    : "[NoName]";

                layerList.Add(new PortraitCoordinates.LayerInfo
                {
                    layerName = assignedName,
                    anchoredPosition = anchoredPos,
                    size = new Vector2(layerWidth, layerHeight)
                });

                fixedNameIndex++;
            }

            Debug.Log($"추출 완료된 레이어 수: {layerList.Count}");

            PortraitCoordinates coordAsset = ScriptableObject.CreateInstance<PortraitCoordinates>();
            coordAsset.layers = layerList;

            string saveDir = "Assets/Generated";
            Directory.CreateDirectory(saveDir);

            string savePath = Path.Combine(saveDir, Path.GetFileNameWithoutExtension(path) + "_Coordinates.asset")
                              .Replace('\\', '/');
            AssetDatabase.CreateAsset(coordAsset, savePath);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ 좌표 추출 완료: {savePath}");
        }
    }

    private int ReadInt32BigEndian(BinaryReader reader)
    {
        if (reader.BaseStream.Position + 4 > reader.BaseStream.Length)
            throw new EndOfStreamException("파일 끝에 도달했습니다 (Int32 읽기 실패)");
        byte[] bytes = reader.ReadBytes(4);
        if (System.BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes);
        return System.BitConverter.ToInt32(bytes, 0);
    }

    private short ReadInt16BigEndian(BinaryReader reader)
    {
        if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
            throw new EndOfStreamException("파일 끝에 도달했습니다 (Int16 읽기 실패)");
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
