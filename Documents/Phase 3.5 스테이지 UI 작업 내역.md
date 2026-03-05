# Phase 3.5 — 스테이지 UI 작업 내역

*작성일: 2026.03.05*

---

## 개요

Phase Common(공용 UI 위젯 8종)과 Phase 2.5(전투 UI 10종)가 완료된 상태에서, 스테이지(월드맵) 씬의 UI 레이어를 구현하였다.
`StageUIBridge`가 발행하는 9개 이벤트를 구독하여 월드맵, HUD, 가방, 이벤트 팝업, 결과 화면을 표시하고,
플레이어의 노드 이동·이벤트 진입·보상 선택 인터랙션을 처리하는 **7개 스테이지 UI 스크립트**를 작성하였다.

- **총 7개 파일, 2,565줄**
- **스테이지 UI 스크립트 7개**: `Assets/_Project/Scripts/Runtime/UI/Stage/`
- **asmdef 수정 1건**: `ProjectStS.UI.asmdef`에 `ProjectStS.Stage` 참조 추가

---

## 생성된 파일 목록

### 스테이지 UI 스크립트 (7개)

| 파일 | 줄 수 | 핵심 역할 |
| :---- | :---: | :---- |
| `StageUIController.cs` | 334 | 스테이지 UI 중앙 컨트롤러. StageUIBridge 9개 이벤트 구독 → 하위 컴포넌트 분배, 노드 클릭·이벤트 진입·보상 선택 플로우 관리 |
| `UIWorldMap.cs` | 518 | 월드맵 렌더러. UIHexTile 오브젝트 풀링(초기 300개) + 뷰포트 컬링, ScrollRect 기반 줌/팬, 큐브좌표→픽셀 변환 |
| `UIStageResult.cs` | 569 | 스테이지 결과/보상 선택 화면. 승리/실패 표시, 고정 보상 N개 선택, 완료/추가 보상 표시, UIItemIcon 풀링 |
| `UIEventPopup.cs` | 331 | 이벤트 진입 팝업. EventType별 한글 표시명/배너 색상, DOTween Scale+Dim 애니메이션, 확인/취소 버튼 |
| `UIBagPanel.cs` | 326 | 인게임 가방 패널. UIItemIcon 재사용, InGameBagItemData→InventoryItemData 매핑, 슬라이드 토글 |
| `UIHexTile.cs` | 291 | 육각 타일 위젯(오브젝트 풀링 대상). HexNode 바인딩, 5단계 상태 비주얼, EventType별 아이콘/색상, IPointerClickHandler |
| `UIStageHUD.cs` | 196 | AP 게이지 + 구역명 HUD. DOTween 펀치 스케일(AP 변경), 페이드 연출(구역 전환) |

---

## 아키텍처

### 이벤트 → UI 매핑

```
StageUIBridge Event                         → UI 컴포넌트
──────────────────────────────────────────────────────────────
OnNodeMoved(HexNode)                        → UIWorldMap.SetCurrentNode() + FocusOnNode()
OnEventTriggered(HexNode, EventData)        → UIEventPopup.Show()
OnEventCompleted(EventData, bool)           → UIWorldMap.UpdateNodeState()
OnAPChanged(int, int)                       → UIStageHUD.SetAP()
OnZoneRevealed(string)                      → UIWorldMap.RevealZone() + UIStageHUD.SetZoneInfo()
OnNodeRevealed(HexNode)                     → UIWorldMap.RevealNode()
OnBagItemAdded(InGameBagItemData)           → UIBagPanel.AddItem()
OnStagePhaseChanged(StagePhase)             → StageUIController.SetExplorationUI()
OnStageResultShown(StageResult, EndReason)  → UIStageResult.Show(settlement)
```

### 데이터 흐름

```
StageManager
 ├─ ExplorationManager ──▶ StageUIBridge (9 events) ──▶ StageUIController
 ├─ InGameBagManager                                      ├─ UIWorldMap + UIHexTile pool
 └─ ZoneManager                                           ├─ UIStageHUD
                                                          ├─ UIBagPanel
                                                          ├─ UIEventPopup
                                                          └─ UIStageResult
```

### 컨트롤러 패턴 (BattleUIController 동일)

```
OnEnable()
  ├─ StageManager 해석 (SerializeField → ServiceLocator 폴백)
  └─ BindStageManager() → 9개 이벤트 구독 + InitializeStageUI()

OnDisable()
  └─ UnbindStageManager() → 9개 이벤트 해제
```

---

## 핵심 설계 포인트

### 1. UIHexTile 오브젝트 풀링 (UIWorldMap 관리)

