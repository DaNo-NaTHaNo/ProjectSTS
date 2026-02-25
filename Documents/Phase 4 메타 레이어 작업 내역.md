# Phase 4: 메타 레이어(로비) 시스템 작업 내역

*작성일: 2026.02.25*
*커밋: `feat(meta): Phase 4 메타 레이어(로비) 핵심 매니저 구현`*

---

## 1. 개요

Phase 4는 게임의 메타 레이어(로비)를 구현한 단계이다.
플레이어의 보유 데이터 관리, 파티 편성, 인벤토리 필터링/정렬, 캠페인 추적, 탐험 개시를 담당한다.
총 **6개 C# 스크립트**, **2,070줄**이 신규 작성되었으며, 기존 **6개 파일**이 수정되었다.
UI/프리팹 없이 순수 로직만 구축하였다.

**네임스페이스**: `ProjectStS.Meta`, `ProjectStS.Data` (GameSettings)

---

## 2. 파일 목록 및 역할

### 2.1 신규 파일 (6개, 2,070줄)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Data/GameSettings.cs` | 74 | 게임 전역 설정 SO. 주인공 unitId, 파티/덱 크기 제한, 씬 이름 |
| `Meta/PlayerDataManager.cs` | 319 | 플레이어 영속 데이터 중앙 매니저. 보유 유닛, 인벤토리, 탐험 기록 관리 |
| `Meta/PartyEdit/PartyEditManager.cs` | 615 | 파티 편성, 덱/스킬/아이템 장비, 속성 검증, ownStack/useStack 관리 |
| `Meta/Inventory/InventoryManager.cs` | 223 | 인벤토리 필터링(속성/코스트/타입/스탯), 5종 정렬, 복합 필터+정렬 |
| `Meta/Campaign/CampaignManager.cs` | 572 | 캠페인 해금/목표 진행/완료/추적. 9가지 트리거 평가, 런타임 상태 관리 |
| `Meta/ExpeditionLauncher.cs` | 267 | 탐험 개시 검증(5단계), 카드 제외 규칙, 씬 전환 |

### 2.2 기존 파일 수정 (6개)

| 파일 | 변경 내용 |
|:---|:---|
| `Packages/manifest.json` | `com.unity.addressables:2.4.3` 패키지 추가 |
| `Data/DataManager.cs` | `GameSettings _gameSettings` 필드 + `Settings` 프로퍼티 추가 |
| `Data/Enums/InventoryEnums.cs` | `InventorySortType` 열거형 추가 (Name, Element, Rarity, Cost, Quantity) |
| `Core/GameBootstrapper.cs` | `DataManager` 참조 필드 추가 + ServiceLocator 등록 로직 |
| `Stage/StageManager.cs` | `OnExternalConditionCheck` 콜백 연결 (OwnCharacter/OwnItem → PlayerDataManager) |
| `Stage/ProjectStS.Stage.asmdef` | `ProjectStS.Meta` 참조 추가 |

---

## 3. 아키텍처

### 3.1 클래스 의존성

```
PlayerDataManager (MonoBehaviour, ServiceLocator 자체 등록)
 ├─ OwnedUnitData[]    ← 보유 유닛 목록
 ├─ InventoryItemData[] ← 인벤토리 목록
 └─ ExplorationRecordData ← 탐험 기록

PartyEditManager (POCO, 수동 생성)
 ├→ PlayerDataManager  ← 보유 유닛/인벤토리 조회·변경
 ├→ DataManager        ← UnitData, CardData, SkillData 마스터 조회
 └→ GameSettings       ← 주인공 ID, 파티/덱 제한

InventoryManager (POCO, 수동 생성)
 └→ PlayerDataManager  ← 인벤토리 목록 조회

CampaignManager (POCO, 수동 생성)
 ├→ PlayerDataManager  ← 보유 확인 (HasUnit/HasCard/HasItem), 탐험 기록
 └→ DataManager        ← CampaignTableSO, CampaignGoalGroupTableSO

ExpeditionLauncher (POCO, 수동 생성)
 ├→ PlayerDataManager  ← 파티 목록, 탐험 기록 갱신
 ├→ PartyEditManager   ← 파티 검증, 덱/스킬/아이템 조회
 ├→ DataManager        ← 카드 속성 검증
 └→ SceneTransitionManager ← 스테이지 씬 전환
```

