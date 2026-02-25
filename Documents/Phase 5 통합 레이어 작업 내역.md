# Phase 5: 통합 레이어 시스템 작업 내역

*작성일: 2026.02.25*
*커밋: `feat(integration): Phase 5 통합 레이어 구현 — 게임 루프, 저장/불러오기, VN 브릿지`*

---

## 1. 개요

Phase 5는 Phase 2~4에서 독립적으로 구축된 전투/스테이지/메타 레이어를 하나의 게임 루프로 연결하는 통합 계층이다.
씬 간 전환, 데이터 흐름, 저장/복원을 관장하는 오케스트레이터(GameFlowController)를 중심으로,
SaveLoadSystem(JSON 세이브/로드), VisualNovelBridge(VN 오버레이 연동), RewardSettlementProcessor(보상 정산),
Scene Bootstrap(씬별 초기화) 등을 구현하였다.

총 **11개 신규 파일**(C# 10개 + asmdef 1개), **1,388줄**이 신규 작성되었으며, 기존 **7개 파일**이 수정되었다.
UI/프리팹 없이 순수 로직만 구축하였다.

**네임스페이스**: `ProjectStS.Integration`, `ProjectStS.Data`, `ProjectStS.Core`, (default assembly)

---

## 2. 파일 목록 및 역할

### 2.1 신규 파일 (11개, 1,388줄)

#### Integration 어셈블리 (7개, 1,084줄)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Integration/GameFlowController.cs` | 448 | 게임 루프 오케스트레이터. Lobby→Stage→Battle/VN→Settlement→Lobby 전환 관리 |
| `Integration/SaveLoadSystem.cs` | 180 | JSON 세이브/로드. PlayerData + Campaign 상태를 Newtonsoft.Json으로 영속화 |
| `Integration/RewardSettlementProcessor.cs` | 189 | 보상 정산. 고정/완료/추가 보상 선택·적용, InGameBagItemData→InventoryItemData 변환 |
| `Integration/IntegrationBootstrap.cs` | 110 | 통합 서비스 초기화 부트스트랩. SaveLoadSystem·CampaignManager·GameFlowController 생성, 세이브 복원, 로비 전환 |
| `Integration/StageSceneBootstrap.cs` | 78 | 스테이지 씬 부트스트랩. StageManager 초기화 + GameFlowController 연결 |
| `Integration/BattleSceneBootstrap.cs` | 59 | 전투 씬 부트스트랩. BattleManager → GameFlowController 전달 |
| `Integration/ProjectStS.Integration.asmdef` | 20 | 통합 어셈블리 정의. Data, Core, Meta, Battle, Stage 모두 참조 |

#### 기타 어셈블리 (4개, 304줄)

| 파일 | 줄 수 | 어셈블리 | 역할 |
|:---|:---:|:---|:---|
| `Data/Enums/GameFlowEnums.cs` | 30 | Data | GameFlowPhase 열거형 (None, Boot, Lobby, Stage, Battle, VisualNovel, Settlement) |
| `Data/SaveData.cs` | 36 | Data | 세이브 데이터 직렬화 모델. 유닛/인벤토리/탐험기록/캠페인 상태 보존 |
| `Core/IVisualNovelBridge.cs` | 24 | Core | VN 시스템 연동 인터페이스. 어셈블리 경계 해결용 |
| `VisualNovel/VisualNovelBridge.cs` | 214 | default | IVisualNovelBridge 구현. 에피소드 레지스트리 + VN 오버레이 관리 |

### 2.2 기존 파일 수정 (7개)

| 파일 | 변경 내용 |
|:---|:---|
| `Data/GameSettings.cs` | `_lobbySceneName = "LobbyScene"`, `_battleSceneName = "BattleScene"` SerializeField + 프로퍼티 추가 |
| `Core/SceneTransitionManager.cs` | `UnloadScene(string, Action)` 메서드 + `UnloadSceneAsync` 코루틴 추가 (Additive 씬 언로드용) |
| `Meta/PlayerDataManager.cs` | `LoadData(List<OwnedUnitData>, List<InventoryItemData>, ExplorationRecordData)` 일괄 복원 메서드 추가 |
| `Meta/Campaign/CampaignManager.cs` | `GetCampaignUnlockedStates()`, `GetCampaignCompletionStates()`, `GetGoalCompletionStates()` — Dictionary 복사본 반환 getter 3개 추가 |
| `VisualNovel/VisualNovelPlayer.cs` | `OnEpisodeCompleted` 이벤트 추가 + `RunEpisode()` 완료 시 Invoke + `PlayEpisode(EpisodeData, Action)` 외부 재생 오버로드 추가 |
| `Core/GameBootstrapper.cs` | IntegrationBootstrap 컴포넌트 존재 시 로비 씬 직접 로드 스킵 + `LoadLobbyScene()` public으로 변경 + DataManager.Settings 경유 씬 이름 해석 |
| `Documents/현재 개발 상태.md` | Phase 5 완료 기록 추가 |

---

## 3. 아키텍처

### 3.1 어셈블리 의존성 구조

```
ProjectStS.Integration (NEW)
 ├→ ProjectStS.Data     (Enum, 모델, DataManager, GameSettings)
 ├→ ProjectStS.Core     (ServiceLocator, SceneTransitionManager, IVisualNovelBridge)
 ├→ ProjectStS.Meta     (PlayerDataManager, CampaignManager)
 ├→ ProjectStS.Battle   (BattleManager, BattleResult)
 └→ ProjectStS.Stage    (StageManager, StageSettlementData)

default assembly (VisualNovelBridge)
 ├→ ProjectStS.Core     (IVisualNovelBridge, ServiceLocator)
 └→ VisualNovelPlayer   (같은 default assembly)
```

**핵심 제약**: Core는 Data만 참조하므로 Integration/Meta/Battle/Stage 타입에 접근 불가.
→ Integration 어셈블리가 최상위 계층으로서 모든 레이어를 참조한다.

**VN 연동 제약**: VisualNovelPlayer는 asmdef이 없는 default assembly에 위치.
→ Core에 `IVisualNovelBridge` 인터페이스 정의, default assembly에서 구현체(`VisualNovelBridge`)를 ServiceLocator에 등록.

### 3.2 클래스 의존성

```
IntegrationBootstrap (MonoBehaviour, Boot 오브젝트)
 ├─ SaveLoadSystem 생성 → ServiceLocator 등록
 ├─ CampaignManager 생성 → ServiceLocator 등록
 ├─ GameFlowController 생성 (AddComponent) → Initialize(saveSystem)
 └─ 세이브 데이터 복원 → ApplyLoadedData()

GameFlowController (MonoBehaviour, DontDestroyOnLoad)
 ├→ SceneTransitionManager  ← 씬 로드/언로드
 ├→ DataManager             ← 씬 이름, 적 조합 조회
 ├→ PlayerDataManager       ← 파티 조회, 보상 적용
 ├→ IVisualNovelBridge      ← VN 재생 요청
 ├→ SaveLoadSystem          ← 자동 저장
 └→ CampaignManager         ← 목표 평가, 캠페인 완료

SaveLoadSystem (POCO, ServiceLocator 등록)
 ├→ PlayerDataManager       ← 데이터 읽기/쓰기
 └→ CampaignManager         ← 상태 읽기/쓰기

RewardSettlementProcessor (POCO, 정산 시 생성)
 └→ PlayerDataManager       ← 보상 아이템 추가

VisualNovelBridge (MonoBehaviour, VN 오버레이 프리팹)
 ├→ VisualNovelPlayer       ← 에피소드 재생 위임
 └→ ServiceLocator          ← IVisualNovelBridge 등록

StageSceneBootstrap (MonoBehaviour, 스테이지 씬)
 ├→ StageManager            ← 초기화
 ├→ PlayerDataManager       ← 파티/기록 조회
 └→ GameFlowController      ← OnStageReady() 호출

BattleSceneBootstrap (MonoBehaviour, 전투 씬)
 ├→ BattleManager           ← 참조 전달
 └→ GameFlowController      ← OnBattleReady() 호출
```

### 3.3 서비스 등록 흐름

```
GameBootstrapper.Awake()  [Core]
 ├─ ServiceLocator.Register(DataManager)
 └─ ServiceLocator.Register(SceneTransitionManager)

PlayerDataManager.Awake()  [Meta]
 └─ ServiceLocator.Register(this)

IntegrationBootstrap.Start()  [Integration, DefaultExecutionOrder(100)]
 ├─ ServiceLocator.Register(SaveLoadSystem)
 ├─ ServiceLocator.Register(CampaignManager)
 ├─ GameFlowController.Initialize() → ServiceLocator.Register(this)
 └─ SaveLoadSystem.ApplyLoadedData()

VisualNovelBridge.Awake()  [default, VN 오버레이 프리팹]
 └─ ServiceLocator.Register<IVisualNovelBridge>(this)
```

### 3.4 씬 로딩 전략

| 전환 | 방식 | 이유 |
|:---|:---|:---|
| Lobby → Stage | Single | 로비 완전 해제 |
| Stage → Battle | **Additive** | StageManager 상태 보존 |
| Battle 완료 → Stage | UnloadScene | Battle 씬만 언로드, Stage 재개 |
| Stage → VN/Encounter | 씬 전환 없음 | 프리팹 오버레이 (VisualNovelBridge) |
| Stage → Lobby | Single | 정산 완료 후 전체 교체 |

---

## 4. 핵심 구현 사항

### 4.1 GameFlowController — 게임 루프 오케스트레이션

**게임 루프 전체 흐름**:

```
Boot
 └→ IntegrationBootstrap.Start()
     └→ LoadLobbyScene()

Lobby (GameFlowPhase.Lobby)
 └→ StartExpedition()
     ├─ PlayerDataManager.GetPartyMembers()
     └─ SceneTransitionManager.LoadScene(StageScene, Single)

Stage (GameFlowPhase.Stage)
 ├→ OnStageReady(StageManager) ← StageSceneBootstrap 호출
 │   ├─ UIBridge.OnEventTriggered += HandleEventTriggered
 │   └─ OnStageEnded += HandleStageEnded
 │
 ├→ HandleEventTriggered(HexNode, EventData)
 │   ├─ BattleNormal/Elite/Boss/BattleEvent → StartBattle()
 │   └─ VisualNovel/Encounter → StartVisualNovel()
 │
 └→ HandleStageEnded(StageResult, StageEndReason)
     ├─ Failure → ReturnToLobby()
     └─ Victory → CalculateSettlement() → OnSettlementReady 발행

Battle (GameFlowPhase.Battle, Additive 씬)
 ├→ OnBattleReady(BattleManager) ← BattleSceneBootstrap 호출
 │   ├─ InitializeBattle(party, waves, eventId)
 │   └─ OnBattleEnded += HandleBattleEnded
 │
 └→ HandleBattleEnded(BattleResult)
     ├─ CleanupBattle() + UnloadScene(BattleScene)
     ├─ Victory → StageManager.OnEventCompleted() → Stage 복귀
     └─ Defeat → HandleStageEnded(Failure, PartyWipe)

VisualNovel (GameFlowPhase.VisualNovel, 오버레이)
 ├→ IVisualNovelBridge.PlayEpisode(eventValue, onCompleted)
 └→ HandleVNCompleted()
     └─ StageManager.OnEventCompleted() → Stage 복귀

Settlement (GameFlowPhase.Settlement)
 └→ CompleteSettlement(selectedRewards)
     ├─ 보상 인벤토리 반영
     ├─ 탐험 기록 갱신 (countComplete++)
     ├─ CampaignManager.EvaluateGoalProgress() + CheckAndCompleteCampaigns()
     ├─ SaveLoadSystem.Save() (자동 저장)
     └─ LoadScene(LobbyScene, Single) → Lobby 복귀
```

**이벤트 분기**:
- `EventType.BattleNormal/Elite/Boss/BattleEvent` → `StartBattle()` → Additive 씬 로드
- `EventType.VisualNovel/Encounter` → `StartVisualNovel()` → VN 오버레이

**적 웨이브 구성**: `eventValue`를 세미콜론으로 분할 → `DataManager.GetEnemyCombination(id)` → `waveCount` 기준 정렬

### 4.2 SaveLoadSystem — JSON 기반 세이브/로드

**저장 대상 (SaveData)**:
- `List<OwnedUnitData> ownedUnits` — 보유 유닛 전체
- `List<InventoryItemData> inventory` — 인벤토리 전체
- `ExplorationRecordData explorationRecord` — 탐험 누적 기록
- `Dictionary<string, bool> campaignUnlockedState` — 캠페인 해금 상태
- `Dictionary<string, bool> campaignCompletionState` — 캠페인 완료 상태
- `Dictionary<string, bool> goalCompletionState` — 목표 완료 상태
- `string trackedCampaignId` — 추적 캠페인 ID

**파일 경로**: `Application.persistentDataPath + "/save_data.json"`
**직렬화**: `Newtonsoft.Json`, `Formatting.Indented`
**버전 관리**: `SaveData.version = 1` (향후 마이그레이션용)

**API**:
- `Save(PlayerDataManager, CampaignManager)` → JSON 파일 쓰기
- `Load()` → JSON 파일 읽기 → SaveData 반환
- `ApplyLoadedData(SaveData, PlayerDataManager, CampaignManager)` → 각 매니저에 데이터 복원
- `HasSaveData()` / `DeleteSaveData()` — 파일 존재 확인 / 삭제

### 4.3 VisualNovelBridge — VN 오버레이 연동

**어셈블리 경계 해결 패턴**:
1. Core에 `IVisualNovelBridge` 인터페이스 정의 (PlayEpisode, IsPlaying)
2. default assembly에 `VisualNovelBridge : MonoBehaviour, IVisualNovelBridge` 구현
3. Awake에서 `ServiceLocator.Register<IVisualNovelBridge>(this)` 등록
4. GameFlowController가 `ServiceLocator.TryGet<IVisualNovelBridge>()` 으로 접근

**에피소드 레지스트리**:
- `List<EpisodeRegistryEntry>` — 인스펙터에서 (episodeId → VisualNovelSO) 매핑 설정
- 초기화 시 Dictionary 룩업 구축
- PlayEpisode 호출 시: ID→SO 조회 → SO→EpisodeData 변환 → VisualNovelPlayer.PlayEpisode(data, callback)

**VN 오버레이 관리**:
- `_vnRoot` GameObject 활성화/비활성화로 씬 전환 없이 오버레이 표시
- VisualNovelPlayer.PlayEpisode(EpisodeData, Action) 오버로드에 일회성 콜백 등록
- 재생 완료 시 _vnRoot 비활성화 + 외부 콜백 호출

### 4.4 RewardSettlementProcessor — 보상 정산

**정산 데이터 수신**: `StageSettlementData` (StageResultCalculator에서 계산)
- `FixedRewardCandidates` — 고정 보상 선택 대상 목록
- `FixedRewardSelectCount` — 선택 가능 수
- `CompletionRewards` — 완료 보상 (드랍 테이블)
- `BonusRewards` — 추가 보상 (엘리트/사건/VN)

**보상 적용 흐름**:
1. UI가 `GetFixedRewardCandidates()` 조회 → 선택지 표시
2. 플레이어 선택 → `SelectFixedRewards(List<int>)` 기록
3. `ApplyRewards(PlayerDataManager)` → InGameBagItemData→InventoryItemData 변환 → AddInventoryItem

### 4.5 IntegrationBootstrap — GameBootstrapper와의 분리

**문제**: GameBootstrapper는 Core 어셈블리 소속이므로 Integration/Meta 타입 참조 불가.
**해결**: 별도의 `IntegrationBootstrap` 컴포넌트를 동일 DontDestroyOnLoad 오브젝트에 배치.

```
[DefaultExecutionOrder(100)]  ← GameBootstrapper.Awake() 이후 실행 보장

IntegrationBootstrap.Start()
 1. SaveLoadSystem 생성 → ServiceLocator 등록
 2. CampaignManager 생성 (PlayerDataManager + DataManager 의존) → ServiceLocator 등록
 3. GameFlowController 생성 (AddComponent) → Initialize(saveSystem)
 4. 세이브 데이터 로드 → ApplyLoadedData()
 5. 로비 씬 로드
```

GameBootstrapper는 IntegrationBootstrap 존재 시 자체 로비 씬 로드를 스킵:
```csharp
if (GetComponent("IntegrationBootstrap") == null)
{
    LoadLobbyScene();
}
```

### 4.6 Scene Bootstrap 패턴

**StageSceneBootstrap** (스테이지 씬 루트):
1. PlayerDataManager에서 파티·탐험 기록 조회
2. 이번 탐험 카운터 초기화
3. StageManager.InitializeStage(party, record)
4. GameFlowController.OnStageReady(stageManager) 호출

**BattleSceneBootstrap** (전투 씬 루트):
1. GameFlowController.OnBattleReady(battleManager) 호출
2. InitializeBattle은 GameFlowController 내부에서 수행 (파티·웨이브 데이터 보유)

---

## 5. 외부 의존성

| 의존 대상 | 용도 |
|:---|:---|
| `DataManager` (ServiceLocator) | 씬 이름 조회 (GameSettings), 적 조합 조회 (GetEnemyCombination) |
| `ServiceLocator` (Core) | 모든 서비스 등록/해제/조회 |
| `SceneTransitionManager` (Core) | LoadScene (Single/Additive), UnloadScene |
| `PlayerDataManager` (Meta) | 파티 조회, 인벤토리 추가, 탐험 기록, 데이터 복원 |
| `CampaignManager` (Meta) | 목표 평가, 캠페인 완료, 상태 저장/복원 |
| `BattleManager` (Battle) | InitializeBattle, OnBattleEnded, CleanupBattle |
| `StageManager` (Stage) | InitializeStage, OnEventCompleted, OnStageEnded, CalculateSettlement |
| `StageUIBridge` (Stage) | OnEventTriggered 이벤트 구독 |
| `VisualNovelPlayer` (default) | PlayEpisode 에피소드 재생 위임 |
| `Newtonsoft.Json` (UPM) | SaveData JSON 직렬화/역직렬화 |

---

## 6. 디렉터리 구조

```
Assets/_Project/Scripts/Runtime/Integration/     ← 신규 디렉터리
├── ProjectStS.Integration.asmdef
├── IntegrationBootstrap.cs
├── GameFlowController.cs
├── SaveLoadSystem.cs
├── RewardSettlementProcessor.cs
├── StageSceneBootstrap.cs
└── BattleSceneBootstrap.cs

Assets/_Project/Scripts/Runtime/Data/
├── Enums/
│   └── GameFlowEnums.cs                         ← 신규
└── SaveData.cs                                   ← 신규

Assets/_Project/Scripts/Runtime/Core/
└── IVisualNovelBridge.cs                         ← 신규

Assets/VisualNovel/Scripts/Runtime/
└── VisualNovelBridge.cs                          ← 신규
```

---

## 7. Unity 에디터 설정 항목

Phase 5 구현 후 Unity 에디터에서 수행해야 할 설정:

| # | 작업 | 상세 |
|:---:|:---|:---|
| 1 | IntegrationBootstrap 배치 | GameBootstrapper와 동일 DontDestroyOnLoad 오브젝트에 `IntegrationBootstrap` 컴포넌트 추가 |
| 2 | GameSettings 씬 이름 설정 | GameSettings SO 인스펙터에서 `LobbySceneName`, `BattleSceneName` 확인/수정 (기본값: "LobbyScene", "BattleScene") |
| 3 | StageSceneBootstrap 배치 | 스테이지 씬 루트 오브젝트에 `StageSceneBootstrap` 컴포넌트 추가 + StageManager SerializeField 연결 |
| 4 | BattleSceneBootstrap 배치 | 전투 씬 루트 오브젝트에 `BattleSceneBootstrap` 컴포넌트 추가 + BattleManager SerializeField 연결 |
| 5 | VisualNovelBridge 배치 | VN 오버레이 프리팹/오브젝트에 `VisualNovelBridge` 컴포넌트 추가 |
| 6 | VisualNovelBridge 참조 연결 | `_player` (VisualNovelPlayer), `_vnRoot` (VN 루트 오브젝트) SerializeField 할당 |
| 7 | 에피소드 레지스트리 설정 | VisualNovelBridge 인스펙터에서 `Episode Registry` 목록에 (episodeId → VisualNovelSO) 매핑 추가 |
| 8 | 씬 빌드 설정 | File > Build Settings에 LobbyScene, StageScene, BattleScene 추가 |

---

## 8. 설계 원칙

| 원칙 | 적용 |
|:---|:---|
| **Phase 2/3/4 패턴 미러** | 오케스트레이터 + 서브시스템 + 이벤트 구독 구조 동일 |
| **어셈블리 계층 분리** | Integration이 모든 레이어를 참조하는 최상위 계층. Core→Integration 역참조 없음 |
| **인터페이스 경계 해결** | IVisualNovelBridge (Core) + VisualNovelBridge (default) 패턴으로 어셈블리 경계 해소 |
| **싱글톤 회피** | ServiceLocator 패턴 일관 사용 |
| **이벤트 구독 방식** | Update() 사용 없이 Action 이벤트로 변경 알림 |
| **Additive 씬 로딩** | 전투 씬을 Additive로 로드하여 Stage 상태 보존 |
| **VN 오버레이** | 씬 전환 없이 GameObject 활성화/비활성화로 VN 표시 |
| **자동 저장** | 정산 완료 시 자동 저장 (수동 저장 API도 제공) |
| **컬렉션 사전 할당** | 모든 List/Dictionary에 capacity 지정 |
| **Bootstrap 분리** | Core 어셈블리 제약을 깨지 않고 IntegrationBootstrap 별도 컴포넌트로 해결 |

---

## 9. 후속 작업

- [ ] Phase 2.5: 전투 UI — BattleUIBridge 이벤트 구독, 전투 씬 프리팹/레이아웃
- [ ] Phase 3.5: 스테이지 UI — StageUIBridge 이벤트 구독, 월드맵 씬 프리팹/레이아웃
- [ ] Phase 4 UI: 로비 화면, 파티 편성 UI, 인벤토리 UI, 캠페인 UI
- [ ] Phase 5 추가: AudioManager 구현
- [ ] CampaignManager.ClearEvent 트리거 구현 (이벤트 ID 추적 필요)
- [ ] CampaignManager.MoveCount 트리거 구현 (moveCount 필드 추가 검토)
- [ ] 정산 UI: OnSettlementReady 이벤트 구독 → 보상 선택 화면 표시 → CompleteSettlement 호출

---

*마지막 업데이트: 2026.02.25 | 작성자: Claude Code*
