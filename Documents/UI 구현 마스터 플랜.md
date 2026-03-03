# UI 구현 마스터 플랜

*작성일: 2026.03.02*

---

## 1. 개요

Phase 0~5(Core/Data/Battle/Stage/Meta/Integration) 완료 상태에서, **각 씬(로비/스테이지/전투)의 데이터를 화면에 표시하기 위한 UI 레이어**를 단계별로 구현한다.

### 현재 상태

| 항목 | 상태 |
|------|------|
| Phase 0~5 (Core/Data/Battle/Stage/Meta/Integration) | **모두 완료** — 111 C# 스크립트 |
| BattleUIBridge | 완료 — **13개 이벤트** 발행 준비 |
| StageUIBridge | 완료 — **9개 이벤트** 발행 준비 |
| Meta 레이어 매니저 이벤트 | 완료 — PlayerDataManager(3개), PartyEditManager(3개), CampaignManager(3개) |
| GameFlowController | 완료 — OnPhaseChanged, OnSettlementReady |
| **UI 레이어 스크립트** | **미구현** — asmdef만 존재 |
| **UI 프리팹** | **미구현** |
| **씬 내 UI 배치** | **미구현** |

### 확정 사항

| 항목 | 결정 |
|------|------|
| UI 프레임워크 | **uGUI** (Canvas + RectTransform) |
| 아트 리소스 | **Placeholder UI** (컬러 블록 + 텍스트 기반, 에셋 완성 시 교체) |
| 구현 범위 | **Phase별 순차 진행** (Common → 2.5 전투 → 3.5 스테이지 → 4 로비 → 정산) |
| 카드 인터랙션 | **드래그 + 클릭 모두 지원** |

### 영향 범위

- **신규 생성**: `Assets/_Project/Scripts/Runtime/UI/` 하위 전체
- **신규 생성**: `Assets/_Project/Prefabs/UI/` 하위 전체
- **수정 대상**: 각 씬 파일 (UI Canvas 배치)
- **수정 대상**: `ProjectStS.UI.asmdef` (참조 업데이트)

### 참조 문서

- `Documents/각 씬(레이어)에 대한 규정.md` — 각 화면의 UI 구성 요소 정의
- `Documents/기획 컨셉 및 게임플레이 개요.md` — 게임 흐름·핵심 메커니즘
- `Documents/구현 마스터 플랜.md` — Phase 2.5 / 3.5 / 4 UI 마일스톤
- `BattleUIBridge.cs` — 전투 UI 이벤트 13종
- `StageUIBridge.cs` — 스테이지 UI 이벤트 9종
- Meta 매니저 이벤트 (PlayerDataManager, PartyEditManager, CampaignManager)

---

## 2. 아키텍처

### 2.1 디렉터리 구조

