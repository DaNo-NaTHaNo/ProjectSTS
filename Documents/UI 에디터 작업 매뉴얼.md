# UI 에디터 작업 매뉴얼

*작성일: 2026.03.07*

---

## 1. 개요

### 현재 상태

| Phase | 스크립트 | 에디터 작업 |
| :---- | :---: | :---: |
| Phase Common (공용 위젯 8종) | **완료** (1,909줄) | **미완료** |
| Phase 2.5 (전투 UI 9종 + DOTween 래퍼) | **완료** (2,245줄) | **미완료** |
| Phase 3.5 (스테이지 UI 7종) | **완료** (2,565줄) | **미완료** |
| Phase 4 UI (로비 UI 12종) | **완료** (3,795줄) | **미완료** |
| Phase 정산 (UISettlement) | **미구현** | **미완료** |

모든 UI 스크립트는 컴파일 완료 상태이며, Unity 에디터에서 **프리팹 생성 → Canvas 배치 → SerializeField 할당** 작업을 수행해야 화면에 표시된다.

### 작업 순서

```
Phase Common (공용 위젯 프리팹)
    ↓ 모든 씬에서 재사용하므로 반드시 선행
Phase 2.5 (전투 씬 UI 배치)
    ↓
Phase 3.5 (스테이지 씬 UI 배치)
    ↓
Phase 4 UI (로비 씬 UI 배치)
    ↓
Phase 정산 (UISettlement — 스크립트 구현 후)
```

### 사전 조건

아래 항목이 완료되어야 한다. 미완료 시 `Documents/Phase B — Unity 에디터 체크리스트 실행 가이드.md`를 먼저 수행한다.

- [ ] 컴파일 에러 0건 확인
- [ ] GameSettings + 19개 테이블 SO 생성 및 DataManager에 할당
- [ ] Build Settings에 4개 씬 등록 (BootScene / LobbyScene / StageScene / BattleScene)
- [ ] 각 씬에 Bootstrap 컴포넌트 배치 완료
- [ ] CSV 임포트 완료

---

## 2. Phase Common — 공용 위젯 프리팹 생성 (8종)

모든 공용 위젯 프리팹은 `Assets/_Project/Prefabs/UI/Common/` 폴더에 저장한다.
폴더가 없으면 생성한다.

> **공통 규칙**
> - 프리팹 생성: Hierarchy에서 오브젝트 구성 → Project 창으로 드래그하여 프리팹 생성
> - TextMeshPro 텍스트는 모두 `TextMeshPro - Text (UI)` 컴포넌트 사용
> - Image는 `UI → Image` 컴포넌트
> - 아트 에셋 미완성이므로 Placeholder 색상/텍스트 기반으로 구성

---

### 2-1. UIElementBadge 프리팹

**가장 먼저 생성** — UICard, UIUnitPortrait, UIStatusIcon 등에서 서브 컴포넌트로 재사용.

**계층 구조:**
```
UIElementBadge (Image + UIElementBadge.cs)
├── Label (TextMeshProUGUI)
```

**SerializeField 할당:**

| 인스펙터 필드 | 할당 대상 |
| :---- | :---- |
| `_background` | 자기 자신(UIElementBadge)의 Image 컴포넌트 |
| `_label` | 자식 `Label`의 TextMeshProUGUI |

**설정 포인트:**
- Image 크기: 약 60×28 px
- Label: 폰트 크기 12~14, 중앙 정렬, 흰색
- 배경 색상은 스크립트에서 `ElementType`에 따라 자동 변경됨

---

### 2-2. UIHPBar 프리팹

**계층 구조:**
```
UIHPBar (UIHPBar.cs)
├── Background (Image — 어두운 회색 바 배경)
│   ├── DamageFill (Image — 빨간색, Fill 타입)
│   └── Fill (Image — 녹색, Fill 타입)
├── HPText (TextMeshProUGUI — "75/100")
└── BlockRoot (GameObject)
    └── BlockText (TextMeshProUGUI — "🛡 12")
```

**SerializeField 할당:**

| 인스펙터 필드 | 할당 대상 |
| :---- | :---- |
| `_fillImage` | `Fill` Image (Image Type: **Filled**, Fill Method: Horizontal, Fill Origin: Left) |
| `_damageFillImage` | `DamageFill` Image (동일 설정, 빨간색) |
| `_hpText` | `HPText` TextMeshProUGUI |
| `_blockText` | `BlockRoot/BlockText` TextMeshProUGUI |
| `_blockRoot` | `BlockRoot` GameObject |
| `_tweenDuration` | `0.4` (기본값) |
| `_damageTrailDelay` | `0.2` (기본값) |
| `_damageTrailDurationMultiplier` | `1.5` (기본값) |

**설정 포인트:**
- Fill과 DamageFill의 Image Type을 **Filled**로 설정 (이 설정이 없으면 fillAmount가 동작하지 않음)
- Fill Method: **Horizontal**, Fill Origin: **Left**
- DamageFill은 Fill 뒤에 배치 (Sibling 순서: DamageFill이 Fill보다 위)
- BlockRoot는 방어도 0일 때 비활성화됨

---

### 2-3. UICard 프리팹

**계층 구조:**
```
UICard (Image + CanvasGroup + UICard.cs)
├── CostText (TextMeshProUGUI — 좌상단)
├── ElementBadge (UIElementBadge 프리팹 인스턴스 — 우상단)
├── ArtworkImage (Image — 중앙 영역)
├── NameText (TextMeshProUGUI)
├── DescriptionText (TextMeshProUGUI)
└── RarityIndicator (Image — 하단 바)
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Card Info** | `_costText` | `CostText` TextMeshProUGUI |
| | `_elementBadge` | `ElementBadge` UIElementBadge 컴포넌트 |
| | `_artworkImage` | `ArtworkImage` Image |
| | `_nameText` | `NameText` TextMeshProUGUI |
| | `_descriptionText` | `DescriptionText` TextMeshProUGUI |
| | `_rarityIndicator` | `RarityIndicator` Image |
| **Card Frame** | `_cardBackground` | 자기 자신(UICard)의 Image |
| | `_canvasGroup` | 자기 자신(UICard)의 CanvasGroup |
| **Interaction** | `_hoverScale` | `1.1` (기본값) |
| | `_hoverOffsetY` | `30` (기본값) |
| | `_hoverDuration` | `0.15` (기본값) |
| | `_dragAlpha` | `0.7` (기본값) |

**설정 포인트:**
- 카드 크기: 약 140×200 px (용도에 따라 조절 가능)
- Raycast Target: UICard의 Image에서 **활성화** (드래그/클릭 감지)
- CanvasGroup 필수 추가 (드래그 시 alpha 변경용)
- `ElementBadge`는 2-1에서 만든 UIElementBadge 프리팹을 Nested Prefab으로 배치

---

### 2-4. UIUnitPortrait 프리팹

**계층 구조:**
```
UIUnitPortrait (UIUnitPortrait.cs)
├── FrameBorder (Image — 테두리)
│   └── PortraitImage (Image — 초상화 / Placeholder: 속성 색상 원형)
├── PlaceholderName (TextMeshProUGUI — 초상화 위 이름)
├── NameText (TextMeshProUGUI — 하단 이름)
└── ElementBadge (UIElementBadge 프리팹 인스턴스)
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Portrait** | `_portraitImage` | `PortraitImage` Image |
| | `_placeholderNameText` | `PlaceholderName` TextMeshProUGUI |
| **Info** | `_nameText` | `NameText` TextMeshProUGUI |
| | `_elementBadge` | `ElementBadge` UIElementBadge 컴포넌트 |
| **Frame** | `_frameBorder` | `FrameBorder` Image |
| | `_highlightColor` | `#FFD54F` (금색 — 기본값) |
| | `_normalColor` | `#424242` (회색 — 기본값) |

