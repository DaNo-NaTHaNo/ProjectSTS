# Phase 2.5 — 전투 UI 작업 내역

*작성일: 2026.03.05*

---

## 개요

Phase Common(공용 UI 위젯 8종)이 완료된 상태에서, 전투 씬의 UI 레이어를 구현하였다.
`BattleUIBridge`가 발행하는 13개 이벤트를 구독하여 전투 상태를 화면에 표시하고,
플레이어의 카드 사용·턴 종료·전투 포기 인터랙션을 처리하는 **9개 전투 UI 스크립트**와
**1개 DOTween 확장 유틸리티**를 작성하였다.

- **총 10개 파일, 2,245줄**
- **전투 UI 스크립트 9개**: `Assets/_Project/Scripts/Runtime/UI/Battle/`
- **DOTween 확장 유틸리티 1개**: `Assets/_Project/Scripts/Runtime/UI/Common/DOTweenUIExtensions.cs`

---

## 생성된 파일 목록

### 전투 UI 스크립트 (9개)

| 파일 | 줄 수 | 핵심 역할 |
| :---- | :---: | :---- |
| `BattleUIController.cs` | 573 | 전투 UI 중앙 컨트롤러. BattleUIBridge 13개 이벤트 구독 → 하위 컴포넌트 분배, 타겟 선택 시스템 관리 |
| `UIBattleUnitPanel.cs` | 433 | 유닛 상태 패널 (아군 3개 + 적 5개 공용). HP/방어도/상태이상/스킬/속성 표시, 대미지/힐 플로팅 텍스트, 사망 연출 |
| `UIHandArea.cs` | 370 | 손패 영역. UICard 오브젝트 풀(18~24개), 카드 추가/제거/사용 연출, 가로 재정렬, 드래그/클릭 인터랙션 |
| `UIEnemyIntent.cs` | 186 | 적 행동 의도 표시. AIDecision ActionType별 아이콘(공격/방어/강화/대기), 속성색, 말풍선 |
| `UIBattleResult.cs` | 183 | 전투 결과 화면. 승리/패배 + 종료 사유, DOTween Scale+Fade 등장 연출, 확인 버튼 |
| `UISkillNotify.cs` | 167 | 스킬 발동 알림 팝업. Scale+Fade 연출, 대기열 순차 재생 |
| `UIEnergyDisplay.cs` | 110 | 에너지 표시 (현재/기본). DOTween Scale 펀치 연출, 에너지 부족 시 색상 변경 |
| `UIBattleActions.cs` | 100 | 턴 종료/전투 포기 버튼. PlayerAction 페이즈에서만 턴 종료 활성, UIPopup 확인 후 포기 |
| `UIDeckCounter.cs` | 41 | 드로우 파일/디스카드 파일 카드 수 표시 |

### DOTween 확장 유틸리티 (1개)

| 파일 | 줄 수 | 핵심 역할 |
| :---- | :---: | :---- |
| `DOTweenUIExtensions.cs` | 82 | DOTweenModuleUI가 asmdef 어셈블리에서 접근 불가한 문제 해결. `DOTween.To()` 제너릭 API로 4개 확장 메서드 래핑 |

---

## 아키텍처

### 이벤트 → UI 매핑

```
BattleUIBridge Event                          → UI 컴포넌트
──────────────────────────────────────────────────────────────
OnPhaseChanged(BattlePhase)                   → BattleUIController (페이즈 전환)
                                                UIBattleActions (PlayerAction일 때만 활성)
OnCardAddedToHand(RuntimeCard)                → UIHandArea (카드 추가 + 드로우 연출)
OnCardRemovedFromHand(RuntimeCard)            → UIHandArea (카드 제거)
OnCardPlayed(BattleUnit, RuntimeCard)         → UIHandArea (카드 사용 연출)
OnUnitDamaged(BattleUnit, int)                → UIBattleUnitPanel (HP 갱신 + 대미지 텍스트)
OnUnitHealed(BattleUnit, int)                 → UIBattleUnitPanel (HP 갱신 + 힐 텍스트)
OnUnitBlockGained(BattleUnit, int)            → UIBattleUnitPanel (방어도 갱신)
OnUnitDefeated(BattleUnit)                    → UIBattleUnitPanel (사망 연출)
OnStatusEffectChanged(BattleUnit, ActiveStatusEffect) → UIBattleUnitPanel (상태아이콘 갱신)
OnEnergyChanged(int)                          → UIEnergyDisplay (에너지 텍스트 갱신)
OnEnemyIntentShown(BattleUnit, AIDecision)    → UIEnemyIntent (의도 아이콘/텍스트)
OnSkillActivated(BattleUnit, SkillData)       → UISkillNotify (알림 팝업 연출)
OnBattleResultShown(BattleResult, BattleEndReason) → UIBattleResult (결과 화면)
```

