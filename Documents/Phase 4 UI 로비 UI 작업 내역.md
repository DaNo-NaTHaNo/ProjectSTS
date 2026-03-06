# Phase 4 UI — 로비 UI 작업 내역

*작성일: 2026.03.06*

---

## 개요

Phase Common(공용 UI 위젯 8종), Phase 2.5(전투 UI 10종), Phase 3.5(스테이지 UI 7종)이 완료된 상태에서, 메타 레이어(로비) 씬의 UI 레이어를 구현하였다.
Meta 매니저(PlayerDataManager, PartyEditManager, InventoryManager, CampaignManager, ExpeditionLauncher)의 이벤트와 API를 구독하여 로비 메인 화면, 파티 편성, 인벤토리, 캠페인 화면을 표시하고, 플레이어의 파티 배치·장비 편집·필터/정렬·캠페인 추적·탐험 개시 인터랙션을 처리하는 **12개 로비 UI 스크립트**를 작성하였다.

- **총 12개 파일, 3,795줄**
- **로비 UI 스크립트 12개**: `Assets/_Project/Scripts/Runtime/UI/Lobby/`
- **asmdef 수정 1건**: `ProjectStS.UI.asmdef`에 `ProjectStS.Meta` 참조 추가

---

## 생성된 파일 목록

### 로비 UI 스크립트 (12개)

| 파일 | 줄 수 | 핵심 역할 |
| :---- | :---: | :---- |
| `LobbyUIController.cs` | 370 | 로비 UI 중앙 컨트롤러. Meta 매니저 생성·연결, 4개 화면 전환(Main/PartyEdit/Inventory/Campaign), 파티 미리보기, 탐험 개시 |
| `PartyEditUIController.cs` | 310 | 파티 편성 화면 컨트롤러. UIPartySlot 3개 + 유닛 리스트 + UIUnitEditPanel 관리, PartyEditManager 이벤트 구독 |
| `UIPartySlot.cs` | 195 | 파티 슬롯 위젯. IDropHandler/IPointerClickHandler, 유닛 초상화 + 장비 아이템 2개 표시, 드래그 드롭 수신 |
| `UIUnitEditPanel.cs` | 400 | 유닛 편집 패널. 덱 6장(UICard) + 스킬 1(선택 UI) + 아이템 2(UIItemIcon), 카드/스킬 교체 팝업 |
| `InventoryUIController.cs` | 200 | 인벤토리 화면 컨트롤러. UIInventoryFilter → InventoryManager API → UIInventoryGrid → UIItemDetail 연동 |
| `UIInventoryFilter.cs` | 340 | 필터/정렬 컨트롤. 카드/아이템 탭, 속성·코스트·타입 필터 드롭다운, 5종 정렬, 오름/내림차순 토글 |
| `UIInventoryGrid.cs` | 170 | 인벤토리 그리드. UIItemIcon 오브젝트 풀링(초기 60개), Queue 기반 회수/재사용 |
| `UIItemDetail.cs` | 320 | 아이템/카드 상세 패널. 카드 전용(코스트·타입·속성) / 아이템 전용(타입·효과·소모 여부), DOTween 슬라이드 등장/소멸 |
| `CampaignUIController.cs` | 260 | 캠페인 화면 컨트롤러. UICampaignList + UICampaignDetail + UICampaignTracker 관리, CampaignManager 이벤트 구독 |
| `UICampaignList.cs` | 220 | 캠페인 리스트. 활성(진행 중)/완료 분리 표시, CampaignEntryView 구조체로 항목 관리 |
| `UICampaignDetail.cs` | 280 | 캠페인 상세 패널. 이름·설명·목표 리스트(미완료 상단/완료 하단 스트라이크스루), 추적 버튼 |
| `UICampaignTracker.cs` | 135 | 추적 캠페인 HUD 오버레이. 캠페인명 + 현재 목표, DOTween 페이드 애니메이션 |

### asmdef 변경

| 변경 | 내용 |
| :---- | :---- |
| `ProjectStS.UI.asmdef` 수정 | `ProjectStS.Meta` 참조 추가 (Meta 매니저 접근용) |

---

## 아키텍처

### 이벤트 → UI 매핑

