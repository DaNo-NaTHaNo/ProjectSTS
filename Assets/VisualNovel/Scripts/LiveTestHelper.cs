using UnityEngine;
using System;

/// <summary>
/// 에디터와 런타임 간의 통신을 중계하고, 
/// 테스트 씬의 데이터 소스(SO)를 지정하는 중앙 허브(Hub)입니다.
/// 이 컴포넌트는 테스트 씬의 GameObject에 하나만 존재해야 합니다.
/// </summary>
public class LiveTestHelper : MonoBehaviour
{
    // --- 1. 데이터 로드용 '깃발' (인스턴스 멤버) ---

    [Tooltip("테스트 씬에서 우선적으로 로드할 VisualNovelSO (CurrentEpisode.asset)를 연결하세요.")]
    public VisualNovelSO testEpisodeSO;

    // --- 2. "Play from Here" (PFH) 통신용 (EditorPrefs 감시) ---

    private void Update()
    {
        // 에디터(VisualNovelGraphView)가 보낸 PFH 요청이 있는지 매 프레임 감시합니다.
        // (씬이 리로드되거나 플레이 도중이어도 EditorPrefs는 유지되므로 안전합니다)
#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool("PFH_Requested", false))
        {
            // 1. 요청 플래그를 즉시 끔 (중복 실행 방지)
            UnityEditor.EditorPrefs.SetBool("PFH_Requested", false);

            // 2. 타겟 노드 ID 가져오기
            string targetID = UnityEditor.EditorPrefs.GetString("PFH_TargetID", "");

            // 3. 씬에 있는 Player 찾기
            // (경고 방지를 위해 FindFirstObjectByType 사용)
            var player = FindFirstObjectByType<VisualNovelPlayer>();

            if (player != null)
            {
                Debug.Log($"[LiveTestHelper] PFH 요청 감지됨. Target: {targetID}");
                // 4. Player의 PFH 진입점 호출
                player.PlayFromHere(targetID);
            }
            else
            {
                Debug.LogError("[LiveTestHelper] 씬에 VisualNovelPlayer가 없습니다.");
            }
        }
#endif
    }

    // --- 3. 노드 하이라이트 통신용 정적(static) 멤버 (Runtime -> Editor) ---

    /// <summary>
    /// 에디터(VisualNovelEditorWindow)가 구독할 이벤트입니다.
    /// Player가 노드를 실행할 때마다 이 이벤트가 발생합니다.
    /// </summary>
    public static event Action<string> OnNodeStartProcessing;

    /// <summary>
    /// [Runtime 전용] VisualNovelPlayer가 이 메서드를 호출하여
    /// 현재 노드 ID를 Editor(EditorWindow)로 안전하게 중계합니다.
    /// </summary>
    public static void TriggerNodeProcessing(string nodeID)
    {
        OnNodeStartProcessing?.Invoke(nodeID);
    }
}