- 전체 노드: 2,437개 (레벨 0~28 방사형 그리드)
- 동시 표시: ~200~300개 (뷰포트 내 Revealed 노드만)
- 풀 크기: 초기 300개, 부족 시 동적 확장
- 컬링: `ScrollRect.onValueChanged` → `UpdateVisibleTiles()` → 뷰포트 밖 타일 반환, 뷰포트 내 미활성 타일 배치

### 2. 육각 좌표 → 픽셀 변환 (Flat-top)

```csharp
float x = size * 1.5f * q;
float y = size * (√3/2 * q + √3 * r);
```

### 3. 카메라(뷰포트) 제어

- **줌**: `Input.mouseScrollDelta` → `_mapContainer.localScale` 변경 (0.3x ~ 2.0x)
- **팬**: `ScrollRect` 기본 드래그
- **포커스**: `DOTween.To()` → `scrollRect.normalizedPosition` 부드러운 이동

### 4. UIHexTile 비주얼 상태

| 상태 | 배경 Alpha | 이벤트 아이콘 | 하이라이트 |
| :---- | :---: | :---: | :---: |
| Hidden (미공개) | 0% | 숨김 | 없음 |
| Revealed (공개) | 50% | 표시 | 없음 |
| Visited (방문) | 30% | 표시 | 없음 |
| Current (현재 위치) | 100% | 표시 | 활성 |
| EventCompleted | 20% | 숨김 | 없음 |

- 경계 영역(IsBoundary) 노드: 이벤트 아이콘 항상 숨김

### 5. EventType 시각 매핑

| EventType | 표시명 | 아이콘 | 색상 |
| :---- | :---- | :---: | :---- |
| BattleNormal | 전투 | 검 | `#F44336` 적 |
| BattleElite | 엘리트 전투 | ★ | `#FF9800` 주황 |
| BattleBoss | 보스 전투 | 왕 | `#9C27B0` 보라 |
| BattleEvent | 이벤트 전투 | ⚡ | `#FF5722` 심적 |
| VisualNovel | 스토리 | 책 | `#2196F3` 청 |
| Encounter | 조우 | ? | `#4CAF50` 녹 |

### 6. 구역 색상 매핑 (CardinalPoint)

| 방위 | 색상 |
| :---- | :---- |
| N | `#4CAF50` 녹 |
| NE | `#2196F3` 청 |
| SE | `#9C27B0` 보라 |
| S | `#F44336` 적 |
| SW | `#FF9800` 주황 |
| NW | `#FFC107` 금 |

### 7. UIStageResult 보상 선택

- **고정 보상**: FixedRewardCandidates에서 FixedRewardSelectCount개 선택 → 아이콘 클릭 토글
- **완료 보상**: CompletionRewards 읽기 전용 표시
- **추가 보상**: BonusRewards 읽기 전용 표시
- 확인 버튼: 선택 수 충족 시 활성화 (`N/M` 카운터 표시)

---

## 수정된 파일 목록

| 변경 | 내용 |
| :---- | :---- |
| `ProjectStS.UI.asmdef` 수정 | `ProjectStS.Stage` 참조 추가 |
| `UIHexTile.cs` 수정 | `using EventType = ProjectStS.Data.EventType;` alias 추가 (UnityEngine.EventType 모호 참조 해결) |
| `UIEventPopup.cs` 수정 | 동일한 EventType alias 추가 |

---

## DOTween 활용 대상

| 연출 | 대상 | 트윈 유형 |
| :---- | :---- | :---- |
| 카메라 이동 | UIWorldMap | DOTween.To → scrollRect.normalizedPosition |
| AP 변경 | UIStageHUD | DOPunchScale (텍스트 펀치) |
| 구역 전환 | UIStageHUD | CanvasGroup.DOFade (페이드 아웃 → 텍스트 교체 → 페이드 인) |
| 노드 공개 | UIHexTile | CanvasGroup.DOFade (0 → 1) |
| 가방 슬라이드 | UIBagPanel | DOAnchorPos (열기/닫기 슬라이드) + DOFade |
| 이벤트 팝업 | UIEventPopup | DOScale(0→1) + Image.DOFade(딤) |
| 결과 화면 | UIStageResult | DOScale(0→1) + Image.DOFade(딤) |

---

## 후속 작업

- [ ] StageScene에 Canvas(Screen Space - Overlay) + EventSystem 배치
- [ ] UIHexTile 프리팹 생성 (육각형 Image + EventIcon + HighlightBorder + CanvasGroup)
- [ ] UIWorldMap에 ScrollRect + MapContainer 구성, UIHexTile 프리팹 할당
- [ ] 각 서브 컴포넌트 프리팹 배치 및 StageUIController에 SerializeField 할당
- [ ] Phase 4 UI: 로비 화면, 파티 편성 UI, 인벤토리 UI, 캠페인 UI

---

*마지막 업데이트: 2026.03.05 | 작성자: Claude Code*