**설정 포인트:**
- 크기: 약 90×120 px
- PortraitImage는 원형(Circle) 마스크 적용 권장 (`Mask` 컴포넌트 또는 원형 스프라이트)
- FrameBorder의 색상은 스크립트에서 하이라이트/일반 전환

---

### 2-5. UIItemIcon 프리팹

**계층 구조:**
```
UIItemIcon (UIItemIcon.cs)
├── RarityBorder (Image — 레어도 색상 테두리)
│   └── IconImage (Image — 아이콘 / Placeholder: 색상 블록)
├── NameAbbreviation (TextMeshProUGUI — 2글자 약칭)
└── QuantityRoot (GameObject)
    └── QuantityText (TextMeshProUGUI — "×3")
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Icon** | `_iconImage` | `IconImage` Image |
| | `_rarityBorder` | `RarityBorder` Image |
| | `_nameAbbreviation` | `NameAbbreviation` TextMeshProUGUI |
| **Quantity** | `_quantityText` | `QuantityRoot/QuantityText` TextMeshProUGUI |
| | `_quantityRoot` | `QuantityRoot` GameObject |

**설정 포인트:**
- 크기: 약 64×64 px
- 수량이 1 이하면 QuantityRoot가 비활성화됨

---

### 2-6. UIStatusIcon 프리팹

**계층 구조:**
```
UIStatusIcon (UIStatusIcon.cs)
├── Background (Image — 버프: 파랑 / 디버프: 빨강)
│   └── IconImage (Image — 상태이상 아이콘)
├── StackRoot (GameObject)
│   └── StackText (TextMeshProUGUI — "×3")
└── DurationRoot (GameObject)
    └── DurationText (TextMeshProUGUI — "2T")
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Icon** | `_iconImage` | `IconImage` Image |
| | `_background` | `Background` Image |
| **Stack** | `_stackText` | `StackRoot/StackText` TextMeshProUGUI |
| | `_stackRoot` | `StackRoot` GameObject |
| **Duration** | `_durationText` | `DurationRoot/DurationText` TextMeshProUGUI |
| | `_durationRoot` | `DurationRoot` GameObject |
| **Colors** | `_buffColor` | `#42A5F5` (파랑 — 기본값) |
| | `_debuffColor` | `#EF5350` (빨강 — 기본값) |

**설정 포인트:**
- 크기: 약 36×36 px (전투 유닛 패널 내 배치)
- 오브젝트 풀링 대상이므로, 프리팹으로 만들어 UIBattleUnitPanel의 `_statusIconPrefab`에 할당

---

### 2-7. UITooltip 프리팹

**계층 구조:**
```
UITooltip (Canvas [Overlay, Sort Order 999] + UITooltip.cs)
└── TooltipPanel (RectTransform + CanvasGroup + Image[배경])
    ├── TitleText (TextMeshProUGUI — 볼드)
    └── BodyText (TextMeshProUGUI)
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **References** | `_tooltipPanel` | `TooltipPanel` RectTransform |
| | `_titleText` | `TitleText` TextMeshProUGUI |
| | `_bodyText` | `BodyText` TextMeshProUGUI |
| | `_canvasGroup` | `TooltipPanel`의 CanvasGroup |
| **Settings** | `_offset` | `(16, -16)` (기본값) |

**설정 포인트:**
- **별도 Canvas (Overlay)** 에 배치 — 다른 모든 UI 위에 표시되어야 함
- Sort Order: 999 (최상위)
- 시작 시 비활성 상태 (`_canvasGroup.alpha = 0`)
- **씬당 1개** — static `Show()`/`Hide()` 메서드로 접근

**배치 방법:**
1. 각 씬(BattleScene, StageScene, LobbyScene)에 `UITooltip` 프리팹 인스턴스를 **최상위에 독립적으로** 배치
2. 또는 DontDestroyOnLoad 오브젝트에 1개만 배치하여 전 씬에서 공유

---

### 2-8. UIPopup 프리팹

**계층 구조:**
```
UIPopup (UIPopup.cs)
├── PopupRoot (GameObject — 전체 온/오프)
│   ├── DimBackground (Image — 반투명 검정, Raycast Block)
│   └── Panel (RectTransform)
│       ├── MessageText (TextMeshProUGUI)
│       ├── ConfirmButtonRoot (GameObject)
│       │   └── ConfirmButton (Button + TextMeshProUGUI)
│       └── CancelButtonRoot (GameObject)
│           └── CancelButton (Button + TextMeshProUGUI)
```

**SerializeField 할당:**

| Header | 인스펙터 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Root** | `_popupRoot` | `PopupRoot` GameObject |
| | `_dimBackground` | `DimBackground` Image |
| **Panel** | `_panelTransform` | `Panel` RectTransform |
| | `_messageText` | `MessageText` TextMeshProUGUI |
| **Buttons** | `_confirmButtonRoot` | `ConfirmButtonRoot` GameObject |
| | `_cancelButtonRoot` | `CancelButtonRoot` GameObject |
| | `_confirmButtonText` | `ConfirmButton` 내부의 TextMeshProUGUI |
| | `_cancelButtonText` | `CancelButton` 내부의 TextMeshProUGUI |
| **Settings** | `_animDuration` | `0.25` (기본값) |
| | `_defaultConfirmText` | `확인` |
| | `_defaultCancelText` | `취소` |

**설정 포인트:**
- DimBackground: 색상 `(0, 0, 0, 0.5)`, Stretch(전체 화면), Raycast Target **활성** (뒷 UI 클릭 차단)
- Panel: 중앙 정렬, 크기 약 400×200 px
- PopupRoot는 시작 시 **비활성** (SetActive(false))
- **씬당 1개** — static `ShowConfirm()`/`ShowInfo()` 메서드로 접근
- UITooltip과 같은 방식으로 각 씬 또는 DontDestroyOnLoad에 배치

---

## 3. Phase 2.5 — 전투 씬 UI 배치

### 3-0. 사전 작업

BattleScene에 아래 오브젝트가 없으면 먼저 생성한다:

1. **Canvas** — `GameObject → UI → Canvas` (Screen Space - Overlay)
2. **EventSystem** — Canvas 생성 시 자동 생성됨. 없으면 `GameObject → UI → EventSystem`

### 3-1. 전투 씬 Canvas 계층 구조

```
BattleScene
├── BattleManager (기존 — Phase B에서 배치)
├── BattleSceneBootstrap (기존 — Phase B에서 배치)
│
├── Canvas (Screen Space - Overlay)
│   └── BattleUIRoot (BattleUIController.cs)
│       │
│       ├── AllyPanelArea (HorizontalLayoutGroup)
│       │   ├── AllyPanel_0 (UIBattleUnitPanel.cs) ← 아군 슬롯 1
│       │   ├── AllyPanel_1 (UIBattleUnitPanel.cs) ← 아군 슬롯 2
│       │   └── AllyPanel_2 (UIBattleUnitPanel.cs) ← 아군 슬롯 3
│       │
│       ├── EnemyPanelArea (HorizontalLayoutGroup)
│       │   ├── EnemyPanel_0 (UIBattleUnitPanel.cs) ← 적 슬롯 1
│       │   ├── EnemyPanel_1 (UIBattleUnitPanel.cs) ← 적 슬롯 2
│       │   ├── EnemyPanel_2 (UIBattleUnitPanel.cs) ← 적 슬롯 3
│       │   ├── EnemyPanel_3 (UIBattleUnitPanel.cs) ← 적 슬롯 4
│       │   └── EnemyPanel_4 (UIBattleUnitPanel.cs) ← 적 슬롯 5
│       │
│       ├── HandArea (UIHandArea.cs — 화면 하단)
│       │   └── CardContainer (Transform — 카드 배치 영역)
│       │
│       ├── EnergyDisplay (UIEnergyDisplay.cs — 좌하단)
│       │
│       ├── DeckCounter (UIDeckCounter.cs — 우하단)
│       │
│       ├── SkillNotify (UISkillNotify.cs — 화면 중앙 상단)
│       │
│       ├── BattleActions (UIBattleActions.cs — 우측)
│       │   ├── EndTurnButton (Button)
│       │   └── SurrenderButton (Button)
│       │
│       └── BattleResult (UIBattleResult.cs — 전체 화면 오버레이)
│           ├── DimBackground (Image)
│           └── ResultPanel (...)
│
├── UITooltip (별도 Canvas Overlay, Sort Order 999)
├── UIPopup (Canvas 내 최상위 또는 별도)
└── EventSystem
```

### 3-2. BattleUIController SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Battle Manager** | `_battleManagerRef` | 씬 내 `BattleManager` 컴포넌트 |
| **Unit Panels** | `_allyPanels` (Size: 3) | `AllyPanel_0`, `AllyPanel_1`, `AllyPanel_2` |
| | `_enemyPanels` (Size: 5) | `EnemyPanel_0` ~ `EnemyPanel_4` |
| **Sub Components** | `_handArea` | `HandArea` UIHandArea 컴포넌트 |
| | `_energyDisplay` | `EnergyDisplay` UIEnergyDisplay 컴포넌트 |
| | `_deckCounter` | `DeckCounter` UIDeckCounter 컴포넌트 |
| | `_skillNotify` | `SkillNotify` UISkillNotify 컴포넌트 |
| | `_battleActions` | `BattleActions` UIBattleActions 컴포넌트 |
| | `_battleResult` | `BattleResult` UIBattleResult 컴포넌트 |

### 3-3. UIBattleUnitPanel SerializeField 할당 (×8개 패널 공통)

각 `AllyPanel_N`, `EnemyPanel_N`에 동일하게 적용:

**패널 내부 계층 구조:**
```
UIBattleUnitPanel (UIBattleUnitPanel.cs + CanvasGroup)
├── Portrait (UIUnitPortrait 프리팹 인스턴스)
├── HPBar (UIHPBar 프리팹 인스턴스)
├── StatusIconContainer (Transform — HorizontalLayoutGroup)
├── SkillRoot (GameObject)
│   ├── SkillIcon (Image)
│   ├── SkillElementBG (Image)
│   └── SkillCooldownText (TextMeshProUGUI)
├── EnemyIntent (UIEnemyIntent — 적 패널에서만 사용)
├── TargetHighlight (GameObject — 선택 시 하이라이트 테두리)
└── FloatingTextAnchor (RectTransform — 대미지/힐 텍스트 기준점)
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Portrait** | `_portrait` | `Portrait` UIUnitPortrait 컴포넌트 |
| **HP & Block** | `_hpBar` | `HPBar` UIHPBar 컴포넌트 |
| **Status Effects** | `_statusIconContainer` | `StatusIconContainer` Transform |
| | `_statusIconPrefab` | Phase Common에서 만든 `UIStatusIcon` **프리팹** 드래그 |
| **Skill** | `_skillRoot` | `SkillRoot` GameObject |
| | `_skillIcon` | `SkillRoot/SkillIcon` Image |
| | `_skillElementBackground` | `SkillRoot/SkillElementBG` Image |
| | `_skillCooldownText` | `SkillRoot/SkillCooldownText` TextMeshProUGUI |
| **Enemy Intent** | `_enemyIntent` | `EnemyIntent` UIEnemyIntent 컴포넌트 (적 패널만) |
| **Floating Text** | `_floatingTextAnchor` | `FloatingTextAnchor` RectTransform |
| **Target Selection** | `_targetHighlight` | `TargetHighlight` GameObject |
| | `_canvasGroup` | 자기 자신의 CanvasGroup |
| **Settings** | `_floatDistance` | `80` (기본값) |
| | `_floatDuration` | `0.8` (기본값) |
| **Colors** | `_damageTextColor` | `RGBA(229, 57, 53, 255)` 빨강 |
| | `_healTextColor` | `RGBA(67, 165, 71, 255)` 녹색 |

