# Plan: Phase 5 — Integration (게임 루프 통합, 저장/불러오기, VN 브릿지)

## 분석

### 요청 요약
Phase 5 구현: GameFlowController, SaveLoadSystem, VisualNovelBridge, RewardSettlementProcessor 로직 구현.
UI 레이아웃/프리팹은 사용자가 직접 작업하므로 로직만 구현한다.

### 핵심 설계 결정
- **전투 씬**: Additive 로딩 (스테이지 씬 위에 전투 씬 추가)
- **VN/Encounter**: 오버레이 (씬 전환 없이 프리팹 기반 재생)
- **새 어셈블리**: `ProjectStS.Integration` — 모든 레이어를 참조하는 통합 계층

### 아키텍처 제약 해결
- Core asmdef → Data만 참조 (Battle/Stage/Meta 참조 불가)
- 새 `ProjectStS.Integration` asmdef 생성 → Core, Data, Meta, Battle, Stage 모두 참조
- VisualNovelPlayer는 default assembly(asmdef 없음) → `IVisualNovelBridge` 인터페이스를 Core에 정의, 구현체는 VisualNovel 디렉터리에 배치

### 참조한 문서
- 구현 마스터 플랜.md (Phase 5 명세)
- 현재 개발 상태.md
- 데이터 구조 가이드 (OwnedUnitData, InventoryItemData, ExplorationRecordData)
- 각 씬(레이어)에 대한 규정.md

### 영향 범위

**신규 파일 (10개):**
| # | 파일 | 어셈블리 | 역할 |
|---|------|----------|------|
| 1 | `Integration/ProjectStS.Integration.asmdef` | - | 통합 어셈블리 정의 |
| 2 | `Integration/GameFlowController.cs` | Integration | 게임 루프 오케스트레이터 |
| 3 | `Integration/SaveLoadSystem.cs` | Integration | JSON 저장/불러오기 |
| 4 | `Integration/RewardSettlementProcessor.cs` | Integration | 보상 정산 적용 로직 |
| 5 | `Integration/StageSceneBootstrap.cs` | Integration | 스테이지 씬 초기화 브릿지 |
| 6 | `Integration/BattleSceneBootstrap.cs` | Integration | 전투 씬 초기화 브릿지 |
| 7 | `Data/SaveData.cs` | Data | 세이브 데이터 모델 |
| 8 | `Data/Enums/GameFlowEnums.cs` | Data | GameFlowPhase 열거형 |
| 9 | `Core/IVisualNovelBridge.cs` | Core | VN 브릿지 인터페이스 |
| 10 | `VisualNovel/Scripts/Runtime/VisualNovelBridge.cs` | default | VN 브릿지 구현체 |

**수정 파일 (6개):**
| # | 파일 | 변경 내용 |
|---|------|----------|
| 1 | `Data/GameSettings.cs` | LobbySceneName, BattleSceneName 필드 추가 |
| 2 | `Core/GameBootstrapper.cs` | GameFlowController·SaveLoadSystem 등록, 세이브 로드 |
| 3 | `Core/SceneTransitionManager.cs` | UnloadScene 메서드 추가 |
| 4 | `Meta/Campaign/CampaignManager.cs` | 상태 Dictionary getter 3개 추가 |
| 5 | `Meta/PlayerDataManager.cs` | LoadData 일괄 로드 메서드 추가 |
| 6 | `VisualNovel/Scripts/Runtime/VisualNovelPlayer.cs` | OnEpisodeCompleted 이벤트 + 외부 재생 메서드 추가 |

---

## 설계

### 게임 루프 흐름
```
[Boot] → [Lobby] ──탐험 개시──▶ [Stage]
                                   │
                    ┌──────────────┼──────────────┐
                    ▼              ▼              ▼
               [Battle]     [VisualNovel]    [Encounter]
               (Additive)   (Overlay)        (Overlay)
                    │              │              │
                    └──────────────┼──────────────┘
                                   ▼
                              [Stage 복귀]
                                   │
                              AP=0 or Boss
                                   ▼
                             [Settlement] → 자동 저장 → [Lobby]
```

