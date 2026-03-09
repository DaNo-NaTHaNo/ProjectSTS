# Play 모드 구동 테스트 추가 작업 내역

*작성일: 2026.03.10*

---

## 1. 개요

마스터 플랜 Phase 0~5까지 모든 게임 시스템 코드 구현이 완료된 상태(148개 런타임 스크립트, 4개 씬, 19개 SO 테이블, 30+ UI 컴포넌트)에서 **Play 모드 구동이 불가능한 3대 원인**을 식별하고 해결하였다.

### 문제 진단

| # | 원인 | 영향 |
|:---:|:---|:---|
| 1 | **19개 CSV 데이터 파일이 전부 헤더만 존재** | SO 테이블이 전부 비어있어 모든 데이터 조회 실패 |
| 2 | **GameSettings._protagonistUnitId가 빈 문자열** | PartyEditManager.ValidateParty()에서 주인공 검증 실패 |
| 3 | **첫 실행 시 초기 데이터 생성 메커니즘 부재** | 세이브 파일 없으면 PlayerDataManager가 빈 상태로 유지 → 파티 없음 → 스테이지 진입 불가 |

### 해결 범위

- **Phase A**: 최소 실행 가능 테스트 CSV 데이터 16개 파일 작성 (68행)
- **Phase B**: GameSettings.asset에 주인공 유닛 ID 설정
- **Phase C**: FirstRunInitializer 신규 생성 + IntegrationBootstrap 부트 체인 수정
- **Phase E**: 4개 파일 방어 코드 보강

