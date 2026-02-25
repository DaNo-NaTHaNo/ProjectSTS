# Phase 2: 전투 시스템 작업 내역

*작성일: 2026.02.25*
*커밋: `feat(battle): Phase 2 전투 시스템 핵심 로직 전체 구현`*

---

## 1. 개요

Phase 2는 턴제 카드 전투의 핵심 로직 레이어를 구현한 단계이다.
총 **20개 C# 스크립트**, **4,944줄**이 신규 작성되었으며, UI/프리팹 없이 순수 로직만 구축하였다.

**네임스페이스**: `ProjectStS.Battle`

---

## 2. 파일 목록 및 역할

### 2.1 Core (전투 골격)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Core/BattleEnums.cs` | 54 | `BattlePhase`, `BattleResult`, `BattleEndReason` 열거형 정의 |
| `Battle/Core/BattleState.cs` | 255 | 전투 공유 상태 컨테이너. 아군/적 유닛, 턴, 에너지, 웨이브, 결과 데이터 보유 |
| `Battle/Core/BattleUnit.cs` | 313 | 런타임 유닛. HP/방어도/상태이상/스킬 추적. Factory 패턴으로 아군/적 생성 |
| `Battle/Core/BattleManager.cs` | 329 | 전투 오케스트레이터(MonoBehaviour). 모든 서브시스템 생성·의존성 주입·이벤트 연결·외부 API 제공 |

### 2.2 Card (카드/덱 시스템)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Card/RuntimeCard.cs` | 145 | CardData 런타임 래퍼. ModifyCard에 의한 코스트/대미지/방어도 임시 보정 추적 |
| `Battle/Card/DeckManager.cs` | 284 | Queue 기반 덱 관리. 전투 덱 구축(파티원 합산+압축), 드로우, 디스카드, 리셔플 |
| `Battle/Card/HandManager.cs` | 208 | 손패 관리. 리필(4장), 리테인, 카드 사용/추가, 타입·속성별 필터 |
| `Battle/Card/CardExecutor.cs` | 551 | 카드 효과 실행 허브. 8개 CardEffectType 분기, 타겟 선택, 체인 깊이 관리(MAX_CHAIN_DEPTH=10) |

### 2.3 TurnSystem (턴 흐름)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/TurnSystem/TurnManager.cs` | 229 | 페이즈 상태 머신. BattleStart → TurnStart → PlayerAction → EnemyAction → TurnEnd 사이클 전환 |
| `Battle/TurnSystem/PhaseHandler.cs` | 197 | 각 페이즈의 구체 실행 로직. 에너지 회복, 손패 리필, AI 결정, 상태이상 처리, 웨이브 전환 |

### 2.4 Skill (스킬 자동 발동)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Skill/SkillExecutor.cs` | 332 | 트리거 조건 평가, 우선순위 정렬(아군>적, Position순), 사용 제한(CoolDown/PerTurn/PerBattle) 체크 후 CardExecutor를 통해 발동 |

### 2.5 StatusEffect (상태이상)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/StatusEffect/ActiveStatusEffect.cs` | 123 | StatusEffectData 런타임 래퍼. 스택/duration/소모형(expendable) 관리 |
| `Battle/StatusEffect/StatusEffectManager.cs` | 402 | 상태이상 적용/제거, 턴 시작·종료 타이밍 처리, 대미지/방어도/감소 보정값 조회, DoT/HoT 실행 |