```
Meta Manager Event                            → UI 컴포넌트
──────────────────────────────────────────────────────────────
PlayerDataManager.OnPartyChanged              → LobbyUIController.RefreshPartyPreview()
                                              → PartyEditUIController.RefreshPartySlots/UnitList()
PlayerDataManager.OnUnitAdded(OwnedUnitData)  → PartyEditUIController.RefreshUnitList()
PlayerDataManager.OnInventoryChanged(Item)    → InventoryUIController.RefreshGrid()

PartyEditManager.OnDeckChanged(unitId)        → UIUnitEditPanel.RefreshDeck()
PartyEditManager.OnSkillChanged(unitId)       → UIUnitEditPanel.RefreshSkill()
PartyEditManager.OnItemChanged(unitId)        → UIUnitEditPanel.RefreshItems()

CampaignManager.OnCampaignUnlocked(Campaign)  → UICampaignList.AddCampaign()
CampaignManager.OnCampaignCompleted(Campaign)  → UICampaignList.MoveToCompleted()
CampaignManager.OnGoalCompleted(GoalGroup)    → CampaignUIController.RefreshTracker()

ExpeditionLauncher.OnExpeditionLaunched       → (씬 전환)
```

### 데이터 흐름

```
ServiceLocator
 ├─ PlayerDataManager ──▶ LobbyUIController (OnEnable에서 획득)
 └─ DataManager        ──▶ LobbyUIController (OnEnable에서 획득)

LobbyUIController (OnEnable)
 ├─ new PartyEditManager(playerData, dataManager)
 ├─ new InventoryManager(playerData)
 ├─ new CampaignManager(playerData, dataManager)
 └─ new ExpeditionLauncher(playerData, partyEdit, dataManager)

LobbyUIController ──▶ PartyEditUIController.Initialize(playerData, partyEdit, dataManager)
                  ──▶ InventoryUIController.Initialize(inventoryManager, dataManager)
                  ──▶ CampaignUIController.Initialize(campaignManager, dataManager)
```

### 화면 전환 구조

```
LobbyUIController
 ├─ ShowScreen(Main)       ─ _mainScreenRoot 활성, 하위 화면 전체 비활성
 ├─ ShowScreen(PartyEdit)  ─ PartyEditUIController.Show()
 ├─ ShowScreen(Inventory)  ─ InventoryUIController.Show()
 └─ ShowScreen(Campaign)   ─ CampaignUIController.Show()

하위 컨트롤러.OnBackRequested → LobbyUIController.ShowScreen(Main)
```

### 컨트롤러 패턴 (StageUIController/BattleUIController 동일)

```
OnEnable()
  ├─ ServiceLocator에서 PlayerDataManager, DataManager 획득
  ├─ 파생 매니저 인스턴스 생성 (PartyEdit, Inventory, Campaign, Expedition)
  ├─ PlayerDataManager 이벤트 3종 구독
  ├─ 하위 컨트롤러 매니저 이벤트 바인딩
  └─ InitializeLobby() → 파티 미리보기, 하위 컨트롤러 초기화

OnDisable()
  ├─ PlayerDataManager 이벤트 3종 해제
  ├─ ExpeditionLauncher 이벤트 해제
  ├─ 하위 컨트롤러 매니저 이벤트 언바인딩
  └─ 파생 매니저 참조 null 처리
```

---

## 핵심 설계 포인트

### 1. UIInventoryGrid 오브젝트 풀링

인벤토리 그리드에서 `UIItemIcon`을 오브젝트 풀링으로 관리한다.

- 초기 풀 크기: 60개 (성능 가이드라인 §6 준수)
- `Queue<UIItemIcon>` + `List<UIItemIcon>` 이중 구조
- `SetItems()` 호출 시 기존 활성 아이콘 전체 회수 → 새 데이터 바인딩
- 풀 부족 시 자동 확장 (`CreatePooledIcon()`)
- Button 컴포넌트 동적 추가로 클릭 이벤트 바인딩

### 2. UIInventoryFilter 필터/정렬 시스템

카드/아이템 2탭 + 조건별 필터 드롭다운 + 5종 정렬을 단일 컴포넌트로 관리한다.

- **카드 필터**: 속성(7종+전체), 코스트(0~5+), 카드타입(4종+전체)
- **아이템 필터**: 아이템타입(3종+전체)
- **정렬**: 이름/속성/레어도/코스트/수량, 오름/내림차순 토글
- 탭 전환 시 필터 UI 자동 교체 (`_cardFilterRoot` / `_itemFilterRoot`)
- `OnFilterChanged` 이벤트로 `InventoryUIController`에 갱신 요청

### 3. UIUnitEditPanel 장비 편집

유닛 1명의 전체 장비(덱 6장, 스킬 1개, 아이템 2개)를 편집하는 패널.

- **덱 편집**: UICard[6] 슬롯 클릭 → 카드 선택 팝업 (`_cardSelectionRoot`)
  - `PartyEditManager.GetAvailableCards()` → 속성 호환 카드만 표시
  - 선택 완료 → `PartyEditManager.TrySetDeck()` 호출
