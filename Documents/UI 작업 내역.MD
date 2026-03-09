# UI 에디터 작업 내역

*마지막 업데이트: 2026.03.09*

이 문서는 MCP for Unity를 활용한 UI 프리팹 에디터 작업의 상세 내역을 기록합니다.

---

## Phase Common — 공용 위젯 프리팹 8종

**작업일**: 2026.03.09
**작업 도구**: MCP for Unity (manage_gameobject, manage_components, manage_prefabs, batch_execute)
**저장 경로**: `Assets/_Project/Prefabs/UI/Common/`
**참조 문서**: `Documents/UI 에디터 작업 매뉴얼.MD` — Phase Common 섹션 (2-1 ~ 2-8)

### 작업 환경

- 활성 씬: BootScene (`Assets/_Project/Scenes/BootScene.unity`)
- Canvas가 없는 상태에서 임시 TempCanvas 생성 (Screen Space Overlay, CanvasScaler 1920x1080, GraphicRaycaster)
- 모든 프리팹 작업 후 TempCanvas 삭제, 씬 저장 완료
- 콘솔 에러: 0건

### 작업 워크플로우

각 프리팹에 대해 동일한 워크플로우를 적용:

1. TempCanvas 하위에 루트 GameObject 생성 (Image 컴포넌트로 RectTransform 강제)
2. 커스텀 스크립트(ProjectStS.UI.*) 추가
3. 자식 오브젝트 생성 및 컴포넌트 구성
4. `manage_components set_property`로 SerializeField 개별 할당
5. `manage_prefabs create_from_gameobject`로 프리팹 저장
6. 씬 인스턴스 삭제

---

### 2-1. UIElementBadge

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIElementBadge.prefab`
**GUID**: `2ce351ed6250da2448422314afeabe4f`
**스크립트**: `ProjectStS.UI.UIElementBadge`

**계층 구조:**
```
UIElementBadge (Image + UIElementBadge.cs) — 60×28
└── Label (TextMeshProUGUI) — fontSize 14, 중앙 정렬, 흰색, Stretch
```

**SerializeField 할당 (2개):**

| 필드명 | 타입 | 할당 대상 |
|--------|------|-----------|
| `_background` | Image | UIElementBadge의 Image |
| `_label` | TextMeshProUGUI | Label의 TMP |

**비고**: 다른 위젯(UICard, UIUnitPortrait)에서 서브 컴포넌트로 재사용되므로 가장 먼저 생성

---

### 2-2. UIHPBar

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIHPBar.prefab`
**GUID**: `2a76bcb3b54f026489fbb59b0dcfa296`
**스크립트**: `ProjectStS.UI.UIHPBar`

**계층 구조:**
```
UIHPBar (Image[투명] + UIHPBar.cs) — 200×24
├── Background (Image — 어두운 회색 #333333) — Stretch
│   ├── DamageFill (Image — 빨간색 #EF5350, Filled, Horizontal, Left) — Stretch
│   └── Fill (Image — 녹색 #66BB6A, Filled, Horizontal, Left) — Stretch
├── HPText (TextMeshProUGUI) — fontSize 14, 중앙 정렬, 흰색, Stretch
└── BlockRoot (GameObject + Image[투명])
    └── BlockText (TextMeshProUGUI) — fontSize 12, 중앙 정렬, 흰색
```

