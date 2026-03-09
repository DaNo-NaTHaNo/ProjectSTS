# Play 모드 구동 테스트를 위한 추가 작업 계획

## Context

마스터 플랜 Phase 0~5까지 모든 게임 시스템 코드 구현이 완료되었다(148개 런타임 스크립트, 4개 씬, 19개 SO 테이블, 30+ UI 컴포넌트). 그러나 **Play 모드에서 게임을 실행할 수 없는** 상태다. 원인은 다음 세 가지:

1. **19개 CSV 데이터 파일이 전부 헤더만 존재** — 게임 데이터가 없음
2. **GameSettings._protagonistUnitId가 빈 문자열** — 파티 편성 검증 실패
3. **첫 실행 시 초기 데이터 생성 메커니즘 부재** — 세이브 없으면 PlayerDataManager가 빈 상태로 유지

이 계획은 최소 실행 가능 테스트 데이터(MVP Test Data)를 준비하고, 부트 체인의 갭을 메우고, E2E 검증까지 수행하는 과정을 정의한다.

---

## Phase A: 테스트 CSV 데이터 작성 ✅ 완료

> **목적**: 게임 루프(로비→스테이지→전투→정산→로비)를 1회전 돌릴 수 있는 최소한의 마스터 데이터 투입
>
> **의존 관계**: 리프 데이터(CardEffects, StatusEffects, AIPatterns/Rules/Conditions)부터 작성 → 참조 데이터(Cards, Skills, Units, Events 등) 순서로 진행

### Step A-1: CardEffects.csv (8행)
- **파일**: `Assets/_Project/Data/CardEffects.csv`
- **의도**: 카드 효과의 모든 주요 타입(Damage, Block, Heal, ApplyStatus, Draw)을 커버하는 기초 효과 세트
- **데이터**: CE_DMG_10, CE_DMG_15, CE_DMG_20, CE_BLK_8, CE_BLK_12, CE_HEAL_5, CE_STATUS_BURN, CE_DRAW_1

### Step A-2: StatusEffects.csv (3행)
- **파일**: `Assets/_Project/Data/StatusEffects.csv`
- **의도**: Buff/Debuff 양측 + 주요 트리거 타이밍(TurnStart, OnAttack, OnDamage) 테스트
- **데이터**: SE_BURN(화상), SE_STR(공격력 강화), SE_ARMOR(방어력 강화)

### Step A-3: AIPatterns.csv (2행)
- **의도**: 일반 적과 보스의 기본 행동 패턴 정의
- **데이터**: AI_BASIC(기본 공격), AI_BOSS(보스 패턴)

### Step A-4: AIPatternRules.csv (3행)
- **의도**: 보스의 공격/방어 교대 패턴 + 일반 적의 단순 공격 규칙

### Step A-5: AIConditions.csv (2행)
- **의도**: TurnMod 조건으로 보스의 홀짝 턴 분기 구현

### Step A-6: Cards.csv (15행)
- **의도**: 아군 3명(Sword/Baton/Medal)에 각 5장씩, 공격/방어/유틸리티 혼합
- **교차 참조**: 모든 cardEffectId가 CardEffects의 CE_* ID와 매칭

### Step A-7: Skills.csv (3행)
- **의도**: 각 아군 유닛에 1개씩 스킬 부여, 서로 다른 트리거 조건 테스트

### Step A-8: Units.csv (5행)
- **의도**: 아군 3명(UNIT_PROTAGONIST, UNIT_MAGE, UNIT_KNIGHT) + 일반적(UNIT_GOBLIN) + 보스(UNIT_BOSS_ORC)

### Step A-9: Items.csv (3행)
- **의도**: Equipment/HasDamage/HasDown 3종 아이템 타입 커버

### Step A-10: EnemyCombinations.csv (2행)
- **의도**: 일반전(고블린 1마리) + 보스전(오크 대장 1마리) 최소 전투 조합

### Step A-11: Areas.csv (2행)
- **의도**: 최소 2개 구역(AREA_START + AREA_FOREST)으로 월드맵 생성 및 이동 테스트

### Step A-12: Events.csv (4행)
- **의도**: BattleNormal, BattleBoss, VisualNovel, BattleNormal(다른 구역) 4종 이벤트

### Step A-13: RewardTable.csv (2행)
- **의도**: 전투 보상 드롭 테이블 최소 데이터

### Step A-14: DropRates.csv (4행)
- **의도**: 이벤트 스폰 및 보상 선정용 레어도별 확률

### Step A-15: Campaigns.csv + CampaignGoalGroups.csv
- **의도**: 최소 1개 캠페인 + 1개 목표 그룹

### Step A-16/17: BattleActions/Timelines (헤더만), ElementAffinity (기존 49행 유지)

---

## Phase B: GameSettings 구성 ✅ 완료

### Step B-1: GameSettings.asset 수정
- **변경**: `_protagonistUnitId` = `"UNIT_PROTAGONIST"`

---

## Phase C: 첫 실행 초기화 메커니즘 구현 ✅ 완료

