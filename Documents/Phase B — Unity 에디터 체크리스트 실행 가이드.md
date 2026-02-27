# Phase B: Unity 에디터 체크리스트 실행 가이드

## Context

Phase A(코드 수정 — 데이터/초기화 관련)가 완료되어 컴파일 에러 0건인 상태이다.
Phase B는 **사용자가 Unity 에디터에서 직접 수행**하는 체크리스트로, SO 생성/할당, 씬 설정, CSV 임포트, VN 연동, 런타임 테스트를 순서대로 진행한다.

---

## Step 1: 컴파일 확인 (Console 에러 0건)

Unity 에디터 열기 → Console 창에서 에러 0건 확인.
Phase A에서 이미 완료된 상태이므로 빠르게 넘어간다.

---

## Step 2: SO 생성 및 할당

### 2-1. SO 에셋 생성

모든 SO는 `Assets/_Project/ScriptableObjects/` 하위에 카테고리별 서브 폴더를 만들어 생성한다.
생성 방법: Project 창에서 우클릭 → `Create → ProjectStS/Data/{SO 이름}`

| # | SO 클래스 | 파일명 | 생성 위치 | 메뉴 경로 |
|---|-----------|--------|-----------|-----------|
| 0 | **GameSettings** | `GameSettings.asset` | `Assets/_Project/ScriptableObjects/` | `ProjectStS/Data/GameSettings` |
| 1 | UnitTableSO | `UnitTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/UnitTable` |
| 2 | CardTableSO | `CardTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/CardTable` |
| 3 | CardEffectTableSO | `CardEffectTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/CardEffectTable` |
| 4 | SkillTableSO | `SkillTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/SkillTable` |
| 5 | ItemTableSO | `ItemTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/ItemTable` |
| 6 | StatusEffectTableSO | `StatusEffectTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/StatusEffectTable` |
| 7 | EventTableSO | `EventTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/EventTable` |
| 8 | AreaTableSO | `AreaTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/AreaTable` |
| 9 | EnemyCombinationTableSO | `EnemyCombinationTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/EnemyCombinationTable` |
| 10 | AIPatternTableSO | `AIPatternTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/AIPatternTable` |
| 11 | AIPatternRuleTableSO | `AIPatternRuleTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/AIPatternRuleTable` |
| 12 | AIConditionTableSO | `AIConditionTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/AIConditionTable` |
| 13 | RewardTableSO | `RewardTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/RewardTable` |
| 14 | ElementAffinityTableSO | `ElementAffinityTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/ElementAffinityTable` |
| 15 | BattleActionTableSO | `BattleActionTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/BattleActionTable` |
| 16 | BattleTimelineTableSO | `BattleTimelineTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/BattleTimelineTable` |
| 17 | CampaignTableSO | `CampaignTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/CampaignTable` |
| 18 | CampaignGoalGroupTableSO | `CampaignGoalGroupTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/CampaignGoalGroupTable` |
| 19 | DropRateTableSO | `DropRateTable.asset` | `Assets/_Project/ScriptableObjects/Tables/` | `ProjectStS/Data/DropRateTable` |

**디렉터리 구조 요약:**
```
Assets/_Project/ScriptableObjects/
├── GameSettings.asset                 ← 전역 설정
└── Tables/                            ← 19개 마스터 데이터 테이블
    ├── UnitTable.asset
    ├── CardTable.asset
    ├── CardEffectTable.asset
    ├── SkillTable.asset
    ├── ItemTable.asset
    ├── StatusEffectTable.asset
    ├── EventTable.asset
    ├── AreaTable.asset
    ├── EnemyCombinationTable.asset
    ├── AIPatternTable.asset
    ├── AIPatternRuleTable.asset
    ├── AIConditionTable.asset
    ├── RewardTable.asset
    ├── ElementAffinityTable.asset
    ├── BattleActionTable.asset
    ├── BattleTimelineTable.asset
    ├── CampaignTable.asset
    ├── CampaignGoalGroupTable.asset
    └── DropRateTable.asset
```

### 2-2. GameSettings 초기값 설정

`GameSettings.asset` 인스펙터에서 다음 값을 확인/설정:

