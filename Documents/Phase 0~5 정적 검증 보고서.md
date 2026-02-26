# Phase 0~5 정적 검증 보고서

*검증일: 2026.02.26*
*검증 범위: Assets/_Project/Scripts/Runtime/ 전체 103개 .cs 파일 + Editor 1개*

---

## 총괄 요약

| 검증 영역 | 결과 | Critical | High | Medium | Low |
|:----------|:-----|:--------:|:----:|:------:|:---:|
| D-1 컴파일 호환성 | **PARTIAL PASS** | 0 | 0 | 2 | 1 |
| D-2 코딩 컨벤션 | **PASS** | 0 | 0 | 0 | 4 |
| D-3 금지 사항 | **PASS** | 0 | 0 | 0 | 1 |
| D-4 데이터 모델 대조 | **PARTIAL PASS** | 1 | 1 | 1 | 0 |
| D-5 서비스 패턴 | **PARTIAL PASS** | 0 | 2 | 0 | 0 |
| D-6 이벤트 패턴 | **PASS** | 0 | 0 | 0 | 1 |
| D-7 게임 로직 | **PARTIAL PASS** | 0 | 1 | 0 | 0 |
| D-8 성능 가이드라인 | **PASS** | 0 | 0 | 2 | 0 |
| **합계** | | **1** | **4** | **5** | **7** |

**전체 판정: 103개 파일 중 심각한 구조적 결함 없음. Critical 1건(데이터 파싱), High 4건(초기화 순서, Get null 안전성, 스킬 우선순위)은 수정 권장.**

---

## D-1. 컴파일 호환성 검증

### D-1-1. asmdef 참조 무결성 — PASS (Low 이슈 1건)

9개 asmdef 파일의 의존성 그래프 검증 완료. **순환 참조 없음**.

| asmdef | 참조 | 상태 |
|:-------|:-----|:-----|
| Data | (없음) | PASS |
| Core | Data | PASS |
| Utils | Data | PASS |
| Battle | Data, Core, Utils | PASS |
| Stage | Data, Core, Utils, Meta | PASS |
| Meta | Data, Core, Utils | PASS |
| UI | Data, Core, Utils | PASS |
| Integration | Data, Core, Meta, Battle, Stage | PASS |
| Editor | Data, Core, Utils, Battle, Stage, Meta, UI | **Low**: Integration 참조 누락 |

> **[Low] Editor asmdef에 `ProjectStS.Integration` 참조 누락** — 현재 CsvToSOImporter가 Integration 타입을 사용하지 않아 컴파일 에러는 없지만, 향후 Editor 스크립트 확장 시 필요.

### D-1-2. 네임스페이스 일관성 — FAIL (Medium 이슈 2건)

103개 파일 중 **101개 PASS**, **2개 FAIL**.

> **[Medium] `CsvRowDefinitions.cs`, `CsvUtility.cs` — 네임스페이스 없음 (글로벌 네임스페이스)**
> - ProjectStS.Utils 어셈블리 소속이나 `namespace` 선언 누락
> - 기본 어셈블리에서 이동할 때 VN 호환성 유지를 위해 의도적으로 유지된 것으로 추정
> - `CsvUtilityVN.cs`가 `CsvUtility.ParseCsvLine()`을 글로벌 네임스페이스로 호출하므로, 네임스페이스 추가 시 VN 쪽도 함께 수정 필요

### D-1-3. EventType using alias — PASS

`using UnityEngine;` + `using ProjectStS.Data;` 조합 사용 파일 32개 중, `EventType`을 실제로 사용하는 4개 파일 모두 alias 보유:
- `StageState.cs`, `StageManager.cs`, `GameFlowController.cs`, `CsvToSOImporter.cs`

---

## D-2. 코딩 컨벤션 검증

### D-2-1. 네이밍 규칙 — PASS
- public/private 필드, 이벤트(On+PascalCase), bool(Is/Has/Can), 메서드 전체 준수
- 데이터 모델 DTO의 camelCase 공개 필드는 JSON 직렬화 호환용으로 허용

### D-2-2. XML 문서 주석 — PASS
- 핵심 6개 파일 전수 검증: 모든 public 멤버 `///` 보유
- private 메서드까지 문서화 (컨벤션 초과 달성)