### 3.2 서비스 등록 흐름

```
GameBootstrapper.Awake()
 ├─ ServiceLocator.Register(DataManager)     ← SerializeField 참조
 └─ ServiceLocator.Register(SceneTransitionManager) ← AddComponent

PlayerDataManager.Awake()
 └─ ServiceLocator.Register(this)            ← 자체 등록 (DontDestroyOnLoad 오브젝트)

PartyEditManager, InventoryManager, CampaignManager, ExpeditionLauncher
 └─ 로비 UI 초기화 시 수동 생성 (ServiceLocator 미등록, 로비 스코프)
```

### 3.3 순환 참조 회피

```
Core → Data                (Core asmdef)
Meta → Data, Core, Utils   (Meta asmdef)
Stage → Data, Core, Utils, Meta  (Stage asmdef — OnExternalConditionCheck 연동용)
```
Core가 Meta를 참조하지 않아 순환 없음. PlayerDataManager는 Awake()에서 자체 등록하여 GameBootstrapper의 Meta 의존 회피.

### 3.4 설계 원칙

| 원칙 | 적용 |
|:---|:---|
| **Phase 2/3 패턴 미러** | 오케스트레이터 + 서브시스템 + 이벤트 브릿지 구조 동일 |
| **싱글톤 회피** | ServiceLocator 패턴으로 PlayerDataManager 등록 |
| **이벤트 구독 방식** | Update() 사용 없이 Action 이벤트로 변경 알림 |
| **컬렉션 사전 할당** | 모든 List/Dictionary에 capacity 지정 |
| **데이터 구조 가이드 준수** | OwnedUnitData, InventoryItemData, ExplorationRecordData 등 기존 모델 그대로 활용 |
| **세미콜론 구분 string 처리** | editedDeck, rewards, eliminatedBossId 등 Split(';')/Join(";") 일관 적용 |
| **GameSettings SO** | 매직 넘버 금지 원칙 → 에디터 설정 SO로 대체 (주인공 ID, 파티/덱 크기 제한) |
| **순환 참조 회피** | Core→Meta 참조 없이 자체 등록 패턴으로 해결 |

---

## 4. 핵심 구현 사항

### 4.1 PlayerDataManager — 영속 데이터 중앙 관리

- **보유 유닛**: `List<OwnedUnitData>` — 유닛 추가/갱신, partyPosition 기반 파티 조회
- **인벤토리**: `List<InventoryItemData>` — productId 기반 조회, ownStack 가산/감산
- **탐험 기록**: `ExplorationRecordData` — 누적/이번 탐험 카운터, 카운터 초기화
- **보유 확인**: `HasUnit()`, `HasItem()`, `HasCard()` — ownStack + useStack > 0 기준
- **이벤트**: `OnUnitAdded`, `OnInventoryChanged`, `OnPartyChanged`

### 4.2 PartyEditManager — 파티 편성 시스템

**파티 편성**:
- 1~3명 유닛 배치 (GameSettings.MaxPartySize)
- 주인공 필수 포함 (GameSettings.ProtagonistUnitId)
- 슬롯 중복 시 기존 유닛 자동 해제
- 주인공 제거 / 최소 인원 미만 제거 차단

**덱 편집**:
- 4~6장 제한 (GameSettings.MinDeckSize/MaxDeckSize)
- **속성 검증**: Wild 유닛 → 모든 카드 | 그 외 → 동일 속성 + Wild 카드만
- ownStack/useStack 자동 관리 (장비 시 own--, use++ | 해제 시 역산)
- 세미콜론 구분 string 파싱: `editedDeck.Split(';')` ↔ `string.Join(";", cardIds)`