| 필드 | 기본값 | 설명 |
|------|--------|------|
| Protagonist Unit Id | *(주인공 유닛 ID — CSV 임포트 후 설정)* | 유닛 테이블의 주인공 유닛 id |
| Max Party Size | `3` | |
| Min Party Size | `1` | |
| Min Deck Size | `4` | |
| Max Deck Size | `6` | |
| Max Skill Count | `1` | |
| Max Item Slots | `2` | |
| Lobby Scene Name | `LobbyScene` | |
| Stage Scene Name | `StageScene` | |
| Battle Scene Name | `BattleScene` | |

### 2-3. DataManager에 SO 할당

**BootScene** → `GameBootstrapper` 오브젝트와 동일한 GameObject의 `DataManager` 컴포넌트 인스펙터:

| Header 그룹 | 필드 | 할당할 SO |
|-------------|------|-----------|
| **Unit / Card / Skill** | Unit Table | `UnitTable.asset` |
| | Card Table | `CardTable.asset` |
| | Card Effect Table | `CardEffectTable.asset` |
| | Skill Table | `SkillTable.asset` |
| **Item / StatusEffect** | Item Table | `ItemTable.asset` |
| | Status Effect Table | `StatusEffectTable.asset` |
| **Stage / Event / Area** | Event Table | `EventTable.asset` |
| | Area Table | `AreaTable.asset` |
| | Enemy Combination Table | `EnemyCombinationTable.asset` |
| **AI** | AI Pattern Table | `AIPatternTable.asset` |
| | AI Pattern Rule Table | `AIPatternRuleTable.asset` |
| | AI Condition Table | `AIConditionTable.asset` |
| **Reward / Element** | Reward Table | `RewardTable.asset` |
| | Element Affinity Table | `ElementAffinityTable.asset` |
| **Battle** | Battle Action Table | `BattleActionTable.asset` |
| | Battle Timeline Table | `BattleTimelineTable.asset` |
| **Campaign / DropRate** | Campaign Table | `CampaignTable.asset` |
| | Campaign Goal Group Table | `CampaignGoalGroupTable.asset` |
| | Drop Rate Table | `DropRateTable.asset` |
| **Settings** | Game Settings | `GameSettings.asset` |

### 2-4. CsvToSOImporter 윈도우에 SO 할당

메뉴 `ProjectStS → Data → CSV → SO Importer` 열기 → 각 슬롯에 Step 2-1에서 생성한 동일 SO 에셋 드래그.
(일괄 임포트 시 SO 참조가 모두 할당되어 있어야 함)

---

## Step 3: 씬 설정

### 3-1. Build Settings

`File → Build Settings` → 아래 순서로 씬 추가:

| Index | 씬 | 경로 |
|-------|-----|------|
| 0 | BootScene | `Assets/_Project/Scenes/BootScene.unity` |
| 1 | LobbyScene | `Assets/_Project/Scenes/LobbyScene.unity` |
| 2 | StageScene | `Assets/_Project/Scenes/StageScene.unity` |
| 3 | BattleScene | `Assets/_Project/Scenes/BattleScene.unity` |

### 3-2. BootScene 컴포넌트 배치

BootScene에 **하나의 DontDestroyOnLoad 루트 오브젝트** (예: `[Boot]`)를 만들고 아래 컴포넌트 배치:

| 컴포넌트 | 역할 | 인스펙터 설정 |
|----------|------|---------------|
| `GameBootstrapper` | 서비스 초기화 + 로비 전환 | `_dataManager` → 같은 오브젝트의 DataManager 컴포넌트<br>`_lobbySceneName` → `"LobbyScene"` |
| `DataManager` | 마스터 데이터 허브 | **Step 2-3** 참조하여 모든 SO 할당 |
| `IntegrationBootstrap` | SaveLoad, CampaignManager, GameFlowController 생성 | (인스펙터 설정 없음 — 자동 초기화) |

> **실행 순서**:
> `GameBootstrapper` (`[DefaultExecutionOrder(-100)]`) → `IntegrationBootstrap` (`[DefaultExecutionOrder(100)]`)
> GameBootstrapper.Awake()에서 ServiceLocator 초기화 + DataManager 등록이 먼저 완료된 후,
> IntegrationBootstrap.Start()에서 SaveLoadSystem, CampaignManager, GameFlowController가 생성된다.

### 3-3. LobbyScene 컴포넌트 배치

LobbyScene에 **PlayerDataManager**를 보유하는 오브젝트를 배치:

| 컴포넌트 | 역할 | 인스펙터 설정 |
|----------|------|---------------|
| `PlayerDataManager` | 플레이어 영속 데이터 관리 | (인스펙터 설정 없음 — Awake에서 ServiceLocator 자동 등록) |

> `PlayerDataManager`는 DontDestroyOnLoad이 **아닌** 씬 오브젝트.
> Awake에서 ServiceLocator에 자체 등록하므로, LobbyScene이 로드될 때 사용 가능해진다.

### 3-4. StageScene 컴포넌트 배치

| 컴포넌트 | 역할 | 인스펙터 설정 |
|----------|------|---------------|
| `StageSceneBootstrap` | 스테이지 초기화 | `_stageManager` → 같은 씬의 StageManager 컴포넌트 |
| `StageManager` | 월드맵 관리 | *(추후 구현 시 설정 — 현재는 빈 컴포넌트)* |

### 3-5. BattleScene 컴포넌트 배치

| 컴포넌트 | 역할 | 인스펙터 설정 |
|----------|------|---------------|
| `BattleSceneBootstrap` | 전투 초기화 | `_battleManager` → 같은 씬의 BattleManager 컴포넌트 |
| `BattleManager` | 전투 관리 | *(추후 구현 시 설정 — 현재는 빈 컴포넌트)* |

### 3-6. VisualNovelBridge 배치

VisualNovelBridge는 **BootScene의 DontDestroyOnLoad 오브젝트** (Step 3-2의 `[Boot]`)에 배치한다.
기존 VisualNovelPlayer 프리팹을 자식으로 인스턴스화하고 연결한다.

| 컴포넌트 | 역할 | 인스펙터 설정 |
|----------|------|---------------|
| `VisualNovelBridge` | VN ↔ 외부 시스템 인터페이스 | `_player` → VisualNovelPlayer 프리팹 인스턴스<br>`_vnRoot` → VN UI 루트 오브젝트 (Canvas 등) |

**배치 구조 예시:**
```
[Boot] (DontDestroyOnLoad)
├── GameBootstrapper
├── DataManager
├── IntegrationBootstrap
└── VisualNovelBridge
    └── VNRoot (비활성 상태)
        └── VisualNovelPlayer (프리팹 인스턴스)
            ├── TextController
            ├── PortraitController
            ├── ImageController
            └── SoundController
```

> VisualNovelBridge.Awake()에서 `ServiceLocator.Register<IVisualNovelBridge>(this)` 호출.
> `_vnRoot`는 비활성 상태로 시작하고, VN 재생 요청 시에만 활성화된다.

---

## Step 4: CSV 임포트

### 4-1. CSV 파일 작성

`Assets/_Project/Data/` 폴더를 생성하고, 아래 19개 CSV 파일을 배치한다.
**파일명은 CsvToSOImporter의 일괄 임포트 매핑에 정확히 맞춰야 한다.**