> **주의**: `_statusIconPrefab`에는 씬 내 인스턴스가 아닌 **Project 창의 프리팹 에셋**을 드래그해야 한다. 런타임에 Instantiate로 풀링 생성됨.

### 3-4. UIHandArea SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **References** | `_cardContainer` | `HandArea/CardContainer` Transform |
| | `_cardPrefab` | Phase Common에서 만든 `UICard` **프리팹** 드래그 |
| **Layout** | `_cardSpacing` | `120` (기본값) |
| | `_rearrangeDuration` | `0.2` (기본값) |
| **Draw Animation** | `_drawStartPosition` | `(-500, -200, 0)` (기본값) |
| | `_drawDuration` | `0.3` (기본값) |
| **Play Animation** | `_playTargetOffset` | `(0, 300, 0)` (기본값) |
| | `_playDuration` | `0.25` (기본값) |
| **Pool** | `_initialPoolSize` | `18` (기본값) |

### 3-5. UIEnemyIntent SerializeField 할당

UIBattleUnitPanel 내부에 자식으로 배치하거나, 별도 오브젝트로 구성:

**내부 계층:**
```
UIEnemyIntent (UIEnemyIntent.cs)
├── IntentBackground (Image — 원형/둥근 사각)
│   └── IntentIcon (Image)
├── IntentText (TextMeshProUGUI)
└── SpeechBubbleRoot (GameObject)
    └── SpeechText (TextMeshProUGUI)
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Intent Display** | `_intentIcon` | `IntentIcon` Image |
| | `_intentBackground` | `IntentBackground` Image |
| | `_intentText` | `IntentText` TextMeshProUGUI |
| **Speech Bubble** | `_speechBubbleRoot` | `SpeechBubbleRoot` GameObject |
| | `_speechText` | `SpeechText` TextMeshProUGUI |
| **Intent Colors** | `_attackColor` | `RGBA(229, 57, 53, 255)` 빨강 |
| | `_defendColor` | `RGBA(30, 136, 229, 255)` 파랑 |
| | `_statusColor` | `RGBA(142, 36, 170, 255)` 보라 |
| | `_buffColor` | `RGBA(255, 193, 7, 255)` 금색 |
| | `_passColor` | `RGBA(158, 158, 158, 255)` 회색 |

### 3-6. UIEnergyDisplay SerializeField 할당

**내부 계층:**
```
UIEnergyDisplay (UIEnergyDisplay.cs)
├── PunchTarget (RectTransform — 에너지 표시 영역)
│   └── EnergyText (TextMeshProUGUI — "3/3")
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **References** | `_energyText` | `EnergyText` TextMeshProUGUI |
| | `_punchTarget` | `PunchTarget` RectTransform |
| **Settings** | `_punchScale` | `0.2` (기본값) |
| | `_punchDuration` | `0.3` (기본값) |
| **Colors** | `_normalColor` | `(255, 255, 255, 255)` 흰색 |
| | `_lowColor` | `RGBA(230, 77, 77, 255)` 빨강 |

### 3-7. UIDeckCounter SerializeField 할당

