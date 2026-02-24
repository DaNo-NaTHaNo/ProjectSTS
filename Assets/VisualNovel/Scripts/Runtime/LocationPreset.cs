[System.Serializable]
public class LocationPreset
{
    public string locationPreset;
    public string leftMost;
    public string left;
    public string center;
    public string right;
    public string rightMost;
    // 실제 사용 시 DOTween.Ease 타입으로 변환이 필요합니다.
    public string ease;
    public float duration;
}