| # | CSV 파일명 | 대응 가이드 문서 | 최소 열 수 | 헤더 열 순서 |
|---|-----------|-----------------|-----------|-------------|
| 1 | `Units.csv` | 유닛 테이블 속성값.csv | 11 | `id`, `unitName`, `unitType`, `element`, `portraitPath`, `maxHp`, `maxEnergy`, `maxAP`, `initialDeckIds`, `initialSkillId`, `aiPatternId` |
| 2 | `Cards.csv` | 카드 테이블 속성값.csv | 14 | `id`, `cardName`, `description`, `artworkPath`, `rarity`, `element`, `cost`, `cardEffectId`, `cardType`, `targetType`, `targetFilter`, `targetSelectionRule`, `targetCount`, `isDisposable` |
| 3 | `CardEffects.csv` | 카드 효과 테이블 속성값.csv | 9 | `id`, `effectType`, `value`, `statusEffectId`, `modificationType`, `modDuration`, `cardTargetSelection`, `targetCardType`, `addCardId` |
| 4 | `Skills.csv` | 스킬 테이블 속성값.csv | 19 | `id`, `skillName`, `description`, `unitId`, `artworkPath`, `rarity`, `element`, `triggerTarget`, `triggerStatus`, `comparisonOperator`, `triggerValue`, `triggerElement`, `cardEffectId`, `targetType`, `targetFilter`, `targetSelectionRule`, `targetCount`, `limitType`, `limitValue` |
| 5 | `Items.csv` | 아이템 테이블 속성값.csv | 13 | `id`, `itemName`, `description`, `artworkPath`, `rarity`, `itemType`, `targetUnit`, `targetStatus`, `modifyValue`, `isDisposable`, `disposeTrigger`, `disposePercentage`, `stackCount` |
| 6 | `StatusEffects.csv` | 상태이상 테이블 속성값.csv | 15 | `id`, `effectName`, `description`, `iconPath`, `statusType`, `triggerTiming`, `effectType`, `effectElement`, `value`, `modifierType`, `isStackable`, `maxStacks`, `isExpendable`, `expendCount`, `duration` |
| 7 | `Events.csv` | 이벤트 테이블 속성값.csv | 13 | `areaId`, `id`, `eventType`, `eventValue`, `spawnTrigger`, `comparisonOperator`, `spawnTriggerValue`, `minLevel`, `maxLevel`, `rarity`, `rewardId`, `rewardMinCount`, `rewardMaxCount` |
| 8 | `Areas.csv` | 구역 테이블 속성값.csv | 15 | `id`, `name`, `description`, `areaLevelMin`, `areaLevelMax`, `areaCardinalPoint`, `logoImagePath`, `floorImagePath`, `skyboxPath`, `cellVisualNovelPath`, `cellEncountPath`, `cellBattleNormalPath`, `cellBattleElitePath`, `cellBattleBossPath`, `cellBattleEventPath` |
| 9 | `EnemyCombinations.csv` | 적 유닛 조합 테이블 속성값.csv | 9 | `id`, `name`, `description`, `waveCount`, `enemyUnit1`, `enemyUnit2`, `enemyUnit3`, `enemyUnit4`, `enemyUnit5` |
| 10 | `AIPatterns.csv` | 적 유닛 AI 패턴 테이블 속성값.csv | 6 | `id`, `patternName`, `description`, `defaultActionType`, `defaultCardId`, `defaultTargetSelection` |
| 11 | `AIPatternRules.csv` | 적 유닛 AI 패턴 룰 테이블 속성값.csv | 9 | `aiPatternId`, `ruleId`, `priority`, `actionType`, `cardId`, `targetSelection`, `speechLine`, `cutInEffect`, `zoomIn` |
| 12 | `AIConditions.csv` | 적 유닛 AI 컨디션 속성값.csv | 6 | `ruleId`, `conditionType`, `comparisonOperator`, `value`, `divisor`, `remainder` |
| 13 | `RewardTable.csv` | 보상 테이블 속성값.csv | 4 | `id`, `itemId`, `rarity`, `dropRate` |
| 14 | `ElementAffinity.csv` | 속성 간 상성 보정치 데이터 속성값.csv | 3 | `attackElement`, `targetElement`, `modValue` |
| 15 | `BattleActions.csv` | 전투 연출 행동 테이블 속성값.csv | 6 | `groupId`, `sequence`, `actionType`, `actionValue`, `targetUnit`, `waitNext` |
| 16 | `BattleTimelines.csv` | 전투 이벤트 타임라인 테이블 속성값.csv | 8 | `id`, `eventId`, `triggerTarget`, `triggerType`, `triggerValue`, `priority`, `isRepeatable`, `actionGroupId` |
| 17 | `Campaigns.csv` | 캠페인 테이블 속성값.csv | 10 | `id`, `name`, `description`, `artworkPath`, `unlockType`, `unlockId`, `groupId`, `rewards`, `isCompleted`, `afterComplete` |
| 18 | `CampaignGoalGroups.csv` | 캠페인 항목 그룹 테이블 속성값.csv | 10 | `groupId`, `sequence`, `name`, `description`, `isEssential`, `triggerType`, `triggerValue`, `additionalRewards`, `isClearTrigger`, `isCompleted` |
| 19 | `DropRates.csv` | 레어도에 따른 기본 드랍율 데이터 속성값.csv | 3 | `category`, `rarity`, `dropValue` |

### 4-2. CSV 작성 규칙

- **1행**은 헤더 (영문 필드명), **2행**부터 데이터
- CsvUtility는 첫 행을 `.Skip(1)`로 건너뜀
- **Enum** 값은 PascalCase 문자열로 입력 (예: `Sword`, `BattleNormal`, `Common`, `Hero`, `Enemy`)
- **bool** 값은 `true`/`false` 또는 `True`/`False`
- **int/float** 파싱 실패 시 기본값 `0` 적용
- **빈 문자열**은 Enum → default 값, string → `""` 으로 처리
- **description** 내 줄바꿈은 `\n` (리터럴 백슬래시 n)으로 표기 → 파서가 실제 줄바꿈으로 변환
- **쉼표가 포함된 값**은 `"값"` (따옴표)로 감싸기
- 가이드 문서(`Documents/데이터 구조 가이드/`)의 CSV를 참고하되, **열 순서는 위 표를 따르기**