- **스킬 편집**: 스킬 버튼 클릭 → 스킬 선택 팝업 (`_skillSelectionRoot`)
  - `PartyEditManager.GetAvailableSkills()` → 해당 유닛 전용 스킬만 표시
  - "스킬 해제" 옵션 포함
- **아이템 편집**: 아이템 슬롯 클릭 → 장비 해제 (`TryRemoveItem`)
- `CanvasGroup` DOTween 페이드 등장/소멸 애니메이션

### 4. UIPartySlot 드래그 드롭

파티 슬롯은 `IDropHandler`를 구현하여 유닛 리스트에서 드래그된 `UIUnitPortrait`를 수신한다.

- 드롭 시 `OnUnitDropped` 이벤트 발행 (슬롯 포지션 + 유닛 ID)
- `PartyEditUIController`가 `PartyEditManager.TryAssignToParty()` 호출
- 기존 슬롯 유닛 자동 교체, 주인공 보호, 최소 인원 제한

### 5. 캠페인 추적 HUD

`UICampaignTracker`는 로비 메인 화면과 스테이지에서 공용으로 사용되는 HUD 오버레이.

- `CampaignManager.GetTrackedCampaign()` → 캠페인명 표시
- `CampaignManager.GetTrackedCurrentGoal()` → 현재 목표 표시
- 추적 캠페인이 없으면 자동 숨김
- DOTween 페이드 애니메이션

### 6. 탐험 개시 흐름

`LobbyUIController`에서 탐험 버튼 클릭 시:

1. `ExpeditionLauncher.ValidateExpedition()` → 5단계 검증 (파티/덱/속성/스킬/아이템)
2. 실패 시 `UIPopup.ShowInfo(errorMessage)` → 오류 내용 팝업
3. 성공 시 `UIPopup.ShowConfirm("탐험을 개시하시겠습니까?")` → 확인 팝업
4. 확인 → `ExpeditionLauncher.LaunchExpedition()` → 씬 전환

---

## 공용 위젯 재사용

| Phase Common 위젯 | 사용 위치 |
| :---- | :---- |
| `UICard` | UIUnitEditPanel 덱 슬롯 6개 + 카드 선택 팝업 |
| `UIUnitPortrait` | UIPartySlot 유닛 표시, 유닛 리스트, 파티 미리보기 |
| `UIItemIcon` | UIPartySlot 장비 표시, UIInventoryGrid 풀링, UIUnitEditPanel 아이템 슬롯 |
| `UIElementBadge` | UIUnitEditPanel 유닛/스킬 속성, UIItemDetail 카드 속성 |
| `UIPopup` | 탐험 개시 검증 실패/확인 팝업 |
| `DOTweenUIExtensions` | UIItemDetail 슬라이드, UICampaignDetail/Tracker 페이드 |

---

## DOTween 활용

| 연출 | 대상 | 트윈 유형 |
| :---- | :---- | :---- |
| 아이템 상세 패널 | `UIItemDetail._panelTransform` | DOAnchorPos 슬라이드 + DOFade 페이드 |
| 유닛 편집 패널 | `UIUnitEditPanel._canvasGroup` | DOFade 등장/소멸 |
| 캠페인 상세 패널 | `UICampaignDetail._canvasGroup` | DOFade 등장/소멸 |
| 캠페인 트래커 | `UICampaignTracker._canvasGroup` | DOFade 표시/숨김 |

---

## 후속 작업

### 에디터 배치 필요

- [ ] LobbyScene에 Canvas (Screen Space - Overlay) + EventSystem 배치
- [ ] LobbyUIController 루트 오브젝트 생성 및 하위 컨트롤러 배치
- [ ] UIPartySlot ×3 프리팹 생성 및 `_slotPosition` 값 설정 (1, 2, 3)
- [ ] UIUnitEditPanel 프리팹 생성 (UICard[6], 스킬/아이템 버튼, 선택 팝업 영역)
- [ ] UIInventoryFilter 프리팹 생성 (TMP_Dropdown ×5, Button ×3)
- [ ] UIInventoryGrid 프리팹 생성 (GridLayoutGroup + UIItemIcon 프리팹 참조)
- [ ] UIItemDetail 프리팹 생성 (카드/아이템 정보 영역 분리)
- [ ] UICampaignList/Detail/Tracker 프리팹 생성
- [ ] UIUnitPortrait 프리팹 참조 설정 (유닛 리스트용)
- [ ] UIPopup 프리팹 배치 (Canvas Overlay 최상위)
- [ ] 모든 SerializeField 인스펙터 할당

### 다음 Phase

- [ ] Phase 정산 UI: `UISettlement.cs` — 보상 정산 화면 (StageScene 오버레이)

---

*마지막 업데이트: 2026.03.06 | 작성자: Claude Code*