총 **1개 신규 파일** (C# 117줄), **5개 기존 파일 수정**, **16개 CSV 데이터 파일 작성**, **1개 SO 에셋 수정**.

**네임스페이스**: `ProjectStS.Integration`, `ProjectStS.Data`, `ProjectStS.Meta`, `ProjectStS.UI`

---

## 2. 파일 목록 및 역할

### 2.1 신규 파일 (1개, 117줄)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Integration/FirstRunInitializer.cs` | 117 | 세이브 없는 최초 실행 시 주인공 OwnedUnitData + 초기 덱 인벤토리 자동 생성 |

### 2.2 기존 파일 수정 (5개)

| 파일 | 변경 내용 |
|:---|:---|
| `Integration/IntegrationBootstrap.cs` | `EnsurePlayerHasData()` 메서드 추가 (12줄). Start() 부트 체인에 첫 실행 감지 로직 삽입 |
| `Integration/StageSceneBootstrap.cs` | 파티 없을 때 에러 로그 세분화 — 누락 서비스명, PartyCount, 힌트 메시지 추가 |
| `Integration/BattleSceneBootstrap.cs` | BattleManager/GameFlowController 누락 시 에러 로그 보강 — 인스펙터 할당 및 BootScene 시작 힌트 |
| `UI/Lobby/LobbyUIController.cs` | 매니저 누락 시 LogWarning→LogError 격상 + 보유 유닛 0일 때 경고 로그 추가 |
| `ScriptableObjects/GameSettings.asset` | `_protagonistUnitId: UNIT_PROTAGONIST` 설정 |

### 2.3 테스트 데이터 파일 (16개 CSV, 68행)

| CSV 파일 | 행 수 | 데이터 요약 |
|:---|:---:|:---|
| `CardEffects.csv` | 8 | Damage(10/15/20), Block(8/12), Heal(5), ApplyStatus, Draw |
| `StatusEffects.csv` | 3 | SE_BURN(화상), SE_STR(공격력↑), SE_ARMOR(방어력↑) |
| `AIPatterns.csv` | 2 | AI_BASIC(일반 공격), AI_BOSS(보스 교대 패턴) |
| `AIPatternRules.csv` | 3 | 보스 방어(짝수턴)/공격(홀수턴) + 일반 공격 |
| `AIConditions.csv` | 2 | TurnMod 짝수/홀수 분기 조건 |
| `Cards.csv` | 15 | Sword 5장, Baton 5장, Medal 5장 |
| `Skills.csv` | 3 | 주인공(연격), 마법사(화염술), 기사(철벽) |
| `Units.csv` | 5 | 아군 3명 + 적 2명 |
| `Items.csv` | 3 | 생명의 반지, 치유 포션, 부활석 |
| `EnemyCombinations.csv` | 2 | 고블린 조우, 오크 대장 보스전 |
| `Areas.csv` | 2 | 시작의 초원, 어두운 숲 |
| `Events.csv` | 4 | 일반전투×2, 보스전투, 비주얼노벨 |
| `RewardTable.csv` | 2 | 포션(Uncommon), 반지(Common) |
| `DropRates.csv` | 4 | SpawnEvent/EventReward별 레어도 확률 |
| `Campaigns.csv` | 1 | 첫 번째 여정 |
| `CampaignGoalGroups.csv` | 1 | 첫 전투 목표 |

---

## 3. Phase A: 테스트 CSV 데이터 작성

### 3.1 설계 원칙

- **최소 실행 가능 데이터(MVP)**: 게임 루프(로비→스테이지→전투→정산→로비) 1회전을 돌릴 수 있는 최소한의 데이터
- **의존 관계 순서**: 리프 데이터(CardEffects, StatusEffects, AI) → 참조 데이터(Cards, Skills, Units) → 상위 데이터(Events, Areas) 순으로 작성
- **교차 참조 무결성**: 모든 FK 관계가 실제 존재하는 ID를 참조하도록 검증
- **헤더 오타 보존**: CSV 파서가 인덱스 기반(열 이름 무시)이므로 기존 헤더의 오타(`cathegory`, `floarImagePath`, `isEssencial`, `additianalRewards`, `actionGrorpId`)를 그대로 유지

### 3.2 데이터 계층 구조

```
CardEffects (8행)  ←── Cards.cardEffectId, Skills.cardEffectId
StatusEffects (3행) ←── CardEffects.statusEffectId
AIPatterns (2행)   ←── Units.aiPatternId
AIPatternRules (3행) ←── AIPatterns.id
AIConditions (2행) ←── AIPatternRules.ruleId

Cards (15행)       ←── Units.initialDeckIds, AIPatternRules.cardId
Skills (3행)       ←── Units.initialSkillId
Units (5행)        ←── EnemyCombinations.enemyUnit_*, Skills.unitId

Items (3행)        ←── RewardTable.itemId
EnemyCombinations (2행) ←── Events.eventValue
Areas (2행)        ←── Events.areaId
Events (4행)       ←── (스테이지 이벤트 스폰)

RewardTable (2행)  ←── Events.rewardId
DropRates (4행)    ←── (드랍율 계산)
Campaigns (1행)    ←── CampaignGoalGroups.groupId
CampaignGoalGroups (1행)
```

### 3.3 CardEffects.csv — 8행

카드 효과의 모든 주요 타입을 커버하는 기초 효과 세트.

| ID | effectType | value | statusEffectId | 설명 |
|:---|:---|:---:|:---|:---|
| CE_DMG_10 | Damage | 10 | | 기본 공격 |
| CE_DMG_15 | Damage | 15 | | 중급 공격 |
| CE_DMG_20 | Damage | 20 | | 강 공격 |
| CE_BLK_8 | Block | 8 | | 기본 방어 |
| CE_BLK_12 | Block | 12 | | 강 방어 |
| CE_HEAL_5 | Heal | 5 | | 기본 회복 |
| CE_STATUS_BURN | ApplyStatus | 1 | SE_BURN | 화상 부여 |
| CE_DRAW_1 | Draw | 1 | | 카드 드로우 |

### 3.4 StatusEffects.csv — 3행

Buff/Debuff 양측과 주요 트리거 타이밍(TurnStart, OnAttack, OnDamage)을 테스트.

| ID | statusType | triggerTiming | effectType | element | value | maxStacks | duration |
|:---|:---|:---|:---|:---|:---:|:---:|:---:|
| SE_BURN | Debuff | TurnStart | DamageOverTime | Baton | 3 | 5 | 3 |
| SE_STR | Buff | OnAttack | ModifyDamage | Wild | 2 | 3 | 3 |
| SE_ARMOR | Buff | OnDamage | ReduceDamage | Wild | 2 | 3 | 3 |

### 3.5 AIPatterns.csv + AIPatternRules.csv + AIConditions.csv

**AI 행동 트리 구성**:

```
AI_BASIC (일반 적)
 └─ RULE_BASIC_ATK (priority 1): PlayCard CARD_SW_ATK1 → LowestHp
    └─ (조건 없음 → 항상 실행)

AI_BOSS (보스)
 ├─ RULE_BOSS_DEF (priority 1): PlayCard CARD_MD_DEF1 → Self
 │   └─ TurnMod: divisor=2, remainder=0 → 짝수 턴
 └─ RULE_BOSS_ATK (priority 2): PlayCard CARD_MD_ATK2 → LowestHp
     └─ TurnMod: divisor=2, remainder=1 → 홀수 턴
```

보스는 홀짝 턴 교대로 공격/방어를 수행한다. `RULE_BOSS_DEF`가 priority 1이므로 짝수 턴에는 방어가 우선 적용되고, 홀수 턴에는 해당 조건 불충족 → `RULE_BOSS_ATK`(priority 2)가 실행된다.

### 3.6 Cards.csv — 15행 (3속성 × 5장)

각 속성별 공격/방어/유틸리티 혼합 구성:

| 속성 | 카드 | 코스트 | 효과 | 레어도 |
|:---|:---|:---:|:---|:---|
| **Sword** | CARD_SW_ATK1 바람의 일격 | 1 | 10 대미지 | Common |
| | CARD_SW_ATK2 질풍참 | 2 | 15 대미지 | Common |
| | CARD_SW_DEF1 바람 방벽 | 1 | 8 방어도 | Common |
| | CARD_SW_DRAW 순풍 | 0 | 1장 드로우 | Uncommon |
| | CARD_SW_ATK3 폭풍의 검 | 3 | 20 대미지 | Rare |
| **Baton** | CARD_BT_ATK1 불꽃 타격 | 1 | 10 대미지 | Common |
| | CARD_BT_ATK2 화염 돌진 | 2 | 15 대미지 | Common |
| | CARD_BT_DEF1 화염 방패 | 1 | 8 방어도 | Common |
| | CARD_BT_BURN 방화 | 1 | 화상 부여 | Uncommon |
| | CARD_BT_ATK3 대화염 | 3 | 20 대미지 | Rare |
| **Medal** | CARD_MD_ATK1 대지의 일격 | 1 | 10 대미지 | Common |
| | CARD_MD_ATK2 지진 | 2 | 15 대미지 | Common |
| | CARD_MD_DEF1 대지의 방패 | 2 | 12 방어도 | Common |
| | CARD_MD_HEAL 대지의 축복 | 1 | 5 회복 | Uncommon |
| | CARD_MD_ATK3 산사태 | 3 | 20 대미지 | Rare |

### 3.7 Skills.csv — 3행

각 아군 유닛에 1개씩, 서로 다른 트리거 조건으로 스킬 시스템을 테스트:

| ID | 유닛 | 트리거 | 효과 | 제한 |
|:---|:---|:---|:---|:---|
| SK_PROTAGONIST | 주인공 | Self가 Sword 카드 사용 시 | 적에게 10 대미지 (CE_DMG_10) | PerTurn 1회 |
| SK_MAGE | 마법사 | 적에게 Baton 상태이상 ≥1 시 | 적에게 화상 부여 (CE_STATUS_BURN) | PerTurn 1회 |
| SK_KNIGHT | 기사 | Self가 Medal 카드 사용 시 | 자신에게 8 방어도 (CE_BLK_8) | PerTurn 1회 |

### 3.8 Units.csv — 5행

전투 루프 E2E 테스트에 필요한 최소 유닛 구성:

| ID | 이름 | 타입 | 속성 | HP | Energy | AP | 덱 | 스킬 | AI |
|:---|:---|:---|:---|:---:|:---:|:---:|:---|:---|:---|
| UNIT_PROTAGONIST | 주인공 | Ally | Sword | 80 | 3 | 5 | SW 5장 | SK_PROTAGONIST | — |
| UNIT_MAGE | 마법사 | Ally | Baton | 60 | 2 | 3 | BT 5장 | SK_MAGE | — |
| UNIT_KNIGHT | 기사 | Ally | Medal | 100 | 2 | 4 | MD 5장 | SK_KNIGHT | — |
| UNIT_GOBLIN | 고블린 | Enemy | Wild | 30 | 0 | 0 | SW_ATK1 ×1 | — | AI_BASIC |
| UNIT_BOSS_ORC | 오크 대장 | Enemy | Medal | 100 | 0 | 0 | MD 3장 | — | AI_BOSS |

### 3.9 Items.csv — 3행

3종 아이템 타입(Equipment/HasDamage/HasDown)을 커버:

| ID | 이름 | itemType | targetStatus | modifyValue | isDisposable | stackCount |
|:---|:---|:---|:---|:---:|:---|:---:|
| ITEM_HP_RING | 생명의 반지 | Equipment | MaxHP | +10 | false | 1 |
| ITEM_POTION | 치유 포션 | HasDamage | NowHP | +5 | true (Used) | 3 |
| ITEM_REVIVE | 부활석 | HasDown | NowHP | +20 | true (HasDown) | 1 |

### 3.10 나머지 데이터

- **EnemyCombinations**: 고블린 1마리 조우(EC_GOBLIN_1), 오크 대장 보스전(EC_BOSS_ORC)
- **Areas**: 시작의 초원(AREA_START, 레벨 1~3, N/NE/NW), 어두운 숲(AREA_FOREST, 레벨 2~5, S/SE/SW)
- **Events**: 일반전투(AREA_START), 보스전투(AREA_START), VN(AREA_START), 일반전투(AREA_FOREST)
- **RewardTable**: RW_01에 포션(Uncommon 50%)과 반지(Common 30%)
- **DropRates**: SpawnEvent — Common 0.6, Rare 0.2 / EventReward — Common 0.5, Rare 0.3
- **Campaigns**: CAMP_01 "첫 번째 여정", 즉시 해금, GRP_01 참조
- **CampaignGoalGroups**: GRP_01 "첫 전투", BattleCount ≥ 1, isClearTrigger=true

---

## 4. Phase B: GameSettings 구성

### 4.1 변경 내용

`Assets/_Project/ScriptableObjects/GameSettings.asset`의 `_protagonistUnitId` 필드를 빈 문자열에서 `UNIT_PROTAGONIST`로 설정.

```yaml
# Before
_protagonistUnitId:

# After
_protagonistUnitId: UNIT_PROTAGONIST
```

### 4.2 영향 범위

- `PartyEditManager.ValidateParty()` — 파티 내 주인공 존재 검증 통과
- `PartyEditManager.IsProtagonist(string)` — 주인공 파티 이탈 방지
- `ExpeditionLauncher.ValidateExpedition()` — 탐험 개시 전 파티 검증 통과
- `FirstRunInitializer.InitializeNewPlayer()` — 주인공 ID로 UnitTable 조회

---

## 5. Phase C: 첫 실행 초기화 메커니즘 구현

### 5.1 문제 분석

기존 부트 체인:

```
IntegrationBootstrap.Start()
 ├─ InitializeIntegrationServices()
 ├─ LoadSaveData()           ← 세이브 없으면 return (PlayerDataManager 빈 상태)
 └─ LoadLobbyScene()         ← 빈 PlayerDataManager로 로비 진입 → 파티 없음
```

세이브 파일이 없는 최초 실행 시 `LoadSaveData()`가 아무 데이터도 복원하지 않고 조기 반환한다. 이후 `LoadLobbyScene()`이 호출되면 `PlayerDataManager`가 비어있어 파티 편성, 탐험 개시 등 모든 기능이 동작하지 않는다.

### 5.2 해결: FirstRunInitializer (신규 파일)

**파일**: `Assets/_Project/Scripts/Runtime/Integration/FirstRunInitializer.cs`
**네임스페이스**: `ProjectStS.Integration`
**줄 수**: 117줄

정적 클래스로 구현. `InitializeNewPlayer(PlayerDataManager, DataManager)` 메서드 1개.

**실행 흐름**:

```
InitializeNewPlayer(playerData, dataManager)
 ├─ GameSettings.ProtagonistUnitId 조회  → "UNIT_PROTAGONIST"
 ├─ DataManager.GetUnit(protagonistId)   → UnitData 조회
 ├─ OwnedUnitData 생성
 │   ├─ unitId        = "UNIT_PROTAGONIST"
 │   ├─ cardElement   = ElementType.Sword  (UnitData.element에서 복사)
 │   ├─ editedDeck    = "CARD_SW_ATK1;CARD_SW_ATK2;CARD_SW_DEF1;CARD_SW_DRAW;CARD_SW_ATK3"
 │   ├─ editedSkill   = "SK_PROTAGONIST"
 │   ├─ partyPosition = 1                 (즉시 파티 편성)
 │   └─ equipItem1/2  = ""
 ├─ playerData.AddUnit(ownedUnit)
 └─ RegisterInitialDeckToInventory()
     └─ 카드 5장 각각 InventoryItemData 생성
         ├─ category   = InventoryCategory.Card
         ├─ productId  = CardData.id
         ├─ ownStack   = 0    (덱에 편성된 상태)
         ├─ useStack   = 1    (사용 중)
         ├─ cardElement = CardData.element
         ├─ cardType   = CardData.cardType
         └─ cardCost   = CardData.cost
```

**설계 결정 — ownStack=0, useStack=1**:

초기 덱 카드는 OwnedUnitData.editedDeck에 이미 편성된 상태이므로, 인벤토리에서는 `useStack=1`(사용 중)으로 등록한다. 이는 `PartyEditManager.RestoreDeckStacks()`에서 덱 해제 시 `ownStack++, useStack--`로 복원하는 기존 로직과 일관성을 유지한다.

### 5.3 IntegrationBootstrap 수정

**변경 후 부트 체인**:

```
IntegrationBootstrap.Start()
 ├─ InitializeIntegrationServices()
 ├─ LoadSaveData()           ← 세이브 없으면 조기 반환
 ├─ EnsurePlayerHasData()    ← ★ 추가: 유닛 0이면 FirstRunInitializer 호출
 └─ LoadLobbyScene()
```

**EnsurePlayerHasData() 구현** (12줄):

```csharp
private void EnsurePlayerHasData()
{
    if (ServiceLocator.TryGet<PlayerDataManager>(out var playerData)
        && ServiceLocator.TryGet<DataManager>(out var dataManager))
    {
        if (playerData.GetOwnedUnits().Count == 0)
        {
            FirstRunInitializer.InitializeNewPlayer(playerData, dataManager);
            Debug.Log("[IntegrationBootstrap] 첫 실행 — 초기 플레이어 데이터 생성 완료.");
        }
    }
}
```

**조건 분기**: `GetOwnedUnits().Count == 0`으로 판단하므로, 세이브가 있는 기존 사용자에게는 영향 없음. 세이브 복원 후에도 유닛이 비어있는 비정상 상태에서도 안전망으로 동작.

---

## 6. Phase E: 방어 코드 보강

데이터 누락/불완전 시 크래시 대신 디버깅에 도움이 되는 에러 메시지를 출력하도록 보강.

### 6.1 StageSceneBootstrap — 파티 없을 때 원인 세분화

**Before**:
```csharp
Debug.LogError("[StageSceneBootstrap] 파티 데이터가 없습니다.");
```

**After**:
```csharp
bool hasPlayerData = ServiceLocator.TryGet<PlayerDataManager>(out _);
bool hasDataManager = ServiceLocator.TryGet<DataManager>(out _);
Debug.LogError($"[StageSceneBootstrap] 파티 데이터가 없습니다. " +
    $"PlayerDataManager={hasPlayerData}, DataManager={hasDataManager}, " +
    $"PartyCount={party?.Count ?? 0}. " +
    "BootScene에서 시작했는지, GameSettings.ProtagonistUnitId가 설정되었는지 확인하세요.");
```

### 6.2 BattleSceneBootstrap — 누락 참조 힌트

**Before**:
```csharp
Debug.LogError("[BattleSceneBootstrap] BattleManager 참조가 없습니다.");
// ...
Debug.LogWarning("[BattleSceneBootstrap] GameFlowController를 찾을 수 없습니다.");
```

**After**:
```csharp
Debug.LogError("[BattleSceneBootstrap] BattleManager 참조가 없습니다. " +
    "BattleScene의 인스펙터에서 BattleManager 컴포넌트를 할당해주세요.");
// ...
Debug.LogError("[BattleSceneBootstrap] GameFlowController를 찾을 수 없습니다. " +
    "BootScene에서 게임을 시작했는지 확인하세요.");
```

`LogWarning` → `LogError` 격상: GameFlowController 누락은 전투 초기화 완전 실패를 의미하므로 Error 수준이 적절.

### 6.3 LobbyUIController — 빈 데이터 경고

매니저 누락 시 `LogWarning` → `LogError` 격상 + BootScene 시작 힌트 추가. 보유 유닛 0개일 때 첫 실행 초기화 미수행 가능성을 경고:

```csharp
if (_playerData.GetOwnedUnits().Count == 0)
{
    Debug.LogWarning("[LobbyUIController] 보유 유닛이 없습니다. 첫 실행 초기화가 수행되지 않았을 수 있습니다.");
}
```

### 6.4 PartyEditManager — 변경 불필요

기존 `GetEditedDeck()`에서 `string.IsNullOrEmpty(unit.editedDeck)` 체크 + `ParseSemicolonList()`에서 null/빈값 처리가 이미 충분히 방어적. 추가 변경 없음.

---

## 7. 교차 참조 검증 맵

테스트 데이터 간 모든 FK 관계의 무결성을 보장하기 위한 참조 맵:

```
Card.cardEffectId    → CardEffect.id
  CARD_SW_ATK1  → CE_DMG_10    ✓
  CARD_SW_ATK2  → CE_DMG_15    ✓
  CARD_SW_DEF1  → CE_BLK_8     ✓
  CARD_SW_DRAW  → CE_DRAW_1    ✓
  CARD_SW_ATK3  → CE_DMG_20    ✓
  CARD_BT_ATK1  → CE_DMG_10    ✓
  CARD_BT_ATK2  → CE_DMG_15    ✓
  CARD_BT_DEF1  → CE_BLK_8     ✓
  CARD_BT_BURN  → CE_STATUS_BURN ✓
  CARD_BT_ATK3  → CE_DMG_20    ✓
  CARD_MD_ATK1  → CE_DMG_10    ✓
  CARD_MD_ATK2  → CE_DMG_15    ✓
  CARD_MD_DEF1  → CE_BLK_12    ✓
  CARD_MD_HEAL  → CE_HEAL_5    ✓
  CARD_MD_ATK3  → CE_DMG_20    ✓

CardEffect.statusEffectId → StatusEffect.id
  CE_STATUS_BURN → SE_BURN     ✓

Unit.initialDeckIds → Card.id
  UNIT_PROTAGONIST → CARD_SW_ATK1;SW_ATK2;SW_DEF1;SW_DRAW;SW_ATK3  ✓
  UNIT_MAGE       → CARD_BT_ATK1;BT_ATK2;BT_DEF1;BT_BURN;BT_ATK3  ✓
  UNIT_KNIGHT     → CARD_MD_ATK1;MD_ATK2;MD_DEF1;MD_HEAL;MD_ATK3    ✓
  UNIT_GOBLIN     → CARD_SW_ATK1                                      ✓
  UNIT_BOSS_ORC   → CARD_MD_ATK1;MD_ATK2;MD_DEF1                     ✓

Unit.initialSkillId → Skill.id
  UNIT_PROTAGONIST → SK_PROTAGONIST  ✓
  UNIT_MAGE       → SK_MAGE         ✓
  UNIT_KNIGHT     → SK_KNIGHT       ✓

Unit.aiPatternId → AIPattern.id
  UNIT_GOBLIN     → AI_BASIC   ✓
  UNIT_BOSS_ORC   → AI_BOSS    ✓

Skill.unitId → Unit.id
  SK_PROTAGONIST → UNIT_PROTAGONIST  ✓
  SK_MAGE       → UNIT_MAGE         ✓
  SK_KNIGHT     → UNIT_KNIGHT       ✓

Skill.cardEffectId → CardEffect.id
  SK_PROTAGONIST → CE_DMG_10        ✓
  SK_MAGE       → CE_STATUS_BURN    ✓
  SK_KNIGHT     → CE_BLK_8          ✓

AIPatternRule.aipatternId → AIPattern.id
  RULE_BOSS_DEF  → AI_BOSS    ✓
  RULE_BOSS_ATK  → AI_BOSS    ✓
  RULE_BASIC_ATK → AI_BASIC   ✓

AICondition.ruleId → AIPatternRule.ruleId
  RULE_BOSS_DEF  ✓
  RULE_BOSS_ATK  ✓

Event.areaId → Area.id
  EVT_BATTLE_01   → AREA_START   ✓
  EVT_BATTLE_BOSS → AREA_START   ✓
  EVT_VN_01       → AREA_START   ✓
  EVT_BATTLE_02   → AREA_FOREST  ✓

Event.eventValue → EnemyCombination.id / episodeId
  EVT_BATTLE_01   → EC_GOBLIN_1  ✓
  EVT_BATTLE_BOSS → EC_BOSS_ORC  ✓
  EVT_VN_01       → EP_TEST_01   (VN 에피소드, 브릿지 없으면 스킵)
  EVT_BATTLE_02   → EC_GOBLIN_1  ✓

EnemyCombination.enemyUnit_* → Unit.id
  EC_GOBLIN_1 → UNIT_GOBLIN    ✓
  EC_BOSS_ORC → UNIT_BOSS_ORC  ✓

RewardTable.itemId → Item.id
  RW_01 → ITEM_POTION   ✓
  RW_01 → ITEM_HP_RING  ✓

Campaign.groupId → CampaignGoalGroup.groupId
  CAMP_01 → GRP_01  ✓
```

---

## 8. 부트 체인 변경 전후 비교

### Before (Phase 5 완료 시점)

```
GameBootstrapper.Awake()                    [Core]
 ├─ ServiceLocator.Register(DataManager)
 └─ ServiceLocator.Register(SceneTransitionManager)

PlayerDataManager.Awake()                   [Meta]
 └─ ServiceLocator.Register(this)            ← _ownedUnits = 빈 리스트

IntegrationBootstrap.Start()                [Integration, ExecutionOrder=100]
 ├─ InitializeIntegrationServices()
 │   ├─ SaveLoadSystem 등록
 │   ├─ CampaignManager 등록
 │   └─ GameFlowController 생성
 ├─ LoadSaveData()                           ← 세이브 없음 → return
 └─ LoadLobbyScene()                         ← 빈 PlayerData로 로비 → 파티 없음 ✗
```

### After (이번 작업 완료)

```
GameBootstrapper.Awake()                    [Core]
 ├─ ServiceLocator.Register(DataManager)     ← SO 테이블에 68행 테스트 데이터 존재
 └─ ServiceLocator.Register(SceneTransitionManager)

PlayerDataManager.Awake()                   [Meta]
 └─ ServiceLocator.Register(this)

IntegrationBootstrap.Start()                [Integration, ExecutionOrder=100]
 ├─ InitializeIntegrationServices()
 │   ├─ SaveLoadSystem 등록
 │   ├─ CampaignManager 등록
 │   └─ GameFlowController 생성
 ├─ LoadSaveData()                           ← 세이브 없음 → return
 ├─ EnsurePlayerHasData()                    ← ★ NEW
 │   └─ GetOwnedUnits().Count == 0
 │       └─ FirstRunInitializer.InitializeNewPlayer()
 │           ├─ OwnedUnitData 생성 (UNIT_PROTAGONIST, partyPosition=1)
 │           └─ 초기 덱 카드 5장 인벤토리 등록
 └─ LoadLobbyScene()                         ← 주인공 파티 편성 완료 상태로 로비 → ✓
```

---

## 9. 후속 작업 (Unity 에디터) — 완료

코드/데이터 작업 이후 에디터 작업을 수행하였다. 아래는 완료된 항목:

| # | Phase | 작업 | 상세 | 상태 |
|:---:|:---:|:---|:---|:---:|
| 1 | D | CSV → SO 일괄 임포트 | `ProjectStS > Data > CSV → SO 임포터` 메뉴 실행 | ✅ |
| 2 | D | SO 무결성 검증 | 임포트 행 수 확인 (총 68행) | ✅ |
| 3 | F | BootScene 검증 | DontDestroyOnLoad 오브젝트에 필수 컴포넌트 + DataManager SO 참조 할당 확인 | ✅ |
| 4 | F | LobbyScene 검증 | LobbyUIController + 하위 컨트롤러 + 버튼 참조 할당 확인 | ✅ |
| 5 | F | StageScene 검증 | StageSceneBootstrap + StageManager 참조 할당 확인 | ✅ |
| 6 | F | BattleScene 검증 | BattleSceneBootstrap + BattleManager 참조 할당 확인 | ✅ |
| 7 | F | Build Settings | BootScene(0), LobbyScene(1), StageScene(2), BattleScene(3) 순서 확인 | ✅ |
| 8 | G | E2E 테스트 | BootScene에서 Play → LobbyScene 전환 확인 (런타임 에러 0건) | ✅ |

---

## 10. Phase H: 4개 씬 카메라 배치

### 10.1 문제

Play 모드에서 모든 씬에 Camera 오브젝트가 없어 화면이 렌더링되지 않음.

### 10.2 해결

4개 씬(BootScene, LobbyScene, StageScene, BattleScene)에 Main Camera 오브젝트를 배치.

| 씬 | 추가 오브젝트 | 컴포넌트 | 태그 |
|:---|:---|:---|:---|
| BootScene | Main Camera | Camera, AudioListener | MainCamera |
| LobbyScene | Main Camera | Camera, AudioListener | MainCamera |
| StageScene | Main Camera | Camera, AudioListener | MainCamera |
| BattleScene | Main Camera | Camera, AudioListener | MainCamera |

---

## 11. Phase I: LobbyScene Canvas 기본 설정 수정

### 11.1 문제

Play 모드에서 LobbyScene 진입 시 화면 좌측 하단에 아주 작은 흰 사각형만 표시되고 버튼 클릭이 반응하지 않음.

### 11.2 원인 분석

| # | 원인 | 영향 |
|:---:|:---|:---|
| 1 | CanvasScaler가 Constant Pixel Size (mode=0) | 해상도 독립 스케일링 미적용 → UI가 극소 크기로 렌더링 |
| 2 | Canvas 레이어가 Default (0) | UI 레이어(5) 미설정 |
| 3 | LobbyUIRoot 등 RectTransform이 stretch-fill 미설정 | 자식 요소가 Canvas 영역을 채우지 않음 |

### 11.3 해결

| 대상 | 변경 항목 | Before | After |
|:---|:---|:---|:---|
| Canvas | CanvasScaler.uiScaleMode | 0 (Constant Pixel Size) | 1 (Scale With Screen Size) |
| Canvas | CanvasScaler.referenceResolution | — | 1920×1080 |
| Canvas | CanvasScaler.matchWidthOrHeight | — | 0.5 |
| Canvas | Layer | Default (0) | UI (5) |
| LobbyUIRoot | RectTransform | 기본값 | Stretch Fill (0,0~1,1) |
| MainScreen | RectTransform | 기본값 | Stretch Fill (0,0~1,1) |
| PartyEditScreen | RectTransform | 기본값 | Stretch Fill (0,0~1,1) |
| InventoryScreen | RectTransform | 기본값 | Stretch Fill (0,0~1,1) |
| CampaignScreen | RectTransform | 기본값 | Stretch Fill (0,0~1,1) |

---

## 12. Phase J: LobbyScene UI 레이아웃 수정 + 미완성 요소 보완

### 12.1 문제

Canvas 설정 수정 후에도 동일한 화면이 출력됨. 추가 조사 결과:

| # | 원인 | 영향 |
|:---:|:---|:---|
| 1 | MainScreen 하위 요소(PartyPreview, NavigationButtons, ExpeditionButton, CampaignTracker)가 모두 기본 100×100 크기 | UI 요소가 화면에 거의 보이지 않음 |
| 2 | PreviewPortrait_0~2에 UIUnitPortrait 컴포넌트만 있고 필수 자식 요소(FrameBorder, PortraitImage, NameText, ElementBadge)가 없음 | 초상화 렌더링 불가, SerializeField 전부 null |

### 12.2 Step 1: PreviewPortrait 프리팹 인스턴스 교체

빈 컨테이너를 삭제하고 `UIUnitPortrait.prefab` 인스턴스로 교체.

| 작업 | 대상 |
|:---|:---|
| 삭제 | PreviewPortrait_0, 1, 2 (빈 GameObject, childCount=0) |
| 생성 | UIUnitPortrait.prefab × 3 인스턴스 → PartyPreview 하위 배치 |
| 재할당 | LobbyUIController._partyPreviewPortraits[0~2] → 새 인스턴스 참조 |

**교체 후 프리팹 구조** (각 인스턴스 동일):

```
PreviewPortrait_N (UIUnitPortrait)
 ├─ FrameBorder (Image)
 │   └─ PortraitImage (Image)
 ├─ PlaceholderName (TextMeshProUGUI)
 ├─ NameText (TextMeshProUGUI)
 └─ ElementBadge (UIElementBadge 프리팹 인스턴스)
     └─ Label (TextMeshProUGUI)
```

### 12.3 Step 2: MainScreen RectTransform 레이아웃 설정

Canvas 기준 1920×1080.

| 오브젝트 | 앵커 | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|
| PartyPreview | Top-Center (0.5, 1) | (0, -120) | (600, 160) | HorizontalLayoutGroup (기존), Spacing=20 |
| NavigationButtons | Center (0.5, 0.5) | (0, -30) | (400, 200) | VerticalLayoutGroup 추가 |
| ExpeditionButton | Bottom-Center (0.5, 0) | (0, 100) | (300, 60) | |
| CampaignTracker | Top-Right (1, 1) | (-140, -40) | (250, 60) | |

**NavigationButtons VerticalLayoutGroup 설정**:

| 속성 | 값 |
|:---|:---|
| Spacing | 10 |
| Child Alignment | Middle Center |
| Child Force Expand Width | false |
| Child Force Expand Height | false |

**네비게이션 버튼 크기** (각 300×50):

| 버튼 | sizeDelta |
|:---|:---|
| PartyEditButton | (300, 50) |
| InventoryButton | (300, 50) |
| CampaignButton | (300, 50) |

### 12.4 Step 3: 하위 화면 구조 확인

3개 하위 화면의 내부 구조가 정상적으로 구성되어 있음을 확인. Stretch Fill 설정 재적용.

| 화면 | 자식 요소 | 상태 |
|:---|:---|:---:|
| PartyEditScreen | PE_BackButton, PartySlotArea(3자식), UnitListArea, UnitEditPanel(7자식) | ✅ |
| InventoryScreen | INV_BackButton, FilterArea(4자식), GridArea, ItemDetailPanel(9자식) | ✅ |
| CampaignScreen | CP_BackButton, CampaignList(6자식), CampaignDetail(6자식) | ✅ |

### 12.5 Step 4: Play 모드 검증

BootScene에서 Play 모드 실행 → LobbyScene 전환 결과:

| 항목 | 결과 |
|:---|:---:|
| 부트 체인 | ✅ GameBootstrapper → IntegrationBootstrap → FirstRunInitializer → LobbyScene |
| 런타임 에러 | 0건 ✅ |
| LobbyUIController 작동 | ✅ 주인공 데이터를 PreviewPortrait에 바인딩 |
| 버튼 텍스트 표시 | ✅ 파티 편성, 인벤토리, 캠페인, 탐험 개시 |
| ElementBadge 속성 표시 | ✅ "바람" 텍스트 설정 확인 |

**확인된 경고** (에러 아님):

| 로그 | 원인 | 조치 |
|:---|:---|:---|
| DontDestroyOnLoad only works for root GameObjects | BootScene 구조 이슈 | 기능 영향 없음, 후속 정리 대상 |
| Korean character not found in LiberationSans SDF | TMP 기본 폰트가 한국어 미지원 | Phase K에서 해결 |

---

## 13. Phase K: LobbyScene TMP 폰트 변경

### 13.1 문제

TextMeshPro 기본 폰트(LiberationSans SDF)가 한국어 문자를 지원하지 않아 모든 한글 텍스트가 □(U+25A1)로 표시됨.

### 13.2 해결

LobbyScene 내 **49개 TextMeshProUGUI 오브젝트**의 폰트를 `NanumGothicBold SDF`로 일괄 변경.

**폰트 에셋**: `Assets/TextMesh Pro/Resources/Fonts & Materials/NanumGothicBold SDF.asset`

**변경된 텍스트 오브젝트 목록**:

| 화면 | 오브젝트 (49개) |
|:---|:---|
| **메인 화면** | PartyEditButtonText, InventoryButtonText, CampaignButtonText, ExpeditionButtonText |
| **파티 프리뷰** | PreviewPortrait ×3의 PlaceholderName, NameText, Label (ElementBadge) — 9개 |
| **캠페인 트래커** | CampaignNameText, CurrentGoalText |
| **파티 편성** | PE_BackButtonText, PositionLabel_1/2/3, EmptyLabel_1/2/3, UnitNameText, DeckCountText, SkillNameText — 10개 |
| **인벤토리** | INV_BackButtonText, SortOrderLabel, NameText, DescriptionText, RarityText, CostText, CardTypeText, ItemTypeText, TargetStatusText, DisposableText, QuantityText — 11개 |
| **캠페인** | CP_BackButtonText, ActiveHeader, CompletedHeader, EmptyActiveText, EmptyCompletedText, CD_NameText, CD_DescriptionText, TrackButtonText — 8개|
| **UITooltip** | TitleText, BodyText — 2개 |
| **UIPopup** | MessageText, ConfirmButtonText, CancelButtonText — 3개 |

---

## 14. Phase L: LobbyScene 버튼 배경 이미지 적용

### 14.1 문제

Button 컴포넌트가 있는 오브젝트의 Image에 Source Image가 미지정 상태로, 기본 흰색 사각형으로 표시됨.

### 14.2 해결

LobbyScene 내 **17개 Button 오브젝트**의 Image → Source Image를 `ButtonBackground.png`로 일괄 적용.

**이미지 에셋**: `Assets/_Project/Artworks/Temp/ButtonBackground.png`

| 화면 | 오브젝트 |
|:---|:---|
| **메인** | PartyEditButton, InventoryButton, CampaignButton, ExpeditionButton |
| **파티 편성** | PE_BackButton, SkillButton, ItemSlot1, ItemSlot2 |
| **인벤토리** | INV_BackButton, CardTab, ItemTab, SortOrderButton |
| **캠페인** | CP_BackButton, TrackButton, CloseButton |
| **UIPopup** | ConfirmButton, CancelButton |

---

## 15. Phase M: LobbyScene 버튼 클릭 불능 수정

### 15.1 문제

네비게이션 버튼 3종(PartyEditButton, InventoryButton, CampaignButton)과 ExpeditionButton이 클릭에 반응하지 않음.

### 15.2 원인 분석

조사 항목과 결과:

| 항목 | 결과 |
|:---|:---|
| EventSystem | ✅ 존재 (InputSystemUIInputModule) |
| Canvas GraphicRaycaster | ✅ 활성 |
| Button.interactable | ✅ 모두 true |
| Image.raycastTarget (버튼) | ✅ 모두 true |
| CanvasGroup 차단 | ✅ MainScreen/NavigationButtons에 CanvasGroup 없음 |

**근본 원인**: UIPopup 루트 오브젝트의 투명 Image가 전체 화면 레이캐스트를 차단.

```
Canvas/LobbyUIRoot
 ├─ MainScreen              ← 버튼들이 여기에 있음
 ├─ PartyEditScreen
 ├─ InventoryScreen
 ├─ CampaignScreen
 ├─ UITooltip
 └─ UIPopup                 ← 마지막 자식 (= 가장 위에 렌더링)
      Image.raycastTarget = true     ← ★ 원인
      RectTransform = Stretch Fill   ← 전체 화면 덮음
      Image.color = (0,0,0,0)       ← 투명이지만 클릭 흡수
```

UIPopup이 LobbyUIRoot의 **마지막 자식**(= UI 렌더링 순서상 최상위)이며, 루트 오브젝트의 Image가 `raycastTarget=true` + 전체 화면 Stretch Fill 상태. 색상이 완전 투명(alpha=0)이라 시각적으로 보이지 않지만, Unity의 GraphicRaycaster는 투명한 Image도 raycastTarget이 true이면 클릭 이벤트를 소비한다.

추가로 LobbyUIRoot도 동일한 상태(raycastTarget=true, 전체 화면, 투명)였으나, 자식 버튼들보다 계층 하위라 직접적 원인은 아님. 예방 차원에서 함께 수정.

### 15.3 해결

| 오브젝트 | 변경 항목 | Before | After |
|:---|:---|:---|:---|
| UIPopup (루트) | Image.raycastTarget | true | **false** |
| LobbyUIRoot | Image.raycastTarget | true | **false** |

**팝업 기능 유지**: PopupRoot 활성화 시 내부 DimBackground(`raycastTarget=true`, 반투명 배경)가 화면을 덮어 레이캐스트를 차단하므로, 팝업 표시 시의 배경 클릭 차단 기능은 정상 유지.

```
UIPopup (Image.raycastTarget = false)  ← 수정: 투과
 └─ PopupRoot (activeSelf=false → 팝업 표시 시 true)
      ├─ DimBackground (raycastTarget=true, 반투명)  ← 팝업 시에만 차단
      └─ Panel
           ├─ MessageText
           ├─ ConfirmButton
           └─ CancelButton
```

### 15.4 검증

BootScene에서 Play 모드 실행 → LobbyScene 전환 → 런타임 에러 0건 확인.

---

## 16. Phase N: 하위 화면 내부 레이아웃 설정 + 프리팹 원본 수정

### 16.1 문제

Phase J에서 MainScreen 자식 요소들의 레이아웃을 정상화했으나, 3개 하위 화면(PartyEditScreen, InventoryScreen, CampaignScreen)의 **내부 자식 요소**들이 모두 기본 크기(100×100, 위치 -960,-540)로 남아 있어 해당 화면 전환 시 UI가 거의 보이지 않는 상태.

추가로 UITooltip/UIPopup/UIUnitPortrait/UIElementBadge **프리팹 원본**의 TMP 폰트가 기본 폰트(LiberationSans SDF)로 유지되어 있음. Phase K에서는 씬 인스턴스만 변경되었고 프리팹 원본은 미수정 상태.

| # | 원인 | 영향 |
|:---:|:---|:---|
| 1 | 하위 화면 내부 자식 요소가 기본 100×100 크기 | 파티 편성/인벤토리/캠페인 화면 전환 시 UI 미표시 |
| 2 | Common 프리팹 TMP 폰트가 LiberationSans SDF | 프리팹 재사용 시 한글 깨짐 (씬 인스턴스 오버라이드에만 의존) |
| 3 | UIPopup 프리팹 Panel 크기가 400×200 | 한글 텍스트 표시 공간 부족 |
| 4 | UITooltip 프리팹에 LayoutGroup/ContentSizeFitter 미설정 | 텍스트 양에 따른 자동 크기 조절 미동작 |

### 16.2 Step 1: PartyEditScreen 내부 레이아웃 설정

Canvas 기준 1920×1080. PartyEditScreen 자체는 Stretch Fill (0,0)→(1,1) 설정 완료 상태.

**자식 요소 RectTransform 설정:**

| 오브젝트 | Anchor | Pivot | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|:---|
| PE_BackButton | Top-Left (0,1)→(0,1) | (0, 1) | (20, -20) | (120, 40) | 뒤로가기 |
| PartySlotArea | Top-Center (0.5,1) | (0.5, 1) | (0, -80) | (720, 240) | HorizontalLayoutGroup 갱신 |
| UnitListArea | Stretch (0,0)→(0.45,1) | (0.5, 0.5) | — | offsetMin(20,20), offsetMax(-10,-340) | 좌측 45% |
| UnitEditPanel | Stretch (0.45,0)→(1,1) | (0.5, 0.5) | — | offsetMin(10,20), offsetMax(-20,-80) | 우측 55% |

**PartySlotArea HorizontalLayoutGroup:**

| 속성 | 값 |
|:---|:---|
| Spacing | 20 |
| Child Alignment | Middle Center |
| Child Force Expand Width / Height | false / false |
| Padding | 10 all |

**파티 슬롯** (각 PartySlot_1/2/3): sizeDelta (210, 220)

**UnitEditPanel 내부:**

| 오브젝트 | Anchor | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|
| UE_UnitInfo | Top Stretch | (0, -10) | (-20, 100) | 유닛 정보 |
| DeckArea | Top Stretch | (0, -120) | (-20, 240) | GridLayoutGroup 추가 (CellSize 70×100) |
| SkillArea | Top Stretch | (0, -370) | (-20, 80) | 스킬 영역 |
| ItemArea | Bottom Stretch | (0, 10) | (-20, 100) | HorizontalLayoutGroup 추가 |
| CardSelectionRoot | Stretch Fill | (0, 0) | (0, 0) | 카드 선택 패널 |
| SkillSelectionRoot | Stretch Fill | (0, 0) | (0, 0) | 스킬 선택 패널 |

### 16.3 Step 2: InventoryScreen 내부 레이아웃 설정

**자식 요소 RectTransform 설정:**

| 오브젝트 | Anchor | Pivot | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|:---|
| INV_BackButton | Top-Left (0,1)→(0,1) | (0, 1) | (20, -20) | (120, 40) | 뒤로가기 |
| FilterArea | Top Stretch (0,1)→(1,1) | (0.5, 1) | — | offsetMin(20,-100), offsetMax(-20,-20) | HorizontalLayoutGroup 추가 |
| GridArea | Stretch (0,0)→(0.65,1) | (0.5, 0.5) | — | offsetMin(20,20), offsetMax(-10,-110) | 좌측 65% |
| ItemDetailPanel | Stretch (0.65,0)→(1,1) | (0.5, 0.5) | — | offsetMin(10,20), offsetMax(-20,-80) | 우측 35% |

**FilterArea HorizontalLayoutGroup:**

| 속성 | 값 |
|:---|:---|
| Spacing | 10 |
| Child Alignment | Middle Left |
| Padding Left | 150 |
| Child Force Expand Width / Height | false / false |

**FilterArea 자식:** TabButtons(220×40), CardFilterRoot(400×40), ItemFilterRoot(300×40), SortArea(200×40)

**GridContainer:** GridLayoutGroup — CellSize(80, 100), Spacing(8, 8), Padding 10, ConstraintCount=5

**ItemDetailPanel 내부:**

| 오브젝트 | Anchor | anchoredPosition | sizeDelta |
|:---|:---|:---|:---|
| ID_PanelRoot | Stretch Fill | (0, 0) | (-20, -20) |
| NameText | Top-Center | (0, -20) | (280, 30) |
| DescriptionText | Top-Center | (0, -60) | (280, 60) |
| RarityText | Top-Center | (0, -130) | (280, 25) |
| ArtworkImage | Center | (0, 0) | (160, 160) |
| QuantityText | Bottom-Center | (0, 20) | (280, 25) |

### 16.4 Step 3: CampaignScreen 내부 레이아웃 설정

**자식 요소 RectTransform 설정:**

| 오브젝트 | Anchor | Pivot | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|:---|
| CP_BackButton | Top-Left (0,1)→(0,1) | (0, 1) | (20, -20) | (120, 40) | 뒤로가기 |
| CampaignList | Stretch (0,0)→(0.4,1) | (0.5, 0.5) | — | offsetMin(20,20), offsetMax(-10,-80) | 좌측 40%, VerticalLayoutGroup 추가 |
| CampaignDetail | Stretch (0.4,0)→(1,1) | (0.5, 0.5) | — | offsetMin(10,20), offsetMax(-20,-80) | 우측 60% |

**CampaignList VerticalLayoutGroup:** Spacing=5, ChildAlignment=UpperCenter, Padding Top=10

**CampaignList 자식:** ActiveHeader(h30), ActiveListContainer(h200, VLG), EmptyActiveText(h30), CompletedHeader(h30), CompletedListContainer(h200, VLG), EmptyCompletedText(h30)

**CampaignDetail 내부:**

| 오브젝트 | Anchor | anchoredPosition | sizeDelta | 비고 |
|:---|:---|:---|:---|:---|
| CD_PanelRoot | Stretch Fill | (0, 0) | (-20, -20) | 패딩 10px |
| CD_NameText | Top-Center | (0, -20) | (400, 35) | 캠페인 이름 |
| CD_DescriptionText | Top-Center | (0, -60) | (400, 80) | 설명 |
| GoalContainer | Center Stretch | (0, -20) | (-40, -200) | VerticalLayoutGroup 추가, Spacing=8 |
| TrackButton | Bottom-Left | (80, 20) | (140, 40) | |
| CloseButton | Bottom-Right | (-80, 20) | (100, 40) | |

### 16.5 Step 4: UITooltip 프리팹 원본 수정

**프리팹 경로**: `Assets/_Project/Prefabs/UI/Common/UITooltip.prefab`

| 오브젝트 | 변경 내용 |
|:---|:---|
| TooltipPanel | VerticalLayoutGroup 추가 (Spacing=4, Padding=10 all, ChildForceExpandHeight=false) |
| TooltipPanel | ContentSizeFitter 추가 (HorizontalFit=PreferredSize, VerticalFit=PreferredSize) |
| TitleText | font → NanumGothicBold SDF |
| BodyText | font → NanumGothicBold SDF |

### 16.6 Step 5: UIPopup 프리팹 원본 수정

**프리팹 경로**: `Assets/_Project/Prefabs/UI/Common/UIPopup.prefab`

| 오브젝트 | 변경 항목 | Before | After |
|:---|:---|:---|:---|
| UIPopup (Root) | Image.raycastTarget | true | **false** |
| Panel | sizeDelta | (400, 200) | **(500, 250)** |
| MessageText | Anchor/Position | Center(-960,-540), 200×50 | **Top-Center(0,-20), 460×140** |
| ConfirmButtonRoot | Anchor/Position | Center(-960,-540), 100×100 | **Bottom(−70,20), 120×45** |
| CancelButtonRoot | Anchor/Position | Center(-960,-540), 100×100 | **Bottom(70,20), 120×45** |
| MessageText | font | LiberationSans SDF | **NanumGothicBold SDF** |
| ConfirmButtonText | font | LiberationSans SDF | **NanumGothicBold SDF** |
| CancelButtonText | font | LiberationSans SDF | **NanumGothicBold SDF** |

### 16.7 Step 6: UIUnitPortrait / UIElementBadge 프리팹 폰트 변경

| 프리팹 | 오브젝트 | 변경 |
|:---|:---|:---|
| `UIUnitPortrait.prefab` | PlaceholderName | font → NanumGothicBold SDF |
| `UIUnitPortrait.prefab` | NameText | font → NanumGothicBold SDF |
| `UIElementBadge.prefab` | Label | font → NanumGothicBold SDF |

### 16.8 검증

BootScene에서 Play 모드 실행 → LobbyScene 전환 결과:

| 항목 | 결과 |
|:---|:---:|
| 부트 체인 정상 | ✅ |
| 런타임 에러 | 0건 ✅ |
| 씬 전환 | BootScene → LobbyScene ✅ |

**확인된 경고** (기존):

| 로그 | 심각도 | 비고 |
|:---|:---:|:---|
| DontDestroyOnLoad only works for root GameObjects | 낮음 | 기존 이슈, 기능 영향 없음 |

---

## 17. 현재 상태 요약

### 완료된 Phase

| Phase | 작업 | 상태 |
|:---|:---|:---:|
| A | 테스트 CSV 데이터 16개 파일 (68행) | ✅ |
| B | GameSettings.asset 주인공 ID 설정 | ✅ |
| C | FirstRunInitializer + IntegrationBootstrap 부트 체인 수정 | ✅ |
| D | CSV → SO 일괄 임포트 (에디터 실행) | ✅ |
| E | 방어 코드 보강 (4개 파일) | ✅ |
| F | 4개 씬 구성 검증 + Build Settings | ✅ |
| G | E2E Play 모드 테스트 (BootScene → LobbyScene) | ✅ |
| H | 4개 씬 카메라 배치 | ✅ |
| I | LobbyScene Canvas 기본 설정 수정 | ✅ |
| J | LobbyScene UI 레이아웃 수정 + 미완성 요소 보완 | ✅ |
| K | LobbyScene TMP 폰트 변경 (NanumGothicBold SDF) | ✅ |
| L | LobbyScene 버튼 배경 이미지 적용 (ButtonBackground.png) | ✅ |
| M | LobbyScene 버튼 클릭 불능 수정 (raycastTarget) | ✅ |
| N | 하위 화면 내부 레이아웃 설정 + 프리팹 원본 수정 | ✅ |

### Play 모드 검증 결과

```
BootScene Play → LobbyScene 전환
 ├─ 부트 체인: GameBootstrapper → IntegrationBootstrap → FirstRunInitializer → LobbyScene ✅
 ├─ 런타임 에러: 0건 ✅
 ├─ LobbyUIController: 주인공 데이터 바인딩 ✅
 ├─ PreviewPortrait: 주인공 초상화 표시 (이름/속성 바인딩) ✅
 ├─ 네비게이션 버튼: 파티 편성/인벤토리/캠페인 텍스트 표시 ✅
 ├─ 탐험 개시 버튼: 텍스트 표시 ✅
 ├─ 캠페인 트래커: 추적 캠페인 없으면 자동 숨김 ✅
 ├─ 버튼 클릭: UIPopup/LobbyUIRoot raycastTarget 차단 해제 완료 ✅
 ├─ 하위 화면 레이아웃: PartyEdit/Inventory/Campaign 내부 배치 완료 ✅
 └─ 프리팹 폰트: UITooltip/UIPopup/UIUnitPortrait/UIElementBadge NanumGothicBold SDF ✅
```

### 알려진 이슈

| 이슈 | 심각도 | 비고 |
|:---|:---:|:---|
| DontDestroyOnLoad 경고 | 낮음 | BootScene 구조 이슈, 기능 영향 없음 |
| `[RewardTableSO] 중복 ID: 'RW_01'` | 낮음 | RewardTable.csv에서 RW_01이 2행(ITEM_POTION, ITEM_HP_RING)으로 사용됨. SO 파싱 로직 확인 필요 |
| `[SceneTransition] 이미 씬 전환이 진행 중입니다` | 낮음 | LobbyScene 로드 중복 호출, 무시 처리됨 |

### 후속 작업

- [ ] 화면 전환 테스트: 파티 편성/인벤토리/캠페인 버튼 클릭 → 하위 화면 전환 → 뒤로가기 → 메인 복귀
- [ ] 탐험 개시 테스트: 탐험 버튼 클릭 → ValidateExpedition → 씬 전환
- [x] ~~하위 화면 내부 RectTransform 세부 레이아웃 조정~~ → Phase N에서 완료
- [x] ~~프리팹 폰트 통일~~ → Phase N에서 완료
- [ ] 하위 화면 실제 데이터 바인딩 검증 (파티 편성 슬롯, 인벤토리 그리드, 캠페인 목록)
- [ ] UITooltip/UIPopup 런타임 동작 테스트 (호버 표시, 팝업 열기/닫기)

---

*마지막 업데이트: 2026.03.10 | 작성자: Claude Code*