### 4-3. CSV 임포트 실행

1. 메뉴 `ProjectStS → Data → CSV → SO Importer`로 에디터 윈도우 열기
2. 각 SO 슬롯에 Step 2-1에서 생성한 SO 에셋이 할당되었는지 확인
3. **CSV 폴더 경로**가 `Assets/_Project/Data`인지 확인
4. **"전체 CSV 일괄 임포트"** 버튼 클릭
5. Console에서 다음 로그 확인:
   - `[CsvToSOImporter] 일괄 임포트 완료: 19개 테이블`
   - 개별 테이블도 `[CsvToSOImporter] {이름} 임포트 완료: {N}건`

### 4-4. 임포트 검증

- 각 SO 에셋을 인스펙터에서 열어 `Entries` 리스트에 데이터가 올바르게 들어갔는지 확인
- `[CsvToSOImporter] Enum 파싱 실패` 경고가 있다면 해당 CSV 값 수정 후 재임포트
- 임포트 후 `GameSettings.asset`의 `Protagonist Unit Id`에 주인공 유닛 ID 입력

---

## Step 5: VN 연동 설정

### 5-1. 기본 연동 (현재 구현 범위)

현재 `VisualNovelBridge`는 episodeId를 키로 `VisualNovelSO`를 조회하여 재생하는 구조.

1. VN 에피소드 SO를 `Assets/VisualNovel/Data/` 또는 원하는 위치에 생성
2. VisualNovelBridge 인스펙터의 `Episode Registry`에 `(episodeId, VisualNovelSO)` 쌍 등록
3. 이벤트 테이블의 `eventValue` 필드에 해당 episodeId를 입력

**현재 VN → 외부 연동 흐름:**
```
EventTable(eventType=VisualNovel/Encounter, eventValue="episode_001")
    → GameFlowController.HandleEventTriggered()
    → StartVisualNovel(eventData)
    → vnBridge.PlayEpisode("episode_001", callback)
    → VisualNovelPlayer 재생
    → 완료 시 callback
    → HandleVNCompleted
    → StageManager.OnEventCompleted(id, true)
```

### 5-2. Encounter/Campaign VN 확장 — 필요 사양 정리

> **이 섹션은 별도 세션에서 구현할 수 있도록 구체적으로 작성되었다.**

---

#### 5-2-A. 문제 정의

**조우 이벤트 (Encounter) 규정** (각 씬(레이어)에 대한 규정.md):
> 조우 이벤트 진입 → 전투 스테이지 생성 → 초기 VN 재생 → 유저가 선택지 선택 →
> 초기 VN 재생 완료 → 선택지에 따른 후속 이벤트 재생 → 후속 이벤트 재생 완료 후 이벤트 클리어

**캠페인 규정** (동 문서):
> 캠페인 클리어 후 연출이 설정되어 있을 경우, 이를 재생한 다음 클리어 처리

**전투 내 VN 트리거** (동 문서):
> 특정 전투 이벤트에서 기획자가 배치한 트리거에 따라 VN 재생, 컷인 연출,
> 스탯 변경, 패턴 변경, 덱/손패 조작, 스킬 변경 등을 강제 실행

**현재 VN 시스템의 한계:**

| 한계 | 상세 |
|------|------|
| **VN → 외부 데이터 전달 불가** | Branch 선택 결과가 VN 내부에서만 소비됨. 선택지에 따라 "전투 시작", "아이템 획득" 등 외부 이벤트를 트리거할 수 없음 |
| **외부 → VN 컨텍스트 전달 불가** | 현재 파티 상태, 캠페인 진행도 등을 VN에 전달하여 조건 분기할 수 없음 |
| **VN 중간에 전투 삽입 불가** | VN 재생 중 전투를 시작하고, 결과에 따라 VN 분기를 계속할 수 없음 |
| **VN 완료 결과가 단순** | `onCompleted` 콜백만 있고, "어떤 선택을 했는지", "어떤 결과인지" 반환하지 않음 |

---

#### 5-2-B. 필요한 신규 노드 타입

