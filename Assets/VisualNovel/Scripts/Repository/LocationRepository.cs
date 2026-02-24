using UnityEngine;

[CreateAssetMenu(fileName = "LocationRepository", menuName = "VisualNovel/Repository/LocationRepository")]
public class LocationRepository : ScriptableObject
{
    public Vector2 leftMost;
    public Vector2 left;
    public Vector2 center;
    public Vector2 right;
    public Vector2 rightMost;
}