**내부 계층:**
```
UIDeckCounter (UIDeckCounter.cs)
├── DrawPileText (TextMeshProUGUI — "덱: 12")
└── DiscardPileText (TextMeshProUGUI — "묘지: 3")
```

| 필드 | 할당 대상 |
| :---- | :---- |
| `_drawPileText` | `DrawPileText` TextMeshProUGUI |
| `_discardPileText` | `DiscardPileText` TextMeshProUGUI |

### 3-8. UISkillNotify SerializeField 할당

**내부 계층:**
```
UISkillNotify (UISkillNotify.cs)
└── NotifyRoot (CanvasGroup)
    └── Panel (RectTransform — 중앙 배너)
        ├── UnitNameText (TextMeshProUGUI)
        ├── SkillNameText (TextMeshProUGUI)
        └── ElementBadge (UIElementBadge 프리팹 인스턴스)
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **References** | `_notifyRoot` | `NotifyRoot` CanvasGroup |
| | `_panelTransform` | `Panel` RectTransform |
| | `_unitNameText` | `UnitNameText` TextMeshProUGUI |
| | `_skillNameText` | `SkillNameText` TextMeshProUGUI |
| | `_elementBadge` | `ElementBadge` UIElementBadge 컴포넌트 |
| **Settings** | `_displayDuration` | `1.5` (기본값) |
| | `_fadeInDuration` | `0.25` (기본값) |
| | `_fadeOutDuration` | `0.3` (기본값) |
| | `_overshootScale` | `1.2` (기본값) |

### 3-9. UIBattleActions SerializeField 할당

**내부 계층:**
```
UIBattleActions (UIBattleActions.cs)
├── EndTurnButton (Button + TextMeshProUGUI "턴 종료")
└── SurrenderButton (Button + TextMeshProUGUI "포기")
```

| 필드 | 할당 대상 |
| :---- | :---- |
| `_endTurnButton` | `EndTurnButton` Button 컴포넌트 |
| `_surrenderButton` | `SurrenderButton` Button 컴포넌트 |

> Button의 OnClick은 인스펙터에서 설정하지 않는다. 스크립트에서 자동 바인딩됨.

### 3-10. UIBattleResult SerializeField 할당

**내부 계층:**
```
UIBattleResult (UIBattleResult.cs)
└── ResultRoot (GameObject)
    ├── DimBackground (Image — 전체 화면, 반투명 검정)
    └── Panel (RectTransform + CanvasGroup)
        ├── ResultTitleText (TextMeshProUGUI — "승리!" / "패배")
        ├── ReasonText (TextMeshProUGUI — 사유)
        └── ConfirmButton (Button)
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **References** | `_resultRoot` | `ResultRoot` GameObject |
| | `_canvasGroup` | `Panel`의 CanvasGroup |
| | `_panelTransform` | `Panel` RectTransform |
| | `_resultTitleText` | `ResultTitleText` TextMeshProUGUI |
| | `_reasonText` | `ReasonText` TextMeshProUGUI |
| | `_confirmButton` | `ConfirmButton` Button |
| | `_dimBackground` | `DimBackground` Image |
| **Settings** | `_animDuration` | `0.35` (기본값) |
| **Colors** | `_victoryColor` | `RGBA(255, 215, 30, 255)` 금색 |
| | `_defeatColor` | `RGBA(229, 57, 53, 255)` 빨강 |

---

## 4. Phase 3.5 — 스테이지 씬 UI 배치

### 4-0. 사전 작업

StageScene에 Canvas와 EventSystem이 없으면 생성한다.

### 4-1. UIHexTile 프리팹 생성 (풀링 대상)

**먼저 프리팹을 생성**해야 UIWorldMap에서 참조할 수 있다.

**저장 경로:** `Assets/_Project/Prefabs/UI/Stage/UIHexTile.prefab`

**계층 구조:**
```
UIHexTile (Image[육각형] + CanvasGroup + UIHexTile.cs)
├── EventIcon (Image — 이벤트 타입 아이콘)
├── EventIconLabel (TextMeshProUGUI — 아이콘 대체 텍스트 "검", "★", "?" 등)
└── HighlightBorder (Image — 현재 위치 하이라이트 테두리)
```

**SerializeField 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Visual Elements** | `_background` | 자기 자신(UIHexTile)의 Image |
| | `_eventIcon` | `EventIcon` Image |
| | `_eventIconLabel` | `EventIconLabel` TextMeshProUGUI |
| | `_highlightBorder` | `HighlightBorder` Image |
| | `_canvasGroup` | 자기 자신의 CanvasGroup |
| **Settings** | `_revealAnimDuration` | `0.3` (기본값) |

**설정 포인트:**
- 육각형 모양: 육각형 스프라이트를 Image의 Source Image로 설정하거나, 일반 사각 스프라이트로 대체 가능
- 크기: `_hexSize` 설정에 따라 스크립트에서 동적 조절 (기본 약 40px)
- CanvasGroup 필수 (공개 애니메이션용)
- HighlightBorder: 시작 시 비활성
- **프리팹으로 저장 후 씬에서 제거** — UIWorldMap이 런타임에 Instantiate

### 4-2. 스테이지 씬 Canvas 계층 구조

```
StageScene
├── StageManager (기존)
├── StageSceneBootstrap (기존)
│
├── Canvas (Screen Space - Overlay)
│   └── StageUIRoot (StageUIController.cs)
│       │
│       ├── WorldMap (UIWorldMap.cs)
│       │   └── ScrollRect (ScrollRect 컴포넌트)
│       │       └── Viewport (Mask)
│       │           └── MapContainer (RectTransform — UIHexTile들이 배치될 영역)
│       │
│       ├── StageHUD (UIStageHUD.cs — 상단 HUD)
│       │   ├── APArea
│       │   │   ├── APFillBar (Image — Filled)
│       │   │   └── APText (TextMeshProUGUI — "AP: 8/12")
│       │   ├── ZoneInfo (CanvasGroup)
│       │   │   ├── ZoneNameText (TextMeshProUGUI)
│       │   │   └── ZoneDescText (TextMeshProUGUI)
│       │   └── EventCountText (TextMeshProUGUI)
│       │
│       ├── BagPanel (UIBagPanel.cs — 우측 슬라이드 패널)
│       │   ├── ToggleButton (Button + TextMeshProUGUI)
│       │   └── PanelRoot (RectTransform + CanvasGroup)
│       │       └── ItemContainer (RectTransform — UIItemIcon 풀링 배치)
│       │
│       ├── EventPopup (UIEventPopup.cs — 중앙 팝업)
│       │   ├── DimBackground (Image)
│       │   └── Panel (RectTransform)
│       │       ├── EventTypeBanner (Image)
│       │       ├── EventTypeLabel (TextMeshProUGUI)
│       │       ├── EventNameText (TextMeshProUGUI)
│       │       ├── ConfirmButton (Button + TextMeshProUGUI)
│       │       └── CancelButton (Button + TextMeshProUGUI)
│       │
│       └── StageResult (UIStageResult.cs — 전체 화면 오버레이)
│           ├── DimBackground (Image)
│           └── ResultPanel (RectTransform)
│               ├── ResultTitle (TextMeshProUGUI)
│               ├── ResultReasonText (TextMeshProUGUI)
│               ├── ResultBanner (Image)
│               ├── FixedRewardArea (보상 선택 영역)
│               ├── CompletionRewardArea (완료 보상 영역)
│               ├── BonusRewardArea (추가 보상 영역)
│               └── ConfirmButton (Button + TextMeshProUGUI)
│
├── UITooltip (별도 Canvas Overlay, Sort Order 999)
├── UIPopup
└── EventSystem
```