##### 1) `GameEvent` 노드 — VN에서 외부 이벤트 트리거

**용도:** VN 재생 흐름 중 외부 게임 이벤트를 트리거하고, 결과를 받아 분기

```
┌──────────────────────┐
│    GameEvent Node     │
│                       │
│  eventAction: string  │  ← 실행할 액션 타입
│  eventParam: string   │  ← 액션 파라미터
│                       │
│  [Out 0] → onSuccess  │  ← 성공 시 다음 노드
│  [Out 1] → onFailure  │  ← 실패 시 다음 노드
└──────────────────────┘
```

**GameEvent 노드 필드 정의:**

| 필드 | 타입 | 설명 |
|------|------|------|
| `eventAction` | string (enum-like) | 실행할 액션 (아래 표 참조) |
| `eventParam` | string | 액션별 파라미터 (문자열 또는 세미콜론 구분) |

**지원할 eventAction 목록:**

| eventAction | eventParam 형식 | 동작 | 결과 분기 |
|-------------|-----------------|------|-----------|
| `StartBattle` | `"enemyCombId_001"` | VN 일시 중지 → 전투 시작 → 전투 결과 대기 → VN 재개 | Out 0 = 승리, Out 1 = 패배 |
| `GiveItem` | `"item_sword_01;2"` (아이템ID;수량) | 인벤토리/인게임 가방에 아이템 추가 | 항상 Out 0 |
| `GiveUnit` | `"unit_npc_01"` | 유닛 획득 처리 | 항상 Out 0 |
| `CheckCondition` | `"hasItem:item_key_01"` | 조건 체크 (아이템 보유 등) | Out 0 = 충족, Out 1 = 미충족 |
| `ModifyUnit` | `"unit_01;maxHP;+50"` (유닛ID;스탯;변화량) | 유닛 스탯 변경 | 항상 Out 0 |
| `SetFlag` | `"flag_tutorial_done;true"` (플래그명;값) | 세이브 데이터 플래그 설정 | 항상 Out 0 |
| `CompleteCampaignGoal` | `"goal_group_01;2"` (그룹ID;시퀀스) | 캠페인 목표 완료 처리 | 항상 Out 0 |

**구현 시 핵심 포인트:**
- `StartBattle`의 경우, VN 코루틴을 일시 중지(`yield return` 대기)하고 전투 씬 로드 → 전투 완료 시 VN 재개
- VisualNovelPlayer에 "일시 중지/재개" 메커니즘 추가 필요
- VN 외부의 `IGameEventHandler` 인터페이스를 통해 이벤트를 위임하고 결과를 콜백으로 수신

##### 2) `ContextBranch` 노드 — 외부 컨텍스트 기반 자동 분기

**용도:** 플레이어 상태(아이템 보유, 캠페인 진행도, 파티 구성 등)에 따른 자동 분기 (선택지 UI 없음)

```
┌───────────────────────────┐
│     ContextBranch Node    │
│                           │
│  conditionType: string    │  ← 조건 타입
│  conditionParam: string   │  ← 조건 파라미터
│                           │
│  [Out 0] → conditionTrue  │
│  [Out 1] → conditionFalse │
└───────────────────────────┘
```

| conditionType | conditionParam 형식 | 분기 기준 |
|---------------|---------------------|-----------|
| `HasItem` | `"item_id"` | 인벤토리에 해당 아이템 보유 여부 |
| `HasUnit` | `"unit_id"` | 해당 유닛 보유 여부 |
| `PartyContains` | `"unit_id"` | 현재 파티에 해당 유닛 포함 여부 |
| `CampaignCompleted` | `"campaign_id"` | 해당 캠페인 완료 여부 |
| `FlagSet` | `"flag_name"` | 세이브 데이터 플래그 체크 |
| `UnitStat` | `"unit_id;HP;>=;50%"` | 유닛 스탯 조건 체크 |

---

#### 5-2-C. 인터페이스 확장 사양

##### IVisualNovelBridge 확장