### D-2-3. #region 사용 — PASS
- 48개 파일에서 247개 `#region` 블록 사용
- 템플릿 필수 영역 (Serialized Fields, Private Fields, Public Properties 등) 모두 준수
- 단순 DTO/Enum 파일은 #region 생략 (적절)

### D-2-4. [SerializeField] private 패턴 — PASS
- MonoBehaviour/SO 클래스: public 필드 노출 0건, 전부 `[SerializeField] private` 사용
- 데이터 모델 DTO: public 필드 사용 (Newtonsoft.Json 직렬화 호환 — 허용)

### D-2-5. 매직 넘버 — CONDITIONAL PASS (Low 4건)

> **[Low]** 아래 위치에 게임 밸런스 관련 매직 넘버 존재:
> 1. `StageResultCalculator.cs:98` — `3f` (이벤트당 보상 비율)
> 2. `StageResultCalculator.cs:100` — `5` (최대 고정 보상 수)
> 3. `DeckManager.cs:261-264` — `6, 5, 4` (인원별 카드 수) + `ExpeditionLauncher.cs:224-227` 동일
> 4. `EventRewardProcessor.cs:211-215` — `50f, 25f, 15f, 7.5f, 2.5f` (폴백 드랍율)

---

## D-3. 금지 사항 검증

| 항목 | 결과 | 비고 |
|:-----|:-----|:-----|
| D-3-1 런타임 탐색 (Find/FindObjectOfType) | **PASS** | 0건 |
| D-3-2 싱글톤 남용 | **PASS** | ServiceLocator 패턴만 사용, static Instance 0건 |
| D-3-3 Update() GC Alloc | **PASS** | Update/LateUpdate/FixedUpdate 메서드 자체가 **0건** (이벤트 구독 방식 일관) |
| D-3-4 Resources 사용 | **PASS** | 0건 |

> **[Low 참고]** `GameBootstrapper.cs:49` — `GetComponent("IntegrationBootstrap")` 문자열 기반 컴포넌트 조회. 어셈블리 경계 제약으로 제네릭 버전 사용 불가한 의도적 설계이나, 리네이밍 시 깨질 수 있음.

---

## D-4. 데이터 모델 vs 데이터 구조 가이드 대조

### 모델 필드 대조 (23개 모델)

| 모델 | 결과 | 비고 |
|:-----|:-----|:-----|
| UnitData | **PARTIAL** | EXTRA: `maxAP` 가이드에 없음 |
| CardData | PASS | |
| CardEffectData | PASS | `AddCardId` → `addCardId` 케이싱 차이 |
| SkillData | PASS | 가이드 오타 `discription` → `description` 교정 |
| ItemData | PASS | |
| StatusEffectData | PASS | |
| AreaData | PASS | 가이드 오타 `floarImagePath` → `floorImagePath` 교정 |
| EventData | PASS | |
| RewardTableData | PASS | |
| ElementAffinityData | PASS | |
| DropRateData | PASS | 가이드 오타 `cathegory` → `category` 교정 |
| OwnedUnitData | PASS | 가이드 오타 `epuipItem` → `equipItem` 교정 |
| InventoryItemData | PASS | 가이드 오타 `cathegory` → `category` 교정 |
| InGameBagItemData | PASS | 동일 |
| ExplorationRecordData | PASS | |
| EnemyCombinationData | PASS | `enemyUnit_1` → `enemyUnit1` 언더스코어 제거 |
| AIPatternData | PASS | |
| AIPatternRuleData | PASS | `aipatternId` → `aiPatternId` 케이싱 교정 |
| AIConditionData | PASS | |
| CampaignData | PASS | |
| CampaignGoalGroupData | PASS | 가이드 오타 2건 교정 (`isEssencial`, `additianalRewards`) |
| BattleActionData | PASS | |
| BattleTimelineData | PASS | 가이드 오타 `actionGrorpId` → `actionGroupId` 교정 |

> **총 12건의 가이드 오타를 코드에서 교정** — 의도적이고 적절한 변경.

### Critical/High 이슈

> **[Critical] CardEffectRow (CsvRowDefinitions) — `value` 타입 불일치 + `duration` 필드 추가**
> - `CardEffectRow.value`가 `int`로 선언되어 있으나 가이드와 모델(`CardEffectData`)은 `float`
> - CSV 파싱 시 소수점 값 손실 발생
> - `CardEffectRow`에 가이드/모델에 없는 `duration` 필드 존재 — 불필요 또는 미반영 설계