### 4-3. StageUIController SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Stage Manager** | `_stageManagerRef` | 씬 내 `StageManager` 컴포넌트 |
| **Sub Components** | `_worldMap` | `WorldMap` UIWorldMap 컴포넌트 |
| | `_stageHUD` | `StageHUD` UIStageHUD 컴포넌트 |
| | `_bagPanel` | `BagPanel` UIBagPanel 컴포넌트 |
| | `_eventPopup` | `EventPopup` UIEventPopup 컴포넌트 |
| | `_stageResult` | `StageResult` UIStageResult 컴포넌트 |

### 4-4. UIWorldMap SerializeField 할당

**핵심: ScrollRect 구성**

1. `WorldMap` 오브젝트에 `ScrollRect` 컴포넌트 추가
2. `Viewport` 자식 생성 → `Mask` 컴포넌트 + `Image` (불투명) 추가
3. `MapContainer` 자식 생성 → ScrollRect의 `Content`에 `MapContainer` 할당
4. ScrollRect의 `Viewport`에 `Viewport` 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Hex Tile Pool** | `_tilePrefab` | `UIHexTile` **프리팹** (4-1에서 생성) |
| | `_mapContainer` | `MapContainer` RectTransform |
| | `_poolInitialSize` | `300` (기본값) |
| **Hex Layout** | `_hexSize` | `40` (기본값) |
| | `_hexSpacing` | `2` (기본값) |
| **Camera Control** | `_scrollRect` | 자기 자신의 ScrollRect 컴포넌트 |
| | `_minZoom` | `0.3` (기본값) |
| | `_maxZoom` | `2.0` (기본값) |
| | `_zoomSpeed` | `0.1` (기본값) |
| | `_panToDuration` | `0.5` (기본값) |
| **Viewport** | `_viewportPadding` | `100` (기본값) |

> **중요**: MapContainer의 크기를 충분히 크게 설정해야 한다 (예: 5000×5000 px). 2,437개 노드의 좌표 범위를 수용할 수 있어야 함.

### 4-5. UIStageHUD SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **AP Display** | `_apText` | `APText` TextMeshProUGUI |
| | `_apFillBar` | `APFillBar` Image (**Filled** 타입 설정 필수) |
| | `_apPunchScale` | `1.2` (기본값) |
| | `_apAnimDuration` | `0.3` (기본값) |
| **Zone Info** | `_zoneNameText` | `ZoneNameText` TextMeshProUGUI |
| | `_zoneDescText` | `ZoneDescText` TextMeshProUGUI |
| | `_zoneInfoGroup` | `ZoneInfo` CanvasGroup |
| | `_zoneFadeDuration` | `0.5` (기본값) |
| **Stage Info** | `_eventCountText` | `EventCountText` TextMeshProUGUI |

### 4-6. UIBagPanel SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Panel** | `_panelRoot` | `PanelRoot` RectTransform |
| | `_panelCanvasGroup` | `PanelRoot`의 CanvasGroup |
| | `_toggleButton` | `ToggleButton` Button |
| | `_toggleButtonText` | `ToggleButton` 내부 TextMeshProUGUI |
| **Item List** | `_itemContainer` | `ItemContainer` RectTransform |
| | `_itemIconPrefab` | `UIItemIcon` **프리팹** (Phase Common에서 생성) |
| | `_poolSize` | `30` (기본값) |
| **Settings** | `_animDuration` | `0.3` (기본값) |
| | `_closedPosition` | `(300, 0)` (기본값) |
| | `_openPosition` | `(0, 0)` (기본값) |

### 4-7. UIEventPopup SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Popup** | `_popupRoot` | `EventPopup` 루트 GameObject |
| | `_dimBackground` | `DimBackground` Image |
| | `_panelTransform` | `Panel` RectTransform |
| **Content** | `_eventTypeLabel` | `EventTypeLabel` TextMeshProUGUI |
| | `_eventNameText` | `EventNameText` TextMeshProUGUI |
| | `_eventTypeBanner` | `EventTypeBanner` Image |
| **Buttons** | `_confirmButton` | `ConfirmButton` Button |
| | `_cancelButton` | `CancelButton` Button |
| | `_confirmText` | `ConfirmButton` 내부 TextMeshProUGUI |
| | `_cancelText` | `CancelButton` 내부 TextMeshProUGUI |
| **Settings** | `_animDuration` | `0.25` (기본값) |
| | `_defaultConfirmText` | `진입` |
| | `_defaultCancelText` | `취소` |

### 4-8. UIStageResult SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Result Panel** | `_resultRoot` | 루트 GameObject |
| | `_dimBackground` | `DimBackground` Image |
| | `_panelTransform` | `ResultPanel` RectTransform |
| **Result Info** | `_resultTitle` | `ResultTitle` TextMeshProUGUI |
| | `_resultReasonText` | `ResultReasonText` TextMeshProUGUI |
| | `_resultBanner` | `ResultBanner` Image |
| **Fixed Rewards** | `_fixedRewardArea` | `FixedRewardArea` GameObject |
| | `_fixedRewardLabel` | 고정 보상 제목 TextMeshProUGUI |
| | `_fixedRewardContainer` | 고정 보상 아이콘 배치 RectTransform (GridLayoutGroup 추가 권장) |
| **Completion Rewards** | `_completionRewardArea` | `CompletionRewardArea` GameObject |
| | `_completionRewardLabel` | 완료 보상 제목 TextMeshProUGUI |
| | `_completionRewardContainer` | 완료 보상 아이콘 배치 RectTransform |
| **Bonus Rewards** | `_bonusRewardArea` | `BonusRewardArea` GameObject |
| | `_bonusRewardLabel` | 추가 보상 제목 TextMeshProUGUI |
| | `_bonusRewardContainer` | 추가 보상 아이콘 배치 RectTransform |
| **Confirm** | `_confirmButton` | `ConfirmButton` Button |
| | `_confirmButtonText` | `ConfirmButton` 내부 TextMeshProUGUI |
| **Prefab** | `_rewardIconPrefab` | `UIItemIcon` **프리팹** (Phase Common에서 생성) |
| **Settings** | `_animDuration` | `0.5` (기본값) |
| | `_iconPoolSize` | `20` (기본값) |

---

## 5. Phase 4 UI — 로비 씬 UI 배치

### 5-0. 사전 작업

LobbyScene에 Canvas와 EventSystem이 없으면 생성한다.

### 5-1. 로비 씬 Canvas 계층 구조

```
LobbyScene
├── PlayerDataManager (기존 — Phase B에서 배치)
│
├── Canvas (Screen Space - Overlay)
│   └── LobbyUIRoot (LobbyUIController.cs)
│       │
│       ├── MainScreen (_mainScreenRoot)
│       │   ├── PartyPreview (HorizontalLayoutGroup)
│       │   │   ├── PreviewPortrait_0 (UIUnitPortrait)
│       │   │   ├── PreviewPortrait_1 (UIUnitPortrait)
│       │   │   └── PreviewPortrait_2 (UIUnitPortrait)
│       │   ├── NavigationButtons
│       │   │   ├── PartyEditButton (Button — "파티 편성")
│       │   │   ├── InventoryButton (Button — "인벤토리")
│       │   │   └── CampaignButton (Button — "캠페인")
│       │   ├── ExpeditionButton (Button — "탐험 개시")
│       │   └── CampaignTracker (UICampaignTracker.cs — HUD 오버레이)
│       │
│       ├── PartyEditScreen (_partyEditController)
│       │   └── (PartyEditUIController 하위 — 5-3 참조)
│       │
│       ├── InventoryScreen (_inventoryController)
│       │   └── (InventoryUIController 하위 — 5-4 참조)
│       │
│       └── CampaignScreen (_campaignController)
│           └── (CampaignUIController 하위 — 5-5 참조)
│
├── UITooltip (별도 Canvas Overlay, Sort Order 999)
├── UIPopup
└── EventSystem
```