**스킬 편집**:
- `SkillData.unitId` 일치 검증 (유닛별 전용 스킬)
- 편집 스킬 없으면 `UnitData.initialSkillId` 반환

**아이템 편집**:
- 슬롯 1~2 (GameSettings.MaxItemSlots)
- 인벤토리 보유 확인 (ownStack > 0, category == Item)
- 기존 아이템 자동 해제 후 신규 장비

### 4.3 InventoryManager — 필터링/정렬

**카드 필터링**: ElementType, cost, CardType (null = 무시)
**아이템 필터링**: ItemType, ItemTargetStatus, isDisposable (null = 무시)
**5종 정렬 기준**: Name(이름), Element(속성), Rarity(레어도), Cost(코스트), Quantity(보유량)
**복합 API**: 필터 + 정렬 동시 적용

### 4.4 CampaignManager — 캠페인 시스템

**런타임 상태 관리** (마스터 데이터 SO 미변경):
- `Dictionary<string, bool> _campaignUnlockedState` — 해금 상태
- `Dictionary<string, bool> _campaignCompletionState` — 완료 상태
- `Dictionary<string, bool> _goalCompletionState` — 목표 완료 상태 (key: `groupId_sequence`)

**해금 트리거 평가** (CampaignTriggerType 9종):

| 트리거 | 평가 방법 |
|:---|:---|
| ClearCampaign | 해당 캠페인 _campaignCompletionState 확인 |
| ClearEvent | 향후 확장 (이벤트 ID 추적 필요) |
| EarnUnit | PlayerDataManager.HasUnit() |
| EarnCard | PlayerDataManager.HasCard() |
| EarnItem | PlayerDataManager.HasItem() |
| EarnSkill | 보유 유닛의 editedSkill 일치 확인 |
| BattleCount | ExplorationRecordData.countBattleAll ≥ 목표값 |
| MoveCount | 향후 확장 (moveCount 필드 추가 필요) |
| EventCount | 전투+VN+조우 총합 ≥ 목표값 |

**목표 진행**: sequence 오름차순, isEssential 미완료 시 다음 sequence 차단
**완료 판정**: isClearTrigger 목표 전부 완료 시 캠페인 완료
**보상 수집**: `rewards.Split(';')` + 목표별 `additionalRewards.Split(';')` 합산

### 4.5 ExpeditionLauncher — 탐험 개시

**5단계 검증**:
1. 파티 유효성 (1~3명, 주인공 포함)
2. 유닛별 덱 크기 (4~6장)
3. 덱 카드 속성 일치
4. 스킬 장비 유효성
5. 아이템 슬롯 수 (0~2개)

**카드 제외 규칙** (전투 덱 합성 시):

| 파티 인원 | 유닛당 포함 카드 | 비고 |
|:---:|:---:|:---|
| 1명 | 전부 (최대 6장) | 제외 없음 |
| 2명 | 최대 5장 | 6번째 카드 제외 |
| 3명 | 최대 4장 | 5~6번째 카드 제외 |

**탐험 개시 흐름**:
1. ValidateExpedition() → 5단계 검증 통과
2. ExplorationRecordData.countDepart++
3. 이번 탐험 카운터 초기화 (countBattleNow, countVisualNovelNow, countEncountNow = 0)
4. SceneTransitionManager.LoadScene(GameSettings.StageSceneName)

### 4.6 StageManager 연동 — OnExternalConditionCheck

`StageManager.InitializeStage()` 내에서 EventPlacementSystem 생성 후 콜백 연결:

```csharp
_eventPlacement.OnExternalConditionCheck = (trigger, value) =>
{
    switch (trigger)
    {
        case SpawnTrigger.OwnCharacter: return playerData.HasUnit(value);
        case SpawnTrigger.OwnItem: return playerData.HasItem(value);
        default: return false;
    }
};
```