### 2.6 Calculator (수치 계산)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Calculator/DamageCalculator.cs` | 142 | static 대미지 계산 유틸. 속성 상성 배율 × 상태이상 보정 × 방어도 적용, 반사 대미지 |
| `Battle/Calculator/ItemEffectProcessor.cs` | 347 | 아이템 효과 처리. Equipment 패시브, HasDamage/HasDown 트리거, modifyValue 수식 파싱(+/-/*) |

### 2.7 AI (적 행동)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/AI/BattleAI.cs` | 468 | 3단 계층 AI: Pattern → Rules(우선순위) → Conditions(AND). 행동 타입: EarnCard, PlayCard, Pass. Stun 시 행동 불가 |

### 2.8 Result (승패 판정)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Result/BattleResultHandler.cs` | 57 | 적 전멸=Victory, 아군 전멸=Defeat, 이벤트 강제=EventTriggered |
| `Battle/Result/BattleUIBridge.cs` | 260 | 전투 서브시스템 이벤트를 UI용 통합 이벤트로 재발행하는 브릿지 |

### 2.9 Timeline (전투 이벤트 연출)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Battle/Timeline/BattleTimelineManager.cs` | 224 | 전투 중 시나리오 트리거 감지 + 액션 그룹 실행. 핸들러 미설정 시 트리거만 감지(스텁) |
| `Battle/Timeline/IBattleTimelineHandler.cs` | 24 | 연출 실행 인터페이스. 후속 Phase에서 구현 예정 |

---

## 3. 아키텍처

### 3.1 클래스 의존성

```
BattleManager (MonoBehaviour, ServiceLocator 등록)
 ├─ BattleState          ← 공유 상태
 ├─ TurnManager          ← 페이즈 상태 머신
 │   └─ PhaseHandler     ← 페이즈 실행 로직
 ├─ DeckManager          ← Queue<RuntimeCard> 기반 덱
 ├─ HandManager          ← 손패 관리 (Retain/Refill)
 ├─ CardExecutor         ← 카드 효과 실행 (8개 EffectType)
 ├─ SkillExecutor        ← 스킬 트리거 → CardExecutor 호출
 ├─ StatusEffectManager  ← 상태이상 스택/타이밍
 ├─ DamageCalculator     ← static 대미지 계산
 ├─ ItemEffectProcessor  ← 아이템 패시브/트리거
 ├─ BattleAI             ← 적 AI 3단 계층
 ├─ BattleResultHandler  ← 승패 판정
 ├─ BattleUIBridge       ← UI 이벤트 브릿지
 └─ BattleTimelineManager ← 전투 연출 트리거
```

### 3.2 턴 흐름

```
BattleStart (덱 구축, 아이템 패시브)
  → TurnStart (턴++, Block 리셋, 에너지 회복, 손패 리필, AI 결정, 턴 시작 상태이상)
    → PlayerAction (카드 사용 → 효과 실행 → 스킬 트리거 체크)
      → EnemyAction (AI 결정 실행, Stun 시 스킵)
        → TurnEnd (턴 종료 상태이상, duration 틱, 승패 체크)
          → [WaveTransition] (적 전멸 + 남은 웨이브 → 다음 웨이브)
          → [BattleEnd] (승리/패배 확정)
```

### 3.3 설계 원칙

| 원칙 | 적용 |
|:---|:---|
| **싱글톤 회피** | ServiceLocator 패턴으로 BattleManager 등록 |
| **이벤트 구독 방식** | Update() 사용 없이 Action 이벤트로 시스템 간 통신 |
| **Queue 기반 덱** | `List.RemoveAt(0)` 대신 `Queue<RuntimeCard>` 사용 |
| **컬렉션 사전 할당** | 모든 List/Queue/Dictionary에 capacity 지정 |
| **체인 깊이 제한** | 카드 → 스킬 → 효과 재귀 호출 최대 10단계로 무한 루프 방지 |
| **UI 분리** | BattleUIBridge가 이벤트 재발행, 실제 UI 구현은 후속 Phase |
| **연출 인터페이스** | IBattleTimelineHandler로 연출 시스템 의존성 역전 |

---

## 4. 핵심 구현 사항

### 4.1 전투 덱 구축 (덱 압축 규칙)

파티 인원수에 따라 유닛당 포함 카드 수가 제한된다:
- 1인 파티: 6장 (제외 없음)
- 2인 파티: 5장 (6번째 카드 제외)
- 3인 파티: 4장 (5~6번째 카드 제외)

최대 전투 덱: 18장. Fisher-Yates 셔플 후 Queue에 적재.

### 4.2 카드 효과 실행 (8개 EffectType)

| EffectType | 동작 |
|:---|:---|
| Damage | 속성 상성 × 상태이상 보정 → 방어도 차감 → HP 피해 → 반사 대미지 |
| Block | 카드/상태이상 보정 후 방어도 추가 |
| Heal | 대상 HP 회복 |
| Energy | 전투 에너지 증감 |
| ApplyStatus | 상태이상 부여 (스택/duration 리프레시) |
| ModifyCard | 손패 카드 수치 변경 (UntilPlayed/Combat 기간) |
| Draw | 추가 드로우 → 손패 추가 |
| AddCard | 지정 카드를 손패에 직접 생성 |

### 4.3 속성 상성

`ElementAffinityTableSO` 조회. DataManager.GetElementAffinity()로 배율 반환.
- 약점: 150%, 내성: 75%, 무속성(Wild): 100%
- Sword↔Medal, Wand↔Grail 상호 약점/내성
- Sola↔Luna 상호 약점

### 4.4 상태이상

- **스택형** (`isStackable`): maxStacks까지 누적
- **비스택형**: duration만 리프레시
- **소모형** (`isExpendable`): 효과 적용 시 expendCount만큼 스택 소모
- **발동 타이밍**: TurnStart, TurnEnd, OnAttack, OnDamage
- **효과 타입**: DamageOverTime, HealOverTime, ModifyDamage, ReduceDamage, ModifyBlock, ReflectDamage, Stun

### 4.5 스킬 발동 우선순위

1. 아군(0) > 적(1)
2. 같은 그룹 내 Position 오름차순 (아군 1→2→3, 적 배치 순서)
3. 사용 제한: CoolDown(턴 기반), PerTurn(턴당 횟수), PerBattle(전투당 횟수)

### 4.6 AI 행동 결정

```
AIPattern
 └─ Rules (priority 내림차순 평가)
     └─ Conditions (AND 조건)
         ├─ TurnCount, TurnMod
         ├─ HpPercent, EnemyHpPercent
         ├─ HasCard, StatusActive
         └─ ComparisonOperator (==, !=, >, <, >=, <=)
```

조건 충족 Rule 없으면 defaultAction 실행. Stun 시 행동 불가.

### 4.7 아이템 효과

- **Equipment**: 전투 시작 시 패시브 적용 (MaxHP, MaxEnergy 보정 등)
- **HasDamage/HasDown**: 피격/전사 시 트리거
- **modifyValue 수식**: `+100`, `-20`, `*1.5` 형식 파싱
- **isDisposable**: disposePercentage 확률로 소모

### 4.8 전투 이벤트 타임라인

- 이벤트 ID 기반 타임라인 로드
- 트리거 타입: TurnCount, HpPercent, UnitDown, UnitSpawn, OnStatus, EnemyCount, PartyCount
- `IBattleTimelineHandler` 인터페이스로 연출 실행 위임 (현재 Phase는 스텁)
- `isRepeatable` 플래그로 1회/반복 실행 구분

---

## 5. 외부 의존성

| 의존 대상 | 용도 |
|:---|:---|
| `DataManager` (ServiceLocator) | 마스터 데이터 조회 (Card, Unit, Skill, StatusEffect, AIPattern, ElementAffinity 등) |
| `ServiceLocator` (Core) | BattleManager 등록/해제 |
| `Data/Models/*` (Phase 0~1) | CardData, UnitData, SkillData, StatusEffectData, AIPatternData, ItemData 등 23개 모델 |
| `Data/Enums/*` (Phase 0~1) | ElementType, CardEffectType, StatusEffectType, AIActionType 등 31개 Enum |

---

## 6. UI 연동 준비

`BattleUIBridge`가 13개 통합 이벤트를 제공한다:

| 이벤트 | 시그니처 |
|:---|:---|
| OnPhaseChanged | `Action<BattlePhase>` |
| OnCardAddedToHand | `Action<RuntimeCard>` |
| OnCardRemovedFromHand | `Action<RuntimeCard>` |
| OnCardPlayed | `Action<BattleUnit, RuntimeCard>` |
| OnUnitDamaged | `Action<BattleUnit, int>` |
| OnUnitHealed | `Action<BattleUnit, int>` |
| OnUnitBlockGained | `Action<BattleUnit, int>` |
| OnUnitDefeated | `Action<BattleUnit>` |
| OnStatusEffectChanged | `Action<BattleUnit, ActiveStatusEffect>` |
| OnEnergyChanged | `Action<int>` |
| OnEnemyIntentShown | `Action<BattleUnit, AIDecision>` |
| OnSkillActivated | `Action<BattleUnit, SkillData>` |
| OnBattleResultShown | `Action<BattleResult, BattleEndReason>` |

---

## 7. 디렉터리 구조

```
Assets/_Project/Scripts/Runtime/Battle/
├── AI/
│   └── BattleAI.cs
├── Calculator/
│   ├── DamageCalculator.cs
│   └── ItemEffectProcessor.cs
├── Card/
│   ├── CardExecutor.cs
│   ├── DeckManager.cs
│   ├── HandManager.cs
│   └── RuntimeCard.cs
├── Core/
│   ├── BattleEnums.cs
│   ├── BattleManager.cs
│   ├── BattleState.cs
│   └── BattleUnit.cs
├── Result/
│   ├── BattleResultHandler.cs
│   └── BattleUIBridge.cs
├── Skill/
│   └── SkillExecutor.cs
├── StatusEffect/
│   ├── ActiveStatusEffect.cs
│   └── StatusEffectManager.cs
├── Timeline/
│   ├── BattleTimelineManager.cs
│   └── IBattleTimelineHandler.cs
└── TurnSystem/
    ├── PhaseHandler.cs
    └── TurnManager.cs
```

---

## 8. 후속 작업 (Phase 3 이후)

- [ ] 전투 UI 프리팹/레이아웃 구현 (BattleUIBridge 이벤트 구독)
- [ ] IBattleTimelineHandler 구현 (VN 재생, 컷인, BGM 변경 등 연출)
- [ ] 전투 씬 통합 (BattleManager 초기화 → 스테이지 연결)
- [ ] Phase 3: 스테이지/월드맵 시스템
- [ ] Phase 4: 메타 레이어(로비)
- [ ] Phase 5: 통합 (GameFlow, SaveLoad, Audio)

---

*마지막 업데이트: 2026.02.25 | 작성자: Claude Code*