### 5-2. LobbyUIController SerializeField 할당

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Sub Controllers** | `_partyEditController` | `PartyEditScreen` PartyEditUIController 컴포넌트 |
| | `_inventoryController` | `InventoryScreen` InventoryUIController 컴포넌트 |
| | `_campaignController` | `CampaignScreen` CampaignUIController 컴포넌트 |
| **Main Screen** | `_mainScreenRoot` | `MainScreen` GameObject |
| **Party Preview** | `_partyPreviewPortraits` (Size: 3) | `PreviewPortrait_0`, `_1`, `_2` 각각의 UIUnitPortrait |
| **Campaign Tracker** | `_campaignTracker` | `CampaignTracker` UICampaignTracker 컴포넌트 |
| **Navigation Buttons** | `_partyEditButton` | `PartyEditButton` Button |
| | `_inventoryButton` | `InventoryButton` Button |
| | `_campaignButton` | `CampaignButton` Button |
| **Expedition** | `_expeditionButton` | `ExpeditionButton` Button |
| | `_expeditionButtonText` | `ExpeditionButton` 내부 TextMeshProUGUI |
| **Popup** | `_popup` | 씬 내 `UIPopup` 컴포넌트 |

### 5-3. PartyEditUIController + 하위 컴포넌트

**계층 구조:**
```
PartyEditScreen (PartyEditUIController.cs + _screenRoot)
├── BackButton (Button)
├── PartySlotArea (HorizontalLayoutGroup)
│   ├── PartySlot_1 (UIPartySlot.cs)
│   ├── PartySlot_2 (UIPartySlot.cs)
│   └── PartySlot_3 (UIPartySlot.cs)
├── UnitListArea
│   └── UnitListScrollRect (ScrollRect)
│       └── Viewport (Mask)
│           └── UnitListContainer (Transform — UIUnitPortrait 동적 생성)
└── UnitEditPanel (UIUnitEditPanel.cs)
```

**PartyEditUIController 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Party Slots** | `_partySlots` (Size: 3) | `PartySlot_1`, `_2`, `_3` UIPartySlot 컴포넌트 |
| **Unit List** | `_unitListScrollRect` | `UnitListScrollRect` ScrollRect |
| | `_unitListContainer` | `UnitListContainer` Transform |
| | `_unitPortraitPrefab` | `UIUnitPortrait` **프리팹** (Phase Common에서 생성) |
| **Unit Edit Panel** | `_unitEditPanel` | `UnitEditPanel` UIUnitEditPanel 컴포넌트 |
| **Navigation** | `_backButton` | `BackButton` Button |
| **Panel** | `_screenRoot` | 자기 자신의 루트 GameObject |

**UIPartySlot 할당 (×3):**

각 슬롯의 내부 계층:
```
UIPartySlot (UIPartySlot.cs)
├── PositionLabel (TextMeshProUGUI — "1번 슬롯")
├── FilledState (_filledStateRoot)
│   ├── UnitPortrait (UIUnitPortrait)
│   ├── ItemIcon1 (UIItemIcon)
│   └── ItemIcon2 (UIItemIcon)
└── EmptyState (_emptyStateRoot)
    └── EmptyLabel (TextMeshProUGUI — "빈 슬롯")
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Slot Info** | `_slotPosition` | 각각 `1`, `2`, `3` 설정 |
| **Unit Display** | `_unitPortrait` | `UnitPortrait` UIUnitPortrait 컴포넌트 |
| | `_itemIcon1` | `ItemIcon1` UIItemIcon 컴포넌트 |
| | `_itemIcon2` | `ItemIcon2` UIItemIcon 컴포넌트 |
| **Empty State** | `_emptyStateRoot` | `EmptyState` GameObject |
| | `_emptyLabel` | `EmptyLabel` TextMeshProUGUI |
| **Visual** | `_filledStateRoot` | `FilledState` GameObject |
| | `_positionLabel` | `PositionLabel` TextMeshProUGUI |

> **중요**: `_slotPosition` 값을 각 슬롯마다 다르게 설정 (1, 2, 3)

**UIUnitEditPanel 할당:**

```
UIUnitEditPanel (UIUnitEditPanel.cs + CanvasGroup)
├── UnitInfo
│   ├── UnitPortrait (UIUnitPortrait)
│   ├── UnitNameText (TextMeshProUGUI)
│   └── ElementBadge (UIElementBadge)
├── DeckArea
│   ├── DeckSlot_0 ~ DeckSlot_5 (UICard ×6)
│   └── DeckCountText (TextMeshProUGUI — "4/6")
├── SkillArea
│   ├── SkillNameText (TextMeshProUGUI)
│   ├── SkillElementBadge (UIElementBadge)
│   └── SkillButton (Button)
├── ItemArea
│   ├── ItemSlot1 (UIItemIcon + Button)
│   └── ItemSlot2 (UIItemIcon + Button)
├── CardSelectionRoot (GameObject — 카드 선택 팝업)
│   └── CardSelectionContainer (Transform)
├── SkillSelectionRoot (GameObject — 스킬 선택 팝업)
│   └── SkillSelectionContainer (Transform)
└── PanelRoot (GameObject)
```

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Unit Info** | `_unitPortrait` | `UnitPortrait` UIUnitPortrait |
| | `_unitNameText` | `UnitNameText` TextMeshProUGUI |
| | `_elementBadge` | `ElementBadge` UIElementBadge |
| **Deck Slots** | `_deckSlots` (Size: 6) | `DeckSlot_0` ~ `DeckSlot_5` 각각의 UICard |
| | `_deckCountText` | `DeckCountText` TextMeshProUGUI |
| **Skill Slot** | `_skillNameText` | `SkillNameText` TextMeshProUGUI |
| | `_skillElementBadge` | `SkillElementBadge` UIElementBadge |
| | `_skillButton` | `SkillButton` Button |
| **Item Slots** | `_itemSlot1` | `ItemSlot1` UIItemIcon |
| | `_itemSlot2` | `ItemSlot2` UIItemIcon |
| | `_itemButton1` | `ItemSlot1`의 Button 컴포넌트 (없으면 Add Component) |
| | `_itemButton2` | `ItemSlot2`의 Button 컴포넌트 (없으면 Add Component) |
| **Card Selection** | `_cardSelectionRoot` | `CardSelectionRoot` GameObject |
| | `_cardSelectionContainer` | `CardSelectionContainer` Transform |
| | `_cardSelectionPrefab` | `UICard` **프리팹** |
| **Skill Selection** | `_skillSelectionRoot` | `SkillSelectionRoot` GameObject |
| | `_skillSelectionContainer` | `SkillSelectionContainer` Transform |
| **Panel** | `_panelRoot` | `PanelRoot` GameObject |
| | `_canvasGroup` | 자기 자신의 CanvasGroup |
| | `_fadeDuration` | `0.2` (기본값) |

### 5-4. InventoryUIController + 하위 컴포넌트

**계층 구조:**
```
InventoryScreen (InventoryUIController.cs + _screenRoot)
├── BackButton (Button)
├── FilterArea (UIInventoryFilter.cs)
│   ├── TabButtons
│   │   ├── CardTabButton (Button + Image[_cardTabHighlight])
│   │   └── ItemTabButton (Button + Image[_itemTabHighlight])
│   ├── CardFilterRoot (GameObject)
│   │   ├── ElementDropdown (TMP_Dropdown)
│   │   ├── CostDropdown (TMP_Dropdown)
│   │   └── CardTypeDropdown (TMP_Dropdown)
│   ├── ItemFilterRoot (GameObject)
│   │   ├── ItemTypeDropdown (TMP_Dropdown)
│   │   └── TargetStatusDropdown (TMP_Dropdown)
│   └── SortArea
│       ├── SortDropdown (TMP_Dropdown)
│       ├── SortOrderButton (Button)
│       └── SortOrderLabel (TextMeshProUGUI — "▲"/"▼")
├── GridArea (UIInventoryGrid.cs)
│   └── GridContainer (Transform + GridLayoutGroup)
└── ItemDetailPanel (UIItemDetail.cs)
```

**InventoryUIController 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Sub Components** | `_filter` | `FilterArea` UIInventoryFilter 컴포넌트 |
| | `_grid` | `GridArea` UIInventoryGrid 컴포넌트 |
| | `_itemDetail` | `ItemDetailPanel` UIItemDetail 컴포넌트 |
| **Navigation** | `_backButton` | `BackButton` Button |
| **Panel** | `_screenRoot` | 자기 자신의 루트 GameObject |

**UIInventoryFilter 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Tab Buttons** | `_cardTabButton` | `CardTabButton` Button |
| | `_itemTabButton` | `ItemTabButton` Button |
| | `_cardTabHighlight` | `CardTabButton`의 Image |
| | `_itemTabHighlight` | `ItemTabButton`의 Image |
| **Card Filters** | `_cardFilterRoot` | `CardFilterRoot` GameObject |
| | `_elementDropdown` | `ElementDropdown` TMP_Dropdown |
| | `_costDropdown` | `CostDropdown` TMP_Dropdown |
| | `_cardTypeDropdown` | `CardTypeDropdown` TMP_Dropdown |
| **Item Filters** | `_itemFilterRoot` | `ItemFilterRoot` GameObject |
| | `_itemTypeDropdown` | `ItemTypeDropdown` TMP_Dropdown |
| | `_targetStatusDropdown` | `TargetStatusDropdown` TMP_Dropdown |
| **Sort** | `_sortDropdown` | `SortDropdown` TMP_Dropdown |
| | `_sortOrderButton` | `SortOrderButton` Button |
| | `_sortOrderLabel` | `SortOrderLabel` TextMeshProUGUI |
| **Visual** | `_activeTabColor` | `#FFD54F` (금색 — 기본값) |
| | `_inactiveTabColor` | `#424242` (회색 — 기본값) |