> **[High] CardRow 필드 순서 불일치**
> - CsvRowDefinitions의 `CardRow` 필드 순서가 가이드 CSV 열 순서와 다름
> - `rarity`, `element`, `targetFilter`, `targetSelectionRule` 위치 변경
> - 위치 기반 CSV 파싱 시 잘못된 데이터 매핑 발생 가능

> **[Medium] UnitData.maxAP — 가이드에 미정의**
> - Phase 3에서 행동력 시스템 구현 시 추가됨
> - 가이드 문서에 반영 필요 또는 의도적 추가 확인 필요

### Enum 대조

31개 Enum 전체 대조 완료. 대부분 일치하며 아래 차이점은 의도적:
- `DisposeTrigger`, `SpawnTrigger` — `None` 값 추가 (기본값 필요)
- `TargetSelectionRule` — `HighestHp`, `Self` 추가 (AI 패턴에서 사용)
- `Baton` vs `Wand` — 마스터 플랜에서 "가이드 기준 `Baton` 통일"로 결정

---

## D-5. 서비스 등록/조회 패턴 검증

### D-5-1. 등록 완전성 — PASS

7개 필수 서비스 + 2개 씬 스코프 서비스 모두 등록 확인:

| 서비스 | 등록 위치 | 해제 |
|:-------|:---------|:-----|
| DataManager | GameBootstrapper.Awake | Clear() |
| SceneTransitionManager | GameBootstrapper.Awake | Clear() |
| PlayerDataManager | PlayerDataManager.Awake | OnDestroy |
| IVisualNovelBridge | VisualNovelBridge.Awake | OnDestroy |
| SaveLoadSystem | IntegrationBootstrap.Start | - |
| CampaignManager | IntegrationBootstrap.Start (조건부) | - |
| GameFlowController | GameFlowController.Initialize | OnDestroy |
| StageManager (씬 스코프) | InitializeStage | CleanupStage |
| BattleManager (씬 스코프) | InitializeBattle | CleanupBattle |

### D-5-2. 조회 안전성 — FAIL (High)

> **[High] 15개 `ServiceLocator.Get<T>()` 호출에 null 체크 없음**
> - 전부 `DataManager` 또는 `SceneTransitionManager` 조회
> - 해당 파일: `BattleState.cs`, `BattleManager.cs`, `DeckManager.cs`, `GameFlowController.cs`, `StageManager.cs`
> - `TryGet<T>()` 16개 호출은 모두 안전하게 처리됨
> - GameBootstrapper 초기화 실패 시 연쇄 NullReferenceException 발생 위험
> - **권장**: `TryGet`으로 통일하거나, 최소한 null 체크 추가

### D-5-3. 초기화 순서 — FAIL (High)

> **[High] `GameBootstrapper`에 `[DefaultExecutionOrder]` 미설정**
> - `GameBootstrapper.Awake()`에서 `ServiceLocator.Clear()` 호출
> - `PlayerDataManager.Awake()`와 `VisualNovelBridge.Awake()`도 동일 Awake(기본 순서 0)
> - Unity는 **같은 실행 순서의 Awake 호출 순서를 보장하지 않음**
> - PlayerDataManager가 먼저 등록한 후 GameBootstrapper가 Clear()하면 서비스 소실
> - **권장**: `GameBootstrapper`에 `[DefaultExecutionOrder(-100)]` 추가

---

## D-6. 이벤트 구독/해제 패턴 검증

### D-6-1. 이벤트 구독 누수 — PASS

5개 클래스의 **28개 이벤트 구독** 전체에 대응하는 해제(`-=`) 확인 완료.

| 클래스 | 구독 수 | 해제 수 | 누수 |
|:-------|:------:|:------:|:----:|
| BattleUIBridge | 14 | 14 | 0 |
| StageUIBridge | 7 | 7 | 0 |
| StageManager | 2 | 2 | 0 |
| BattleManager | 2 | 2 | 0 |
| GameFlowController | 3 | 3 | 0 |

### D-6-2. 이벤트 시그니처 일치 — PASS

발행 측과 구독 측의 `Action<>` 파라미터 타입 전체 일치.
`OnDamageDealt` (3-arg) → `OnUnitDamaged` (2-arg) 변환도 정확.