Phase 3에서 미연결 상태였던 `OnExternalConditionCheck`가 이제 PlayerDataManager를 통해 완전히 동작한다.

### 4.7 GameBootstrapper 수정

- `[SerializeField] private DataManager _dataManager` 추가
- `InitializeServices()`에서 DataManager를 ServiceLocator에 등록
- 기존 SceneTransitionManager만 등록되던 것에서 DataManager 등록 추가

### 4.8 Addressable 패키지 추가

- `com.unity.addressables:2.4.3` 추가 (Unity 6000.3.x 호환)
- 실제 Addressable 그룹 설정 및 에셋 로딩은 UI Phase에서 구현 예정
- 아트 리소스 경로가 이미 Addressable ID로 설계되어 있으므로 패키지 선행 추가

---

## 5. 외부 의존성

| 의존 대상 | 용도 |
|:---|:---|
| `DataManager` (ServiceLocator) | 마스터 데이터 조회 (Units, Cards, Skills, Items, Campaigns, CampaignGoalGroups) |
| `ServiceLocator` (Core) | PlayerDataManager 등록/해제 |
| `SceneTransitionManager` (Core) | 스테이지 씬 전환 |
| `GameSettings` (Data) | 주인공 ID, 파티/덱 제한, 씬 이름 |
| `Data/Models/*` (Phase 0~1) | OwnedUnitData, InventoryItemData, ExplorationRecordData, CampaignData, CampaignGoalGroupData, UnitData, CardData, SkillData |
| `Data/Enums/*` (Phase 0~1) | InventoryCategory, ElementType, CardType, ItemType, ItemTargetStatus, CampaignTriggerType, Rarity, InventorySortType, SpawnTrigger |

---

## 6. 디렉터리 구조

```
Assets/_Project/Scripts/Runtime/Meta/
├── Campaign/
│   └── CampaignManager.cs
├── Inventory/
│   └── InventoryManager.cs
├── PartyEdit/
│   └── PartyEditManager.cs
├── PlayerDataManager.cs
├── ExpeditionLauncher.cs
└── ProjectStS.Meta.asmdef

Assets/_Project/Scripts/Runtime/Data/
├── GameSettings.cs              ← 신규
├── Enums/
│   └── InventoryEnums.cs        ← InventorySortType 추가
└── DataManager.cs               ← GameSettings 참조 추가
```

---

## 7. Unity 에디터 설정 항목

Phase 4 구현 후 Unity 에디터에서 수행해야 할 설정:

| # | 작업 | 상세 |
|:---:|:---|:---|
| 1 | GameSettings SO 생성 | `Create > ProjectStS > Data > GameSettings` → ProtagonistUnitId 설정 |
| 2 | DataManager에 GameSettings 할당 | DataManager 인스펙터의 Settings 필드에 SO 드래그 |
| 3 | GameBootstrapper에 DataManager 할당 | GameBootstrapper 인스펙터의 Data Manager 필드에 할당 |
| 4 | PlayerDataManager 배치 | DontDestroyOnLoad 오브젝트(GameBootstrapper와 동일 권장)에 컴포넌트 추가 |

---

## 8. 후속 작업

- [ ] Phase 4 UI: 로비 화면, 파티 편성 UI, 인벤토리 UI, 캠페인 UI
- [ ] Save/Load 시스템: PlayerDataManager의 JSON 직렬화/역직렬화 (Newtonsoft.Json)
- [ ] CampaignManager.ClearEvent 트리거 구현 (이벤트 ID 추적 필요)
- [ ] CampaignManager.MoveCount 트리거 구현 (ExplorationRecordData에 moveCount 필드 추가 검토)
- [ ] Phase 5: 통합 (GameFlowController, SaveLoadManager, AudioManager)

---

*마지막 업데이트: 2026.02.25 | 작성자: Claude Code*