```
Assets/_Project/Scripts/Runtime/UI/
├── Common/                ← 재사용 공용 컴포넌트
│   ├── UICard.cs              카드 표시 위젯
│   ├── UIUnitPortrait.cs      유닛 초상화 위젯
│   ├── UIItemIcon.cs          아이템 아이콘 위젯
│   ├── UIStatusIcon.cs        상태이상 아이콘 위젯
│   ├── UIHPBar.cs             HP/방어도 게이지 바
│   ├── UIElementBadge.cs      속성 표시 배지
│   ├── UITooltip.cs           호버 툴팁 시스템
│   └── UIPopup.cs             범용 팝업 프레임워크
│
├── Lobby/                 ← Phase 4 UI (LobbyScene)
│   ├── LobbyUIController.cs          로비 메인 화면 컨트롤러
│   ├── PartyEditUIController.cs       파티 편성 화면 컨트롤러
│   ├── InventoryUIController.cs       인벤토리 화면 컨트롤러
│   ├── CampaignUIController.cs        캠페인 화면 컨트롤러
│   ├── UIPartySlot.cs                 파티 슬롯 위젯
│   ├── UIUnitEditPanel.cs             유닛 편집 패널
│   ├── UIInventoryGrid.cs             인벤토리 그리드 리스트
│   ├── UIInventoryFilter.cs           필터/정렬 컨트롤
│   ├── UIItemDetail.cs                소지품 상세 정보 패널
│   ├── UICampaignList.cs              캠페인 리스트
│   ├── UICampaignDetail.cs            캠페인 상세 패널
│   └── UICampaignTracker.cs           캠페인 내비게이터 (HUD)
│
├── Stage/                 ← Phase 3.5 UI (StageScene)
│   ├── StageUIController.cs           스테이지 메인 UI 컨트롤러
│   ├── UIHexTile.cs                   육각 타일 표시 단위
│   ├── UIWorldMap.cs                  월드맵 렌더러 (카메라·줌·팬)
│   ├── UIStageHUD.cs                  AP, 구역명, 미니맵 등 HUD
│   ├── UIBagPanel.cs                  인게임 가방 패널
│   ├── UIEventPopup.cs               이벤트 진입 팝업
│   └── UIStageResult.cs              스테이지 결과/보상 선택 화면
│
├── Battle/                ← Phase 2.5 UI (BattleScene)
│   ├── BattleUIController.cs          전투 메인 UI 컨트롤러
│   ├── UIBattleUnitPanel.cs           유닛 상태 패널 (아군/적 공용)
│   ├── UIHandArea.cs                  손패 영역
│   ├── UIEnergyDisplay.cs             에너지 표시
│   ├── UIDeckCounter.cs               덱/묘지 카운터
│   ├── UIEnemyIntent.cs               적 행동 의도 표시
│   ├── UISkillNotify.cs               스킬 발동 알림
│   ├── UIBattleActions.cs             행동 종료/포기 버튼
│   └── UIBattleResult.cs              전투 결과 화면
│
└── Settlement/            ← Phase 5 보상 정산 UI
    └── UISettlement.cs                보상 정산 화면
```

### 2.2 이벤트 → UI 데이터 흐름

```
[로직 레이어]                    [브릿지]                      [UI 레이어]

BattleManager
 ├─ TurnManager        ──▶  BattleUIBridge (13 events)  ──▶  BattleUIController
 ├─ HandManager                                               ├─ UIHandArea
 ├─ CardExecutor                                              ├─ UIBattleUnitPanel
 ├─ StatusEffectManager                                       ├─ UIEnergyDisplay
 ├─ SkillExecutor                                             ├─ UIEnemyIntent
 ├─ BattleAI                                                  └─ UIBattleResult
 └─ BattleResultHandler

StageManager
 ├─ ExplorationManager  ──▶  StageUIBridge (9 events)   ──▶  StageUIController
 ├─ InGameBagManager                                          ├─ UIWorldMap
 └─ ZoneManager                                               ├─ UIStageHUD
                                                              └─ UIStageResult

PlayerDataManager      ──▶  직접 이벤트 (3 events)      ──▶  LobbyUIController
PartyEditManager       ──▶  직접 이벤트 (3 events)      ──▶  PartyEditUIController
CampaignManager        ──▶  직접 이벤트 (3 events)      ──▶  CampaignUIController
InventoryManager       ──▶  API 호출 (필터/정렬)         ──▶  InventoryUIController

GameFlowController     ──▶  OnSettlementReady           ──▶  UISettlement
```

### 2.3 asmdef 참조 구조

```
ProjectStS.UI 참조:
  ← ProjectStS.Data    (모델/Enum 접근)
  ← ProjectStS.Core    (ServiceLocator 접근)
  ← ProjectStS.Battle  (BattleUIBridge, BattleUnit 등 읽기)
  ← ProjectStS.Stage   (StageUIBridge, HexNode 등 읽기)
  ← ProjectStS.Meta    (PlayerDataManager, PartyEditManager 등 읽기)
```

---

## 3. 각 씬별 UI 요소 상세

### 3.1 LobbyScene — 메타 레이어(로비) UI