### Step C-1: FirstRunInitializer 클래스 생성
- **파일**: `Assets/_Project/Scripts/Runtime/Integration/FirstRunInitializer.cs`
- **설계**: 세이브 없는 최초 실행 시 주인공 OwnedUnitData 생성 + 초기 덱 카드 인벤토리 등록
- **핵심 로직**:
  - GameSettings에서 주인공 ID → UnitTable에서 UnitData 조회
  - OwnedUnitData 생성 (cardElement = UnitData.element, editedDeck = initialDeckIds, partyPosition = 1)
  - 초기 카드를 InventoryItemData(category=Card, useStack=1)로 인벤토리에 추가

### Step C-2: IntegrationBootstrap에 첫 실행 감지 로직 추가
- **변경**: Start()에서 LoadSaveData() 후 EnsurePlayerHasData() 호출
- PlayerDataManager가 비어있으면 FirstRunInitializer.InitializeNewPlayer() 실행

---

## Phase D: CSV → SO 임포트 실행 ⏳ Unity 에디터 작업 필요

### Step D-1: Unity 에디터에서 CSV 일괄 임포트
1. Unity 에디터 열기
2. `ProjectStS > Data > CSV → SO 임포터` 메뉴 실행
3. CSV 폴더 경로 = `Assets/_Project/Data`
4. "전체 CSV 일괄 임포트" 클릭
5. 콘솔에서 각 테이블별 임포트 행 수 확인:
   - CardEffects: 8행, StatusEffects: 3행, Cards: 15행, Skills: 3행
   - Units: 5행, Items: 3행, AIPatterns: 2행, AIPatternRules: 3행
   - AIConditions: 2행, EnemyCombinations: 2행, Areas: 2행
   - Events: 4행, RewardTable: 2행, DropRates: 4행
   - Campaigns: 1행, CampaignGoalGroups: 1행, ElementAffinity: 49행(기존)
   - BattleActions: 0행, BattleTimelines: 0행

### Step D-2: SO 데이터 무결성 검증
- CardTable의 각 카드 cardEffectId → CardEffectTable에 존재하는지
- UnitTable의 initialDeckIds 각 카드 ID → CardTable에 존재하는지
- UnitTable의 initialSkillId → SkillTable에 존재하는지
- UnitTable의 aiPatternId → AIPatternTable에 존재하는지
- EventTable의 eventValue(전투 이벤트) → EnemyCombinationTable에 존재하는지
- EnemyCombinationTable의 enemyUnit_* → UnitTable에 존재하는지

---

## Phase E: 방어 코드 보강 ✅ 완료

### Step E-1: StageSceneBootstrap 방어 로그 개선
- 파티 데이터 없을 때 누락 원인 세분화 로그 추가

### Step E-2: BattleSceneBootstrap 방어 로그 개선
- BattleManager null, GameFlowController 누락 시 힌트 메시지 보강

### Step E-3: LobbyUIController 빈 데이터 처리
- 보유 유닛 0일 때 경고 로그 추가

### Step E-4: PartyEditManager 빈 덱 방어
- 기존 코드가 이미 충분히 방어적 (ParseSemicolonList의 null/빈값 처리)

---

## Phase F: 씬 구성 검증 ⏳ Unity 에디터 작업 필요

### Step F-1: BootScene 검증
- **확인 항목**:
  - DontDestroyOnLoad 오브젝트에 GameBootstrapper, IntegrationBootstrap, PlayerDataManager 컴포넌트 존재
  - DataManager 컴포넌트에 19개 SO 테이블 + GameSettings 참조 할당
  - VisualNovelBridge 컴포넌트 존재 (IVisualNovelBridge 등록용)

### Step F-2: LobbyScene 검증
- **확인 항목**:
  - LobbyUIController 존재, 하위 UI 컨트롤러 참조 할당
  - 버튼 참조(파티편성, 인벤토리, 캠페인, 탐험 개시) 할당

### Step F-3: StageScene 검증
- **확인 항목**:
  - StageSceneBootstrap 존재, _stageManager 참조 할당
  - StageManager 컴포넌트 존재

### Step F-4: BattleScene 검증
- **확인 항목**:
  - BattleSceneBootstrap 존재, _battleManager 참조 할당
  - BattleManager 컴포넌트 존재

### Step F-5: Build Settings 확인
- BootScene(index 0), LobbyScene(1), StageScene(2), BattleScene(3) 순서로 등록

---

## Phase G: E2E Play 모드 검증 ⏳ Unity 에디터 작업 필요

### Step G-1: Boot → Lobby 전환
1. BootScene에서 Play 모드 진입
2. **예상 흐름**: GameBootstrapper.Awake() → ServiceLocator 초기화 → IntegrationBootstrap.Start() → 세이브 없음 → FirstRunInitializer로 UNIT_PROTAGONIST 생성 (partyPosition=1) → LobbyScene 로드
3. **검증**: 콘솔에 에러 없음, 로비 화면 표시, 파티 프리뷰에 주인공 표시

### Step G-2: Lobby → 파티 편성
1. 파티 편성 버튼 클릭
2. **검증**: 주인공이 슬롯 1에 표시, 덱 5장 확인, 스킬 1개 확인

