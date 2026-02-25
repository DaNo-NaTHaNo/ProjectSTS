# Phase 3: 스테이지(월드맵) 시스템 작업 내역

*작성일: 2026.02.25*
*커밋: `feat(stage): Phase 3 스테이지/월드맵 시스템 핵심 로직 전체 구현`*

---

## 1. 개요

Phase 3는 육각형 방사형 월드맵 탐험의 핵심 로직 레이어를 구현한 단계이다.
총 **11개 C# 스크립트**, **2,919줄**이 신규 작성되었으며, 기존 **5개 파일**이 수정되었다.
UI/프리팹 없이 순수 로직만 구축하였다.

**네임스페이스**: `ProjectStS.Stage`

---

## 2. 파일 목록 및 역할

### 2.1 WorldMap (그리드 구조)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/WorldMap/HexNode.cs` | 130 | 육각 셀 런타임 데이터. 큐브 좌표(Q,R,S), Level, 구역 ID, 이벤트, 상태 플래그, 6방향 오프셋 상수 |
| `Stage/WorldMap/HexGridGenerator.cs` | 174 | 큐브 좌표 방사형 그리드 생성. 링 순회 알고리즘, 인접 노드 연결, 레벨/좌표 기반 조회 API |
| `Stage/WorldMap/ZoneManager.cs` | 308 | 구역 할당/조회. 큐브 좌표 최대 절대값 기반 6방위 판별, 경계 영역(q=0∥r=0∥s=0) 판별 |

### 2.2 Events (이벤트 배치/보상)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/Events/EventPlacementSystem.cs` | 517 | 이벤트 배치. 구역별 풀, 레벨별 수량 계산, 인접성 규칙, SpawnTrigger 평가, 레어도 가중 랜덤 |
| `Stage/Events/EventRewardProcessor.cs` | 354 | 이벤트/완료/추가 보상 롤링. RewardTableSO + DropRateTableSO 기반 가중 랜덤, Card/Item 조회 |

### 2.3 Core (오케스트레이터/상태)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/StageManager.cs` | 382 | 스테이지 오케스트레이터(MonoBehaviour). 서브시스템 생성·의존성 주입·이벤트 연결·외부 API 제공 |
| `Stage/StageState.cs` | 159 | 스테이지 공유 상태 컨테이너. 그리드, 현재 노드, AP, 방문 이력, 탐험 기록, 보상 카운트 |
| `Stage/StageUIBridge.cs` | 175 | 스테이지→UI 이벤트 브릿지. 10개 통합 이벤트 재발행, 서브시스템 이벤트 구독/해제 |

### 2.4 Exploration (탐험)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/ExplorationManager.cs` | 324 | 이동/행동력(AP) 관리/시야 제어. 구역 첫 진입 시 전체 공개, 인접 노드 공개, 캠페인 목표 노드 표시 |

### 2.5 Bag (인게임 가방)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/InGameBagManager.cs` | 159 | 탐험 중 획득 아이템 관리. isNewForNow 필터, 레어도 필터, Epic 제외 필터 |

### 2.6 Result (보상 정산)

| 파일 | 줄 수 | 역할 |
|:---|:---:|:---|
| `Stage/StageResultCalculator.cs` | 237 | 스테이지 완료 보상 정산. 고정/완료/추가 보상 계산 + StageSettlementData 정의 |

### 2.7 기존 파일 수정 (5개)

| 파일 | 변경 내용 |
|:---|:---|
| `Data/Models/UnitData.cs` | `maxAP` 필드 추가 (유닛의 행동력, 파티 합류 시 가산) |
| `Data/Enums/StageEnums.cs` | `StagePhase`, `StageResult`, `StageEndReason` 열거형 추가 |
| `Scripts/Utilities/CsvRowDefinitions.cs` | `UnitRow`에 `maxAP` 필드 추가 |
| `Scripts/Utilities/CsvUtility.cs` | `ParseUnits`에 `maxAP` 파싱 추가 (인덱스 7, 이후 필드 시프트) |
| `Scripts/Editor/CsvToSOImporter.cs` | `ImportUnits`에 `maxAP = row.maxAP` 매핑 추가 |

---

## 3. 아키텍처

### 3.1 클래스 의존성

```
StageManager (MonoBehaviour, ServiceLocator 등록)
 ├─ StageState               ← 공유 상태
 ├─ HexGridGenerator         ← 방사형 그리드 생성
 │   └─ HexNode              ← 큐브 좌표 기반 육각 셀
 ├─ ZoneManager              ← 구역 할당 (방위/경계 판별)
 ├─ EventPlacementSystem     ← 이벤트 배치 (가중 랜덤)
 ├─ EventRewardProcessor     ← 보상 롤링 (드랍 테이블)
 ├─ ExplorationManager       ← 이동/AP/시야 제어
 ├─ InGameBagManager         ← 인게임 가방
 ├─ StageResultCalculator    ← 완료 보상 정산
 └─ StageUIBridge            ← UI 이벤트 브릿지
```