```csharp
// === 기존 ===
void PlayEpisode(string episodeId, Action onCompleted);
bool IsPlaying { get; }

// === 추가 필요 ===
/// <summary>
/// 컨텍스트 정보를 포함하여 에피소드를 재생하고, 결과를 반환한다.
/// Encounter/Campaign 연동 시 사용.
/// </summary>
void PlayEpisode(string episodeId, VNContext context, Action<VNResult> onCompleted);

/// <summary>
/// 현재 재생 중인 에피소드를 일시 중지한다. (GameEvent 노드에서 전투 등 실행 시)
/// </summary>
void PauseEpisode();

/// <summary>
/// 외부 이벤트 결과를 전달하여 일시 중지된 에피소드를 재개한다.
/// </summary>
void ResumeEpisode(GameEventResult result);
```

##### 신규 데이터 클래스

```csharp
/// <summary>
/// VN 재생 시 외부에서 전달하는 컨텍스트 데이터.
/// VisualNovelPlayer 내에서 ContextBranch 노드의 조건 평가에 사용된다.
/// </summary>
[System.Serializable]
public class VNContext
{
    /// <summary>현재 진행 중인 캠페인 ID (null 가능)</summary>
    public string activeCampaignId;

    /// <summary>현재 이벤트 ID</summary>
    public string eventId;

    /// <summary>전투 이벤트 타입 (Encounter, BattleEvent 등)</summary>
    public string eventType;

    /// <summary>커스텀 플래그 (key-value)</summary>
    public Dictionary<string, string> flags;
}

/// <summary>
/// VN 재생 완료 시 외부로 반환하는 결과 데이터.
/// </summary>
[System.Serializable]
public class VNResult
{
    /// <summary>VN이 정상 완료되었는지</summary>
    public bool isCompleted;

    /// <summary>마지막 선택지 인덱스 (-1이면 선택지 없었음)</summary>
    public int lastBranchChoice;

    /// <summary>GameEvent 노드에서 실행된 이벤트 목록</summary>
    public List<GameEventRecord> executedEvents;
}

/// <summary>
/// GameEvent 노드 실행 기록.
/// </summary>
[System.Serializable]
public class GameEventRecord
{
    public string eventAction;
    public string eventParam;
    public bool success;
}

/// <summary>
/// GameEvent 노드에서 외부 이벤트 실행 후 반환하는 결과.
/// </summary>
[System.Serializable]
public class GameEventResult
{
    public bool success;
    public string resultData;
}
```

##### IGameEventHandler 인터페이스 (VN → 외부 위임)

```csharp
/// <summary>
/// VN 시스템이 GameEvent 노드를 처리할 때 호출하는 핸들러.
/// GameFlowController 또는 전용 매니저가 구현한다.
/// ServiceLocator에 등록하여 VN 측에서 조회한다.
/// </summary>
public interface IGameEventHandler
{
    /// <summary>
    /// 게임 이벤트를 실행하고 결과를 콜백으로 반환한다.
    /// 전투 등 비동기 작업의 경우, 완료 시점에 콜백을 호출한다.
    /// </summary>
    void ExecuteGameEvent(string eventAction, string eventParam, Action<GameEventResult> onResult);

    /// <summary>
    /// 외부 컨텍스트 조건을 평가한다. ContextBranch 노드에서 사용.
    /// </summary>
    bool EvaluateCondition(string conditionType, string conditionParam);
}
```

---

#### 5-2-D. 구현 범위 및 우선순위

**Phase 1 (최소 기능 — Encounter 기본 동작):**
1. `GameEvent` 노드 타입 추가 (`NodeFieldData.cs`에 `GameEventNodeFields` 추가)
2. `VisualNovelPlayer`에 GameEvent 노드 실행 로직 추가 (ProcessNode의 switch case)
3. `IGameEventHandler` 인터페이스 정의 + ServiceLocator 등록
4. `IVisualNovelBridge`에 `PlayEpisode(id, context, callback)` 오버로드 추가
5. `GameFlowController`에서 Encounter 전용 흐름 구현:
   ```
   Encounter 진입 → VN 재생 → [GameEvent: StartBattle] → 전투 → 결과 → VN 재개 → 완료
   ```

**Phase 2 (캠페인 연동):**
1. `ContextBranch` 노드 타입 추가
2. `VNContext` 전달 메커니즘 구현
3. `CampaignManager`에서 캠페인 완료 후 VN 재생 흐름
4. `IGameEventHandler.EvaluateCondition()` 구현

**Phase 3 (전투 내 VN 트리거):**
1. `BattleTimeline` 시스템에서 VN 재생 트리거
2. 전투 중 VN 오버레이 (전투 일시 정지 → VN 재생 → 재개)
3. VN에서 전투 스탯 변경/패턴 변경 이벤트 처리 (`ModifyUnit`, `ChangePattern` 등)