### D-6-3. BattleUIBridge 13개 이벤트 — PASS
### D-6-4. StageUIBridge 이벤트 — PASS

> **[Low 보정]** StageUIBridge는 **9개 이벤트** (당초 플랜에서 10개로 기재되었으나 실제 9개).
> `OnAPDepleted` 핸들러는 no-op (StageManager에서 직접 처리).

---

## D-7. 게임 로직 정합성 검증

| 항목 | 결과 | 비고 |
|:-----|:-----|:-----|
| D-7-1 전투 덱 구성 | **PASS** | 1명=6, 2명=5, 3명=4장, 최대 18장 |
| D-7-2 손패 리필 | **PASS** | 매 턴 4장 리필, Retain 구현 |
| D-7-3 속성 상성 | **PASS** | 데이터 드리븐 (ElementAffinityTableSO). `Baton`은 마스터 플랜 결정사항 |
| D-7-4 스킬 우선순위 | **FAIL** | 아래 상세 |
| D-7-5 행동력(AP) | **PASS** | 파티원 합산, -1/이벤트, 0이면 귀환 |
| D-7-6 고정 보상 계산 | **PASS** | ceil(events/3), 1~5 클램프, 실패 시 Epic 제외 |
| D-7-7 카드 속성 검증 | **PASS** | 같은 속성+Wild, Wild 유닛=전속성 |
| D-7-8 씬 전환 흐름 | **PASS** | Lobby(Single)→Stage(Single)→Battle(Additive)→Unload→Lobby(Single) |

> **[High] D-7-4 스킬 발동 우선순위 — 5단계 중 1단계만 구현**
>
> 기획서 규정 (5단계):
> 1. 예외처리 최우선
> 2. 스킬 > 상태이상
> 3. 버프 > 디버프
> 4. 아군 > 적
> 5. 예외처리 최후
>
> 현재 구현: **4번(아군 > 적) + 포지션 순서만 구현**
>
> 미구현: 1, 2, 3, 5번 규칙 + 보스 > 일반 적 구분
> - `SkillExecutor.SortByPriority()` 확장 필요
> - UnitType에 Boss 값 추가 또는 BattleUnit에 `IsBoss` 플래그 추가 검토

---

## D-8. 성능 가이드라인 검증

| 항목 | 결과 | 비고 |
|:-----|:-----|:-----|
| D-8-1 Queue 사용 | **PASS** | `Queue<RuntimeCard>` 사용, `RemoveAt(0)` 0건 |
| D-8-2 컬렉션 사전 할당 | **FAIL** (Minor) | CsvUtility: 26개 List capacity 미지정 (에디터 타임 코드) |
| D-8-3 문자열 연결 | **FAIL** (Minor) | 3개 파일에서 `+` 연산 (키 생성용, 비핫패스) |
| D-8-4 반복문 LINQ | **PASS** | LINQ는 CsvUtility `.Skip(1)` 뿐, 루프 내 0건 |
| D-8-5 Camera.main | **PASS** | 사용 0건 |

---

## 우선순위별 수정 권장 사항

### Critical (즉시 수정 필요)

| # | 위치 | 이슈 | 수정 방안 |
|:--|:-----|:-----|:---------|
| C-1 | `CsvRowDefinitions.cs` CardEffectRow | `value` 타입이 `int` (가이드/모델은 `float`), 불필요한 `duration` 필드 존재 | `value`를 `float`로 변경, `duration` 제거 또는 가이드에 반영 |

### High (조기 수정 권장)

| # | 위치 | 이슈 | 수정 방안 |
|:--|:-----|:-----|:---------|
| H-1 | `GameBootstrapper.cs` | `[DefaultExecutionOrder]` 미설정 — Awake 순서 미보장 | `[DefaultExecutionOrder(-100)]` 추가 |
| H-2 | 5개 파일, 15개소 | `ServiceLocator.Get<T>()` null 체크 없음 | `TryGet<T>()`으로 교체 또는 null 체크 추가 |
| H-3 | `SkillExecutor.cs` SortByPriority | 5단계 우선순위 중 1단계만 구현 | 나머지 4단계 우선순위 로직 구현 |
| H-4 | `CsvRowDefinitions.cs` CardRow | CSV 필드 순서가 가이드와 불일치 | 가이드 열 순서에 맞게 재정렬 |