| UI 영역 | 표시 데이터 | 데이터 소스 | 상호작용 |
|---------|------------|-----------|---------|
| **로비 메인** | 파티 초상화, 캠페인 추적 정보, 탐험 개시 버튼 | `PlayerDataManager.GetPartyMembers()`, `CampaignManager` | 하위 화면 전환, 탐험 개시 → `ExpeditionLauncher` |
| **파티 배치 영역** | 3개 슬롯, 유닛 포트레이트, 장비 아이템 표시 | `OwnedUnitData.partyPosition`, `UnitData.portraitPath` | 드래그 배치 → `PartyEditManager.TryAssignToParty()` |
| **유닛 리스트** | 보유 유닛 가로 스크롤 | `PlayerDataManager.GetOwnedUnits()` | 클릭 → 유닛 편집 영역 열기 |
| **유닛 편집** | 아이템 슬롯 2개 + 카드 덱 6장 + 스킬 1개 | `OwnedUnitData` | 장비 변경 → `PartyEditManager.TryEquipItem()`, `TrySetDeck()`, `TrySetSkill()` |
| **인벤토리 리스트** | 카드/아이템 2탭, 그리드 | `InventoryManager.GetFilteredCards/Items()`, `GetSortedInventory()` | 필터/정렬 드롭다운, 아이템 클릭 → 상세 |
| **아이템 상세** | 이름, 설명, 아트, 레어도, 속성, 코스트 | `CardData` / `ItemData` (DataManager 조회) | 읽기 전용 |
| **캠페인 리스트** | 진행/완료 캠페인 목록 | `CampaignManager.GetActiveCampaigns()` | 클릭 → 상세, 추적 버튼 |
| **캠페인 상세** | 이름, 설명, 하위 목표(완료/진행 구분) | `CampaignData`, `CampaignGoalGroupData` | 읽기 전용 |
| **캠페인 내비게이터** | 추적 중인 캠페인 현 목표 | `CampaignManager._trackedCampaignId` | HUD 오버레이 |

### 3.2 StageScene — 스테이지(월드맵) UI

| UI 영역 | 표시 데이터 | 데이터 소스 | 상호작용 |
|---------|------------|-----------|---------|
| **월드맵** | 육각 타일 그리드, 구역 색상, 이벤트 아이콘 | `HexNode` (Q,R,S, AreaId, AssignedEvent, IsRevealed, IsVisited) | 노드 클릭 → 이동/이벤트 트리거 |
| **HUD - AP** | 현재 AP / 최대 AP | `StageUIBridge.OnAPChanged` | 읽기 전용 |
| **HUD - 구역** | 현재 구역 이름/비주얼 | `StageUIBridge.OnZoneRevealed` → `AreaData` | 읽기 전용 |
| **인게임 가방** | 스테이지 중 획득 아이템 목록 | `StageUIBridge.OnBagItemAdded`, `InGameBagManager` | 열기/닫기, 아이템 상세 확인 |
| **이벤트 팝업** | 이벤트 타입, 속성, 이름 | `StageUIBridge.OnEventTriggered` → `EventData` | 진입 확인/취소 |
| **캠페인 내비게이터** | 추적 캠페인 목표 + 맵 상 표시 | `CampaignManager` | HUD 오버레이 |
| **결과 화면** | 정산 유형, 보상 선택 | `StageUIBridge.OnStageResultShown` + `GameFlowController.OnSettlementReady` | 보상 선택 → `CompleteSettlement()` |

#### 월드맵 렌더링 고려사항

- 2,437 노드 전체 렌더링 시 성능 이슈 → **오브젝트 풀링 + 카메라 뷰포트 컬링** 필수
- `UIHexTile` 풀 크기: 화면 내 최대 표시 가능 수 (약 200~300개)
- 카메라: 2D 직교, 핀치 줌/팬 지원

### 3.3 BattleScene — 전투 UI