### 3.2 탐험 흐름

```
StageInitialize (그리드 생성, 구역 할당, 이벤트 배치, AP 합산, 시작점 배치)
  → Exploration (노드 이동 → 시야 공개 → 구역 첫 진입 시 전체 공개)
    → [이벤트 없음] → 다음 이동
    → [이벤트 있음] → EventExecuting (AP 1 소모)
      → 전투/VN 씬 전환 → OnEventCompleted 콜백
        → [성공] → 보상 처리 + 추가 보상 카운트 + 종료 조건 확인
        → [실패] → 전투 패배=PartyWipe, 기타=EventFailed → StageEnd
    → [AP 고갈] → Victory(APDepleted) → StageEnd
    → [보스 클리어] → Victory(BossCleared) → StageEnd
  → StageEnd → CalculateSettlement (고정/완료/추가 보상)
```

### 3.3 설계 원칙

| 원칙 | 적용 |
|:---|:---|
| **Phase 2 패턴 미러** | BattleManager/State/UIBridge 구조를 StageManager/State/UIBridge에 동일 적용 |
| **싱글톤 회피** | ServiceLocator 패턴으로 StageManager 등록 |
| **이벤트 구독 방식** | Update() 사용 없이 Action 이벤트로 시스템 간 통신 (6개 이벤트 소스, 10개 UI 이벤트) |
| **컬렉션 사전 할당** | 모든 List/Dictionary/HashSet에 capacity 지정 |
| **UI 분리** | StageUIBridge가 이벤트 재발행, 실제 UI 구현은 후속 Phase 3.5 |
| **데이터 구조 가이드 준수** | AreaData, EventData, RewardTableData 등 기존 모델 활용 |
| **외부 조건 콜백** | SpawnTrigger의 OwnCharacter/OwnItem 평가를 Func 콜백으로 위임 |

---

## 4. 핵심 구현 사항

### 4.1 큐브 좌표 방사형 그리드

- **좌표계**: 큐브 좌표 `(Q, R, S)` where `Q + R + S = 0`
- **레벨**: `max(|Q|, |R|, |S|)` = 시작점(0,0,0)으로부터의 거리
- **링 순회 알고리즘**: 시작점 `(n, -n, 0)`에서 6방향으로 각 n번 이동
- **6방향 오프셋**: `N(0,-1,+1)`, `NE(+1,-1,0)`, `SE(+1,0,-1)`, `S(0,+1,-1)`, `SW(-1,+1,0)`, `NW(-1,0,+1)`
- **노드 수**: 레벨 0~28 → `1 + 3 × 28 × 29 = 2,437`개
- **인접 연결**: 6방향 이웃 자동 연결

### 4.2 구역 할당

- **레벨 0~7**: 중앙 구역 (AreaData의 `areaCardinalPoint`에 "C" 포함)
- **레벨 8~28 (비경계)**: 큐브 좌표 최대 절대값 성분 기반 6방위 결정
  ```
  |s| 최대 → s<0: N, s>0: S
  |q| 최대 → q>0: NE, q<0: SW
  |r| 최대 → r<0: SE, r>0: SW
  ```
- **경계 영역**: 레벨 8+ 에서 `q=0 ∥ r=0 ∥ s=0` → 고난이도 이벤트, 속성 아이콘 비표시

### 4.3 이벤트 배치

- **경계 영역**: 모든 칸에 이벤트 강제 배치
- **일반 구역**: 레벨별 순회
  - 이벤트 수: `ceil(칸수/2)` ~ `max(ceil(칸수/2), 칸수-레벨)` 범위 내 랜덤
  - **인접성 규칙**: 레벨 n에 이벤트가 있는 셀과 인접한 레벨 n+1 셀 우선 선택
- **이벤트 선택**:
  - 구역(areaId)별 이벤트 풀 필터링
  - 레벨 범위 (`minLevel` ~ `maxLevel`) 체크
  - SpawnTrigger 조건 평가
  - 레어도별 가중치 (DropRateTableSO의 SpawnEvent 카테고리)
  - Fisher-Yates 셔플 + 가중 랜덤 선택

### 4.4 SpawnTrigger 평가

| Trigger | 평가 방법 |
|:---|:---|
| `None` | 항상 통과 |
| `ClearCampaign` | `record.countComplete` vs `spawnTriggerValue` (ComparisonOperator) |
| `WinToBoss` | `record.eliminatedBossId`에 해당 ID 포함 여부 |
| `OwnCharacter` / `OwnItem` | 외부 콜백 `OnExternalConditionCheck` 위임 |

### 4.5 행동력(AP) 시스템

- **초기값**: 파티원 `UnitData.maxAP` 합산
- **소모**: 이벤트 실행(전투/VN/사건) 시 1 소모
- **이동만**: 이벤트 없는 노드 이동은 AP 소모 없음
- **고갈**: AP = 0 → `StageResult.Victory` + `StageEndReason.APDepleted`