### Medium

| # | 위치 | 이슈 |
|:--|:-----|:-----|
| M-1 | `CsvRowDefinitions.cs`, `CsvUtility.cs` | 네임스페이스 누락 (글로벌 네임스페이스) |
| M-2 | `UnitData.cs` | `maxAP` 필드가 가이드에 미정의 (가이드 업데이트 필요) |
| M-3 | `CsvUtility.cs` | 26개 List 생성 시 capacity 미지정 |
| M-4 | 3개 파일 | 문자열 키 생성에 `+` 연산 사용 (`$""` 권장) |
| M-5 | StageUIBridge | 이벤트 수 9개 (플랜/현재 개발 상태에 10개로 기재 — 문서 수정 필요) |

### Low

| # | 위치 | 이슈 |
|:--|:-----|:-----|
| L-1 | `ProjectStS.Editor.asmdef` | Integration 참조 누락 |
| L-2 | `GameBootstrapper.cs:49` | 문자열 기반 GetComponent (어셈블리 경계 제약) |
| L-3 | `StageResultCalculator.cs` | 매직 넘버 `3f`, `5` |
| L-4 | `DeckManager.cs` + `ExpeditionLauncher.cs` | 매직 넘버 `6, 5, 4` (인원별 카드 수) |
| L-5 | `EventRewardProcessor.cs` | 폴백 드랍율 매직 넘버 |
| L-6 | `CsvUtility.cs` | `.Skip(1)` LINQ 사용 (에디터 타임, 비핫패스) |
| L-7 | `GameFlowController.OnDestroy` | Stage 활성 상태에서 파괴 시 CleanupStage 미호출 (DontDestroyOnLoad이므로 실질 영향 없음) |

---

## 사용자(Unity 에디터) 수행 필수 항목 체크리스트

### Step 1: 컴파일 확인 [Critical]
- [ ] Unity 에디터 열기 → Console 에러 0건 확인
- [ ] "Failed to resolve assembly" Burst 경고 부재 확인

### Step 2: SO 생성 및 할당 [Critical]
- [ ] GameSettings SO 생성 (`_Project/ScriptableObjects/`)
- [ ] GameSettings 값 설정: ProtagonistUnitId, MaxPartySize(3), MaxDeckSize(6), MinDeckSize(4), LobbySceneName, BattleSceneName
- [ ] 19개 마스터 테이블 SO 에셋 생성
- [ ] DataManager 컴포넌트에 19개 SO + GameSettings 할당

### Step 3: 씬 설정 [Critical]
- [ ] Build Settings에 BootScene(0), LobbyScene(1), StageScene(2), BattleScene(3) 등록
- [ ] BootScene: GameBootstrapper + DataManager + SceneTransitionManager + IntegrationBootstrap 배치
- [ ] LobbyScene: PlayerDataManager 배치
- [ ] StageScene: StageSceneBootstrap + StageManager 연결
- [ ] BattleScene: BattleSceneBootstrap + BattleManager 연결

### Step 4: CSV 데이터 [Critical]
- [ ] 19개 테이블용 CSV 파일 작성 (최소 테스트용 1~2행)
- [ ] Units.csv에 `maxAP` 열 추가 (인덱스 7번)
- [ ] CsvToSOImporter 메뉴에서 일괄 임포트 실행
- [ ] 임포트 결과 SO 인스펙터에서 확인

### Step 5: VN 연동 [High]
- [ ] VisualNovelBridge 컴포넌트를 VN 오버레이 프리팹에 추가
- [ ] `_player`, `_vnRoot` SerializeField 연결
- [ ] Episode Registry 매핑 (episodeId → VisualNovelSO)
- [ ] VN 에디터 CSV Import/Export 정상 동작 확인

### Step 6: 런타임 통합 테스트 [High]
- [ ] Boot → Lobby 자동 전환 확인
- [ ] ServiceLocator 등록 확인 (Debug.Log 또는 브레이크포인트)
- [ ] SaveLoadSystem.Save() → JSON 생성 → Load() 복원 확인
- [ ] Lobby → Stage → Battle(Additive) → Stage(Unload) → Lobby 전체 흐름

---

*작성자: Claude Code | 검증 대상: 103개 .cs 파일, 9개 asmdef, 23개 데이터 구조 가이드 문서*