### 씬 로딩 전략
- **Lobby → Stage**: Single 로드
- **Stage → Battle**: Additive 로드 (Stage 유지)
- **Battle → Stage**: Battle 씬 Unload (Stage 재개)
- **Stage → VN/Encounter**: 씬 변환 없음 (프리팹 오버레이)
- **Stage → Lobby**: Single 로드 (Stage 해제)

### 어셈블리 의존성
```
ProjectStS.Integration (NEW)
├── ProjectStS.Core
├── ProjectStS.Data
├── ProjectStS.Meta
├── ProjectStS.Battle
└── ProjectStS.Stage

default Assembly-CSharp (VisualNovelBridge)
├── ProjectStS.Core (IVisualNovelBridge 접근)
└── VisualNovel 기존 코드 (VisualNovelPlayer 접근)
```

### 주요 클래스 설계

#### GameFlowController (MonoBehaviour, DontDestroyOnLoad)
- ServiceLocator에 등록
- 씬 전환 조율: ExpeditionLauncher → StageSceneBootstrap → 이벤트 라우팅 → 결과 처리
- 이벤트 라우팅: EventType별 Battle(Additive)/VN(Overlay)/Encounter(Overlay) 분기
- BattleContext(파티, 웨이브, 이벤트ID) 보관 → BattleSceneBootstrap이 읽어감
- 자동 저장 트리거 관리

#### SaveLoadSystem
- Newtonsoft.Json 기반 JSON 직렬화
- 파일: Application.persistentDataPath + "/save_data.json"
- SaveData ← PlayerDataManager + CampaignManager 상태 추출
- 로드 시 PlayerDataManager.LoadData() + CampaignManager.Set*State() 호출
- 자동 저장: 정산 완료 후, 파티 편성 변경 후

#### IVisualNovelBridge / VisualNovelBridge
- Core에 인터페이스 정의, default assembly에서 구현
- PlayEpisode(episodeId, onComplete) 래핑
- ServiceLocator에 등록 → GameFlowController가 접근

#### RewardSettlementProcessor
- StageSettlementData 수신 → 플레이어 선택 처리 → PlayerDataManager 반영
- 이벤트 기반: OnSettlementReady, OnSettlementCompleted

#### Scene Bootstraps
- StageSceneBootstrap: [SerializeField] StageManager 참조 → GameFlowController.OnStageReady() 호출
- BattleSceneBootstrap: [SerializeField] BattleManager 참조 → GameFlowController.OnBattleReady() 호출

---

## 실행 단계

- [ ] Step 1: 기반 구조 — asmdef, Enum, SaveData 모델, IVisualNovelBridge 인터페이스
- [ ] Step 2: 기존 코드 수정 — GameSettings, SceneTransitionManager, PlayerDataManager, CampaignManager, VisualNovelPlayer
- [ ] Step 3: SaveLoadSystem 구현
- [ ] Step 4: VisualNovelBridge 구현
- [ ] Step 5: GameFlowController 구현
- [ ] Step 6: Scene Bootstraps + RewardSettlementProcessor 구현
- [ ] Step 7: GameBootstrapper 수정 (서비스 등록 + 세이브 로드 연결)
- [ ] Step 8: 현재 개발 상태 문서 업데이트

---

## 확인 사항

1. **Encounter 이벤트** → VN 오버레이 방식으로 처리 (사용자 확인 완료)
2. **전투 씬** → Additive 로딩 (사용자 확인 완료)
3. **세이브 슬롯** → 단일 슬롯 (추후 확장 가능한 구조)
4. VisualNovelPlayer에 OnEpisodeCompleted 이벤트와 외부 재생 메서드를 최소한으로 추가 (기존 구조 존중)