### 4.6 시야 시스템

- **시작**: 시작점 + 인접 노드 공개, 중앙 구역 전체 공개
- **이동 시**: 현재 노드의 인접 노드 자동 공개
- **구역 진입 시**: 해당 구역 전체 노드 일괄 공개 (최초 진입 1회)
- **캠페인 목표**: 목표 이벤트 노드는 항상 공개 (위치 표시)

### 4.7 보상 정산

| 보상 유형 | 수량 | 조건 |
|:---|:---|:---|
| **고정 보상** | `ceil(completedEventCount / 3)` (1~5) | 신규 아이템 중 선택. 실패 시 Epic 제외 |
| **완료 보상** | BossCleared/CampaignGoal = 3개, APDepleted/EventEscape = 1개 | 드랍 테이블 랜덤 롤링. 실패 시 없음 |
| **추가 보상** | `BonusRewardCount`개 (Elite/Encounter/VN 완료 시 +1) | 드랍 테이블 랜덤 롤링. 실패 시에도 수령 가능 |

### 4.8 이벤트 보상 롤링

- `RewardTableSO`에서 `rewardId`로 항목 조회 (세미콜론 구분 시 복수 테이블 합산)
- `rewardMinCount` ~ `rewardMaxCount` 범위에서 수량 결정
- `DropRateTableSO`의 EventReward 카테고리에서 레어도별 가중치 적용
- 가중 랜덤으로 아이템 선택 → `InGameBagItemData` 생성 (`isNewForNow = true`)

### 4.9 기본 드랍율 (DropRateTableSO 미설정 시)

| 레어도 | SpawnEvent | EventReward |
|:---|:---:|:---:|
| Common | 50 | 50 |
| Uncommon | 25 | 25 |
| Rare | 15 | 15 |
| Unique | 7.5 | 7.5 |
| Epic | 2.5 | 2.5 |

---

## 5. 외부 의존성

| 의존 대상 | 용도 |
|:---|:---|
| `DataManager` (ServiceLocator) | 마스터 데이터 조회 (Areas, Events, Rewards, DropRates, Units, Cards, Items) |
| `ServiceLocator` (Core) | StageManager 등록/해제 |
| `Data/Models/*` (Phase 0~1) | AreaData, EventData, RewardTableData, DropRateData, InGameBagItemData, ExplorationRecordData, OwnedUnitData, UnitData, CardData, ItemData |
| `Data/Enums/*` (Phase 0~1) | EventType, SpawnTrigger, CardinalPoint, InventoryCategory, DropRateCategory, ComparisonOperator, Rarity, StagePhase, StageResult, StageEndReason |

---

## 6. UI 연동 준비

`StageUIBridge`가 10개 통합 이벤트를 제공한다:

| 이벤트 | 시그니처 |
|:---|:---|
| OnNodeMoved | `Action<HexNode>` |
| OnEventTriggered | `Action<HexNode, EventData>` |
| OnEventCompleted | `Action<EventData, bool>` |
| OnAPChanged | `Action<int, int>` |
| OnZoneRevealed | `Action<string>` |
| OnNodeRevealed | `Action<HexNode>` |
| OnBagItemAdded | `Action<InGameBagItemData>` |
| OnStagePhaseChanged | `Action<StagePhase>` |
| OnStageResultShown | `Action<StageResult, StageEndReason>` |

추가로 `StageManager`의 직접 이벤트:

| 이벤트 | 시그니처 |
|:---|:---|
| OnStageInitialized | `Action` |
| OnStageEnded | `Action<StageResult, StageEndReason>` |

---

## 7. 디렉터리 구조

```
Assets/_Project/Scripts/Runtime/Stage/
├── Events/
│   ├── EventPlacementSystem.cs
│   └── EventRewardProcessor.cs
├── WorldMap/
│   ├── HexNode.cs
│   ├── HexGridGenerator.cs
│   └── ZoneManager.cs
├── ExplorationManager.cs
├── InGameBagManager.cs
├── StageManager.cs
├── StageResultCalculator.cs
├── StageState.cs
├── StageUIBridge.cs
└── ProjectStS.Stage.asmdef
```

---

## 8. 후속 작업 (Phase 3.5 이후)

- [ ] 스테이지 UI 프리팹/레이아웃 구현 (StageUIBridge 이벤트 구독)
- [ ] 월드맵 씬: 노드 아이콘 배치, 이동 경로 표시, 구역 시각화
- [ ] AP/가방 UI: 행동력 게이지, 인게임 가방 UI
- [ ] 보상 정산 UI: StageSettlementData 표시 (고정/완료/추가 보상 선택)
- [ ] Phase 4: 메타 레이어(로비) — PartyEditManager, DeckEditManager, InventoryManager, CampaignManager
- [ ] Phase 5: 통합 (GameFlow, SaveLoad, Audio)

---

*마지막 업데이트: 2026.02.25 | 작성자: Claude Code*