| UI 영역 | 표시 데이터 | 데이터 소스(이벤트) | 상호작용 |
|---------|------------|-------------------|---------|
| **아군 유닛 패널** (×3) | 포트레이트, HP바, 방어도, 상태이상 아이콘, 스킬 아이콘 | `OnUnitDamaged`, `OnUnitHealed`, `OnUnitBlockGained`, `OnStatusEffectChanged`, `OnUnitDefeated` | 카드 타겟 선택 |
| **적 유닛 패널** (×5) | 포트레이트, HP바, 상태이상, **행동 의도** | `OnEnemyIntentShown`, `OnUnitDamaged`, `OnUnitDefeated` | 카드 타겟 선택 |
| **손패 영역** | 카드 4~6장 (이름, 코스트, 속성, 설명, 아트) | `OnCardAddedToHand`, `OnCardRemovedFromHand`, `OnCardPlayed` | 드래그/클릭 → `CardExecutor.TryPlayCard()` |
| **에너지 표시** | 현재/기본 에너지 | `OnEnergyChanged` | 읽기 전용 |
| **덱/묘지 카운터** | 덱 남은 장수, 묘지 장수 | `DeckManager` 조회 | 클릭 → 목록 팝업 |
| **스킬 발동 알림** | 유닛명, 스킬명, 연출 | `OnSkillActivated` | 자동 재생 |
| **행동 종료 버튼** | PlayerAction 페이즈에서만 표시 | `OnPhaseChanged` (PlayerAction) | 클릭 → `TurnManager.EndPlayerPhase()` |
| **전투 포기 버튼** | 항상 접근 가능 | — | 클릭 → 확인 팝업 → `BattleManager.Surrender()` |
| **전투 결과** | 승리/패배, 사유 | `OnBattleResultShown` | 확인 → 스테이지 복귀 |

### 3.4 Settlement — 보상 정산 UI

| UI 영역 | 표시 데이터 | 데이터 소스 | 상호작용 |
|---------|------------|-----------|---------|
| **고정 보상 선택** | 신규 획득 아이템 중 N개 선택 | `StageSettlementData.fixedRewardCandidates`, `fixedRewardCount` | 아이템 선택 → 확정 |
| **완료 보상** | 드랍 테이블 랜덤 아이템 | `StageSettlementData.completionRewards` | 읽기 전용 |
| **추가 보상** | 엘리트/사건/VN 보상 | `StageSettlementData.additionalRewards` | 읽기 전용 |
| **확인 버튼** | — | — | 클릭 → `GameFlowController.CompleteSettlement()` |

---

## 4. 공용 위젯 설계

### 4.1 UICard

카드 한 장을 표시하는 위젯. 전투 손패, 인벤토리, 덱 편집에서 공용.

```
┌────────────────┐
│ [코스트]   [속성] │  ← UIElementBadge
│                │
│   [아트워크]    │  ← Placeholder: 컬러 블록
│                │
│ [카드 이름]     │
│ [효과 설명]     │
│ [레어도 표시]   │
└────────────────┘
```

- **데이터 바인딩**: `CardData` 또는 `RuntimeCard`
- **상태**: Normal, Hover, Selected, Disabled, Dragging
- **인터랙션**: IPointerEnterHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler

### 4.2 UIUnitPortrait

유닛 초상화 + 기본 정보. 로비, 전투 양쪽에서 사용.

```
┌──────────┐
│ [초상화]  │  ← Placeholder: 속성 색상 원형
│          │
│ [이름]    │
│ [속성]    │  ← UIElementBadge
└──────────┘
```

- **데이터 바인딩**: `UnitData` + `OwnedUnitData`

### 4.3 UIItemIcon

아이템 아이콘 + 수량 배지. 인벤토리, 장비 슬롯에서 사용.

```
┌────────┐
│ [아이콘] │  ← Placeholder: 레어도별 테두리 색상
│   [×N]  │  ← 수량 배지
└────────┘
```

- **데이터 바인딩**: `ItemData` + `InventoryItemData`

### 4.4 UIStatusIcon

상태이상 아이콘 + 스택/지속 표시.

```
┌──────┐
│ [아이콘] │
│  [×3]  │  ← 스택 수
│  [2T]  │  ← 남은 턴
└──────┘
```

- **데이터 바인딩**: `ActiveStatusEffect`

### 4.5 UIHPBar

HP/방어도 게이지 바. DOTween으로 증감 애니메이션.

```
[██████████░░░░] 75/100  [🛡 12]
     HP 바                방어도
```

- **데이터 바인딩**: `BattleUnit.CurrentHP`, `BattleUnit.MaxHP`, `BattleUnit.Block`
- **트윈**: HP 변경 시 DOTween으로 부드러운 게이지 이동

### 4.6 UIElementBadge

속성 타입을 색상 + 텍스트로 표시하는 배지.

