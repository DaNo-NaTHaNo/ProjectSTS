using UnityEngine;
using UnityEditor;
using System.Reflection;
using UnityEngine.UIElements;

public class UIToolkitVersionChecker
{
    [MenuItem("Tools/Check UI Toolkit Version")]
    public static void CheckVersion()
    {
        // UI Toolkit의 핵심 클래스인 VisualElement를 통해
        // 어떤 라이브러리(Assembly)에서 로드되었는지 정보를 가져옵니다.
        Assembly uiToolkitAssembly = typeof(VisualElement).Assembly;

        // 라이브러리의 전체 이름(버전 정보 포함)을 콘솔에 출력합니다.
        Debug.Log($"현재 프로젝트가 참조하는 UI Toolkit 라이브러리 정보:\n{uiToolkitAssembly.FullName}");
    }
}