> **Dropdown 옵션은 스크립트에서 동적 생성**되므로 인스펙터에서 Options 항목을 미리 설정할 필요 없음.

**UIInventoryGrid 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Pool Settings** | `_iconPrefab` | `UIItemIcon` **프리팹** (Phase Common에서 생성) |
| | `_gridContainer` | `GridContainer` Transform |
| | `_initialPoolSize` | `60` (기본값) |

> GridContainer에 `GridLayoutGroup` 컴포넌트 추가:
> - Cell Size: 64×64 (또는 UIItemIcon 크기에 맞춤)
> - Spacing: 8×8
> - Constraint: Fixed Column Count (화면 너비에 맞게 조정)

**UIItemDetail 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Panel** | `_panelRoot` | 루트 GameObject |
| | `_canvasGroup` | CanvasGroup |
| | `_panelTransform` | RectTransform |
| **Common Info** | `_nameText` | 이름 TextMeshProUGUI |
| | `_descriptionText` | 설명 TextMeshProUGUI |
| | `_rarityText` | 레어도 TextMeshProUGUI |
| | `_artworkImage` | 아트워크 Image |
| | `_elementBadge` | UIElementBadge |
| **Card Specific** | `_cardInfoRoot` | 카드 전용 정보 영역 GameObject |
| | `_costText` | 코스트 TextMeshProUGUI |
| | `_cardTypeText` | 카드 타입 TextMeshProUGUI |
| **Item Specific** | `_itemInfoRoot` | 아이템 전용 정보 영역 GameObject |
| | `_itemTypeText` | 아이템 타입 TextMeshProUGUI |
| | `_targetStatusText` | 대상 상태 TextMeshProUGUI |
| | `_disposableText` | 소모 여부 TextMeshProUGUI |
| **Quantity** | `_quantityText` | 수량 TextMeshProUGUI |
| **Animation** | `_slideDuration` | `0.25` (기본값) |
| | `_hideOffset` | `(300, 0)` (기본값) |

### 5-5. CampaignUIController + 하위 컴포넌트

**계층 구조:**
```
CampaignScreen (CampaignUIController.cs + _screenRoot)
├── BackButton (Button)
├── CampaignList (UICampaignList.cs)
│   ├── ActiveHeader (TextMeshProUGUI — "진행 중")
│   ├── ActiveListContainer (Transform — 항목 동적 생성)
│   ├── EmptyActiveText (TextMeshProUGUI — "진행 중인 캠페인이 없습니다")
│   ├── CompletedHeader (TextMeshProUGUI — "완료")
│   ├── CompletedListContainer (Transform)
│   └── EmptyCompletedText (TextMeshProUGUI)
├── CampaignDetail (UICampaignDetail.cs)
│   └── DetailPanel (CanvasGroup)
│       ├── NameText (TextMeshProUGUI)
│       ├── DescriptionText (TextMeshProUGUI)
│       ├── GoalContainer (Transform)
│       ├── TrackButton (Button + TextMeshProUGUI)
│       └── CloseButton (Button)
└── CampaignTracker (UICampaignTracker.cs — HUD 오버레이)
    └── TrackerPanel (CanvasGroup)
        ├── CampaignNameText (TextMeshProUGUI)
        └── CurrentGoalText (TextMeshProUGUI)
```

**CampaignUIController 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Sub Components** | `_campaignList` | `CampaignList` UICampaignList |
| | `_campaignDetail` | `CampaignDetail` UICampaignDetail |
| | `_campaignTracker` | `CampaignTracker` UICampaignTracker |
| **Navigation** | `_backButton` | `BackButton` Button |
| **Panel** | `_screenRoot` | 자기 자신의 루트 GameObject |

**UICampaignList 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Active Campaign List** | `_activeListContainer` | `ActiveListContainer` Transform |
| | `_activeHeaderText` | `ActiveHeader` TextMeshProUGUI |
| **Completed Campaign List** | `_completedListContainer` | `CompletedListContainer` Transform |
| | `_completedHeaderText` | `CompletedHeader` TextMeshProUGUI |
| **Entry Prefab** | `_campaignEntryPrefab` | 캠페인 항목 **프리팹** (아래 참조) |
| **Empty State** | `_emptyActiveText` | `EmptyActiveText` TextMeshProUGUI |
| | `_emptyCompletedText` | `EmptyCompletedText` TextMeshProUGUI |

> **캠페인 항목 프리팹 생성이 필요하다:**
> `Assets/_Project/Prefabs/UI/Lobby/CampaignEntry.prefab`
> ```
> CampaignEntry (Button + Image)
> ├── CampaignName (TextMeshProUGUI)
> └── ProgressText (TextMeshProUGUI — "2/5")
> ```
> 스크립트는 `UICampaignList`에서 동적으로 텍스트를 설정하므로, 프리팹에는 레이아웃만 구성.

