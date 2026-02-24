using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PortraitCoordinates", menuName = "Portrait/Coordinates")]
public class PortraitCoordinates : ScriptableObject
{
    [System.Serializable]
    public class LayerInfo
    {
        public string layerName;          // PNG 파일명(확장자 제외)
        public Vector2 anchoredPosition;  // 2048 Canvas 중앙 기준 좌표
        public Vector2 size;              // Sprite sizeDelta
    }

    public List<LayerInfo> layers = new List<LayerInfo>();
}
