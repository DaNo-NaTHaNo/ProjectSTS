# Phase Common — 공용 UI 위젯 작업 내역

*작업일: 2026.03.05*

---

## 1. 작업 개요

| 항목 | 값 |
| :---- | :---- |
| **Phase** | Phase Common (공용 UI 위젯) |
| **경로** | `Assets/_Project/Scripts/Runtime/UI/Common/` |
| **신규 파일** | 8개 |
| **수정 파일** | 1개 (`ProjectStS.UI.asmdef`) |
| **총 줄 수** | 1,909줄 |
| **커밋** | 4건 (feat 1 + fix 3) |

---

## 2. 파일별 상세

| 위젯 | 파일 | 줄 수 | 핵심 역할 |
| :---- | :---- | :---: | :---- |
| UICard | `UICard.cs` | 555 | 카드 표시 + 상호작용. 상태 머신(Normal/Hover/Selected/Disabled/Dragging), 드래그 앤 드롭, 호버 애니메이션 |
| UIElementBadge | `UIElementBadge.cs` | 133 | 속성 뱃지 (색상 + 한글 라벨). UICard/UIUnitPortrait/UIStatusIcon에서 서브 컴포넌트로 재사용 |
| UIHPBar | `UIHPBar.cs` | 159 | HP/방어도 게이지 바. DOTween 증감 애니메이션 + 대미지 잔상 트레일 |
| UIItemIcon | `UIItemIcon.cs` | 226 | 아이템 아이콘. 레어도 색상 테두리, 수량 뱃지, ItemData/InventoryItemData 지원 |
| UIPopup | `UIPopup.cs` | 313 | 범용 팝업 프레임워크. 확인/취소 + 정보 표시, DOTween 스케일+딤 애니메이션 |
| UIStatusIcon | `UIStatusIcon.cs` | 168 | 상태이상 아이콘. 스택/지속턴 표시, 버프(파랑)/디버프(빨강) 배경, 풀링 최적화 지원 |
| UITooltip | `UITooltip.cs` | 186 | 호버 툴팁. 화면 경계 자동 클램핑, 제목+본문 텍스트 |
| UIUnitPortrait | `UIUnitPortrait.cs` | 169 | 유닛 초상화. UIElementBadge 재사용, 하이라이트 테두리, UnitData/OwnedUnitData 지원 |

---

## 3. 설계 패턴

### 데이터 바인딩
- 모든 위젯이 `SetData()`/`Clear()` 패턴을 따름
- 데이터 타입별 오버로드 지원 (예: `UIItemIcon.SetData(ItemData)` vs `SetData(InventoryItemData)`)

### 싱글톤
- `UIPopup`, `UITooltip` — static `Show()`/`Hide()` 메서드, 씬당 단일 인스턴스

### 상태 머신
- `UICard` — `CardState` 열거형 (Normal, Hover, Selected, Disabled, Dragging)

### 풀링 친화
- `UIStatusIcon.UpdateValues()` — 스택/지속턴 수치만 빠르게 갱신 (풀링 시 SetData 재호출 최소화)

### 아트 플레이스홀더 전략
- 카드 → CardType 색상 배경
- 아이템 → 레어도 색상 테두리 + 2글자 약칭
- 유닛 → 속성 색상 원형 + 이름
- 상태이상 → 속성 색상 원형

---

## 4. asmdef 설정 변경

`ProjectStS.UI.asmdef` 참조 목록:

| 참조 | 용도 |
| :---- | :---- |
| `ProjectStS.Data` | 마스터/런타임 데이터 모델 |
| `ProjectStS.Core` | 코어/부트스트랩 시스템 |
| `ProjectStS.Battle` | 전투 런타임 타입 (RuntimeCard, ActiveStatusEffect) |
| `ProjectStS.Utils` | 유틸리티 확장 메서드 |
| `Unity.TextMeshPro` | TMP 지원 (fix 커밋으로 추가) |
| `Unity.ugui` | UI 프레임워크 (fix 커밋으로 추가) |

---

## 5. 수정된 컴파일 에러

### 에러 1: Unity.TextMeshPro 참조 누락

- **증상**: `TMPro.TextMeshProUGUI` CS0246 타입 미발견
- **원인**: `ProjectStS.UI.asmdef`에 `Unity.TextMeshPro` 어셈블리 참조 누락
- **수정**: asmdef references에 `"Unity.TextMeshPro"` 추가

### 에러 2: Unity.ugui 참조 누락

- **증상**: `UnityEngine.UI.Image` CS0246 타입 미발견
- **원인**: `ProjectStS.UI.asmdef`에 `Unity.ugui` 어셈블리 참조 누락
- **수정**: asmdef references에 `"Unity.ugui"` 추가 + UICard 호버 오프셋 적용

### 에러 3: DOTween UI 확장 메서드 접근 불가 (CS1061/CS1929)

- **증상**:
  - `Image.DOFillAmount()` — CS1061 정의 없음 (UIHPBar.cs)
  - `Image.DOFade()` — CS1929 수신자 타입 불일치 (UIPopup.cs)
- **원인**: `DOFillAmount`, `DOFade(Image)` 등 UI 확장 메서드는 `DOTweenModuleUI.cs` 소스 파일에 정의됨. 이 파일은 글로벌 Assembly-CSharp에만 컴파일되므로 asmdef 기반 `ProjectStS.UI` 어셈블리에서 접근 불가
- **수정**: DOTween 코어 DLL의 `DOTween.To()` 람다 API로 대체
  - `_fillImage.DOFillAmount(ratio, duration)` → `DOTween.To(() => _fillImage.fillAmount, x => _fillImage.fillAmount = x, ratio, duration)`
  - `_dimBackground.DOFade(0.5f, duration)` → `DOTween.To(() => color.a getter/setter, targetValue, duration)`

---

## 6. 커밋 이력

| 순서 | 커밋 메시지 | 비고 |
| :---: | :---- | :---- |
| 1 | `feat(ui): Phase Common 공용 UI 위젯 8종 구현` | 위젯 8개 신규 생성 |
| 2 | `fix(ui): asmdef에 Unity.TextMeshPro 참조 추가` | TMP 컴파일 에러 해결 |
| 3 | `fix(ui): Unity.ugui 참조 추가 및 UICard hover 오프셋 적용` | UI Image 컴파일 에러 해결 |
| 4 | `fix(ui): DOTween UI 확장 메서드를 DOTween.To() 코어 API로 대체` | DOTween asmdef 호환성 해결 |

---

## 7. 후속 작업

- [ ] 각 위젯에 대응하는 프리팹 생성 (Unity 에디터)
- [ ] Phase 2.5: 전투 UI — BattleUIBridge 이벤트 구독 + 전투 씬 레이아웃
- [ ] Phase 3.5: 스테이지 UI — StageUIBridge 이벤트 구독 + 월드맵 씬 레이아웃
- [ ] Phase 4 UI: 로비 화면, 파티 편성, 인벤토리, 캠페인 UI