### 재사용한 공용 위젯

| 위젯 | 사용 위치 |
| :---- | :---- |
| `UICard` | UIHandArea에서 카드 표시 (SetData, 드래그/클릭 이벤트) |
| `UIHPBar` | UIBattleUnitPanel에서 HP/방어도 게이지 |
| `UIStatusIcon` | UIBattleUnitPanel에서 상태이상 아이콘 (풀링) |
| `UIElementBadge` | UIBattleUnitPanel 속성 배지, UIEnemyIntent 속성 표시 |
| `UIUnitPortrait` | UIBattleUnitPanel 초상화 |
| `UIPopup` | UIBattleActions 전투 포기 확인 팝업 |
| `UITooltip` | 카드 호버 시 툴팁 |

---

## DOTweenUIExtensions 상세

### 배경

`ProjectStS.UI`는 자체 asmdef 어셈블리로 컴파일된다. DOTween의 코어(`DOTween.dll`)는 프리컴파일 DLL이므로 자동 참조되지만, UI 확장 메서드가 정의된 `DOTweenModuleUI.cs`(소스 파일)는 `Plugins/` 폴더에 있어 `Assembly-CSharp-firstpass`로 컴파일된다. asmdef 어셈블리는 predefined 어셈블리를 직접 참조할 수 없으므로, `CanvasGroup.DOFade()`, `RectTransform.DOAnchorPos()` 등의 확장 메서드를 사용할 수 없다.

### 해결 방법

`ProjectStS.UI` 네임스페이스 내에 동일한 시그니처의 확장 메서드를 정의하고, 내부적으로 `DOTween.To()` 제너릭 API(프리컴파일 DLL)로 위임한다. 호출부 코드를 변경하지 않고 동일한 API를 제공.

### 제공 확장 메서드

| 메서드 | 대상 타입 | 설명 |
| :---- | :---- | :---- |
| `DOFade(float, float)` | `CanvasGroup` | alpha 트윈 |
| `DOFade(float, float)` | `Image` | color.a 트윈 |
| `DOAnchorPos(Vector2, float, bool)` | `RectTransform` | anchoredPosition 트윈 |
| `DOAnchorPosY(float, float, bool)` | `RectTransform` | anchoredPosition.y 트윈 |

> **참고**: Phase Common에서 이미 `UIHPBar.cs`의 `DOFillAmount`와 `UIPopup.cs`의 `DOFade(Image)`를 `DOTween.To()` 인라인 호출로 대체한 전례가 있다. 이번 Phase에서는 반복 사용이 11곳으로 늘어나 확장 메서드로 통합하였다.

---

## asmdef 변경 사항

| 변경 | 내용 |
| :---- | :---- |
| `ProjectStS.UI.asmdef` | DOTween.Modules 참조 추가 → 삭제 (asmdef 접근법 실패 후 제거) |

### DOTween asmdef 접근법 실패 기록

asmdef로 DOTween Modules를 별도 어셈블리로 분리하는 접근법을 2차에 걸쳐 시도하였으나 실패:

1. **1차 시도**: `overrideReferences: true` + 명시적 DLL 참조 → DOTweenPro Editor에서 DemiLib/DemiEditor 참조 불가 에러 추가 발생
2. **2차 시도**: `overrideReferences: false` → 동일 문제 지속, .meta 파일 부재로 Unity 인식 실패 추정
3. **최종 해결**: asmdef 파일 전부 삭제 + `DOTweenUIExtensions.cs` 래퍼 생성으로 문제 해결

---

## 후속 작업

- [ ] BattleScene에 Canvas + 각 UI 컴포넌트 프리팹 배치 (에디터 작업)
- [ ] BattleUIController에 하위 컴포넌트 SerializeField 할당
- [ ] UICard 프리팹 설정 (UIHandArea의 _cardPrefab 참조)
- [ ] UIBattleUnitPanel 프리팹: UIUnitPortrait, UIHPBar, UIStatusIcon, UIElementBadge 배치
- [ ] 기능 테스트: BattleManager.InitializeBattle() 호출 후 각 이벤트가 UI에 정상 반영되는지 수동 테스트

---

*작성자: Claude Code*
