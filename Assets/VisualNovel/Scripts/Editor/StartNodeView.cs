public class StartNodeView : NodeView
{
    public StartNodeView() : base("Start", false, true) 
    {
        style.minHeight = 45;
        style.maxHeight = 45;
        style.minWidth = 120;
        style.maxWidth = 120;
    }

    // Start 노드는 저장할 필드가 없으므로 null을 반환합니다.
    public override BaseNodeFields SaveData()
    {
        return null;
    }

    // 불러올 데이터도 없으므로 아무것도 하지 않습니다.
    public override void LoadData(BaseNodeFields data) { }
}