| ElementType | 표시명 | Placeholder 색상 |
|-------------|-------|:---:|
| Sword | 바람 | `#4CAF50` (녹) |
| Wand | 불 | `#F44336` (적) |
| Medal | 땅 | `#FF9800` (주황) |
| Grail | 물 | `#2196F3` (청) |
| Sola | 빛 | `#FFC107` (금) |
| Luna | 어둠 | `#9C27B0` (보라) |
| Wild | 무 | `#9E9E9E` (회) |

### 4.7 UITooltip

호버 시 표시되는 툴팁 시스템.

- 싱글톤 Canvas 오버레이로 동작
- `Show(string title, string body, Vector2 position)`
- 화면 경계 자동 클램핑

### 4.8 UIPopup

범용 팝업 프레임워크. 확인/취소, 선택지, 정보 표시 등.

- `ShowConfirm(string message, Action onConfirm, Action onCancel)`
- `ShowInfo(string message, Action onClose)`
- 배경 딤 처리, DOTween 등장/소멸 애니메이션

---

## 5. 오브젝트 풀링 대상 (성능 가이드라인 §6)

| 풀링 대상 | 사용 씬 | 예상 풀 크기 | 비고 |
|-----------|--------|:---:|------|
| `UIHexTile` | StageScene | 200~300 | 뷰포트 컬링, 카메라 이동 시 재활용 |
| `UICard` | BattleScene | 18~24 | 전투 덱 최대 18장 + 여유분 |
| `UIStatusIcon` | BattleScene | 30~40 | 유닛당 복수 상태이상 가능 |
| `UIItemIcon` | LobbyScene | 50~60 | 인벤토리 그리드 셀 |

---

## 6. 아트 리소스 로딩

### Addressable 기반 로딩 전략

모든 데이터 모델에 `portraitPath`, `artworkPath` 등 **Addressable ID**가 정의됨.

- `manifest.json`에 `com.unity.addressables:2.4.3` 이미 추가됨
- UI에서 아트 로딩 시 `Addressables.LoadAssetAsync<Sprite>(path)` 사용
- 로딩 실패 시 placeholder 스프라이트 표시 (아트 에셋 미작업 상태 대비)
- 비동기 로딩 완료 콜백에서 Image.sprite 할당

### Placeholder 전략

아트 에셋 미완성 상태에서의 대체 표시:

| 리소스 유형 | Placeholder |
|------------|-------------|
| 유닛 초상화 | 속성 색상 원형 + 유닛 이름 텍스트 |
| 카드 아트워크 | 카드 타입별 색상 블록 + 이름 텍스트 |
| 아이템 아이콘 | 레어도별 테두리 색상 사각형 + 이름 약어 |
| 스킬 아이콘 | 속성 색상 다이아몬드 + 스킬명 |

---

## 7. DOTween 활용 대상

| 연출 | 대상 | 트윈 유형 |
|------|------|----------|
| 카드 드로우 | `UICard` | Move + Scale (덱 → 손패 위치) |
| 카드 사용 | `UICard` | Move + Fade (손패 → 타겟 방향 → 소멸) |
| HP 변경 | `UIHPBar` | Fill Amount 트윈 (현재 → 새 값) |
| 대미지 표시 | 플로팅 텍스트 | Move + Fade (위로 이동 후 소멸) |
| 스킬 발동 | `UISkillNotify` | Scale + Fade (팝업 → 확대 → 소멸) |
| 턴 배너 | 턴 시작 UI | Move (화면 밖 → 중앙 → 화면 밖) |
| 카메라 이동 | 월드맵 | DOTween.To (현재 위치 → 목표 노드) |
| 팝업 | `UIPopup` | Scale (0 → 1 등장, 1 → 0 소멸) |

---

## 8. 실행 단계

### 실행 순서 총괄