### Step G-3: Lobby → 탐험 개시 → Stage
1. 탐험 개시 버튼 클릭
2. **예상 흐름**: ExpeditionLauncher.ValidateExpedition() 통과 → StageScene 로드
3. **검증**: 육각 그리드 생성, AP 표시(주인공 5), 이벤트 노드 배치

### Step G-4: Stage → 전투 이벤트 → Battle
1. BattleNormal 이벤트 노드 클릭
2. **예상 흐름**: GameFlowController → BattleScene 로드 → BattleManager 초기화
3. **검증**: 아군 1명(주인공) + 적 1명(고블린) 배치, 손패 4장 리필, 에너지 3

### Step G-5: Battle 턴 진행
1. 카드 사용(드래그 또는 클릭) → 턴 종료
2. **검증**: 대미지 계산 정상, 적 AI 행동, 스킬 발동, 상태이상 적용

### Step G-6: Battle 승리 → Stage 복귀
1. 고블린 HP 0 → 전투 승리
2. **검증**: 결과 화면 표시, Stage 씬 복귀, AP -1

### Step G-7: Stage 완료 → 정산 → Lobby 복귀
1. AP 소진 또는 추가 이벤트 진행 후 스테이지 종료
2. **검증**: 정산 화면 표시(고정 보상 선택), Lobby 씬 복귀, 세이브 데이터 생성

---

## 작업 순서 총괄

| # | Phase | Step | 핵심 산출물 | 상태 |
|---|-------|------|-------------|------|
| 1 | A | A-1~A-17 | CSV 데이터 16개 파일 | ✅ 완료 |
| 2 | B | B-1 | GameSettings._protagonistUnitId 설정 | ✅ 완료 |
| 3 | C | C-1~C-2 | FirstRunInitializer + IntegrationBootstrap 수정 | ✅ 완료 |
| 4 | E | E-1~E-4 | 방어 코드 보강 | ✅ 완료 |
| 5 | D | D-1~D-2 | SO 임포트 + 검증 | ⏳ 에디터 작업 |
| 6 | F | F-1~F-5 | 씬 구성 검증 | ⏳ 에디터 확인 |
| 7 | G | G-1~G-7 | E2E Play 모드 검증 | ⏳ 테스트 |

---

## 수정/생성 파일 목록

### 데이터 파일 (수정 — 데이터 행 추가)
- `Assets/_Project/Data/CardEffects.csv` (8행)
- `Assets/_Project/Data/StatusEffects.csv` (3행)
- `Assets/_Project/Data/AIPatterns.csv` (2행)
- `Assets/_Project/Data/AIPatternRules.csv` (3행)
- `Assets/_Project/Data/AIConditions.csv` (2행)
- `Assets/_Project/Data/Cards.csv` (15행)
- `Assets/_Project/Data/Skills.csv` (3행)
- `Assets/_Project/Data/Units.csv` (5행)
- `Assets/_Project/Data/Items.csv` (3행)
- `Assets/_Project/Data/EnemyCombinations.csv` (2행)
- `Assets/_Project/Data/Areas.csv` (2행)
- `Assets/_Project/Data/Events.csv` (4행)
- `Assets/_Project/Data/RewardTable.csv` (2행)
- `Assets/_Project/Data/DropRates.csv` (4행)
- `Assets/_Project/Data/Campaigns.csv` (1행)
- `Assets/_Project/Data/CampaignGoalGroups.csv` (1행)

### 코드 파일
- `Assets/_Project/Scripts/Runtime/Integration/FirstRunInitializer.cs` — **신규 생성**
- `Assets/_Project/Scripts/Runtime/Integration/IntegrationBootstrap.cs` — **수정** (EnsurePlayerHasData 추가)
- `Assets/_Project/Scripts/Runtime/Integration/StageSceneBootstrap.cs` — **수정** (방어 로그)
- `Assets/_Project/Scripts/Runtime/Integration/BattleSceneBootstrap.cs` — **수정** (방어 로그)
- `Assets/_Project/Scripts/Runtime/UI/Lobby/LobbyUIController.cs` — **수정** (빈 데이터 처리)

### SO 에셋
- `Assets/_Project/ScriptableObjects/GameSettings.asset` — **수정** (_protagonistUnitId 설정)

---

## 테스트 데이터 교차 참조 맵

```
Card.cardEffectId → CardEffect.id (CE_*)
Unit.initialDeckIds → Card.id (세미콜론 구분)
Unit.initialSkillId → Skill.id (SK_*)
Unit.aiPatternId → AIPattern.id (AI_*)
Skill.cardEffectId → CardEffect.id
Skill.unitId → Unit.id
Event.areaId → Area.id
Event.eventValue → EnemyCombination.id (전투) / episodeId (VN)
EnemyCombination.enemyUnit_* → Unit.id
RewardTable.itemId → Item.id
Campaign.groupId → CampaignGoalGroup.groupId
```

---

*마지막 업데이트: 2026.03.10 | 작성자: Claude Code*