**UICampaignDetail 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Panel** | `_panelRoot` | `DetailPanel` 부모 GameObject |
| | `_canvasGroup` | `DetailPanel` CanvasGroup |
| **Campaign Info** | `_nameText` | `NameText` TextMeshProUGUI |
| | `_descriptionText` | `DescriptionText` TextMeshProUGUI |
| **Goals** | `_goalContainer` | `GoalContainer` Transform |
| | `_goalEntryPrefab` | 목표 항목 **프리팹** (아래 참조) |
| **Track Button** | `_trackButton` | `TrackButton` Button |
| | `_trackButtonText` | `TrackButton` 내부 TextMeshProUGUI |
| **Close Button** | `_closeButton` | `CloseButton` Button |
| **Animation** | `_fadeDuration` | `0.2` (기본값) |
| **Goal Colors** | `_incompleteGoalColor` | `(255, 255, 255, 255)` 흰색 |
| | `_completedGoalColor` | `#9E9E9E` 회색 |

> **목표 항목 프리팹 생성이 필요하다:**
> `Assets/_Project/Prefabs/UI/Lobby/GoalEntry.prefab`
> ```
> GoalEntry (HorizontalLayoutGroup)
> ├── StatusIcon (TextMeshProUGUI — "□" / "✓")
> └── GoalText (TextMeshProUGUI — 목표 설명)
> ```

**UICampaignTracker 할당:**

| Header | 필드 | 할당 대상 |
| :---- | :---- | :---- |
| **Display** | `_campaignNameText` | `CampaignNameText` TextMeshProUGUI |
| | `_currentGoalText` | `CurrentGoalText` TextMeshProUGUI |
| **Panel** | `_panelRoot` | `TrackerPanel` 부모 GameObject |
| | `_canvasGroup` | `TrackerPanel` CanvasGroup |
| **Animation** | `_fadeDuration` | `0.3` (기본값) |

---

## 6. Phase 정산 — UISettlement

### 현재 상태

`UISettlement.cs` 스크립트는 **아직 미구현**이다 (UI 구현 마스터 플랜에 명시).

### 예상 배치

- **위치**: StageScene Canvas 내 오버레이 (UIStageResult와 유사한 구조)
- **트리거**: `GameFlowController.OnSettlementReady` 이벤트
- 스크립트 구현 완료 후 StageScene Canvas 내에 배치 예정

### 향후 작업

- [ ] `UISettlement.cs` 구현 (Phase 정산)
- [ ] StageScene Canvas에 UISettlement 프리팹 배치
- [ ] StageUIController 또는 GameFlowController에서 연동

---

## 7. 검증 체크리스트

### 7-1. Phase Common 프리팹 검증

- [ ] 8개 프리팹이 `Assets/_Project/Prefabs/UI/Common/`에 존재
- [ ] 각 프리팹을 Hierarchy에 드래그하여 Inspector에서 **Missing Reference 없음** 확인
- [ ] UIPopup 프리팹: PopupRoot가 비활성 상태인지 확인
- [ ] UITooltip 프리팹: CanvasGroup alpha가 0인지 확인

### 7-2. Phase 2.5 전투 UI 검증

- [ ] BattleScene에 Canvas + EventSystem 존재
- [ ] BattleUIController의 모든 SerializeField에 **None 없음** 확인
- [ ] `_allyPanels` 배열 Size=3, `_enemyPanels` 배열 Size=5
- [ ] `_cardPrefab` (UIHandArea)에 **프리팹 에셋** 할당 (씬 인스턴스가 아님)
- [ ] `_statusIconPrefab` (UIBattleUnitPanel)에 **프리팹 에셋** 할당
- [ ] UIHPBar의 Fill Image들이 **Filled 타입**으로 설정됨
- [ ] Play 모드에서 BattleManager.InitializeBattle() 호출 시 이벤트 → UI 반영 확인

### 7-3. Phase 3.5 스테이지 UI 검증

- [ ] StageScene에 Canvas + EventSystem 존재
- [ ] `UIHexTile` 프리팹이 `Assets/_Project/Prefabs/UI/Stage/`에 존재
- [ ] UIWorldMap의 ScrollRect 구성 (Viewport, Content 할당)
- [ ] MapContainer 크기가 충분히 큰지 확인 (5000×5000 이상)
- [ ] StageUIController의 모든 SerializeField에 None 없음
- [ ] `_tilePrefab` (UIWorldMap)에 프리팹 에셋 할당
- [ ] `_itemIconPrefab` (UIBagPanel, UIStageResult)에 프리팹 에셋 할당

### 7-4. Phase 4 UI 로비 UI 검증

- [ ] LobbyScene에 Canvas + EventSystem 존재
- [ ] LobbyUIController의 모든 SerializeField에 None 없음
- [ ] `_partyPreviewPortraits` 배열 Size=3
- [ ] `_partySlots` 배열 Size=3, 각 `_slotPosition` 값이 1, 2, 3
- [ ] `_deckSlots` 배열 Size=6
- [ ] 프리팹 참조 확인: `_unitPortraitPrefab`, `_iconPrefab`, `_cardSelectionPrefab`, `_campaignEntryPrefab`, `_goalEntryPrefab`
- [ ] 하위 화면 전환 테스트: 메인 → 파티 편성 → 뒤로가기 → 메인
- [ ] UIInventoryFilter의 TMP_Dropdown 5개가 올바르게 할당됨

### 7-5. 통합 테스트

- [ ] BootScene에서 Play → LobbyScene 전환 → 에러 없음
- [ ] 로비 → 탐험 개시 → StageScene 전환 → 에러 없음
- [ ] 스테이지 → 전투 진입 → BattleScene 전환 → 에러 없음
- [ ] 전투 완료 → 스테이지 복귀 → 에러 없음
- [ ] 각 씬에서 UIPopup.ShowConfirm() 정상 동작
- [ ] UITooltip이 각 씬에서 정상 표시

---

## 부록: 프리팹 저장 경로 요약

```
Assets/_Project/Prefabs/UI/
├── Common/
│   ├── UIElementBadge.prefab
│   ├── UIHPBar.prefab
│   ├── UICard.prefab
│   ├── UIUnitPortrait.prefab
│   ├── UIItemIcon.prefab
│   ├── UIStatusIcon.prefab
│   ├── UITooltip.prefab
│   └── UIPopup.prefab
│
├── Battle/
│   └── (씬 내 직접 구성 — 별도 프리팹 불필요)
│
├── Stage/
│   └── UIHexTile.prefab
│
└── Lobby/
    ├── CampaignEntry.prefab
    └── GoalEntry.prefab
```

---

## 부록: 추가 생성이 필요한 프리팹 목록

스크립트에서 `_xxxPrefab` 형태로 참조하는 프리팹 중, Phase Common 외에 **별도 생성**이 필요한 것:

| 프리팹 | 참조하는 스크립트 | 필드명 | 설명 |
| :---- | :---- | :---- | :---- |
| `CampaignEntry.prefab` | `UICampaignList` | `_campaignEntryPrefab` | 캠페인 리스트 항목 (Button + 이름 + 진행률 텍스트) |
| `GoalEntry.prefab` | `UICampaignDetail` | `_goalEntryPrefab` | 캠페인 목표 항목 (아이콘 + 설명 텍스트) |

> Phase Common의 `UICard`, `UIUnitPortrait`, `UIItemIcon`, `UIStatusIcon`, `UIHexTile` 프리팹은 각 컨트롤러에서 오브젝트 풀링용으로 재사용된다.

---

*작성일: 2026.03.07 | 작성자: Claude Code*