**SerializeField 할당 (8개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_fillImage` | Image | Fill의 Image | — |
| `_damageFillImage` | Image | DamageFill의 Image | — |
| `_hpText` | TextMeshProUGUI | HPText의 TMP | — |
| `_blockText` | TextMeshProUGUI | BlockText의 TMP | — |
| `_blockRoot` | GameObject | BlockRoot | — |
| `_tweenDuration` | float | — | 0.4 |
| `_damageTrailDelay` | float | — | 0.2 |
| `_damageTrailDurationMultiplier` | float | — | 1.5 |

**비고**: Image Fill Type = 3 (Filled), fillMethod = 0 (Horizontal), fillOrigin = 0 (Left)

---

### 2-3. UICard

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UICard.prefab`
**GUID**: `c15e3d138c407a6429e2de2b663eef6f`
**스크립트**: `ProjectStS.UI.UICard`

**계층 구조:**
```
UICard (Image + CanvasGroup + UICard.cs) — 140×200, Raycast Target ON
├── CostText (TextMeshProUGUI) — fontSize 18, 좌상단(5,5), 흰색
├── ElementBadge (UIElementBadge 프리팹 인스턴스) — 우상단(-5,-5)
├── ArtworkImage (Image) — 중앙, 120×100
├── NameText (TextMeshProUGUI) — fontSize 14, 중앙 정렬
├── DescriptionText (TextMeshProUGUI) — fontSize 11, 상단 정렬, 120×50
└── RarityIndicator (Image — 금색 #FFD54F) — 하단, 140×4
```

**SerializeField 할당 (12개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_costText` | TextMeshProUGUI | CostText의 TMP | — |
| `_nameText` | TextMeshProUGUI | NameText의 TMP | — |
| `_descriptionText` | TextMeshProUGUI | DescriptionText의 TMP | — |
| `_elementBadge` | UIElementBadge | ElementBadge | — |
| `_artworkImage` | Image | ArtworkImage의 Image | — |
| `_rarityIndicator` | Image | RarityIndicator의 Image | — |
| `_cardBackground` | Image | UICard 루트의 Image | — |
| `_canvasGroup` | CanvasGroup | UICard 루트의 CanvasGroup | — |
| `_hoverScale` | float | — | 1.1 |
| `_hoverOffsetY` | float | — | 30 |
| `_hoverDuration` | float | — | 0.15 |
| `_dragAlpha` | float | — | 0.7 |

**비고**: ElementBadge는 UIElementBadge.prefab을 `prefab_path`로 인스턴스화하여 네스티드 프리팹으로 포함

---

### 2-4. UIUnitPortrait

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIUnitPortrait.prefab`
**GUID**: `857ed3a43d9781a4bb5a400f880991a6`
**스크립트**: `ProjectStS.UI.UIUnitPortrait`

**계층 구조:**
```
UIUnitPortrait (Image[투명] + UIUnitPortrait.cs) — 90×120
├── FrameBorder (Image — #424242 테두리) — Stretch
│   └── PortraitImage (Image) — 패딩 4px
├── PlaceholderName (TextMeshProUGUI) — fontSize 12, 중앙 정렬
├── NameText (TextMeshProUGUI) — fontSize 12, 중앙 정렬
└── ElementBadge (UIElementBadge 프리팹 인스턴스)
```

**SerializeField 할당 (7개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_portraitImage` | Image | PortraitImage의 Image | — |
| `_placeholderNameText` | TextMeshProUGUI | PlaceholderName의 TMP | — |
| `_nameText` | TextMeshProUGUI | NameText의 TMP | — |
| `_elementBadge` | UIElementBadge | ElementBadge | — |
| `_frameBorder` | Image | FrameBorder의 Image | — |
| `_highlightColor` | Color | — | #FFD54F |
| `_normalColor` | Color | — | #424242 |

---

### 2-5. UIItemIcon

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIItemIcon.prefab`
**GUID**: `067c0b2c8fdebff4b941994c119c9755`
**스크립트**: `ProjectStS.UI.UIItemIcon`

**계층 구조:**
```
UIItemIcon (Image[투명] + UIItemIcon.cs) — 64×64
├── RarityBorder (Image — 금색 #FFD54F 테두리) — Stretch
│   └── IconImage (Image) — 패딩 3px
├── NameAbbreviation (TextMeshProUGUI) — fontSize 10, 중앙 정렬
└── QuantityRoot (Image[투명])
    └── QuantityText (TextMeshProUGUI) — fontSize 10, 우하단
```

**SerializeField 할당 (5개):**

| 필드명 | 타입 | 할당 대상 |
|--------|------|-----------|
| `_iconImage` | Image | IconImage의 Image |
| `_rarityBorder` | Image | RarityBorder의 Image |
| `_nameAbbreviation` | TextMeshProUGUI | NameAbbreviation의 TMP |
| `_quantityText` | TextMeshProUGUI | QuantityText의 TMP |
| `_quantityRoot` | GameObject | QuantityRoot |

---

### 2-6. UIStatusIcon

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIStatusIcon.prefab`
**GUID**: `a12d84700289038449ea05f39340089b`
**스크립트**: `ProjectStS.UI.UIStatusIcon`

**계층 구조:**
```
UIStatusIcon (Image[투명] + UIStatusIcon.cs) — 36×36
├── Background (Image — 파랑 #42A5F5) — Stretch
│   └── IconImage (Image) — 패딩 2px
├── StackRoot (Image[투명]) — 우하단
│   └── StackText (TextMeshProUGUI) — fontSize 10, 중앙 정렬, 흰색
└── DurationRoot (Image[투명]) — 좌하단
    └── DurationText (TextMeshProUGUI) — fontSize 10, 중앙 정렬, 흰색
```

**SerializeField 할당 (8개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_iconImage` | Image | IconImage의 Image | — |
| `_background` | Image | Background의 Image | — |
| `_stackText` | TextMeshProUGUI | StackText의 TMP | — |
| `_durationText` | TextMeshProUGUI | DurationText의 TMP | — |
| `_stackRoot` | GameObject | StackRoot | — |
| `_durationRoot` | GameObject | DurationRoot | — |
| `_buffColor` | Color | — | #42A5F5 |
| `_debuffColor` | Color | — | #EF5350 |

---

### 2-7. UITooltip

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UITooltip.prefab`
**GUID**: `d488ff8390df53548b2be3bcfde643eb`
**스크립트**: `ProjectStS.UI.UITooltip`

**계층 구조:**
```
UITooltip (Canvas[Overlay, Sort Order 999] + CanvasScaler[1920×1080] + GraphicRaycaster + UITooltip.cs)
└── TooltipPanel (RectTransform + CanvasGroup[alpha=0] + Image[배경 #2A2A2A]) — 300×150
    ├── TitleText (TextMeshProUGUI) — fontSize 16, 굵게, 좌상단 정렬, 흰색
    └── BodyText (TextMeshProUGUI) — fontSize 13, 좌상단 정렬, #CCCCCC
```

**SerializeField 할당 (5개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_tooltipPanel` | RectTransform | TooltipPanel의 RectTransform | — |
| `_titleText` | TextMeshProUGUI | TitleText의 TMP | — |
| `_bodyText` | TextMeshProUGUI | BodyText의 TMP | — |
| `_canvasGroup` | CanvasGroup | TooltipPanel의 CanvasGroup | — |
| `_offset` | Vector2 | — | (16, -16) |

**비고**: 별도 Canvas(Overlay, Sort Order 999)로 다른 모든 UI 위에 표시. 시작 시 canvasGroup.alpha = 0으로 비활성

---

### 2-8. UIPopup

**프리팹**: `Assets/_Project/Prefabs/UI/Common/UIPopup.prefab`
**GUID**: `d794e23be0675a14f89eead6d60a8482`
**스크립트**: `ProjectStS.UI.UIPopup`

**계층 구조:**
```
UIPopup (Image[투명] + UIPopup.cs)
└── PopupRoot (Image[투명], 시작 시 SetActive=false)
    ├── DimBackground (Image — 반투명 검정 rgba(0,0,0,0.5), Stretch, Raycast Target ON)
    └── Panel (RectTransform + Image[배경 #2A2A2A]) — 중앙, 400×200
        ├── MessageText (TextMeshProUGUI) — fontSize 18, 중앙 정렬, 흰색
        ├── ConfirmButtonRoot (Image[투명])
        │   └── ConfirmButton (Image[#4CAF50 녹색] + Button)
        │       └── ConfirmButtonText (TextMeshProUGUI) — "확인", fontSize 16, 중앙 정렬, 흰색
        └── CancelButtonRoot (Image[투명])
            └── CancelButton (Image[#F44336 빨간색] + Button)
                └── CancelButtonText (TextMeshProUGUI) — "취소", fontSize 16, 중앙 정렬, 흰색
```

**SerializeField 할당 (11개):**

| 필드명 | 타입 | 할당 대상 | 기본값 |
|--------|------|-----------|--------|
| `_popupRoot` | GameObject | PopupRoot | — |
| `_dimBackground` | Image | DimBackground의 Image | — |
| `_panelTransform` | RectTransform | Panel의 RectTransform | — |
| `_messageText` | TextMeshProUGUI | MessageText의 TMP | — |
| `_confirmButtonRoot` | GameObject | ConfirmButtonRoot | — |
| `_cancelButtonRoot` | GameObject | CancelButtonRoot | — |
| `_confirmButtonText` | TextMeshProUGUI | ConfirmButtonText의 TMP | — |
| `_cancelButtonText` | TextMeshProUGUI | CancelButtonText의 TMP | — |
| `_animDuration` | float | — | 0.25 |
| `_defaultConfirmText` | string | — | "확인" |
| `_defaultCancelText` | string | — | "취소" |

**비고**: PopupRoot는 시작 시 비활성(SetActive false). Button의 OnClick은 스크립트에서 런타임 자동 바인딩

---

## MCP 작업 시 발견된 이슈 및 해결법

Phase Common 프리팹 생성 과정에서 발견된 MCP for Unity 도구 관련 이슈를 기록합니다.
이후 Phase 에디터 작업 시 동일 패턴을 재사용할 수 있도록 합니다.

### 이슈 1: 중첩 폴더 일괄 생성 불가

- **현상**: `manage_asset create_folder` 에 `Assets/_Project/Prefabs/UI/Common` 경로를 전달하면, 상위 폴더(`Prefabs`)가 없어 실패
- **해결**: 한 단계씩 순차 생성 — `Prefabs` → `UI` → `Common`

### 이슈 2: SerializeField 할당 시 properties dict 키 자동 변환

- **현상**: `manage_components set_property`의 `properties` (dict) 파라미터로 `{"_background": {...}}` 전달 시, 키가 자동으로 PascalCase 변환되어 `"Background"`로 검색됨 → 필드를 찾지 못함
- **해결**: 개별 `property`/`value` (단수) 파라미터로 호출하면 키 변환 없이 정확한 필드명 매칭 가능

### 이슈 3: Canvas 하위 RectTransform 생성 조건

- **현상**: Canvas 하위에 `manage_gameobject create`로 빈 GameObject를 생성하면 일반 `Transform`이 부착됨 (RectTransform이 아님)
- **해결**: UI 컴포넌트(Image 등)를 함께 추가하면 자동으로 RectTransform으로 변환됨. 시각적 표시가 불필요한 컨테이너에는 투명 Image(alpha=0) 추가

### 이슈 4: 프리팹 생성 시 target 타입

- **현상**: `manage_prefabs create_from_gameobject`의 `target` 파라미터에 정수 instanceID를 전달하면 pydantic 타입 검증 실패
- **해결**: 문자열로 GameObject 이름을 전달 (예: `"UIElementBadge"`)

### 이슈 5: Button + TextMeshProUGUI 동일 오브젝트 제한

- **현상**: 동일 GameObject에 Image + Button + TMPro.TextMeshProUGUI를 모두 추가하면 에러 (disallowed)
- **해결**: Button 오브젝트(Image + Button)와 텍스트 자식 오브젝트(TextMeshProUGUI)를 분리 구성

---

## 총 작업 요약

| 항목 | 수치 |
|------|------|
| 생성된 프리팹 | 8개 |
| 총 SerializeField 할당 | 58개 |
| 네스티드 프리팹 인스턴스 | 2개 (UICard, UIUnitPortrait 내 UIElementBadge) |
| 콘솔 에러 | 0건 |
| 프리팹 저장 경로 | `Assets/_Project/Prefabs/UI/Common/` |

---

## Phase 3.5 — 스테이지 씬 UI 배치

**작업일**: 2026.03.09
**작업 도구**: MCP for Unity (manage_gameobject, manage_components, manage_prefabs, manage_scene, batch_execute)
**작업 씬**: `Assets/_Project/Scenes/StageScene.unity`
**참조 문서**: `Documents/UI 에디터 작업 매뉴얼.MD` — Phase 3.5 섹션 (4-1 ~ 4-8)

### 작업 환경

- 활성 씬: StageScene (build_index: 2)
- 기존 오브젝트: StageSceneBootstrap + StageManager만 존재
- 작업 후 콘솔 에러: **0건**

### 생성된 프리팹

| 프리팹 | 경로 | GUID | SerializeField |
|--------|------|------|:-:|
| UIHexTile | `Assets/_Project/Prefabs/UI/Stage/UIHexTile.prefab` | `c8d16788bfc28cd44b7e8e47356debc9` | 5개 |

**UIHexTile 계층 구조:**
```
UIHexTile (Image + CanvasGroup + UIHexTile.cs)
├── EventIcon (Image)
├── EventIconLabel (TextMeshProUGUI)
└── HighlightBorder (Image — 시작 시 비활성)
```

### 씬 배치 계층 구조

```
StageScene
├── StageSceneBootstrap (기존)
├── StageManager (기존)
├── Canvas (Screen Space Overlay, CanvasScaler 1920×1080, matchWidthOrHeight=0.5)
│   ├── StageUIRoot (Image[투명] + StageUIController.cs)
│   │   ├── WorldMap (Image[#1A1A26] + ScrollRect + UIWorldMap.cs)
│   │   │   └── Viewport (Image + Mask)
│   │   │       └── MapContainer (Image[투명], 5000×5000)
│   │   ├── StageHUD (Image[투명] + UIStageHUD.cs)
│   │   │   ├── APArea (Image[투명])
│   │   │   │   ├── APFillBar (Image — Filled, Horizontal, Left, 녹색 #4CAF50)
│   │   │   │   └── APText (TMP)
│   │   │   ├── ZoneInfo (Image[투명] + CanvasGroup)
│   │   │   │   ├── ZoneNameText (TMP)
│   │   │   │   └── ZoneDescText (TMP)
│   │   │   └── EventCountText (TMP)
│   │   ├── BagPanel (Image[투명] + UIBagPanel.cs)
│   │   │   ├── ToggleButton (Image + Button)
│   │   │   │   └── ToggleButtonText (TMP — "가방 (0)")
│   │   │   └── PanelRoot (Image + CanvasGroup[alpha=0])
│   │   │       └── ItemContainer (Image[투명] + GridLayoutGroup[64×64, spacing 8])
│   │   ├── EventPopup (Image[투명] + UIEventPopup.cs — **시작 시 비활성**)
│   │   │   ├── EventPopupDim (Image — rgba(0,0,0,0.5))
│   │   │   └── EventPopupPanel (Image — #2A2A33)
│   │   │       ├── EventTypeBanner (Image)
│   │   │       ├── EventTypeLabel (TMP)
│   │   │       ├── EventNameText (TMP)
│   │   │       ├── EventConfirmBtn (Image[#4CAF50] + Button)
│   │   │       │   └── EventConfirmText (TMP — "진입")
│   │   │       └── EventCancelBtn (Image[#F44336] + Button)
│   │   │           └── EventCancelText (TMP — "취소")
│   │   └── StageResult (Image[투명] + UIStageResult.cs — **시작 시 비활성**)
│   │       ├── StageResultDim (Image — rgba(0,0,0,0.5))
│   │       └── ResultPanel (Image — #2A2A33)
│   │           ├── ResultTitle (TMP)
│   │           ├── ResultReasonText (TMP)
│   │           ├── ResultBanner (Image)
│   │           ├── FixedRewardArea (Image[투명])
│   │           │   ├── FixedRewardLabel (TMP — "보상 선택")
│   │           │   └── FixedRewardContainer (Image[투명] + GridLayoutGroup[64×64, spacing 8])
│   │           ├── CompletionRewardArea (Image[투명])
│   │           │   ├── CompletionRewardLabel (TMP — "완료 보상")
│   │           │   └── CompletionRewardContainer (Image[투명] + GridLayoutGroup[64×64, spacing 8])
│   │           ├── BonusRewardArea (Image[투명])
│   │           │   ├── BonusRewardLabel (TMP — "추가 보상")
│   │           │   └── BonusRewardContainer (Image[투명] + GridLayoutGroup[64×64, spacing 8])
│   │           └── ResultConfirmBtn (Image[#4CAF50] + Button)
│   │               └── ResultConfirmText (TMP — "확인")
│   └── UIPopup (Common 프리팹 인스턴스)
├── EventSystem (EventSystem + StandaloneInputModule)
└── UITooltip (Common 프리팹 인스턴스 — Canvas Sort Order 999)
```

### SerializeField 할당 요약

| 컴포넌트 | 명시 할당 | 기본값 스킵 | 합계 |
|----------|:-:|:-:|:-:|
| UIHexTile (프리팹) | 5 | 1 | 6 |
| StageUIController | 6 | 0 | 6 |
| UIWorldMap | 3 | 8 | 11 |
| UIStageHUD | 6 | 3 | 9 |
| UIBagPanel | 6 | 4 | 10 |
| UIEventPopup | 10 | 3 | 13 |
| UIStageResult | 18 | 3 | 21 |
| **합계** | **54** | **22** | **76** |

### 프리팹 참조

| SerializeField | 프리팹 | GUID |
|----------------|--------|------|
| UIWorldMap._tilePrefab | UIHexTile.prefab | `c8d16788bfc28cd44b7e8e47356debc9` |
| UIBagPanel._itemIconPrefab | UIItemIcon.prefab | `067c0b2c8fdebff4b941994c119c9755` |
| UIStageResult._rewardIconPrefab | UIItemIcon.prefab | `067c0b2c8fdebff4b941994c119c9755` |

### 비활성 오브젝트

- `EventPopup`: SetActive(false) — 이벤트 진입 시 스크립트에서 활성화
- `StageResult`: SetActive(false) — 스테이지 결과 표시 시 스크립트에서 활성화

### MCP 추가 이슈

#### 이슈 6: 프리팹 에셋 참조 GUID 정확성

- **현상**: `_tilePrefab` 할당 시 잘못된 GUID를 사용하면 "Failed to convert value" 에러 발생
- **해결**: `manage_asset get_info`로 정확한 GUID를 먼저 조회한 후 `{"guid": "정확한GUID"}` 형태로 전달

#### 이슈 7: manage_gameobject target 타입 (modify)

- **현상**: `manage_gameobject modify`의 `target`에 정수 instanceID 전달 시 pydantic string 타입 검증 실패
- **해결**: 문자열 이름으로 전달 (예: `"EventPopup"`)

---

## 총 작업 요약 (누적)

| 항목 | Phase Common | Phase 3.5 | 합계 |
|------|:-:|:-:|:-:|
| 생성된 프리팹 | 8개 | 1개 | 9개 |
| SerializeField 할당 | 58개 | 54개 | 112개 |
| 씬 배치 오브젝트 | — | 38개 | 38개 |
| 콘솔 에러 | 0건 | 0건 | 0건 |

---

## 후속 작업

- [ ] **Phase 2.5 에디터**: 전투 씬 Canvas + 프리팹 배치, SerializeField 할당
- [ ] **Phase 4 UI 에디터**: 로비 씬 Canvas + 프리팹 배치, 파티/인벤토리/캠페인 UI 구성