```
Phase Common (8 스크립트)
  ├── UICard, UIUnitPortrait, UIItemIcon, UIStatusIcon
  ├── UIHPBar, UIElementBadge
  ├── UITooltip, UIPopup
  └── asmdef 참조 업데이트
         ↓
Phase 2.5: 전투 UI (8 스크립트)
  ├── BattleUIController (UIBridge 13개 이벤트 구독)
  ├── UIBattleUnitPanel, UIHandArea, UIEnergyDisplay
  ├── UIDeckCounter, UIEnemyIntent, UISkillNotify
  ├── UIBattleActions, UIBattleResult
  └── BattleScene Canvas + 프리팹 배치
         ↓
Phase 3.5: 스테이지 UI (7 스크립트)
  ├── StageUIController (UIBridge 9개 이벤트 구독)
  ├── UIHexTile (오브젝트 풀링), UIWorldMap (카메라 줌/팬)
  ├── UIStageHUD, UIBagPanel, UIEventPopup
  ├── UIStageResult
  └── StageScene Canvas + 프리팹 배치
         ↓
Phase 4 UI: 로비 UI (12 스크립트)
  ├── LobbyUIController
  ├── PartyEditUIController + UIPartySlot + UIUnitEditPanel
  ├── InventoryUIController + UIInventoryGrid + UIInventoryFilter + UIItemDetail
  ├── CampaignUIController + UICampaignList + UICampaignDetail + UICampaignTracker
  └── LobbyScene Canvas + 프리팹 배치
         ↓
Phase 정산: 보상 정산 UI (1 스크립트)
  └── UISettlement (StageScene 오버레이)
```

### 총 산출물 요약

| Phase | 스크립트 수 | 핵심 산출물 |
|-------|:---:|-------------|
| Common | 8 | 재사용 공용 위젯 + asmdef 설정 |
| 2.5 전투 UI | 8 | BattleUIController + 전투 UI 서브 컴포넌트 |
| 3.5 스테이지 UI | 7 | StageUIController + 월드맵 렌더러 + HUD |
| 4 로비 UI | 12 | LobbyUIController + 편성/인벤토리/캠페인 UI |
| 정산 | 1 | UISettlement |
| **합계** | **~36** | |

### 각 Phase 진행 방식

1. 각 Phase 착수 전 **해당 Phase의 상세 설계를 Plan으로 제시** → 승인 후 구현
2. Phase 완료 시 **변경 파일 목록 + 테스트 필요 항목** 보고
3. 커밋 후 다음 Phase 진행 여부 확인

### 구현 우선순위 근거

```
[Common 위젯] ──필수 선행──▶ [전투 UI] ──필수 선행──▶ [스테이지 UI] ──필수 선행──▶ [로비 UI]
                              Phase 2.5              Phase 3.5                Phase 4 UI
```

- **전투 UI가 가장 독립적**: BattleUIBridge 이벤트만 구독하면 동작
- **스테이지 UI**: 전투 진입/복귀 흐름 테스트 필요 → 전투 UI 후
- **로비 UI**: 파티 편성 → 탐험 개시 → 스테이지 진입 흐름 → 스테이지 UI 후

---

## 9. 기술 요구사항 메모

### 씬 설정 필요사항

4개 씬 파일 모두 UI Canvas 미배치 상태. 각 씬에 아래 요소 배치 필요:

- `Canvas` (Screen Space - Overlay)
- `EventSystem`
- 씬별 UIController 루트 오브젝트

### 카드 인터랙션 (드래그 + 클릭 겸용)

- **드래그**: `IBeginDragHandler` → `IDragHandler` → `IEndDragHandler`
  - 드래그 시작 시 카드 확대 + 반투명화
  - 유효 타겟 위에서 드롭 → 카드 사용
  - 무효 영역 드롭 → 원위치 복귀 (DOTween)
- **클릭**: `IPointerClickHandler`
  - 카드 클릭 → 선택 상태 (하이라이트)
  - 타겟 클릭 → 카드 사용
  - 빈 영역 클릭 → 선택 해제

### 타겟 선택 시스템

`TargetSelectionRule`에 따른 분기:

| Rule | UI 동작 |
|------|--------|
| Manual | 플레이어가 직접 타겟 클릭/드롭 |
| AllTargets | 즉시 실행 (타겟 선택 불필요) |
| LowestHp | 즉시 실행 + 자동 타겟 하이라이트 |
| HighestHp | 즉시 실행 + 자동 타겟 하이라이트 |
| Random | 즉시 실행 + 랜덤 타겟 하이라이트 |
| Self | 즉시 실행 + 자기 자신 하이라이트 |

---

*마지막 업데이트: 2026.03.02 | 작성자: Claude Code*