**VN 에디터 확장 (각 Phase와 병행):**
1. 노드 에디터에 `GameEvent` / `ContextBranch` 노드 UI 추가
2. 출력 포트 2개 (success/failure 또는 true/false) 지원
3. `eventAction` 드롭다운 선택 UI
4. `conditionType` 드롭다운 선택 UI

---

#### 5-2-E. 영향 받는 파일 목록

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/VisualNovel/Scripts/Data/NodeFieldData.cs` | `GameEventNodeFields`, `ContextBranchNodeFields` 클래스 추가 |
| `Assets/VisualNovel/Scripts/Runtime/VisualNovelPlayer.cs` | ProcessNode에 GameEvent/ContextBranch case 추가, 일시 중지/재개 메커니즘 |
| `Assets/VisualNovel/Scripts/Runtime/VisualNovelBridge.cs` | `PlayEpisode` 오버로드, `PauseEpisode`/`ResumeEpisode`, `IGameEventHandler` 연동 |
| `Assets/_Project/Scripts/Runtime/Core/IVisualNovelBridge.cs` | 인터페이스 메서드 추가 |
| `Assets/_Project/Scripts/Runtime/Core/IGameEventHandler.cs` | **신규** — 인터페이스 정의 |
| `Assets/_Project/Scripts/Runtime/Data/VNContext.cs` | **신규** — VN 컨텍스트/결과 데이터 클래스 |
| `Assets/_Project/Scripts/Runtime/Integration/GameFlowController.cs` | Encounter 전용 HandleEventTriggered 분기, IGameEventHandler 구현 |
| `Assets/VisualNovel/Scripts/Editor/` (노드 에디터) | 신규 노드 타입 UI, 포트 2개 대응 |

---

## Step 6: 런타임 통합 테스트

### 6-1. 부트 시퀀스 테스트

1. **BootScene**에서 Play
2. Console에서 순서대로 확인:
   - `[GameBootstrapper] 서비스 초기화 완료.`
   - `[IntegrationBootstrap] 통합 서비스 초기화 완료.`
   - LobbyScene 로드 로그
3. **에러 없이** LobbyScene까지 전환되면 성공

### 6-2. DataManager 접근 테스트

LobbyScene에서 임시 테스트 스크립트 또는 Console 입력으로 확인:
```csharp
// 테스트 코드
if (ServiceLocator.TryGet<DataManager>(out var dm))
{
    Debug.Log($"Units: {dm.Units.Count}, Cards: {dm.Cards.Count}");
    Debug.Log($"Settings - MaxParty: {dm.Settings.MaxPartySize}");
    Debug.Log($"Settings - ProtagonistId: {dm.Settings.ProtagonistUnitId}");
}
else
{
    Debug.LogError("DataManager를 찾을 수 없습니다.");
}
```

### 6-3. CSV 데이터 무결성 체크

- 각 테이블의 `Count`가 CSV 데이터 행 수(헤더 제외)와 일치하는지 확인
- 특정 ID로 `GetById()` 호출하여 올바른 데이터 반환 확인
- `[CsvToSOImporter] Enum 파싱 실패` 경고가 없는지 Console 확인

### 6-4. VN 재생 테스트 (Step 5 기본 연동 완료 시)

1. 이벤트 테이블에 `eventType=VisualNovel`, `eventValue="test_episode"` 항목 추가
2. VisualNovelBridge의 Episode Registry에 해당 에피소드 등록
3. 스테이지에서 해당 이벤트 트리거 → VN 재생 → 완료 → 스테이지 복귀 확인

---

## 체크리스트 요약

- [ ] **Step 1**: 컴파일 에러 0건 확인
- [ ] **Step 2**: GameSettings + 19개 테이블 SO 생성 → DataManager 및 CsvToSOImporter에 할당
- [ ] **Step 3**: Build Settings 씬 순서 설정 + 각 씬 Bootstrap 컴포넌트 배치
- [ ] **Step 4**: CSV 파일 작성 → 일괄 임포트 → 데이터 검증
- [ ] **Step 5**: VisualNovelBridge 배치 + 에피소드 레지스트리 등록
- [ ] **Step 6**: 부트 시퀀스 → 데이터 접근 → VN 재생 순서로 통합 테스트

---

*작성: Claude Code | 2026.02.26*
